# C4 Model — Level 2: Container

Zooms into the "Lunch Ticket System" box from `context.md`. Actors are the
same four from Level 1; their arrows now point at the specific container
they use instead of the system as a whole.

Frontend is split into four containers (Student, Staff, Kitchen, Admin)
rather than one, because each is an independently servable static app with
a distinct interaction pattern — only Kitchen needs a persistent SSE
connection. SSE itself is **not** a container: `GET /api/kitchen/stream` is
just another route inside the same FastAPI process, sharing code and a DB
session with every other endpoint, so it has no independent deploy
lifecycle. It's shown as the protocol on the Backend API → Kitchen Display
arrow instead.
