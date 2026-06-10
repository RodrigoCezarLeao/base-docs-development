#!/usr/bin/env bash
#
# Adds ONE project (API + FE) to an already-provisioned VPS — run once per project.
# Each project gets its own container, host port, domain, nginx site and database,
# so many projects coexist on the same box. Idempotent.
#
# Run as root: sudo ./add-project.sh <project> <domain> <host_port>
#   e.g. sudo ./add-project.sh docmap docmap.example.com 8081
#
# Required environment variables:
#   GHCR_OWNER   lowercase GitHub owner/org that owns the image
#   ACME_EMAIL   email for Let's Encrypt expiry notices
# Optional:
#   DB_PASSWORD  (default: generated)   JWT_SECRET  (default: empty)
#   CORS_ORIGINS (default: https://<domain>)
set -euo pipefail

PROJECT="${1:-}"; DOMAIN="${2:-}"; API_PORT="${3:-}"
if [[ -z "$PROJECT" || -z "$DOMAIN" || -z "$API_PORT" ]]; then
    echo "Usage: sudo ./add-project.sh <project> <domain> <host_port>" >&2
    echo "Example: sudo ./add-project.sh docmap docmap.example.com 8081" >&2
    exit 1
fi
[[ $EUID -eq 0 ]] || { echo "Must run as root (use sudo)." >&2; exit 1; }
[[ -f /etc/base-docs.conf ]] || { echo "Run provision-vps.sh first (missing /etc/base-docs.conf)." >&2; exit 1; }
: "${GHCR_OWNER:?set GHCR_OWNER}"
: "${ACME_EMAIL:?set ACME_EMAIL}"

# shellcheck disable=SC1091
source /etc/base-docs.conf            # ENVIRONMENT, DEPLOY_USER
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DB_NAME="${PROJECT}_${ENVIRONMENT}"
DB_USER="${PROJECT}_${ENVIRONMENT}"
DB_PASSWORD="${DB_PASSWORD:-$(openssl rand -base64 24)}"
JWT_SECRET="${JWT_SECRET:-}"
CORS_ORIGINS="${CORS_ORIGINS:-https://${DOMAIN}}"
APP_DIR="/opt/apps/${PROJECT}"
WEB_DIR="/var/www/${PROJECT}"

echo "═══ Adding ${PROJECT} → ${DOMAIN} (host port ${API_PORT}, ${ENVIRONMENT}) ═══"

# Reject a port already taken by another project on this box.
if grep -Rqs "^API_PORT=${API_PORT}$" /opt/apps/*/.env 2>/dev/null && [[ ! -f "${APP_DIR}/.env" ]]; then
    echo "Port ${API_PORT} is already used by another project. Pick a free one." >&2
    exit 1
fi

# ── Database ────────────────────────────────────────────────────────────────--
DB_PASSWORD="$DB_PASSWORD" bash "${SCRIPT_DIR}/setup-postgres.sh" "$DB_NAME" "$DB_USER" "$DB_PASSWORD"

# ── App layout + .env + compose ─────────────────────────────────────────────────
install -d -o "$DEPLOY_USER" -g "$DEPLOY_USER" "$APP_DIR" "$WEB_DIR"
cp "${SCRIPT_DIR}/docker-compose.prod.yml" "${APP_DIR}/docker-compose.yml"
cp "${SCRIPT_DIR}/deploy-remote.sh" "${APP_DIR}/deploy-remote.sh"
chmod +x "${APP_DIR}/deploy-remote.sh"
ENV_FILE="${APP_DIR}/.env"
if [[ ! -f "$ENV_FILE" ]]; then
    cat > "$ENV_FILE" <<EOF
PROJECT=${PROJECT}
GHCR_OWNER=${GHCR_OWNER}
IMAGE_TAG=latest
API_PORT=${API_PORT}
DB_NAME=${DB_NAME}
DB_USER=${DB_USER}
DB_PASSWORD=${DB_PASSWORD}
CORS_ORIGINS=${CORS_ORIGINS}
JWT_SECRET=${JWT_SECRET}
EOF
fi
chown "$DEPLOY_USER:$DEPLOY_USER" "$APP_DIR/docker-compose.yml" "$APP_DIR/deploy-remote.sh" "$ENV_FILE"
chmod 600 "$ENV_FILE"

# ── nginx site + HTTPS ──────────────────────────────────────────────────────────
SITE="/etc/nginx/sites-available/${PROJECT}"
sed -e "s/__PROJECT__/${PROJECT}/g" -e "s/__DOMAIN__/${DOMAIN}/g" -e "s/__API_PORT__/${API_PORT}/g" \
    "${SCRIPT_DIR}/nginx-site.conf.template" > "$SITE"
ln -sf "$SITE" "/etc/nginx/sites-enabled/${PROJECT}"
[[ -f "${WEB_DIR}/index.html" ]] || echo "<h1>${PROJECT} (${ENVIRONMENT}) — awaiting first deploy</h1>" > "${WEB_DIR}/index.html"
nginx -t && systemctl reload nginx
certbot --nginx -d "$DOMAIN" --non-interactive --agree-tos -m "$ACME_EMAIL" --redirect

cat <<EOF

✅ ${PROJECT} added on ${DOMAIN} (host port ${API_PORT}).

Next steps:
  1. Add a job calling deploy.yml for '${PROJECT}' in deploy-{dev,prod}.yml.
  2. In the '${ENVIRONMENT}' GitHub Environment, ensure VPS_HOST/USER/SSH_KEY + TS_OAUTH_* are set.
  3. Merge into develop/master → CI ships the image + frontend here.
  4. Verify: https://${DOMAIN}/health   |   inspect with: app status

Secrets live in ${ENV_FILE} (edit with: app env ${PROJECT}).
EOF
