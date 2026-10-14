#!/bin/bash
# Сборка: подставляет заново сгенерированную SVG-схему в текущую страницу (модель и сценарии не трогаются)
D="$TEMP/vr/gen"; PAGE="/c/Users/egorb/Documents/Пособие по тормозному оборудованию/Воздухораспределитель 337.004.html"; OUT="${1:-$PAGE}"
sed -e 's|<g id="v1s"|<g id="v1s" class="part" data-part="vz1"|; s|<g id="v2s"|<g id="v2s" class="part" data-part="vz2"|; s|<g id="ptube"|<g id="ptube" class="part" data-part="feedv"|; s|<g id="mdia"|<g id="mdia" class="part" data-part="mdia"|; s|<g id="mdiaB"|<g id="mdiaB" class="part" data-part="mdia"|; s|<g id="cup"|<g id="cup" class="part" data-part="cup"|; s|<g id="rodU"|<g id="rodU" class="part" data-part="mrod"|; s|<g id="sleeve"|<g id="sleeve" class="part" data-part="sp"|; s|<g id="sp"|<g id="sp" class="part" data-part="sp"|; s|<g id="md"|<g id="md" class="part" data-part="md"|; s|<g id="klP"|<g id="klP" class="part" data-part="klsd"|; s|<g id="astem"|<g id="astem" class="part" data-part="astem"|; s|<g id="cylP"|<g id="cylP" class="part" data-part="cyl"|' "$D/out_parts.txt" > "$D/out_parts_dp.txt"
{ cat "$D/svg_head.txt"; cat "$D/out_base.txt"; echo '<g id="regs">'; cat "$D/out_reg.txt"; echo '</g>'; cat "$D/out_parts_dp.txt"; cat "$D/svg_tail.txt"; } > "$D/svg_new.txt"
S1=$(grep -n '<svg id="scheme"' "$PAGE" | cut -d: -f1); S2=$(grep -n '^          </svg>' "$PAGE" | cut -d: -f1)
echo "svg $S1-$S2"
{ sed -n "1,$((S1-1))p" "$PAGE"; cat "$D/svg_new.txt"; sed -n "$((S2+1)),\$p" "$PAGE"; } > "$D/page_tmp.html"
cp "$D/page_tmp.html" "$OUT"
