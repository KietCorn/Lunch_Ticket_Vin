# How to Design a Sequence Diagram (self-guided)

Fills the gap flagged in `docs/architecture/README.md` / Arc42 §6 ("no sequence diagrams exist yet"). This is a **method + cheat sheet**, not a finished diagram set. Work through it use case by use case and build your own `.mmd` blocks. One fully worked example is included so you can see the pattern before doing the rest yourself.

Adapted from an earlier prototype (`main` branch) — the method and Mermaid syntax are stack-agnostic and unchanged; participant naming below is generalized (backend language/framework for this branch is ASP.NET Core / C#, not the FastAPI prototype this guide originally assumed) and UC numbers are updated to match `docs/investment/usecase.md` (UC1–UC28).

---

## 1. What a sequence diagram shows

*In what order do participants exchange messages to complete one use case?*

It is the **behavioral zoom-in** on a single flow you already defined in `docs/investment/usecase.md`. If the use case's Main Flow is numbered steps in prose, the sequence diagram is that same list turned into arrows between participants.

---

## 2. Mermaid sequence syntax — cheat sheet

```mermaid
sequenceDiagram
    participant A as Actor Name
    participant B as Component Name

    A->>B: synchronous call (waits for response)
    activate B
    B-->>A: return value
    deactivate B

    A-)B: async / fire-and-forget (open arrowhead)

    Note over A,B: a note spanning both lifelines
    Note right of A: a note on one lifeline

    alt condition true
        A->>B: happy path message
    else condition false
        A->>B: alternate path message
    end

    opt optional step
        A->>B: only happens sometimes
    end

    loop every N seconds
        A->>B: repeated message
    end
```

| Syntax | Meaning |
|---|---|
| `participant X as Label` | Declares a lifeline. Order left-to-right = declaration order. |
| `->>` | Solid arrow, synchronous call |
| `-->>` | Dashed arrow, return message |
| `-)` | Open arrow, async / fire-and-forget |
| `activate` / `deactivate` | Draws the thin "busy" bar on a lifeline (optional but recommended for anything with a return) |
| `alt / else / end` | If/else branching — use for your use case's Alt Flow |
| `opt / end` | One optional branch, no else |
| `loop / end` | Repetition (e.g. SSE polling) |
| `Note over A,B: text` | Annotation, good for stating a business rule inline |

---

## 3. The 5-step method: use case → sequence diagram

### Step 1 — Pick the participants
Look at the use case's actor plus every system component it touches. For this project, the recurring participants are:

| Participant | Represents |
|---|---|
| The actor (Student / Staff / Kitchen / Admin) | The human triggering the flow |
| `App` | The relevant frontend app (Student, Staff & Kitchen, or Admin — see `docs/structure/frontend.md`) |
| `API` | The backend API layer — specific controller/service naming isn't finalized yet, keep this generic until it is |
| `DB` | The persistence layer (SQLite; ORM/data-access approach not decided yet) |
| (sometimes) `SSE Stream` | The kitchen live-update channel |

Don't add a participant that doesn't send or receive at least one message.

### Step 2 — Walk the Main Flow numbered steps
Open the use case in `docs/investment/usecase.md` / `docs/investment/requirements.md`. Its **Main Flow** is already a numbered list — that list *is* your message sequence in prose form. Convert each numbered step into one arrow.

### Step 3 — Add the Alt Flow as `alt`/`opt`
Every **Alt Flow** entry in the use case becomes a branch. Ask: does this alternate path always get evaluated (→ `alt/else`), or does it only sometimes trigger (→ `opt`)?

### Step 4 — Add `activate`/`deactivate` around anything that returns a value
If a message expects a response, wrap it. This keeps the diagram readable when multiple calls happen back-to-back.

### Step 5 — Cross-check against `<<include>>` relationships
If the use case includes a system use case (UC25 Append Transaction Record, UC26 Enforce Balance, UC28 Available Quantity — see the relationship table in `docs/investment/usecase.md`), those need to show up as real messages/notes in the diagram — they are not optional just because they're "internal."

---

## 4. Worked example — UC1 Login

Source (from `docs/investment/usecase.md` / `docs/investment/requirements.md`):
> 1. Actor opens the app → enters credentials. 2. System validates credentials. 3. System returns session token; app shows home screen.
> Alt: 2a. Invalid credentials → system shows error; actor retries.

```mermaid
sequenceDiagram
    participant Actor
    participant App
    participant API
    participant DB

    Actor->>App: Enter credentials
    App->>API: POST /auth/login
    activate API
    API->>DB: Look up account by username
    activate DB
    DB-->>API: account row (or none)
    deactivate DB

    alt credentials valid
        API-->>App: 200 OK + session token
        App-->>Actor: Show home screen
    else credentials invalid
        API-->>App: 401 Unauthorized
        App-->>Actor: Show error, allow retry
    end
    deactivate API
```

Notice the mapping:
- Numbered step 1 → `Actor->>App` and `App->>API`
- Step 2 ("System validates") → `API->>DB` / `DB-->>API`
- Step 3 → the `alt` block's success branch
- Alt Flow 2a → the `else` branch

---

## 5. Your turn — use cases worth diagramming, with participants pre-picked

Don't just do one. A sequence diagram set is usually built for the flows that are **complex, multi-actor, or state-changing** — not trivial CRUD. Below is a shortlist from `docs/investment/usecase.md`, in priority order, each with participants named for you but the arrows left for you to derive using the method above.

### UC6 — Place Pre-Order (do this one first — it's the most important flow in the system)
Participants: `Student, App, API, DB, SSE Stream`
Hint: this one must show UC26 (balance check) and UC28 (quantity decrement) as real steps, and the alt flow has **two** failure branches (insufficient balance / sold out) — use `alt/else if/else`.

### UC7 — Cancel Pre-Order
Participants: `Student, App, API, DB`
Hint: mirror of UC6 but refund direction — reuse the same shape, flip the sign. The cancellation cutoff itself is still `[TBD]` (`docs/investment/requirements.md`) — don't hardcode a specific cutoff time in the diagram, note it as a placeholder.

### UC8 — Confirm Pre-Order (QR Scan)
Participants: `Staff, POS App, API, DB`
Hint: the alt flow branches on **token state**, not balance — expired token, not-found token, already-delivered order are three distinct outcomes. QR expiry rule is also still `[TBD]`.

### UC9 — Create Walk-in Order
Participants: `Staff, POS App, API, DB, SSE Stream`
Hint: same balance/quantity checks as UC6, but the order needs `placed_by_staff` set and it must show up in a **separate** queue on the SSE stream (pre-order and walk-in queues are never merged — non-negotiable principle #3) — say so explicitly with a `Note`.

### UC19 — Mark Order as Ready (good one to learn `loop`/SSE push pattern)
Participants: `Kitchen Staff, Kitchen Display, API, DB, SSE Stream`
Hint: this is where you show the SSE broadcast — one action from Kitchen fans out to every connected client. You can show this as one `API-)SSE Stream: broadcast` async arrow.

---

## 6. Common mistakes checklist

- [ ] Did you name a participant that never sends or receives a message? Remove it.
- [ ] Did every arrow that expects a response get a return arrow (`-->>`)?
- [ ] Did you turn every **Alt Flow** row from the use case table into an `alt`/`opt` block — not just describe it in a note?
- [ ] Did you show `<<include>>` system use cases (balance check, transaction append, quantity decrement) as actual messages, not skip them because they're "internal"?
- [ ] Did you avoid hardcoding a `[TBD]` business rule value (cutoff times, thresholds) as if it were decided?
