# Phase 31 — Challenge Tiers (PLAN)

> Status: **planning only**. Companion to `spec.md` and `tasks.md`.
> Stack: C# 12 + ASP.NET Core 9 + EF Core 9 + Postgres 16 on the
> API side; Next.js 16 + Capacitor + TanStack Query + Vitest on
> the mobile side.

## 1. Goal

Let the owner of a challenge define **progression tiers** inside
that challenge — a list of `(min_level, title)` steps that each
member crosses as their challenge-scoped progress grows. The owner
of a 100-day training challenge can declare "level 10 ⇒ Aprendiz,
level 30 ⇒ Mago, level 60 ⇒ ArchMago", and every member sees their
current tier + the next one on the challenge detail.

This is **distinct from Phase 8 themed titles**, which name the
*user's global level* across the whole app (seeded Medieval and
Sci-Fi themes). Challenge tiers are owner-defined per challenge,
have no seed data, and live only inside the challenge detail.

## 2. Scope

**In scope**

- One new table (`challenge_tiers`) with one row per tier per
  challenge.
- Owner defines tiers at challenge creation (`POST /challenges`)
  as an optional list.
- `GET /challenges/:id` returns the ordered tier list and the
  caller's current tier (derived from
  `membership.progress.value`).
- Mobile create form: repeatable tier rows (title + min level),
  up to a hard cap (10 tiers).
- Mobile detail: progression ladder with current tier
  highlighted; the next tier shown as the horizon.
- Hard cap on the number of tiers per challenge: **10**.

**Out of scope (deferred by name)**

- Editing tiers after creation (no `PATCH /challenges/:id/tiers`).
  The owner either picks the final list up front or deletes +
  recreates the challenge. Keeps the service simple and the
  member-facing tier list stable.
- Artwork/avatar swap per tier — the equivalent of Phase 22
  "evolution forms" scoped to a challenge. Deferred, likely
  bundled with Phase 22 when it ships.
- Automatic insignia issuance when a tier is reached. That is
  Phase 18's territory and can be extended there later.
- Server-emitted "tier reached" event (notification / timeline).
  Current beta has no notification layer; add in a follow-up
  phase alongside Phase 24.
- Reward hooks (XP bonus, unlocks) when a tier is reached —
  separate scope.

## 3. Approach

### 3.1. Data

New table:

```
challenge_tiers (
  id             uuid PK,
  challenge_id   uuid FK -> challenges (cascade),
  min_level      integer not null,
  title          varchar(60) not null,
  created_at     timestamptz not null,
  updated_at     timestamptz not null
)
```

Index: `(challenge_id, min_level)` unique — doubles as the
ordered-read index. No `position` column; order is derived from
`min_level ASC`.

Constraints:
- `min_level > 0`
- `length(trim(title)) BETWEEN 1 AND 60`
- `(challenge_id, min_level)` unique — no two tiers share a floor.

### 3.2. Service

- `ChallengeService.CreateAsync` accepts `IReadOnlyList<CreateTierInput>?`
  and persists `ChallengeTier` rows in the same `SaveChangesAsync`
  as the challenge, owner membership and share-link invitation.
- `ChallengeService.GetForUserAsync` loads the ordered tier list
  and, when a membership exists, computes the caller's current
  tier (the row with the highest `min_level ≤ progress.value`).

### 3.3. API shape

`POST /challenges` request grows an optional field:

```json
{
  "title": "100 dias de treino",
  "targetLevel": 60,
  "tiers": [
    { "minLevel": 10, "title": "Aprendiz" },
    { "minLevel": 30, "title": "Mago" },
    { "minLevel": 60, "title": "ArchMago" }
  ],
  ...
}
```

`GET /challenges/:id` response grows two fields:

```json
{
  "challenge": { ... },
  "membership": { ... },
  "shareLink": { ... },
  "tiers": [
    { "id": "...", "minLevel": 10, "title": "Aprendiz" },
    { "id": "...", "minLevel": 30, "title": "Mago" },
    { "id": "...", "minLevel": 60, "title": "ArchMago" }
  ],
  "currentTier": { "id": "...", "minLevel": 30, "title": "Mago" }
}
```

`currentTier` is `null` when the caller is not a member or has not
reached the first tier yet.

### 3.4. Mobile

- `CreateChallengeForm`: new optional "Fases" section with a
  repeatable row — "Nivel mínimo" + "Título" + remove button —
  and an "Adicionar fase" affordance capped at 10 rows. Submitted
  only when at least one row is present and all rows pass
  validation.
- `ChallengeDetail`: new `<ChallengeDetailTiers>` organism that
  renders the ladder:
  ```
  ○ Lv 10 — Aprendiz   (alcançado)
  ● Lv 30 — Mago       (atual)
  ○ Lv 60 — ArchMago   (próximo)
  ```
  Only shown when the challenge has at least one tier.

### 3.5. Shared types

- New `ChallengeTier` interface in `@xpeak/shared/challenges/types`.
- `CreateChallengeRequest` grows `tiers?: CreateTierInput[]`.
- `GetChallengeResponse` grows `tiers: ChallengeTier[]` (empty
  array when none) and `currentTier: ChallengeTier | null`.

## 4. Dependencies

- Blocked by: XPK-32 (share-link MVP) — same `CreateAsync`
  surface and same mobile form file. Must merge first.
- Relates to: Phase 8 (user-global titles) — different concept
  but the UI vocabulary (title tiers) overlaps. Keep names
  distinct in code (`ChallengeTier` vs Phase 8's `LevelTitleTier`).
- Blocks: Phase 18 (challenge insignias) if we later want to
  award insignias on tier-reach — Phase 18 would need to call
  into the tier resolver.

## 5. Open questions

1. **Should the mobile form require tiers?** Default to optional
   (a challenge without tiers works exactly like today). Owner
   can skip the section entirely.
2. **Do we validate that at least one tier is `≤ targetLevel`?**
   Soft recommendation: yes, so a member who reaches the end has
   at least passed one tier. Enforced only in the validator with
   a warning, not a DB constraint.
3. **What happens to tiers when the owner ends the challenge
   early (`POST /challenges/:id/end`)?** Keep them — they're
   immutable context for the audit. Already covered by the
   migration FK semantics (cascade on delete; owner-end does not
   delete the challenge).
4. **Tier titles collision across challenges?** No uniqueness
   constraint across challenges — each owner names freely.
5. **Localization of tier titles?** Owner enters free text; no
   i18n layer for now.

## 6. Success criteria

- `POST /challenges` with `tiers: [...]` persists the rows and
  the create response carries them back.
- `POST /challenges` without `tiers` still works (empty array in
  the response).
- `GET /challenges/:id` for a member with progress between the
  first two tiers returns the first tier as `currentTier`.
- `GET /challenges/:id` for a non-member on a public challenge
  returns `tiers` populated but `currentTier: null`.
- Mobile form allows up to 10 tier rows and rejects
  `min_level ≤ 0` or blank title.
- Mobile detail renders the ladder with the correct current and
  next tier highlighted.
- `dotnet test apps/api.Tests` + `pnpm --filter @xpeak/mobile test`
  green.
