# Wireframes — Lunch Ticket System

Plain, click-through HTML wireframes — not a build, no dependencies, no framework. Open any `.html` file directly in a browser, or serve the folder locally:

```
cd docs/wireframes
python3 -m http.server 8000
# then open http://localhost:8000/student/index.html or http://localhost:8000/staff-kitchen/index.html
```

Source of truth for navigation and use-case mapping: [`docs/screens_hierarchy/ia-screens.md`](../screens_hierarchy/ia-screens.md). Source of truth for colors/spacing/typography/status labels: [`docs/design-system.md`](../design-system.md), mirrored in `shared/tokens.css`.

These are **wireframes**, not final hi-fi UI — each screen renders inside a mock device frame (phone chrome for Student, browser chrome for Staff & Kitchen) with real layout and sample content (menu cards, order lists, receipts, ledger rows), not just a title and a list of links. IA traceability (use case, "Reached from", "Leads to") is shown in a `.dev-note` box below the frame, kept separate from the mock UI itself.

## Structure

```
wireframes/
├── shared/
│   ├── tokens.css       — colors/spacing/typography, mirrors docs/design-system.md
│   └── components.css   — nav bar, card, button, status pill, empty state, TBD placeholder, alert banner
├── student/              — 10 screens, index.html for the full list
└── staff-kitchen/        — 8 screens (6 Counter Staff + 2 Kitchen), index.html for the full list
```

## Apps covered in this pass

- **Student** — [`student/index.html`](student/index.html)
- **Staff & Kitchen** — [`staff-kitchen/index.html`](staff-kitchen/index.html)
- **Admin** — not started (deferred per `CLAUDE.md`)

## Adding or updating a screen

Use the `/design-screen` slash command (`.claude/commands/design-screen.md`) — it loads the `ui-consistency` skill, looks up the screen's row in `ia-screens.md`, reuses `docs/design-system.md` tokens/components, and keeps nav links wired to the correct neighboring screens. Don't hand-add a screen file outside that workflow without following the same steps — that's how the set stays consistent and connected.

## Known simplifications

- Overlays (Stock-Out Alert, Suspicious Usage Alert) render as a shared `.alert-banner` partial on the pages that can show them — they are not separate screen files, per `ia-screens.md`'s own flags.
- Business-rule values (QR one-time-use expiry, 2-hour cancellation refund cutoff / 100%-50% tiers) are now confirmed per `docs/investment/requirements.md` and reflected directly on the QR Display, Order Detail, and Cancel Confirmation screens — no more `.tbd-placeholder` boxes for these values.
- Sample data (student names, order IDs, amounts) is illustrative only, not tied to `lunchcard.db`.
