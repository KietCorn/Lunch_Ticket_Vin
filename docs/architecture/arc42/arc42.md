# Arc42 Architecture Documentation — Lunch Ticket System

Source of truth: `docs/investment/problem-statement.md`, `docs/investment/usecase.md`, `docs/investment/requirements.md`, `docs/architecture/c4/*.md`.

## 1. Introduction and Goals

Digitize a manual, paper-based cafeteria process for a single canteen at a single school. Goals, in priority order (from `problem-statement.md`):

1. **Eliminate financial risk** — decouple money from the physical card, so losing a card never means losing money (highest priority).
2. **Restore priority & fairness** — students who pre-pay should get a real advantage over walk-ins, via separate, non-mergeable queues.
3. **Reduce processing-speed bottlenecks** — remove manual staff ticking at the counter.
4. **Give the kitchen real demand data** — real-time order visibility for ingredient/prep planning (secondary concern).

## 2. Architecture Constraints

- **Tech stack** (fixed, per `CLAUDE.md`): ASP.NET Core Web API (C#) + Entity Framework Core + SQLite + Server-Sent Events + ReactJS/Ant Design. No framework substitutions expected.
- **Project type**: short-term school project, not a production system — architecture and documentation depth are scoped accordingly (no HA/scaling/production-hardening concerns).
- **Scope constraint**: single canteen, single school only (see §3) — a deliberate constraint to keep the 4 pain points solved thoroughly rather than spread thin.
- **Actor set is fixed**: Student, Counter Staff, Kitchen, Admin — no new actors permitted without revisiting `problem-statement.md`'s explicit out-of-scope list.

## 3. System Scope and Context

Pulled from `docs/architecture/c4/context.md` (C4 Level 1).

**Business scope**: a single canteen at a single school (deliberate scoping decision — see `problem-statement.md` §Scope). Not multi-canteen, not multi-school.

**Actors** (unchanged from `usecase.md`, no actor beyond these four):

| Actor | Interacts with the system to... |
|---|---|
| Student | Log in; browse the daily menu; place and cancel pre-orders; show a QR code to confirm pickup; view order status, balance, and transaction history; receive stock-out notifications |
| Counter Staff | Log in; confirm pre-orders; create walk-in orders; issue and lock student cards; top up balances; handle suspicious card-usage alerts |
| Kitchen | View the live order queue (pre-order and walk-in queues kept separate); mark orders ready; report items out of stock |
| Admin | Log in; manage menu items, time slots, and quantities; manage accounts; view transaction reports |

**Technical scope**: see `docs/architecture/c4/container.md` for the containers (Student Web App, Staff & Kitchen Web App, Admin Back-office Web App, Backend API, Database) and their communication paths (REST/JSON, SSE, SQL).

**Explicitly out of scope** (from `problem-statement.md`): multi-canteen/multi-school support, actors beyond the four above, and top-level features/screens beyond the core flows already modeled.

## 4. Solution Strategy

*Placeholder — not covered by this pass; candidate for a future update once technology-level decisions beyond what's already in `CLAUDE.md` (ASP.NET Core Web API + EF Core + SQLite + SSE + ReactJS/Ant Design) need justifying.*

## 5. Building Block View

Pulled from `docs/architecture/c4/container.md` (Level 2) and `docs/architecture/c4/component.md` (Level 3).

**Level 2 — Containers**: Student Web App, Staff & Kitchen Web App, Admin Back-office Web App (not yet built), Backend API (ASP.NET Core Web API), Database (SQLite). Full diagram and rationale in `c4/container.md`.

**Level 3 — Components of the Backend API**: Order Service, Ledger Service, Card & Fraud Service, Menu Service, Notification Service. Grouping rationale and full use-case traceability in `c4/component.md`.

**Data model**: documented in `docs/architecture/erd.md` (ERD) and `docs/schema.dbml`, matching the live schema in `lunchcard.db` at the repo root.

## 6. Runtime View

A sequence-diagram method + one worked example (UC1 Login) now exists in `docs/architecture/sequence-diagram-guide.md`, with a shortlist of remaining use cases worth diagramming next (Place Pre-Order, Cancel Pre-Order, Confirm Pre-Order, Create Walk-in Order, Mark Order as Ready) — no runtime scenario is duplicated here.

## 7. Deployment View

*Placeholder — not covered by this pass. This is a school project without a defined hosting target yet (per `CLAUDE.md`, backend dev is local `dotnet run`; frontend dev tooling for the React apps is not yet decided — flagged separately, not guessed here).*

## 8. Cross-cutting Concepts

*Placeholder — not covered by this pass. Candidate topics once revisited: authentication/session handling, SSE connection lifecycle, error/validation conventions across services.*

## 9. Architecture Decisions

ADR-style entries for decisions already made, per `problem-statement.md`'s non-negotiable design principles and `requirements.md`'s use-case structure.

### ADR-1: Balance is decoupled from the physical card
- **Decision**: Student balance lives in a dedicated `accounts` table keyed by `student_id`, not on the card itself. Cards (`cards` table) only reference which account they unlock; they hold no monetary value.
- **Reason**: Pain Point 1 (financial risk) is the system's highest-priority problem — losing a card must never mean losing money. This is the foundation the Ledger Service and all four Group 1 use cases (Deduct Balance, Refund Balance, Append Transaction Record, Enforce Balance) are built on.
- **Rejected alternative**: storing a balance value on the card chip/token itself (mirrors the manual paper-card process being replaced) — rejected because it reintroduces the exact risk the project exists to remove, and provides no offsetting benefit given the settled assumption that all students have reliable smartphone/internet access.

### ADR-2: Pre-order and walk-in queues are always separate
- **Decision**: Order Service models pre-order and walk-in as distinct queues at every layer — data (`orders.order_type`), API, and UI (`View Order Queue` shows both, never merged).
- **Reason**: Pain Point 2 (priority & fairness) — students who pay in advance currently get no advantage over walk-ins. Merging the queues would silently re-introduce that unfairness even if pre-ordering exists as a feature.
- **Rejected alternative**: a single unified queue with a "priority" flag/sort order — rejected because a sort-order flag is a soft guarantee that can be bypassed or misconfigured, whereas structurally separate queues make the fairness guarantee load-bearing in the design itself, per the non-negotiable principle.

### ADR-3: One SSE channel is reused for both kitchen updates and stock-out notifications
- **Decision**: Stock-out notifications (Receive Stock-Out Notification, Group 2) are delivered over the same SSE stream already used for kitchen order-queue updates, rather than a second real-time channel.
- **Reason**: Pain Point 4 (kitchen operations) already justified one real-time channel; introducing a second (e.g. WebSockets for notifications, SSE for queue) adds operational complexity — two connections to manage, two failure modes — without a corresponding benefit for a single-canteen, short-term student project.
- **Rejected alternative**: polling from the Student Web App for stock-out status — rejected because it delays notification (defeats the "near real time" requirement in `requirements.md`) and adds load without reusing infrastructure that already exists for exactly this kind of push.

**Dependency note**: none of the three ADRs above resolve the open `[TBD]` business-rule values in `requirements.md` (cancellation cutoff hours/percentage, fraud-detection time window/attempt threshold, QR expiry rule) — those remain open and are not decided here.

## 10. Quality Requirements

| Quality attribute | Requirement | Status |
|---|---|---|
| Performance / Concurrency | System must handle **[TBD: number]** concurrent students/orders without degradation during peak lunch hours — `problem-statement.md` flags this load as "non-trivial" but doesn't fix a number | Open — needs a concrete figure before it's testable |
| Data integrity | Every balance mutation (Deduct/Refund) is atomic with its Append Transaction Record entry — no balance change without a matching audit entry, and vice versa | Defined (`requirements.md` Group 1) |
| Fairness | Pre-order and walk-in queues are never merged, at data/API/UI layers | Defined (ADR-2) |
| Financial safety | Balance can never go negative (`CHECK (balance >= 0)` at the DB layer, backed by Enforce Balance) | Defined |
| Real-time-ness | Stock-out notifications and kitchen queue updates are delivered "near real time" via SSE — no numeric latency target set | Open — no target defined yet |

## 11. Risks and Technical Debt

*Placeholder — not covered by this pass. Known gaps already surfaced elsewhere in the docs, worth carrying forward here once this section is filled in: missing ERD document (§5), missing sequence diagrams (§6), Admin Back-office frontend not yet built (`c4/container.md`), and the diagram inconsistencies flagged in `requirements.md` (Enforce Balance / Available Quantity not wired into Place Pre-Order; Confirm Pre-Order possibly double-charging via Deduct Balance).*

## 12. Glossary

*Placeholder — not covered by this pass. Candidate terms once filled in: Pre-order, Walk-in order, Ledger, SSE, QR pickup, Stock-out.*

