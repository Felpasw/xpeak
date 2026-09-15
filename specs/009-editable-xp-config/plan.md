# Phase 9 — Editable XP & Level Configuration

> Status: **planning only**. Companion to `spec.md` and `tasks.md`.

> **Stack note:** artifacts in this file (module names, package names,
> code samples) were originally written for Elixir/Phoenix. The
> project switched to C# + ASP.NET Core 9 in Phase 2. See
> `specs/roadmap.md` → "Stack migration note" for the mapping table.
> Concepts and endpoint contracts still hold; concrete artifacts get
> rewritten when this phase is picked up.


## 1. Goal

Turn everything that was hardcoded ("category weights", "streak
tier table", "level curve") into **editable configuration** stored
in the DB, with a minimal admin surface (Kaffy or a small LiveView
admin) and an audit log. This is the "board" that lets values be
tweaked without a deploy.

## 2. Scope

**In scope**
- `settings` key/value table for scalar knobs (e.g., overall
  multiplier cap).
- Lift streak tier bands from `config.exs` → DB table
  `streak_tiers`.
- Category CRUD in the admin surface (already have the schema from
  Phase 4).
- Level curve editable: switch between formula
  (`base * n^exponent`) and lookup table.
- `admin_users` table (or role flag on `users`) — one bootstrap
  admin.
- Simple audit log (`admin_events`): who changed what, when, before
  and after values.
- Kaffy (or LiveView) admin at `/admin`.
- Runtime cache of settings (ETS) with pub/sub invalidation across
  nodes (Phoenix.PubSub).

**Out of scope**
- Multi-tenant admin (gym as client — deferred, `README.md` §10).
- Per-challenge override of XP values (Phase 15 will introduce
  challenge-scoped values, on top of this infrastructure).
- End-user-facing "self-service" XP editor.

## 3. Approach

- Config service (`Xpeak.Config`) exposes typed getters:
  `Config.get_number("multiplier.cap")` etc.
- On boot: warm the ETS cache with all `settings` rows.
- On write: update DB → publish invalidation event → other nodes
  refresh ETS.
- Domain code never reads DB directly — always goes through
  `Config.get_*`.
- Per-scope caps (min/max) declared in the settings row itself so
  an admin can't put obviously absurd values.

## 4. Artifacts

- `priv/repo/migrations/*_create_settings.exs`,
  `*_create_streak_tiers.exs`, `*_create_admin_events.exs`,
  `*_add_role_to_users.exs`.
- `lib/xpeak/config.ex` — read API + cache.
- `lib/xpeak/config/setting.ex`, `streak_tier.ex`, `admin_event.ex`.
- Update `StreakTier` from Phase 7 to read from DB (with fallback
  seed).
- Update `MultiplierResolver.@cap` to read from `Config`.
- Update `LevelCurve` to read parameters from `Config`.
- `Xpeak.Admin` context with authorization plug.
- Kaffy config (or LiveView admin pages).

## 5. Dependencies

- Blocked by: Phases 4, 7.
- Blocks: Phase 15 (challenge XP overrides), Phase 20 (custom trophy
  values probably read from config too), Phase 21 (recap reads
  configured curve so historical values are consistent).

## 6. Open questions

1. **Admin UI choice** — Kaffy (works fine, less flexible) vs.
   hand-rolled LiveView admin (more work, more control) vs.
   plain-Ex IEx-only for MVP?
2. **Role model** — add `role` column (`user`/`admin`) or separate
   `admin_users` table?
3. **Bootstrap admin** — `mix xpeak.grant_admin <email>` task, or
   env var `INITIAL_ADMIN_EMAIL`?
4. **Audit log retention** — keep forever, or roll off after 1 year?
5. **Concurrent write conflicts** — optimistic locking on `settings`
   rows or last-write-wins?

## 7. Success criteria

- `Xpeak.Config.get("multiplier.cap")` returns the DB value, cached
  in ETS.
- Editing a `settings` row in the admin surface takes effect on the
  next request (or within one PubSub tick across nodes).
- Category weights editable from the admin UI; a new check-in uses
  the updated weight.
- Streak tier bands editable; `StreakTier.multiplier_for/1` reflects
  changes immediately.
- Audit log records the before/after diff on every admin write.
- Non-admin user hitting `/admin` → 403.
- CI green.
