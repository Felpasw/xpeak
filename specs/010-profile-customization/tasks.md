# Phase 10 — Profile Customization (TASKS)

## Legend
`[T]` TDD, `[S]` sequential, `[P]` parallel.

---

## Section A — Schema + seeds

- [ ] **T-010-01 `[T][S]`** — Users additions + frames migration/schema
  - Failing tests: fields present, FK integrity.
  - Migration + schema.
  - Green.

- [ ] **T-010-02 `[S]`** — Seed 10 default frames
  - `priv/repo/seeds/frames.exs`.

## Section B — Context

- [ ] **T-010-03 `[T][S]`** — `Xpeak.Profile.update_profile/2`
  - Failing tests per `spec.md` §4.
  - Implement.
  - Green.

## Section C — HTTP surface

- [ ] **T-010-04 `[T][S]`** — `PATCH /me/profile`
  - Failing controller tests.
  - Implement.
  - Green.

- [ ] **T-010-05 `[T][S]`** — `POST /me/media/presign` (avatar/banner)
  - Failing tests: size cap per purpose.
  - Extend media controller or add new one.
  - Green.

- [ ] **T-010-06 `[T][S]`** — `GET /frames`
  - Failing test: lists active frames with signed preview URLs.
  - Implement.
  - Green.

## Section D — Mobile

- [ ] **T-010-07 `[T][S]`** — Profile edit screen scaffold
  - Failing spec: renders avatar/banner/frame tiles.
  - Implement.
  - Green.

- [ ] **T-010-08 `[T][S]`** — Avatar/banner upload flow
  - Extends media picker from Phase 6 for a single item.
  - Failing spec: upload happy path.
  - Green.

- [ ] **T-010-09 `[T][S]`** — Frame gallery + preview
  - Failing spec: selecting a frame overlays on the avatar preview.
  - Implement.
  - Green.

- [ ] **T-010-10 `[T][S]`** — Profile card with cosmetics
  - Failing spec: renders animated avatar + frame overlay + banner.
  - Update Phase 3 profile screen.
  - Green.

## Section E — Wrap-up

- [ ] **T-010-11 `[P]`** — ADR `0010-profile-customization.md`.
- [ ] **T-010-12 `[S]`** — Phase close.

---

## Bundling strategy for PRs

1. `feat(profile): schema + frames seeds + context` — A + B
2. `feat(profile): endpoints for edit + presign + frames list` — C
3. `feat(mobile): profile edit + cosmetics rendering` — D
4. `docs(adr): profile customization` — E
