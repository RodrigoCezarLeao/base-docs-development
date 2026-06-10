#!/usr/bin/env bash
#
# Bootstraps a new full-stack project (API + Web) from the temperature-* templates.
#
#   - Copies temperature-api  → projects/<slug>-api   and renames the namespace/db
#     (delegates to the backend's rename.sh).
#   - Copies temperature-web  → projects/<slug>-web   and renames its identity
#     (package name + page title). The example feature is kept as a reference.
#   - Reassigns local ports (API / PostgreSQL / dev server) so the new project
#     runs alongside the existing ones without collisions.
#   - Appends commented deploy stubs to the deploy workflows, ready to fill in.
#
# Run from the repository root:  ./create-project.sh <slug>
#   <slug> is kebab-case, e.g.  orders  |  product-catalog
set -euo pipefail

SLUG="${1:-}"
if [[ -z "$SLUG" ]]; then
    echo "Usage: ./create-project.sh <slug>        (kebab-case, e.g. orders, product-catalog)" >&2
    exit 1
fi
if [[ ! "$SLUG" =~ ^[a-z][a-z0-9]*(-[a-z0-9]+)*$ || ${#SLUG} -lt 2 ]]; then
    echo "Error: <slug> must be kebab-case, start with a letter, min 2 chars (e.g. orders, product-catalog)." >&2
    exit 1
fi
[[ -d projects/temperature-api && -d projects/temperature-web ]] || {
    echo "Error: run this from the repository root (projects/temperature-* not found)." >&2; exit 1; }

# slug → PascalCase namespace and a human Title.
NS=$(echo "$SLUG"    | sed -E 's/(^|-)([a-z])/\U\2/g')
TITLE=$(echo "$SLUG" | sed -E 's/(^|-)([a-z])/ \U\2/g' | sed 's/^ //')
API_DIR="projects/${SLUG}-api"
WEB_DIR="projects/${SLUG}-web"

[[ -e "$API_DIR" || -e "$WEB_DIR" ]] && { echo "Error: $API_DIR or $WEB_DIR already exists." >&2; exit 1; }

# ── Port allocation (avoid collisions with existing projects) ───────────────────
max_plus_one() { { cat; echo "$2"; } | tr ' ' '\n' | grep -E '^[0-9]+$' | sort -n | tail -1 | awk '{print $1+1}'; }

api_ports=$(grep -rhoE 'localhost:[0-9]+' projects/*-api/src/*/Properties/launchSettings.json 2>/dev/null | grep -oE '[0-9]+$' | tr '\n' ' ')
web_ports=$(grep -rhoE 'port:[[:space:]]*[0-9]+' projects/*-web/vite.config.ts 2>/dev/null | grep -oE '[0-9]+' | tr '\n' ' ')
pg_ports=$( grep -rhoE '[0-9]+:5432'        projects/*-api/docker-compose.yml 2>/dev/null | cut -d: -f1   | tr '\n' ' ')

NEW_PG=$(echo "$pg_ports"               | max_plus_one - "5432 5433")
NEW_API=$(echo "$api_ports $web_ports"  | max_plus_one - "5000 5172 5173 5174")
NEW_WEB=$((NEW_API + 1))

echo "Creating project '${SLUG}'"
echo "  Namespace : ${NS}"
echo "  Title     : ${TITLE}"
echo "  Ports     : API ${NEW_API}  |  Web ${NEW_WEB}  |  PostgreSQL ${NEW_PG}"
echo ""

# ── Copy templates (excluding heavy build/dependency dirs) ──────────────────────
copy_template() {   # $1 = template dir name, $2 = dest path
    local tmp; tmp="$(mktemp -d)"
    ( cd projects && tar \
        --exclude=node_modules --exclude=dist --exclude=.vite --exclude=coverage \
        --exclude=bin --exclude=obj --exclude=.vs --exclude=TestResults \
        -cf - "$1" ) | ( cd "$tmp" && tar -xf - )
    mv "$tmp/$1" "$2"
    rmdir "$tmp"
}
copy_template temperature-api "$API_DIR"
copy_template temperature-web "$WEB_DIR"

# ── Backend: rename namespace/db, then reassign ports ───────────────────────────
( cd "$API_DIR" && bash rename.sh "$NS" >/dev/null )
rm -f "$API_DIR/rename.sh"
sed -i "s|localhost:5000|localhost:${NEW_API}|g" "$API_DIR/src/${NS}.Api/Properties/launchSettings.json"
sed -i -e "s|Port=5432|Port=${NEW_PG}|g" -e "s|localhost:5173|localhost:${NEW_WEB}|g" \
    "$API_DIR/src/${NS}.Api/appsettings.json"
sed -i "s|\"5432:5432\"|\"${NEW_PG}:5432\"|g" "$API_DIR/docker-compose.yml"

# ── Frontend: rename identity + ports ───────────────────────────────────────────
sed -i "s|\"name\": \"temperature-web\"|\"name\": \"${SLUG}-web\"|" "$WEB_DIR/package.json"
sed -i "s|<title>.*</title>|<title>${TITLE}</title>|" "$WEB_DIR/index.html"
sed -i "s|  plugins: \[react(), tailwindcss()\],|  plugins: [react(), tailwindcss()],\n  server: { port: ${NEW_WEB} },|" \
    "$WEB_DIR/vite.config.ts"
for f in "$WEB_DIR"/.env.example "$WEB_DIR"/.env.development "$WEB_DIR"/.env.local; do
    [[ -f "$f" ]] && sed -i "s|localhost:5000|localhost:${NEW_API}|g" "$f"
done

# ── Deploy stubs (commented — fill in the domain, then uncomment) ───────────────
append_deploy_stub() {   # $1 = workflow file, $2 = environment, $3 = api_base_url
    cat >> "$1" <<EOF

  # ${SLUG}:
  #   uses: ./.github/workflows/deploy.yml
  #   with:
  #     environment: ${2}
  #     project: ${SLUG}
  #     api_path: ${API_DIR}
  #     web_path: ${WEB_DIR}
  #     api_base_url: ${3}
  #   secrets: inherit
EOF
}
append_deploy_stub .github/workflows/deploy-dev.yml  dev        "https://${SLUG}-dev.example.com"
append_deploy_stub .github/workflows/deploy-prod.yml production "https://${SLUG}.example.com"

cat <<EOF

✅ Project '${SLUG}' created.

  Backend : ${API_DIR}   (namespace ${NS})
  Frontend: ${WEB_DIR}
  Ports   : API ${NEW_API}  |  Web ${NEW_WEB}  |  PostgreSQL ${NEW_PG}

Next steps:
  1. Run it locally:
       (cd ${API_DIR} && docker compose up -d && dotnet run --project src/${NS}.Api)
       (cd ${WEB_DIR} && pnpm install && pnpm dev)
  2. Add CI jobs for '${SLUG}' in .github/workflows/ci.yml (mirror the temperature jobs).
  3. To deploy: uncomment the '${SLUG}' blocks in deploy-{dev,prod}.yml and set the real
     domains, then provision the VPS — see guidelines/infra-devops.md:
       ./add-project.sh ${SLUG} <domain> ${NEW_API}
  4. Add this project's ports to the "Local ports" table in CLAUDE.md.

The temperature example (TemperatureReading / temperature-list) is kept as a reference.
Add your own entities/features following the same pattern, then remove it.
EOF
