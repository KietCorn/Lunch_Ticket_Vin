# Entity-Relationship Diagram — Student Lunch Card System

Source of truth: `app/models.py`. Mirrored in `docs/schema.dbml` (dbdiagram.io) and `docs/schema.sql` (generated `CREATE TABLE` statements). Paste the block below into [mermaid.live](https://mermaid.live) to render.

```mermaid
erDiagram
    students ||--|| accounts : "UC-S09 owns"
    students ||--o{ cards : "UC-C05/C06 owns"
    students ||--o{ orders : "UC-S03/C03 places"
    accounts ||--o{ transactions : "UC-SYS01 logs"
    menu_items ||--o{ daily_menu : "UC-A04 listed in"
    timeslots ||--o{ daily_menu : "UC-A04 scheduled in"
    timeslots ||--o{ orders : "denormalized for queue queries"
    daily_menu ||--o{ orders : "UC-S03/C03 ordered as"
    orders ||--o{ transactions : "UC-SYS01 triggers"
    staff ||--o{ orders : "UC-C03 places (walk-in)"
    staff ||--o{ orders : "UC-C02 delivers"
    staff ||--o{ transactions : "UC-C04 processes top-up"

    students {
        int id PK
        string student_id UK "MSSV"
        string full_name
        string email UK
        datetime created_at
        datetime updated_at
    }

    accounts {
        int id PK
        int student_id FK, UK
        decimal balance
        datetime updated_at
    }

    cards {
        int id PK
        int student_id FK
        string card_token UK
        string status "active | locked"
        datetime issued_at
        datetime locked_at
    }

    staff {
        int id PK
        string username UK
        string full_name
        string password_hash
        bool is_admin
        bool is_active
        datetime created_at
    }

    menu_items {
        int id PK
        string name
        string description
        decimal price
        bool is_active
    }

    timeslots {
        int id PK
        string label
        string start_time
        string end_time
        int sort_order
    }

    daily_menu {
        int id PK
        string date
        int timeslot_id FK
        int menu_item_id FK
        int available_quantity
    }

    orders {
        int id PK
        int student_id FK
        int daily_menu_id FK
        int timeslot_id FK
        string order_type "pre_order | walk_in"
        string status "pending | ready | delivered | cancelled"
        string source "qr_scan | manual_entry"
        decimal amount_charged
        string qr_token UK
        datetime qr_expires_at
        int placed_by_staff FK
        int delivered_by FK
        datetime created_at
        datetime updated_at
    }

    transactions {
        int id PK
        int account_id FK
        string transaction_type "top_up | deduction | refund"
        decimal amount
        decimal balance_after
        int reference_order FK
        int actor_id FK
        string note
        datetime created_at
    }
```

## FK relationships and the use cases they support

| FK | Relationship | Use case(s) |
|---|---|---|
| `accounts.student_id → students.id` | 1–1 | UC-S09 (view balance), UC-C04 (top-up). Balance decoupled from card (non-negotiable principle #1). |
| `cards.student_id → students.id` | 1–N | UC-C05 (lock card), UC-C06 (issue new card). Old locked card + new active card coexist; balance carries over. |
| `orders.student_id → students.id` | 1–N | UC-S03, UC-C03. Every order belongs to exactly one student. |
| `orders.daily_menu_id → daily_menu.id` | 1–N | UC-S03, UC-C03. Ties order to item+timeslot+date; drives `available_quantity` decrement (UC-SYS04). |
| `orders.timeslot_id → timeslots.id` | 1–N | UC-K01 (kitchen queue). Denormalized so the SSE queue can filter/sort by timeslot without joining through `daily_menu`. |
| `orders.placed_by_staff → staff.id` | 1–N, nullable | UC-C03. Null for pre-orders (student self-service); set for walk-ins. |
| `orders.delivered_by → staff.id` | 1–N, nullable | UC-C02. Which staff member confirmed pickup (QR scan or manual). |
| `daily_menu.menu_item_id → menu_items.id` | N–N junction (with `timeslots`) | UC-A04. `daily_menu` carries the relationship-specific attribute `available_quantity`. |
| `daily_menu.timeslot_id → timeslots.id` | N–N junction | UC-A04. |
| `transactions.account_id → accounts.id` | 1–N | UC-SYS01, triggered by UC-S03/S04/C03/C04. Append-only balance ledger. |
| `transactions.reference_order → orders.id` | 1–N, nullable | UC-S03 (deduction), UC-S04 (refund). Null for top-ups (UC-C04) since those aren't tied to an order. |
| `transactions.actor_id → staff.id` | 1–N, nullable | UC-C04. Records which staff member processed a top-up; null for student-triggered deductions/refunds. |

## Notes on divergence from the simplified BA-level ERD

The earlier draft (given directly in chat, not saved to a file) covered only the 9 core entities from the use case spec. This version adds the staff-accountability columns that already exist in `app/models.py` — `orders.source/placed_by_staff/delivered_by`, `orders.timeslot_id`, and `transactions.reference_order/actor_id/note` — so that `usecase.md`, `schema.dbml`, this ERD, and `schema.sql` all describe the same schema with no drift.
