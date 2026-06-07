#!/usr/bin/env bash
# Renomeia o projeto template para um novo namespace.
# Uso: ./rename.sh <NomeDoProjeto>
# Exemplo: ./rename.sh OrdersApi
set -euo pipefail

OLD_NS="TemperatureApi"
OLD_DB="temperature_db"
OLD_VOL="postgres_data"

NEW_NS="${1:-}"
if [[ -z "$NEW_NS" ]]; then
    echo "Uso: ./rename.sh <NomeDoProjeto>" >&2
    echo "Exemplo: ./rename.sh OrdersApi" >&2
    exit 1
fi
if [[ ! "$NEW_NS" =~ ^[A-Z][A-Za-z0-9]+$ ]]; then
    echo "Erro: o nome deve ser PascalCase sem espaços ou símbolos (ex: OrdersApi, ProductCatalog)." >&2
    exit 1
fi

# PascalCase → snake_case para nome do banco e do volume Docker
NEW_DB=$(echo "$NEW_NS" | sed 's/\([A-Z]\)/_\1/g' | sed 's/^_//' | tr '[:upper:]' '[:lower:]')
NEW_VOL="${NEW_DB}_postgres_data"

echo "Renomeando projeto..."
echo "  Namespace : $OLD_NS → $NEW_NS"
echo "  Banco     : $OLD_DB → ${NEW_DB}_db"
echo "  Volume    : $OLD_VOL → $NEW_VOL"
echo ""

# ── 1. Substituir conteúdo dos arquivos ────────────────────────────────────
find . -type f \( \
    -name "*.cs"    -o -name "*.csproj" -o -name "*.sln" \
    -o -name "*.json" -o -name "*.yml"  -o -name "*.yaml" \
    -o -name "*.sql" -o -name "*.md" \
\) ! -path "*/bin/*" ! -path "*/obj/*" -print0 \
| while IFS= read -r -d '' file; do
    sed -i \
        -e "s/${OLD_NS}/${NEW_NS}/g" \
        -e "s/${OLD_DB}/${NEW_DB}_db/g" \
        -e "s/${OLD_VOL}/${NEW_VOL}/g" \
        "$file"
done

# ── 2. Renomear arquivos e pastas (profundidade decrescente, só o basename) ─
# Renomear o basename de cada item evita tentar mover para um diretório pai
# que ainda não foi renomeado.
find . -depth \( -name "*${OLD_NS}*" \) ! -path "*/bin/*" ! -path "*/obj/*" -print0 \
| while IFS= read -r -d '' path; do
    dir=$(dirname "$path")
    base=$(basename "$path")
    new_base="${base//${OLD_NS}/${NEW_NS}}"
    [ "$base" != "$new_base" ] && mv "$path" "$dir/$new_base"
done

echo "✅ Projeto renomeado com sucesso!"
echo ""
echo "Próximos passos:"
echo "  1. git rm rename.sh && git add -A && git commit -m 'chore: bootstrap ${NEW_NS}'"
echo "  2. docker compose up -d"
echo "  3. dotnet restore ${NEW_NS}.sln"
echo "  4. dotnet run --project src/${NEW_NS}.Api"
echo ""
echo "A entidade de exemplo (TemperatureReading) serve como referência."
echo "Adicione suas entidades seguindo o mesmo padrão e remova-a quando não precisar mais."
