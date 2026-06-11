# Changelog

All notable changes to this project are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project follows
[Semantic Versioning](https://semver.org/). Bump the `<Version>` in
`Directory.Build.props` on release. See `VERSIONING.md` at the repository root.

## [Unreleased]

## [0.4.0] - 2026-06-11
### Added
- In-process request metrics (`IMetricsCollector` + `MetricsMiddleware`) and an admin
  endpoint `GET /api/v1/admin/metrics` (active users, in-flight, totals, per-endpoint
  counts, 60s traffic series).

## [0.3.0] - 2026-06-11
### Added
- In-memory cache (`ICacheService` over `IMemoryCache`): cache-aside reads with a TTL
  backstop and explicit invalidation on writes (demonstrated on `GET .../readings/{id}`).

## [0.2.0] - 2026-06-10
### Added
- JWT authentication (register / login / me) with a `users` table and an `is_admin` role.
- File logging to daily `logs/yyyy-MM-dd.txt` files and admin-only log viewer endpoints
  (`GET /api/v1/admin/logs` and `/admin/logs/files`, `[Authorize(Roles="Admin")]`).

## [0.1.0] - 2026-06-10
### Added
- Initial version.
