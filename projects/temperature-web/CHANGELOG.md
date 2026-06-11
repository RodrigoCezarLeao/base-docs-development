# Changelog

All notable changes to this project are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project follows
[Semantic Versioning](https://semver.org/). Bump the `"version"` in `package.json`
on release. See `VERSIONING.md` at the repository root.

## [Unreleased]

## [0.5.0] - 2026-06-11
### Added
- Settings menu (gear icon → dropdown) for theme (light / dark / system) and language,
  synced with the URL (`?theme=&lang=`) and persisted to `localStorage`. Class-based dark
  mode applied across the UI.

## [0.4.0] - 2026-06-11
### Added
- Guided product tour (driver.js) via a reusable `useTour` hook — auto-runs once on first
  visit and re-runnable from a "Tour" button.

## [0.3.0] - 2026-06-11
### Added
- React Query cache tiers (`lib/cache.ts`) and `localStorage` persistence of the stable
  tier via `PersistQueryClientProvider` (busted by the app version).

## [0.2.0] - 2026-06-10
### Added
- Authentication (login / register, JWT) with React Router, and the admin-only log viewer.

## [0.1.0] - 2026-06-10
### Added
- Initial version.
