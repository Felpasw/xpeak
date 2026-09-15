# Phase 17 — Group Challenges (SPEC)

## 1. Share-link model

Reuses `challenge_invitations` from Phase 16 with these fields:

- `invitee_id`: **nullable** (link-based invitations don't target a
  specific user).
- `code`: ULID string, unique, populated for share-link invitations.
- `max_uses`: integer, nullable (unlimited).
- `uses`: integer, not null, default 0.
- `expires_at`: not null (default: challenge `ends_at`).

Add these fields via migration.

Constraint: exactly one of `invitee_id` or `code` is set.

## 2. Context additions

```elixir
def create_share_link(challenge, opts) do
  # opts: max_uses, expires_at
  # generates ULID, inserts invitation with code, invitee_id nil
end

def join_with_code(user, code) do
  # find invitation by code
  # validate not expired, not over max_uses, not already a member
  # increment uses in a Multi
  # insert challenge_membership
  # emit event
end

def revoke_link(challenge, code) do
  # transitions invitation to "expired"
end
```

## 3. Endpoints

- `POST /challenges/:id/share_links` — `{ max_uses?, expires_at? }`
  → `{ url, code, expires_at, max_uses, uses }`.
- `GET /challenges/:id/share_links` — list active links (members
  only).
- `DELETE /challenges/:id/share_links/:code` — revoke.
- `POST /challenges/join` — `{ code }` → 200 with challenge summary.
- `GET /challenges/preview?code=` — public preview (no auth) for the
  landing page.
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
