# Phase 10 — Profile Customization

> Status: **planning only**.

> **Stack note:** artifacts in this file (module names, package names,
> code samples) were originally written for Elixir/Phoenix. The
> project switched to C# + ASP.NET Core 9 in Phase 2. See
> `specs/roadmap.md` → "Stack migration note" for the mapping table.
> Concepts and endpoint contracts still hold; concrete artifacts get
> rewritten when this phase is picked up.


## 1. Goal

Give users cosmetic control over their profile: avatar (image or
GIF), profile banner, and avatar frame. All slots accept static or
animated formats per the media policy in `ideas.md` §8.

## 2. Scope

**In scope**
- Avatar upload (photo or GIF/APNG/animated WebP).
- Banner upload.
- Frame selection from a system catalog (unlocking logic stub — all
  unlocked in MVP).
- Reuse **Cloudinary** pipeline from Phase 6 for all avatar / banner /
  frame assets. Backend stores `cloudinary_public_id`; client builds
  transformed URLs (avatar `w_128,h_128,c_fill`; banner `w_1200,c_limit`).
- `PATCH /me/profile` for avatar/banner/frame changes.
- Mobile: profile edit screen with previews.

**Out of scope**
- Purchasable cosmetics (no store yet).
- User-uploaded frames (moderation — deferred).
- Level "evolution forms" — Phase 22.
- Achievement-based unlocks — Phase 17 for insignias, Phase 20 for
  trophies (frames tied to those come later).

## 3. Approach

- Two categories of cosmetics:
  - **User-uploaded** (avatar, banner) — go through the presign
    flow from Phase 6.
  - **System catalog** (frames) — seeded PNGs shipped with the app,
    stored in a static path or in the assets bucket.
- `users` gains `banner_url` (uploaded), `frame_slug` (references
  system catalog).
- Frame is an overlay applied by the client on top of the avatar
  (SVG or PNG mask).

## 4. Artifacts

- Migrations: `add_banner_and_frame_to_users.exs`,
  `create_frames.exs`.
- `lib/xpeak/profile.ex` — context (`update_profile/2`).
- `lib/xpeak/profile/frame.ex` — schema.
- Seeds: 10 default frames.
- `PATCH /me/profile` endpoint.
- Mobile: profile edit screen, frame gallery, avatar/banner picker.

## 5. Dependencies

- Blocked by: Phase 3 (users), Phase 6 (media storage).
- Blocks: Phase 17 (insignias may unlock frames), Phase 20 (trophies
  may unlock frames).

## 6. Open questions

1. **Frame catalog origin** — bundled as static assets in the mobile
   build, or served from the media bucket like user uploads?
2. **Animated banner size cap** — 5 MB? Higher?
3. **Show cosmetic changes as events on the feed** (Phase 13)?
   Probably no (noise), but optionally yes with a per-user toggle.

## 7. Success criteria

- `PATCH /me/profile` updates fields; `GET /me` reflects them.
- Uploading a GIF avatar renders animated on profile.
- Switching frame changes the overlay visible on the profile card.
- No regression on Phase 3 profile screen.
- CI green.
