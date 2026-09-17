# LunchTicket.Api

ASP.NET Core Web API (.NET 8), 3-tier (Controllers → Services → Repositories → Models/DTOs), EF Core Code-First on SQLite. Scaffolded to match `docs/structure/backend.md` and the business rules in `../CLAUDE.md`. This file exists so you can catch up on what was actually built, file by file — the design docs describe the *target*, this describes what's *here now*.

## Status: scaffold + working core flows, not production-ready

Everything below compiles, the DB migration applies, and the atomic order/ledger flows have been smoke-tested end-to-end (place pre-order → balance deducted + quantity decremented + one transaction row written; cancel → correct 100%/50% refund tier applied + quantity restored). What's *not* done: no auth, no tests, no input validation beyond what EF/C# gives for free, no seed data script.

## Run it

```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"   # not on PATH by default in this environment
dotnet ef database update    # creates lunchticket.db from Migrations/ if it doesn't exist
dotnet run                   # Swagger UI at /swagger when Development
```

`appsettings.json` → `ConnectionStrings:Default` points at `Data Source=lunchticket.db` (gitignored, created locally by `dotnet ef database update`).

## Folder-by-folder — what's actually in each

### `Models/` — EF Core entities
Mirrors `docs/schema.dbml` table-for-table: `Student`, `Account`, `Card` (+`Staff`, `MenuItem`, `Timeslot`, `DailyMenuEntry`, `Order`, `Transaction`). One addition not in the dbml: **`Card.FailedScanCount`** — needed to implement the resolved "3 failed scans → lock" rule, which postdates the dbml file.

### `Data/AppDbContext.cs`
Fluent API encodes the constraints the docs call out as non-negotiable:
- `CHECK (Balance >= 0)` on `Accounts`, `CHECK (AvailableQuantity >= 0)` on `DailyMenuEntries`
- Unique indexes: `Student.StudentCode`, `Card.CardToken`, `Order.QrToken`, `Staff.Username`, and the compound `(Date, TimeslotId, MenuItemId)` on `DailyMenuEntries`
- Enums (`CardStatus`, `OrderType`, `OrderStatus`, `TransactionType`, `OrderSource`) stored as strings, not ints — readable directly in the sqlite file.

### `Migrations/`
One migration, `InitialCreate`, generated from the above. Regenerate with `dotnet ef migrations add <Name>` after any Models/AppDbContext change; apply with `dotnet ef database update`.

### `DTOs/`
Field names matched against `docs/api/openapi.yaml`'s `components.schemas` where they exist (Order, Account, Transaction, Card, MenuItem, Timeslot, request/response shapes for each endpoint). One deliberate deviation: openapi's `Order.status` enum (`pending/confirmed/ready/picked_up/cancelled`) doesn't match `docs/schema.dbml`'s live-schema note (`pending/ready/delivered/cancelled`) — **I went with schema.dbml** since `CLAUDE.md` names it the DB source of truth. Worth reconciling in openapi.yaml later.

### `Repositories/`
Pure EF Core queries, no business logic, one interface+impl pair per component (`Order`, `Ledger`, `Card`, `Menu`) per `docs/structure/backend.md`. No `NotificationRepository` — deliberate, `NotifySvc` doesn't own a table (see backend.md's explanation).

### `Services/` — where the business rules actually live
- **`LedgerService`** — `DeductBalanceAsync`/`RefundBalanceAsync`/`EnforceBalance`, every balance mutation writes one append-only `Transaction` row snapshotting `BalanceAfter`. `TopUpAsync` for staff-initiated top-ups.
- **`MenuService`** — `CheckAndReserveQuantityAsync` (check + decrement in one call — throws `OUT_OF_STOCK` if none left), `RestoreQuantityAsync`, `ReportOutOfStockAsync` (zeroes the slot, auto-refunds every affected pending/ready order, publishes a `stock_out` SSE event).
- **`CardFraudService`** — `IssueCardAsync` implements the lost-card flow literally: locks any existing active card for that student, then issues a new one, balance untouched by construction. `RecordFailedScanAsync` increments `FailedScanCount`, auto-locks at 3. `UnlockCardAsync` is the manual-unlock path (UC11) — resets the counter. Login uses a placeholder SHA-256 hash + opaque GUID token, **not JWT** — no auth package is installed yet, this is a stand-in until student/staff auth is actually designed.
- **`OrderService`** — the orchestrator. Each multi-step flow (`PlacePreOrderAsync`, `CancelPreOrderAsync`, `ConfirmPreOrderAsync`, `CreateWalkInOrderAsync`) wraps its steps in one `Database.BeginTransactionAsync()`/`SaveChangesAsync()`/`CommitAsync()`, in the exact step order documented in `docs/architecture/endpoint-atomicity.md`. Cancellation cutoff (2h) and refund tiers (100%/50%) are constants at the top of the file. QR confirm sets `QrToken = null` after first successful scan — that's the one-time-use rule, no background expiry job.
- **`NotificationService`** — SSE fan-out via `System.Threading.Channels`, one `Channel<NotificationEvent>` per connected subscriber, singleton-scoped so all services share the same fan-out point (ADR-3). No table, no repository, per backend.md.

### `Controllers/`
Thin — parse request, call one service method, return. Routes follow `docs/api/openapi.yaml` paths under `/api/...`. **`NotificationController.Stream`** is the one exception to "controllers don't do business logic" — it's just an SSE write loop, no rules.

## Known gap: no auth, so `studentId`/`staffId` are query params

Every endpoint that needs to know "who is acting" (`PlacePreOrder`, `ConfirmPreOrder`, `CreateWalkInOrder`, `TopUp`) takes that ID as a `[FromQuery]` parameter instead of reading it off a validated session/JWT claim. This isn't an oversight — `CLAUDE.md`'s Known Gaps section says student auth "is not designed yet," and I didn't want to invent an auth scheme unasked. `CardFraudService.LoginAsync` returns an opaque token today but nothing validates it on subsequent requests. **This needs a real decision before anything here is demo-safe or callable from an untrusted frontend.**

## Other things deliberately left undone

- No `[ApiController]` input validation attributes beyond required-ness from record types — no range checks on `amount`, no format checks on dates/times passed as strings.
- No unit/integration tests.
- No seed data — `Students`/`MenuItems`/`Timeslots`/`DailyMenuEntries` are empty on a fresh `dotnet ef database update`. I inserted throwaway rows via `sqlite3` directly to smoke-test, then deleted the DB and re-migrated to leave it clean.
- `CardFraudController`'s `IssueCard`/lock/unlock endpoints have no staff-authorization check — anyone who can reach the API can issue or lock any card.
- Concurrency-safety gap flagged in `CLAUDE.md` ("one active card per student", "one non-cancelled order per student per slot") is still enforced at the application level only (`HasActiveOrderForSlotAsync`, `GetActiveByStudentIdAsync` checks before insert) — no DB-level partial unique index yet, so it's still race-condition-prone under concurrent requests, exactly as flagged.
