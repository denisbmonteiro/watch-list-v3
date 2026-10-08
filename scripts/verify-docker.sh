#!/usr/bin/env bash
# Verificação antes de publicar: roda os testes e sobe a imagem de produção no Docker.
# Uso: scripts/verify-docker.sh [porta]   (padrão 8089)
set -euo pipefail

cd "$(dirname "$0")/.."

PORT="${1:-8089}"
IMAGE="watchlist:verify"
CONTAINER="watchlist-verify"
SDK_IMAGE="mcr.microsoft.com/dotnet/sdk:10.0"

step() { printf '\n==> %s\n' "$*"; }
fail() { printf 'FALHOU: %s\n' "$*" >&2; exit 1; }

cleanup() { docker rm -f "$CONTAINER" >/dev/null 2>&1 || true; }
trap cleanup EXIT

# 1. Testes no SDK do Docker (mesma imagem do build de produção). O código entra só leitura e é
#    copiado dentro do container, sem bin/obj, para não sujar a pasta local com arquivos de root.
step "dotnet test no $SDK_IMAGE"
docker run --rm -v "$PWD":/src:ro "$SDK_IMAGE" sh -c '
    set -e
    mkdir /work
    tar -C /src --exclude=.git --exclude=bin --exclude=obj --exclude=TestResults -cf - . | tar -C /work -xf -
    cd /work
    dotnet test WatchList.slnx
'

# 2. Imagem de produção, exatamente como o Dockerfile publica.
step "docker build"
docker build -t "$IMAGE" .

# 3. Container rodando e respondendo.
step "docker run na porta $PORT"
cleanup
docker run -d --name "$CONTAINER" -p "$PORT:8080" "$IMAGE" >/dev/null

BASE="http://localhost:$PORT"
for _ in $(seq 1 30); do
    curl -fs -o /dev/null "$BASE/" && break
    sleep 1
done
curl -fs -o /dev/null "$BASE/" || { docker logs "$CONTAINER"; fail "a aplicação não respondeu em $BASE"; }

# Cada página precisa responder 200 e renderizar o título, não a tela de erro.
check_page() {
    local path="$1" heading="$2" body
    body="$(curl -fsS "$BASE$path")" || fail "GET $path não retornou 2xx"
    grep -q "<h1>$heading</h1>" <<<"$body" || fail "GET $path não renderizou <h1>$heading</h1>"
    printf '  ok  %-12s %s\n' "$path" "$heading"
}

step "páginas"
check_page /           "In Progress"
check_page /anime      "Anime"
check_page /movie      "Movies"
check_page /serie      "Series"
check_page /book       "Books"
check_page /game       "Games"
check_page /manga      "Manga"
check_page /queue      "Queue"
check_page /dashboard  "Dashboard"

# Arquivos estáticos que já quebraram no Docker (PR #2) e uma capa servida de verdade.
check_static() {
    local path="$1"
    curl -fs -o /dev/null "$BASE$path" || fail "GET $path não retornou 2xx"
    printf '  ok  %s\n' "$path"
}

step "arquivos estáticos"
check_static /_framework/blazor.web.js
check_static /app.css
check_static /favicon.ico
cover="$(curl -fsS "$BASE/movie" | grep -o 'images/movie/[^"]*\.jpg' | head -n1)"
[[ -n "$cover" ]] || fail "nenhuma capa encontrada em /movie"
check_static "/$cover"

# Os .txt de dados ficam fora do wwwroot: não podem ser servidos por nenhum caminho.
check_private() {
    local path="$1" status
    status="$(curl -s -o /dev/null -w '%{http_code}' "$BASE$path")"
    [[ "$status" == 404 ]] || fail "GET $path retornou $status; os dados não podem ser públicos"
    printf '  ok  %-24s 404\n' "$path"
}

step "dados privados"
check_private /AppData/movies.txt
check_private /movies.txt

# Nenhuma exceção no log durante as requisições acima, nem .txt ausente ou linha descartada
# (esses dois são só warning e deixariam as páginas no ar com a lista vazia ou incompleta).
step "logs"
if docker logs "$CONTAINER" 2>&1 | grep -E '^(fail|crit):|Unhandled exception|not found; the list is empty|Skipping line'; then
    fail "o container registrou erros (acima)"
fi
echo "  ok  sem fail/crit, arquivo ausente ou linha descartada no log"

step "tudo certo"
