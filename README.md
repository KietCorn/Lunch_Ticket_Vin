# Lunch_Ticket_Vin
First Repo on On boarding day at VSF

## Documentation

Student lunch card management system — see `CLAUDE.md` for the project brief, dev commands, and non-negotiable design principles. The `docs/` tree below is the design/documentation pipeline produced so far, one artifact building on the previous one.

### 1. Investment — brainstorm, use cases, requirements (`docs/investment/`)
- [`problem-statement.md`](docs/investment/problem-statement.md) — scope, pain points (priority order), depth areas, out-of-scope
- [`usecase.md`](docs/investment/usecase.md) — use case diagram (28 use cases, 4 actors), Mermaid + traceability table
- [`requirements.md`](docs/investment/requirements.md) — one INVEST user story per use case, grouped by depth; open `[TBD]` business rules listed at the bottom
- [`README.md`](docs/investment/README.md) — folder guide, diagram images

### 2. Architecture — C4 model + Arc42 (`docs/architecture/`)
- [`c4/context.md`](docs/architecture/c4/context.md) — Level 1, system context (actors ↔ system)
- [`c4/container.md`](docs/architecture/c4/container.md) — Level 2, containers (frontends, API, database)
- [`c4/component.md`](docs/architecture/c4/component.md) — Level 3, backend components: Order, Ledger, Card & Fraud, Menu, Notification services
- [`arc42/arc42.md`](docs/architecture/arc42/arc42.md) — Arc42 sections 1–12 (goals, constraints, building blocks, ADRs, quality requirements; some sections left as tracked gaps)
- [`README.md`](docs/architecture/README.md) — reading order, known gaps

### 3. Information architecture (`docs/ia-screens.md`)
Screen hierarchies for all 4 actors (Student, Counter Staff, Kitchen, Admin), each screen cross-referenced to a use case. Structure/navigation only, no UI design.

### 4. Folder structure design (`docs/structure/`)
- [`frontend.md`](docs/structure/frontend.md) — ReactJS structure, 3 apps (Student, Staff & Kitchen, Admin) mirroring the C4 containers
- [`backend.md`](docs/structure/backend.md) — FastAPI 3-tier structure (routers → services → repositories), mirroring the 5 C4 components

### 5. API spec (`docs/openapi.yaml`)
OpenAPI 3.0 working skeleton — endpoints grouped by the same 5 backend services, reusable schemas matched against the live `lunchcard.db` schema (no formal ERD exists yet), unresolved business rules marked inline with `# TBD:` comments rather than guessed.

### Known gaps (tracked across the docs above, not hidden)
- No formal ERD document (`docs/schema.dbml` referenced in `CLAUDE.md` doesn't exist) — `lunchcard.db`'s live schema is the current stand-in.
- No sequence diagrams yet (Arc42 §6 names candidate scenarios instead).
- Admin frontend not built yet (wireframes deferred).
- Open `[TBD]` business rules: pre-order cancellation refund cutoff/%, fraud-detection thresholds, QR expiry rule.
- Two flagged use-case-diagram inconsistencies: Enforce Balance/Available Quantity not wired into Place Pre-Order; Confirm Pre-Order possibly double-charging via Deduct Balance.
