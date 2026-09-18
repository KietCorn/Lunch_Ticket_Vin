# LunchTicket.Api

ASP.NET Core Web API (.NET 8), 3-tier (Controllers → Services → Repositories → Models/DTOs), EF Core Code-First on SQLite. Scaffolded to match `docs/structure/backend.md` and the business rules in `../CLAUDE.md`. This file exists so you can catch up on what was actually built, file by file — the design docs describe the *target*, this describes what's *here now*.

## Status: scaffold + working core flows, not production-ready

Everything below compiles, the DB migration applies, and the atomic order/ledger flows have been smoke-tested end-to-end (place pre-order → balance deducted + quantity decremented + one transaction row written; cancel → correct 100%/50% refund tier applied + quantity restored). JWT-based auth is now implemented and smoke-tested (see "Auth" below). What's *not* done: no tests, no input validation beyond what EF/C# gives for free, no seed data script beyond the one bootstrap admin row.

## Run it

```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"   # not on PATH by default in this environment
dotnet ef database update    # creates lunchticket.db from Migrations/ if it doesn't exist
dotnet run                   # Swagger UI at /swagger when Development
```

`appsettings.json` → `ConnectionStrings:Default` points at `Data Source=lunchticket.db` (gitignored, created locally by `dotnet ef database update`).

**First-run bootstrap:** a fresh `dotnet ef database update` gives you an empty DB with no login-capable accounts — since `POST /api/admin/*` (the only way to create students/staff) is itself admin-only, there's a chicken-and-egg problem. Seed one admin row directly:

```bash
python3 - <<'EOF'
import sqlite3, hashlib
h = hashlib.sha256("admin123".encode()).hexdigest().upper()
conn = sqlite3.connect("lunchticket.db")
conn.execute("INSERT INTO Staff (Username, FullName, PasswordHash, IsAdmin, IsActive, CreatedAt) VALUES (?, ?, ?, 1, 1, datetime('now'))",
             ("admin", "Bootstrap Admin", h))
conn.commit()
conn.close()
EOF
```

Then `POST /api/auth/login` with `{"username":"admin","password":"admin123"}` to get a token, and use `POST /api/admin/students` / `POST /api/admin/staff` from there to create real accounts. There's no API-level seed endpoint on purpose — this keeps the "only an admin can create accounts" rule from having a backdoor.

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
- **`CardFraudService`** — `IssueCardAsync` implements the lost-card flow literally: locks any existing active card for that student, then issues a new one, balance untouched by construction. `RecordFailedScanAsync` increments `FailedScanCount`, auto-locks at 3. `UnlockCardAsync` is the manual-unlock path (UC11) — resets the counter. `LoginAsync` now issues a real JWT (see "Auth" below); `CreateStudentAsync`/`CreateStaffAsync` back the admin-only account-creation endpoints.
- **`OrderService`** — the orchestrator. Each multi-step flow (`PlacePreOrderAsync`, `CancelPreOrderAsync`, `ConfirmPreOrderAsync`, `CreateWalkInOrderAsync`) wraps its steps in one `Database.BeginTransactionAsync()`/`SaveChangesAsync()`/`CommitAsync()`, in the exact step order documented in `docs/architecture/endpoint-atomicity.md`. Cancellation cutoff (2h) and refund tiers (100%/50%) are constants at the top of the file. QR confirm sets `QrToken = null` after first successful scan — that's the one-time-use rule, no background expiry job.
- **`NotificationService`** — SSE fan-out via `System.Threading.Channels`, one `Channel<NotificationEvent>` per connected subscriber, singleton-scoped so all services share the same fan-out point (ADR-3). No table, no repository, per backend.md.

### `Controllers/`
Thin — parse request, call one service method, return. Routes follow `docs/api/openapi.yaml` paths under `/api/...`. **`NotificationController.Stream`** is the one exception to "controllers don't do business logic" — it's just an SSE write loop, no rules.

## Auth

Not in any of the design docs — `CLAUDE.md`'s Known Gaps said student auth "is not designed yet," so this is a from-scratch decision, not a spec I followed. Kept as simple as the school-project scope allows:

- **JWT Bearer**, one unified `POST /api/auth/login { username/studentCode, password }` — `CardFraudService.LoginAsync` tries staff-by-username first, falls back to student-by-code. Returns `{ token, role, id, fullName }`. Role is `student`, `staff`, or `admin` (staff row's `IsAdmin` flag decides `staff` vs `admin`).
- **Passwords** — same placeholder SHA-256 hash used before (`Student.PasswordHash`/`Staff.PasswordHash`), not a real KDF (no bcrypt/PBKDF2/Argon2). Fine for a school project, flagging it because it's not fine for anything else.
- **Signing key** — hardcoded dev value in `appsettings.json` under `Jwt:SigningKey`. Must be replaced (and moved out of source control) before any real deployment.
- **Authorization** — every controller has `[Authorize(Roles = "...")]` at the class or action level. Student-scoped resources (own orders, own balance, own transactions) additionally do a manual ownership check (`User.GetUserId() == studentId`, `Forbid()` otherwise) since role alone doesn't prove *whose* data it is.
- **`studentId`/`staffId` are no longer query params** — they come off the JWT claims via `ClaimsPrincipalExtensions.GetUserId()` (reads `ClaimTypes.NameIdentifier`).
- **Account creation is admin-only** — `POST /api/admin/students` / `POST /api/admin/staff`, gated by `[Authorize(Roles = "admin")]`. See the bootstrap note above for how the *first* admin gets created.
- **SSE and auth** — `EventSource` (used for `/api/kitchen/stream`) can't set an `Authorization` header, so that one endpoint also accepts the token as an `?access_token=` query param (wired via `JwtBearerEvents.OnMessageReceived` in `Program.cs`, scoped to that path only).

Verified end-to-end: unauthenticated requests get `401`; a valid token authorizes matching roles; a student token gets `403` on staff-only routes (e.g. `/api/orders/queue`) and `200` on their own balance/orders.

## Other things deliberately left undone

- No `[ApiController]` input validation attributes beyond required-ness from record types — no range checks on `amount`, no format checks on dates/times passed as strings.
- No unit/integration tests.
- No seed data beyond the one bootstrap admin row (see "Auth" above) — `Students`/`MenuItems`/`Timeslots`/`DailyMenuEntries` are empty on a fresh `dotnet ef database update`. I inserted throwaway rows directly to smoke-test, then deleted the DB and re-migrated to leave it clean.
- Concurrency-safety gap flagged in `CLAUDE.md` ("one active card per student", "one non-cancelled order per student per slot") is still enforced at the application level only (`HasActiveOrderForSlotAsync`, `GetActiveByStudentIdAsync` checks before insert) — no DB-level partial unique index yet, so it's still race-condition-prone under concurrent requests, exactly as flagged.
- Refresh tokens / token revocation aren't implemented — the JWT is valid for its full lifetime (`Jwt:AccessTokenMinutes`, currently 8h) with no server-side logout or blocklist.
