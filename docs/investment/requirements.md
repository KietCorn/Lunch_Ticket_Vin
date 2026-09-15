# User Stories — Lunch Ticket System

Source of truth: `docs/usecase.md` (28 use cases across 4 actors). One story per use case, grouped by depth of treatment. `[TBD: ...]` marks business-rule numbers not yet decided elsewhere in the project — do not treat these as final until confirmed.

Resolved during this pass: stock-out resolution path is **auto-refund** (confirmed by product owner) — see Report Item Out of Stock.

---

## Group 1 — Financial Ledger

These four use cases are system-level, reachable only via `<<include>>` from other use cases (Place Pre-Order, Cancel Pre-Order, Confirm Pre-Order, Create Walk-in Order, Top Up Student Balance). Each story is framed from the perspective of the including use case's actor, not "the system."

### Deduct Balance
**Story**: As a Student placing or confirming a pre-order (or a Counter Staff member creating a walk-in order on my behalf), I want my balance to be reduced by exactly the order amount at the moment of payment, so that the order is paid for using my decoupled account balance rather than the physical card.

**Acceptance Criteria**:
- Balance is decremented by exactly the order total; no partial deduction occurs.
- Deduction only proceeds if Enforce Balance confirms sufficient funds; if insufficient, the deduction fails and balance is left unchanged.
- Deduction and the corresponding Append Transaction Record entry succeed or fail together as a single atomic operation — a deduction is never recorded without a matching transaction entry, or vice versa.
- On any failure mid-transaction, balance is rolled back to its pre-deduction value and the triggering order is not marked paid.
- Concurrent deductions against the same student's balance are serialized so balance can never go negative.

**INVEST check**: Small and testable, but not Independent — it has no story of its own outside the use cases that include it. Treat it as a shared acceptance-criteria contract that Place Pre-Order, Confirm Pre-Order, and Create Walk-in Order all depend on.

### Refund Balance
**Story**: As a Student whose pre-order is cancelled (or otherwise refunded, e.g. via a stock-out auto-refund), I want the refunded amount credited back to my balance, so that I don't lose money for a cancellation or resolution I'm entitled to.

**Acceptance Criteria**:
- Refunded amount is calculated per the triggering policy (cancellation refund policy, or full refund for stock-out) and credited to the student's balance.
- Refund and the corresponding Append Transaction Record entry are atomic — both succeed or both fail.
- Refund amount never exceeds the original amount paid for the order being refunded.
- A refund cannot be applied twice against the same order (idempotent per order/refund event).

**INVEST check**: Same dependency issue as Deduct Balance — only reachable via Cancel Pre-Order and Report Item Out of Stock; not independently valuable or shippable on its own.

### Append Transaction Record
**Story**: As a Student or Counter Staff member performing any money-moving action (pre-order payment, cancellation refund, walk-in payment, balance top-up), I want every balance change recorded in an immutable transaction log, so that there is a full, auditable history of account activity.

**Acceptance Criteria**:
- Every record captures at minimum: amount, direction (debit/credit), timestamp, and the related order (or top-up reference) that caused it.
- Records are immutable once created — no update or delete path.
- Record creation is atomic with the balance change it documents (see Deduct Balance / Refund Balance) — never one without the other.
- A student's View Balance & Transaction screen reflects every record, in chronological order, without omission.

**INVEST check**: Valuable and testable (it's the audit trail), but not independently shippable — depends on Deduct Balance, Refund Balance, and Top Up Student Balance all calling it correctly. Best treated as a cross-cutting contract rather than a standalone feature.

### Enforce Balance
**Story**: As a Student or Counter Staff member attempting to pay for an order, I want the transaction rejected up front if the balance is insufficient, so that a student's balance can never go negative.

**Acceptance Criteria**:
- If balance < order total, the triggering use case fails before any Deduct Balance or Append Transaction Record calls occur.
- The actor sees a clear "insufficient balance" error, distinct from other failure reasons (e.g. item out of stock).
- The balance check and the subsequent deduction are effectively atomic — no time-of-check-to-time-of-use gap that lets a race condition push balance negative under concurrent orders.

**INVEST check**: Small and testable. Flagging a diagram inconsistency worth resolving: per `docs/usecase.md`, only Create Walk-in Order includes Enforce Balance — Place Pre-Order and Confirm Pre-Order don't. Confirm whether that's intentional (e.g. pre-orders are checked differently) or a gap in the diagram.

---

## Group 2 — Depth-Area Use Cases

### Cancel Pre-Order
**Story**: As a Student, I want to cancel a pre-order I've placed, so that I can get some or all of my money back if my plans change before the meal.

**Acceptance Criteria**:
- Cancellation is only allowed while the order is in a cancellable state (not yet confirmed/picked up).
- Refund percentage depends on how far before the scheduled meal the cancellation occurs: full refund if cancelled more than **[TBD: X hours]** before the meal; **[TBD: Y%]** refund if cancelled after that cutoff.
- The computed refund amount is passed to Refund Balance, which credits the balance and creates a matching Append Transaction Record entry atomically.
- Order status updates to "cancelled" and disappears from Kitchen's View Order Queue.
- Student sees a confirmation showing the refund amount and percentage applied.

**INVEST check**: Not yet Estimable — blocked on the **[TBD: X, Y]** cutoff and percentage. Cannot be considered ready for implementation until those are confirmed.

### Report Item Out of Stock
**Story**: As Kitchen staff, I want to mark a menu item as out of stock mid-service, so that no further orders are accepted for it and students who already pre-ordered it are handled fairly.

**Acceptance Criteria**:
- Marking an item out of stock sets its Available Quantity to zero and removes it from the orderable menu immediately.
- All existing pre-orders containing that item are automatically refunded in full via Refund Balance (resolution path confirmed as **auto-refund**, not auto-substitute or manual staff handling).
- Every affected student receives a Receive Stock-Out Notification identifying the item and confirming the refund.
- The out-of-stock action is logged (who, when) for Kitchen's own record-keeping.

**INVEST check**: Depends on the Group 1 Refund Balance contract for the actual money movement — this story should trigger that contract, not re-specify refund mechanics.

### Receive Stock-Out Notification
**Story**: As a Student with a pre-order affected by a stock-out, I want to be notified in near real time, so that I know my order changed and don't show up expecting an item that won't be served.

**Acceptance Criteria**:
- Notification is delivered via the existing real-time channel (conceptually the same SSE mechanism already used for kitchen order status) — no new delivery channel.
- Notification identifies the affected order, the out-of-stock item, and the refund confirmation/amount.
- Only students who actually had a pre-order for the affected item are notified — not broadcast to all students.

**INVEST check**: Small and testable, but tightly coupled to Report Item Out of Stock (only reachable via its `<<include>>`) — not independently valuable without it.

### Detect Suspicious Card Usage
**Story**: As a Counter Staff member, I want to be alerted when a student's card is used in a way that looks fraudulent, so that I can stop the transaction before money moves incorrectly.

**Acceptance Criteria**:
- Flag as suspicious when the same card is used in two payment attempts within **[TBD: time window]** at different terminals/counters.
- After **[TBD: N]** consecutive failed card-scan attempts on the same card, temporarily lock that card from further scan attempts.
- When flagged, the in-progress Deduct Balance call is blocked — payment does not complete while the flag is active.
- Counter Staff sees a clear on-screen alert distinguishing "suspicious usage" from an ordinary declined/insufficient-balance transaction.

**INVEST check**: Not yet Estimable — blocked on both **[TBD]** thresholds. Also flagging that the "temporary lock" duration and unlock process aren't defined yet either — worth deciding alongside the thresholds.

### Available Quantity
**Story**: As Counter Staff or Kitchen staff, when an order is being created or an item is reported out of stock, I want the system to check and update a menu item's remaining quantity, so that the kitchen never oversells an item beyond what's actually available.

**Acceptance Criteria**:
- Quantity is checked before Create Walk-in Order completes; if requested quantity exceeds available quantity, the order is rejected with a clear "not enough stock" error, distinct from insufficient balance.
- Reporting an item out of stock immediately sets its available quantity to zero, blocking further orders.
- Quantity decrements are atomic with order creation to avoid overselling under concurrent walk-in orders.

**INVEST check**: Same shared-contract caveat as the Group 1 use cases — only reachable via include, not independently valuable. Also flagging: per the diagram, it's wired only into Create Walk-in Order and Report Item Out of Stock, not Place Pre-Order — worth confirming whether pre-orders should also check/reserve quantity against the same pool.

---

## Group 3 — QR Pickup Flow

### Show QR
**Story**: As a Student, I want to display a QR code for my order, so that Counter Staff can scan it to confirm pickup.

**Acceptance Criteria**:
- QR encodes enough information to uniquely identify the order and student for the pickup-confirmation flow.
- QR becomes invalid once Expire QR's conditions are met — the student cannot reuse an expired QR.
- If an expired QR is scanned, Staff sees a clear "expired, please regenerate" message rather than a silent confirm.

**INVEST check**: Small and testable, but depends entirely on Expire QR's rules being defined — currently blocked on Expire QR's `[TBD]`.

### Expire QR
**Story**: As a Student, I want my order's QR code to stop being valid once it's no longer needed, so that it can't be reused or scanned by someone else after pickup.

**Acceptance Criteria**:
- QR expires under: **[TBD: time-based expiry duration from generation, and/or one-time-use — invalidated immediately after first successful scan]**.
- An expired QR cannot be used to confirm pickup even if presented again.
- Student can regenerate/re-display a fresh QR after expiry, provided the underlying order is still valid.

**INVEST check**: Not yet Estimable — need to confirm whether expiry is time-based, one-time-use, or both, before this can be implemented or tested.

---

## Group 4 — Everything Else

### Login
**Story**: As a Student, Counter Staff member, or Admin, I want to log in with my credentials, so that I can access the features appropriate to my role.

**Acceptance Criteria**:
- Successful login routes to the correct role-specific home screen (Student app, Staff POS, Admin back-office).
- Invalid credentials show a generic error without revealing whether the username or password was wrong.
- A logged-in session carries the actor's role for all subsequent authorization checks.

**INVEST check**: Per `CLAUDE.md`, student auth isn't implemented yet in the current demo (staff-only login exists) — this story is aspirational for the Student actor until that's built.

### View Daily Menu
**Story**: As a Student, I want to view today's available menu items and time slots, so that I can decide what to pre-order or expect as a walk-in.

**Acceptance Criteria**:
- Only items with Available Quantity > 0 for the selected time slot are shown as orderable.
- Items marked out of stock are visibly flagged, not silently hidden.
- Menu reflects Admin's current Manage Menu Item / Set Menu & Quantities configuration.

**INVEST check**: No concerns.

### View Order Status
**Story**: As a Student, I want to see the current status of my order (placed, confirmed, ready, picked up, cancelled), so that I know when to head to the counter.

**Acceptance Criteria**:
- Status reflects the latest state from Confirm Pre-Order / Mark Order as Ready.
- Cancelled/refunded orders show their final resolved state.

**INVEST check**: No concerns.

### View Balance & Transaction
**Story**: As a Student, I want to view my current balance and transaction history, so that I can track my spending and verify refunds/top-ups.

**Acceptance Criteria**:
- Displays current balance and a chronological transaction list sourced from Append Transaction Record.
- Each entry shows amount, direction, timestamp, and related order.

**INVEST check**: Depends on Append Transaction Record (Group 1) being correct — not independently testable without it.

### Place Pre-Order
**Story**: As a Student, I want to place a pre-order for a menu item and time slot, so that I get priority queue treatment instead of waiting in the walk-in line.

**Acceptance Criteria**:
- Order can only be placed for items with sufficient Available Quantity and within the slot's ordering window.
- Placing the order triggers Deduct Balance and Append Transaction Record atomically — order is not created if payment fails.
- Student receives confirmation with order details and a reference to the cancellation deadline.

**INVEST check**: Money mechanics intentionally not re-specified here — see Deduct Balance / Append Transaction Record (Group 1) for that contract.

### Confirm Pre-Order
**Story**: As a Counter Staff member, I want to confirm a student's pre-order at pickup (e.g. via QR scan), so that the order is marked fulfilled and removed from the pending queue.

**Acceptance Criteria**:
- Confirmation requires a valid, non-expired QR (or equivalent identifier).
- Confirming updates order status to fulfilled.

**INVEST check**: Flagging a likely diagram inconsistency: `docs/usecase.md` shows Confirm Pre-Order also including Deduct Balance, which would double-charge on top of Place Pre-Order's own deduction. Recommend confirming with you whether Confirm Pre-Order should actually trigger payment, or whether that include arrow is a leftover from the old draft that should be removed.

### Create Walk-in Order
**Story**: As a Counter Staff member, I want to create an order on the spot for a walk-in student, so that students without a pre-order can still be served.

**Acceptance Criteria**:
- Requires an Available Quantity check and an Enforce Balance check before the order is created.
- Payment is deducted and recorded atomically as part of order creation.
- Walk-in orders go into a queue separate from pre-orders (non-negotiable design principle — queues are never merged).

**INVEST check**: This story bundles order creation, stock check, and payment into one action. Flagging as a candidate to split into "Create Walk-in Order" plus a reused payment story if implementation complexity grows — keeping as one for now since the diagram models it as a single use case.

### Create Student Card
**Story**: As a Counter Staff member, I want to issue a new physical card linked to a student's account, so that a student who lost their card (or is new) can resume using the system without losing their balance.

**Acceptance Criteria**:
- New card links to the student's existing account and balance — balance is never reset.
- If replacing a lost card, the old card must be locked as part of the same workflow.

**INVEST check**: Tightly coupled to Lock Student Card for the lost-card scenario — consider whether these should be a single combined story for that specific flow.

### Lock Student Card
**Story**: As a Counter Staff member, I want to lock a student's card, so that a lost or compromised card can no longer be used for payment.

**Acceptance Criteria**:
- A locked card is rejected at scan with a clear "card locked" message, not a generic error.
- Locking a card does not affect the student's balance.

**INVEST check**: No concerns.

### Top Up Student Balance
**Story**: As a Counter Staff member, I want to add funds to a student's balance (e.g. cash top-up at the counter), so that the student can pay for orders using their account.

**Acceptance Criteria**:
- Top-up amount is credited to balance and recorded via Append Transaction Record atomically.
- Staff sees confirmation of the new balance after top-up.

**INVEST check**: Depends on Append Transaction Record (Group 1).

### Manage Menu Item
**Story**: As an Admin, I want to create, edit, or remove menu items, so that the menu Kitchen prepares and Students see stays accurate.

**Acceptance Criteria**:
- Changes are reflected in View Daily Menu without a system restart.
- Removing/disabling an item does not delete historical order records referencing it.

**INVEST check**: No concerns.

### Manage Menu Time Slots
**Story**: As an Admin, I want to configure the available pickup/meal time slots, so that pre-orders and walk-ins are organized into defined serving windows.

**Acceptance Criteria**:
- Time slots have a start/end time and are selectable when placing pre-orders.
- Overlapping or invalid slot configurations are rejected.

**INVEST check**: No concerns.

### Set Menu & Quantities
**Story**: As an Admin, I want to assign quantities to menu items for a given day/slot, so that Available Quantity has a starting number to count down from.

**Acceptance Criteria**:
- Setting a quantity initializes Available Quantity for that item/slot.
- Quantities can be adjusted intraday and changes take effect immediately for new orders.

**INVEST check**: Directly feeds the Available Quantity contract (Group 2) — dependency worth noting when sequencing implementation.

### Manage Accounts
**Story**: As an Admin, I want to create, edit, or deactivate Student/Staff accounts, so that account access stays current (e.g. graduated students, new staff).

**Acceptance Criteria**:
- Deactivating an account preserves its historical transaction records.
- Account changes take effect on next login (or immediately, if sessions are invalidated).

**INVEST check**: No concerns.

### View Transaction Report
**Story**: As an Admin, I want to view an aggregated report of transactions over a period, so that I can reconcile balances and monitor the system's financial health.

**Acceptance Criteria**:
- Report is built from Append Transaction Record entries and is read-only.
- Report can be filtered by date range at minimum.

**INVEST check**: Depends entirely on Append Transaction Record data quality (Group 1).

### View Order Queue
**Story**: As Kitchen staff, I want to see incoming orders in real time, split into pre-order and walk-in queues, so that I can prepare food with visibility into actual demand.

**Acceptance Criteria**:
- Pre-order and walk-in queues are always displayed separately, never merged (non-negotiable design principle).
- Queue updates in near real time as new orders arrive (reuses the existing SSE channel).

**INVEST check**: No concerns.

### Mark Order as Ready
**Story**: As Kitchen staff, I want to mark an order as ready for pickup, so that Counter Staff and the Student know it can be collected.

**Acceptance Criteria**:
- Marking ready updates order status visible on View Order Status (Student) and the counter.
- Only orders in an appropriate prior state (e.g. "in preparation") can be marked ready.

**INVEST check**: No concerns.

---

## Open TBDs to confirm

- Cancel Pre-Order refund cutoff hours and post-cutoff refund percentage.
- Detect Suspicious Card Usage: time window for duplicate-terminal detection; consecutive failed-scan threshold before lock; lock duration/unlock process.
- Expire QR: exact expiry rule (time-based duration, one-time-use after scan, or both).

## Open diagram questions to confirm

- Enforce Balance is only wired into Create Walk-in Order in the diagram — should Place Pre-Order and Confirm Pre-Order also include it?
- Confirm Pre-Order includes Deduct Balance alongside Place Pre-Order already including it — is a second deduction at confirmation intentional, or should that include arrow be removed?
- Available Quantity is only wired into Create Walk-in Order and Report Item Out of Stock — should Place Pre-Order also check/reserve against it?
