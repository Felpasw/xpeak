# Phase 25 — Mobile Polish & PWA Fallback

> Status: **planning only**.

## 1. Goal

Final surface polish for shipping to real users: offline check-in
queue, media optimization, PWA fallback (web install), and a
performance pass to hit the "feed opens < 1s on 4G" bar from the
brief.

## 2. Scope

**In scope**
- Offline queue for check-ins (photo capture works offline; uploads
  sync when back online).
- Client-side media compression (photo resize, video transcoding
  for common sizes).
- Resumable upload for large videos (multi-part or chunked).
- PWA manifest + service worker (offline shell, cached assets).
- Performance pass: bundle size analysis, code-split heavy screens,
  image lazy-loading, prefetch hints.
- Accessibility sweep: labels, contrast, keyboard nav on web
  fallback.
- Crash / error reporting (Sentry or similar).

**Out of scope**
- Native app store submission itself (release engineering, not
  code).
- Deep observability on backend (separate initiative).

## 3. Approach

- **Offline queue:** IndexedDB-backed queue via `idb` or Dexie;
  workers upload on `navigator.onLine` events.
- **Media compression:** `@capacitor/camera` handles capture,
  `browser-image-compression` for photos, `ffmpeg.wasm` for video
  (feature-flag, may skip in MVP for size reasons).
- **PWA:** Next.js manifest + custom SW via `@ducanh2912/next-pwa`
  or hand-rolled.
- **Performance:** Lighthouse budget in CI; fail if > threshold on
  hero pages.

## 4. Artifacts

- `lib/offline/queue.ts` — offline check-in queue.
- `lib/media/compressor.ts` — client compression pipeline.
- `next.config.ts` PWA plugin.
- `public/manifest.webmanifest`.
- Custom service worker (or generated).
- Sentry (or alternative) SDK wiring on both api and mobile.
- CI: Lighthouse-CI job with budget.

## 5. Dependencies

- Blocked by: All previous phases.
- Blocks: —

## 6. Open questions

1. **Video compression on device** — ffmpeg.wasm is heavy (~30 MB
   web); include or skip in MVP?
2. **Error reporting provider** — Sentry, Bugsnag, self-hosted
   GlitchTip?
3. **PWA install prompt UX** — auto-prompt or manual "Add to home
   screen" link?
4. **Offline data reach** — just check-ins queue, or also allow
   browsing profile/feed offline from cached data?

## 7. Success criteria

- Airplane mode → user creates a check-in with media → returns to
  online → check-in syncs and lands.
- Photo compression reduces average upload by ≥ 60%.
- PWA installable on Chrome/Edge desktop and mobile; opens
  offline to the last-viewed screen.
- Lighthouse mobile performance ≥ 90 on the feed and profile
  screens.
- Runtime errors reported to Sentry with source maps.
- No axe-core violations at the AA level on the primary flows.
- CI green including Lighthouse budget.
