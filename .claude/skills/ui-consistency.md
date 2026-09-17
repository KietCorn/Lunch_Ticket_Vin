---
name: ui-consistency
description: Keep screens, components, and design tokens consistent across the Student, Staff & Kitchen, and Admin frontend apps. Use whenever creating or editing a screen/page, a UI component, a wireframe, or any color/spacing/typography/status-label value.
---

# UI Consistency

This is a school project with three separate frontend apps (Student, Staff & Kitchen, Admin — see `docs/architecture/c4/container.md`). Without a shared source of truth they will drift. `docs/design-system.md` is that source of truth. It does not exist yet — the first time this skill is used, create it.

## Before touching any screen or component

1. Open `docs/design-system.md`. If it doesn't exist, create it with these sections and fill only what's actually needed for the screen at hand (don't pre-populate hypothetical tokens):
   - **Colors** (name → hex, e.g. `primary`, `danger`, `success`, `warning`, `neutral-bg`)
   - **Spacing scale** (e.g. 4/8/12/16/24/32px, named `xs/sm/md/lg/xl`)
   - **Typography** (font sizes/weights for heading/body/caption)
   - **Status labels** — one row per order/card status (`pending`, `confirmed`, `ready`, `picked-up`, `cancelled`) with exact display text and color mapping
   - **Shared components** — name, purpose, which app(s) use it
2. Open `docs/screens_hierarchy/ia-screens.md` for the screen's navigation context (what it's reached from, what it leads to) — this skill is about visual/token consistency, not navigation, but don't contradict that structure.
3. Check `docs/structure/frontend.md` for where the component/page belongs and its expected name (PascalCase files, one `services/` file per backend component).

## Rules

- Never hardcode a color, spacing value, font size, or status label string directly in a component. Reference (or add to) `docs/design-system.md` first.
- If an existing color/spacing/component in `docs/design-system.md` already covers the need, reuse it — don't add a near-duplicate ("primary-blue" next to an existing "primary").
- Adding a genuinely new token or shared component is fine — add it to `docs/design-system.md` in the same change, with a one-line reason, rather than leaving it undocumented.
- Status/state wording and colors must match `docs/design-system.md` exactly across all three apps — a "ready" order must look the same in the Student app and the Kitchen display.
- Fields still marked `[TBD]` in `docs/investment/requirements.md` (refund cutoff, fraud thresholds, QR expiry) must render as a labeled placeholder, never a guessed number.

## After finishing a screen or component

Report briefly: which existing tokens/components were reused, and what (if anything) was newly added to `docs/design-system.md` and why.
