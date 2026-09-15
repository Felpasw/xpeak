# Phase 17 — Group Challenges (Full)

> Status: **planning only**.

## 1. Goal

Full group challenge modality: **public** (browseable via search)
or **private** (share-link only). Public/private semantics per
`ideas.md` §2.7.

## 2. Scope

**In scope**
- Reuse the challenge schema from Phase 15 (`mode: "group"`,
  `visibility: "public" | "private"`).
- Share-link mechanics: signed, unguessable, revocable code with
  optional `max_uses` and `expires_at`.
- Discovery endpoint (`GET /challenges?search=...&status=open&...`)
  for public challenges.
- Group feed (list of check-ins tied to the challenge, visible to
  members).
- Multiple share links per challenge (one per channel).
- Deep-link handling on mobile: `xpeak://challenges/join?code=...`.

**Out of scope**
- Insignias for completion (Phase 18).
- Group multiplier / co-located bonus (Phase 19).
- Trophies (Phase 20).

## 3. Approach

- Public/private is a simple visibility flag. All lookup queries
  (search, list) filter accordingly.
- Share link = a `challenge_invitations` row with `invitee_id: nil`
  and `code` populated. Joining consumes a "use" and, when
  `max_uses` reached or `expires_at` passed, transitions to
  `expired`.
- Code = ULID (26 chars, unguessable). Optionally signed with HMAC
  in the URL for tamper detection, though ULID alone is enough for
  MVP.

## 4. Artifacts

- Extend `challenges` with `visibility` filters (schema already has
  the column).
- Extend `Xpeak.Challenges` with `create_share_link/2`,
  `join_with_code/2`, `revoke_link/2`.
- New endpoints for discovery + share links + join by code.
- Group feed endpoint reusing Phase 13 activity events, scoped to
  the challenge.
- Mobile: browse tab, share-link generator, join-by-code screen,
  deep-link handler.

## 5. Dependencies

- Blocked by: Phases 15, 16.
- Blocks: Phase 18, Phase 19, Phase 20.

## 6. Open questions

1. **Search ranking** — popularity + recency? Signals to consider:
   member count, XP earned this week by members, days remaining.
2. **Co-ownership** — allow owner to promote a member to co-owner?
3. **Anonymous join preview** — before signing in, should the
   invited user see a card previewing the challenge?
   Recommendation: yes, minimal preview endpoint.
4. **Reporting a challenge** — abuse queue? Deferred, likely.

## 7. Success criteria

- `POST /challenges` with `mode: "group", visibility: "public"` →
  challenge shows up in search.
- Private challenge with share code — anyone with the code can
  join up to `max_uses`; can't be found in search.
- Revoking a link expires it immediately.
- Group feed shows check-ins from all members.
- Deep-link `xpeak://challenges/join?code=X` opens a preview and
  joins on confirm.
- CI green.
