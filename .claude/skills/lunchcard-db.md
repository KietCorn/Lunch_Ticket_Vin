---
name: lunchcard-db
description: Database design guidelines for the student lunch card system. Load when working on schema design, ERD, migrations, or any entity that touches balance, orders, or cards.
---

Load `/lunchcard-context` first if the full project context isn't already in scope.

**Schema is implemented in `app/models.py` — read that file as the source of truth. This skill documents the rationale and constraints behind it.**

---

## Core Entities

| Entity | Key responsibility |
|---|---|
| `students` | Identity record — linked to the school's existing student info system |
| `cards` | Physical card tokens; a student may have multiple rows, only one `status = active` |
| `accounts` | Balance ledger, **belongs to the student, not the card** |
| `transactions` | Append-only audit log of every balance change (top-up or deduction) |
| `menu_items` | Master catalog of food items |
| `daily_menu` | Junction: which items are served on a given date, with per-timeslot slot counts |
| `timeslots` | Named pickup windows (e.g., 11:00–11:30) |
| `orders` | One order per student per timeslot per day; `type` = `pre_order` or `walk_in` |
| `order_items` | Line items within an order |

---

## Must-Have Constraints

### Financial integrity
- `accounts.balance` must have a `CHECK (balance >= 0)` constraint — never allow a negative balance.
- Every balance mutation (top-up, deduction, refund) must write a row to `transactions` **in the same database transaction**. No orphaned balance changes.
- `transactions` is append-only. No `UPDATE` or `DELETE` on committed rows.

### Card–account separation
- `cards` holds only the physical token identifier and `status` (`active` / `locked`).
- `cards` has a foreign key to `students`, not to `accounts`. Balance lookups always go through `accounts → students`, never through the card.
- Locking a card (`cards.status = locked`) must never affect `accounts.balance`.

### Slot availability — race condition risk
- `daily_menu.available_quantity` is decremented when an order is confirmed.
- Use an optimistic lock (version column) or a `SELECT ... FOR UPDATE` on the `daily_menu` row before decrementing. A simple `UPDATE ... WHERE available_quantity > 0` with row-count check is acceptable for this project's scale.
- Never let `available_quantity` go below 0 — add a `CHECK (available_quantity >= 0)` constraint as a backstop.

### Pre-order / walk-in separation
- `orders.type` must be an enum: `pre_order` | `walk_in`. Never nullable.
- No query or report should aggregate pre-orders and walk-ins into a single undifferentiated queue — always preserve the distinction.

---

## Naming Conventions

- Table names: `snake_case`, plural nouns (`orders`, `order_items`).
- Primary keys: `id` (surrogate, auto-increment or UUID — decide once and apply consistently).
- Foreign keys: `<referenced_table_singular>_id` (e.g., `student_id`, `order_id`).
- Status/type columns: string enums over integer codes — prefer readability (`active`, `locked`) over magic numbers.
- Timestamps: `created_at`, `updated_at` on every table. `transactions` gets `created_at` only (no updates allowed).

---

## Things to Watch

| Risk | Mitigation |
|---|---|
| Two students grabbing the last slot simultaneously | Optimistic lock / `FOR UPDATE` on `daily_menu` row |
| Balance going negative due to concurrent orders | Wrap balance check + deduction in a single atomic transaction |
| Card replacement losing balance | FK chain: card → student → account; balance never moves on card swap |
| Missing audit trail for disputes | `transactions` must record `actor_id` (who performed the top-up) and a `reference` field (order ID or manual note) |
| Offline fallback at the counter | Schema should support a `source` field on orders (`qr_scan` / `manual_entry`) so manual fallback is traceable |
