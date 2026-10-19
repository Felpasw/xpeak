# ADR 0030 — Drop Groups and Categories (collapse the social model into Challenges)

- **Status:** Proposed
- **Date:** 2026-10-14
- **Context:** Phase 30 (`specs/030-drop-categories-and-groups/`),
  on branch `XPK-31/felpa-drop-categories-and-groups`.
- **Supersedes (in part):** parts of ADR 0004 (`Category` /
  `XpRule` separation), parts of ADR 0005 (`group_id`
  denormalization, group-scoped streak).

## Context

Phases 4 and 5 shipped a schema sized for a hierarchical social
model that never arrived:

- `groups` + `group_memberships` + `group_configs` were reserved
  for "sub-groups land in Phase 12+". Phase 12 (Friends) ended up
  modelling the "persistent crew" use case through the
  friendship graph instead — a direct user ↔ user edge, not a
  container entity. Only the seeded `Global` group row exists in
  production; sub-group CRUD has no owner on the roadmap.
- `categories` + `xp_rules` modelled per-category XP weights.
  Phase 15 (Solo Challenges) removed the `challenge_categories`
  whitelist, which was the last gameplay surface that cared which
  category a check-in belonged to. The only remaining job of
  categories is to force the user to classify a workout on the
  check-in form.

Phase 15 also shipped Challenges as the actual social unit of
the product (owner + membership + progress + state machine). The
Challenge entity's own comment admits this: _"Every challenge is
inherently group-shaped; the distinction between 'solo', 'friend'
and 'group' is just MemberCap"_. Challenges observe check-ins
through `(user_id, performed_at BETWEEN starts_at AND ends_at)`,
with no reference to `groups`.

So the model today has two parallel social frames:

1. **Group** — the hierarchical container that was reserved but
   never shipped.
2. **Challenge** — the time-boxed social unit that actually
   ships.

Carrying both forward keeps asking every reviewer the same
question: _"what are groups for?"_ The honest answer is _"nothing
the product uses today, and nothing on the roadmap specifically
needs them"_.

## Decision

We **delete both `Category` and `Group`** (and everything that
hangs off either) in one phase:

- Drop the five tables: `categories`, `xp_rules`,
  `group_memberships`, `group_configs`, `groups`.
- Drop the two check-in columns: `category_id`, `group_id`.
- Delete the Progression and Groups C# modules except for the
  pure XP arithmetic (`XpCalculator`, `LevelCurve`,
  `LevelUpService`), which stays in `Xpeak.Api.Xp`.
- Streak becomes a per-user derivation on `check_ins`. The
  weekly-mode machinery (`StreakConfig` with `Mode`,
  `RequiredDaysPerWeek`, `WeekStart`) that was schema-ready but
  never shipped goes away. `StreakMode` the enum stays for
  Phase 11 (streak medals) to reuse the daily tier labels.
- XP amount per check-in becomes
  `XpCalculator.CheckInXp = 10` — a `const int` matching the
  current seeded `base_xp = 10`. Phase 9 ("editable XP config")
  picks the real home when it ships (likely an `app_settings`
  key-value table).

Challenges stay untouched. Their observation of check-ins keeps
working because the SQL already filtered by `user_id` +
`performed_at`, never by `group_id` or `category_id`.

## Alternatives considered

### A. Rename `Group` to `Challenge` (merge the two entities)

The product-level insight is right ("challenges are groups with a
deadline"), but the entities carry different fields —
`Challenge` has `starts_at`, `ends_at`, `target_level`, `state`,
`visibility`, `member_cap`, none of which fit the Global
container. Making `Global` a special Challenge would require
nullable time-box fields and a magic state, which trades a
vestigial entity for a special-case entity. Rejected.

### B. Keep `Group` and `Category`, document them as internal

Zero code change, keeps the schema, moves the problem to a
comment. Every future reviewer still asks the same question
and every future feature still has to decide whether to use
Group or Challenge for scoping. Rejected.

### C. Keep `Group` but delete `Category` only

The Phase 30 that was originally drafted. The user feedback
during planning killed this: without categories, `Group` loses
its last concrete responsibility (owning categories). It
degrades to "the thing that owns the streak", which the user
can do directly. The in-review version of the plan proposed
`XpRuleIds.Base` for this and was called out as pretending there
is more complexity than there is. Rejected.

### D. Collapse now, revisit if we ever need multi-tenant / gym mode

The chosen direction. If "gym multi-tenant" (every academia is
an org, students belong to orgs, rankings scoped to orgs) ever
becomes a real product bet, that is a dedicated phase with its
own ADR and its own entity. It would not be well-served by the
current `Group` shape anyway. Delete now, re-add if ever
needed.

## Consequences

### Positive

- Schema loses five tables and two FK-carrying columns.
- `GLOBAL_GROUP_ID` disappears from the mobile code.
- Every future reviewer sees exactly one social unit
  (`Challenge`) and one scoring surface
  (`XpCalculator.CheckInXp`).
- Streak logic simplifies — one query shape (`check_ins WHERE
  user_id = ?`), one streak number per user.
- The check-in form loses a required field, reducing friction.

### Negative

- Phase 19 (`specs/019-group-bonuses-and-streak/`) is now
  stale. The ADR patches the spec with a note so the next
  reviewer knows the phase will be re-scoped to challenges, but
  no code work happens for Phase 19 as part of Phase 30.
- Phase 9 inherits the question of where editable XP config
  lives. Until it ships, every check-in pays exactly 10 XP;
  tuning requires a code change.
- If "gym multi-tenant" ever lands, we rebuild the Group concept
  from scratch rather than extending the vestigial one.
  Acceptable — the vestigial one was wrong-shaped anyway.
- The migration is destructive. Any existing seed data in
  `categories` / `xp_rules` / `groups` is lost. Acceptable —
  production has one row in each and no user content yet.

## Open follow-ups

- **Phase 9** — decide where editable XP config lives.
- **Phase 11** — streak-medal thresholds inherit the per-user
  streak directly; the medal entity doesn't need `group_id`.
- **Phase 19** — re-scope to challenge bonuses / challenge
  streak when picked up.
- **Possible future ADR** — multi-tenant / gym mode, if the
  product bet materialises.
