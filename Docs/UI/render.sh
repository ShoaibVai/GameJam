#!/usr/bin/env bash
# Renders the SVG art set into Unity-ready PNG sprites.
# Sources:  Docs/UI/svg/*.svg
# Output:   Assets/Resources/UI/<name>.png (2x supersampled, except vignette at 1x)
# Usage:    bash Docs/UI/render.sh
set -euo pipefail
cd "$(dirname "$0")/../.."

SRC=Docs/UI/svg
OUT=Assets/Resources/UI
mkdir -p "$OUT"

for svg in "$SRC"/*.svg; do
  name="$(basename "$svg" .svg)"
  scale=2
  if [ "$name" = "vignette" ]; then
    scale=1
  fi
  w=$(grep -o 'width="[0-9]*"' "$svg" | head -1 | grep -o '[0-9]*')
  h=$(grep -o 'height="[0-9]*"' "$svg" | head -1 | grep -o '[0-9]*')
  rsvg-convert -w $((w * scale)) -h $((h * scale)) "$svg" -o "$OUT/$name.png"
  echo "rendered $name -> $OUT/$name.png ($((w * scale))x$((h * scale)))"
done

echo "done: $(ls "$OUT"/*.png | wc -l) textures"
