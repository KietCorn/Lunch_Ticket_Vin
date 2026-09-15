# C4 Model — Level 1: System Context

Student Lunch Card System. Actors sourced from `docs/usecase.md`.

Note: `usecase.md` lists "System" as a secondary actor (balance rules, QR
expiry, queue separation, transaction integrity). At this level that is
internal behavior of the system itself, not an external actor — so it is
absorbed into the "Lunch Ticket System" box rather than drawn separately.

```mermaid
flowchart TB
    Student([Student])
    Staff([Counter Staff])
    Kitchen([Kitchen])
    Admin([Admin])

    System[["Lunch Ticket System"]]

    Student -- "places pre-order / pays" --> System
    Student -- "view QR / status& balance" --> System

    Staff -- "processes orders & payments" --> System
    Staff -- "manages student cards" --> System

    Kitchen -- "views live order queue / marks ready" --> System

    Admin -- "configures menu, timeslots & accounts" --> System
    Admin -- "views transaction reports" --> System
```
