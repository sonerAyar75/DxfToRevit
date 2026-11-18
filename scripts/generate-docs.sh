#!/usr/bin/env bash
# Generates the DxfToRevit documentation with Doxygen.
# Output: doxygen_out/html/index.html, warnings: doxygen_out/doxygen_warnings.log
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

find_doxygen() {
  command -v doxygen 2>/dev/null && return 0
  for c in /usr/bin/doxygen /usr/local/bin/doxygen /opt/homebrew/bin/doxygen \
           "/c/Program Files/doxygen/bin/doxygen.exe"; do
    [[ -x "$c" ]] && { echo "$c"; return 0; }
  done
  return 1
}

if ! DOXYGEN_BIN="$(find_doxygen)"; then
  echo "error: doxygen was not found. Install it from https://www.doxygen.nl or add it to PATH." >&2
  exit 1
fi

mkdir -p doxygen_out
echo "Using: $DOXYGEN_BIN"
"$DOXYGEN_BIN" Doxyfile

LOG="doxygen_out/doxygen_warnings.log"
WARNINGS=0
[[ -f "$LOG" ]] && WARNINGS="$(grep -c . "$LOG" || true)"
echo "Documentation: $ROOT/doxygen_out/html/index.html"
echo "Warnings: $WARNINGS (see $LOG)"
