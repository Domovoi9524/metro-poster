#!/bin/bash
# dump x0 y0 x1 y1
x0=$1;y0=$2;x1=$3;y1=$4; W=$((x1-x0+1))
hdr=""; for ((i=0;i<W;i++)); do x=$((x0+i)); if (( x%10==0 )); then hdr+="${x: -2:1}"; else hdr+=" "; fi; done
hdr2=""; for ((i=0;i<W;i++)); do x=$((x0+i)); hdr2+="${x: -1}"; done
echo "    $hdr"; echo "    $hdr2"
awk -v y0=$y0 -v y1=$y1 -v x0=$x0 -v w=$W 'NR>=y0+1 && NR<=y1+1 {printf "%3d %s\n", NR-1, substr($0,x0+1,w)}' "$TEMP/vr/gen/qmap.txt"
