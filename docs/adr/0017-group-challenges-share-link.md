# ADR 0017 — Group Challenges share-link MVP

- **Status:** Proposed
- **Date:** 2026-10-20
- **Context:** Phase 17 (`specs/017-group-challenges/`), on branch
  `XPK-32/felpa-group-challenges-sharelink`.
- **Supersedes (in part):** nothing. Carves a cut out of Phase 17.
- **Related:** ADR 0030 (collapse into Challenges), which makes
  Challenges the sole social unit this cut extends.

## Context

Phase 15 (Solo Challenges) shipped the Challenge entity and its
owner-only flow: create, list, view, delete, progress. There is no
way yet for a second user to join a challenge, so the product has
no social loop and no path to a beta with real users.

Phase 17 as originally drafted targeted the full "group challenge"
surface — public discovery search, preview endpoint, group feed,
public/private visibility, share-link mechanics with per-link usage
caps and expirations. Shipping the whole thing takes several
sprints and blocks on Phase 12 (friends), Phase 13 (activity
events) and a lifecycle job that is itself deferred.

The product need right now is narrower: **let the owner of a
challenge invite other users via a link so we can hand the app to
beta testers**. Public discovery, public preview, group feed and
browse tab can wait until there is content worth browsing.

## Decision

We ship a minimal cut of Phase 17 — **"Share-link MVP"** — with
the following shape:

### `challenge_invitations` table

Create the table from scratch (Phase 16 "targeted friend
challenges" is being skipped in this cut, so there is no existing
table to extend). Columns:

| Column               | Notes                                                  |
|----------------------|--------------------------------------------------------|
| `id` (PK, uuid)      | **The public share token.** Carried verbatim in URLs. |
| `challenge_id`       | FK → `challenges`, cascade on delete.                 |
| `invited_by_user_id` | FK → `users`, non-null. Who generated the link.        |
| `invitee_user_id`    | FK → `users`, nullable. Null ⇒ share-link; non-null ⇒ targeted (Phase 16). |
| `state`              | `pending \| accepted \| rejected \| expired`. Share-link lifecycle only uses `pending`. |
| `created_at`, `updated_at` | timestamps.                                   |

Indexes: `(challenge_id, invitee_user_id)`, `(challenge_id, state)`.
No separate `code` column — the row's `id` (UUID v4, 122 bits of
entropy) is unguessable enough. No `expires_at`, no `max_uses`, no
`uses` counter.

### Auto-create at challenge creation

`ChallengeService.CreateAsync` persists the challenge, the owner's
membership and a default share-link invitation in the same
`SaveChangesAsync`. The create response carries the share-link id
so the client can render "copy link" with no follow-up request.

There is **no standalone `POST /challenges/:id/share_links`
endpoint** in this cut. If a link gets compromised, the owner
closes the challenge; see "Early end" below.

### Join via `POST /challenges/join`

Body is `{ invitationId: uuid }`. The service:

1. Looks up the invitation (`InvitationNotFoundException` → 404).
2. Checks the invitation is `pending` (`InvitationNotActiveException` → 409).
3. Loads the challenge and checks `State == Active`
   (`ChallengeNotActiveException` → 409).
4. Checks the caller is not already a member
   (`AlreadyMemberException` → 409).
5. Checks `MemberCap` via `COUNT(*)` on memberships
   (`MemberCapReachedException` → 409).
6. Inserts a `ChallengeMembership` row with `Role = Member` and
   fresh progress.

### Owner-only early end

`POST /challenges/:id/end` flips the challenge to
`ChallengeState.Completed` and deletes every invitation for that
challenge in the same transaction. 404 for anyone who is not the
owner (no leak of existence); 409 if the challenge is already
non-Active.

This replaces the "revoke share-link" endpoint from the original
plan. The owner either lets the challenge run to `ends_at` (future
lifecycle job, T-015-07, deferred) or closes it manually before
the deadline.

### Mobile surface

- **Share button** on the challenge detail (owner only): displays
  `window.location.origin + '/challenges/join?invitation=<id>'`
  and copies it to the clipboard via `navigator.clipboard.writeText`.
- **Join page** at `/challenges/join` reads `?invitation=<uuid>`
  from the search params, calls the join mutation on mount, and
  redirects to the challenge detail on success (or to
  `/challenges` on error).
- Native deep-link listener (`@capacitor/app` `appUrlOpen`) is
  **deferred**. Beta is web-first; `xpeak://` scheme and native
  intent filters land when native builds do.

### Hook pattern

The mobile `useChallenges.ts` module was converted to the project
convention of class + interface + singleton (mirroring `useAuth`).
Consumers call `challengesHooks.useDetail(id)` etc. instead of
top-level named exports. See the shared mobile hooks pattern in
`feedback_mobile_hooks_pattern.md`.

## Alternatives considered

### A. Dedicated `code` column (ULID / short code)

Keep the row's `id` as a hidden PK and expose a shorter `code` for
URLs. Allows "say it over the phone" use cases. Rejected: adds a
column, a unique partial index, a shape constraint, and nobody
dictates share-link codes over the phone in 2026. Reintroduce if a
real need shows up.

### B. Signed stateless tokens (no row at all)

Compute `code = base64({ challengeId, expiresAt }) + hmac` and
skip the DB write. Attractive at first — but **no revoke**, no
`uses` audit, no "which link did this join come from". Rejected
because any realistic revoke story drags the DB back in.

### C. `expires_at` on the invitation

Original plan. Beta usability feedback killed it: generating a
link should be zero-friction, and the challenge's own `ends_at` is
the natural upper bound. If the challenge ends, the join endpoint
refuses based on `State != Active`; the invitation row itself
doesn't need to expire.

### D. `max_uses` + `uses` counter

Original plan. Kept until a late review surfaced the question
"what does `max_uses` give us that `challenge.member_cap` doesn't
already give us?". The challenge-wide cap covers the only concrete
case ("don't let 100 strangers in"), and per-link caps complicate
the join path with a `uses++` race. Rejected.

### E. Per-invitation revoke (`DELETE /share_links/:id`)

Keep share-link rows and flip `state → expired` (or delete the row)
on revoke. Rejected in favour of **owner-level early end**: in
practice the owner reaches for "revoke" because they regret the
whole challenge, not because of a specific link. Collapsing one
button into another ("end the whole thing") is simpler to explain
and simpler to code. Reintroduce if beta users ask for finer
control.

### F. Confirmation screen before auto-join

Alternative mobile UX: the join page shows "Entrar no challenge
XYZ? [Entrar]" and only fires the mutation on click. Rejected for
MVP because the preview endpoint is also deferred — we have no way
to render challenge metadata without first calling join. User
clicking the link = implicit consent for beta. Confirmation screen
revisits with the preview endpoint.

## Consequences

### Positive

- Beta can ship this week. One link per challenge, generated at
  creation, copied with one tap.
- Zero schema debt (no `code`, no `expires_at`, no `max_uses`, no
  `uses`). The row has 7 columns and 1 check constraint.
- Join path has clear 404 vs 409 semantics the mobile already
  maps to toasts.
- Challenge delete already cascades invitations via FK; no cleanup
  job needed.
- Reusing `Completed` for owner early-end avoids an enum migration.
- The mobile hooks module now matches the `useAuth` pattern, so
  future hook modules have one convention to follow.

### Negative

- No per-invitation revoke. If a link leaks and the owner wants to
  keep the challenge alive, their only option is "end the whole
  challenge and start a new one". Acceptable for beta scale.
- No public discovery. Users can only enter a challenge via a link
  they received directly. Phase 17's deferred Section C
  (search + preview) is the right place to add this.
- No group feed. The challenge detail does not show other members'
  check-ins. Blocked upstream on Phase 13; revisit then.
- The join page auto-fires the mutation on mount, which requires a
  `useRef` guard to survive React StrictMode. Minor code smell;
  the confirmation-screen variant above resolves it when the
  preview endpoint lands.
- `member_cap` enforcement is at app level with `COUNT(*)`, not
  via a row lock or trigger. Two concurrent joins could race past
  the cap by one. Acceptable at beta scale; revisit if we hit
  contention.
- Reusing `Completed` for "closed early by owner" loses the
  distinction between "target hit" and "owner called it off". If
  the product cares about this difference, add a `Cancelled` or
  `EndedEarly` state in a follow-up migration.
- Native deep-link handler is deferred, so the first native build
  will need a Phase-17 follow-up to register `xpeak://` and route
  it to the join page.

## Open follow-ups

- **Phase 17 Section C** (search + preview) — public discovery
  when there are enough public challenges to discover.
- **Phase 17 Section D** (group feed) — blocked on Phase 13 events.
- **Phase 17 T-017-02** (`challenges.member_count` cache) — revisit
  alongside Section C ranking.
- **Phase 17 T-017-09** (browse tab) — blocked on Section C.
- **Phase 17 T-017-12** (feed tab) — blocked on Section D.
- **Phase 15 T-015-07** (lifecycle Hangfire job) — when it lands,
  it must also delete invitations for the challenges it transitions
  out of `Active` (same semantics as owner early-end).
- **Native deep links** — register `xpeak://challenges/join?invitation=`
  via `@capacitor/app` listener and the native intent filters when
  the first native build happens.
- **Possible follow-up ADR** — if beta surfaces real need for
  per-link revoke or dedicated "ended early" state, revisit the
  decisions in alternatives E and F above.
