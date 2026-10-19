# Phase 17 — Group Challenges (TASKS)

> **Scope cut (2026-10-19) — "Share-link MVP"**
>
> Phase 17 is being partially shipped under branch
> `XPK-32/felpa-group-challenges-sharelink` so we can hand the app to
> beta testers without rebuilding the full social graph. Only
> share-link-based invitations are in scope right now. Everything
> that depends on Phase 12 (friends), Phase 13 (activity events) or
> on public search is marked `[DEFERRED]` and will be revisited in a
> follow-up phase or sub-phase.
>
> Phase 16 (friend challenges) is being **skipped** for this cut, so
> `challenge_invitations` is created **from scratch** in T-017-01
> (not "extended"). The `invitee_id` column exists but stays `null`
> everywhere in this cut — it's reserved for Phase 16.
>
> Post-Phase 30 reminder: `challenge_categories` no longer exists.
> Any task or sub-section that assumed category filtering is already
> part of the deferred bucket.

## Legend
`[T]` TDD, `[S]` sequential, `[P]` parallel, `[DEFERRED]` out of
scope for this cut.

---

## Section A — Schema updates

- [ ] **T-017-01 `[T][S]`** — `challenge_invitations` table (create)
  - Migration: new `challenge_invitations` table with `id`,
    `challenge_id` (FK), `invited_by_user_id` (FK → users, not
    null — share-link rows still have a creator), `invitee_user_id`
    (FK → users, nullable), `state`
    (`pending|accepted|rejected|expired`), `created_at`, `updated_at`.
    No separate share code: the row's `id` is the public share
    token. No `expires_at` and no per-link usage cap — the parent
    challenge's lifecycle and its `member_cap` are the only limits.
  - Shape distinction: `invitee_user_id IS NULL` ⇒ share-link,
    otherwise targeted (Phase 16).
  - Indexes: `(challenge_id, invitee_user_id)`, `(challenge_id,
    state)`.
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

## Section C — Discovery `[DEFERRED]`

- [ ] **T-017-06 `[T][S][DEFERRED]`** — Search query + endpoint
  - Depends on public discovery UX; not required for share-link
    MVP. Revisit when we open browseable public challenges.

- [ ] **T-017-07 `[T][S][DEFERRED]`** — Preview endpoint (no auth)
  - Nice-to-have for the deep-link landing. Can ship after T-017-11
    if beta needs it. For MVP the join flow goes straight to the
    challenge detail after auth, no preview needed.

## Section D — Group feed `[DEFERRED]`

- [ ] **T-017-08 `[T][S][DEFERRED]`** — `GET /challenges/:id/feed`
  - Depends on Phase 13 (activity events) which is still 0/15.
  - Revisit after Phase 13 lands.

## Section E — Mobile

- [ ] **T-017-09 `[T][S][DEFERRED]`** — Browse tab
  - Blocked by T-017-06 / T-017-07.

- [ ] **T-017-10 `[T][S]`** — Share link generator
  - Failing spec: create link, copy, share via native, revoke.
  - Green.

- [ ] **T-017-11 `[T][S]`** — Join by invitation screen + deep link
  - Failing spec: on submit, posts to `POST /challenges/join` with
    the invitation id and lands on the challenge detail.
  - Wire `xpeak://challenges/join?invitation=` deep link handler so
    a tap on a shared link auto-submits (user confirms on screen).
  - Preview card from `GET /challenges/preview` is `[DEFERRED]` —
    for MVP the screen shows the challenge detail after joining.
  - Green.

- [ ] **T-017-12 `[T][S][DEFERRED]`** — Group feed tab on detail
  - Blocked by T-017-08.

## Section F — Wrap-up

- [ ] **T-017-13 `[P]`** — ADR `0017-group-challenges.md`.
- [ ] **T-017-14 `[S]`** — Phase close.

---

## Bundling strategy for PRs

Share-link MVP cut (`XPK-32/felpa-group-challenges-sharelink`):

1. `feat(challenges): add challenge_invitations and member_count` — A
2. `feat(challenges): share-link context (create/join/revoke)` — B
3. `feat(mobile): share-link generator + join-by-code screen` — E
   (T-017-10, T-017-11 only)
4. `docs(adr): group challenges share-link MVP` — F (T-017-13)
5. Phase close — F (T-017-14)

Deferred sections (C, D, parts of E) ship under follow-up branches
after the beta cycle, likely bundled with Phase 12/13 work.
