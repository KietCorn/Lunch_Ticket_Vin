# Architecture Documentation — Lunch Ticket System

C4 model and Arc42 documentation for the system, derived from `docs/investment/usecase.md` and `docs/investment/requirements.md`. This is a fresh set of documents — there was no prior C4/Arc42 material anywhere in the repo before this folder.

## Structure

```
architecture/
├── c4/
│   ├── context.md     Level 1 — System Context (actors ↔ system, no tech names)
│   ├── container.md   Level 2 — Containers (frontends, Backend API, Database, tech choices)
│   └── component.md   Level 3 — Components inside the Backend API
└── arc42/
    └── arc42.md        Arc42 sections 1–12
```

## Reading order

1. **`c4/context.md`** — start here for the big picture: who (Student, Counter Staff, Kitchen, Admin) talks to the system and why.
2. **`c4/container.md`** — zooms in one level: the actual frontends, the FastAPI backend, SQLite, and how the ledger design shows up in the live DB schema (`lunchcard.db`, since no ERD doc exists yet).
3. **`c4/component.md`** — zooms in again, inside the Backend API only: Order Service, Ledger Service, Card & Fraud Service, Menu Service, Notification Service, with the reasoning for each grouping and a full use-case traceability table.
4. **`arc42/arc42.md`** — the full narrative document. Sections 3, 5, and 6 point back to the C4 diagrams above rather than duplicating them; section 9 records the three architecture decisions already made (ledger decoupled from card, queues never merged, one SSE channel reused); sections 1, 2, and 10 are filled in from direct answers on goals/constraints/quality; sections 4, 7, 8, 11, and 12 are left as placeholders — not yet in scope.

## Known gaps (tracked, not hidden)

- No ERD document exists (`CLAUDE.md` references `docs/schema.dbml`, which isn't present) — `lunchcard.db`'s live schema is the current stand-in.
- No sequence diagrams exist yet — Arc42 §6 (Runtime View) names candidate scenarios to diagram next instead of fabricating one.
- The Admin Back-office frontend isn't built yet (wireframes deferred per `CLAUDE.md`), though its use cases and container are modeled here.
- Three `[TBD]` business-rule values from `docs/investment/requirements.md` (cancellation refund cutoff/%, fraud-detection window/threshold, QR expiry rule) and one peak-concurrency number (Arc42 §10) remain unresolved — none are invented here.
- Two possible use-case-diagram inconsistencies flagged in `requirements.md` (Enforce Balance / Available Quantity not wired into Place Pre-Order; Confirm Pre-Order possibly double-charging via Deduct Balance) are still open.
