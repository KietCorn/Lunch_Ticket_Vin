# API Traceability — Use Case / Sequence Diagram → Real Endpoint

Swagger/OpenAPI (`http://localhost:8000/docs`) is the single source of truth for request/response contracts. This file only maps **which use case / sequence diagram step corresponds to which real endpoint**, so you can verify there's no conflict between what's designed on paper (`usecase.md`, `docs/sequence-*.md`) and what's actually running (`app/routers/*.py`).

Regenerate this by re-reading the routers whenever endpoints change — it is a snapshot, not a contract.

---

## Student use cases

| Use Case | Method & Path | Router | Notes |
|---|---|---|---|
| UC-S01 Login | `POST /api/auth/login` | `auth.py` | **Divergence:** no separate student login exists. This is the staff login endpoint, reused as the demo stand-in (see `CLAUDE.md` — "demo limitation: student auth not yet implemented"). |
| UC-S02 View Daily Menu | `GET /api/menu` | `menu.py` | Optional `?date=` query, defaults to today. |
| — (supporting) | `GET /api/menu/timeslots` | `menu.py` | Used to render timeslot labels alongside the menu. |
| UC-S03 Place Pre-Order | `POST /api/orders/preorder` | `orders.py` | Includes UC-SYS02 (balance check, `_validate_order`), UC-SYS04 (slot decrement, `_create_order_and_deduct`), UC-SYS01 (transaction insert, same helper). QR token/expiry generated inline after creation (lines 37–41). |
| UC-S04 Cancel Pre-Order | `DELETE /api/orders/{order_id}` | `orders.py` | Includes UC-SYS01 (refund transaction, `reference_order` set). Enforces the 30-min-before-timeslot deadline (`_cancel_deadline`) — this is BR-04, not explicitly in the use case's precondition text; worth back-filling into `usecase.md` UC-S04. |
| UC-S05 Show QR Code | *(no dedicated endpoint)* | — | `qr_token` / `qr_expires_at` are already returned as fields on `OrderOut` from UC-S03's response and from `GET /api/orders` — this use case is a pure frontend render step, correctly has no separate call. |
| UC-S08 View Order Status | `GET /api/orders?student_id=` | `orders.py` | `list_orders`, filterable by `student_id`, `date`, `status`. |
| UC-S09 View Balance & Transactions | `GET /api/students/{student_id}/account` + `GET /api/students/{student_id}/account/transactions` | `accounts.py` | Two calls — balance and history are separate endpoints, sequence diagram should show both. |

---

## Counter Staff use cases

| Use Case | Method & Path | Router | Notes |
|---|---|---|---|
| UC-C01 Login | `POST /api/auth/login` | `auth.py` | Same endpoint as UC-S01 — see divergence note above. |
| — (supporting) | `GET /api/students/lookup?q=` | `students.py` | "Staff searches for student" step in UC-C03/C05/C06 main flows. |
| UC-C02 Confirm Pre-Order (QR Scan) | `POST /api/orders/{order_id}/confirm` | `orders.py` | `body.source` distinguishes `qr_scan` vs `manual_entry` (UC-C02 Alt Flow 2a). Includes UC-SYS03 (QR expiry check, lines 100–102) — inline guard, not a separate endpoint. |
| UC-C03 Create Walk-In Order | `POST /api/orders/walkin` | `orders.py` | Same UC-SYS01/02/04 includes as UC-S03. `body.staff_id` sets `placed_by_staff` explicitly (not derived from session token). |
| UC-C04 Top Up Student Balance | `POST /api/students/{student_id}/account/topup` | `accounts.py` | Includes UC-SYS01. `body.staff_id` sets `transactions.actor_id`; `body.note` sets `transactions.note`. |
| UC-C05 Lock Student Card | `POST /api/students/{student_id}/cards/{card_id}/lock` | `students.py` | Confirms balance is untouched — no `Account` reference in this handler at all. |
| UC-C06 Issue New Card | `POST /api/students/{student_id}/cards` | `students.py` | Rejects with `409 ACTIVE_CARD_EXISTS` if the old card isn't locked first — enforces UC-C05 → UC-C06 "precedes" relationship from `usecase.md`'s relationship table. |
| — (supporting) | `GET /api/students/{student_id}/cards` | `students.py` | Lists card history for the card-management screen. |

---

## Kitchen use cases

| Use Case | Method & Path | Router | Notes |
|---|---|---|---|
| UC-K01 View Live Order Queue | `GET /api/kitchen/stream` (SSE) | `kitchen.py` | Pushes the active order list every 2s (`PUSH_INTERVAL_SECONDS`). No auth dependency — the URL itself is the access control (documented in the router's docstring). |
| — (supporting) | `GET /api/kitchen/orders` | `kitchen.py` | Snapshot endpoint for initial page load before the SSE connection opens. |
| UC-K02 Mark Order as Ready | `POST /api/orders/{order_id}/ready` | `orders.py` | **Note:** lives in `orders.py`, not `kitchen.py` — if your sequence diagram assumed a `kitchen.py` endpoint, correct it here. |

---

## Admin use cases — ⚠ backend gap

| Use Case | Method & Path | Router | Notes |
|---|---|---|---|
| UC-A01 Login | `POST /api/auth/login` | `auth.py` | Same endpoint; distinguished by `staff.is_admin` flag (not checked anywhere in `auth.py` itself — no route currently rejects a non-admin from hitting admin-only actions, because none of those actions exist yet). |
| UC-A02 Manage Menu Items | *(not implemented)* | — | `menu.py` only has `GET /api/menu`. No `POST/PUT/DELETE` for `menu_items`. |
| UC-A03 Manage Timeslots | *(not implemented)* | — | `menu.py` only has `GET /api/menu/timeslots`. No create/edit. |
| UC-A04 Set Daily Menu & Quantities | *(not implemented)* | — | No `POST` for `daily_menu` anywhere; only seeded via `app/seed.py`. |
| UC-A05 Manage Staff Accounts | *(not implemented)* | — | No staff CRUD router exists. |
| UC-A06 Manage Student Accounts | *(not implemented)* | — | Only `GET /api/students/lookup` exists; no student creation/edit endpoint. |
| UC-A07 View Transaction Reports | *(not implemented)* | — | Only per-student `GET /api/students/{id}/account/transactions` exists — no cross-student filter by date range/type as UC-A07 describes. |

This matches `CLAUDE.md`'s status table (`Wireframes — admin: Deferred`), but is worth stating explicitly here: **the backend has zero admin endpoints**, not just missing UI. If UC-A02–UC-A07 sequence diagrams are drawn, their `API` lifeline arrows currently point at endpoints that don't exist — flag them as "design, not yet implemented" rather than tracing them to real code.

---

## System use cases (UC-SYS01–04) — where they live in code

| Use Case | Implementation | Where |
|---|---|---|
| UC-SYS01 Append Transaction Record | Inline `Transaction(...)` + `db.add()` | `orders.py` (`_create_order_and_deduct`, `cancel_order`), `accounts.py` (`top_up`) — not a shared helper, duplicated 3x |
| UC-SYS02 Enforce Balance ≥ 0 | `if int(account.balance) < price: raise 409` | `orders.py::_validate_order` |
| UC-SYS03 Expire QR Token | `if ... datetime.utcnow() > order.qr_expires_at: raise 409` | `orders.py::confirm_delivery` |
| UC-SYS04 Decrement Available Quantity | `daily_menu.available_quantity -= 1` | `orders.py::_create_order_and_deduct` (no `SELECT ... FOR UPDATE` — SQLite's single-writer lock is relied on instead; fine for this project's scale, not something to flag as a bug) |

---

## How to use this file

1. When you draw a sequence diagram, every `App->>API` arrow should have a row in this table it can point to.
2. If it doesn't — either the endpoint doesn't exist yet (see Admin gap above) or your sequence diagram invented a call that isn't real. Fix whichever one is wrong.
3. Re-check this file after any router change; it is a snapshot of `app/routers/*.py` as of this write-up, not a live contract.
