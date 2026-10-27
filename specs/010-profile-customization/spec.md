# Phase 10 — Profile Customization (SPEC)

> **Scope cut (2026-11-10) — "Background picker MVP"**
>
> First slice of Phase 10 ships under
> `XPK-34/felpa-profile-background` and covers only the
> **background** story. Avatar upload, banner uploads and the
> frames catalog stay in `tasks.md` as `[DEFERRED]` and ship in a
> follow-up branch. The roadmap already pivoted "banner" to
> "background" (a swappable **component** rendered from the
> mobile `backgrounds/registry.ts`, not an uploaded image), so the
> MVP persists only a short slug on the user row and does not
> introduce a `cosmetics` catalog table yet.
>
> **Slug is a strict enum on both sides.** The backend's
> `BackgroundSlug` C# enum and the shared TS `BackgroundSlug`
> enum both list the same three members (`Stars`, `Lightning`,
> `EtchedAccretion`); `PATCH /me/background` rejects any value
> outside the enum with 422. Adding a new background means
> adding to both enums + registering the component in
> `backgrounds/registry.ts` — three places, one atomic change.
> No "loose string + fallback" lenience.
>
> Everything in §1-§3 below describes the full Phase 10 vision.
> The MVP implements a minimal subset:
> `users.active_background_slug` + `PATCH /me/background` +
> mobile picker screen. See `tasks.md` for the implemented
> surface.

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
