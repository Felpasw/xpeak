# Phase 4 — Categories & Pure XP Domain (SPEC)

## 1. Summary

`categories` table + `Xpeak.Progression` context with pure XP and
level services. Zero HTTP endpoints, zero UI.

## 2. Data model

### 2.1. `categories` table

| Column                | Type    | Constraints                             |
|-----------------------|---------|-----------------------------------------|
| `id`                  | uuid    | PK                                      |
| `slug`                | citext  | not null, unique                        |
| `name`                | string  | not null                                |
| `icon`                | string  | not null (emoji or glyph key)           |
| `base_xp`             | integer | not null, default `10`, check `> 0`     |
| `weight_multiplier`   | decimal | not null, default `1.0`, check `0.5..2.5` |
| `is_system`           | boolean | not null, default `false`               |
| `active`              | boolean | not null, default `true`                |
| `inserted_at`/`updated_at` | utc_datetime_usec | not null           |

Indexes: `slug` unique.

### 2.2. Seed set (`is_system: true`)

| Slug        | Name        | Icon | Base XP | Weight |
|-------------|-------------|------|---------|--------|
| `legs`      | Legs        | 🦵   | 15      | 1.4    |
| `chest`     | Chest       | 💪   | 12      | 1.2    |
| `back`      | Back        | 🔙   | 12      | 1.2    |
| `shoulders` | Shoulders   | 🏋️   | 10      | 1.1    |
| `arms`      | Arms        | 💪   | 8       | 1.0    |
| `core`      | Core        | 🧘   | 8       | 0.9    |
| `running`   | Running     | 🏃   | 15      | 1.3    |
| `cycling`   | Cycling     | 🚴   | 12      | 1.2    |
| `mobility`  | Mobility    | 🧘   | 6       | 0.8    |
| `other`     | Other       | ❓   | 5       | 1.0    |

Values tunable in Phase 9. Final numbers to be reviewed with `plan.md`
§6 Q2.

## 3. Domain modules (pure)

### 3.1. `Xpeak.Progression.XpCalculator`

```elixir
@spec compute(input :: %{category: Category.t()}) :: pos_integer()
def compute(%{category: %Category{base_xp: base, weight_multiplier: w}}) do
  round(base * Decimal.to_float(w))
end
```

Rounding: `Kernel.round/1` (banker's rounding acceptable; document
the choice in the ADR).

### 3.2. `Xpeak.Progression.LevelCurve`

```elixir
@spec xp_for_level(non_neg_integer()) :: non_neg_integer()
def xp_for_level(0), do: 0
def xp_for_level(n) when n > 0, do: round(100 * :math.pow(n, 1.5))
```

Cumulative XP required to reach level `n` from level 0.

### 3.3. `Xpeak.Progression.LevelUpService`

```elixir
@spec level_for_xp(non_neg_integer()) :: non_neg_integer()
def level_for_xp(total_xp) do
  # binary search over LevelCurve.xp_for_level/1
end

@spec level_up?(prev_xp, new_xp) :: {leveled_up :: boolean(),
  new_level :: non_neg_integer(), levels_gained :: non_neg_integer()}
def level_up?(prev_xp, new_xp) do
  prev = level_for_xp(prev_xp)
  new = level_for_xp(new_xp)
  {new > prev, new, new - prev}
end
```

## 4. Context API

`Xpeak.Progression`:

- `list_categories/0` — active categories.
- `get_category_by_slug!/1`.
- `compute_xp/1` — delegates to `XpCalculator.compute/1`.
- `xp_for_level/1`, `level_for_xp/1`, `level_up?/2` — delegates to
  the pure modules above.

## 5. Test plan

- `test/xpeak/progression/category_test.exs` — schema constraints,
  weight bounds.
- `test/xpeak/progression/xp_calculator_test.exs` — property-based
  test with `stream_data`: for any category, `compute/1 > 0` and
  monotonic on `base_xp`.
- `test/xpeak/progression/level_curve_test.exs` — monotonic increase;
  values at levels 0, 1, 10, 100.
- `test/xpeak/progression/level_up_service_test.exs` — level_for_xp
  round-trip; `level_up?` edge cases (crossing a level boundary
  exactly).
- `test/xpeak/progression_test.exs` — context integration (loads
  seeded categories via `Repo.get_by/2`).

## 6. Non-goals

- No mutation of `users.xp` or `users.level` in this phase.
- No admin surface.
- No user-defined categories.
