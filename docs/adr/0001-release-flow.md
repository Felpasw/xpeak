# ADR 0001 — Release flow

- **Status:** Accepted
- **Date:** 2026-09-15
- **Context:** Phase 1 (`specs/001-versioning/`).

## Context

Every merge into `main` should automatically maintain an
up-to-date **Release PR** that accumulates upcoming changes, computes
the next SemVer bump from Conventional Commit history, and — once
merged — publishes a GitHub Release with the changelog and a
`vX.Y.Z` tag. The pipeline runs unattended (single-maintainer
project); the safety net is the required CI (`lint`, `test`,
`build`, `commitlint`) and the ability to review the Release PR
before it auto-merges.

## Decision

Adopt **[release-please]** (Google) via the official
[`googleapis/release-please-action@v4`][action]. It fits the
"auto-updating Release PR" pattern natively, works well with
`pnpm` monorepos through its `extra-files` support, and produces
English-only changelogs by construction (commits are English by
convention — see below).

### Repository configuration

- `release-please-config.json` — single package at `.`,
  `release-type: node`, `bump-minor-pre-major: true` while we're on
  `0.x`, and `changelog-sections` limited to user-facing types
  (`feat`, `fix`, `perf`, `revert`).
- `.release-please-manifest.json` — seed `{ ".": "0.0.1" }`.
- `.github/workflows/release.yml` — trigger on `push` to `main`;
  runs the action, then enables auto-merge (squash) on the produced
  Release PR via `gh pr merge --auto --squash`.

### Commit convention

- **Conventional Commits**, English only.
- Types → SemVer impact: `feat` = minor, `fix`/`perf` = patch,
  `feat!` or `BREAKING CHANGE:` footer = major.
- Enforcement:
  - Local: `commitlint` via Husky `commit-msg` hook.
  - CI: `wagoid/commitlint-github-action@v6` validating PR titles
    (we squash-merge, so PR title becomes the commit subject).
- PR title is required to be a valid Conventional Commit line.

### CI as the gate

`.github/workflows/ci.yml` runs on every PR against `main`:
- `install` — pnpm install with cache.
- `lint`, `test`, `build` — matrix of independent jobs.
- `commitlint` — PR title validation.

Branch protection on `main` (configured in the GitHub UI, tracked in
`specs/001-versioning/tasks.md` T-001-14):
- Require PR before merging.
- Require the checks above.
- Require branches up to date before merging.
- Require linear history.
- Allow squash merge only.
- Allow auto-merge.
- Disallow force-push and deletions.

### Auto-merge

- Zero required approvals on Release PRs for MVP (single
  maintainer). The safety comes from the required CI.
- Any Release PR can still be blocked manually (close the PR or
  push a `chore(release): skip` message).

## Alternatives considered

- **[changesets]** — requires a `.changeset/*.md` file per PR
  describing the impact. More friction, more control. Not chosen
  because the "PR-that-updates-itself" pattern that we want is
  release-please's default.
- **[semantic-release]** — publishes straight from the push, no
  intermediate PR. Does not fit the "review before merging"
  posture we want.

## Consequences

- Every merge into `main` triggers `release-please` and may
  update/create a Release PR.
- The version in `apps/api/VERSION` and `apps/mobile/package.json`
  will be added to `extra-files` in Phase 2 so bumps flow through
  both apps in one release.
- If a commit lands on `main` without matching Conventional
  Commits (bypassed protection), release-please will skip it —
  the changelog will silently omit the change. Enforcement at the
  PR title level should prevent this.
- Failed CI blocks auto-merge; the Release PR stays open until
  the failure is resolved (which is the intended behavior).

## Revisit triggers

- If we start shipping mobile and api on independent cadences,
  switch `release-please-config.json` to per-package releases and
  reconsider the manifest layout.
- If we grow beyond a single maintainer, revisit the "zero
  approvals" stance on Release PRs.

[release-please]: https://github.com/googleapis/release-please
[action]: https://github.com/googleapis/release-please-action
[changesets]: https://github.com/changesets/changesets
[semantic-release]: https://github.com/semantic-release/semantic-release
