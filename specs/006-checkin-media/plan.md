# Phase 6 — Check-in Media

> Status: **planning only**. Companion to `spec.md` and `tasks.md`.

> **Stack note:** artifacts in this file (module names, package names,
> code samples) were originally written for Elixir/Phoenix. The
> project switched to C# + ASP.NET Core 9 in Phase 2. See
> `specs/roadmap.md` → "Stack migration note" for the mapping table.
> Concepts and endpoint contracts still hold; concrete artifacts get
> rewritten when this phase is picked up.


## 1. Goal

Make media attachment on check-ins a first-class requirement.
Multiple photos or videos per check-in, uploaded from the mobile
client to object storage, referenced from the API, and rendered
back on the profile and (eventually) the feed.

## 2. Scope

**In scope**
- `check_in_media` schema linking media items to check-ins.
- **Cloudinary** as the storage + CDN backend. Client uploads
  straight to Cloudinary via a signed upload preset; the API only
  receives the resulting `public_id` and metadata.
- Backend enforces: at least 1 media per check-in (raises Phase 5
  optional → Phase 6 required).
- Mobile: camera/roll picker (multi-select), preview grid, upload
  progress, retry on failure.
- Playback in-app (photo full-screen, video with native controls).
  Delivery URLs are built by the client using Cloudinary's URL
  transformation grammar (`f_auto,q_auto,w_<width>`) — no signed
  GET round-trip needed for the common case; assets are public
  by default, obscured by their random `public_id`.

**Out of scope**
- Client-side media compression / optimization (Phase 25).
- Moderation queue (deferred, `ideas.md` §2.5.4).
- Deep EXIF stripping / face-blur (deferred).
- Feed rendering (Phase 13).

## 3. Approach

- **Cloudinary** as the single storage + CDN provider for every
  visual asset in XPeak (check-in media, avatars, banners, frames,
  medal/insignia/trophy artwork, level "evolution forms",
  category icons). One vendor, one SDK, one mental model.
- **Signed upload preset** — the API generates a signature for the
  client, the client uploads directly to Cloudinary. The API is
  never on the upload hot path.
- **API stores only** `cloudinary_public_id`, `kind` (photo/video),
  `mime_type`, `byte_size`, `width`, `height`, `duration_seconds`
  (video only). No bytes ever hit our disk.
- **Delivery** is done by the client building URLs with Cloudinary
  transformations (`f_auto,q_auto,w_<width>`) — no signed GET
  round-trip. Assets are public by default; obscurity comes from
  the random `public_id`. Signed delivery is available when we
  need it (private media that shouldn't be indexed).
- Media rows created **after** upload succeeds (client posts the
  metadata + `public_id` back). No orphan rows for aborted
  uploads.

## 4. Artifacts

- Migration: `AddCheckInMedia` (`check_in_media` table with
  `cloudinary_public_id`, `kind`, dimensions, byte size).
- `Infrastructure/Cloudinary/CloudinaryOptions.cs` — POCO for
  `Cloudinary:CloudName`, `Cloudinary:ApiKey`,
  `Cloudinary:ApiSecret`, `Cloudinary:UploadPreset`.
- `Infrastructure/Cloudinary/CloudinaryClient.cs` — thin wrapper
  over `CloudinaryDotNet` with `SignUpload(...)` and
  `DeleteAsset(publicId)`.
- `Endpoints/CheckInMediaEndpoints.cs` — `POST /check_ins/{id}/media/upload-signature`
  returns signature for client, `POST /check_ins/{id}/media`
  registers the resulting `public_id` + metadata.
- Update `POST /check_ins` to require at least one media
  reference.
- Mobile: `lib/media/cloudinary.ts` (upload via Cloudinary widget
  or fetch to `/upload` with signature) + `MediaPicker` component
  + updated check-in screen.

## 5. Dependencies

- Blocked by: Phase 5 (check-in exists).
- Blocks: Phase 10 (avatar/banner/frame reuse the same Cloudinary
  pipeline), Phase 13 (feed rendering), Phase 20 (trophy assets),
  Phase 22 (evolution forms), Phase 25 (media polish).

## 6. Open questions

1. **Max media per check-in** — 5? 10? Unlimited?
2. **Max file size** — 10 MB photo, 100 MB video? Enforce via
   Cloudinary upload preset (server-side, unbypassable) + mirror
   client-side for UX.
3. **Accepted MIME types** — jpeg/png/webp (photo), mp4/mov
   (video)? Cloudinary auto-converts HEIC on upload (`f_auto`),
   so we can accept it too.
4. **Cloudinary account tier** — free (25 credits/month) fine
   for MVP; upgrade to Plus (~$99/mo) when we hit the ceiling.
5. **Public vs. authenticated delivery** — MVP: public with
   unguessable `public_id`. Later, if privacy tightens, switch
   to Cloudinary "authenticated" delivery with signed URLs.

## 7. Success criteria

- `POST /check_ins/{id}/media/upload-signature` returns a valid
  Cloudinary upload signature for the client.
- After the client uploads to Cloudinary, `POST /check_ins/{id}/media`
  with the `public_id` + metadata creates the DB rows.
- `POST /check_ins` without media → 422 (rule tightened from
  Phase 5).
- Profile screen shows the check-in with a media thumbnail grid
  built from Cloudinary URL transformations.
- Full-screen viewer opens on tap; videos play via Cloudinary
  streaming URL.
- CI green with the Cloudinary SDK behind a mockable interface.
