---
name: lunchcard-context
description: Full project context for the student lunch card management system. Load at the start of any session touching this project — pain points, design principles, settled assumptions, actor model, and decision-making process.
---

## Project

Student lunch card management system. Replaces a manual, paper-based cafeteria process. Short-term school project.

### Problem (as-is)

Students queue to buy food. Those who pre-purchased a lunch card still wait in the same line — staff manually tick items on paper. No priority for people who already paid. Losing the card means losing the remaining balance because money is tied to the physical card.

---

## Pain Points — Priority Order

When design or implementation decisions conflict, resolve them in this order:

1. **Financial risk** — losing the card = losing money *(most critical)*
2. **Priority & fairness** — students who paid in advance get no queue advantage today
3. **Processing speed** — manual ticking creates bottlenecks
4. **Kitchen operations** — no real-time demand data *(secondary, not the main focus)*

---

## Non-Negotiable Design Principles

These may not be overridden by any tech, UX, or cost argument:

- **Money is decoupled from the physical card.** Balance lives on the student account, not the card chip. Losing a card never means losing money.
- **Lost card flow:** lock old card → issue new card → balance carries over intact. No exceptions.
- **Pre-order and walk-in queues are always kept separate.** Never merge into a single queue, even for simplicity.

---

## Settled Assumptions — Do Not Re-Open

- 100% of students have a smartphone and stable internet access.
- Card-lending risk during idle periods is accepted as minor — no complex anti-fraud needed at this stage.
- Why the current system hasn't been automated is **unknown** — do not assume a reason; this requires real-world investigation before proposing solutions that might repeat the same blockers.

---

## Actors & Module Map

| Actor | Interface |
|---|---|
| Student | Mobile web (responsive) |
| Counter staff | POS web interface |
| Kitchen | Read-only display (auto-refresh) |
| Admin | Back-office web interface |

**Modules and dependency order:**

```
Identity & Auth  ──┐
Menu & Availability─┤──▶  Order & Priority Queue  ──▶  Kitchen Display
                    │              │
                    └──────────────▼
                         Transaction & Balance   ← build this first;
                                                   all other modules depend on it
```

---

## Decision-Making Process

Apply this whenever a significant design or stack decision is being made — especially during tech stack selection, before `lunchcard-coding` has concrete content.

1. **Ask about real-world constraints before proposing a solution.** Relevant questions: budget, existing infrastructure, device availability at the cafeteria, who will maintain the system. Do not assume the constraints are the same as the old system's.

2. **Always present ≥ 2 options with explicit trade-offs.** For each option state: complexity, cost/effort, how well it addresses the prioritized pain points, and risks. Never recommend a single option without alternatives.

3. **Check against the old system's known blockers.** The reason the current system isn't automated is unknown. Any proposed solution must not inadvertently reproduce the same barrier (e.g., requiring hardware the school can't afford, requiring network infrastructure that doesn't exist). Flag this risk explicitly if the blocker is still unknown.

4. **Map decisions back to the pain point priority.** If a choice improves pain point #3 (speed) but worsens pain point #1 (financial risk), it must be rejected or reworked — the priority order is binding.

---

## Current Status

| Area | Status |
|---|---|
| Brainstorming | Complete |
| Business requirements | Complete (4 actors, use cases, business rules defined) |
| UI/UX — student flow | In progress (`wireframes/student/index.html`) |
| UI/UX — staff / kitchen / admin | Not started |
| Tech stack | **Not decided** |
| Database schema | Not started |
| Application code | Not started |
