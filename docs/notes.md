# Notes

---

## UML (Unified Modeling Language)

A visual language for modeling software. Not a methodology — just a notation.

### Two families

| Family | Focus | Examples |
|---|---|---|
| **Structural** | What the system *is* | Class, Component, Deployment |
| **Behavioral** | What the system *does* | Use Case, Sequence, Activity, State Machine |

---

### Use Case Diagram

Who does what with the system?

| Element | Meaning |
|---|---|
| Stick figure | Actor — a role outside the system |
| Oval | Use Case — a goal the actor achieves |
| Rectangle | System boundary |
| Solid line | Actor participates in use case |
| `<<include>>` | Always triggered — mandatory |
| `<<extend>>` | Optionally triggered — conditional |
| Hollow triangle arrow | Generalization (inheritance) |

---

### Class Diagram

What are the data structures and their relationships?

**Visibility:** `+` public · `-` private · `#` protected

**Relationships:**

| Arrow | Name | Memory hook |
|---|---|---|
| Solid line | Association | "has a reference to" |
| Hollow diamond | Aggregation | "contains, but B lives on" |
| Filled diamond | Composition | "owns — B dies with A" |
| Dashed arrow | Dependency | "uses temporarily" |
| Hollow triangle (solid) | Inheritance | "is a" |
| Hollow triangle (dashed) | Realization | "implements interface" |

**Multiplicity:** `1` · `0..1` · `*` · `1..*`

---

### Sequence Diagram

In what order do objects talk to each other?

| Element | Meaning |
|---|---|
| Vertical dashed line | Lifeline — time flows downward |
| Solid arrow → | Synchronous message (waits for response) |
| Dashed arrow ← | Return message |
| Open arrow → | Async message (fire and forget) |
| `alt` / `opt` / `loop` frame | if-else / optional / repeat |

---

### Activity Diagram

Step-by-step flow of a process.

| Element | Meaning |
|---|---|
| Filled circle | Start |
| Circle with ring | End |
| Rounded rectangle | Action |
| Diamond | Decision or merge |
| Thick bar | Fork (split) or Join (sync) |
| Swim lane | Who performs the actions |

---

### State Machine Diagram

What states can an object be in, and what triggers transitions?

Transition syntax: `event [guard] / action`

Example: `confirm [balance >= price] / deduct()`

---

## UI/UX Design — From UML to Interface

### How UML feeds into UI/UX

UML defines the *logic*. UI/UX translates that logic into *screens people can use*.

| UML artifact | What it gives UI/UX |
|---|---|
| Use Case Diagram | List of screens needed — one screen per major use case |
| Actor | Persona — who the screen is designed for |
| `<<include>>` flow | A step the user must complete before moving forward |
| Sequence Diagram | The exact order of UI interactions and feedback states |
| State Machine | What the UI shows per state (e.g. pending → button enabled, delivered → button hidden) |
| Class / Schema | What data fields appear on the form or card |

---

### The Pipeline

```
Use Case Diagram
      ↓
  Screen List         — one use case = one (or more) screen(s)
      ↓
  User Flow           — connect screens with arrows showing how user navigates
      ↓
  Wireframe           — layout of each screen, no colors, no style
      ↓
  Prototype           — clickable wireframe to validate the flow
      ↓
  High-fidelity UI    — real colors, typography, components
```

---

### Use Case → Screen mapping

Each use case owned by an actor maps to a screen that actor sees.

| Use Case | Actor | Screen |
|---|---|---|
| Login | Student / Staff | Login screen |
| View Daily Menu | Student | Menu / Home screen |
| Place Pre-Order | Student | Order confirmation screen |
| Show QR Code | Student | QR display screen |
| View Order Status | Student | Order history screen |
| View Balance & Transactions | Student | Balance screen |
| Confirm Pre-Order (QR scan) | Staff | POS scan screen |
| Create Walk-In Order | Staff | POS order screen |
| Top Up Balance | Staff | POS top-up screen |
| Lock / Issue Card | Staff | POS card management screen |
| View Live Order Queue | Kitchen | Kitchen display screen |
| Manage Menu / Timeslots | Admin | Admin back-office screens |

---

### Key UI/UX principles to keep in mind

- **One use case = one clear action** — don't put two primary actions on the same screen
- **States are visible** — every object state (pending, ready, cancelled) must have a distinct visual treatment
- **Error = feedback** — every `alt flow` in the use case needs an error message or empty state on the UI
- **Actor = context** — design each screen from the actor's mental model, not the database structure
- **Guards become disabled states** — if `[balance < price]` blocks an action, the button should be disabled (or show a warning), not just throw an error after tap

---

## How to Design a Use Case Diagram (step by step)

### Step 1 — List the actors
Ask: *who or what interacts with the system?*
- Human roles (Student, Staff, Admin, Kitchen)
- External systems only if they push/pull data automatically (rare at this stage)
- Don't list a role that never directly triggers an action — that's not an actor

### Step 2 — List the goals per actor
For each actor, ask: *what do they come to the system to accomplish?*
- One goal = one use case (verb + noun, e.g. "Place Pre-Order", not "Order screen")
- Skip UI details — a use case is a goal, not a button

### Step 3 — Find shared/repeated steps → `<<include>>`
If two or more use cases always perform the same sub-step, pull it out.
- Example: Place Pre-Order and Create Walk-In Order both always "Deduct Balance" and "Append Transaction" → make those separate use cases connected with `<<include>>`

### Step 4 — Find optional/conditional steps → `<<extend>>`
If a use case *sometimes* adds extra behavior, model it as extend, not include.
- Example: "Show QR" only matters if the student picked pre-order pickup — it extends "Confirm Pre-Order", it doesn't always happen

### Step 5 — Draw the boundary
- One big rectangle labeled with the system name
- All use cases go inside; all actors go outside
- Actors connect to boundary with a solid line, not directly to each other

### Step 6 — Sanity checklist before calling it done
- [ ] Every actor has at least one use case
- [ ] Every use case has at least one actor (no orphans)
- [ ] No two ovals describe the same goal with different words
- [ ] `<<include>>` only used for mandatory, always-happens steps
- [ ] `<<extend>>` only used for optional steps
- [ ] Verbs are active and specific ("Cancel Pre-Order", not "Manage Orders")

---

## How to Design an ERD (Entity-Relationship Diagram)

### Step 1 — List the entities (nouns)
Pull nouns straight out of your use cases and pain points.
- Example from lunch card system: Student, Card, Account, Transaction, MenuItem, Timeslot, DailyMenu, Order, Staff
- Rule of thumb: if it needs to be looked up, filtered, or has its own lifecycle → it's an entity

### Step 2 — List attributes per entity
For each entity, ask: *what facts do I need to store about one instance of it?*
- Always include a primary key (`id`)
- Keep attributes atomic — don't store "full address" as one field if you need city separately later
- Mark which are required (`not null`) vs optional

### Step 3 — Identify relationships between entities
For every pair of related entities, ask three questions:
1. **Does A relate to B?** (draw a line if yes)
2. **What's the cardinality?** — one-to-one, one-to-many, many-to-many
3. **Is the relationship mandatory or optional?** — must every Order have a Student, or can it be null?

| Cardinality | Symbol (crow's foot) | Example |
|---|---|---|
| One to one | `——\|\|——\|\|` | Student — Account (1 student has exactly 1 account) |
| One to many | `——\|\|———\|<` | Account — Transaction (1 account has many transactions) |
| Many to many | `——\|<———\|<` | Needs a junction table (e.g. DailyMenu links MenuItem × Timeslot) |

### Step 4 — Resolve many-to-many with a junction table
A many-to-many relationship cannot be drawn directly in a relational schema — it always needs a bridge table holding foreign keys to both sides plus any relationship-specific attributes.
- Example: `DailyMenu` bridges `MenuItem` and `Timeslot`, and also carries `available_quantity` — an attribute that belongs to the *relationship*, not to either entity alone

### Step 5 — Mark keys
- **PK (Primary Key)** — uniquely identifies a row
- **FK (Foreign Key)** — points to another entity's PK, this is what draws the connecting line
- **Unique constraint** — not a key, but enforces "only one per X" (e.g. one active card per student, enforced separately if not a strict DB constraint)

### Step 6 — Encode business rules as notes/constraints
Anything that isn't a plain data shape goes in a note attached to the entity or relationship.
- Example: `balance >= 0` on Account
- Example: `available_quantity >= 0, decremented atomically` on DailyMenu
- Example: `transactions is append-only, no UPDATE/DELETE` on Transaction

### Step 7 — Sanity checklist before calling it done
- [ ] Every entity has a primary key
- [ ] Every relationship has cardinality marked on both ends
- [ ] Every many-to-many is resolved into a junction table
- [ ] Business rules that aren't expressible as columns are written as notes
- [ ] No duplicate entities representing the same real-world thing under different names

---

## Use Case Diagram vs ERD — how they connect

Both come from the same source material (your requirements/pain points) but answer different questions:

| | Use Case Diagram | ERD |
|---|---|---|
| Answers | What can actors *do*? | What data must the system *store*? |
| Built from | Verbs (goals, actions) | Nouns (things, records) |
| Actors become | Stick figures | Usually not modeled directly (unless they need their own table, e.g. Student, Staff) |
| Drives | Screens, flows, sequence diagrams | Database schema, class diagram |

**Practical order to design in:**
1. Use Case Diagram first — figure out what the system needs to let people *do*
2. Pull nouns out of those use cases → seed your ERD entity list
3. Pull verbs/state changes out of those use cases → these become methods/transactions in the ERD's business rules (e.g. "Place Pre-Order" → touches Account, Order, DailyMenu, Transaction)
4. If the ERD reveals a missing concept (e.g. you need a `Transaction` table to log every balance change), go back and check whether the use case diagram should show it as an included system use case — it usually should

---
