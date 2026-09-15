# Phase 1 — Code Versioning & Release Automation

> Status: **planning only**. No code written yet. This document exists to
> align the approach before we open a working branch and start implementing.

## 1. Goal

Every merge into `main` must automatically produce (or update) a
**Release PR** that accumulates upcoming changes, generates a changelog,
bumps the version following SemVer, and — once merged (via auto-merge) —
publishes a GitHub Release with an English-only changelog and tags the
commit as `vX.Y.Z`.

Reference we want to replicate: `github.com/Felpasw/moneta` (structure
still to be inspected — see §8).

## 2. Expected end-to-end flow

1. Developer branches off `main` following the global naming convention
   (`XPK-N/felpa-<name>`).
2. Commits follow **Conventional Commits in English**:
   - `feat: ...` → minor bump
   - `fix: ...` → patch bump
   - `feat!: ...` or `BREAKING CHANGE:` footer → major bump
   - `chore:`, `docs:`, `refactor:`, `test:`, `ci:` → no bump (still
     appear in changelog under proper sections when relevant)
3. PR from feature branch → `main`, squash-merged. The squash subject
   must remain a valid Conventional Commit line.
4. Push into `main` triggers the release workflow, which either:
   - **Creates** a new Release PR (`chore(release): X.Y.Z`) if none is
     open, or
   - **Updates** the existing open Release PR, recalculating the version
     bump and appending new changelog entries.
5. The Release PR body contains the full changelog for the upcoming
   version, grouped by change type, all in English.
6. Auto-merge is enabled on the Release PR by the workflow. It waits
   for required CI checks (and reviews, if branch protection requires)
   and then merges automatically.
7. Merging the Release PR triggers the release job, which:
   - Creates the SemVer tag `vX.Y.Z`
   - Publishes the GitHub Release with the changelog body
   - Updates `version` fields in the tracked manifest files
   - (Future) Kicks off downstream jobs: mobile build (Capacitor
     Android/iOS), image build, deploy, registry publish, etc.

## 3. Tooling — **DECIDED: `release-please`**

Confirmed against the `moneta` repository. We adopt
**`release-please`** with the official GitHub Action
`googleapis/release-please-action`.

Rationale:
- Native support for the "PR that auto-updates on every merge" pattern.
- Monorepo-friendly via `release-please-config.json`.
- English-only changelogs by construction.
- Already battle-tested in a sibling project (`moneta`), which lets us
  mirror a proven setup instead of reinventing it.

Discarded alternatives, for the record:
- `changesets` — requires a changeset file per PR (extra friction, not
  the pattern we want).
- `semantic-release` — publishes straight from the push, no
  intermediate PR (does not fit the auto-mergeable Release PR
  requirement).

## 4. Files that will be created (during implementation, not now)

- `.github/workflows/release.yml`
  - Trigger: `push` to `main`
  - Runs the release-please action with `token: ${{ secrets.GITHUB_TOKEN }}`
  - Enables auto-merge on the produced Release PR
- `release-please-config.json`
  - Declares packages: root (with `release-type: node`) plus each package
    under `apps/*` and `packages/*` that we want versioned
  - Points to the manifest file
  - Optional: sets `changelog-sections` to customize headings
- `.release-please-manifest.json`
  - Seed with `"." : "0.0.1"` and equivalents for other tracked packages
- `.github/workflows/ci.yml`
  - Runs on every PR to `main`
  - Steps: install (`pnpm install --frozen-lockfile`), `pnpm lint`,
    `pnpm test`, `pnpm build`
  - Required by branch protection so the Release PR only auto-merges
    when green
- `.github/pull_request_template.md`
  - Reminds contributor about Conventional Commits and the English-only
    commit rule
- `.github/CODEOWNERS` (optional)
  - Auto-assign reviewer on release PRs

## 5. Branch protection settings (manual, GitHub UI)

To be enabled on `main` before the flow is trusted:

- Require PR before merging
- Require status checks to pass (`ci` job)
- Require branches to be up to date before merging
- Allow **squash merge only** (linear history + clean subjects for
  Conventional Commits)
- Enable **auto-merge**
- Restrict who can push directly (only maintainers, and only for
  emergencies)

## 6. Monorepo strategy — decision to make (§10)

Two options:

### 6.A Single release for the whole repo
- One version number for the entire `xpeak` monorepo.
- Simpler. Recommended for early stage.
- `release-please-config.json` declares a single package at `.`.

### 6.B Independent release per package
- `apps/mobile` and `apps/api` get their own version numbers and tags
  (`mobile-v1.2.0`, `api-v0.5.1`).
- Better once mobile and API can evolve on independent cadences.
- More setup, more moving pieces.

**Proposal:** start with **6.A** (single release). Migrate to 6.B when
the two apps diverge enough to justify it.

## 7. Commit message rules (recap, for this phase)

- All commits in English.
- Conventional Commits format: `<type>(<scope>): <subject>`.
- Trailer with task and ticket: `T-001-XX Task title [XPK-N]`.

Example:
```
feat(release): add release-please workflow for main branch

Introduces the release automation pipeline: every merge into main
opens or updates a Release PR that auto-merges when CI is green and
publishes a GitHub Release with an English changelog.

T-001-02 Release-please workflow [XPK-2]
```

## 8. Open questions (must be answered before we cut code)

1. **Confirm `moneta`'s tool of choice** — is it release-please,
   changesets, or something custom? (Need repo access; if private,
   paste key files here.)
2. **Ticket prefix** — is it `XPK`? Jira, Linear, or none?
3. **Initial version** — start at `0.0.1` or jump to `0.1.0`?
4. **First-run behavior** — do we backfill a changelog from existing
   commits, or start clean from the moment the workflow lands?
5. **Auto-merge requirements** — do we require 1 human approval on the
   Release PR (safer), or fully hands-off?
6. **Downstream jobs** — do we want the release job to also trigger a
   Capacitor build / TestFlight / Play Console upload from day 1, or
   leave that for a later phase?
7. **`packages/shared`** — will we publish it to a registry (npm/GitHub
   Packages) or keep it workspace-internal only?
8. **Deploy target for Phoenix API** — Fly.io / Gigalixir / Kamal? Affects
   whether the release job needs deploy steps.

## 9. Task breakdown proposal (to be added to `tasks.md` after approval)

Once the questions above are answered, this phase will be split into
`T-001-XX` items along these lines (order preserved):

1. `T-001-01 [T]` Add `commitlint` + `commitizen` (or equivalent) to
   enforce Conventional Commits locally + in CI
2. `T-001-02 [T]` Add CI workflow (`ci.yml`) running lint/test/build
3. `T-001-03 [T]` Add `release-please` workflow + config + manifest
4. `T-001-04 [S]` Configure branch protection on `main` (manual,
   documented in `docs/adr/0001-release-flow.md`)
5. `T-001-05 [T]` PR template + CODEOWNERS
6. `T-001-06 [HUMANO]` Enable auto-merge and required checks on GitHub
   (needs repo admin)
7. `T-001-07 [S]` ADR `0001-release-flow.md` documenting the decision
8. `T-001-08 [T]` End-to-end dry run: open a dummy `feat:` PR, merge,
   observe Release PR created; open a dummy `fix:` PR, merge, observe
   Release PR updated; approve, watch auto-merge and release publish

Tags legend: `[T]` = TDD required, `[S]` = sequential, `[HUMANO]` =
human-only step.

## 10. Out of scope for this phase

- Deploying the Phoenix API or the Capacitor build to any environment
- Publishing packages to npm / GitHub Packages
- Signing / notarization of native builds
- Release-note distribution channels beyond the GitHub Release page

These will be tackled once the base release loop is proven stable.

## 11. Next step

Answer the questions in §8 (specially #1), then this document turns
into `tasks.md` and we open the first working branch to implement
`T-001-01`.
