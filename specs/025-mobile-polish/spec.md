# Phase 25 — Mobile Polish & PWA Fallback (SPEC)

## 1. Offline check-in queue

### 1.1. Storage

- `IndexedDB` store: `checkin_queue` (id, form_payload_json,
  media_blobs, state, attempts, last_error).

### 1.2. Flow

1. User completes check-in form while offline.
2. Media captured via camera plugin, stored as blobs in IndexedDB.
3. Row inserted in queue with `state: "pending"`.
4. On `online` event or app resume, worker picks pending rows:
   - Runs the two-step upload flow (presign → PUT → attach) from
     Phase 6.
   - On success → deletes row.
   - On failure → increments `attempts`, exponential backoff.

### 1.3. UX

- Queue count badge in the header.
- Tapping opens a queue viewer with per-item status.
- Manual retry button.

## 2. Media compression

- **Photo:** `browser-image-compression` targeting max 1920×1920,
  quality 0.8 → typical 60–80% reduction.
- **Video:** `ffmpeg.wasm` transcoding to H.264 baseline at 720p
  30fps (behind a feature flag; skip in MVP if budget is too
  tight).
- Compression runs before insertion into the queue.

## 3. PWA

### 3.1. Manifest

- `public/manifest.webmanifest`:
  - name, short_name, theme_color, background_color, icons at 192
    and 512, display "standalone", start_url `/`.

### 3.2. Service worker

- Precache app shell.
- Runtime caching: TanStack Query cache on network-first;
  images/media on stale-while-revalidate.
- Skip caching for auth-sensitive endpoints (`/auth/*`, `/me`).

### 3.3. Install prompt

- Simple non-nagging banner after 3rd session.

## 4. Performance

- Bundle analysis: `next build && next-bundle-analyzer`.
- Code-split heavy screens (recap viewer, forms gallery).
- `next/image` with static export needs manual sizing hints.
- Prefetch hints on navigation targets.
- Route-level suspense for slow data.

## 5. Error reporting

- Sentry SDK on mobile (`@sentry/nextjs`) and on api (`sentry_elixir`).
- Source maps uploaded during build.
- Privacy: never send PII (email, media blobs); scrub tokens from
  headers.

## 6. Accessibility

- axe-core CI check.
- Alt text audit across all rendered media.
- Focus management on modals and navigation.
- Contrast pass on the active theme.

## 7. Test plan

- Offline queue: unit tests for state transitions; integration test
  with mocked network toggling.
- Compression: golden files (input → expected output size range).
- PWA: Lighthouse CI job produces the report and fails on budget.
- Error reporting: verify events reach a Sentry test project (integration).
- axe-core: no AA violations on hero screens.

## 8. Non-goals

- Native distribution automation (fastlane, EAS, etc.).
- Automated end-to-end mobile tests on real devices (Detox is set
  up in Phase 2 but full E2E coverage isn't part of this phase).
- Backend performance optimization beyond what's needed for the
  mobile bar.
