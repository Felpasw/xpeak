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
- `.github/workflows/release.yml` — triggers on `push` to `main`
  and on `workflow_dispatch` (manual re-run from the Actions tab);
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

### Repo settings checklist (manual, one-time)

Learned the hard way after Phase 2 shipped — release-please and
auto-merge silently fail if these are not set. Track in
`specs/001-versioning/tasks.md` T-001-14/15.

At `Settings → Actions → General → Workflow permissions`:
- ☑ **Read and write permissions** — needed for release-please to
  push branches and commits.
- ☑ **Allow GitHub Actions to create and approve pull requests** —
  needed for release-please to open the Release PR itself. If
  disabled, the workflow logs `GitHub Actions is not permitted to
  create or approve pull requests` and the Release PR never
  appears.

At `Settings → General → Pull Requests`:
- ☑ **Allow squash merging** — the only merge method we allow.
- ☐ Allow merge commits — disable.
- ☐ Allow rebase merging — disable.
- ☑ **Allow auto-merge** — needed so `gh pr merge --auto` in
  `release.yml` succeeds. If disabled, the workflow logs `Auto
  merge is not allowed for this repository
  (enablePullRequestAutoMerge)`.
- ☑ Automatically delete head branches — optional but clean.

At `Settings → Rulesets → main-protection` → **Require status
checks to pass**:
- Add every job name from `.github/workflows/ci.yml` that we want
  to gate the merge on. Job **names** (the `name:` field), not
  the YAML key. When we rename a job, update the ruleset in the
  same PR — otherwise merges get stuck waiting on a check that
  will never report.

### Release PR title pattern

**We don't set `pull-request-title-pattern`.** Release-please 4
carries its own default that produces sensible titles like
`chore: release 0.1.2` for a single-package repo, and any
customization we tried triggered a bug where `${version}` silently
fell back to the branch name (`main`) — even patterns that mirror
the documented default. The Release PR ended up titled
`chore: release main`, the squash commit lost its recognizable
version marker, and tags stopped being created (though version
bumps in `package.json` / `apps/api/VERSION` /
`apps/mobile/package.json` still propagated).

**If a broken cycle happens** — bumps propagate but the "Releases"
page stays empty and every subsequent workflow run aborts with
`There are untagged, merged release PRs outstanding` — recovery
is one-time:

1. Create the missing tag manually:
   `gh release create vX.Y.Z --target <sha> --generate-notes`
2. Drop the `autorelease: pending` label from the stuck PR.
3. Trigger the release workflow again (push or
   `workflow_dispatch`).

After that, the default title pattern keeps future cycles clean
and no intervention is needed.

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
