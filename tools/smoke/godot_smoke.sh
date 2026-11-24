#!/usr/bin/env bash
# Smoke headless do jogo (AI-SPEC Foundation Fase 4, E2E-GD-02/03).
# Uso: GODOT=~/bin/godot4 tools/smoke/godot_smoke.sh [segundos=60] [argumentos extras do jogo...]
set -euo pipefail
secs="${1:-60}"
if [ "$#" -gt 0 ]; then shift; fi
: "${GODOT:?defina GODOT com o binário do Godot 4.7.2 mono}"
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
dotnet build "$root/Rts.sln" -nologo -clp:ErrorsOnly
set +e
out="$(timeout "$((secs + 60))" "$GODOT" --headless --path "$root/game" -- --smoke-seconds="$secs" "$@" 2>&1)"
code=$?
set -e
line="$(printf '%s\n' "$out" | grep '^SMOKE ' | tail -n 1 || true)"
if [ -z "$line" ]; then
  printf '%s\n' "$out" | tail -n 20
  echo "SMOKE result=NO_OUTPUT"
  [ "$code" -eq 0 ] && code=1
else
  echo "$line"
fi
exit "$code"
