# Backend Folder Structure (ASP.NET Core Web API, 3-Tier Architecture)

Structure only — no business logic implemented yet. Layers: **Presentation/Controller** (`Controllers/`) → **Business Logic/Service** (`Services/`) → **Data Access/Repository** (`Repositories/`), backed by **Models/** (EF Core entities) and **DTOs/** (request/response shapes). Each layer only calls the layer directly below it — controllers never touch the database directly, and services never build HTTP responses.

Dependency direction is wired via constructor injection through ASP.NET Core's built-in DI container: controllers depend on service *interfaces* (`IOrderService`, etc.), services depend on repository *interfaces* (`IOrderRepository`, etc.). Every interface/implementation pair is registered once in `Program.cs` (e.g. `builder.Services.AddScoped<IOrderService, OrderService>();`) — there's no per-endpoint dependency declaration the way FastAPI's `Depends()` works; a class simply asks for an interface in its constructor and the container supplies the registered implementation.

Service breakdown is the same five components as `docs/architecture/c4/component.md` — **Order Service, Ledger Service, Card & Fraud Service, Menu Service, Notification Service** — no re-grouping done here.

## Layering rule and one exception

`controller → service → repository`, strictly one direction, no skipping. The one deliberate exception, already implied by `docs/architecture/c4/component.md`'s own diagram (`OrderSvc -- "order status changes" --> NotifySvc`, `MenuSvc -- "out-of-stock triggers notification" --> NotifySvc`): **other services call `INotificationService` directly**, service-to-service via constructor injection, to push SSE events — this isn't a controller→service call, it's how the "single SSE fan-out point" design (ADR-3 in `docs/architecture/arc42/arc42.md`) is implemented. `NotificationController` itself only exposes the SSE stream endpoint (`GET /api/kitchen/stream`); it doesn't originate business events.

## Why Notification Service has no `Repositories/` file

Per `component.md`'s diagram, `NotifySvc` only *reads* order/menu state to compose outgoing events (dashed arrow to the database) — it doesn't own any table. It composes events from data that `OrderRepository` / `MenuRepository` already expose, rather than querying the database on its own. Flagging this here explicitly rather than adding an empty repository class with nothing to put in it.

## Structure

```
LunchTicket.Api/
├── Program.cs                        # [bootstrap] WebApplication builder, DI registration, middleware, CORS
├── appsettings.json                  # [bootstrap] configuration (connection string, etc.)
├── Data/
│   └── AppDbContext.cs               # [bootstrap] EF Core DbContext — SQLite provider, Code-First
│
├── Models/                          # [Data Access layer] EF Core entities — mirrors lunchcard.db schema, see docs/api/erd.md
│   ├── Student.cs                    # students, accounts, cards tables
│   ├── Staff.cs                      # staff table
│   ├── Menu.cs                       # menu_items, timeslots, daily_menu tables
│   ├── Order.cs                      # orders table
│   └── Transaction.cs                # transactions table
│
├── DTOs/                            # request/response shapes, used by controllers to validate I/O
│   ├── OrderDtos.cs                  # used by OrderController
│   ├── LedgerDtos.cs                 # used by LedgerController
│   ├── CardFraudDtos.cs              # used by CardFraudController
│   ├── MenuDtos.cs                   # used by MenuController
│   └── NotificationDtos.cs           # SSE event payload shapes, used by NotificationController
│
├── Controllers/                     # [Presentation / Controller layer] — HTTP concerns only, no business rules
│   ├── OrderController.cs            # Order Service endpoints: place/cancel/confirm/walk-in/queue/mark-ready
│   ├── LedgerController.cs           # Ledger Service endpoints: balance, transaction history, top-up, report
│   ├── CardFraudController.cs        # Card & Fraud Service endpoints: login, create/lock card, manage accounts
│   ├── MenuController.cs             # Menu Service endpoints: menu items, time slots, quantities, out-of-stock
│   └── NotificationController.cs     # Notification Service endpoint: GET /api/kitchen/stream (SSE only)
│
├── Services/                        # [Business Logic layer] — one interface + implementation pair per C4 component, owns all business rules
│   ├── IOrderService.cs / OrderService.cs                 # Order Service: pre-order/walk-in lifecycle; pre-order/walk-in queue separation rule (non-negotiable)
│   ├── ILedgerService.cs / LedgerService.cs               # Ledger Service: Deduct/Refund Balance, Append Transaction Record, Enforce Balance — atomicity lives here
│   ├── ICardFraudService.cs / CardFraudService.cs         # Card & Fraud Service: card issuance/lock, login/session, suspicious-usage detection
│   ├── IMenuService.cs / MenuService.cs                   # Menu Service: menu/slot/quantity management, out-of-stock handling (triggers ledger refund + notification)
│   └── INotificationService.cs / NotificationService.cs   # Notification Service: SSE fan-out; called directly by other services (see layering exception above)
│
└── Repositories/                    # [Data Access layer] — direct EF Core queries only, no business rules
    ├── IOrderRepository.cs / OrderRepository.cs
    ├── ILedgerRepository.cs / LedgerRepository.cs
    ├── ICardRepository.cs / CardRepository.cs
    └── IMenuRepository.cs / MenuRepository.cs
```

Root namespace is `LunchTicket.Api`, with one sub-namespace per folder (`LunchTicket.Api.Controllers`, `LunchTicket.Api.Services`, `LunchTicket.Api.Repositories`, `LunchTicket.Api.Models`, `LunchTicket.Api.DTOs`).

## Cross-component call map (which service calls which, per `component.md`)

| Caller | Calls into | Why (per `docs/architecture/c4/component.md`) |
|---|---|---|
| `OrderService` | `ILedgerService` | include: Deduct Balance / Refund Balance, Enforce Balance |
| `OrderService` | `ICardFraudService` | include: verify card |
| `OrderService` | `IMenuService` | include: Available Quantity check |
| `OrderService` | `INotificationService` | order status changes pushed over SSE |
| `MenuService` | `ILedgerService` | out-of-stock triggers auto-refund |
| `MenuService` | `INotificationService` | out-of-stock triggers Receive Stock-Out Notification |
| `CardFraudService` | `ILedgerService` | extend: suspicious usage blocks Deduct Balance |

## Notes / gaps carried forward

- This is a structural target — no backend code exists yet on this branch (see `CLAUDE.md` Current State). An earlier, unrelated prototype on the `main` branch used a flat Python/FastAPI structure with business logic inside routers; that code was not reused (see `CLAUDE.md` Business Rules section for what was carried forward from it in words only). This .NET layout starts with the 3-tier separation from day one rather than migrating from a flat state.
- The business rules from `docs/investment/requirements.md` (2-hour cancellation cutoff with 100%/50% refund tiers, 3-failed-scan fraud lock with manual unlock, one-time-use QR expiry) are now confirmed and live inside `LedgerService` / `CardFraudService` / `NotificationService` respectively.
- The ERD gap this doc originally flagged is now filled — see `docs/api/erd.md` and `docs/api/schema.dbml`. `Models/` above should mirror that schema via EF Core Code-First entities and migrations.
