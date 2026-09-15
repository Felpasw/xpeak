# Phase 17 — Group Challenges (TASKS)

## Legend
`[T]` TDD, `[S]` sequential, `[P]` parallel.

---

## Section A — Schema updates

- [ ] **T-017-01 `[T][S]`** — invitations extended fields
  - Migration: `code`, `max_uses`, `uses`, `expires_at` (nullable
    invitee_id).
  - Constraint: exactly one of invitee_id / code.
  - Green.

- [ ] **T-017-02 `[T][S]`** — `challenges.member_count` cached column
  - Migration + trigger (or context-level increment).
  - Failing tests: join/leave adjusts count.
  - Green.

## Section B — Share-link context

- [ ] **T-017-03 `[T][S]`** — `create_share_link/2`
  - Failing tests: ULID uniqueness, defaults.
  - Green.

- [ ] **T-017-04 `[T][S]`** — `join_with_code/2`
  - Failing tests: happy, over-cap, expired, already-member.
  - Green.

- [ ] **T-017-05 `[T][S]`** — `revoke_link/2`
  - Failing tests.
  - Green.

## Section C — Discovery

- [ ] **T-017-06 `[T][S]`** — Search query + endpoint
  - Failing tests per filter/sort combination.
  - Green.

- [ ] **T-017-07 `[T][S]`** — Preview endpoint (no auth)
  - Failing tests: returns minimal shape for public + valid code
    (private).
  - Green.

## Section D — Group feed

- [ ] **T-017-08 `[T][S]`** — `GET /challenges/:id/feed`
  - Failing tests: members-only, scoped to window, uses Phase 13
    events.
  - Green.

## Section E — Mobile

- [ ] **T-017-09 `[T][S]`** — Browse tab
  - Failing spec: search, filters, join button.
  - Green.

- [ ] **T-017-10 `[T][S]`** — Share link generator
  - Failing spec: create link, copy, share via native, revoke.
  - Green.

- [ ] **T-017-11 `[T][S]`** — Join by code screen + deep link
  - Failing spec: renders preview then joins on confirm.
  - Wire `xpeak://challenges/join?code=` deep link handler.
  - Green.

- [ ] **T-017-12 `[T][S]`** — Group feed tab on detail
  - Failing spec: renders member events.
  - Green.

## Section F — Wrap-up

- [ ] **T-017-13 `[P]`** — ADR `0017-group-challenges.md`.
- [ ] **T-017-14 `[S]`** — Phase close.

---

## Bundling strategy for PRs

1. `feat(challenges): share-link schema + member_count` — A
2. `feat(challenges): share-link context (create/join/revoke)` — B
3. `feat(challenges): discovery search + preview` — C
4. `feat(challenges): group feed endpoint` — D
5. `feat(mobile): browse + share-link + join-by-code + group feed` — E
6. `docs(adr): group challenges` — F
