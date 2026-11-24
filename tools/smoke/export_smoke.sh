#!/usr/bin/env bash
# Exporta o jogo (Linux x86_64) e roda o smoke no executável exportado (AI-SPEC Foundation Fase 4, E2E-GD-05; ADR-009).
# Uso: GODOT=~/bin/godot4 tools/smoke/export_smoke.sh [segundos=10]
set -euo pipefail
secs="${1:-10}"
: "${GODOT:?defina GODOT com o binário do Godot 4.7.2 mono}"
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
tpl="$HOME/.local/share/godot/export_templates/4.7.2.stable.mono"
for f in linux_debug.x86_64 linux_release.x86_64; do
  if [ ! -f "$tpl/$f" ]; then
    echo "SMOKE result=NO_TEMPLATES path=$tpl/$f"
    exit 4
  fi
done
out_dir="$root/build/linux"
rm -rf "$out_dir"
mkdir -p "$out_dir"
dotnet build "$root/Rts.sln" -nologo -clp:ErrorsOnly
if ! timeout 900 "$GODOT" --headless --path "$root/game" --export-release "Linux" "$out_dir/Rts.x86_64"; then
  echo "SMOKE result=NO_EXPORT"
  exit 5
fi
if [ ! -f "$out_dir/Rts.x86_64" ] || [ ! -f "$out_dir/Rts.pck" ]; then
  echo "SMOKE result=NO_EXPORT"
  exit 5
fi
run_dir="$(mktemp -d)"
cp -a "$out_dir/." "$run_dir/"
if [ -e "$run_dir/data" ]; then
  echo "SMOKE result=FAIL reason=data_dir"
  exit 1
fi
set +e
out="$(cd "$run_dir" && timeout "$((secs + 60))" ./Rts.x86_64 --headless -- --smoke-seconds="$secs" 2>&1)"
code=$?
set -e
line="$(printf '%s\n' "$out" | grep '^SMOKE ' | tail -n 1 || true)"
if [ -z "$line" ]; then
  printf '%s\n' "$out" | tail -n 20
  echo "SMOKE result=NO_OUTPUT"
  [ "$code" -eq 0 ] && code=1
  exit "$code"
fi
echo "$line"
if [ "$code" -ne 0 ]; then exit "$code"; fi
case "$line" in *" source=res"*) ;; *) echo "SMOKE result=FAIL reason=source"; exit 1 ;; esac
case "$line" in *" exported=1"*) ;; *) echo "SMOKE result=FAIL reason=exported"; exit 1 ;; esac
exit 0
