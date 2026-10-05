# Phase 6 — Check-in Media (TASKS)

## Legend

- `[T]` TDD, `[S]` sequential, `[P]` parallel, `[HUMAN]` human-only.

---

## Section A — Storage layer

- [ ] **T-006-01 `[HUMAN][S]`** — Pick storage provider (Q1) and
  provision bucket + IAM keys.
  - Paste credentials into local `.env` (not committed).
  - Update `.env.example` with placeholders.

- [ ] **T-006-02 `[T][S]`** — `IMediaStorage` interface + `FakeMediaStorage`
  - Failing test: `FakeMediaStorage.PresignedPutUrlAsync` returns a
    deterministic URL.
  - Implement interface + fake.
  - Green.

- [ ] **T-006-03 `[T][S]`** — `CloudinaryMediaStorage`
  - Failing test with `WireMock.Net` (or `HttpMessageHandler` stub)
    simulating Cloudinary.
  - Implement using `CloudinaryDotNet`.
  - Green.

## Section B — Schema

- [ ] **T-006-04 `[T][S]`** — `check_in_media` migration + schema
  - Failing tests: FK, kind whitelist, unique storage_key.
  - Implement.
  - Green.

## Section C — Presign + attach endpoints

- [ ] **T-006-05 `[T][S]`** — `POST /check_ins/{id}/media/presign`
  - Failing controller tests: happy, 403 not owner, 422 over-cap.
  - Implement.
  - Green.

- [ ] **T-006-06 `[T][S]`** — `POST /check_ins/{id}/media`
  - Failing controller tests: happy, 422 mismatched keys, 403 not
    owner.
  - Implement (records rows + updates check-in status if needed).
  - Green.

- [ ] **T-006-07 `[T][S]`** — `GET /check_ins/{id}/media/{media_id}/url`
  - Failing test: returns fresh signed URL; TTL respected.
  - Implement.
  - Green.

## Section D — Two-step flow + cron cleanup

- [x] ✅ commit `e412eff` **T-006-08 `[T][S]`** — Two-step `/check_ins` with
      `CreateAsync` (persist only) + `PublishAsync` (XP + streak +
      flip from pending)
  - **Design**: `xp_earned = 0` IS the pending signal. No new
    columns, no status enum, no intent count. When
    `with_media=true`, the row is born with `xp_earned=0` and no
    XP/streak side-effects. XP, level and streak are only
    computed when `ICheckInService.PublishAsync` runs — which is
    invoked by `IMediaService.ConfirmMediaAsync` on the first
    media batch confirm that observes `xp_earned=0`.
    Rationale: if the client never finishes uploading, Hangfire
    (T-006-09) deletes the pending row with zero XP rollback.
    Batch-only upload semantics (confirmed by UX): streaming is
    not supported, so the single confirm batch is a reliable
    "done" signal — no need to count against an intent.
  - Entity: `CheckIn` unchanged shape — no new columns. The
    existing `ck_check_ins_xp_earned_positive` CHECK relaxes to
    `ck_check_ins_xp_non_negative` (`xp_earned >= 0`) so pending
    rows can live with `xp_earned=0`.
  - Migration: `RelaxCheckInXpConstraintForPending` only drops
    the old CHECK and adds the new one. Zero schema change.
  - DTOs: `CreateCheckInRequest` + `CreateCheckInInput` gain
    `WithMedia` (bool, default `false`). `CheckInResponse` gains
    `HasMedia` (bool). `From(CheckIn)` sets `HasMedia=false`;
    `GET /check_ins` projects `HasMedia` via
    `db.CheckInMedia.Any(...)` in the repository.
  - Service: `CheckInService.CreateAsync` branches on
    `WithMedia`. Pending path persists the row with
    `XpEarned=0`, empty `ScoringSnapshot`, no user update, no
    streak query. Add `PublishAsync(Guid checkInId,
    CancellationToken)` to `ICheckInService`: loads the row,
    runs the existing scoring block, updates `xp_earned` +
    `scoring_snapshot` + `user.xp/level` in one transaction,
    derives streak post-commit. Idempotent-by-throw: calling on
    a row with `xp_earned != 0` raises
    `CheckInAlreadyPublishedException`.
  - Media: `MediaService.ConfirmMediaAsync` injects
    `ICheckInService`, after inserting `check_in_media` rows
    reads `xp_earned` from the check-in; if `0`, calls
    `PublishAsync` and returns the delta in the response.
    `ConfirmMediaResponse` carries an optional publish delta
    (`user`, `streak`, `leveled_up`) when a publish happened.
  - Failing tests
    (`apps/api.Tests/CheckIns/CheckInServiceTests.cs`,
    `apps/api.Tests/CheckIns/CheckInPersistenceTests.cs`,
    `apps/api.Tests/CheckIns/CreateCheckInEndpointTests.cs`,
    `apps/api.Tests/Media/ConfirmMediaEndpointTests.cs`):
    - Create with `withMedia=true` → persisted row has
      `xp_earned=0`, `has_media=false`; `user.xp` unchanged.
    - CHECK accepts `xp_earned=0` (pending signal).
    - `PublishAsync` on a pending row → `xp_earned > 0`,
      `user.xp` incremented, streak derived.
    - `PublishAsync` on an already-published row throws.
    - `ConfirmMediaAsync` on a pending check-in publishes and
      returns the delta; on an already-published check-in the
      delta is `null`.
  - Green.

- [ ] **T-006-09 `[T][S]`** — Hangfire recurring job: hard-delete
      pending check-ins older than 15 min
  - Depends on T-006-08 (needs rows with `xp_earned=0`).
  - Adds Hangfire + Postgres storage to `apps/api`, wires the
    dashboard behind the existing admin auth.
  - Job: `CleanupPendingCheckInsJob.ExecuteAsync` runs every
    5 minutes, deletes rows where `xp_earned = 0 AND created_at
    < now - 15 min`. Add partial index `(created_at) WHERE
    xp_earned = 0` so the sweep stays O(pending-count).
  - Hard delete, not soft: pending rows never had XP/streak
    side-effects, no history to preserve. The `check_in_media`
    cascade handles any orphan media rows (unlikely since
    publish never ran, but the FK cleans up regardless).
  - Failing test: instantiate the job class and invoke
    `ExecuteAsync` directly against the test host; assert the
    pending record older than 15 min is gone and a fresh
    pending row (< 15 min) survives.
  - Implement.
  - Green.

## Section E — Mobile media flow

- [ ] **T-006-10 `[T][S]`** — Media picker component
  - Failing spec: renders selected items, respects max count.
  - Implement using Capacitor Camera + Filesystem.
  - Green.

- [ ] **T-006-11 `[T][S]`** — Upload flow orchestration
  - Failing spec: happy path (mock adapter), retry path.
  - Implement `lib/media/uploader.ts`.
  - Green.

- [ ] **T-006-12 `[T][S]`** — Wire picker into check-in screen
  - Extend Phase 5 screen: media required.
  - Failing spec: submit disabled without media.
  - Green.

- [ ] **T-006-13 `[T][S]`** — Full-screen viewer
  - Failing spec: opens on thumbnail tap; video renders.
  - Implement.
  - Green.

## Section F — Profile refresh

- [ ] **T-006-14 `[T][S]`** — Profile shows check-in thumbnails
  - Failing spec: recent check-ins render with 1st media thumb.
  - Implement.
  - Green.

- [x] **T-006-17 `[T][S]`** ✅ commit `02eb5ea` — Date picker atom for
      `performedOn` backfill
  - Replaces the native `<input type="date">` on `CheckinForm` with
    an in-app date picker that matches the project style
    (Tailwind + motion floating label + sky/zinc dark theme) and
    honors the schema window (today → today − `MAX_BACKFILL_DAYS`).
  - New atom `DatePicker` + RHF wrapper `ControlledDatePicker`.
  - Exports `CHECKIN_MAX_BACKFILL_DAYS` from `useCheckinForm` so the
    form derives `min`/`max` from the single source of truth.
  - Smoke spec: opens popover, selects a day, closes on Escape,
    disables out-of-range days.

## Section G — Wrap-up

- [ ] **T-006-15 `[P]`** — ADR `0006-media-storage.md`
  - Provider choice, presign flow, TTLs, two-step vs. single-step.

- [ ] **T-006-16 `[S]`** — Phase close.

---

## Dependencies

```
A ──▶ B ──▶ C ──▶ D
                 ↓
         E (mobile) ──▶ F
                        ↓
                        G
```

## Bundling strategy for PRs

1. `feat(storage): IMediaStorage + Cloudinary adapter + fake` — A
2. `feat(check-ins): media schema` — B
3. `feat(check-ins): presign and attach endpoints` — C
4. `feat(check-ins): two-step flow with cleanup cron` — D
5. `feat(mobile): media picker + upload flow` — E1
6. `feat(mobile): fullscreen viewer + check-in integration` — E2 + F
7. `docs(adr): media storage` — G
