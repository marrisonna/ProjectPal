"""Password hashing (3_Authentication/Plan.md §3.1, D1.3-5) — Argon2id via
argon2-cffi, using the library's own default parameters rather than
hand-tuned ones (Level 1 has no throughput/latency target to tune against).
"""

from argon2 import PasswordHasher
from argon2.exceptions import VerificationError, InvalidHashError, VerifyMismatchError

_hasher = PasswordHasher()


def hash_password(password: str) -> str:
    return _hasher.hash(password)


def verify_password(password: str, password_hash: str | None) -> bool:
    if password_hash is None:
        return False
    try:
        return _hasher.verify(password_hash, password)
    except (VerifyMismatchError, VerificationError, InvalidHashError):
        return False
