---
name: lunchcard-ui
description: UI/UX guidelines for the student lunch card system. Load when designing screens, wireframes, or component layouts for any actor interface.
---

Load `/lunchcard-context` first if the full project context isn't already in scope.

---

## Interfaces by Actor

| Actor | Interface type | Key constraint |
|---|---|---|
| Student | Mobile-first responsive web | All core flows must work on a small screen in one hand |
| Counter staff | POS web (tablet or desktop) | Must work when network is degraded — manual fallback always reachable in ≤2 taps |
| Kitchen | Read-only display web page | No interaction beyond "mark item done"; auto-refreshes, no login friction |
| Admin | Back-office web | Desktop-first; density over simplicity |

---

## Completed Wireframes

**Student flow** — `wireframes/student/index.html` (open in browser, no server needed)

| Screen | Status | Key decisions baked in |
|---|---|---|
| Login | Done | SSO (school account) + manual MSSV/password; two separate paths, not hidden |
| Dashboard | Done | Balance card prominent at top; quick-action row for the 4 most common tasks; today's pre-order shown immediately |
| Menu (today) | Done | Timeslot tabs across the top; slot count shown per item; "sold out" state visually distinct and non-interactive |
| Pre-order | Done | 3-step stepper (select item → select timeslot → confirm); balance preview shows amount before and after; "low stock" and "full" slot states visible |
| QR confirmation | Done | QR + short alphanumeric code (fallback for scan failure); expiry time displayed prominently in red; cancel button accessible |
| Transaction history | Done | Grouped by date; credit vs. debit visually differentiated; filter tabs (all / top-up / orders) |

---

## Screens Not Yet Started

### Counter staff (POS)
- Scan QR or type student ID manually (always both options — offline fallback)
- Display pre-order details for confirmation
- Walk-in order entry: select student → select item → confirm + deduct
- Mark order as delivered

### Kitchen display
- Live order queue grouped by food item
- Each order card shows: item name, quantity, pickup timeslot, order type (pre-order vs. walk-in visually distinct)
- "Done" button per item; completed items move to a separate done column or fade out
- No login — display URL is the access control

### Admin
- Top-up balance for a student account
- Lock card / issue replacement card (balance carry-over must be shown explicitly in the UI)
- Create/edit daily menu: set items, per-timeslot slot counts, date
- Reports: revenue, orders by timeslot, pre-order vs. walk-in ratio

---

## Interaction Rules

**QR expiry:** QR codes expire 15 minutes after the start of the chosen timeslot. Show a countdown or a fixed expiry time — never hide the deadline.

**Slot availability display:**
- > 5 slots remaining: show count in neutral color
- ≤ 5 slots: show count in red/warning color
- 0 slots: item is non-interactive (greyed out, no add button)

**Queue separation:** Pre-order and walk-in must be visually distinguishable at all times on both the POS and kitchen display. Use a label, color, or badge — never rely on order-of-arrival alone.

**Offline fallback on POS:** If QR scan fails, staff can type the student ID. This path must be reachable in ≤ 2 taps/clicks from the main POS screen — not buried in a settings menu.

**Balance display:** Always show the balance *after* a transaction completes on the confirmation screen. Never leave the user without feedback on their new balance.

---

## Wireframe Conventions (match existing student wireframes)

- Phone frames rendered as white cards with `border-radius: 20px` and a layered box-shadow
- Status bar + top bar use `#2563eb` (Tailwind blue-600) as the primary color
- Bottom nav for student screens: Home / Menu / My Orders / Account
- Badges: `badge-success` (green), `badge-warning` (amber), `badge-primary` (blue), `badge-danger` (red), `badge-gray`
- All wireframes are static HTML — no JS framework, no build step, open directly in browser
