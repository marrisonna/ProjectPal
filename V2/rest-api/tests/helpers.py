def auth(token: str) -> dict:
    return {"Authorization": f"Bearer {token}"}
