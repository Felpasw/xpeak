# Phase 22 — Evolution Forms (SPEC)

## 1. Data model

### 1.1. `theme_forms`

| Column                | Type    | Constraints                        |
|-----------------------|---------|------------------------------------|
| `id`                  | uuid    | PK                                 |
| `theme_id`            | uuid    | FK → themes                        |
| `tier_id`             | uuid    | FK → level_title_tiers             |
| `artwork_storage_key` | string  | not null                           |
| `mime_type`           | string  | not null                           |
| `is_animated`         | boolean | not null, default `false`          |
| `position`            | integer | not null                           |

Unique index: `(theme_id, tier_id)`.

## 2. Seed

For each of Medieval, Sci-Fi themes, seed forms for the 5 tiers.
Mark tiers 4 and 5 as `is_animated: true`.

## 3. Resolver

```elixir
defmodule Xpeak.Titles do
  def resolve_form(%User{} = user) do
    tier = current_tier(user)
    Repo.get_by(ThemeForm, theme_id: user.theme_id, tier_id: tier.id)
    || fallback_form(user.theme_id, tier)
  end
end
```

`fallback_form/2` walks down to earlier tiers if the current tier
has no form, then falls back to a bundled system placeholder.

## 4. Endpoints

### 4.1. `GET /me` additions

```json
"current_form": {
  "artwork_url": "...",
  "mime_type": "image/webp",
  "is_animated": true,
  "tier_title": "Mage"
},
"next_form": {
  "artwork_url": "...",
  "mime_type": "image/png",
  "is_animated": false,
  "tier_title": "War God",
  "min_level": 100
}
```

`next_form` may be null if user is at the terminal tier.

### 4.2. `GET /themes/:slug/forms`

Returns all forms in the theme with locked/unlocked flag for the
caller.

## 5. Mobile

### 5.1. Profile

- Form illustration prominent above (or replacing) the avatar
  section. Configurable per user? MVP: always shown, avatar
  becomes a smaller inset overlaid on the form.
- Level-up animation: cross-fade to the new form for 2 seconds
  with a "You unlocked <Title>" caption.

### 5.2. Forms gallery screen

- Grid: 5 rows (one per tier), all themes selectable at top.
- Each row: form artwork (blurred if locked), tier title, min level.

## 6. Test plan

- `resolve_form/1`: correct pick per tier + fallback path.
- Endpoint: `GET /me` includes both current and next.
- Gallery endpoint returns locked/unlocked per level.
- Mobile: form swaps on level up; gallery renders locked states.

## 7. Non-goals

- Custom form uploads.
- Marketplace / purchases.
- Form animations synced across users (feed rendering stays static
  thumbnail).
