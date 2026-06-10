#!/usr/bin/env bash
#
# `app` — one command to manage every project on the VPS.
# Installed at /usr/local/bin/app by provision-vps.sh. Each project lives in
# /opt/apps/<project> with its own docker-compose.yml + .env.
#
# Standardizes the day-to-day: logs, restart, config edits, status.
set -euo pipefail

APPS_ROOT="/opt/apps"

usage() {
    cat <<'EOF'
app — manage projects on this VPS

Usage:
  app ls                      list projects and their status
  app status                  overview: containers + disk + memory
  app ps       <project>      container status for a project
  app logs     <project> [..] tail logs (pass -f to follow, e.g. app logs api -f)
  app restart  <project>      restart the project's container
  app up       <project>      start (or recreate) the container
  app down     <project>      stop and remove the container
  app pull     <project>      pull the latest image tag from .env and restart
  app env      <project>      edit .env, then offer to restart
  app help                    this help
EOF
}

dir_for() {
    local p="$1" d="${APPS_ROOT}/$1"
    [[ -d "$d" ]] || { echo "Unknown project '$p'. Try: app ls" >&2; exit 1; }
    echo "$d"
}

# Run a docker compose command in a project directory.
dc() { ( cd "$(dir_for "$1")" && shift && docker compose "$@" ); }

cmd="${1:-help}"; shift || true

case "$cmd" in
    ls)
        printf "%-20s %-10s %s\n" "PROJECT" "PORT" "STATE"
        for d in "$APPS_ROOT"/*/; do
            [[ -f "$d/docker-compose.yml" ]] || continue
            p="$(basename "$d")"
            port="$(grep -E '^API_PORT=' "$d/.env" 2>/dev/null | cut -d= -f2)"
            state="$( ( cd "$d" && docker compose ps --format '{{.State}}' 2>/dev/null ) | head -n1)"
            printf "%-20s %-10s %s\n" "$p" "${port:-?}" "${state:-stopped}"
        done
        ;;
    status)
        echo "── Containers ──"; docker ps --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}'
        echo; echo "── Disk ──";   df -h / | tail -n +1
        echo; echo "── Memory ──"; free -h
        ;;
    ps)      dc "${1:?project}" ps ;;
    logs)    proj="${1:?project}"; shift; dc "$proj" logs --tail=200 "$@" ;;
    restart) dc "${1:?project}" restart ;;
    up)      dc "${1:?project}" up -d ;;
    down)    dc "${1:?project}" down ;;
    pull)    proj="${1:?project}"; dc "$proj" pull && dc "$proj" up -d ;;
    env)
        proj="${1:?project}"; f="$(dir_for "$proj")/.env"
        "${EDITOR:-nano}" "$f"
        read -rp "Restart ${proj} to apply changes? [y/N] " a
        [[ "$a" =~ ^[Yy]$ ]] && dc "$proj" up -d || echo "Not restarted."
        ;;
    help|-h|--help) usage ;;
    *) echo "Unknown command '$cmd'." >&2; usage; exit 1 ;;
esac
