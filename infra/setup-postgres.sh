#!/usr/bin/env bash
#
# Creates a role + database on the HOST PostgreSQL and allows the Docker bridge
# network to connect, so the API container can reach the DB via host.docker.internal.
#
# Idempotent: safe to run repeatedly. Run as root (it uses `sudo -u postgres`).
#
# Usage: ./setup-postgres.sh <db_name> <db_user> <db_password>
set -euo pipefail

DB_NAME="${1:-}"
DB_USER="${2:-}"
DB_PASSWORD="${3:-}"

if [[ -z "$DB_NAME" || -z "$DB_USER" || -z "$DB_PASSWORD" ]]; then
    echo "Usage: ./setup-postgres.sh <db_name> <db_user> <db_password>" >&2
    exit 1
fi

psql_su() { sudo -u postgres psql -v ON_ERROR_STOP=1 "$@"; }

echo "→ Ensuring role '${DB_USER}'..."
if [[ "$(psql_su -tAc "SELECT 1 FROM pg_roles WHERE rolname='${DB_USER}'")" != "1" ]]; then
    psql_su -c "CREATE ROLE \"${DB_USER}\" LOGIN PASSWORD '${DB_PASSWORD}';"
else
    # Keep the password in sync with the .env on every run.
    psql_su -c "ALTER ROLE \"${DB_USER}\" WITH PASSWORD '${DB_PASSWORD}';"
fi

echo "→ Ensuring database '${DB_NAME}'..."
if [[ "$(psql_su -tAc "SELECT 1 FROM pg_database WHERE datname='${DB_NAME}'")" != "1" ]]; then
    psql_su -c "CREATE DATABASE \"${DB_NAME}\" OWNER \"${DB_USER}\";"
fi

# ── Allow the Docker bridge to reach PostgreSQL ────────────────────────────────
CONF_FILE="$(psql_su -tAc 'SHOW config_file')"
HBA_FILE="$(psql_su -tAc 'SHOW hba_file')"

echo "→ Configuring listen_addresses in ${CONF_FILE}..."
# Listen on localhost + the docker bridge gateway. Public access stays blocked by ufw.
if ! grep -qE "^\s*listen_addresses\s*=\s*'localhost,172.17.0.1'" "$CONF_FILE"; then
    sed -i "/^\s*#\?\s*listen_addresses\s*=/d" "$CONF_FILE"
    echo "listen_addresses = 'localhost,172.17.0.1'" >> "$CONF_FILE"
fi

echo "→ Configuring host access in ${HBA_FILE}..."
HBA_LINE="host    ${DB_NAME}    ${DB_USER}    172.16.0.0/12    scram-sha-256"
if ! grep -qF "$HBA_LINE" "$HBA_FILE"; then
    echo "# Added by setup-postgres.sh — Docker bridge access for ${DB_USER}" >> "$HBA_FILE"
    echo "$HBA_LINE" >> "$HBA_FILE"
fi

echo "→ Reloading PostgreSQL..."
systemctl reload postgresql

echo "✅ PostgreSQL ready: database '${DB_NAME}' / role '${DB_USER}'."
