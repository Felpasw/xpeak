# Phase 22 — Level "Evolution Forms"

> Status: **planning only**.

> **Stack note:** artifacts in this file (module names, package names,
> code samples) were originally written for Elixir/Phoenix. The
> project switched to C# + ASP.NET Core 9 in Phase 2. See
> `specs/roadmap.md` → "Stack migration note" for the mapping table.
> Concepts and endpoint contracts still hold; concrete artifacts get
> rewritten when this phase is picked up.


## 1. Goal

Give each themed title tier a **visual form**: an illustration
(static or animated) that represents the user's current "stage" —
Aprendiz → Guerreiro → Cavaleiro → Mago → Deus da Guerra. The form
follows the active theme (Phase 8) and swaps automatically as the
user levels up.

## 2. Scope

**In scope**
- `theme_forms` schema linking a `level_title_tier` to an artwork
  asset.
- Seed the two default themes (Medieval, Sci-Fi) with 5 forms each.
- Animated forms (GIF/APNG/animated WebP) unlock at higher tiers.
- `GET /me` includes `current_form` (asset URL) and `next_form`
  preview.
- Mobile: form rendered prominently on profile card; a "gallery"
  screen shows all forms in the active theme with locked/unlocked
  state.
- Reuse Phase 10 rendering pipeline for animated assets.

**Out of scope**
- User-uploaded custom form packs (deferred, moderation-heavy).
- Non-theme-tied form packs (e.g., seasonal).
- Trading / gifting.

## 3. Approach

- `theme_forms`: `theme_id, tier_id, artwork_storage_key,
  mime_type, is_animated`.
- Resolver: `Xpeak.Titles.resolve_form(user)` returns the form for
  the user's current tier in the active theme.
- If a theme lacks forms for a tier, fall back to the previous tier
  or a neutral system placeholder.

## 4. Artifacts

- Migration: `create_theme_forms.exs`.
- Seeds: 10 form rows (5 tiers × 2 themes).
- `lib/xpeak/titles/form.ex` — schema.
- `Xpeak.Titles.resolve_form/1` — pure lookup.
- Endpoint additions: `GET /me` gains `current_form` +
  `next_form`; `GET /themes/:slug/forms` for the gallery.
- Mobile: profile form illustration; forms gallery screen.

## 5. Dependencies

- Blocked by: Phases 8 (themes/tiers), 10 (cosmetic rendering
  pipeline), 6 (media storage).
- Blocks: —

## 6. Open questions

1. **Animated form threshold** — which tiers get animation? Only
   the top tier? Top two?
2. **Locked form preview** — silhouetted with a "?" overlay, or fully
   hidden until unlocked?
3. **Form transition animation on level up** — subtle fade or big
   celebratory swap?
4. **Third theme** — ship a third theme in this phase (e.g., Anime
   or Cyberpunk) or wait?

## 7. Success criteria

- Level 1 user with default theme → shows Apprentice form artwork
  on profile.
- Level up crossing a tier boundary swaps the form.
- Switching themes swaps the form pack.
- Gallery screen shows all 5 forms with locked/unlocked state
  based on current level.
- Animated forms play smoothly on scroll.
- CI green.
