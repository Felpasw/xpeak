# Phase 16 — Friend Challenges (TASKS)

## Legend
`[T]` TDD, `[S]` sequential, `[P]` parallel.

---

## Section A — Schema

- [ ] **T-016-01 `[T][S]`** — challenge_invitations migration/schema
  - Failing tests: state whitelist, unique code.
  - Green.

## Section B — Context

- [ ] **T-016-02 `[T][S]`** — `Challenges.invite/3`
  - Failing tests: friendship check, duplicate check, notification
    emitted.
  - Green.

- [ ] **T-016-03 `[T][S]`** — `accept_invitation/2` +
      `reject_invitation/2`
  - Failing tests.
  - Green.

- [ ] **T-016-04 `[T][S]`** — Expiration job (Oban)
  - Failing tests.
  - Green.

## Section C — Leaderboard

- [ ] **T-016-05 `[T][S]`** — `ChallengeLeaderboard.rank_members/1`
  - Failing tests.
  - Implement (pure).
  - Green.

## Section D — HTTP surface

- [ ] **T-016-06 `[T][S]`** — Invitation endpoints
  - Failing tests: create, list (per-challenge + inbox), accept,
    reject, cancel.
  - Green.

- [ ] **T-016-07 `[T][S]`** — Extend `GET /challenges/:id` with
      leaderboard
  - Failing test: members list sorted by progress.
  - Green.

## Section E — Mobile

- [ ] **T-016-08 `[T][S]`** — "Challenge @friend" button on profile
  - Failing spec: friends see button; non-friends don't.
  - Green.

- [ ] **T-016-09 `[T][S]`** — Shortened wizard for friend challenge
  - Failing spec: creates challenge + invitation.
  - Green.

- [ ] **T-016-10 `[T][S]`** — Invitations inbox section
  - Failing spec: list + accept/reject actions.
  - Green.

- [ ] **T-016-11 `[T][S]`** — Leaderboard on detail
  - Failing spec: renders member ranks.
  - Green.

## Section F — Wrap-up

- [ ] **T-016-12 `[P]`** — ADR `0016-friend-challenges.md`.
- [ ] **T-016-13 `[S]`** — Phase close.

---

## Bundling strategy for PRs

1. `feat(challenges): invitations schema + context` — A + B
2. `feat(challenges): leaderboard + expiration + endpoints` — C + D
3. `feat(mobile): friend challenge wizard + invitations inbox + leaderboard` — E
4. `docs(adr): friend challenges` — F
