# Phase 6 — Check-in Media (SPEC)

## 1. Summary

Mandatory media attachment on check-ins. Presigned PUT flow to
object storage. In-app playback via signed GET URLs.

## 2. Data model

### 2.1. `check_in_media` table

| Column                | Type    | Constraints                            |
|-----------------------|---------|----------------------------------------|
| `id`                  | uuid    | PK                                     |
| `check_in_id`         | uuid    | FK → `check_ins.id`, not null          |
| `kind`                | string  | not null, in `["photo", "video"]`      |
| `storage_key`         | string  | not null, unique                       |
| `mime_type`           | string  | not null                               |
| `byte_size`           | integer | not null, check `> 0`                  |
| `width`               | integer | nullable                               |
| `height`              | integer | nullable                               |
| `duration_seconds`    | integer | nullable (video only)                  |
| `position`            | integer | not null, default `0` (ordering)       |
| `inserted_at`/`updated_at` | utc_datetime_usec | not null          |

Indexes: `check_in_id`, `storage_key` unique.

## 3. Storage layer

### 3.1. `Xpeak.Storage` behaviour

```elixir
defmodule Xpeak.Storage do
  @callback presigned_put_url(key :: String.t(), opts :: keyword()) ::
    {:ok, url :: String.t()} | {:error, term()}

  @callback presigned_get_url(key :: String.t(), opts :: keyword()) ::
    {:ok, url :: String.t()} | {:error, term()}

  @callback delete(key :: String.t()) :: :ok | {:error, term()}
end
```

### 3.2. `S3Adapter`

- Config from `runtime.exs`: `AWS_ACCESS_KEY_ID`,
  `AWS_SECRET_ACCESS_KEY`, `AWS_REGION`, `MEDIA_BUCKET`,
  `S3_ENDPOINT_URL` (for R2/MinIO compatibility).
- Uses `ex_aws` + `ex_aws_s3`.
- TTLs: PUT 10 min, GET 5 min.

### 3.3. Testing

- `MockAdapter` for tests — returns deterministic fake URLs, records
  calls.
- Configured via `config :xpeak, :storage_adapter, MockAdapter` in
  `test.exs`.

## 4. Endpoints

### 4.1. `POST /check_ins/{id}/media/presign`

Auth required, must own the check-in.

Request:
```json
{
  "items": [
    { "kind": "photo", "mime_type": "image/jpeg", "byte_size": 2048000 },
    { "kind": "video", "mime_type": "video/mp4",  "byte_size": 50000000 }
  ]
}
```

Response `200`:
```json
{
  "presigned": [
    { "storage_key": "...", "url": "https://...", "expires_in": 600 },
    { "storage_key": "...", "url": "https://...", "expires_in": 600 }
  ]
}
```

Failures: `403` (not owner), `422` (kind/mime mismatch, size over
cap).

### 4.2. `POST /check_ins/{id}/media`

Auth required, must own the check-in.

Request:
```json
{
  "items": [
    { "storage_key": "...", "kind": "photo", "mime_type": "image/jpeg",
      "byte_size": 2048000, "width": 1920, "height": 1080, "position": 0 }
  ]
}
```

Response `201`: `{ "media": [ /* rows */ ] }`.

Failures: `422` (keys don't match a presign issued for this check-in
in the last 15 min), `403`.

### 4.3. `GET /check_ins/{id}/media/{media_id}/url`

Auth required, must have visibility. Returns `{ url, expires_in }` —
a fresh signed GET URL.

### 4.4. Tightening of `POST /check_ins`

- Add pre-condition: request must include a
  `media_intent_count >= 1`.
- Alternative: two-step flow. Step 1: `POST /check_ins` returns id.
  Step 2: presign + upload + attach. If step 2 fails, the check-in
  is soft-deleted after 15 min without media (Oban cron).
- Recommended: **two-step** with cron cleanup — simpler for the
  client, safer against half-broken uploads.

## 5. Mobile

### 5.1. Media picker

- `@capacitor/camera` for shoot-and-attach, `Capacitor.Filesystem`
  for gallery pick.
- Multi-select up to `max_media_per_check_in` (config).
- Preview grid with reorder handles.
- Per-item progress bar during upload.

### 5.2. Upload flow

1. User picks media → local previews rendered.
2. Tap "Log check-in" → `POST /check_ins` (creates the record with
   `media_intent_count`).
3. Client requests presigned URLs.
4. Parallel PUT uploads (up to 3 concurrent).
5. On all success → `POST /check_ins/{id}/media` with real metadata.
6. Level-up overlay (Phase 5) fires once media step returns.
7. On any failure → retry per-item, or cancel (soft-deletes
   check-in).

### 5.3. Playback

- Tap on media thumbnail → full-screen viewer.
- Photo: pinch to zoom, swipe to dismiss.
- Video: native `<video>` controls (Capacitor uses WKWebView / Chrome
  WebView).

## 6. Test plan

### API
- Behaviour: mock adapter returns fake URLs; controller calls it
  the right number of times.
- Ownership: presign/attach as another user → 403.
- Size cap: over-cap request → 422.
- Attach with mismatched keys → 422.
- Cron: Oban job soft-deletes check-ins without media after 15 min
  (Oban.Testing helpers).
- `POST /check_ins` without media → still creates the record, but a
  `GET /check_ins/{id}` before media attach flags it as pending.

### Mobile
- Picker renders selected items.
- Upload flow with mocked adapter: happy path all succeed, failure
  path shows retry.
- Full-screen viewer opens on thumbnail tap; video plays.

## 7. Non-goals

- Client-side compression (Phase 25).
- Resumable/chunked upload for very large videos (nice-to-have,
  future).
- Server-side transcode (definitely later).
- Content moderation.
