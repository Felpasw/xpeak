# Phase 17 — Group Challenges (SPEC)

> **Scope cut (2026-10-19) — "Share-link MVP"**
>
> First slice of Phase 17 ships without Phase 16 (friend challenges)
> and without Phase 13 (activity events). The share-link flow is
> enough to open the app for beta testing. Public search / preview
> endpoint / group feed / browse tab are tracked as `[DEFERRED]` in
> `tasks.md` and revisited after Phase 12 and Phase 13 land.
>
> Phase 30 fallout: `challenge_categories` no longer exists, so any
> search filter by category is out of scope here as well.

## 1. Share-link model

Introduces `challenge_invitations` from scratch (Phase 16 is being
skipped for this cut). Minimal shape: no separate `code` column —
the row's `id` (UUID v4, 122 bits of entropy) is the public share
token, carried verbatim in URLs. No `expires_at` and no per-link
usage cap either. The only limits on who can join a share-link are
the parent challenge's lifecycle (`state → completed|failed`) and
its `member_cap` (if set). Share-link vs targeted is a binary
distinction:

- `invitee_user_id IS NULL` → share-link (anyone with the id joins).
- `invitee_user_id IS NOT NULL` → targeted (Phase 16; only that user
  joins).

Add the entity + the table via migration.

## 2. Context additions

Share-link creation is folded into `ChallengeService.CreateAsync`:
every new challenge is persisted together with a pending share-link
invitation in the same `SaveChangesAsync`, so the client gets the
share token on the create response without a follow-up call. There
is no standalone `CreateShareLink` service method in this cut; if
the owner revokes the link and wants a new one, that's a deferred
sub-feature.

```csharp
def join_with_invitation(user, invitation_id) do
  # find invitation by id
  # validate state is pending, challenge is active,
  #   member_cap not reached, not already a member
  # insert challenge_membership
  # emit event
end

def end_challenge(caller, challenge_id) do
  # validates caller is the owner and challenge is Active,
  # flips State -> Completed and deletes every invitation for the
  # challenge in the same transaction (no "revoke" state)
end
```

## 3. Endpoints

- `POST /challenges` already returns the default share-link in the
  response body (see §2); no separate "create link" endpoint ships
  in this cut.
- `POST /challenges/join` — `{ invitation_id }` → 200 with challenge
  summary.
- `POST /challenges/:id/end` — owner-only early close. Flips the
  challenge to `Completed` and deletes every invitation for it in
  one transaction. No per-invitation revoke endpoint.
- `GET /challenges/preview?invitation=` — public preview (no auth)
  for the landing page.
- `GET /challenges?search=&categories=&sort=popular&status=open`
  — public discovery.
- `GET /challenges/:id/feed?cursor=` — group feed of check-ins
  filtered to challenge members within the window.

## 4. Search

Search query (simplified):

```sql
SELECT c.* FROM challenges c
WHERE c.mode = 'group'
  AND c.visibility = 'public'
  AND c.state IN ('upcoming','active')
  AND (:search IS NULL OR c.title ILIKE '%' || :search || '%'
       OR c.description ILIKE '%' || :search || '%')
  AND (:categories IS NULL OR EXISTS (
        SELECT 1 FROM challenge_categories cc
        WHERE cc.challenge_id = c.id AND cc.category_id = ANY(:categories)))
ORDER BY
  CASE :sort
    WHEN 'popular' THEN c.member_count DESC
    WHEN 'ending_soon' THEN c.ends_at ASC
    ELSE c.inserted_at DESC
  END
LIMIT :limit OFFSET :offset;
```

Materialize `member_count` on the challenge row (updated by trigger
or by hand on join/leave) for cheap sorting.

## 5. Mobile

### 5.1. Browse tab

- Search bar + filters (category chips + sort selector).
- Card list: title, member count, days left, category chips, join
  button.

### 5.2. Share link generator

- On the challenge detail: "Invite" section.
- Buttons: **Copy link**, **Share via**... (uses `@capacitor/share`
  when native), **Revoke**.
- Optional max_uses input; default unlimited within challenge
  window.

### 5.3. Join by code

- Screen at `/challenges/join`: enter code manually or arrives via
  deep link with prefilled code.
- Shows preview card from `GET /challenges/preview` before confirming.

### 5.4. Group feed on detail

- New tab on the challenge detail: **Feed**.
- List of member check-ins during the challenge window (uses Phase 13
  cards).

## 6. Test plan

- Public challenge appears in search; private doesn't.
- Share link:
  - Anyone with code joins.
  - `max_uses` respected (Nth+1 join → 422).
  - Revocation expires immediately.
- `member_count` updates on join/leave.
- Preview endpoint returns without auth for both public and
  code-based (via `?code=`).
- Mobile: deep link flow → preview → join.

## 7. Non-goals

- Insignias awarded on group challenge completion (Phase 18).
- Co-ownership / role escalation.
- Public discovery ranking beyond popularity/recency.
- Featured / curated section.
