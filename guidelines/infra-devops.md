# Guideline — Infrastructure & Deployment (VPS + CI/CD)

## Purpose

This document defines the adopted standard for getting a project from this base
**onto a server and exposed over HTTPS**. It covers the CI/CD pipeline, how a VPS is
provisioned, and the runtime topology. The companion code lives in [`infra/`](../infra)
and [`.github/workflows/`](../.github/workflows).

Any developer (or Claude) should be able to stand up a new environment by following
this guide, without inventing structure. The guiding principles:

- **The VPS never compiles.** CI builds artifacts; the server only pulls and runs them.
- **Backend runs in Docker** (isolation + a hard memory ceiling so a leak can't take the box down). **The frontend is static** and served directly by the host nginx — no container, minimal disk.
- **PostgreSQL runs on the host**, not in a container. Data outlives any container churn.
- **No public SSH.** Administration happens over Tailscale; only 80/443 are open to the internet.
- **DEV and PROD are separate VPS** with the same shape and different secrets/domains.

```
                      Internet (80/443 only)
                              │
                    ┌─────────▼─────────┐
                    │   nginx (host)    │  TLS via certbot / Let's Encrypt
                    │  - serves FE dist │  /var/www/<project>
                    │  - proxies /api ──┐│
                    └─────────────────┼─┘
                                      │ 127.0.0.1:8080
                    ┌─────────────────▼─────────────────┐
                    │  Docker: API container (.NET)      │  restart=unless-stopped
                    │  ASPNETCORE_URLS=http://+:8080      │  mem_limit + healthcheck
                    └─────────────────┬─────────────────┘
                                      │ host.docker.internal:5432
                    ┌─────────────────▼─────────────────┐
                    │  PostgreSQL 16 (host, localhost)   │  one db + role per project
                    └───────────────────────────────────┘

  SSH: only via Tailscale (tailscale0). ufw blocks public 22, allows 80/443.
```

---

## VPS topology — DEV and PROD, many projects per box

Two separate VPS — one DEV, one PROD — identical in shape, different secrets/domains.
**Each VPS hosts one or more projects.** Every project is fully isolated on the box:

| Per project | Value |
|---|---|
| Container | `<project>-api` |
| Host port (loopback) | `API_PORT` — unique per project (8080, 8081, 8082, …) |
| Domain | its own (`temp.example.com`, `docmap.example.com`) + nginx site |
| Database | `<project>_<env>` with its own role |
| App dir | `/opt/apps/<project>/` (compose + `.env`) |
| Frontend | `/var/www/<project>/` |

DEV vs PROD differences:

| | DEV | PROD |
|---|---|---|
| Trigger | merge into `develop` | merge into `master` (+ manual approval) |
| Domain | `*.dev.example.com` | `*.example.com` |
| Database suffix | `<project>_dev` | `<project>_prod` |
| GitHub Environment | `dev` | `production` |
| Image tag deployed | commit SHA | commit SHA |

> ✅ Keep DEV and PROD on truly separate boxes. A shared box re-introduces the blast
> radius we are trying to avoid. Within a box, projects are isolated by their own
> port + domain + container + database.

> **Port allocation:** assign each project a dedicated loopback port and keep a note of
> it (8080, 8081, …). `add-project.sh` refuses a port already taken by another project.

---

## Provisioning a VPS

Two idempotent steps on a fresh Ubuntu 22.04/24.04 VPS, as root. Copy the `infra/`
folder onto the box first.

**Step 1 — host baseline (once per box):**

```bash
cd infra
export TAILSCALE_AUTHKEY="tskey-auth-..."       # from the Tailscale admin console
export DEPLOY_SSH_PUBKEY="ssh-ed25519 AAAA..."  # CI's deploy public key
# optional: DEPLOY_USER
./provision-vps.sh dev
```

Sets up: system upgrades + swap → deploy user → Tailscale (with SSH) → ufw → Docker →
PostgreSQL service → nginx/certbot packages → the `app` CLI. Writes `/etc/base-docs.conf`.

**Step 2 — add each project (once per project):**

```bash
export GHCR_OWNER="your-github-user"   # lowercase
export ACME_EMAIL="you@example.com"
# optional: DB_PASSWORD, JWT_SECRET, CORS_ORIGINS
./add-project.sh temperature temp.example.com   8080
./add-project.sh docmap      docmap.example.com 8081
```

Per project this creates: database + role (via `setup-postgres.sh`) → app layout +
`.env` (with `API_PORT`) → nginx site on its domain → TLS certificate. Re-running with
the same args converges; it refuses a port already used by another project.

**Pre-requisite:** a DNS `A` record for each `<domain>` pointing at the VPS public IP
(certbot needs it to issue the certificate).

Resulting layout on the box:

```
/etc/base-docs.conf       ← ENVIRONMENT + DEPLOY_USER (written by provision-vps.sh)
/usr/local/bin/app        ← management CLI
/opt/apps/<project>/
  docker-compose.yml      ← from infra/docker-compose.prod.yml
  deploy-remote.sh        ← from infra/deploy-remote.sh
  .env                    ← secrets incl. API_PORT, 0600, never leaves the box
/var/www/<project>/       ← static frontend (rsynced by CI)
```

---

## Tailscale & SSH hardening

- The VPS joins the tailnet during provisioning (`tailscale up --ssh`). Administration
  uses **Tailscale SSH** — no keys to manage, identity comes from the tailnet.
- `ufw` denies all inbound except `tailscale0`, `80`, and `443`. **Public port 22 is closed.**
- CI reaches the box by joining the tailnet ephemerally (the `tailscale/github-action`
  step) and then `ssh`/`rsync` over the tailnet address.

> ✅ The tailnet ACL must grant `tag:ci` permission to SSH into the server tags.
> The deploy job authenticates with a Tailscale **OAuth client** (`TS_OAUTH_CLIENT_ID`
> / `TS_OAUTH_SECRET`), which is the durable, non-expiring way to connect CI.

> ❌ Never open port 22 to `0.0.0.0/0` "just to debug". If you're locked out of the
> tailnet, use the VPS provider's web console.

---

## PostgreSQL on the host

PostgreSQL is installed with `apt` and runs as a normal systemd service. One database
and one login role per project/environment (`<project>_<env>`), created idempotently by
`setup-postgres.sh`.

**The one gotcha — reaching the host DB from inside the container:**

- The container resolves the host via `host.docker.internal`, mapped to the bridge
  gateway by `extra_hosts: ["host.docker.internal:host-gateway"]` in the compose file.
- PostgreSQL must therefore listen on the bridge gateway too:
  `listen_addresses = 'localhost,172.17.0.1'`.
- `pg_hba.conf` must allow the Docker subnet for that role:
  `host <db> <user> 172.16.0.0/12 scram-sha-256`.

`setup-postgres.sh` applies all three. The connection string the container uses is
built in the compose file:

```
Host=host.docker.internal;Port=5432;Database=<db>;Username=<user>;Password=<...>
```

> ✅ The database listens only on `localhost` + the bridge gateway, never on a public
> interface. Combined with ufw, it is unreachable from the internet.

> **Note — migrations run on container startup.** `Program.cs` calls
> `IMigrationRunner.Run()` (DbUp) at boot, so each deploy applies pending scripts
> automatically. Run a **single replica** so two containers don't race the migration.

### Backups

Schedule a daily `pg_dump` with retention via cron on the host:

```bash
# /etc/cron.d/<project>-backup  — 03:15 daily, keep 14 days
15 3 * * * postgres pg_dump <project>_prod | gzip > /var/backups/<project>_$(date +\%F).sql.gz && find /var/backups -name '<project>_*.sql.gz' -mtime +14 -delete
```

---

## Docker conventions (backend)

The API ships as a small image built by a **multi-stage** `Dockerfile` (see
[`projects/temperature-api/Dockerfile`](../projects/temperature-api/Dockerfile)). It is
**name-agnostic** — the same file works for any project from the base because it
discovers the `*.Api` project at build time.

- **Build stage:** `mcr.microsoft.com/dotnet/sdk:8.0`, `dotnet restore --locked-mode`
  (reproducible, fails on stale `packages.lock.json`), then `dotnet publish -c Release`.
- **Runtime stage:** `mcr.microsoft.com/dotnet/aspnet:8.0-alpine` (~110 MB), **non-root
  user**, `EXPOSE 8080`, `ASPNETCORE_URLS=http://+:8080`.
- **`.dockerignore`** excludes `bin/`, `obj/`, `tests/`, docs and local env files.

**Configuration is injected, not baked.** The image carries no secrets; everything is
overridden via environment variables using ASP.NET's `__` (double-underscore) section
syntax — reusing the existing binding in `Program.cs`, **no code change**:

| App setting | Environment variable |
|---|---|
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` |
| `Cors:AllowedOrigins` | `Cors__AllowedOrigins` |
| `Jwt:Secret` | `Jwt__Secret` |
| environment | `ASPNETCORE_ENVIRONMENT=Production` |

**Guarding against memory leaks** (a stated goal of containerizing the backend):

- `mem_limit: 512m` + `mem_reservation: 256m` — a runaway container is OOM-killed and
  restarted, never the host.
- `DOTNET_GCHeapHardLimitPercent=50` — the GC respects the container ceiling.
- `restart: unless-stopped` — automatic recovery after a crash or OOM.
- `healthcheck` on `/health` and capped `json-file` logging (`max-size`, `max-file`).

> ❌ Don't run the frontend in a container. It's static files — host nginx serves them
> straight from `/var/www/<project>`, which saves an image per deploy and keeps disk lean.

---

## nginx + HTTPS

The host nginx terminates TLS, serves the SPA, and reverse-proxies the API. The vhost
is generated from [`infra/nginx-site.conf.template`](../infra/nginx-site.conf.template):

- `root /var/www/<project>` with `try_files $uri $uri/ /index.html` (SPA fallback).
- `location /api/`, `= /ping`, `= /health` → `proxy_pass http://127.0.0.1:8080`
  **without path rewrite** — controllers already own the `/api/v1/...` prefix.
- Security headers + long-cache for hashed `/assets/`.
- `certbot --nginx` issues the certificate and rewrites the vhost for 443 + the
  80→443 redirect. Renewal is automatic via the certbot systemd timer.

> ✅ Because the API is published only on `127.0.0.1:8080`, the only way in is through
> nginx over TLS. The container port is never exposed publicly.

---

## CI/CD pipeline

> **This base repo vs. a project repo.** Everything here — the deploy workflows, branch
> protection, environments — is meant for the **project repos created from this base**,
> not the base itself. The base only carries them as ready-to-use templates. So the
> deploy workflows are **gated**: each deploy job runs only when the repository variable
> `ENABLE_DEPLOY` is `"true"` (Settings → Secrets and variables → Actions → Variables).
> The base leaves it unset (deploy jobs are skipped, no failed runs); a project repo sets
> it to `true` to turn deployment on. `ci.yml` is **not** gated — it validates the
> templates in the base too.

Three workflows under `.github/workflows/`:

| Workflow | Trigger | Does |
|---|---|---|
| `ci.yml` | any push / PR to `master`,`develop` | lint + test + **build** (BE and FE) — quality gate, always on |
| `deploy.yml` | `workflow_call` | reusable: build image → GHCR, build FE, ship to VPS |
| `deploy-dev.yml` | push to `develop`, if `ENABLE_DEPLOY` | calls `deploy.yml` with `environment: dev` |
| `deploy-prod.yml` | push to `master`, if `ENABLE_DEPLOY` | calls `deploy.yml` with `environment: production` |

**Artifact flow** (the VPS never compiles):

1. **build-backend** — `docker/build-push-action` builds the image and pushes it to
   **GHCR** tagged with the commit SHA (immutable) and the environment name. Layer cache
   via GitHub Actions cache.
2. **build-frontend** — `pnpm build` with `VITE_API_URL` set for the target environment.
   > ⚠️ Vite **inlines** `VITE_API_URL` at build time, so the frontend is built **per
   > environment** — the dev build points at the dev API, the prod build at the prod API.
   The `dist/` is uploaded as an artifact.
3. **deploy** — joins Tailscale, `rsync`s `dist/` to `/var/www/<project>`, then SSHes in
   to run `deploy-remote.sh`, which pulls the image, restarts the stack, prunes old
   images, and health-checks `/health`.

**To deploy more than one project**, add another job in the caller that calls
`deploy.yml` with that project's inputs (see the commented `docmap` block in
`deploy-dev.yml`). Each project typically targets its own VPS.

### Branch strategy & protection (in the project repo)

Apply these in each **project repo** created from the base — not in the base itself:

- `master` = production (default branch), `develop` = dev, created from `master`.
- Both **protected** (Settings → Branches): require a PR, require the `ci.yml` status
  checks, require ≥1 review, block force-push — so code lands only via PR.
- Set the repository variable `ENABLE_DEPLOY=true` to activate the deploy workflows.
- Create the `dev` and `production` Environments (Settings → Environments); give
  `production` **Required reviewers** so a prod deploy pauses for a manual click.

---

## Secrets & environment management

Two clearly separated buckets:

| Lives in… | Holds | Why |
|---|---|---|
| `/opt/apps/<project>/.env` on the **VPS** (0600) | `DB_PASSWORD`, `JWT_SECRET`, `CORS_ORIGINS`, `DB_*` | App secrets never enter CI; provisioned once, updated rarely. |
| GitHub **Environment** secrets (`dev`, `production`) | `TS_OAUTH_CLIENT_ID`, `TS_OAUTH_SECRET`, `VPS_HOST`, `VPS_USER`, `VPS_SSH_KEY` | Only transport/registry access. |

- The deploy job updates **only** `IMAGE_TAG` in the VPS `.env` — it never reads or
  rewrites the application secrets. Humans edit the `.env` with `app env <project>`.
- GHCR pulls on the VPS use the run-scoped `GITHUB_TOKEN` (`packages: read`), passed over
  the SSH session for the duration of the deploy.

> ❌ Never commit a real `.env`, a private SSH key, or a JWT secret. `infra/.env.example`
> is the only env file in the repo, and it holds placeholders.

---

## Disk & memory hygiene

- `deploy-remote.sh` runs `docker image prune -f` after every deploy — only the current
  and in-use images remain.
- Images are small (alpine runtime) and the frontend isn't containerized.
- 2 GB swap (set during provisioning) absorbs spikes; `mem_limit` caps the container.
- Capped container logs (`max-size: 10m`, `max-file: 3`) prevent log-driven disk fill.

---

## Operations — the `app` CLI

`provision-vps.sh` installs `/usr/local/bin/app`, one command for day-to-day work so
maintenance is standardized across every project on the box (no remembering compose
paths). Run over Tailscale SSH:

```bash
app ls                  # all projects, their port and state
app status              # containers + disk + memory at a glance
app logs docmap -f      # follow a project's logs
app restart docmap      # restart one project
app pull docmap         # pull the current image tag and recreate
app env docmap          # edit /opt/apps/docmap/.env, then offer to restart
```

> ✅ Change a secret or a CORS origin with `app env <project>` — it opens the `.env`
> and restarts the container for you. No manual `docker compose` invocations.

---

## Rollback

Images are tagged by **immutable commit SHA** in GHCR, so rolling back is re-deploying
an earlier SHA:

```bash
# On the VPS, as the deploy user:
GHCR_USER=<user> GHCR_TOKEN=<token> \
  bash /opt/apps/<project>/deploy-remote.sh <previous-sha>
```

For the frontend, re-run the deploy workflow from the previous green commit (Actions →
Re-run jobs) so the matching `dist/` is rsynced back.

---

## Checklists

### Provision a new VPS (per environment, once)

- [ ] Tailscale auth key generated; tailnet ACL allows `tag:ci` to reach the server
- [ ] CI deploy keypair created; public key exported as `DEPLOY_SSH_PUBKEY`
- [ ] Run `provision-vps.sh <env>` as root
- [ ] `ufw status` shows only `tailscale0`, 80, 443; public SSH closed
- [ ] GitHub Environment secrets added (`VPS_HOST/USER/SSH_KEY`, `TS_OAUTH_*`)

### Add a project to a VPS (per project)

- [ ] DNS `A` record for the project's domain points at the VPS IP
- [ ] Pick a free host port (8080, 8081, …)
- [ ] Run `add-project.sh <project> <domain> <port>` as root
- [ ] `https://<domain>/health` returns healthy; certificate valid (`certbot certificates`)
- [ ] Daily `pg_dump` cron in place for the project's database
- [ ] `Dockerfile` + `.dockerignore` present in the API project (copy from the base)
- [ ] Add a job calling `deploy.yml` in `deploy-dev.yml` / `deploy-prod.yml` with the project's `project` / `api_path` / `web_path` / `api_base_url`
- [ ] `master` + `develop` protected; `production` Environment requires reviewers
- [ ] First deploy verified end-to-end on DEV before enabling PROD
