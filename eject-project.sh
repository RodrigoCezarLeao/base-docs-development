#!/usr/bin/env bash
#
# Ejects a standalone project from this base into ANOTHER directory, ready to be
# its own git repository. Unlike create-project.sh (which scaffolds inside this
# repo under projects/), this produces a self-contained, flat single-product repo:
#
#   <target>/
#     api/            ← renamed backend (from temperature-api)
#     web/            ← renamed frontend (from temperature-web)
#     infra/          ← VPS provisioning & deploy templates
#     guidelines/     ← infra-devops.md reference
#     .github/workflows/  ← ci.yml + deploy.yml + deploy-{dev,prod}.yml (flat paths)
#     .gitignore, README.md
#
# It renames the namespace/identity, keeps the example domain as a reference,
# wires the workflows to the flat api/ + web/ layout, and runs `git init` +
# initial commit on `master`.
#
# Run from the repository root:  ./eject-project.sh <slug> <target-dir>
#   e.g.  ./eject-project.sh orders ../orders
set -euo pipefail

SLUG="${1:-}"; TARGET="${2:-}"
if [[ -z "$SLUG" || -z "$TARGET" ]]; then
    echo "Usage: ./eject-project.sh <slug> <target-dir>     (e.g. ./eject-project.sh orders ../orders)" >&2
    exit 1
fi
if [[ ! "$SLUG" =~ ^[a-z][a-z0-9]*(-[a-z0-9]+)*$ || ${#SLUG} -lt 2 ]]; then
    echo "Error: <slug> must be kebab-case, start with a letter, min 2 chars (e.g. orders, product-catalog)." >&2
    exit 1
fi
[[ -d projects/temperature-api && -d projects/temperature-web ]] || {
    echo "Error: run this from the repository root (projects/temperature-* not found)." >&2; exit 1; }
if [[ -e "$TARGET" && -n "$(ls -A "$TARGET" 2>/dev/null)" ]]; then
    echo "Error: target '$TARGET' already exists and is not empty." >&2; exit 1
fi

NS=$(echo "$SLUG"    | sed -E 's/(^|-)([a-z])/\U\2/g')
TITLE=$(echo "$SLUG" | sed -E 's/(^|-)([a-z])/ \U\2/g' | sed 's/^ //')
mkdir -p "$TARGET"
TARGET_ABS="$(cd "$TARGET" && pwd)"

echo "Ejecting '${SLUG}' (namespace ${NS}) → ${TARGET_ABS}"

# ── Copy templates (excluding heavy build/dependency dirs) ──────────────────────
copy_dir() {   # $1 = source path under repo root, $2 = destination path
    local parent name tmp
    parent="$(dirname "$1")"; name="$(basename "$1")"; tmp="$(mktemp -d)"
    ( cd "$parent" && tar \
        --exclude=node_modules --exclude=dist --exclude=.vite --exclude=coverage \
        --exclude=bin --exclude=obj --exclude=.vs --exclude=TestResults --exclude=.git \
        -cf - "$name" ) | ( cd "$tmp" && tar -xf - )
    mv "$tmp/$name" "$2"
    rmdir "$tmp"
}
copy_dir projects/temperature-api "$TARGET_ABS/api"
copy_dir projects/temperature-web "$TARGET_ABS/web"
copy_dir infra                    "$TARGET_ABS/infra"

# ── Backend: rename namespace/db (keeps default ports — fresh repo, runs alone) ─
( cd "$TARGET_ABS/api" && bash rename.sh "$NS" >/dev/null )
rm -f "$TARGET_ABS/api/rename.sh"

# ── Frontend: rename identity ───────────────────────────────────────────────────
sed -i "s|\"name\": \"temperature-web\"|\"name\": \"${SLUG}-web\"|" "$TARGET_ABS/web/package.json"
sed -i "s|<title>.*</title>|<title>${TITLE}</title>|" "$TARGET_ABS/web/index.html"

# ── Guideline + .gitignore ──────────────────────────────────────────────────────
mkdir -p "$TARGET_ABS/guidelines"
cp guidelines/infra-devops.md "$TARGET_ABS/guidelines/infra-devops.md"
[[ -f .gitignore ]] && cp .gitignore "$TARGET_ABS/.gitignore"

# ── Workflows wired to the flat api/ + web/ layout ──────────────────────────────
mkdir -p "$TARGET_ABS/.github/workflows"
cp .github/workflows/deploy.yml "$TARGET_ABS/.github/workflows/deploy.yml"   # reusable, path-agnostic

cat > "$TARGET_ABS/.github/workflows/ci.yml" <<EOF
name: CI

on:
  push:
    branches: ["**"]
  pull_request:
    branches: [master, develop]

jobs:
  frontend:
    name: "Frontend"
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: web
    steps:
      - uses: actions/checkout@v4
      - uses: pnpm/action-setup@v4
        with:
          version: 11
      - uses: actions/setup-node@v4
        with:
          node-version: 22
          cache: pnpm
          cache-dependency-path: web/pnpm-lock.yaml
      - run: pnpm install --frozen-lockfile
      - run: pnpm lint
      - run: pnpm test:run
      - run: pnpm build

  backend:
    name: "Backend"
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "8.0.x"
      - run: dotnet restore api/${NS}.sln
      - run: dotnet build api/${NS}.sln --no-restore --configuration Release
      - run: dotnet test api/${NS}.sln --no-build --configuration Release --verbosity normal
EOF

cat > "$TARGET_ABS/.github/workflows/deploy-dev.yml" <<EOF
# Auto-deploy to DEV on every merge into develop.
# Activates only when the repository variable ENABLE_DEPLOY == 'true'.
name: Deploy DEV

on:
  push:
    branches: [develop]

jobs:
  ${SLUG}:
    if: vars.ENABLE_DEPLOY == 'true'
    uses: ./.github/workflows/deploy.yml
    with:
      environment: dev
      project: ${SLUG}
      api_path: api
      web_path: web
      api_base_url: https://dev.example.com
    secrets: inherit
EOF

cat > "$TARGET_ABS/.github/workflows/deploy-prod.yml" <<EOF
# Auto-deploy to PROD on every merge into master.
# Activates only when the repository variable ENABLE_DEPLOY == 'true'.
# Give the 'production' Environment Required reviewers for a manual approval gate.
name: Deploy PROD

on:
  push:
    branches: [master]

jobs:
  ${SLUG}:
    if: vars.ENABLE_DEPLOY == 'true'
    uses: ./.github/workflows/deploy.yml
    with:
      environment: production
      project: ${SLUG}
      api_path: api
      web_path: web
      api_base_url: https://example.com
    secrets: inherit
EOF

# ── README ──────────────────────────────────────────────────────────────────--
cat > "$TARGET_ABS/README.md" <<EOF
# ${TITLE}

Bootstrapped from the base-docs-development template.

- \`api/\`  — .NET 8 + Dapper + PostgreSQL (namespace \`${NS}\`)
- \`web/\`  — React + TypeScript + Vite
- \`infra/\` — VPS provisioning & deploy templates
- \`guidelines/infra-devops.md\` — deployment conventions

## Run locally

\`\`\`bash
# Backend
cd api && docker compose up -d && dotnet run --project src/${NS}.Api
# Frontend (another terminal)
cd web && pnpm install && cp .env.example .env.local && pnpm dev
\`\`\`

## Deploy

See \`guidelines/infra-devops.md\`. In short: create \`master\` (default) + \`develop\`,
protect them, set the repository variable \`ENABLE_DEPLOY=true\`, create the \`dev\` and
\`production\` Environments, then provision a VPS with \`infra/provision-vps.sh\` +
\`infra/add-project.sh ${SLUG} <domain> 8080\`.

The example domain (\`TemperatureReading\` / \`temperature-list\`) is kept as a reference —
replace it with your own entities and remove it.
EOF

# ── git init + initial commit ───────────────────────────────────────────────────
git -C "$TARGET_ABS" init -q -b master
git -C "$TARGET_ABS" add -A
if ! git -C "$TARGET_ABS" commit -q -m "chore: bootstrap ${SLUG} from base-docs-development"; then
    echo "⚠️  Files copied, but the initial commit failed — set your git user.name/email and commit manually."
fi

cat <<EOF

✅ Ejected '${SLUG}' → ${TARGET_ABS}

  api/  (namespace ${NS})   web/   infra/   .github/workflows/   guidelines/

Next steps:
  1. cd ${TARGET_ABS}
  2. Create the repo on GitHub, then:  git remote add origin <url> && git push -u origin master
  3. Create 'develop' from master; protect both; set repo variable ENABLE_DEPLOY=true;
     create 'dev' and 'production' Environments (production: Required reviewers).
  4. Run it locally (see README.md). Deploy per guidelines/infra-devops.md.
EOF
