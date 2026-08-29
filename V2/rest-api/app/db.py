from contextlib import contextmanager

from psycopg_pool import ConnectionPool

from app.config import DATABASE_URL

# One shared pool for the whole service (Claude/Level1_Implementation/2_RestApi/Plan.md
# §5.1) — every route borrows a connection from here rather than opening its own.
pool = ConnectionPool(DATABASE_URL, min_size=1, max_size=10, open=False)


def open_pool() -> None:
    pool.open()


def close_pool() -> None:
    pool.close()


@contextmanager
def get_conn():
    with pool.connection() as conn:
        conn.execute("SET search_path TO projectpal")
        yield conn


def one(cur) -> dict | None:
    row = cur.fetchone()
    if row is None:
        return None
    columns = [c.name for c in cur.description]
    return dict(zip(columns, row))


def many(cur) -> list[dict]:
    columns = [c.name for c in cur.description]
    return [dict(zip(columns, row)) for row in cur.fetchall()]
