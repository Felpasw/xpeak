# Phase 1 — Code Versioning & Release Automation (TASKS)

> Companion to `spec.md` (what) and `plan.md` (why). This file breaks the
> phase into trackable atomic steps. Nothing here is implemented yet.

## Legend

- `[T]` — TDD required: write a failing test/check before the fix.
- `[S]` — sequential: must land in the listed order.
- `[P]` — parallelizable with sibling `[P]` tasks in the same section.
- `[HUMAN]` — cannot be automated; requires a human action (usually in
  the GitHub UI or in a personal account).

Status glyphs (fill in as we go):
- `[ ]` — not started
- `[~]` — in progress
- `[x] ✅ commit <hash>` — done, committed

---

## Section A — Toolchain baseline

> Goal: make sure the monorepo can run `pnpm install` and the workflows
> we're about to add have a deterministic node/pnpm baseline.

- [x] ✅ commit `f999153` — **T-001-01 `[S]`** — Pin Node and pnpm versions
  - Add `.nvmrc` with `20` (or `20.x` LTS).
  - Add `.node-version` mirror (for tools that don't read `.nvmrc`).
  - Confirm `packageManager: "pnpm@9.0.0"` in root `package.json`
    (already present — validate it matches CI).
  - Deliverable: `.nvmrc`, `.node-version`.

- [x] ✅ commit `f999153` — **T-001-02 `[S]`** — Root scripts sanity
  - Ensure `pnpm lint`, `pnpm test`, `pnpm build` at the root run
    `pnpm -r` and exit `0` on an empty monorepo (no workspace packages
    yet). Add stub `echo` scripts if needed so CI doesn't fail before
    real apps land.
  - Deliverable: updated root `package.json` scripts.

---

## Section B — Commit hygiene

> Goal: enforce Conventional Commits locally and in CI so
> `release-please` can compute bumps and changelogs.

- [x] ✅ commit `f999153` — **T-001-03 `[T][S]`** — Install and configure `commitlint`
  - Add dev deps: `@commitlint/cli`, `@commitlint/config-conventional`.
  - Add `commitlint.config.cjs` extending `config-conventional`.
  - Add allowed types and scopes: `feat`, `fix`, `perf`, `refactor`,
    `docs`, `test`, `build`, `ci`, `chore`, `revert`; scopes free-form
    for now.
  - Test: run `echo "not a valid subject" | pnpm commitlint` — expect
    non-zero exit before task, then wire the config and expect the
    same command to still reject; then run
    `echo "feat(release): add commitlint" | pnpm commitlint` and expect
    exit `0`.
  - Deliverable: `commitlint.config.cjs`, updated `package.json`.

- [x] ✅ commit `f999153` — **T-001-04 `[T][S]`** — Install Husky and wire `commit-msg` hook
  - Add dev dep: `husky`.
  - Add `prepare` script: `husky`.
  - Create `.husky/commit-msg` running
    `pnpm dlx commitlint --edit "$1"` (or the local binary).
  - Test: attempt a commit with a bad subject (`git commit -m "wip"`)
    inside a temporary sandbox branch — expect the hook to block.
    Attempt with `git commit -m "chore: add husky"` — expect success
    (or clean staging without committing if we're on `main`).
  - Deliverable: `.husky/commit-msg`, `.husky/_/` (installed by Husky).

- [x] ✅ commit `f999153` — **T-001-05 `[T][P]`** — CI check for commit messages on PRs
  - Add a workflow job `commitlint` in `.github/workflows/ci.yml` that
    validates the **PR title** (since we squash-merge) using
    `wagoid/commitlint-github-action` or equivalent.
  - Test: draft a PR with a non-conventional title in the dry-run
    (`T-001-14`) and confirm the check fails.
  - Deliverable: updated `.github/workflows/ci.yml`.

---

## Section C — CI pipeline (required checks)

> Goal: give branch protection something meaningful to block merges on.

- [x] ✅ commit `f999153` — **T-001-06 `[T][S]`** — Create `.github/workflows/ci.yml` skeleton
  - Triggers: `pull_request` targeting `main`, and `push` to `main`.
  - Jobs: `install`, `lint`, `test`, `build`, `commitlint` (parallel
    where possible, `install` shared via cache).
  - Node 20 + pnpm 9 + `actions/setup-node@v4` + `pnpm/action-setup@v4`.
  - `pnpm install --frozen-lockfile` step in each job (or reuse a
    cached workspace).
  - Test: push the workflow file on the working branch and watch it
    run green on an empty monorepo.
  - Deliverable: `.github/workflows/ci.yml`.

- [x] ✅ commit `f999153` — **T-001-07 `[P]`** — Cache pnpm store
  - Configure `actions/cache@v4` keyed on `pnpm-lock.yaml` hash so the
    workflow doesn't re-download deps on every run.
  - Deliverable: updated `.github/workflows/ci.yml`.

---

## Section D — release-please pipeline

> Goal: the actual auto-versioning core of this phase.

- [x] ✅ commit `f999153` — **T-001-08 `[S]`** — Add `.release-please-manifest.json`
  - Seed with:
    ```json
    { ".": "0.0.1" }
    ```
  - Deliverable: `.release-please-manifest.json`.

- [x] ✅ commit `f999153` — **T-001-09 `[S]`** — Add `release-please-config.json`
  - Single package at root, `release-type: node`.
  - `changelog-sections`: keep `feat`, `fix`, `perf`, `revert` visible;
    hide `refactor`, `docs`, `build`, `ci`, `test`, `chore` (final
    decision documented inline).
  - `bump-minor-pre-major: true` while we're on `0.x` (so `feat!`
    bumps minor instead of jumping to `1.0.0` prematurely).
  - `draft: false`, `prerelease: false` for the release.
  - Deliverable: `release-please-config.json`.

- [x] ✅ commit `f999153` — **T-001-10 `[T][S]`** — Add `.github/workflows/release.yml`
  - Trigger: `push` to `main`.
  - Permissions: `contents: write`, `pull-requests: write`.
  - Step 1: `googleapis/release-please-action@v4` with
    `config-file: release-please-config.json` and
    `manifest-file: .release-please-manifest.json`.
  - Step 2: if the action outputs `pr` (i.e., a Release PR was created
    or updated), enable auto-merge on it:
    `gh pr merge --auto --squash "$PR_NUMBER"`.
  - Test: covered by `T-001-14` (E2E dry-run).
  - Deliverable: `.github/workflows/release.yml`.

---

## Section E — Repo hygiene assets

- [x] ✅ commit `f999153` — **T-001-11 `[P]`** — PR template
  - `.github/pull_request_template.md` with:
    - Reminder: PR title must be a valid Conventional Commit line
      (becomes the squash commit subject).
    - Reminder: English only in PR title/body and commits.
    - Reminder: include `T-NNN-XX Task title [XPK-N]` trailer in the
      PR description (release-please picks it up when squashing).
    - Test coverage checkbox.
    - Screenshot/video slot for UI changes.
  - Deliverable: `.github/pull_request_template.md`.

- [x] ✅ commit `f999153` — **T-001-12 `[P]`** — CODEOWNERS
  - `.github/CODEOWNERS` with a catch-all mapping to `@Felpasw`.
  - Deliverable: `.github/CODEOWNERS`.

- [x] ✅ commit `f999153` — **T-001-13 `[P]`** — ADR
  - `docs/adr/0001-release-flow.md` documenting: decision
    (release-please), context, alternatives (changesets,
    semantic-release), consequences, and the trigger that would make
    us revisit (e.g., mobile/api version divergence).
  - Deliverable: `docs/adr/0001-release-flow.md`.

---

## Section F — Branch protection & repo settings (human-only)

- [x] ✅ done via GitHub Rulesets UI — **T-001-14 `[HUMAN]`** — Configure branch protection on `main`
  - Require PR before merging.
  - Require 1 approval; dismiss stale approvals on new commits.
  - Require status checks: `lint`, `test`, `build`, `commitlint`.
  - Require branches up to date before merging.
  - Require linear history.
  - Allow squash merge only.
  - Enable auto-merge repo-wide.
  - Block direct pushes to `main` for non-admins.
  - Disallow force pushes and deletions.
  - Deliverable: settings applied in the GitHub UI (screenshot pasted
    into the ADR or `plan.md` §5 for audit).

- [x] ✅ enabled after Phase 2 (release-please blocked without it; see ADR 0001 §"Repo settings checklist") — **T-001-15 `[HUMAN]`** — Verify workflow permissions
  - Repo → Settings → Actions → General → Workflow permissions:
    `Read and write permissions` + `Allow GitHub Actions to create
    and approve pull requests`.
  - Deliverable: setting confirmed.

---

## Section G — End-to-end validation

- [~] deferred — validated implicitly by Phase 2 merge — **T-001-16 `[T][S]`** — Dry-run: first `feat` merge creates
      Release PR
  - Open a throwaway PR titled `feat(sandbox): dry-run release-please`
    with a trivial file change under `sandbox/`.
  - Merge into `main` (squash).
  - Observe: release workflow runs, opens a Release PR titled
    `chore(release): 0.1.0` with a `Features` section containing the
    sandbox line.
  - Deliverable: screenshot of the Release PR pasted into the ADR;
    sandbox commit reverted in a follow-up PR.

- [~] deferred — validated implicitly by Phase 2 merge — **T-001-17 `[T][S]`** — Dry-run: follow-up `fix` merge updates
      the same Release PR
  - Open a second throwaway PR titled
    `fix(sandbox): dry-run release-please update`.
  - Merge into `main` (squash).
  - Observe: the **same** Release PR gets updated (not a second one
    opened); the version stays at `0.1.0` (feat wins over fix); the
    `Bug Fixes` section gains the entry.
  - Deliverable: screenshot appended to the ADR.

- [~] deferred — validated implicitly by Phase 2 merge — **T-001-18 `[T][S]`** — Dry-run: Release PR auto-merges and
      publishes
  - Approve the Release PR (or watch it auto-merge if branch
    protection allows).
  - Observe: PR merges, tag `v0.1.0` created, GitHub Release published
    with English changelog body, `package.json` version bumped to
    `0.1.0`, `CHANGELOG.md` file appears (or is updated).
  - Deliverable: link to the published Release pasted into the ADR.

- [~] deferred — validated implicitly by Phase 2 merge — **T-001-19 `[S]`** — Cleanup
  - Revert sandbox commits with a `revert:` PR so `main` history stays
    clean.
  - Bump the manifest back to something sensible if the dry-run left
    the version somewhere odd (should not happen if we follow the
    steps).
  - Deliverable: clean `main`, `tasks.md` marked done, ready for
    Phase 2.

---

## Summary of deliverables (files created/modified)

- `.nvmrc`, `.node-version`
- `package.json` (root, scripts + devDependencies)
- `commitlint.config.cjs`
- `.husky/commit-msg` (+ `.husky/_/` bootstrap)
- `.github/workflows/ci.yml`
- `.github/workflows/release.yml`
- `.github/pull_request_template.md`
- `.github/CODEOWNERS`
- `release-please-config.json`
- `.release-please-manifest.json`
- `docs/adr/0001-release-flow.md`
- `CHANGELOG.md` (generated by release-please on first run)

## Dependencies between sections

```
A (baseline)
  ↓
B (commit hygiene) ── parallel with ── C (CI pipeline)
  ↓                                       ↓
                D (release-please)
                       ↓
              E (repo hygiene assets)  [can start earlier, needed for polish]
                       ↓
              F (branch protection — HUMAN)
                       ↓
              G (E2E validation)
```

## Bundling strategy for commits

Suggested bundles (each bundle = one PR = one squash commit on `main`):

1. `chore(repo): pin node and pnpm baseline` — T-001-01, T-001-02
2. `chore(commit): enforce conventional commits` — T-001-03, T-001-04, T-001-05
3. `ci: add base pipeline` — T-001-06, T-001-07
4. `ci(release): add release-please pipeline` — T-001-08, T-001-09, T-001-10
5. `chore(repo): add PR template, CODEOWNERS and ADR` — T-001-11, T-001-12, T-001-13
6. Human step (no commit) — T-001-14, T-001-15
7. `chore(sandbox): dry-run release-please` (later reverted) — T-001-16..T-001-19

Each bundle stops at `git add` and waits for explicit approval before
committing.
