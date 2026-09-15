# Phase 8 — Themed Titles & Themes

> Status: **planning only**. Companion to `spec.md` and `tasks.md`.

## 1. Goal

Replace the flat `level = 42` display with a themed **title**
(`"Warrior"`, `"Mage"`, `"War God"`). Themes are picked per-user
and change the label without changing the level tiers.

## 2. Scope

**In scope**
- `themes` + `level_title_tiers` schemas.
- Seed two themes: **Medieval** (default) and **Sci-Fi**.
- `Xpeak.Titles.resolve/2` — pure lookup returning current title
  and the next-tier target.
- `PATCH /me/theme` — user picks their active theme.
- `GET /me` exposes `current_title` + `next_title` + `progress_to_next`.
- Mobile: theme picker in profile settings; title rendered on
  profile card.

**Out of scope**
- Custom user-authored themes (deferred; nice-to-have).
- Illustrated level "forms" — Phase 22.
- Editable tier ranges — Phase 9.

## 3. Approach

- Tiers are inclusive ranges: `{min, max, title}`. `max = nil` means
  "the terminal tier".
- Themes ship as data seeds. Adding a theme = adding a migration
  that inserts a `themes` row + its `level_title_tiers`.
- No app code changes to ship a new theme.

## 4. Artifacts

- `priv/repo/migrations/*_create_themes.exs`.
- `priv/repo/migrations/*_create_level_title_tiers.exs`.
- `priv/repo/migrations/*_add_theme_id_to_users.exs`.
- `lib/xpeak/titles.ex` — context.
- `lib/xpeak/titles/theme.ex`, `lib/xpeak/titles/tier.ex`.
- `lib/xpeak_web/controllers/me_controller.ex` — new `PATCH /me/theme`.
- Mobile: profile settings screen with theme picker.

## 5. Dependencies

- Blocked by: Phase 3 (users), Phase 4 (levels).
- Blocks: Phase 22 (forms use theme).

## 6. Open questions

1. **Default theme** — Medieval or Sci-Fi first?
2. **Tier bands** — exactly Apprentice 1–9, Warrior 10–24, Knight
   25–49, Mage 50–99, War God 100+? Adjust?
3. **Sci-Fi labels** — Rookie / Trooper / Officer / Commander /
   Overlord? Bikeshed here or leave for content review?

## 7. Success criteria

- `GET /me` returns `current_title.name` matching the user's level +
  active theme.
- `PATCH /me/theme` with a valid theme id switches labels
  immediately.
- User with 3 check-ins on the default Medieval theme sees
  `"Apprentice"`; the same user after switching to Sci-Fi sees
  `"Rookie"`.
- CI green.
