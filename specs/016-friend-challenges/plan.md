# Phase 16 — Friend Challenges

> Status: **planning only**.

> **Stack note:** artifacts in this file (module names, package names,
> code samples) were originally written for Elixir/Phoenix. The
> project switched to C# + ASP.NET Core 9 in Phase 2. See
> `specs/roadmap.md` → "Stack migration note" for the mapping table.
> Concepts and endpoint contracts still hold; concrete artifacts get
> rewritten when this phase is picked up.


## 1. Goal

Lightweight 1v1 or small-group challenges between users already
connected as friends. Faster to create than a full group challenge
(fewer options, no discovery, invite-only via the friendship
graph). Reuses everything from Phase 15 except the join flow.

## 2. Scope

**In scope**
- New challenge `mode: "friend"` semantics.
- Simplified create flow ("Challenge @friend").
- Invitations sent to specific friends (uses the friendship graph
  from Phase 12).
- Accept/reject invitations.
- Small-group support (2–8 members).
- Personal ranking within the friend challenge (fastest to reach
  target, etc.).
- Feed emission on challenge start / finish.

**Out of scope**
- Full public/private group challenges (Phase 17).
- Insignias/trophies (Phases 18, 20).

## 3. Approach

- Friend challenges are always `visibility: "private"` and
  `mode: "friend"`.
- Invitations are records (`challenge_invitations`) tied to specific
  `friendship_id`s. Only mutual friends can be invited.
- Accepting creates a `challenge_memberships` row.
- The leaderboard for a friend challenge is scoped to its members.

## 4. Artifacts

- Migration: `create_challenge_invitations.exs` (used by both
  friend and group challenges).
- Extend `Xpeak.Challenges` with `invite/3`, `accept_invitation/2`,
  `reject_invitation/2`.
- Endpoints for invitations.
- Mobile: "Challenge @friend" flow from a friend's profile.

## 5. Dependencies

- Blocked by: Phases 12, 15.
- Blocks: Phase 17 (group challenges reuse the invitation model +
  add share-link).

## 6. Open questions

1. **Max members** — 8? 12?
2. **Owner-only cancel or member-can-leave?** — leave allowed with
   `result: "cancelled"` on that membership only.
3. **Invitation expiration** — 7 days? Match challenge `starts_at`?

## 7. Success criteria

- From a friend's profile: "Challenge @user" → wizard → creates
  challenge + invitation.
- Invitee sees pending invite → accepts → is now a member.
- Both users see the challenge in the challenges tab.
- Progress is per-member; leaderboard sorts.
- CI green.
