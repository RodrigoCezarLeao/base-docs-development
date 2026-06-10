#!/usr/bin/env bash
#
# Provisions the HOST-level baseline of a VPS — run ONCE per box.
# Idempotent. Run as root: sudo ./provision-vps.sh <env> [hostname]
#
# After this, add one or more projects with ./add-project.sh (each gets its own
# container, host port, domain and database on the same VPS).
#
# Sets up: system upgrades + swap, deploy user, Tailscale (SSH only via tailnet),
# ufw firewall, Docker, host PostgreSQL service, and the `app` management CLI.
#
# Required environment variables:
#   TAILSCALE_AUTHKEY     auth key from the Tailscale admin console
#   DEPLOY_SSH_PUBKEY     public SSH key the CI pipeline deploys with
# Optional:
#   DEPLOY_USER           deploy user name (default: deploy)
set -euo pipefail

ENVIRONMENT="${1:-}"
HOSTNAME_LABEL="${2:-${ENVIRONMENT}-vps}"
if [[ -z "$ENVIRONMENT" ]]; then
    echo "Usage: sudo ./provision-vps.sh <env> [hostname]   (e.g. dev)" >&2
    exit 1
fi
[[ $EUID -eq 0 ]] || { echo "Must run as root (use sudo)." >&2; exit 1; }
: "${TAILSCALE_AUTHKEY:?set TAILSCALE_AUTHKEY}"
: "${DEPLOY_SSH_PUBKEY:?set DEPLOY_SSH_PUBKEY}"

DEPLOY_USER="${DEPLOY_USER:-deploy}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "═══ Provisioning host baseline (${ENVIRONMENT}) ═══"

# ── 1. System base + swap ──────────────────────────────────────────────────────
echo "→ [1/7] System packages, unattended upgrades and swap..."
export DEBIAN_FRONTEND=noninteractive
apt-get update -y && apt-get upgrade -y
apt-get install -y ca-certificates curl gnupg lsb-release ufw unattended-upgrades openssl rsync
dpkg-reconfigure -f noninteractive unattended-upgrades || true
if [[ ! -f /swapfile ]]; then
    fallocate -l 2G /swapfile && chmod 600 /swapfile && mkswap /swapfile && swapon /swapfile
    echo '/swapfile none swap sw 0 0' >> /etc/fstab
fi

# ── 2. Deploy user ─────────────────────────────────────────────────────────────
echo "→ [2/7] Deploy user '${DEPLOY_USER}'..."
id "$DEPLOY_USER" &>/dev/null || adduser --disabled-password --gecos "" "$DEPLOY_USER"
install -d -m 700 -o "$DEPLOY_USER" -g "$DEPLOY_USER" "/home/${DEPLOY_USER}/.ssh"
AUTH_KEYS="/home/${DEPLOY_USER}/.ssh/authorized_keys"
grep -qF "$DEPLOY_SSH_PUBKEY" "$AUTH_KEYS" 2>/dev/null || echo "$DEPLOY_SSH_PUBKEY" >> "$AUTH_KEYS"
chown "$DEPLOY_USER:$DEPLOY_USER" "$AUTH_KEYS"; chmod 600 "$AUTH_KEYS"

# ── 3. Tailscale (private SSH) ──────────────────────────────────────────────────
echo "→ [3/7] Tailscale..."
command -v tailscale &>/dev/null || curl -fsSL https://tailscale.com/install.sh | sh
tailscale up --authkey="$TAILSCALE_AUTHKEY" --ssh --hostname="$HOSTNAME_LABEL"

# ── 4. Firewall: no public SSH ──────────────────────────────────────────────────
echo "→ [4/7] ufw firewall..."
ufw default deny incoming
ufw default allow outgoing
ufw allow in on tailscale0          # SSH/admin only over the tailnet
ufw allow 80/tcp
ufw allow 443/tcp
ufw --force enable

# ── 5. Docker Engine ─────────────────────────────────────────────────────────--
echo "→ [5/7] Docker..."
if ! command -v docker &>/dev/null; then
    install -m 0755 -d /etc/apt/keyrings
    curl -fsSL https://download.docker.com/linux/ubuntu/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
    chmod a+r /etc/apt/keyrings/docker.gpg
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" \
        > /etc/apt/sources.list.d/docker.list
    apt-get update -y
    apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
fi
usermod -aG docker "$DEPLOY_USER"
systemctl enable --now docker

# ── 6. PostgreSQL service + nginx/certbot packages ─────────────────────────────
echo "→ [6/7] PostgreSQL, nginx, certbot..."
command -v psql &>/dev/null    || apt-get install -y postgresql postgresql-contrib
command -v nginx &>/dev/null   || apt-get install -y nginx
command -v certbot &>/dev/null || apt-get install -y certbot python3-certbot-nginx
systemctl enable --now postgresql
rm -f /etc/nginx/sites-enabled/default

# ── 7. Management CLI + host config ─────────────────────────────────────────────
echo "→ [7/7] Installing 'app' CLI and host config..."
install -m 0755 "${SCRIPT_DIR}/manage.sh" /usr/local/bin/app
install -d /opt/apps
cat > /etc/base-docs.conf <<EOF
# Written by provision-vps.sh — read by add-project.sh and the 'app' CLI.
ENVIRONMENT=${ENVIRONMENT}
DEPLOY_USER=${DEPLOY_USER}
EOF

cat <<EOF

✅ Host baseline ready (${ENVIRONMENT}). SSH is reachable only over Tailscale.

Next: add one project per API+FE pair (repeat for each project on this VPS):

  GHCR_OWNER=your-user ACME_EMAIL=you@example.com \\
    ./add-project.sh <project> <domain> <host_port>

  e.g.  ./add-project.sh temperature temp.example.com 8080
        ./add-project.sh docmap     docmap.example.com 8081

Manage anything with:  app help
EOF
