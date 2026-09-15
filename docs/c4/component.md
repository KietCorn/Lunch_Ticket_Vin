# C4 Model — Level 3: Component

Zooms into the "Backend API [FastAPI]" container from `container.md`.
Components are drawn from the actual code structure (`app/routers/*.py` +
`app/auth.py`), not a generic textbook layout. See the explanation above
this file (session notes) for the three ways the real structure diverges
from a typical "Order/Balance/Menu/Auth service" split:

1. No service layer — logic lives directly in router functions.
2. No shared Balance/Payment component — debit/credit/refund logic is
   duplicated inline in both `orders.py` and `accounts.py`.
3. "Mark ready" is implemented in the **Orders** component (`orders.py`),
   not the **Kitchen Stream** component (`kitchen.py`), even though it's
   conceptually a kitchen action. Kitchen Stream is read-only (SSE +
   snapshot) and uses its own DB session per tick instead of the shared
   `get_db` dependency.

Admin → Menu / Accounts arrows are marked `(planned)` — `menu.py` is
GET-only today and no reports endpoint exists yet, matching the "Admin
deferred" status in `CLAUDE.md`.

```mermaid
flowchart LR
    StudentApp(["Student Web App"])
    StaffApp(["Staff Web App"])
    KitchenDisplay(["Kitchen Display"])
    AdminApp(["Admin Web App"])

    subgraph API["Backend API [FastAPI]"]
        Auth["Auth Component\n[auth.py router + auth.py]"]
        Students["Students & Cards Component\n[students.py]"]
        Accounts["Accounts & Transactions Component\n[accounts.py]"]
        Menu["Menu Component\n[menu.py]"]
        Orders["Orders Component\n[orders.py]"]
        KitchenStream["Kitchen Stream Component\n[kitchen.py]"]
    end

    DB[("Database\n[SQLite]")]

    StudentApp -- "login [HTTP/REST]" --> Auth
    StudentApp -- "browse daily menu [HTTP/REST]" --> Menu
    StudentApp -- "place/cancel pre-order, QR [HTTP/REST]" --> Orders
    StudentApp -- "view balance & history [HTTP/REST]" --> Accounts

    StaffApp -- "login [HTTP/REST]" --> Auth
    StaffApp -- "lookup student, lock/issue card [HTTP/REST]" --> Students
    StaffApp -- "top-up balance [HTTP/REST]" --> Accounts
    StaffApp -- "walk-in order, confirm QR pickup [HTTP/REST]" --> Orders

    KitchenDisplay -- "snapshot + live queue [HTTP/REST + SSE]" --> KitchenStream
    KitchenDisplay -- "mark ready [HTTP/REST]" --> Orders

    AdminApp -- "manage menu & timeslots (planned) [HTTP/REST]" --> Menu
    AdminApp -- "view transaction reports (planned) [HTTP/REST]" --> Accounts

    Auth -- "staff lookup [SQL]" --> DB
    Students -- "reads/writes [SQL]" --> DB
    Accounts -- "reads/writes [SQL]" --> DB
    Menu -- "reads [SQL]" --> DB
    Orders -- "reads/writes [SQL]" --> DB
    KitchenStream -- "reads (own session) [SQL]" --> DB

    classDef actor fill:#08427b,stroke:#052e56,color:#fff
    classDef component fill:#85bbf0,stroke:#5d82a8,color:#000
    classDef database fill:#438dd5,stroke:#2e6295,color:#fff

    class StudentApp,StaffApp,KitchenDisplay,AdminApp actor
    class Auth,Students,Accounts,Menu,Orders,KitchenStream component
    class DB database
```
