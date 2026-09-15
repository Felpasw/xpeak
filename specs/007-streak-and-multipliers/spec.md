# Phase 7 — Streak & Category Multipliers (SPEC)

## 1. Summary

Composed multiplier stack persisted on every check-in.

## 2. Data model

### 2.1. `check_ins` addition

- `multiplier_snapshot` `:map` (JSONB), not null, default `%{}`.
- Backfill: `%{ "streak" => 1.0, "category" => weight, "challenge"
  => 1.0, "total" => weight }` for existing rows (single migration).

## 3. Domain modules

### 3.1. `Xpeak.Progression.StreakTier`

```elixir
@tiers [
  {0, 2, 1.0},
  {3, 6, 1.1},
  {7, 13, 1.25},
  {14, 29, 1.5},
  {30, :infinity, 2.0}
]

@spec multiplier_for(non_neg_integer()) :: float()
def multiplier_for(streak_days) do
  Enum.find_value(@tiers, 1.0, fn {min, max, mult} ->
    streak_days >= min and (max == :infinity or streak_days <= max) && mult
  end)
end
```

### 3.2. `Xpeak.Progression.MultiplierResolver`

```elixir
@cap 3.0

@spec resolve(User.t(), Category.t(), Challenge.t() | nil) :: Multiplier.t()
def resolve(user, category, challenge) do
  s = StreakTier.multiplier_for(user.current_streak_days)
  c = Decimal.to_float(category.weight_multiplier)
  x = if challenge, do: Decimal.to_float(challenge.xp_multiplier), else: 1.0
  total = min(s * c * x, @cap)

  %Multiplier{streak: s, category: c, challenge: x, total: total}
end
```

### 3.3. `XpCalculator` update

```elixir
@spec compute(Category.t(), Multiplier.t()) :: pos_integer()
def compute(%Category{base_xp: base}, %Multiplier{total: t}) do
  round(base * t)
end
```

## 4. Check-in flow update

`Xpeak.CheckIns.create_check_in/2` now:

1. Loads user + category.
2. `MultiplierResolver.resolve(user, category, challenge)`.
3. `XpCalculator.compute(category, multiplier)`.
4. Inserts check-in with `multiplier_snapshot: Multiplier.to_map(m)`.
5. Bumps user's `xp` / `level` / `streak` (same as Phase 5).

Streak update runs **before** multiplier resolution when the current
check-in extends the streak? Or after?

**Decision:** streak is updated **before** multiplier resolution.
The streak on the day of the check-in reflects "including today", so
the multiplier for today's check-in already benefits from crossing a
tier boundary. Documented in the ADR.

## 5. API surface

### 5.1. `POST /check_ins` response addition

```json
"multiplier_snapshot": {
  "streak": 1.25,
  "category": 1.4,
  "challenge": 1.0,
  "total": 1.75
}
```

### 5.2. `GET /me` addition

```json
"active_multiplier": {
  "streak": 1.25,
  "category": null,
  "challenge": null,
  "total": 1.25
}
```

Category and challenge are null in `/me` because they only apply
per-check-in.

## 6. Mobile

### 6.1. Check-in success screen

- Show breakdown: `"+26 XP · base 15 × 🦵 1.4 × 🔥 7d 1.25"`.
- Small tap-target reveals the full formula if the user is curious.

### 6.2. Profile

- Small badge next to current streak: `"🔥 1.25× active"`.
- Shows next tier: `"7 more days for 1.5×"`.

## 7. Test plan

- `StreakTier` — pure test cases for each band boundary.
- `MultiplierResolver` — happy paths + cap saturation.
- `XpCalculator.compute/2` — arithmetic + rounding.
- Context integration: check-in with streak 7 + legs → `xp_earned:
  26`, snapshot matches.
- Regression: all Phase 5/6 tests still pass.

## 8. Non-goals

- Editable tier table (Phase 9).
- Group multiplier (Phase 19).
- Real challenge multiplier (Phase 15 — resolver already accepts a
  `Challenge.t()`, so wiring is trivial then).
