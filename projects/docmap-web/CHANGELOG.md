# Changelog

All notable changes to this project are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project follows
[Semantic Versioning](https://semver.org/). Bump the `"version"` in `package.json`
on release. See `VERSIONING.md` at the repository root.

## [Unreleased]

## [0.7.0] - 2026-06-11
### Added
- LGPD consent UX: a global consent banner (Accept / Decline + link to the policy) shown
  until a decision is stored. `lib/consent.ts` gates the `X-Tracking-Consent` header in
  `api.ts` — it is sent only after explicit acceptance.
- Privacy policy page (`/privacy`, pt-BR + en) describing data collected, purpose, legal
  basis, retention and rights; authenticated users can withdraw consent and delete their
  own tracking data (right to erasure) from there. A "Privacy" link in the settings menu.
- Admin access viewer (`/admin/access`): paginated table of access events (time, user, IP,
  device, request, status) with filters.

## [0.6.0] - 2026-06-11
### Added
- Admin metrics dashboard (`/admin/metrics`): active users, in-flight, total requests, a
  live per-endpoint table and a dependency-free SVG traffic chart, polling ~2s.

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
- Admin-only log viewer (visible to `is_admin` users) with date/level/search filters.

## [0.1.0] - 2026-06-10
### Added
- Initial version.
