# Phase 1 — Code Versioning & Release Automation (SPEC)

> Status: **specification only**. No implementation yet. Companion to
> `plan.md` (high-level approach) and `tasks.md` (execution breakdown).

## 1. Summary

Adopt `release-please` (Google) to automate versioning and release
publication for the `xpeak` monorepo. Every merge into `main`
creates or updates a **Release PR**. The Release PR is auto-mergeable
once CI passes. Merging it publishes a tagged GitHub Release with an
English-only changelog.

## 2. Scope

**In scope**
- Conventional Commits enforcement (local hook + CI check).
- CI pipeline (lint, test, build placeholder) as a required check.
- `release-please` workflow, config, and manifest.
- GitHub PR template and CODEOWNERS.
- Branch protection rules on `main`.
- ADR documenting the decision.
- End-to-end dry-run validation.

**Out of scope (later phases)**
- Deploying the Phoenix API to any environment.
- Building/uploading native artifacts (Android/iOS) to stores.
- Publishing `packages/shared` (or any package) to a registry.
- Native signing/notarization pipelines.

## 3. Conventions

### 3.1. Commit format (Conventional Commits, English-only)

```
<type>(<scope>): <subject>

<optional body>

<optional footer, including task/ticket trailer>
```

**Allowed types → SemVer impact:**

| Type       | Impact             | Notes                                       |
|------------|--------------------|---------------------------------------------|
| `feat`     | minor              | New user-facing capability                  |
| `fix`      | patch              | Bug fix                                     |
| `perf`     | patch              | Performance improvement                     |
| `refactor` | none (in changelog) | Internal restructuring                     |
| `docs`     | none               | Documentation only                          |
| `test`     | none               | Test-only changes                           |
| `build`    | none               | Build system / dependencies                 |
| `ci`       | none               | CI configuration                            |
| `chore`    | none               | Misc, incl. release commits                 |
| `revert`   | none / patch       | Depends on what is reverted                 |

**Breaking changes** — appending `!` after the type (`feat!:`) or adding
a `BREAKING CHANGE:` footer triggers a **major** bump.

**Scope** — optional, lowercase, single word (`auth`, `check-in`,
`release`, `mobile`, `api`). Multiple scopes not allowed on a single
commit.

**Trailer format (project-specific, required on non-trivial commits):**

```
T-NNN-XX Task title [XPK-N]
```

Where `T-NNN-XX` is the task id from `specs/*/tasks.md` and `[XPK-N]`
is the branch's ticket tag.

### 3.2. English-only rule

Applies to:
- Commit subject, body, and footer
- PR title and body
- Changelog entries (release-please generates them from commit
  subjects, so English commits ⇒ English changelog by construction)
- Release notes
- Tag messages

## 4. `release-please` configuration

### 4.1. Strategy: single package (monorepo, single version)

The whole repository is versioned under **one** SemVer line. The
version lives in the root `package.json` and is mirrored in the
manifest file.

Rationale: at the current stage the `mobile` and `api` apps ship
together as a product. Splitting into per-package versions is
premature and can be introduced later with a `release-please` config
migration (documented as future work in `plan.md` §6).

### 4.2. Release type

- `release-type: node` for the root package (bumps `package.json`
  version + generates `CHANGELOG.md`).

### 4.3. Changelog sections (in order)

Groups shown in the generated changelog:

1. `feat` → **Features**
2. `fix` → **Bug Fixes**
3. `perf` → **Performance Improvements**
4. `revert` → **Reverts**
5. `refactor` → **Code Refactoring** (hidden by default; visible via
   config)
6. `docs` → **Documentation** (hidden by default; visible via config)
7. `build` → **Build System** (hidden by default)
8. `ci` → **Continuous Integration** (hidden by default)
9. `test` → **Tests** (hidden by default)
10. `chore` → **Miscellaneous Chores** (hidden by default)

The exact set of hidden sections is finalized in `T-001-05`.

### 4.4. Initial version

Bootstrap at **`0.0.1`** in `.release-please-manifest.json`. The first
`feat:` commit after bootstrap will produce `0.1.0` in the Release PR.

### 4.5. Release PR

- Branch name: `release-please--branches--main` (default).
- Title: `chore(release): X.Y.Z` (or `chore: release X.Y.Z` depending
  on template config — final choice in `T-001-05`).
- Body: full grouped changelog for the upcoming version.
- Labels: `autorelease: pending` while open, `autorelease: tagged`
  after merge.

### 4.6. Auto-merge

The release workflow enables auto-merge (squash) on the Release PR via:

```
gh pr merge --auto --squash <pr-number>
```

Effect: GitHub merges the PR automatically once **all required
checks** pass and any required approvals are in place. If no reviews
are required by branch protection, the merge triggers as soon as CI
is green.

## 5. CI pipeline

Required check for the Release PR to auto-merge. Runs on every PR and
on push to `main`.

**Workflow file:** `.github/workflows/ci.yml`

**Jobs:**

- `install` — checkout, setup Node 20, setup pnpm, `pnpm install
  --frozen-lockfile`, cache node_modules.
- `lint` — `pnpm lint`
- `test` — `pnpm test`
- `build` — `pnpm build`
- `commitlint` — validates commit subjects against the Conventional
  Commits config (runs on PR events, checks the PR title since we
  squash-merge)

All jobs run in parallel where possible; `lint`, `test`, `build`, and
`commitlint` are all required checks on the `main` branch protection.

Later phases will add: Phoenix ExUnit job (`api-test`), Playwright job
(`e2e-web`), Detox job (`e2e-native`).

## 6. Branch protection on `main`

Configured manually in GitHub (documented in `T-001-10`). Settings:

- Require a pull request before merging
- Require approvals: **1** (safety net; the Release PR still
  auto-merges once approved)
- Dismiss stale approvals when new commits are pushed
- Require status checks to pass: `lint`, `test`, `build`, `commitlint`
- Require branches to be up to date before merging
- Require linear history (aligns with squash-only)
- Allow **squash merge only** (disable merge commit and rebase merge)
- Allow **auto-merge**
- Restrict who can push to `main` (admins only, break-glass)
- Do **not** allow force pushes
- Do **not** allow deletions

## 7. Commit hygiene (local)

- **Husky** installs Git hooks on `pnpm install` (via `prepare` script).
- **commitlint** hook (`commit-msg`) validates every commit against
  `@commitlint/config-conventional`.

## 8. PR template

`.github/pull_request_template.md` reminds the contributor of:

- Conventional Commits format for the PR title (since we squash-merge,
  the PR title becomes the commit subject on `main`).
- English-only rule.
- Task reference (`T-NNN-XX`) and ticket tag (`[XPK-N]`) placement.
- Test coverage checklist item (TDD is mandatory).

## 9. CODEOWNERS

`.github/CODEOWNERS` maps every path to the current maintainer
(`@Felpasw` for now). Auto-assigns him as reviewer on every PR.

## 10. ADR

`docs/adr/0001-release-flow.md` documents:
- The decision to use release-please.
- Alternatives considered (changesets, semantic-release) and why
  rejected.
- Single-package strategy vs. per-package, and the trigger for
  revisiting it.
- Enforcement mechanisms (branch protection, commitlint, Husky).

## 11. Success criteria

The phase is done when **all** of the following are true:

- A dummy `feat:` PR merged into `main` produces a Release PR with the
  correct bumped version and grouped changelog.
- A follow-up dummy `fix:` PR merged into `main` **updates** the same
  open Release PR (not creates a second one).
- The Release PR auto-merges after CI is green and the required
  approval is in place.
- Merging the Release PR publishes a GitHub Release tagged `vX.Y.Z`
  with the English changelog body.
- A local commit with a non-conventional subject (e.g., `wip stuff`)
  is rejected by commitlint.
- A push to `main` that bypasses the release workflow is impossible
  (branch protection blocks direct pushes for non-admins).

## 12. Risks and mitigations

| Risk                                              | Mitigation                                                        |
|---------------------------------------------------|-------------------------------------------------------------------|
| Release PR auto-merges buggy code                 | Required CI (`lint`, `test`, `build`) + 1 review before auto-merge |
| Contributor writes commit in pt-BR                | commitlint (subject) + reviewer catches body                       |
| Squash merge changes the subject and breaks bump  | PR template + commitlint on PR title                              |
| Force-push destroys history                       | Branch protection disallows force-push                            |
| Manifest and `package.json` drift                 | release-please owns both; do not edit by hand                     |
| First-run behavior differs from expectations      | `T-001-12` dry-run validates before we trust the pipeline         |

## 13. Assumptions (verify before implementation)

- Repository is (or will be) hosted on GitHub.
- The account running the workflow has `contents: write` and
  `pull-requests: write` on the repo.
- `main` is (or will be) the default branch.
- Node 20 + pnpm 9 stays the toolchain baseline.
- `Felpasw` is the sole/main maintainer for now (CODEOWNERS default).

## 14. Open follow-ups (post-phase)

- Introduce per-package versioning when mobile and api decouple.
- Add native build jobs (Android AAB, iOS IPA) as required checks or
  as post-release jobs.
- Add Phoenix ExUnit + Postgres service to CI when the API bootstraps.
- Consider auto-publishing changelog entries to a Discord/Slack channel
  once the team grows.
