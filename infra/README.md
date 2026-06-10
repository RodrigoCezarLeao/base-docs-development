# infra/ — VPS provisioning & deployment templates

Parameterizable templates for hosting a project from this base on a VPS. Full
rationale and conventions live in [`guidelines/infra-devops.md`](../guidelines/infra-devops.md).

| File | Runs on | Purpose |
|------|---------|---------|
| `provision-vps.sh` | VPS (root, **once per box**) | Host baseline: swap, deploy user, Tailscale, ufw, Docker, PostgreSQL service, nginx/certbot, the `app` CLI. |
| `add-project.sh` | VPS (root, **once per project**) | Adds one API+FE: dedicated container, host port, domain, nginx site + HTTPS, database. Many projects coexist. |
| `setup-postgres.sh` | VPS (root) | Creates the role + database and opens the Docker bridge to PostgreSQL. Called by `add-project.sh`. |
| `manage.sh` → `app` | VPS | Day-to-day CLI: `app ls / status / logs / restart / env / pull`. Installed at `/usr/local/bin/app`. |
| `deploy-remote.sh` | VPS (deploy user) | Pulls the image from GHCR, restarts, prunes, health-checks. Invoked by CI over SSH. |
| `docker-compose.prod.yml` | VPS | One API service per project (backend only — frontend is static, DB is on the host). |
| `nginx-site.conf.template` | VPS | Serves the static frontend and reverse-proxies the API on its per-project port. |
| `.env.example` | VPS | Per-project secrets/config. Copied to `/opt/apps/<project>/.env`, never committed. |

## Quick start

```bash
# On a fresh Ubuntu 22.04/24.04 VPS, as root.
# 1) Host baseline — once per box:
export TAILSCALE_AUTHKEY=tskey-...  DEPLOY_SSH_PUBKEY="ssh-ed25519 AAAA..."
./provision-vps.sh dev

# 2) Add each project — its own domain + host port:
export GHCR_OWNER=your-github-user  ACME_EMAIL=you@example.com
./add-project.sh temperature temp.example.com   8080
./add-project.sh docmap      docmap.example.com 8081
```

Then add `VPS_HOST` / `VPS_USER` / `VPS_SSH_KEY` to the matching GitHub Environment,
add a job per project in `deploy-{dev,prod}.yml`, and trigger a deploy (merge into
`develop` or `master`). Manage everything afterwards with `app help`.
