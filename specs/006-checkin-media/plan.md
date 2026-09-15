# Phase 6 — Check-in Media

> Status: **planning only**. Companion to `spec.md` and `tasks.md`.

## 1. Goal

Make media attachment on check-ins a first-class requirement.
Multiple photos or videos per check-in, uploaded from the mobile
client to object storage, referenced from the API, and rendered
back on the profile and (eventually) the feed.

## 2. Scope

**In scope**
- `check_in_media` schema linking media items to check-ins.
- Object storage integration (provider TBD) with a service-agnostic
  wrapper.
- Upload flow: mobile requests a presigned URL → uploads binary
  directly to storage → tells the API "these keys are attached to
  this check-in".
- Backend enforces: at least 1 media per check-in (raises Phase 5
  optional → Phase 6 required).
- Mobile: camera/roll picker (multi-select), preview grid, upload
  progress, retry on failure.
- Playback in-app (photo full-screen, video with native controls).

**Out of scope**
- Client-side media compression / optimization (Phase 25).
- Moderation queue (deferred, `ideas.md` §2.5.4).
- Deep EXIF stripping / face-blur (deferred).
- Feed rendering (Phase 13).

## 3. Approach

- Provider-agnostic via a `Xpeak.Storage` behaviour + one adapter
  per real provider. First adapter: `S3` (works with AWS S3,
  Cloudflare R2, MinIO).
- Presigned PUT URLs (short TTL, 10 min). Bucket policy: private,
  server-side encryption.
- API stores only `storage_key`, `kind` (photo/video), `mime_type`,
  `byte_size`, `width`, `height`, `duration_seconds` (video only).
- Media rows created **after** the upload succeeds (client posts
  the metadata back). No orphan rows for aborted uploads.
- Signed GET URLs generated on demand for playback (5 min TTL).

## 4. Artifacts

- `priv/repo/migrations/*_create_check_in_media.exs`.
- `lib/xpeak/storage.ex` — behaviour + facade.
- `lib/xpeak/storage/s3_adapter.ex`.
- `lib/xpeak/check_ins/media.ex` — schema.
- `lib/xpeak_web/controllers/media_controller.ex` — presigned URL +
  metadata attach.
- Update `POST /check_ins` to require at least one media reference.
- Mobile: `lib/media/uploader.ts` + `MediaPicker` component +
  updated check-in screen.

## 5. Dependencies

- Blocked by: Phase 5 (check-in exists).
- Blocks: Phase 13 (feed rendering), Phase 20 (trophy assets reuse
  the storage layer), Phase 25 (media polish).

## 6. Open questions

1. **Provider** — S3 (AWS), R2 (Cloudflare, cheap egress), Backblaze
   B2, self-hosted MinIO? (Same open in `README.md` §10.)
2. **Max media per check-in** — 5? 10? Unlimited?
3. **Max file size** — 10 MB photo, 100 MB video? Enforce client
   and server side.
4. **Accepted MIME types** — jpeg/png/webp (photo), mp4/mov (video)?
   Skip HEIC or convert client-side?
5. **Public URLs vs. always-signed** — feed rendering (Phase 13) can
   benefit from short-lived signed URLs cached in the client, but
   long feeds may push us to CDN with tokenized paths.

## 7. Success criteria

- `POST /check_ins/{id}/media/presign` returns a valid PUT URL for
  each item.
- After binary PUT succeeds, `POST /check_ins/{id}/media` with the
  keys creates the DB rows.
- `POST /check_ins` without media → 422 (rule tightened from Phase 5).
- Profile screen shows the check-in with a media thumbnail grid.
- Full-screen viewer opens on tap, plays videos.
- CI green including a stubbed S3 adapter (Bypass).
