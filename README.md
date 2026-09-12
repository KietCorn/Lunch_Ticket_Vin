# LunchCard

A student lunch card management system that replaces a manual, paper-based cafeteria process with a digital system. Money is decoupled from the physical card — balance lives on the student account, so losing a card never means losing money.

## Quick Start

```bash
pip install fastapi uvicorn sqlalchemy python-multipart
uvicorn app.main:app --reload
```

Seed demo data (run once after first start):

```bash
python -m app.seed
```

- API: http://localhost:8000
- Swagger UI: http://localhost:8000/docs

Open `frontend/student/index.html` directly in your browser. Log in with `staff1 / staff123` (demo limitation: student auth not yet implemented).

## Stack

- **Backend:** FastAPI + SQLAlchemy + SQLite
- **Realtime:** Server-Sent Events (SSE) for kitchen display
- **Frontend:** Vanilla JS, no build step required

## Features

| Actor            | Status                                                        |
| ---------------- | ------------------------------------------------------------- |
| Student frontend | Live — wired to API                                          |
| Staff POS        | Static wireframe                                              |
| Kitchen display  | Static wireframe (SSE stream ready at`/api/kitchen/stream`) |
| Admin            | Deferred                                                      |

## Design Principles

- Balance lives on the student account, not the card. Losing a card never means losing money.
- Lost card flow: lock old card, issue new card, balance carries over intact.
- Pre-order and walk-in queues are always separate — never merged.

## Demo Credentials

| Username | Password | Role          |
| -------- | -------- | ------------- |
| admin    | admin123 | Admin         |
| staff1   | staff123 | Counter staff |
| staff2   | staff123 | Counter staff |

## Database

Schema in `docs/schema.dbml` — paste into [dbdiagram.io](https://dbdiagram.io) to visualize. Source of truth is `app/models.py`.

## Documentation / BA Artifacts

The `docs/` folder holds the analysis & design trail for this project — use case spec, ERD, sequence diagrams, and a traceability map back to the real API. See `docs/README.md` for the full work log; short version:

| File | What it is |
| --- | --- |
| `docs/usecase.md` | Use case specification — 5 actors, 26 use cases, preconditions/flows/postconditions |
| `docs/Usecase_diagram.png` | Use case diagram (draw.io) |
| `docs/erd.md` | Entity-relationship diagram (Mermaid) + FK-to-use-case mapping |
| `docs/schema.dbml` | dbdiagram.io schema, kept in sync with `app/models.py` |
| `docs/schema.sql` | `CREATE TABLE` statements generated straight from `app/models.py` — regenerate, don't hand-edit |
| `docs/sequence-diagram-guide.md` | Self-guided method for building sequence diagrams (Mermaid); one worked example, rest left to design |
| `docs/api-mapping.md` | Maps each use case / sequence diagram step to the actual endpoint in `app/routers/*.py` |
| `docs/notes.md` | UML + UI/UX study notes |

**⚠ Known gap, confirmed by reading the code (see `docs/api-mapping.md`):** the backend has **no Admin endpoints implemented** — `menu.py` only exposes `GET` routes, and there is no router at all for staff/student CRUD or transaction reports. This matches the "Admin: Deferred" status above, but is called out explicitly here so it isn't mistaken for an oversight in the docs — the use case spec (`usecase.md`) documents the *intended* Admin flows (UC-A01–UC-A07), not what's currently running.
