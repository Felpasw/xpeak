# Phase 21 — Metrics Dashboard & Yearly Recap

> Status: **planning only**.

## 1. Goal

Two related surfaces:

1. **Metrics dashboard** — the always-on progression view surfaced
   every time the user opens the app (per `ideas.md` §2.8.1 locked
   decision).
2. **Yearly recap** — story-style, shareable summary generated at
   year rollover (or anniversary — decision pending).

## 2. Scope

**In scope**
- Aggregation queries: totals + time-sliced breakdowns.
- Streak calendar heatmap data endpoint.
- Multiplier history breakdown from `multiplier_snapshot` rows.
- `recaps` table + Oban batch job for yearly recap.
- `GET /recaps/:period` endpoint.
- Mobile: metrics screen on home + full dashboard behind link;
  yearly recap full-screen story viewer.
- Shareable card rendering (per-stat + final summary image).

**Out of scope**
- Charting library selection (deferred per user).
- Comparative stats ("you out-trained X%") — `ideas.md` §2.8.3
  decision pending.
- Deep analytics dashboards (product-level insights).

## 3. Approach

- Read models built from existing tables:
  - `check_ins` (XP, streaks, categories, dates).
  - `activity_events` (level-ups, medals, insignias, trophies,
    challenges).
  - `friendships` (friends made this year).
- Precompute expensive rollups (per-day XP, per-category totals,
  per-week/month totals) via a nightly Oban job into a `metric_daily`
  materialization table.
- Recap generation: Oban job iterates all users with ≥ 1 check-in
  in the period; produces one `recaps` row per user with cached
  payload.
- Shareable card rendering: backend renders PNG via headless
  Chromium (Chrome/Playwright) or via a canvas-based Elixir library.
  Decision deferred.

## 4. Artifacts

- Migrations: `create_metric_daily.exs`, `create_recaps.exs`.
- `lib/xpeak/metrics.ex` — read API for the dashboard.
- `lib/xpeak/metrics/rollup_job.ex` (Oban nightly).
- `lib/xpeak/metrics/recap_generator.ex` (Oban batch on
  rollover/anniversary).
- Endpoints: `GET /me/metrics`, `GET /me/metrics/heatmap`,
  `GET /recaps/:period`.
- Mobile: home metrics snapshot + dashboard screen + recap story
  viewer.

## 5. Dependencies

- Blocked by: Phases 5, 7, 11, 15/17/18/20 (all achievement flavors
  supply data), 13 (events feed rollup).
- Blocks: —

## 6. Open questions (already tracked in `ideas.md` §2.8.3)

1. Recap trigger date — calendar year vs. anniversary.
2. Charting library (deferred).
3. Comparative stats yes/no.
4. Shareable rendering — backend PNG vs. client canvas.
5. Home layout: Option A (snapshot in profile + deep screen) vs.
   Option B (dedicated Metrics tab).

## 7. Success criteria

- `GET /me/metrics?period=year` returns totals + per-category
  breakdown + streak heatmap.
- Nightly rollup runs and populates `metric_daily`; dashboard reads
  from it (not from raw `check_ins`).
- On year rollover (or anniversary), `RecapGenerator` produces a
  `recaps` row per eligible user.
- `GET /recaps/2026` returns the payload or 202 if still processing.
- Mobile home shows snapshot; deep screen loads full breakdown;
  recap opens as story cards, sharable.
- CI green.
