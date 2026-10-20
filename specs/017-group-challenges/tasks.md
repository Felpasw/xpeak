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

- [x] **T-017-01 `[T][S]`** ✅ commit `0aabae4` — `challenge_invitations` table (create)
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

- [ ] **T-017-02 `[T][S][DEFERRED]`** — `challenges.member_count`
  cached column
  - Original purpose was to support search ranking (sort by
    popularity) and avoid N+1 in list rendering. Search is
    DEFERRED (T-017-06) and the share-link MVP doesn't need the
    cache — `COUNT(*)` on `challenge_memberships` at join time is
    plenty for `member_cap` enforcement at beta scale (small
    challenges). Revisit alongside T-017-06.

## Section B — Share-link context

- [x] **T-017-03 `[T][S]`** ✅ commit `a6c78d3` — Auto-create share-link on challenge
  creation
  - `ChallengeService.CreateAsync` persists a share-link invitation
    alongside the challenge + owner membership in a single
    `SaveChangesAsync`. The row's `id` is the public share token.
  - `CreateChallengeResult` and `CreateChallengeResponse` grow a
    `ShareLink` field so the client renders "copy link" straight
    after creation — no `POST /challenges/:id/share_links` call
    needed in the MVP.
  - Failing tests: `ChallengeService.CreateAsync` persists a
    pending share-link row with `invitee_user_id = null` and the
    owner as `invited_by_user_id`; `POST /challenges` response body
    carries the share-link id.
  - A standalone endpoint for regenerating a link post-revoke is
    `[DEFERRED]` (see T-017-05).
  - Green.

- [x] **T-017-04 `[T][S]`** ✅ commit `e1524bf` — `join_with_invitation`
  - Failing tests: happy, challenge not active, invitation not
    found, invitation expired/revoked, member_cap reached,
    already-member.
  - Green.

- [x] **T-017-05 `[T][S]`** ✅ commit `89f7122` — Owner-only early end of a challenge
  - Reinterpreted from "revoke share-link". Beta UX decision: no
    revoke of individual invitations; the owner either lets the
    challenge run to `ends_at` (future lifecycle job, T-015-07
    `[DEFERRED]`) or closes it manually before the deadline.
  - Service: `ChallengeService.EndAsync(id, callerId)` returns
    `false` when the challenge is missing or caller is not the
    owner (endpoint 404), throws `ChallengeAlreadyEndedException`
    when state is not `Active` (endpoint 409), otherwise flips
    `State → Completed`, deletes every row from
    `challenge_invitations` for that challenge in the same
    transaction, and returns `true`.
  - Endpoint: `POST /challenges/:id/end` → 204 on success.
  - Reuses existing `Completed` state — no new enum value or
    migration needed. Semantic is "challenge is closed"; if we
    later want to distinguish "finished early by owner" from
    "target reached", introduce a dedicated state then.
  - Failing tests: service happy path flips state + deletes
    invitations; non-owner caller returns false; unknown
    challenge returns false; already-ended throws. Endpoint:
    204 happy, 404 non-owner, 409 already-ended.
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
