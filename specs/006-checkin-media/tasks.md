# Phase 6 — Check-in Media (TASKS)

## Legend

- `[T]` TDD, `[S]` sequential, `[P]` parallel, `[HUMAN]` human-only.

---

## Section A — Storage layer

- [ ] **T-006-01 `[HUMAN][S]`** — Pick storage provider (Q1) and
  provision bucket + IAM keys.
  - Paste credentials into local `.env` (not committed).
  - Update `.env.example` with placeholders.

- [ ] **T-006-02 `[T][S]`** — `Xpeak.Storage` behaviour + `MockAdapter`
  - Failing test: `MockAdapter.presigned_put_url/2` returns
    deterministic URL.
  - Implement behaviour + mock.
  - Green.

- [ ] **T-006-03 `[T][S]`** — `S3Adapter`
  - Failing test with Bypass simulating S3.
  - Implement using `ex_aws_s3`.
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

- [ ] **T-006-08 `[T][S]`** — Update `POST /check_ins` for two-step
  - Add `media_intent_count` and a pending state.
  - Failing test: check-in created with `has_media: false`.
  - Green.

- [ ] **T-006-09 `[T][S]`** — Oban cron: soft-delete pending
      check-ins after 15 min
  - Depends on Oban already installed (adds it if not).
  - Failing test: Oban.Testing.perform_job/2 removes pending record
    older than 15 min.
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

1. `feat(storage): storage behaviour + s3 adapter + mock` — A
2. `feat(check-ins): media schema` — B
3. `feat(check-ins): presign and attach endpoints` — C
4. `feat(check-ins): two-step flow with cleanup cron` — D
5. `feat(mobile): media picker + upload flow` — E1
6. `feat(mobile): fullscreen viewer + check-in integration` — E2 + F
7. `docs(adr): media storage` — G
