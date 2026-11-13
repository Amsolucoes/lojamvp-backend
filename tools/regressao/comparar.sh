#!/bin/bash
# Sobe a API num banco novo, roda o cenário (cenario.py) e grava resultado-<rotulo>.json.
# uso: tools/regressao/comparar.sh <pasta do build> <rotulo> [grupos]
#   ex: tools/regressao/comparar.sh bin/Release/net10.0 novo
# Precisa de um PostgreSQL local (usuário postgres) e do comando "psql".
# Informe a senha do postgres em LOJA_PG_SENHA (ex.: export LOJA_PG_SENHA=...).
# Para comparar uma refatoração: rode com o build ANTES (rotulo "base") e DEPOIS (rotulo "novo"),
# depois: tools/regressao/diff.py
set -e
PASTA="$(cd "$1" && pwd)"; ROTULO="$2"; GRUPOS="$3"
AQUI="$(cd "$(dirname "$0")" && pwd)"
DB="loja_$ROTULO"; PORTA=5110
pkill -f "LojaApi.dll" 2>/dev/null || true; sleep 1
su postgres -c "psql -qc 'DROP DATABASE IF EXISTS $DB;' -c 'CREATE DATABASE $DB;'" >/dev/null
( cd "$PASTA" && ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:$PORTA \
  ConnectionStrings__Default="Host=localhost;Port=5432;Database=$DB;Username=postgres;Password=${LOJA_PG_SENHA:?defina LOJA_PG_SENHA}" \
  nohup dotnet LojaApi.dll > "$AQUI/api-$ROTULO.log" 2>&1 & )
for i in $(seq 1 60); do sleep 2; c=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:$PORTA/api/publico/x/chacara/dados); [ "$c" != "000" ] && break; done
python3 "$AQUI/cenario.py" http://localhost:$PORTA "$DB" "$AQUI/resultado-$ROTULO.json" $GRUPOS
pkill -f "LojaApi.dll" || true
