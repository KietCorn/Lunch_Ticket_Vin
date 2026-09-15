# Frontend Folder Structure (ReactJS)

Structure only — no components, pages, or logic implemented yet. Derived from `docs/ia-screens.md` (screens/flows) and `docs/architecture/c4/container.md` (the 3 frontend containers) and `docs/architecture/c4/component.md` (backend component names, mirrored here for `services/`).

## Why three app folders, not one

`docs/architecture/c4/container.md` defines three separate frontend containers — **Student Web App**, **Staff & Kitchen Web App**, **Admin Back-office Web App** — because they serve different actors with different deployment/access needs (per `CLAUDE.md`, `frontend/student/` already exists as its own app today). This structure keeps that split rather than merging everything into one app with role-based routing, so it matches the container diagram exactly.

A small `shared/` folder holds the few things genuinely duplicated across all three (API client config, formatters) — kept minimal since this is a student project, not a monorepo with build tooling to justify a shared package.

## `services/` naming — mirrors `docs/architecture/c4/component.md`

Each app's `services/` folder has at most one file per **backend component** it actually talks to (not one per use case) — `orderService`, `ledgerService`, `cardFraudService`, `menuService`, `notificationService`. An app only gets the service files for components its screens actually call (e.g. Admin never touches Order Service, since no Admin use case does).

`notificationService.js` is used by both the Student app (QR/stock-out alerts) and the Staff & Kitchen app (live order queue). This follows `docs/architecture/c4/component.md`'s own reasoning: Notification Service is the single SSE fan-out point that other components push through, per ADR-3 in `docs/architecture/arc42/arc42.md` ("one SSE channel is reused for both kitchen updates and stock-out notifications") — so the Kitchen queue view subscribes through `notificationService`, not a separate ad hoc SSE client.

## Structure

```
frontend/
├── shared/                          # code duplicated across all 3 apps — kept minimal on purpose
│   ├── services/
│   │   └── apiClient.js             # shared fetch/axios instance: base URL, auth header injection
│   └── utils/
│       ├── formatCurrency.js        # shared money formatting (used everywhere balances/prices show)
│       └── formatDate.js            # shared date/time formatting (timeslots, transaction timestamps)
│
├── student/                         # Student Web App container (docs/architecture/c4/container.md)
│   └── src/
│       ├── assets/                  # images/icons specific to the student app
│       ├── components/
│       │   └── common/              # reusable pieces local to this app (MenuItemCard, QRCode, StatusBadge)
│       ├── pages/                   # one file per screen — mirrors docs/ia-screens.md "Student" section
│       │   ├── LoginPage.jsx                # UC1 Login
│       │   ├── HomeMenuPage.jsx              # UC2 View Daily Menu
│       │   ├── PreOrderReviewPage.jsx        # UC6 Place Pre-Order (review step)
│       │   ├── OrderPlacedPage.jsx           # UC6 Place Pre-Order (result)
│       │   ├── MyOrdersPage.jsx              # UC3 View Order Status (list)
│       │   ├── OrderDetailPage.jsx           # UC3 View Order Status (detail) — hub for QR/Cancel
│       │   ├── QRDisplayPage.jsx             # UC5 Show QR
│       │   ├── CancelOrderPage.jsx           # UC7 Cancel Pre-Order (confirm + refund preview)
│       │   ├── CancellationResultPage.jsx    # UC7 Cancel Pre-Order (result)
│       │   └── BalancePage.jsx               # UC4 View Balance & Transaction
│       ├── services/                # one file per backend component this app actually calls
│       │   ├── orderService.js       # Order Service — place/cancel/confirm, order status
│       │   ├── ledgerService.js      # Ledger Service — balance, transaction history
│       │   ├── cardFraudService.js   # Card & Fraud Service — login/session
│       │   ├── menuService.js        # Menu Service — daily menu, availability
│       │   └── notificationService.js# Notification Service — SSE: stock-out alerts, QR expiry
│       ├── hooks/
│       │   ├── useAuth.js            # current student/session, from cardFraudService
│       │   └── useSSE.js             # generic SSE subscription hook (used by notificationService consumers)
│       ├── context/
│       │   ├── AuthContext.jsx       # logged-in student, route guarding
│       │   └── NotificationContext.jsx # in-app stock-out alert state (overlay, per ia-screens.md)
│       ├── routes/
│       │   └── AppRouter.jsx         # Student screen navigation tree
│       ├── App.jsx
│       └── main.jsx                  # entry point
│
├── staff-kitchen/                   # Staff & Kitchen Web App container — one app, two screen groups
│   └── src/
│       ├── components/
│       │   ├── staff/                # StudentSearchBar, CardScanner, etc.
│       │   └── kitchen/              # QueueColumn, OrderTicket, etc.
│       ├── pages/                   # mirrors docs/ia-screens.md "Counter Staff" + "Kitchen" sections
│       │   ├── staff/
│       │   │   ├── LoginPage.jsx              # UC1 Login
│       │   │   ├── POSHomePage.jsx             # nav hub (see ia-screens.md flag — not use-case-backed)
│       │   │   ├── ConfirmPreOrderPage.jsx     # UC8 Confirm Pre-Order
│       │   │   ├── CreateWalkinOrderPage.jsx   # UC9 Create Walk-in Order
│       │   │   ├── StudentLookupPage.jsx       # supports UC10/UC11/UC12
│       │   │   └── StudentAccountPage.jsx      # UC10 Create Card, UC11 Lock Card, UC12 Top Up
│       │   └── kitchen/
│       │       ├── OrderQueuePage.jsx          # UC18 View Order Queue, UC19 Mark Ready (inline)
│       │       └── ReportOutOfStockPage.jsx    # UC20 Report Item Out of Stock
│       ├── services/
│       │   ├── orderService.js       # Order Service — confirm/walk-in/queue/mark-ready
│       │   ├── ledgerService.js      # Ledger Service — top-up balance
│       │   ├── cardFraudService.js   # Card & Fraud Service — login, cards, suspicious-usage alerts
│       │   ├── menuService.js        # Menu Service — report item out of stock
│       │   └── notificationService.js# Notification Service — SSE: live order queue (see note above)
│       ├── hooks/
│       │   ├── useAuth.js
│       │   └── useSSE.js
│       ├── context/
│       │   ├── AuthContext.jsx
│       │   └── QueueContext.jsx      # live queue state for Kitchen view, fed by useSSE
│       ├── routes/
│       │   └── AppRouter.jsx         # role-aware routing: Staff POS tree vs Kitchen display tree
│       ├── App.jsx
│       └── main.jsx
│
└── admin/                            # Admin Back-office Web App container (not yet built — CLAUDE.md: deferred)
    └── src/
        ├── pages/                    # mirrors docs/ia-screens.md "Admin" section
        │   ├── LoginPage.jsx                  # UC1 Login
        │   ├── DashboardPage.jsx               # nav hub (see ia-screens.md flag — not use-case-backed)
        │   ├── MenuItemsPage.jsx               # UC13 Manage Menu Item
        │   ├── TimeSlotsPage.jsx               # UC14 Manage Menu Time Slots
        │   ├── QuantitiesPage.jsx              # UC15 Set Menu & Quantities
        │   ├── AccountsPage.jsx                # UC16 Manage Accounts
        │   └── ReportsPage.jsx                 # UC17 View Transaction Report
        ├── services/
        │   ├── menuService.js        # Menu Service — items, time slots, quantities
        │   ├── cardFraudService.js   # Card & Fraud Service — login, account management
        │   └── ledgerService.js      # Ledger Service — transaction report
        ├── hooks/
        │   └── useAuth.js
        ├── context/
        │   └── AuthContext.jsx
        ├── routes/
        │   └── AppRouter.jsx
        ├── App.jsx
        └── main.jsx
```

## Notes / gaps carried forward

- No `orderService.js` or `notificationService.js` in `admin/` — no Admin use case in `docs/investment/usecase.md` touches Order Service or needs a live SSE subscription. Not an oversight.
- `POSHomePage.jsx` and `DashboardPage.jsx` are navigational hubs flagged in `docs/ia-screens.md` as not backed by a single use case — kept as pages since they're still real, necessary screens, just noting the same caveat here.
- This is a structural target, independent of the current Vanilla JS implementation (`frontend/student/index.html` + `app.js`) — migrating to this structure is a separate decision, not assumed here.
