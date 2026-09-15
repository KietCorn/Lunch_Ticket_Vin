# Problem Statement

## Scope

The system applies to a single canteen at a single school (not multi-canteen, not multi-school). This is a deliberate scoping decision to ensure the 4 pain points below are solved thoroughly rather than spreading effort across a broader, shallower solution. Note that even at a single canteen, expected concurrent user load during peak lunch hours is non-trivial.

## Pain Points (in priority order)

1. Financial risk — balance is tied to the physical card, so losing the card means losing the money on it (highest priority)
2. Priority & fairness — students who pre-pay get no advantage over walk-in students; everyone should queue the same way
3. Processing speed — manual staff ticking causes bottlenecks during peak hours
4. Kitchen operations — lack of real demand data makes ingredient planning difficult (secondary concern, not the main focus)

## Depth Areas

These are business rules that add depth to the existing core flows (Place Pre-order, Process Payment, Kitchen order handling) WITHOUT introducing new actors, screens, or modules. The goal is analytical depth, not feature sprawl — this keeps the system testable and debuggable while still being substantial enough for a mentor review that requires more than a simple app.

### 1. Refund & cancellation limits (deepens: Place Pre-order)

- Daily limit on how many pre-orders one student can place
- Cancellation policy tied to a time cutoff before the meal — earlier cancellation gets a higher refund percentage, late cancellation gets partial or no refund
- This should surface as: a branching flow in the sequence diagram (early cancel vs late cancel), and new fields in the existing orders table (e.g. cancellation_deadline, refund_percent) rather than new tables

### 2. Stock-out exception handling (deepens: Kitchen order handling + Place Pre-order)

- What happens when a menu item runs out mid-service while pre-orders for that item already exist
- Needs a notification flow between Kitchen and affected Students, reusing the existing SSE channel (no new channel)
- Needs a business decision on the resolution path: auto-suggest a substitute item, auto-refund, or flag for staff to resolve manually — open decision, not yet made

### 3. Fairness & anti-fraud rules (deepens: Process Payment)

- Limit on failed card-scan attempts
- Detection logic for the same card being used in two places at the same time (a fraud signal)
- This should produce concrete, testable acceptance criteria — this area is meant to justify meaningful backend validation logic and unit tests later

## Explicitly Out of Scope

- Multi-canteen or multi-school support
- New actors beyond the existing Student, Counter Staff, Kitchen, Admin
- New top-level features/screens beyond the existing core flows
