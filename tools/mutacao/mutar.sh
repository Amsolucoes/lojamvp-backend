#!/bin/bash
# Teste de mutação manual: planta UM bug de propósito num arquivo, roda os testes e confere se algum FALHA.
# Se os testes continuam passando, eles NÃO protegem aquela regra. O arquivo é restaurado no fim, sempre.
#
# uso: tools/mutacao/mutar.sh <arquivo.cs> '<expressão sed>' '<filtro dotnet test>' '<descrição do bug>'
#   ex: tools/mutacao/mutar.sh src/Domain/Services/Chacara/RegrasAgenda.cs \
#         's/(r.ExpiraEm == null || r.ExpiraEm > agora)/true/' 'FullyQualifiedName~Chacara' 'pendente vencida ocupa a agenda'
# Saída: "DETECTADO" (algum teste falhou = bom) ou "NÃO DETECTADO" (os testes não pegaram o bug).
# Precisa de LOJA_TESTES_DB (veja tests/README.md) para rodar os testes de integração.
set -u
RAIZ="$(cd "$(dirname "$0")/../.." && pwd)"
ARQ="$RAIZ/$1"; EXPR="$2"; FILTRO="$3"; DESC="$4"
[ -f "$ARQ" ] || { echo "arquivo não existe: $1"; exit 2; }
BKP="$(mktemp)"; cp "$ARQ" "$BKP"
restaurar() { cp "$BKP" "$ARQ"; rm -f "$BKP"; }
trap restaurar EXIT

sed -i "$EXPR" "$ARQ"
if cmp -s "$ARQ" "$BKP"; then echo "!! a mutação não alterou o arquivo (expressão não casou): $DESC"; exit 2; fi

SAIDA="$(cd "$RAIZ/tests/LojaApi.Tests" && dotnet test --nologo -v q --filter "$FILTRO" 2>&1)"
if echo "$SAIDA" | grep -q "error CS"; then echo "!! a mutação não compila: $DESC"; echo "$SAIDA" | grep "error CS" | head -2; exit 2; fi
RESUMO="$(echo "$SAIDA" | grep -E "Passed!|Failed!" | head -1)"
if echo "$RESUMO" | grep -q "Failed!"; then echo "DETECTADO     | $DESC | $RESUMO"; else echo "NÃO DETECTADO | $DESC | ${RESUMO:-sem resumo (nenhum teste rodou?)}"; fi
