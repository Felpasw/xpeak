# ADR 0031 — Challenge Tiers

- **Status:** Proposed
- **Date:** 2026-10-21
- **Context:** Phase 31 (`specs/031-challenge-tiers/`), on branch
  `XPK-33/felpa-challenge-tiers`.
- **Related:** ADR 0017 (share-link MVP) — builds on the same
  `Challenge` + `ChallengeMembership` surface and the same
  `ChallengeService.CreateAsync` / `GetForUserAsync` entry points.
  Also distinct from Phase 8 (themed titles for the user's global
  level), which lands later.

## Context

Phase 15 (Solo Challenges) and Phase 17 (share-link MVP) shipped
Challenges as the social unit of the product and gave the owner a
way to invite others via a link. What members see inside a
challenge, though, is still a single number: their challenge-scoped
`progress.value` on their way to the challenge's `targetLevel`.

The product request is **progression tiers inside a single
challenge**: the owner of a 100-day training challenge wants to
declare "level 10 ⇒ Aprendiz, level 30 ⇒ Mago, level 60 ⇒
ArchMago", and every member sees their current tier highlighted on
the challenge detail. This is distinct from the per-user titles
Phase 8 ships against the user's global XP (Medieval / Sci-Fi
themes); the ask here is per-challenge, owner-defined, with no
global tie.

## Decision

We ship a dedicated `challenge_tiers` table owned by the challenge,
declared at creation only, and resolved into a `currentTier` at
read time by a pure helper. The shape is deliberately minimal — no
artwork, no insignia, no edit-after-create.

### Data

`challenge_tiers`:

| Column         | Notes                                                    |
|----------------|----------------------------------------------------------|
| `id`           | PK (uuid).                                               |
| `challenge_id` | FK → `challenges`, cascade on delete.                    |
| `min_level`    | not null, `> 0` (check), `≤ 999` at the validator.       |
| `title`        | varchar(60), `length(trim(title)) BETWEEN 1 AND 60`.     |
| `created_at`   | timestamptz.                                             |
| `updated_at`   | timestamptz.                                             |

Indexes: unique `(challenge_id, min_level)` — doubles as the
ordered-read index. **No `position` column** — order is derived
from `min_level ASC`.

Hard cap at the service: **10 tiers per challenge**. Enforced via
`TooManyTiersException` → HTTP 422.

### Domain

- `ChallengeTier` entity, 5 fields plus timestamps.
- `ChallengeTierResolver.ResolveCurrent(IReadOnlyList<ChallengeTier>, int)`
  — pure static. Loops until `MinLevel > progressValue` and returns
  the last tier that passed; `null` for an empty list or progress
  below the first floor.

### Service wiring

- `ChallengeService.CreateAsync` accepts
  `IReadOnlyList<CreateChallengeTierInput>?`:
  - Null / empty ⇒ no rows persisted. The response ships an empty
    `Tiers` list.
  - Non-empty ⇒ validate (`count ≤ 10`, no duplicate `MinLevel`),
    normalise titles with `Trim()`, sort by `MinLevel`, insert
    all in the same `SaveChangesAsync` as the challenge, owner
    membership and share-link invitation.
- `ChallengeService.GetForUserAsync` loads tiers ordered by
  `MinLevel` and resolves `CurrentTier` via the resolver when a
  membership exists.
- `ChallengeDetail` grows `Tiers?` and `CurrentTier?` fields;
  `ListForUserAsync` leaves them `null` (list surface doesn't need
  the ladder).

### HTTP

- `POST /challenges` request grows an optional `tiers` array
  (`{ minLevel, title }`).
- `CreateChallengeRequestValidator` guards count ≤ 10, no duplicate
  `minLevel`, each `minLevel` in `[1, 999]`, each `title` trimmed
  to 1..60 chars.
- `POST /challenges` response and `GET /challenges/:id` response
  ship `tiers` (always populated, empty when none). The GET
  response also ships a nullable `currentTier`.

### Mobile

- `CreateChallengeForm` grows a "Fases (opcional)" section backed
  by `useFieldArray`, capped at 10 rows. Form state, schema and
  submit live in `useCreateChallengeForm` to match the
  `useCheckinForm` convention; the component stays presentational.
  The submit omits `tiers` entirely when zero rows.
- `<ChallengeDetailTiers>` renders the ladder on the challenge
  detail. The current tier is matched by id once at the component
  root and the per-row status (`reached` / `current` / `next` /
  `upcoming`) is derived by a tiny pure helper over the row index
  and the current index. The organism returns `null` when the tier
  list is empty so the detail page can mount it unconditionally.
- The detail page destructures `tiers` + `currentTier` from the
  GET payload — no sort, no reshape, no client-side resolver.

## Alternatives considered

### A. Reuse Phase 8 themed titles inside challenges

Hook each challenge to a `theme_id` and let the member's
challenge-scoped progress pick a tier from the theme's seeded
Medieval/Sci-Fi tiers. Rejected because:

- Phase 8 isn't shipped yet, so this would be gated on a bigger
  dependency.
- The product ask is **owner-defined per challenge**, not
  "pick-from-catalogue". The seeded themes model a different
  concept (user-wide level).
- Even if we eventually offer "seed from a theme", that's a
  convenience over the primitive we're building here. Build the
  primitive first.

### B. `position` column instead of deriving order from `min_level`

Original draft of the spec. Rejected during T-031-01: the index on
`min_level` is already unique per challenge, so `ORDER BY
min_level ASC` is already cheap. A separate `position` adds a
column to maintain, a second ordering invariant to break
(position=0 at min_level=30 is a legitimate bug), and zero query
cost reduction. Dropping it also simplifies the HTTP response — no
mystery integer on each wire tier.

### C. Service-less resolution: do everything in SQL

Alternative was to compute `currentTier` with a correlated
subquery on the GET: `SELECT ... FROM challenge_tiers WHERE
challenge_id = ? AND min_level ≤ ? ORDER BY min_level DESC LIMIT
1`. Rejected because:

- The resolver is a 10-line pure function tested without a DB.
- The service already needs the ordered tier list for the response
  payload. Resolving once in memory over that list is strictly less
  work than a second query.
- Easier to reuse the resolver elsewhere (e.g. tests, future UI
  previews, insignia emission when Phase 18 extends).

### D. Edit tiers after creation (`PUT /challenges/:id/tiers`)

Attractive — owners might want to tweak after seeing early
feedback. Rejected for the MVP because:

- Mutating a tier list that members have already crossed opens
  three ambiguities per membership: do they lose a reached tier?
  Does the current tier snapshot stick or jump to the new
  resolution? What about future joiners seeing a different ladder
  than their peers?
- Shipping without edit forces owners to think up front — the beta
  is a good forcing function to learn what the real edit UX needs
  to be.
- When signal exists, add `PUT /challenges/:id/tiers` as a full
  replace with explicit "membership snapshot vs re-resolve"
  semantics documented in its own ADR.

### E. Per-tier artwork / avatar swap

The equivalent of Phase 22 (evolution forms) scoped to a challenge.
Rejected because Phase 22 isn't shipped and the data model it
needs (`theme_forms` keyed by `tier_id`) is bigger than the beta
needs. A follow-up extension in Phase 22 can key its forms on
`challenge_tiers.id` instead of global theme tiers.

### F. Automatic insignia issuance on tier reach

Phase 18 (challenge insignias) ships insignias on challenge
completion. Awarding an insignia per tier reach would blur the two
mechanics. Rejected pending Phase 18 — once insignias exist, a
clean extension is `user_insignias (user_id, challenge_id,
tier_id)` with an event emitter on the resolver. Not scope for 31.

### G. Push notification on tier reach

Nice-to-have for engagement but blocked on Phase 24 (push). The
resolver running on every GET makes "detect tier change" trivially
derivable at the client side if we ever need an in-app toast
before Phase 24 ships; for now, the UI just shows the current tier
on load.

## Consequences

### Positive

- One table, two constraints, one index. The schema debt is
  bounded.
- The resolver is a pure function, tested without a DB — the
  non-trivial logic in this phase runs in milliseconds.
- The create form and the detail page use the same tier shape with
  no client-side reshape (per the "backend owns the shape" rule).
- Owners get a visible progression ladder in the beta without
  blocking on Phase 8 (themes), Phase 18 (insignias), Phase 22
  (artwork) or Phase 24 (push).
- Future extensions (insignia on tier reach, artwork per tier,
  push on tier change) all key off `challenge_tiers.id`, so the
  table is the right place to add FKs when those phases ship.

### Negative

- No edit after creation. Owners who declare a bad ladder have to
  delete the challenge or live with it until an edit endpoint
  ships.
- No visual richness yet — the tier row is text + a dot + a pill.
  Phase 22's evolution forms will be the right place to add
  per-tier artwork; the current UI is a floor, not a ceiling.
- `currentTier` is derived at read time. On a hot path with many
  tiers (we cap at 10, so trivial), the N tier objects are
  materialised on every GET. If the beta surfaces a hotter path we
  cache; today the cost is noise.
- `position` is derived at read time too (array index on the
  mobile). If we ever expose a "tier at position N" query path on
  the backend, we'd compute it ad-hoc or add a view. No signal for
  that today.
- Tier titles are free text per challenge — no cross-challenge
  normalisation, no i18n layer. Owners can enter inconsistent or
  silly titles; this is on them for the beta.

## Open follow-ups

- **Phase 18 extension** — when challenge insignias ship, consider
  awarding an insignia on tier reach, keyed by
  `(user_id, challenge_id, tier_id)`.
- **Phase 22 extension** — when evolution forms ship, allow a tier
  to reference a `theme_form_id` for a member avatar swap scoped to
  the challenge.
- **Phase 24 extension** — "You just reached Mago in <challenge>"
  becomes a push category.
- **Tier edit endpoint** — if beta feedback asks for it, add a
  dedicated `PUT /challenges/{id}/tiers` that replaces the full
  list and decides whether memberships snapshot the pre-edit
  resolution or jump to the new one. Deferred until there is
  signal.
- **Soft recommendation that at least one tier ≤ targetLevel** —
  not enforced today. Could land as a validator warning (not a
  hard error) when a tier list is submitted and the top tier is
  above `targetLevel`.
