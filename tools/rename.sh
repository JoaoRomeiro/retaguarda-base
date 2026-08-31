#!/usr/bin/env bash
# =============================================================================
# rename.sh — transforma o Retaguarda Base num projeto novo.
#
# Renomeia namespaces, pastas, .csproj, a solution, o banco, os containers, a
# rede, os volumes, o nome do projeto Compose e o e-mail do admin de dev; gera
# UserSecretsId novos; e (opcionalmente) desloca as portas e troca o rótulo
# "Planta" nas telas.
#
# Uso (a partir da raiz do repositório):
#   ./tools/rename.sh --name MeuProjeto --dry-run     # simula, não escreve nada
#   ./tools/rename.sh --name MeuProjeto               # aplica
#
# Opções:
#   --name <PascalCase>        Obrigatório. Vira o namespace raiz (MeuProjeto.Web, …)
#                              e o nome do banco. Só letras e números, começando
#                              com maiúscula.
#   --site-label <texto>       Troca o rótulo "Planta" no repositório inteiro (telas,
#                              comentários, docs) — ex.: Filial, Loja, Obra. A ENTIDADE
#                              continua sendo Site, no código e no banco.
#   --site-label-plural <txt>  Plural do rótulo. Padrão: <label>s.
#   --port-offset <n>          Soma n a todas as portas (banco, dev e Docker), para
#                              o projeto novo rodar ao lado da base.
#   --dry-run                  Mostra o que mudaria e sai.
#
# Depois de rodar, siga os "próximos passos" impressos no fim.
# =============================================================================
set -euo pipefail

NEW_NAME=""
SITE_LABEL=""
SITE_LABEL_PLURAL=""
PORT_OFFSET=0
DRY_RUN=0

die() { printf '\nERRO: %s\n\n' "$1" >&2; exit 1; }

while [[ $# -gt 0 ]]; do
    case "$1" in
        --name)              NEW_NAME="${2:-}"; shift 2 ;;
        --site-label)        SITE_LABEL="${2:-}"; shift 2 ;;
        --site-label-plural) SITE_LABEL_PLURAL="${2:-}"; SITE_LABEL_PLURAL_EXPLICIT=1; shift 2 ;;
        --port-offset)       PORT_OFFSET="${2:-}"; shift 2 ;;
        --dry-run)           DRY_RUN=1; shift ;;
        -h|--help)           sed -n '2,30p' "$0"; exit 0 ;;
        *) die "opção desconhecida: $1 (use --help)" ;;
    esac
done

[[ -n "$NEW_NAME" ]] || die "informe --name <PascalCase>. Use --help para ver as opções."
[[ "$NEW_NAME" =~ ^[A-Z][A-Za-z0-9]+$ ]] || die "--name deve ser PascalCase, só letras e números (ex.: MeuProjeto). Recebido: '$NEW_NAME'"
[[ "$PORT_OFFSET" =~ ^-?[0-9]+$ ]] || die "--port-offset deve ser um número inteiro."

# Raiz do repositório: a pasta acima de tools/.
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

[[ -f "Retaguarda.sln" ]] || die "Retaguarda.sln não encontrado em $ROOT. Este script só roda uma vez, na base recém-clonada."

SLUG="$(printf '%s' "$NEW_NAME" | tr '[:upper:]' '[:lower:]')"
[[ -n "$SITE_LABEL" && -z "$SITE_LABEL_PLURAL" ]] && SITE_LABEL_PLURAL="${SITE_LABEL}s"

# Portas da base, na ordem em que aparecem no projeto.
PORTS=(15433 7202 5154 7286 5286 8090 8091)

echo "=============================================================="
echo " Retaguarda Base  ->  $NEW_NAME"
echo "=============================================================="
echo "  namespace raiz .......... Retaguarda.*  ->  $NEW_NAME.*"
echo "  banco ................... RetaguardaBase  ->  $NEW_NAME"
echo "  containers/rede/volumes . retaguarda-base-*  ->  $SLUG-*"
echo "  projeto Compose ......... retaguarda-base  ->  $SLUG"
echo "  admin de dev ............ admin@retaguarda.local  ->  admin@$SLUG.local"
if [[ "$PORT_OFFSET" -ne 0 ]]; then
    printf '  portas .................. '
    for p in "${PORTS[@]}"; do printf '%s->%s ' "$p" "$((p + PORT_OFFSET))"; done; echo
else
    echo "  portas .................. inalteradas (${PORTS[*]})"
fi
if [[ -n "$SITE_LABEL" ]]; then
    echo "  rótulo da planta ........ Planta/Plantas  ->  $SITE_LABEL/$SITE_LABEL_PLURAL"
    [[ -z "${SITE_LABEL_PLURAL_EXPLICIT:-}" ]] && echo "                            (plural derivado com 's'; use --site-label-plural se estiver errado)"
fi
[[ "$DRY_RUN" -eq 1 ]] && echo "  MODO ....................  --dry-run (nada será escrito)"
echo

# --- Arquivos de texto a processar -------------------------------------------
# Fora: .git, artefatos de build e as libs de terceiros (wwwroot/lib), que não
# contêm o nome do projeto e são grandes.
mapfile -t FILES < <(find . \
    \( -path ./.git -o -name bin -o -name obj -o -path ./src/Retaguarda.Web/wwwroot/lib \) -prune -o \
    -type f -print | LC_ALL=C sort)

is_text() {
    case "${1,,}" in
        *.woff2|*.woff|*.ttf|*.eot|*.ico|*.png|*.jpg|*.jpeg|*.gif|*.svg|*.pdf|*.xlsx|*.zip|*.dll|*.exe) return 1 ;;
    esac
    return 0
}

changed_files=0
SELF="./tools/$(basename "${BASH_SOURCE[0]}")"

for f in "${FILES[@]}"; do
    # O próprio script contém os nomes que ele substitui: reescrevê-lo enquanto o
    # bash ainda o está lendo corrompe a execução. Ele é descartável de qualquer forma.
    [[ "$f" == "$SELF" ]] && continue
    is_text "$f" || continue
    grep -qI . "$f" 2>/dev/null || continue          # pula binários residuais

    args=(
        -e "s/RetaguardaBase/$NEW_NAME/g"
        -e "s/retaguarda-base/$SLUG/g"
        -e "s/Retaguarda/$NEW_NAME/g"
        -e "s/retaguarda/$SLUG/g"
    )
    if [[ "$PORT_OFFSET" -ne 0 ]]; then
        for p in "${PORTS[@]}"; do
            args+=(-e "s/\\b$p\\b/$((p + PORT_OFFSET))/g")
        done
    fi
    # O rótulo troca no repositório inteiro (telas, comentários, docs): "planta" só
    # aparece como palavra isolada aqui — não há falso positivo. A ENTIDADE continua Site.
    if [[ -n "$SITE_LABEL" ]]; then
        args+=(
            -e "s/PLANTAS/${SITE_LABEL_PLURAL^^}/g"
            -e "s/Plantas/$SITE_LABEL_PLURAL/g"
            -e "s/plantas/${SITE_LABEL_PLURAL,,}/g"
            -e "s/PLANTA/${SITE_LABEL^^}/g"
            -e "s/Planta/$SITE_LABEL/g"
            -e "s/planta/${SITE_LABEL,,}/g"
        )
    fi

    # sed -E preserva os bytes do arquivo (inclusive a quebra de linha final).
    before_sum="$(cksum < "$f")"
    if [[ "$DRY_RUN" -eq 1 ]]; then
        after_sum="$(sed -E "${args[@]}" "$f" | cksum)"
        [[ "$before_sum" != "$after_sum" ]] && { changed_files=$((changed_files + 1)); echo "  conteúdo: $f"; }
    else
        sed -E -i "${args[@]}" "$f"
        [[ "$before_sum" != "$(cksum < "$f")" ]] && changed_files=$((changed_files + 1))
    fi
done

# --- Renomear pastas e arquivos ----------------------------------------------
# Mais profundos primeiro, para o caminho do pai ainda existir quando o filho é movido.
mapfile -t PATHS < <(find . -path ./.git -prune -o -name '*Retaguarda*' -print | awk '{print gsub("/","/") "\t" $0}' | sort -rn | cut -f2-)
renamed=0
for p in "${PATHS[@]}"; do
    base="$(basename "$p")"
    dir="$(dirname "$p")"
    newbase="${base//RetaguardaBase/$NEW_NAME}"
    newbase="${newbase//Retaguarda/$NEW_NAME}"
    [[ "$newbase" == "$base" ]] && continue
    renamed=$((renamed + 1))
    if [[ "$DRY_RUN" -eq 1 ]]; then
        echo "  renomear: $p  ->  $dir/$newbase"
    else
        mv "$p" "$dir/$newbase"
    fi
done

# --- UserSecretsId novos ------------------------------------------------------
newguid() { command -v uuidgen >/dev/null && uuidgen || python3 -c 'import uuid;print(uuid.uuid4())'; }
for proj in Web Api; do
    csproj="src/$NEW_NAME.$proj/$NEW_NAME.$proj.csproj"
    [[ "$DRY_RUN" -eq 1 ]] && { echo "  UserSecretsId novo: src/$NEW_NAME.$proj/$NEW_NAME.$proj.csproj"; continue; }
    [[ -f "$csproj" ]] || continue
    sed -i -E "s|<UserSecretsId>[^<]*</UserSecretsId>|<UserSecretsId>$(newguid)</UserSecretsId>|" "$csproj"
done

echo
if [[ "$DRY_RUN" -eq 1 ]]; then
    echo "--dry-run: $changed_files arquivo(s) teriam o conteúdo alterado, $renamed caminho(s) renomeado(s)."
    echo "Rode de novo sem --dry-run para aplicar."
    exit 0
fi

echo "Pronto: $changed_files arquivo(s) alterado(s), $renamed caminho(s) renomeado(s)."
echo
echo "Próximos passos:"
echo "  1. Apague este script (ele só serve uma vez):  rm -r tools/"
echo "  2. Segredos de dev (troque a porta se usou --port-offset):"
echo "       dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \\"
echo "         \"Host=localhost;Port=$((15433 + PORT_OFFSET));Database=$NEW_NAME;Username=saci;Password=H4ck3r@978\" \\"
echo "         --project src/$NEW_NAME.Web"
echo "       (repita para src/$NEW_NAME.Api)"
echo "       dotnet user-secrets set \"Jwt:SigningKey\" \"\$(openssl rand -base64 48)\" --project src/$NEW_NAME.Api"
echo "  3. Banco e migration:"
echo "       docker compose -f docker/docker-compose.yml up -d postgres"
echo "       dotnet ef database update --project src/$NEW_NAME.Data --startup-project src/$NEW_NAME.Web"
echo "  4. Gate:  dotnet build $NEW_NAME.sln -c Release && dotnet test $NEW_NAME.sln -c Release"
echo "  5. Crie o docs/dominio-$SLUG.md com o glossário do seu domínio (regra 4 do CLAUDE.md)."
[[ -n "$SITE_LABEL" ]] && echo "  6. O rótulo trocou nas telas, nos comentários e nos docs. A ENTIDADE continua Site (código e banco)."
echo
