---
name: lunchcard-coding
description: Development conventions for the student lunch card system. Load when writing application code.
---

Load `/lunchcard-context` first if the full project context isn't already in scope.

---

## Tech Stack

```
Backend:   FastAPI (Python)
Database:  SQLite via SQLAlchemy (ORM)
Realtime:  Server-Sent Events (SSE) — kitchen display only
Frontend:  Vanilla JS + Fetch API, building on existing HTML wireframes (no build step)
Auth:      Session-based (HTTP-only cookie); JWT is overkill for a local demo
Hosting:   Local machine only (demo context — no deployment target)
Student data: Standalone demo — no real school API integration; seed data replaces the external REST API
```

### Why these choices (do not re-litigate without new constraints)
- **FastAPI over Flask:** `StreamingResponse` handles SSE cleanly in async context; Flask requires thread-per-connection workaround. Auto-docs (`/docs`) save debugging time on a short timeline.
- **SSE over WebSocket:** Kitchen display only receives data. SSE is one-directional, natively supported via `EventSource`, and fires `onerror` on disconnect — the reconnection banner in the wireframe is free.
- **SQLite over PostgreSQL:** Zero-config for local demo. Swap to PostgreSQL if ever deployed to a real server (SQLAlchemy makes this a one-line change).
- **Vanilla JS over React/Vue:** No build step; frontend builds directly on the existing HTML wireframes. Fetch API + `EventSource` cover everything needed.

---

## API Design

Use REST. Follow these conventions once the backend is implemented:

- Resource URLs are plural nouns: `/api/orders`, `/api/students/{id}/account`
- Mutations use the appropriate verb: `POST` to create, `PATCH` to partial-update, `DELETE` to remove
- Balance mutations are **never a direct PATCH on `accounts.balance`** — they go through a dedicated endpoint (e.g., `POST /api/accounts/{id}/topup`, `POST /api/orders/{id}/confirm`) that writes the transaction atomically
- Return the updated resource (or at minimum the new balance) in the response body — the client must never need a second request to show the user their new state

---

## Critical Implementation Rules

### Balance atomicity
Any operation that changes `accounts.balance` must:
1. Check balance sufficiency inside the same transaction (not before it)
2. Write a `transactions` row in the same commit
3. Return a 409 Conflict (not 400) if the balance is insufficient — it's a concurrency-safe check, not a validation error

### Slot reservation
Decrement `daily_menu.available_quantity` inside a transaction with a row-level lock. Treat a 0-count as a hard stop — return 409 and do not create the order.

### Card operations
Locking or replacing a card must never touch `accounts.balance`. A card swap is a write to `cards` only. Verify this in tests.

### Pre-order vs. walk-in
`order.type` is set at creation time and must never be changed after the fact. Endpoints, queries, and reports must preserve this distinction — no "normalize everything into one list" shortcuts.

---

## FastAPI-Specific Conventions

### Project structure
```
app/
  main.py          # FastAPI app instance, router includes, lifespan
  database.py      # SQLAlchemy engine + session factory (SQLite)
  models.py        # SQLAlchemy ORM models
  schemas.py       # Pydantic request/response schemas
  routers/
    students.py
    orders.py
    accounts.py
    menu.py
    kitchen.py     # SSE stream endpoint lives here
  seed.py          # Demo data (replaces school API integration)
```

### SSE pattern for kitchen display
```python
# routers/kitchen.py
from fastapi.responses import StreamingResponse
import asyncio, json

async def event_generator(request):
    while True:
        if await request.is_disconnected():
            break
        data = get_pending_orders_snapshot()   # synchronous DB read
        yield f"data: {json.dumps(data)}\n\n"
        await asyncio.sleep(2)                 # push every 2s

@router.get("/api/kitchen/stream")
async def kitchen_stream(request: Request):
    return StreamingResponse(event_generator(request), media_type="text/event-stream")
```

### Database session dependency
```python
# Always use the dependency, never create sessions manually in route handlers
def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()

@router.post("/api/orders/{order_id}/confirm")
def confirm_order(order_id: int, db: Session = Depends(get_db)):
    ...
```

### Balance mutation pattern
Never mutate `accounts.balance` directly via PATCH. Always use a dedicated endpoint that atomically updates balance and writes a `transactions` row:
```python
with db.begin():
    account = db.query(Account).filter_by(student_id=sid).with_for_update().first()
    if account.balance < amount:
        raise HTTPException(status_code=409, detail={"code": "INSUFFICIENT_BALANCE", ...})
    account.balance -= amount
    db.add(Transaction(account_id=account.id, amount=-amount, reference=order_id))
```

---

## Error Response Format

```json
{
  "error": {
    "code": "INSUFFICIENT_BALANCE",
    "message": "Account balance (25000) is less than the order total (35000).",
    "details": {}
  }
}
```

Use machine-readable `code` values for client-side branching. Keep `message` human-readable. Never expose stack traces in production responses.

---

## Testing Expectations

These scenarios must have tests regardless of the testing framework chosen:

- Balance cannot go negative (concurrent deductions)
- Slot count cannot go below zero (concurrent orders)
- Locking a card does not affect balance
- Replacing a card preserves the full balance on the new card
- Pre-order and walk-in are always stored with distinct `type` values
- QR expiry is enforced server-side (not just client-side)

---

## Known Limitations (accepted for demo scope)

**One-active-card-per-student is app-level, not DB-enforced.**
The partial unique index needed for this constraint (`WHERE status = 'active'`) requires raw DDL in SQLite — SQLAlchemy's `UniqueConstraint` doesn't support it. The guard lives in `routers/students.py:issue_card`. This means two concurrent card-issuance requests for the same student could theoretically both pass the check and create two active cards. At demo scale (local, single staff terminal) this is an accepted risk. Fix for production: add the partial index via `op.execute("CREATE UNIQUE INDEX ...")` in a migration, or switch to PostgreSQL which supports it via `UniqueConstraint(..., postgresql_where=...)`.

**One-non-cancelled-order-per-student-per-slot is app-level, not DB-enforced.**
Same root cause: the uniqueness is status-aware (cancelled orders don't count), which SQLite's `UniqueConstraint` can't express. The guard lives in `routers/orders.py:place_preorder` — it queries for an existing non-cancelled order before inserting. Race condition risk is identical to the card case, and accepted for the same reason.

**QR expiry does not auto-cancel orders.**
When a QR expires, the order stays `pending`, the slot remains consumed, and the balance stays deducted. Staff can still confirm via `manual_entry` source (which bypasses the expiry check). There is no background job to sweep up un-collected pending orders after a timeslot ends. For a production system this would need a scheduled task (e.g., mark all `pending` orders `cancelled` 30 minutes after timeslot end, refund balances, restore slots).

**Student auth is not implemented — the student frontend uses staff credentials.**
The demo has one auth system (the `Staff` table). The student frontend (`frontend/student/`) logs in with staff credentials and hardcodes student MSSV 20110001. A real deployment would need a `Student` password column or SSO integration.

---

## Local Dev Setup

```bash
pip install fastapi uvicorn sqlalchemy python-multipart
uvicorn app.main:app --reload       # API at http://localhost:8000
                                    # Swagger UI at http://localhost:8000/docs
```

Open frontend HTML files directly in the browser — no dev server needed.
