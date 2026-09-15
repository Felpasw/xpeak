# Phase 23 — Exploration / Locations (Gyms)

> Status: **planning only**.

## 1. Goal

Turn the passive GPS data collected in Phase 19 into an active
mechanic: **visiting different locations** (gyms, parks, tracks)
unlocks XP bonuses, badges, and regional leaderboards.

## 2. Scope

**In scope**
- `locations` table (gyms and similar POIs).
- Location matching on check-in (find or create nearest known
  location within radius, or "unknown location" fallback).
- Bonus XP on first-ever check-in at a location.
- `visited_locations` per-user list.
- Regional leaderboards: city-level (derived from user-declared
  location or geocoded).
- Location-tied challenges (a challenge scoped to a specific
  location).
- Mobile: locations screen showing visited gyms + map (or list).

**Out of scope**
- Gym-owner accounts / claiming a location (deferred).
- Real-time crowd counter ("how many members here now").
- Global POI database import (start empty + let users seed via
  check-in).

## 3. Approach

- Data source for locations: user-created via a "New location"
  flow in the check-in screen. Optional Google Places integration
  later.
- Matching algorithm: on check-in with GPS, if a known location
  exists within N meters of the point, associate. Else prompt the
  user "Add this as a new location?".
- Region derivation: reverse geocode on first check-in (Nominatim
  or Google Geocoding — decision below).
- Regional leaderboard integrates with Phase 14 rankings as a new
  scope (`xp_all_time:region:<slug>`).

## 4. Artifacts

- Migrations: `create_locations.exs`,
  `create_user_visited_locations.exs`,
  `add_location_id_to_check_ins.exs`,
  `add_region_to_users.exs` (or derived).
- `lib/xpeak/exploration.ex` — context.
- `lib/xpeak/exploration/location_matcher.ex`.
- Integration with `MultiplierResolver` for first-visit bonus.
- Ranking scope addition in Phase 14.
- Mobile: location picker on check-in, visited-locations screen.

## 5. Dependencies

- Blocked by: Phases 5, 6, 14, 19.
- Blocks: —

## 6. Open questions

1. **Geocoding provider** — Nominatim (free, rate-limited) vs. Google
   Geocoding (paid, reliable)?
2. **Match radius** — 50 m default? Larger for outdoor sports?
3. **Duplicate location prevention** — user proximity + name match
   heuristic; or manual admin merge?
4. **Location visibility** — public by default? Some gyms may want
   privacy.
5. **First-visit bonus** — 25% XP boost on the check-in? Cap
   interaction with global 3× cap.

## 7. Success criteria

- Check-in with GPS near a known gym → associates location + no
  bonus if visited before.
- Check-in with GPS far from any known location → prompts to
  create a new one.
- First-ever visit → XP bonus applied.
- Visited locations list on the profile.
- Regional leaderboard scope works in the rankings tab.
- CI green.
