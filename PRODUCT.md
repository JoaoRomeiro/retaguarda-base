# Product

## Register

product

## Users

This repository is a **starter base**, not a shipped product. Its users are the **developers** who
clone it to start a new system, and — once a derived project exists — the **admins and managers**
who operate the multi-plant back-office it provides.

- **Admins** — configure the installation: sites (plants), users, roles, and the user↔site links.
- **Managers** — operational oversight within their active plant.
- **End users of the derived project** — undefined here. Each project defines its own audience; that
  decision drives the one design rule below that is audience-dependent (contrast).

Context of use assumed by the inherited design: on-premise, repeat daily use, up to a few dozen
simultaneous web users per site. One centralized install serves multiple plants (Sites); every
domain query is scoped to the user's active Site.

## Product Purpose

Retaguarda Base ships the plumbing so a new project starts at the interesting part: authentication
(cookie for web, JWT for the API), multi-site isolation, user/role/plant management, localization,
auditing with soft delete, Excel/PDF export, security headers, structured logging, and Docker
packaging — plus the docs that keep all of it consistent.

The base itself has **no business flow**: the web is a login + welcome shell over the platform
registrations, and the API exposes authentication. The value is a clean, secure, localized,
multi-tenant foundation that a domain extends without reworking the plumbing.

> The design language below was shaped by the original warehouse-operator profile and is **retained
> as the design-system baseline**. A derived project confirms or adjusts its own end-user profile;
> until it does, these principles govern any UI added.

## Brand Personality

**Sturdy, legible, unambiguous.** Industrial-grade confidence through clarity, not polish. High
contrast, obvious affordances, nothing subtle. The tone is plain and direct (Portuguese UI, via
`IStringLocalizer` + `.resx`, never hardcoded). It should read instantly to a tired operator on a bad
monitor and feel trustworthy to a manager.

## Anti-references

- **Trendy SaaS / startup.** No gradient heroes, glassmorphism, pastel marketing aesthetics, or oversized whitespace. This is a tool, not a landing page.
- **Elegant / minimal grays.** No light-gray "elegance": thin borders, muted body text, low-contrast surfaces. Explicitly ruled out for the inherited operator profile in `docs/padrao-ui.md` §6 — when choosing between an elegant-lighter and an obvious-darker color/border/shadow, choose the darker.
- **Consumer mobile app.** No playful illustrations, big rounded cards, emoji, or animation-as-decoration. Stays desktop-dense and businesslike.
- **Generic Bootstrap default.** No rainbow-Bootstrap (default blue/green/red buttons everywhere, no tokens, no restraint). Bootstrap 5 is the base, but re-mapped to the contained theme tokens.

## Design Principles

1. **Legibility over elegance.** The inherited user profile (older operators, irregular light, cheap screens, glare) is the constraint that wins every tie. Darker, more obvious, higher-contrast beats subtle. Status is never carried by color alone: always color + icon + text. A project with a different audience may revisit this — explicitly, and recorded in `docs/padrao-ui.md`.
2. **One shell, themed once.** The app shell lives in a few layout files; visual identity lives in a single `theme.css` of design tokens. Rebrand = edit one file. Nothing visual repeats without becoming a component (Partial, ViewComponent, or Tag Helper).
3. **Server-side first, minimal frontend.** Razor renders; JavaScript is vanilla and scoped to small local UX. No JS framework, no build step, no CDN. Every dependency is a conscious decision.
4. **Consistency across CRUD screens.** Registrations follow one template (`page-header` + `.card` body + `.card-footer` actions), with the Site CRUD as reference. A new screen should look and behave like the others; predictability is a feature for repeat daily users.
5. **Localized from day one.** Every user-visible string is localized; the structure accepts new languages without code changes even though only pt-BR ships now.

## Accessibility & Inclusion

Target: WCAG AA minimum, prefer AAA where reachable, with extra rigor driven by the inherited user
profile (`docs/padrao-ui.md` §9).

- **Text contrast** ≥ 4.5:1 (prefer 7:1). **Component borders** (input, button, card, badge) ≥ 3:1; when in doubt between 3:1 and 5:1, choose 5:1.
- **Focus** visible, thick (2–3px), high-contrast against both background and the focused element. Never remove the default outline.
- **Status** (success/warning/danger) always shown as color + icon + text (color blindness + cheap monitors).
- **Disabled** shown with a distinct solid color, never opacity alone.
- **Tap targets** minimum 44×44px. Semantic HTML throughout (`<button>`, `<header>`, `<main>`, `<nav>`, ordered headings), `<label>` on every field, skip-to-main-content link, full keyboard navigation.
- Base font fixed at 14px (`rem`-scaled, AdminLTE density) per the 2026-05-25 product-owner decision; contrast thresholds were left unchanged. Reevaluate font size if field reading proves insufficient with real users.
