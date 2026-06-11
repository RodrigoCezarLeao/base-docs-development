# Changelog

All notable changes to this project are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project follows
[Semantic Versioning](https://semver.org/). Bump the `<Version>` in
`Directory.Build.props` on release. See `VERSIONING.md` at the repository root.

## [Unreleased]

## [0.5.0] - 2026-06-11
### Added
- LGPD-compliant user access tracking. A consent gate (`X-Tracking-Consent` header →
  `ConsentMiddleware`) blocks **all** per-request observation until the user accepts:
  access tracking, the request log, per-user metrics, and the file logger's user identity
  are all suppressed pre-consent.
- `access_events` capture pipeline: `AccessTrackingMiddleware` parses the User-Agent
  (UAParser) and enqueues to a `Channel`, drained by a background `AccessEventWriter` that
  batch-inserts (no request-latency cost). Optional IP anonymization via `Tracking:AnonymizeIp`.
- Consent audit (`consents` table) + endpoints: `POST /api/v1/me/consent` (granted / denied /
  withdrawn), `DELETE /api/v1/me/tracking-data` (right to erasure — hard delete, no backup),
  and admin `GET /api/v1/admin/access` + `DELETE /api/v1/admin/access/users/{userId}`.

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
