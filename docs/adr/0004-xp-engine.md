# ADR 0004 — XP engine (curve, rounding, rule extraction, group scoping)

- **Status:** Accepted
- **Date:** 2026-09-23
- **Context:** Phase 4 (`specs/004-categories-and-xp-domain/`),
  shipped on branch `XPK-15/felpa-xp-domain`.

## Context

Phase 4 introduces the pure arithmetic and the schema every future
XP-earning feature will lean on: check-in creation (Phase 5), streak
and category multipliers (Phase 7), themed titles (Phase 8), editable
XP config (Phase 9), all the challenge and social phases (12–20), and
the yearly recap (Phase 21). Everything downstream reads XP totals,
compares against a level curve, and stores per-check-in scoring
breakdowns. We wanted to lock in the shape before that cascade
started firing so we don't have to migrate later.

Four things needed a call:

1. What curve maps a cumulative XP total to a level?
2. What rounding rule turns `base × weight` into an integer?
3. Where do the scoring numbers live — on the category, or as their
   own entity?
4. Are categories globally shared or scoped to a group?

Plus two derivative decisions that fell out of (3) and (4):

5. What does the module export publicly?
6. Do we ship seed data with the engine?

## Decision

### 1. Level curve — `XpForLevel(n) = round(100 * n^1.5)`

Cumulative XP required to reach level `n` from level 0. Anchored at
`XpForLevel(0) = 0` and `XpForLevel(1) = 100`.

Reference values (locked in tests):

| Level | Cumulative XP |
|-------|---------------|
| 0     | 0             |
| 1     | 100           |
| 5     | 1 118         |
| 10    | 3 162         |
| 50    | 35 355        |
| 100   | 100 000       |

**Alternatives considered:**

- **Linear (`100 * n`)** — punishes late-game feel; level 100 costs
  only 10k XP and every level takes the same effort. No sense of
  progression tightening.
- **Quadratic (`100 * n^2`)** — steep late-game (L100 = 1M XP);
  discouraging for casual users who plateau by L30.
- **Fibonacci-like** — hard to reason about, hard to communicate
  ("what does level 42 cost?"), no closed form for the reverse
  query.
- **Piecewise** — flexible but pushes complexity to the config layer.
  We defer that flexibility to Phase 9 (editable config); day-one
  simplicity wins.

`n^1.5` gives a **√n** growth rate on the marginal cost between
levels — cheap and rewarding early, gently more expensive as you
climb, without any brick-wall late-game. Fits the "casual → engaged
gym-goer" audience.

**Tunability:** the constant `100` and the exponent `1.5` are
currently hard-coded in `LevelCurve.XpForLevel`. Phase 9 moves them
to a `xp_config` row (or a versioned `level_curves` table) with an
admin surface + audit log, and `LevelCurve` becomes a thin wrapper
around a config lookup. The shape of every downstream consumer
(`LevelUpService`, `IProgressionService`) stays identical.

### 2. Rounding — `MidpointRounding.AwayFromZero`

.NET's default `Math.Round(x)` uses **banker's rounding**
(`MidpointRounding.ToEven`) — `4.5 → 4`, `5.5 → 6`. Statistically
sound, but surprising to end users. Example:

- Rule `{ BaseXp = 5, WeightMultiplier = 0.9 }` → raw = 4.5.
  Banker's: **4**. AwayFromZero: **5**.
- The user sees "0.9 × 5 = 4.5" and expects **5**. Banker's rounding
  makes the app look buggy.

Away-from-zero matches the arithmetic every non-statistician was
taught in school. The trade-off (a tiny statistical bias) is
irrelevant at check-in scale where each score is <100 XP.

Locked in `XpCalculatorTests.Compute_rounds_half_away_from_zero_not_banker`.

### 3. Rule extraction — `xp_rules` as its own entity

`Category` doesn't carry `base_xp` and `weight_multiplier` inline.
It has an `xp_rule_id` FK into `xp_rules`, which owns the numbers.

**Why not inline them on `Category`?**

Because the same two-field shape (`BaseXp`, `WeightMultiplier`) will
also apply to:

- **Group-level bonuses (Phase 7)** — a group can carry its own
  multiplier for members ("this crew earns 1.1× base").
- **Challenge modifiers (Phase 15+)** — a challenge attaches a
  multiplier that decays over time or activates on a threshold.
- **User overrides (Phase 9)** — an admin can bump a specific user's
  XP curve inside bounded ranges.

Inlining the fields on `Category` would force us to replicate the
same two columns on `Group`, `Challenge` and `UserXpOverride` (or
wherever the future consumers land). Extraction removes that
duplication and gives Phase 9 a single, versionable, auditable
target for the "editable XP" story.

Two categories with the same numbers (say `chest` and `back`, both
`{ 12, 1.2 }`) can share the same row — cheap dedup on the reference
values, and updates propagate everywhere the rule is used.

**Trade-off:** one extra JOIN on every check-in query. Indexed FK,
cost is negligible; measured under 0.5 ms on the local Postgres
container.

### 4. Group scoping — `categories.group_id NOT NULL`

Every category is owned by a group. There is one root group
(`GroupIds.Global = 00000000-0000-0000-0000-000000000001`, `is_root =
true`) every user joins on registration; sub-groups will arrive in
Phase 12+ but the schema already accommodates them today.

**Why not a flat `is_system` boolean?**

The original spec proposed `is_system = true/false` on `Category`
itself, with 10 seeded system rows. That worked as long as
categories were universal. But the product intent — validated in
review during T-004-01 — is that different groups will run different
rules ("the escaladores crew values `legs` at 20 base_xp, the yoga
crew doesn't have `legs` at all"). A flat `is_system` boolean
doesn't express that.

Owner-based scoping does. The root group is the "everything is
universal" case today; sub-groups later just register categories
with their own `group_id`, without any special-case logic on the
consumers.

**Membership hook:** because every user needs a home group,
`RegisterEndpoint` and `GoogleAccountLinker.FindOrCreateAsync` call
`IGroupService.AddUserToGlobalAsync` right after
`UserManager.CreateAsync`. The service is idempotent (early-return
on existing membership) so retries after a partial failure are safe.

### 5. Public surface — only `IGroupService` and `IProgressionService`

The Groups module exports **only** `IGroupService` via `AddGroups()`.
The Progression module exports **only** `IProgressionService` via
`AddProgression()`. Everything else — `GroupService`,
`ProgressionService`, `CategoryRepository`, the entities'
persistence layer — is internal to its module.

Consumers (check-in handler in Phase 5, rankings, LLM tools) always
depend on the interface, never on the concrete class or the
repository. That keeps the composition seam tight and lets us
substitute implementations later (a caching decorator, a snapshot
reader that hits a materialized view, an in-memory fake for tests)
without churn on the callers.

The pure math (`XpCalculator`, `LevelCurve`, `LevelUpService`,
`LevelUpResult`) lives in a separate `Xpeak.Api.Xp` namespace with
**no module** and **no DI registration** — see §7 for why.

### 6. No seed data

The engine ships **without any category rows**. The migration
creates `groups`, `xp_rules`, `categories`, `group_memberships` and
inserts a single row into `groups` (the Global root). Nothing else.

**Why not the 10 opinionated seeds (legs, chest, back, …) the
original spec proposed?**

- Different groups will want different sets. A universal seed becomes
  either untouchable (defeats the point) or migrates weirdly when
  sub-groups clone from it.
- The seed choice is a **product decision**, not an engine decision.
  The engine should ship neutral and let content flow in via the
  Phase 9 admin surface or Phase 17 group-owner CRUD.
- Fewer moving parts today = fewer things to migrate when Phase 9
  arrives with the real editable-XP story.

Phase 5 (check-in) will fail cleanly if you try to check in against
a category that doesn't exist yet — which is the correct behaviour.
The mobile UI will surface "no categories yet, ask your group owner
to add some" until content lands.

### 7. Xp namespace split — math lives outside Progression

`XpCalculator`, `LevelCurve`, `LevelUpService` and `LevelUpResult`
sit in `Xpeak.Api.Xp` — **not** in `Xpeak.Api.Progression.Services`
where they originally landed. The split happened in a follow-up
refactor after review flagged that the arithmetic was too coupled
to the persistence/orchestration layer semantically.

**Why the split:**

- `Xp` is truly reusable — every future compound multiplier
  (streak × group × challenge in Phase 7, challenge modifiers in
  Phase 15+) is math with the same shape. Keeping the math in its
  own namespace means Phase 7's `MultiplierComposer` (or however it
  ends up named) lands right next to `XpCalculator` instead of
  bloating `Progression/Services/`.
- **Dependency direction is one-way.** `Progression` imports `Xp`;
  `Xp` imports nothing from `Progression`. `XpCalculator` needs
  `XpRule` as its input type — `XpRule` currently lives in
  `Xpeak.Api.Progression.Entities`, which technically inverts the
  cleaner direction. Not worth extracting `XpRule` into `Xp` today
  (the entity has DB concerns via EF Core mapping), but the note is
  captured here in case a future refactor wants the arithmetic
  namespace to be entirely leaf-free.
- **No module, no DI.** Everything in `Xp` is `static`, so there's
  nothing to register. `Program.cs` doesn't call `.AddXp()`. That
  matches the "Xp is a math primitive" mental model — you don't
  register `Math` or `System.Random`, you just call it.
- **Tests move too.** `apps/api.Tests/Xp/` mirrors the source
  layout — `XpCalculatorTests`, `LevelCurveTests`,
  `LevelUpServiceTests`. `apps/api.Tests/Progression/` keeps
  everything that needs Testcontainers (persistence, repository,
  the end-to-end `ProgressionServiceTests`).

**Trade-off:** one more folder in `apps/api/src/`. Cheap price for
the clearer semantic boundary.

## Consequences

**Now:**

- 4 tables (`groups`, `group_memberships`, `xp_rules`, `categories`)
  + 1 seeded row + 2 module registrations
  (`.AddGroups().AddProgression()`) + auth-side membership hook.
- 3 pure static services (`XpCalculator`, `LevelCurve`,
  `LevelUpService`) with 30+ tests, including FsCheck property tests
  (round-trip, monotonicity, positivity).
- 122 tests total in `apps/api.Tests` — every code path new to Phase
  4 covered, every pre-existing test still green.

**Downstream unlocks:**

- **Phase 5 (check-in):** `IProgressionService.ComputeXp(rule)` +
  `EvaluateLevelUp(prevXp, newXp)` inside the check-in transaction;
  persist the result and (later) the multiplier snapshot.
- **Phase 7 (streak/group multipliers):** compose additional
  multipliers on top of `XpCalculator` output; store the full
  breakdown on `check_ins.multiplier_snapshot` (JSONB).
- **Phase 8 (themed titles):** map levels to titles via a
  `level_title_tiers` table that reads the curve.
- **Phase 9 (editable config):** move `xp_rules` values + the
  `LevelCurve` constants to persisted config with an admin surface.
- **Phase 12+ (sub-groups):** register `group_memberships` rows via
  the group-invitation flow; categories with `group_id != Global`
  become the norm.

**Trade-offs accepted:**

- Extra JOIN on category → rule lookup (negligible at the current
  scale; can be denormalized on hot paths later if needed).
- No day-one content — mobile onboarding will need to explain the
  empty state until Phase 9 or Phase 17 provides a CRUD.
- Rounding differs from .NET default — must be noted in any future
  ADR / dev doc that discusses arithmetic elsewhere in the codebase.

## Related

- `specs/004-categories-and-xp-domain/plan.md`
- `specs/004-categories-and-xp-domain/spec.md`
- `specs/004-categories-and-xp-domain/tasks.md`
- Roadmap Phase 4, Phase 7, Phase 9, Phase 12, Phase 17.
