#!/bin/sh
# Builds the developer wiki (ADR 0008): generates the reference pages from Core and content, then runs DocFX.
# The site lands in tools/wiki/_site. --serve serves it at http://localhost:8080; --strict fails on missing strings or docs.
set -e
cd "$(git rev-parse --show-toplevel)"

serve=""
strict=""
for arg in "$@"; do
  case "$arg" in
    --serve) serve="--serve" ;;
    --strict) strict="--strict" ;;
    *) echo "Usage: tools/wiki/build.sh [--serve] [--strict]" >&2; exit 2 ;;
  esac
done

dotnet build tools/__GAME__.Wiki/__GAME__.Wiki.csproj -c Debug --nologo -v quiet
dotnet run --no-build --project tools/__GAME__.Wiki -- --commit "$(git rev-parse --short HEAD 2>/dev/null || echo local)" $strict
dotnet docfx tools/wiki/docfx.json --warningsAsErrors $serve
