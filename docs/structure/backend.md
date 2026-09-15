# Backend Folder Structure (FastAPI, 3-Tier Architecture)

Structure only — no business logic implemented yet. Layers: **Presentation/Controller** (`routers/`) → **Business Logic/Service** (`services/`) → **Data Access/Repository** (`repositories/`). Each layer only calls the layer directly below it — routers never touch the database directly, and services never build HTTP responses.

Service breakdown is the same five components as `docs/architecture/c4/component.md` — **Order Service, Ledger Service, Card & Fraud Service, Menu Service, Notification Service** — no re-grouping done here.

## Layering rule and one exception

`router → service → repository`, strictly one direction, no skipping. The one deliberate exception, already implied by `docs/architecture/c4/component.md`'s own diagram (`OrderSvc -- "order status changes" --> NotifySvc`, `MenuSvc -- "out-of-stock triggers notification" --> NotifySvc`): **other services call `notification_service.py` directly**, service-to-service, to push SSE events — this isn't a router→service call, it's how the "single SSE fan-out point" design (ADR-3 in `docs/architecture/arc42/arc42.md`) is implemented. `notification_router.py` itself only exposes the SSE stream endpoint (`GET /api/kitchen/stream`); it doesn't originate business events.

## Why Notification Service has no `repository/` file

Per `component.md`'s diagram, `NotifySvc` only *reads* order/menu state to compose outgoing events (dashed arrow to the database) — it doesn't own any table. It composes events from data that `order_repository.py` / `menu_repository.py` already expose, rather than querying the database on its own. Flagging this here explicitly rather than adding an empty repository file with nothing to put in it.

## Structure

```
app/
├── main.py                          # [bootstrap] FastAPI app instance, router registration, CORS, startup
├── database.py                      # [bootstrap] SQLAlchemy engine/session setup (SQLite)
├── seed.py                          # [bootstrap] demo data script (existing, per CLAUDE.md)
│
├── models/                          # [Data Access layer] SQLAlchemy ORM models — mirrors lunchcard.db schema
│   ├── student.py                    # students, accounts, cards tables
│   ├── staff.py                      # staff table
│   ├── menu.py                       # menu_items, timeslots, daily_menu tables
│   ├── order.py                      # orders table
│   └── transaction.py                # transactions table
│
├── schemas/                         # Pydantic request/response DTOs, used by routers to validate I/O
│   ├── order_schema.py               # used by order_router.py
│   ├── ledger_schema.py              # used by ledger_router.py
│   ├── card_fraud_schema.py          # used by card_fraud_router.py
│   ├── menu_schema.py                # used by menu_router.py
│   └── notification_schema.py        # SSE event payload shapes, used by notification_router.py
│
├── routers/                         # [Presentation / Controller layer] — HTTP concerns only, no business rules
│   ├── order_router.py               # Order Service endpoints: place/cancel/confirm/walk-in/queue/mark-ready
│   ├── ledger_router.py              # Ledger Service endpoints: balance, transaction history, top-up, report
│   ├── card_fraud_router.py          # Card & Fraud Service endpoints: login, create/lock card, manage accounts
│   ├── menu_router.py                # Menu Service endpoints: menu items, time slots, quantities, out-of-stock
│   └── notification_router.py        # Notification Service endpoint: GET /api/kitchen/stream (SSE only)
│
├── services/                        # [Business Logic layer] — one module per C4 component, owns all business rules
│   ├── order_service.py              # Order Service: pre-order/walk-in lifecycle; pre-order/walk-in queue separation rule (non-negotiable)
│   ├── ledger_service.py             # Ledger Service: Deduct/Refund Balance, Append Transaction Record, Enforce Balance — atomicity lives here
│   ├── card_fraud_service.py         # Card & Fraud Service: card issuance/lock, login/session, suspicious-usage detection
│   ├── menu_service.py               # Menu Service: menu/slot/quantity management, out-of-stock handling (triggers ledger refund + notification)
│   └── notification_service.py       # Notification Service: SSE fan-out; called directly by other services (see layering exception above)
│
└── repositories/                    # [Data Access layer] — direct DB queries only, no business rules
    ├── order_repository.py           # queries: orders, daily_menu
    ├── ledger_repository.py          # queries: accounts, transactions
    ├── card_repository.py            # queries: students, cards, staff
    └── menu_repository.py            # queries: menu_items, timeslots, daily_menu
```

## Cross-component call map (which service calls which, per `component.md`)

| Caller | Calls into | Why (per `docs/architecture/c4/component.md`) |
|---|---|---|
| `order_service.py` | `ledger_service.py` | include: Deduct Balance / Refund Balance, Enforce Balance |
| `order_service.py` | `card_fraud_service.py` | include: verify card |
| `order_service.py` | `menu_service.py` | include: Available Quantity check |
| `order_service.py` | `notification_service.py` | order status changes pushed over SSE |
| `menu_service.py` | `ledger_service.py` | out-of-stock triggers auto-refund |
| `menu_service.py` | `notification_service.py` | out-of-stock triggers Receive Stock-Out Notification |
| `card_fraud_service.py` | `ledger_service.py` | extend: suspicious usage blocks Deduct Balance |

## Notes / gaps carried forward

- This is a structural target, independent of the current implementation. `CLAUDE.md` describes today's `app/` as flat (`app/models.py`, `app/schemas.py`, `app/routers/*.py` with business logic inside the routers) — this doc proposes splitting that into the 3-tier layout above with an explicit service and repository layer. Migrating is a separate decision, not assumed here.
- The `[TBD]` business rules from `docs/investment/requirements.md` (cancellation refund cutoff/%, fraud-detection thresholds, QR expiry rule) live inside `ledger_service.py` / `card_fraud_service.py` / `notification_service.py` respectively once decided — no placeholder logic is implied by this structure.
- No ERD document exists yet (`docs/architecture/c4/container.md` already flags this) — `models/` above is inferred from `lunchcard.db`'s live schema, not from a `docs/schema.dbml` that doesn't exist.
