from contextlib import asynccontextmanager

from fastapi import FastAPI

from app.db import close_pool, open_pool
from app.errors import register_error_handlers
from app.routes import admin, attachments, auth, components, dependencies, projects, remarks, search, tasks, teams


@asynccontextmanager
async def lifespan(app: FastAPI):
    open_pool()
    yield
    close_pool()


app = FastAPI(title="ProjectPal V2 REST API", lifespan=lifespan)

register_error_handlers(app)

app.include_router(auth.router)
app.include_router(search.router)
app.include_router(teams.router)
app.include_router(projects.router)
app.include_router(components.router)
app.include_router(tasks.router)
app.include_router(dependencies.router)
app.include_router(attachments.router)
app.include_router(remarks.router)
app.include_router(admin.router)
