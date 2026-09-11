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

| Actor | Status |
|---|---|
| Student frontend | Live — wired to API |
| Staff POS | Static wireframe |
| Kitchen display | Static wireframe (SSE stream ready at `/api/kitchen/stream`) |
| Admin | Deferred |

## Design Principles

- Balance lives on the student account, not the card. Losing a card never means losing money.
- Lost card flow: lock old card, issue new card, balance carries over intact.
- Pre-order and walk-in queues are always separate — never merged.

## Demo Credentials

| Username | Password | Role |
|---|---|---|
| admin | admin123 | Admin |
| staff1 | staff123 | Counter staff |
| staff2 | staff123 | Counter staff |

## Database

Schema in `docs/schema.dbml` — paste into [dbdiagram.io](https://dbdiagram.io) to visualize. Source of truth is `app/models.py`.
