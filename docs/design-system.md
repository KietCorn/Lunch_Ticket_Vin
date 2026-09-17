# Design System — Lunch Ticket System

Source of truth for colors, spacing, typography, status-label wording, and shared components across all three frontend apps (Student, Staff & Kitchen, Admin — see `docs/architecture/c4/container.md`). Owned by the `ui-consistency` skill (`.claude/skills/ui-consistency.md`) — read that first before creating or editing any screen, component, or token.

This file was reconstructed from the tokens/components already implemented in `docs/wireframes/shared/tokens.css` and `docs/wireframes/shared/components.css` (the wireframes were built following this file's intended structure, but the file itself had gone missing from the repo). Every value below is taken directly from those two CSS files — nothing here is newly invented; where the wireframes hadn't picked a value yet, it's left unfilled rather than guessed.

Only Student and Staff & Kitchen wireframes exist so far (Admin is deferred per `CLAUDE.md`) — sections below reflect that; extend them, don't duplicate, when Admin screens are designed.

---

## Colors

| Token | Hex | Usage |
|---|---|---|
| `--color-bg` | `#ffffff` | Page/card background |
| `--color-surface` | `#f5f5f5` | Secondary surface (muted chips, thumbnails, empty-state icons) |
| `--color-border` | `#d9d9d9` | Default border/divider |
| `--color-text` | `#1a1a1a` | Primary text |
| `--color-text-muted` | `#6b6b6b` | Secondary/caption text |
| `--color-primary` | `#2563eb` | Primary actions, links, active nav state |
| `--color-primary-dark` | `#1d4ed8` | Primary gradient/hover accent |
| `--color-primary-soft` | `#dbeafe` | Primary tint background (tags, icon badges) |
| `--color-success` | `#15803d` | Success/ready/active state text |
| `--color-success-soft` | `#dcfce7` | Success state background |
| `--color-warning` | `#b45309` | Warning/out-of-stock state text |
| `--color-warning-soft` / `--color-warning-bg` | `#fef3c7` | Warning state background, alert banners |
| `--color-danger` | `#b91c1c` | Danger/cancelled/locked/insufficient-balance state text |
| `--color-danger-soft` | `#fee2e2` | Danger state background |
| `--color-on-primary` | `#ffffff` | Text/icons on a primary-colored fill |
| `--color-page-bg` | `#eef1f6` | Outer page/stage background (behind device/browser frame) |

## Spacing scale

| Token | Value |
|---|---|
| `--space-xs` | 4px |
| `--space-sm` | 8px |
| `--space-md` | 16px |
| `--space-lg` | 24px |
| `--space-xl` | 32px |

## Corner radius scale

| Token | Value | Usage |
|---|---|---|
| `--radius-sm` | 8px | Inputs, small placeholders |
| `--radius-md` | 14px | Cards, icon badges |
| `--radius-lg` | 20px | Hero panels, browser frame |
| `--radius-pill` | 999px | Buttons, status pills, tags |

## Typography

| Token | Size | Weight | Usage |
|---|---|---|---|
| `--text-heading-size` / `--text-heading-weight` | 20px | 600 | Screen/section headings (`h1`) |
| `--text-body-size` / `--text-body-weight` | 14px | 400 | Default body text, buttons, inputs |
| `--text-caption-size` / `--text-caption-weight` | 12px | 400 | Captions, labels, nav breadcrumbs, status pills |

Font family: `--font-family: -apple-system, "Segoe UI", Roboto, sans-serif` (system font stack — no custom webfont, keeps wireframes dependency-free).

## Status labels

One row per order/card/balance/stock state actually used across the wireframes. Display text and color mapping must match exactly across all apps — a "Ready" order looks the same in the Student app and the Kitchen display.

| State | Display text | Style class | Color |
|---|---|---|---|
| Order — pending | Pending | `.status-pending` | Muted (`--color-text-muted` on `--color-surface`) |
| Order — ready | Ready | `.status-ready` | Success |
| Order — delivered / picked up | Delivered | `.status-delivered` | Muted |
| Order — cancelled | Cancelled | `.status-cancelled` | Danger |
| QR/token — already used | Already used | `.status-cancelled` | Danger |
| QR/token — not found | Token not found | `.status-cancelled` | Danger |
| QR/token — already delivered | Already delivered | `.status-cancelled` | Danger |
| Card — active | Active | `.status-active` | Success |
| Card — locked | Locked (or "Locked (old card)" in context) | `.status-locked` | Danger |
| Balance — sufficient | Sufficient balance | `.status-balance-ok` | Success |
| Balance — insufficient | Insufficient balance | `.status-insufficient-balance` | Danger |
| Menu item — out of stock | Out of stock | `.status-out-of-stock` | Warning |

Non-negotiable principle #4 (`CLAUDE.md`): "insufficient balance" and "out of stock" must always render as visually distinct states — enforced here by `status-insufficient-balance` (danger/red) vs. `status-out-of-stock` (warning/amber) never sharing a color.

## Shared components

| Component | Class(es) | Purpose | Used by |
|---|---|---|---|
| Page shell | `.page` | Max-width content column, consistent outer padding | Student, Staff & Kitchen |
| Nav bar | `.nav-bar`, `.breadcrumb`, `.use-case`, `.leads-to` | Screen title + "Reached from" breadcrumb + "Leads to" forward links + use-case tag (IA traceability, per `docs/screens_hierarchy/ia-screens.md`) | Student, Staff & Kitchen |
| Card | `.card`, `.card-elevated` | Default content container; `-elevated` variant for flagship/screenshot screens | Student, Staff & Kitchen |
| Button | `.btn`, `.btn-primary` | Pill-shaped action buttons, default and primary variants | Student, Staff & Kitchen |
| Status pill | `.status-pill` + state modifier (see Status labels above) | Compact colored state chip | Student, Staff & Kitchen |
| Tag | `.tag`, `.tag-primary`, `.tag-success`, `.tag-warning` | Small colored label chip (categories, "Popular", ratings) | Student |
| Empty state | `.empty-state`, `.empty-icon` | Dashed-border placeholder for empty lists | Student, Staff & Kitchen |
| TBD placeholder | `.tbd-placeholder` | Labeled dashed-border placeholder for any business-rule value not yet decided — never a guessed number. (As of this pass, no `[TBD]` values remain per `docs/investment/requirements.md` "Resolved business rules" — kept as a component for any future undecided field.) | Student, Staff & Kitchen |
| Alert banner | `.alert-banner`, `.dismiss` | Stock-Out Alert / Suspicious Usage Alert overlay partial (per `docs/wireframes/README.md` "Known simplifications") | Student, Staff & Kitchen |
| Form field | `label`, `input`, `select`, `.field` | Standard form input styling | Staff & Kitchen |
| App bottom nav (text) | `.app-nav` | Text-link bottom nav (fallback/legacy — superseded by `.tab-bar` on phone-frame screens) | Student |
| Index list | `.index-list` | Vertical list wrapper on `index.html` screen-directory pages | Student, Staff & Kitchen |
| Device frame (mobile) | `.device-frame`, `.device-statusbar`, `.device-screen`, `.device-content`, `.tab-bar` | Phone chrome mockup wrapper + bottom tab bar | Student |
| Browser frame (desktop) | `.browser-frame`, `.browser-toolbar`, `.browser-dots`, `.browser-url`, `.top-nav`, `.browser-content` | Browser chrome mockup wrapper + desktop app header nav | Staff & Kitchen (and Student screens rendered as laptop screenshots) |
| Dev note | `.dev-note`, `.dev-note.wide` | IA traceability annotation box (use case, "Reached from", "Leads to"), kept visually separate from the mock UI | Student, Staff & Kitchen |
| Layout helpers | `.row`, `.row-between`, `.cols`, `.grid`, `.panel-narrow` | Generic flex/grid layout utilities reused across screen mockups | Student, Staff & Kitchen |
| Thumb / avatar / icon | `.thumb`, `.avatar`, `.icon-circle` (`.success`/`.danger`), `.icon-badge` (`.muted`) | Small circular/rounded image or icon placeholders | Student, Staff & Kitchen |
| Chevron / list row | `.chevron`, `.list-row` | Disclosure indicator + clickable list-row wrapper | Student, Staff & Kitchen |
| Hero panel | `.hero` | Gradient highlight panel (flagship screens only) | Student |
| Stock bar | `.stock-bar`, `.stock-bar.low` | Menu-item remaining-quantity indicator | Student |
| QR frame | `.qr-frame`, `.corner` (`.tl`/`.tr`/`.bl`/`.br`) | Scanner-corner frame around a QR code mock | Student |
| Badge count | `.badge-count` | Small numeric badge (e.g. queue counts) | Staff & Kitchen |
| Kanban header / status accent | `.kanban-header`, `.status-accent` (`.pending`/`.ready`) | Left-border accent + header for queue-column layouts | Staff & Kitchen |
| Data table | `.data-table`, `tr.clickable` | Sortable/row-clickable table for lists (student lookup, reports) | Staff & Kitchen |

## Notes

- `docs/wireframes/shared/tokens.css` and `docs/wireframes/shared/components.css` are the CSS implementation of this file — the two must stay in sync; a token or component added to one belongs in both.
- Admin app has no tokens/components of its own yet — when Admin wireframes are designed, reuse everything above before adding anything new (Admin's screens are all desktop/browser-frame, same as Staff & Kitchen).
- All previously-`[TBD]` business-rule values referenced by status/placeholder text (QR expiry, cancellation refund cutoff) are resolved — see `docs/investment/requirements.md` "Resolved business rules." The `.tbd-placeholder` component itself is kept for any future undecided field, not removed.
