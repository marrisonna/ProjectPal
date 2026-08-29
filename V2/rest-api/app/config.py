import os

DATABASE_URL = os.environ["DATABASE_URL"]
JWT_SECRET = os.environ["JWT_SECRET"]
JWT_ALGORITHM = "HS256"
JWT_TTL_SECONDS = 8 * 60 * 60  # 8 hours — Level 1 has no refresh-token story yet.
