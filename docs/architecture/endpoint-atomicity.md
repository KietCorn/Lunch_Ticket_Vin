# Endpoint Atomicity — Ordered Step Breakdown

`docs/api/openapi.yaml` names which sub-steps each multi-step endpoint "includes" (e.g. "Includes Deduct Balance and Append Transaction Record") but not the *order* they run in or which ones can independently fail. This file fills that one gap — it does not duplicate request/response shapes, which stay in `docs/api/openapi.yaml`, or message-by-message actor/component flow, which stays in `docs/architecture/sequence-diagram-guide.md`.

Adapted from an earlier prototype's `docs/api-mapping.md` (endpoint paths and UC IDs renumbered to match this branch's `docs/investment/usecase.md`, UC1–UC28; no code reused — see `CLAUDE.md` Business Rules). Regenerate this only when an endpoint's step order actually changes — it's a snapshot, not a contract.

Each row below is one atomic unit per the Business Rules in `CLAUDE.md`: all steps succeed together or all roll back together, in the same transaction.

---

## Order Service

### `POST /orders/pre-orders` — UC6 Place Pre-Order
1. Check Available Quantity (UC28) — reject if the menu slot has none left.
2. Enforce Balance (UC26) — reject if balance would go negative.
3. Deduct Balance (UC25's counterpart) — subtract price from the student's account.
4. Decrement the menu slot's available quantity.
5. Append Transaction Record (UC25) — one new row, snapshots `balance_after`.
6. Create the order row (`order_type = pre-order`, QR token + expiry set).

Steps 2–6 roll back together if any one fails; step 1 is a precondition check before the transaction begins.

### `POST /orders/{orderId}/cancel` — UC7 Cancel Pre-Order
1. Check the cancellation cutoff — **[TBD]**: exact cutoff hours before timeslot start not finalized (`docs/investment/requirements.md`). Reject if past cutoff.
2. Refund Balance — add back the refund amount. Refund percentage post-cutoff is also **[TBD]**; pre-cutoff assumed 100%.
3. Restore the menu slot's available quantity.
4. Append Transaction Record — one new refund row.

Mirror of Place Pre-Order; steps 2–4 are atomic together.

### `POST /orders/{orderId}/confirm` — UC8 Confirm Pre-Order (QR scan)
1. Check QR expiry inline (guard, not a background job — see `CLAUDE.md` Business Rules) — reject if `now` is past `qr_expires_at`.
2. Check order isn't already confirmed/delivered — reject if so.
3. Mark the order fulfilled.

**Open question carried from `docs/api/openapi.yaml`**: the use-case diagram shows this step *possibly* including a second Deduct Balance call in addition to Place Pre-Order's own deduction (double-charge risk) — unresolved, not decided here. This endpoint's step list above assumes no second deduction.

### `POST /orders/walk-ins` — UC9 Create Walk-in Order
1. Verify card (`POST /cards/{cardToken}/verify`) — may surface a suspicious-usage flag (UC22, thresholds **[TBD]**).
2. Check Available Quantity (UC28).
3. Enforce Balance (UC26).
4. Deduct Balance.
5. Decrement the menu slot's available quantity.
6. Append Transaction Record.
7. Create the order row (`order_type = walk-in`, `placed_by_staff` set from the requesting staff session, not derived from the student).

Same atomic core as Place Pre-Order (steps 2–7); step 1 is a precondition. The resulting order always lands in the walk-in queue, structurally separate from pre-orders (non-negotiable principle #3).

---

## Ledger Service

### `POST /students/{studentId}/topup` — UC12 Top Up Student Balance
1. Add the top-up amount to the student's account balance.
2. Append Transaction Record — one new row, `actor_id` set to the staff member performing the top-up.

---

## Menu Service

### `POST /menu/daily/{dailyMenuId}/report-out-of-stock` — UC20 Report Item Out of Stock
1. Set the daily menu slot's available quantity to zero.
2. For every affected pre-order on that slot: Refund Balance (auto-refund is the confirmed resolution — not auto-substitute, not manual).
3. Append Transaction Record for each refund in step 2.
4. Trigger Receive Stock-Out Notification (UC21) over the shared SSE channel (ADR-3).

Step 2–3 repeat per affected order but each order's refund+transaction pair is atomic; step 4 fires after the DB transaction commits, not inside it.

---

## Not multi-step (single side effect, no ordering to document)

- `POST /cards/{cardToken}/verify` (UC22 extend) — read-only check, no balance/quantity mutation. Card lock/issue endpoints never touch balance, by construction (non-negotiable principle #1/#2).
- `GET` endpoints (menu, orders, balance, transactions, queue, reports) — no side effects.

---

## How to use this file

1. Before implementing an endpoint listed above, this is the step order the transaction boundary must enforce — not a suggestion.
2. If an endpoint's actual implementation needs a different order, update this file in the same change, not after.
3. `[TBD]` values referenced above (cancellation cutoff/refund %, fraud thresholds, QR expiry offset) are not invented here — see `CLAUDE.md` Known Gaps.
