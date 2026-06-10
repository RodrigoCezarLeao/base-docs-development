# Versioning & Release Plan

How every project from this base is versioned, where the version is shown, and how
versions flow through deploy and updates. Applies to both the API and the frontend.

## Scheme — manual SemVer, per app

Each app owns its version as **[Semantic Versioning](https://semver.org/) `MAJOR.MINOR.PATCH`**,
declared **manually in a file** (single source of truth):

| App | Source of truth | Bump by editing |
|-----|-----------------|-----------------|
| API (.NET) | `<Version>` in `Directory.Build.props` (project root) | that one line |
| Frontend (React) | `"version"` in `package.json` | that one field |

The API and the frontend of a project are versioned **independently** — bump only what
changed. Increment:

- **PATCH** (`0.1.0 → 0.1.1`) — backwards-compatible bug fixes.
- **MINOR** (`0.1.1 → 0.2.0`) — backwards-compatible features.
- **MAJOR** (`0.2.0 → 1.0.0`) — breaking changes (API contract, etc.).

Build metadata (**commit** + **build time**) is attached automatically by CI — never
edited by hand. Locally it falls back to `local` / `unknown`.

## Where the version is shown

| Surface | Shows | Source |
|---------|-------|--------|
| API `GET /version` | `{ name, version, commit, builtAt }` | assembly version + `APP_COMMIT`/`APP_BUILD_TIME` env |
| API `GET /health` | `status` + `version` + `commit` | same |
| Frontend | discreet badge in the bottom-left corner (`v0.1.0`); hover shows commit + build time | `__APP_VERSION__` (from `package.json`) + `VITE_APP_COMMIT`/`VITE_APP_BUILD_TIME` |
| GHCR image | tag = immutable commit SHA (+ environment tag) | the deploy pipeline |
| `CHANGELOG.md` | human-readable history per app | maintained by hand |

CI injects the build metadata: the backend image receives `APP_COMMIT` / `APP_BUILD_TIME`
as Docker build-args; the frontend build receives `VITE_APP_COMMIT` / `VITE_APP_BUILD_TIME`
as env (see `.github/workflows/deploy.yml`).

## CHANGELOG

Each app keeps a `CHANGELOG.md` at its root, following
[Keep a Changelog](https://keepachangelog.com/). Add entries under `## [Unreleased]`
as you work; on release, rename that section to the new version + date.

## Deploy & update plan

Branches and environments follow [`guidelines/infra-devops.md`](guidelines/infra-devops.md)
(`develop` → DEV, `master` → PROD). A release is just a versioned change flowing through:

1. **Branch** off `develop` and make the change.
2. **Bump the version** in the file(s) for the app(s) you changed (`Directory.Build.props`
   and/or `package.json`) and add a `CHANGELOG.md` entry.
3. **PR → `develop`.** On merge, DEV deploys automatically. Verify `https://dev.<domain>/version`
   shows the new version + the deployed commit.
4. **PR `develop` → `master`.** On merge, PROD deploys (gated by the `production`
   Environment's required reviewers). Verify `/version` and the frontend badge.
5. **Rollback** = redeploy the previous commit SHA (images are immutable in GHCR) —
   see the Rollback section in `guidelines/infra-devops.md`.

> Tip: the version is the human-readable label; the commit SHA at `/version` is the exact
> deployed build. Use the SHA for precise rollback, the version for communication.
