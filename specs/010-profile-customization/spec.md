# Phase 10 — Profile Customization (SPEC)

## 1. Data model

### 1.1. `users` additions

- `avatar_storage_key` (string, nullable) — replaces the free-form
  `avatar_url` from Phase 3 (kept as fallback for Google-imported
  avatars).
- `banner_storage_key` (string, nullable).
- `banner_mime_type` (string, nullable).
- `frame_slug` (citext, nullable, FK → `frames.slug`).

### 1.2. `frames` table

| Column        | Type    | Constraints                        |
|---------------|---------|------------------------------------|
| `id`          | uuid    | PK                                 |
| `slug`        | citext  | not null, unique                   |
| `name`        | string  | not null                           |
| `storage_key` | string  | not null (or bundled asset key)    |
| `is_system`   | boolean | not null, default `true`           |
| `active`      | boolean | not null, default `true`           |

Seeded set: 10 frames (`plain`, `bronze`, `silver`, `gold`,
`fire`, `neon`, `wreath`, `dragon`, `angel`, `void`).

## 2. Endpoints

### 2.1. `PATCH /me/profile`

Request (partial):
```json
{
  "avatar_storage_key": "...",
  "avatar_mime_type": "image/gif",
  "banner_storage_key": "...",
  "banner_mime_type": "image/webp",
  "frame_slug": "gold"
}
```

Response `200`: updated `/me` payload.

Failures: `422` (unknown frame slug, mime type not in allow-list).

### 2.2. Avatar/banner presign

Reuses Phase 6 storage: `POST /me/media/presign` with
`{ kind, mime_type, byte_size, purpose: "avatar" | "banner" }` →
returns storage_key + presigned PUT URL.

Size caps: avatar 5 MB, banner 10 MB.

### 2.3. `GET /frames`

Public list of active frames with signed GET URLs for previews.

## 3. Mobile

### 3.1. Profile edit screen

- Avatar tile → tap → picker (photo or gif) → preview → save.
- Banner tile → same.
- Frame section → grid of frame previews → tap to select →
  overlaid on the avatar preview immediately.
- "Save" button commits `PATCH /me/profile`.

### 3.2. Profile card update

- Renders avatar (animated if GIF) with the frame overlay.
- Banner behind the card.

## 4. Test plan

- `Profile.update_profile/2`:
  - Happy path.
  - Unknown frame → error.
  - Nulling a slot (`avatar_storage_key: nil`) → falls back to
    default.
- `PATCH /me/profile` controller.
- Mobile: preview updates on selection; save persists; profile card
  shows the frame + avatar.

## 5. Non-goals

- Cropping / editing tools.
- Multiple avatar slots.
- Frame animations (frames themselves could be animated later, but
  MVP frames are static PNGs).
