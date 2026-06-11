# Changelog

All notable changes to this project are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project follows
[Semantic Versioning](https://semver.org/). Bump the `"version"` in `package.json`
on release. See `VERSIONING.md` at the repository root.

## [Unreleased]

## [0.3.0] - 2026-06-11
### Added
- React Query cache tiers (`lib/cache.ts`) and `localStorage` persistence of the stable
  tier via `PersistQueryClientProvider` (busted by the app version).

## [0.2.0] - 2026-06-10
### Added
- Admin-only log viewer (visible to `is_admin` users) with date/level/search filters.

## [0.1.0] - 2026-06-10
### Added
- Initial version.
