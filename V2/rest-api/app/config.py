import os

DATABASE_URL = os.environ["DATABASE_URL"]
JWT_SECRET = os.environ["JWT_SECRET"]
JWT_ALGORITHM = "HS256"
# Default 8 hours — Level 1 has no refresh-token story yet, so "log in again"
# on expiry is acceptable (3_Authentication/Plan.md D1.3-6). Configurable via
# environment rather than hardcoded, so it can be tuned without a code change.
JWT_TTL_SECONDS = int(os.environ.get("JWT_TTL_SECONDS", 8 * 60 * 60))
