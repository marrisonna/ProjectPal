"""Maps Postgres exceptions to clean HTTP responses (Plan.md §4.4) — the
three DB-enforced business rules (dependency-cycle rejection, Remark
authorship reassignment, Attachment dedup) and ordinary constraint
violations alike. Nothing here should ever leak a raw Postgres/psycopg
exception string to the client.
"""

import re

import psycopg.errors
from fastapi import FastAPI, Request
from fastapi.responses import JSONResponse


def _error(message: str, status_code: int) -> JSONResponse:
    return JSONResponse(status_code=status_code, content={"error": message})


def _clean_raise_message(exc: psycopg.errors.RaiseException) -> str:
    # PL/pgSQL RAISE EXCEPTION messages here are already user-facing text
    # (see 1_DatabaseSetup's trigger functions) — just strip the parenthetical
    # id detail psycopg appends, keep the sentence itself.
    message = str(exc).split("\n")[0].strip()
    return re.sub(r"\s*\(remark_id = \d+\)$", "", message)


def register_error_handlers(app: FastAPI) -> None:
    @app.exception_handler(psycopg.errors.RaiseException)
    def _raise_exception_handler(request: Request, exc: psycopg.errors.RaiseException):
        # Both DB-enforced rules implemented as RAISE EXCEPTION (dependency
        # cycles, Remark authorship reassignment) land here — both represent
        # "this operation conflicts with an existing invariant", hence 409.
        return _error(_clean_raise_message(exc), 409)

    @app.exception_handler(psycopg.errors.UniqueViolation)
    def _unique_violation_handler(request: Request, exc: psycopg.errors.UniqueViolation):
        diag = exc.diag
        if diag and diag.constraint_name == "ux_attachment_dedup":
            return _error("An identical attachment already exists on this item", 409)
        return _error("This value already exists", 409)

    @app.exception_handler(psycopg.errors.ForeignKeyViolation)
    def _foreign_key_violation_handler(request: Request, exc: psycopg.errors.ForeignKeyViolation):
        return _error("Refers to a record that doesn't exist", 400)

    @app.exception_handler(psycopg.errors.NotNullViolation)
    def _not_null_violation_handler(request: Request, exc: psycopg.errors.NotNullViolation):
        diag = exc.diag
        field = diag.column_name if diag else None
        return _error(f"'{field}' is required" if field else "A required field is missing", 400)

    @app.exception_handler(psycopg.errors.CheckViolation)
    def _check_violation_handler(request: Request, exc: psycopg.errors.CheckViolation):
        return _error("Violates a data rule for this record", 400)

    @app.exception_handler(psycopg.Error)
    def _generic_db_error_handler(request: Request, exc: psycopg.Error):
        # Catch-all so nothing ever surfaces a raw psycopg/Postgres string to
        # the client — anything reaching here is a bug worth investigating
        # server-side, not a client-facing detail.
        return _error("Unexpected database error", 500)
