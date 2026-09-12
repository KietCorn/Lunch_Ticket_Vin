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
