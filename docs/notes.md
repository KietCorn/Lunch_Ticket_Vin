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
