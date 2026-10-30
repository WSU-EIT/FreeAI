#!/bin/bash
# Usage: gif.sh <out.gif> <secondsPerFrame> <width> frame1.png frame2.png ...
# Frames are padded to a common size, then encoded with a generated palette.
set -e
out=$1; spf=$2; w=$3; shift 3
tmp=$(mktemp -d)
i=0
for f in "$@"; do cp "$f" "$tmp/$(printf '%03d' $i).png"; i=$((i+1)); done
ffmpeg -loglevel error -y -framerate "1/$spf" -i "$tmp/%03d.png" \
  -vf "scale=$w:-2:flags=lanczos,pad=ceil(iw/2)*2:ceil(ih/2)*2:color=white,split[a][b];[a]palettegen=max_colors=192:stats_mode=diff[p];[b][p]paletteuse=dither=bayer:bayer_scale=4" \
  -loop 0 "$out"
rm -rf "$tmp"
