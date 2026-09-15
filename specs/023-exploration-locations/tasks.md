# Phase 23 — Exploration / Locations (TASKS)

## Legend
`[T]` TDD, `[S]` sequential, `[P]` parallel.

---

## Section A — Schema

- [ ] **T-023-01 `[T][S]`** — locations + user_visited_locations
      migrations
  - Green.

- [ ] **T-023-02 `[T][S]`** — check_ins.location_id +
      users.home_region_slug
  - Green.

## Section B — Matcher + geocoding

- [ ] **T-023-03 `[T][S]`** — `LocationMatcher.match_or_prompt/2`
  - Failing tests.
  - Green.

- [ ] **T-023-04 `[S]`** — Geocoding provider adapter
  - Choice per `plan.md` §6 Q1.
  - Behaviour + one adapter + mock.

## Section C — Endpoints

- [ ] **T-023-05 `[T][S]`** — Location endpoints
  - `GET /locations`, `POST /locations`,
    `GET /me/visited_locations`,
    `GET /users/:username/visited_locations`.
  - Failing tests.
  - Green.

## Section D — First-visit bonus

- [ ] **T-023-06 `[T][S]`** — Bonus in `MultiplierResolver`
  - Failing tests: applied once per user × location.
  - Green.

## Section E — Regional rankings

- [ ] **T-023-07 `[T][S]`** — Region scope in rankings snapshot job
  - Failing tests.
  - Green.

- [ ] **T-023-08 `[T][S]`** — `GET /rankings/region/:slug/:scope`
  - Failing tests.
  - Green.

## Section F — Location-tied challenges

- [ ] **T-023-09 `[T][S]`** — challenges.location_id
  - Failing tests: only matching check-ins count.
  - Extend ProgressCalculator + resolver filter.
  - Green.

## Section G — Mobile

- [ ] **T-023-10 `[T][S]`** — Location step on check-in
  - Failing spec: prefills / prompts / adds new.
  - Green.

- [ ] **T-023-11 `[T][S]`** — Visited locations screen
  - Failing spec: list + map view.
  - Green.

- [ ] **T-023-12 `[T][S]`** — Regional leaderboard in rankings tab
  - Failing spec: region scope selectable.
  - Green.

## Section H — Wrap-up

- [ ] **T-023-13 `[P]`** — ADR `0023-exploration-locations.md`.
- [ ] **T-023-14 `[S]`** — Phase close.

---

## Bundling strategy for PRs

1. `feat(exploration): locations schema + matcher` — A + B
2. `feat(exploration): first-visit bonus + endpoints` — C + D
3. `feat(exploration): regional rankings scope` — E
4. `feat(challenges): location-tied challenges` — F
5. `feat(mobile): location step, visited screen, regional rankings` — G
6. `docs(adr): exploration and locations` — H
