# Use Case Specification — Student Lunch Card System

## Actors

| Actor | Type | Description |
|---|---|---|
| **Student** | Primary | Places pre-orders and walk-in orders; manages their own balance via card |
| **Counter Staff** | Primary | Processes walk-in orders, confirms QR pickups, tops up balances, manages cards |
| **Kitchen** | Primary | Reads the real-time order display; marks orders ready |
| **Admin** | Primary | Manages menu, timeslots, staff accounts, and student accounts |
| **System** | Secondary | Enforces balance rules, QR expiry, queue separation, and transaction integrity |

---

## Use Case Diagram (draw.io structure)

```
System boundary: Lunch Card System
│
├── Student
│   ├── UC-S01  Login
│   ├── UC-S02  View Daily Menu
│   ├── UC-S03  Place Pre-Order          <<include>> UC-S06 Deduct Balance
│   ├── UC-S04  Cancel Pre-Order         <<include>> UC-S07 Refund Balance
│   ├── UC-S05  Show QR Code at Counter
│   ├── UC-S08  View Order Status
│   ├── UC-S09  View Balance & Transactions
│
├── Counter Staff
│   ├── UC-C01  Login
│   ├── UC-C02  Confirm Pre-Order (scan QR)   <<include>> UC-S06 Deduct Balance (already done)
│   ├── UC-C03  Create Walk-In Order           <<include>> UC-S06 Deduct Balance
│   ├── UC-C04  Top Up Student Balance         <<include>> UC-SYS01 Append Transaction
│   ├── UC-C05  Lock Student Card
│   ├── UC-C06  Issue New Card
│
├── Kitchen
│   ├── UC-K01  View Live Order Queue (auto-refresh / SSE)
│   ├── UC-K02  Mark Order as Ready
│
├── Admin
│   ├── UC-A01  Login
│   ├── UC-A02  Manage Menu Items (CRUD)
│   ├── UC-A03  Manage Timeslots (CRUD)
│   ├── UC-A04  Set Daily Menu & Quantities
│   ├── UC-A05  Manage Staff Accounts
│   ├── UC-A06  Manage Student Accounts
│   ├── UC-A07  View Transaction Reports
│
└── System (internal / <<include>> targets)
    ├── UC-SYS01  Append Transaction Record
    ├── UC-SYS02  Enforce Balance ≥ 0
    ├── UC-SYS03  Expire QR Token
    └── UC-SYS04  Decrement Available Quantity
```

> **Draw.io tip:** Place each actor as a stick figure outside the system boundary rectangle. Draw solid arrows (association) from actor to use case. Use dashed arrows for `<<include>>` and `<<extend>>` relationships inside the boundary.

---

## Use Cases — Student

### UC-S01 Login
| Field | Detail |
|---|---|
| **Actor** | Student |
| **Precondition** | Student has an account in the system |
| **Main Flow** | 1. Student opens the app → enters student ID and password. 2. System validates credentials. 3. System returns session token; app shows home screen. |
| **Alt Flow** | 2a. Invalid credentials → system shows error; student retries. |
| **Postcondition** | Student is authenticated; session token stored client-side. |

---

### UC-S02 View Daily Menu
| Field | Detail |
|---|---|
| **Actor** | Student |
| **Precondition** | Student is logged in |
| **Main Flow** | 1. Student navigates to "Menu" screen. 2. System returns today's available menu items grouped by timeslot. 3. App displays item name, price, and remaining quantity per slot. |
| **Alt Flow** | 2a. No items available for today → app shows "No menu today" message. |
| **Postcondition** | Student can see what is available and at what price. |

---

### UC-S03 Place Pre-Order
| Field | Detail |
|---|---|
| **Actor** | Student |
| **Precondition** | Student is logged in; selected menu item has `available_quantity > 0`; order deadline for the timeslot has not passed; student has no existing non-cancelled order for the same daily_menu entry |
| **Main Flow** | 1. Student selects a menu item and timeslot → taps "Order". 2. App shows confirmation with price. 3. Student confirms. 4. System checks balance ≥ price (UC-SYS02). 5. System atomically: deducts balance, appends deduction transaction (UC-SYS01), decrements `available_quantity` (UC-SYS04), creates order with `order_type = pre_order` and `status = pending`, generates `qr_token`. 6. App shows success screen with QR code. |
| **Alt Flow** | 4a. Insufficient balance → system rejects; app prompts student to top up. 4b. `available_quantity` reached 0 between step 1 and 5 → system rejects with "Sold out". |
| **Postcondition** | Order exists with status `pending`; balance reduced; QR token ready for pickup. |
| **Includes** | UC-SYS01, UC-SYS02, UC-SYS04 |

---

### UC-S04 Cancel Pre-Order
| Field | Detail |
|---|---|
| **Actor** | Student |
| **Precondition** | Student is logged in; order exists with `status = pending`; cancellation deadline has not passed |
| **Main Flow** | 1. Student opens order detail → taps "Cancel". 2. System sets `status = cancelled`. 3. System atomically: refunds balance, appends refund transaction (UC-SYS01), increments `available_quantity`. 4. App shows updated balance. |
| **Alt Flow** | 1a. Order already `ready` or `delivered` → cancel button disabled; student cannot cancel. |
| **Postcondition** | Order status is `cancelled`; balance restored; slot quantity freed. |

---

### UC-S05 Show QR Code at Counter
| Field | Detail |
|---|---|
| **Actor** | Student |
| **Precondition** | Student has a `pending` pre-order with a valid (non-expired) `qr_token` |
| **Main Flow** | 1. Student opens order detail. 2. App displays QR code encoding `qr_token`. 3. Student presents screen to counter staff. |
| **Alt Flow** | 2a. Token expired (past `qr_expires_at`) → app shows "QR Expired" and prompts student to contact staff. |
| **Postcondition** | Counter staff can scan and confirm pickup. |
| **Extends** | UC-C02 (counter staff scans the QR) |

---

### UC-S08 View Order Status
| Field | Detail |
|---|---|
| **Actor** | Student |
| **Precondition** | Student is logged in |
| **Main Flow** | 1. Student opens "My Orders". 2. System returns all orders for the student. 3. App displays status (`pending`, `ready`, `delivered`, `cancelled`) for each order. |
| **Postcondition** | Student can track current and past orders. |

---

### UC-S09 View Balance & Transactions
| Field | Detail |
|---|---|
| **Actor** | Student |
| **Precondition** | Student is logged in |
| **Main Flow** | 1. Student opens "Balance" screen. 2. System returns current balance and transaction history (top-ups, deductions, refunds) with timestamps. |
| **Postcondition** | Student has full visibility of their financial activity. |

---

## Use Cases — Counter Staff

### UC-C01 Login
| Field | Detail |
|---|---|
| **Actor** | Counter Staff |
| **Precondition** | Staff account exists and is active |
| **Main Flow** | 1. Staff enters username and password on POS interface. 2. System validates credentials. 3. System returns session; POS shows main screen. |

---

### UC-C02 Confirm Pre-Order (QR Scan)
| Field | Detail |
|---|---|
| **Actor** | Counter Staff |
| **Precondition** | Staff is logged in; student presents QR code; `qr_token` is valid and not expired; order `status = pending` |
| **Main Flow** | 1. Staff scans QR code. 2. System looks up `qr_token` → finds matching order. 3. System sets `status = delivered`, `source = qr_scan`, and `delivered_by = <staff.id>`. 4. POS shows order details and student name as confirmation. |
| **Alt Flow** | 2a. Token not found or expired → POS shows error; staff confirms manually by student ID, system sets `source = manual_entry` instead. 2b. Order already `delivered` → POS shows "Already collected". |
| **Postcondition** | Order status is `delivered`; `delivered_by` records which staff member confirmed pickup; balance was already deducted at pre-order time (no second deduction). |

---

### UC-C03 Create Walk-In Order
| Field | Detail |
|---|---|
| **Actor** | Counter Staff |
| **Precondition** | Staff is logged in; student is present at counter; selected menu item has `available_quantity > 0` |
| **Main Flow** | 1. Staff searches for student (by student ID or card scan). 2. Staff selects menu item and timeslot → confirms. 3. System checks balance ≥ price (UC-SYS02). 4. System atomically: deducts balance, appends transaction (UC-SYS01), decrements quantity (UC-SYS04), creates order with `order_type = walk_in`, `status = pending`, `timeslot_id` (denormalized for fast queue queries), and `placed_by_staff = <staff.id>`. 5. POS shows success; order appears in walk-in queue on kitchen display. |
| **Alt Flow** | 3a. Insufficient balance → POS shows error; staff informs student to top up. 3b. Sold out → POS blocks selection. |
| **Postcondition** | Walk-in order exists in a **separate queue** from pre-orders. |
| **Non-negotiable** | Walk-in queue and pre-order queue are **never merged**. |

---

### UC-C04 Top Up Student Balance
| Field | Detail |
|---|---|
| **Actor** | Counter Staff |
| **Precondition** | Staff is logged in; student is present with cash or voucher |
| **Main Flow** | 1. Staff searches for student. 2. Staff enters top-up amount. 3. System appends a `top_up` transaction (UC-SYS01) recording `actor_id = <staff.id>` (who processed it) and increases `accounts.balance`. 4. POS and student app show new balance. |
| **Postcondition** | Balance increased; transaction record appended. |

---

### UC-C05 Lock Student Card
| Field | Detail |
|---|---|
| **Actor** | Counter Staff |
| **Precondition** | Staff is logged in; student reports card lost/stolen; card `status = active` |
| **Main Flow** | 1. Staff finds student record. 2. Staff clicks "Lock Card". 3. System sets `cards.status = locked` and records `locked_at`. 4. Balance on `accounts` is **not touched**. |
| **Postcondition** | Card is locked; balance is preserved intact on the student account. |
| **Non-negotiable** | Locking a card never touches `accounts.balance`. |

---

### UC-C06 Issue New Card
| Field | Detail |
|---|---|
| **Actor** | Counter Staff |
| **Precondition** | Staff is logged in; old card is locked (UC-C05); new physical card is available |
| **Main Flow** | 1. Staff selects student. 2. Staff scans new card token. 3. System creates a new `cards` row linked to the student with `status = active`. 4. Previous card remains locked. |
| **Postcondition** | Student has one new active card; balance unchanged; old card permanently locked. |

---

## Use Cases — Kitchen

### UC-K01 View Live Order Queue
| Field | Detail |
|---|---|
| **Actor** | Kitchen |
| **Precondition** | Kitchen display is open; staff optionally logged in |
| **Main Flow** | 1. Kitchen display connects to `GET /api/kitchen/stream` (SSE). 2. System pushes new/updated orders in real time. 3. Display shows two separate sections: **Pre-Order Queue** and **Walk-In Queue**, each sorted by timeslot and creation time. |
| **Postcondition** | Kitchen always sees live demand without manual refresh. |
| **Non-negotiable** | The two queues are always rendered separately, never merged. |

---

### UC-K02 Mark Order as Ready
| Field | Detail |
|---|---|
| **Actor** | Kitchen |
| **Precondition** | Order exists with `status = pending` |
| **Main Flow** | 1. Kitchen staff taps order card on display. 2. System sets `status = ready`. 3. SSE stream broadcasts update to all connected clients. |
| **Postcondition** | Order status is `ready`; student app can reflect this if polling. |

---

## Use Cases — Admin

### UC-A01 Login
| Field | Detail |
|---|---|
| **Actor** | Admin |
| **Precondition** | Admin staff account exists with `is_admin = true` |
| **Main Flow** | 1. Admin logs in with username/password. 2. System validates credentials and admin flag. 3. Back-office interface loads. |

---

### UC-A02 Manage Menu Items
| Field | Detail |
|---|---|
| **Actor** | Admin |
| **Precondition** | Admin is logged in |
| **Main Flow** | 1. Admin opens "Menu Items". 2. Admin can Create / Edit / Deactivate items. Deactivating sets `is_active = false`; past orders are unaffected. |

---

### UC-A03 Manage Timeslots
| Field | Detail |
|---|---|
| **Actor** | Admin |
| **Main Flow** | Admin creates or reorders timeslots (label, start_time, end_time, sort_order). Changes affect future daily menus only. |

---

### UC-A04 Set Daily Menu & Quantities
| Field | Detail |
|---|---|
| **Actor** | Admin |
| **Main Flow** | 1. Admin selects a date. 2. Admin assigns menu items to timeslots and sets `available_quantity` for each. 3. System creates `daily_menu` rows; `available_quantity` is the stock ceiling. |

---

### UC-A05 Manage Staff Accounts
| Field | Detail |
|---|---|
| **Actor** | Admin |
| **Main Flow** | Admin creates, edits, or deactivates staff accounts. Setting `is_active = false` prevents login. |

---

### UC-A06 Manage Student Accounts
| Field | Detail |
|---|---|
| **Actor** | Admin |
| **Main Flow** | Admin creates student records, links cards, and can view or adjust balance (with transaction record). |

---

### UC-A07 View Transaction Reports
| Field | Detail |
|---|---|
| **Actor** | Admin |
| **Main Flow** | Admin filters transactions by date range, type (`top_up`, `deduction`, `refund`), or student. System returns ordered results with running balance. |

---

## System Use Cases (internal)

### UC-SYS01 Append Transaction Record
Triggered by: UC-S03, UC-S04, UC-C03, UC-C04.
Atomically inserts one row into `transactions` with `transaction_type`, `amount`, and `balance_after` snapshot. Deduction/refund rows link back to the triggering order via `reference_order` (null for top-ups). Rows created by staff (top-up) record `actor_id`; an optional `note` may be attached for audit context. **No UPDATE or DELETE ever runs on committed transaction rows.**

### UC-SYS02 Enforce Balance ≥ 0
Triggered before every deduction. If `balance - price < 0`, the operation is rejected with an error. Balance never goes negative.

### UC-SYS03 Expire QR Token
Triggered at pickup time (UC-C02). If `now > qr_expires_at`, the token is considered invalid and the order cannot be confirmed via QR. Manual override by staff is required.

### UC-SYS04 Decrement Available Quantity
Triggered by UC-S03 and UC-C03. Uses an atomic check-then-decrement: if `available_quantity = 0` at the moment of decrement, the order is rejected with "Sold out".

---

## Relationships Summary (for draw.io)

| Relationship | Type | Note |
|---|---|---|
| UC-S03 → UC-SYS01 | `<<include>>` | Every pre-order always appends a transaction |
| UC-S03 → UC-SYS02 | `<<include>>` | Balance check is mandatory before deduction |
| UC-S03 → UC-SYS04 | `<<include>>` | Quantity must be decremented atomically |
| UC-S04 → UC-SYS01 | `<<include>>` | Refund always appends a transaction |
| UC-C03 → UC-SYS01 | `<<include>>` | Walk-in deduction always appends a transaction |
| UC-C03 → UC-SYS02 | `<<include>>` | Balance check before walk-in deduction |
| UC-C03 → UC-SYS04 | `<<include>>` | Quantity decremented for walk-in too |
| UC-C04 → UC-SYS01 | `<<include>>` | Top-up always appends a transaction |
| UC-S05 → UC-C02 | `<<extend>>` | Student showing QR enables staff to confirm |
| UC-C05 → UC-C06 | precedes | Lock old card before issuing new one |
