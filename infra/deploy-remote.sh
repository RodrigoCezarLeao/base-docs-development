#!/usr/bin/env bash
#
# Runs ON THE VPS, invoked by the CI deploy job over SSH (Tailscale).
# Pulls the new image from GHCR, restarts the stack, prunes old images, health-checks.
#
# Lives at /opt/apps/<project>/deploy-remote.sh (copied there by provision-vps.sh).
#
# Usage: ./deploy-remote.sh <image_tag>
# Required env (passed by the CI job): GHCR_USER, GHCR_TOKEN
set -euo pipefail

IMAGE_TAG="${1:?usage: ./deploy-remote.sh <image_tag>}"
: "${GHCR_USER:?set GHCR_USER}"
: "${GHCR_TOKEN:?set GHCR_TOKEN}"

cd "$(dirname "${BASH_SOURCE[0]}")"   # /opt/apps/<project>

echo "→ Logging in to GHCR..."
echo "$GHCR_TOKEN" | docker login ghcr.io -u "$GHCR_USER" --password-stdin

echo "→ Pinning IMAGE_TAG=${IMAGE_TAG} in .env..."
if grep -q '^IMAGE_TAG=' .env; then
    sed -i "s/^IMAGE_TAG=.*/IMAGE_TAG=${IMAGE_TAG}/" .env
else
    echo "IMAGE_TAG=${IMAGE_TAG}" >> .env
fi

echo "→ Pulling and restarting..."
docker compose pull api
docker compose up -d api

echo "→ Pruning dangling images (keep disk lean)..."
docker image prune -f
docker logout ghcr.io || true

echo "→ Health check..."
API_PORT="$(grep -E '^API_PORT=' .env | cut -d= -f2)"; API_PORT="${API_PORT:-8080}"
for i in $(seq 1 20); do
    if wget --spider -q "http://127.0.0.1:${API_PORT}/health"; then
        echo "✅ Deploy healthy (IMAGE_TAG=${IMAGE_TAG})."
        exit 0
    fi
    sleep 3
done

echo "❌ Health check failed after restart. Recent logs:" >&2
docker compose logs --tail=50 api >&2
exit 1
