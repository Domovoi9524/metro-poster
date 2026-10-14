#!/bin/bash
# Сборка страницы воздухораспределителя из точной векторной копии схемы
D="$TEMP/vr/gen"; SRC="$TEMP/vr/backup_before_pixel.html"; OUT="/c/Users/egorb/Documents/Пособие по тормозному оборудованию/Воздухораспределитель 337.004.html"
# --- пути пузырьков (FL)
fl() { # группа, имена путей
  g=$1; shift; echo -n " $g:["; first=1
  for n in "$@"; do d=$(grep "^$n|" "$D/out_paths.txt" | head -1 | cut -d'|' -f2); [ $first = 1 ] || echo -n ","; echo -n "\"$d\""; first=0; done; echo "],"
}
{
fl tmpath tmpath; fl mkb mkb; fl v2vent v2vent; fl hole holeA holeB; fl kdr kdr; fl nmfeed nmfeed; fl tcfeed tcfeed; fl tcvent tcvent1 tcvent2
fl p1fill p1fill; fl p1vent p1vent; fl okvent okvent; fl klvent klvent; fl arfeed arfeed; echo -n " arvent:[\"$(grep '^arvent|' "$D/out_paths.txt" | cut -d'|' -f2)\"]"; echo
} > "$D/fl.txt"
awk -v flfile="$(cygpath -m "$D/fl.txt")" '/%%FL%%/{while((getline line < flfile)>0) print line; next} {print}' "$D/js_anim.txt" > "$D/js_anim_built.txt"
# --- части: data-part
sed -e 's|<g id="v1s"|<g id="v1s" class="part" data-part="vz1"|; s|<g id="v2s"|<g id="v2s" class="part" data-part="vz2"|; s|<g id="ptube"|<g id="ptube" class="part" data-part="feedv"|; s|<g id="mdia"|<g id="mdia" class="part" data-part="mdia"|; s|<g id="mdiaB"|<g id="mdiaB" class="part" data-part="mdia"|; s|<g id="cup"|<g id="cup" class="part" data-part="cup"|; s|<g id="rodU"|<g id="rodU" class="part" data-part="mrod"|; s|<g id="sleeve"|<g id="sleeve" class="part" data-part="sp"|; s|<g id="sp"|<g id="sp" class="part" data-part="sp"|; s|<g id="md"|<g id="md" class="part" data-part="md"|; s|<g id="klP"|<g id="klP" class="part" data-part="klsd"|; s|<g id="astem"|<g id="astem" class="part" data-part="astem"|; s|<g id="cylP"|<g id="cylP" class="part" data-part="cyl"|' "$D/out_parts.txt" > "$D/out_parts_dp.txt"
{
  cat "$D/svg_head.txt"
  cat "$D/out_base.txt"
  echo '<g id="regs">'; cat "$D/out_reg.txt"; echo '</g>'
  cat "$D/out_parts_dp.txt"
  cat "$D/svg_tail.txt"
} > "$D/svg_new.txt"
S1=$(grep -n '<svg id="scheme"' "$SRC" | cut -d: -f1); S2=$(grep -n '^          </svg>' "$SRC" | cut -d: -f1)
J1=$(grep -n '/\* ---------- цвета давлений' "$SRC" | cut -d: -f1); J2=$(grep -n '/\* ---------- описания' "$SRC" | cut -d: -f1)
echo "svg $S1-$S2 js $J1-$J2"
{
  sed -n "1,$((S1-1))p" "$SRC"
  cat "$D/svg_new.txt"
  sed -n "$((S2+1)),$((J1-1))p" "$SRC"
  cat "$D/js_anim_built.txt"
  sed -n "$J2,\$p" "$SRC"
} > "$D/page_new.html"
sed -i 's|aspect-ratio:880/650|aspect-ratio:880/590|; s|\*880/650)|*880/590)|g; s|data-fsratio="1.692"|data-fsratio="1.725"|' "$D/page_new.html"
sed -i 's|  D.u=5\*(1-D.atmO)+5\*D.feedO; .*|  D.upx=2*(1-D.atmO)+4*D.feedO;                               /* подъём режимной диафрагмы, px: 0 — атмосферный клапан открыт, 2 — перекрыша, 6 — питательный клапан открыт */|; s|  if(D.Ttar<0.005\&\&S.Ptc>0.002) D.atmO=1; .*|  if(D.Ttar<0.005) D.atmO=1;                                  /* нет торможения: диафрагма внизу, атмосферный клапан открыт */|; s|cur.u=D.u;|cur.u=D.upx;|' "$D/page_new.html"
grep -c 'D.upx' "$D/page_new.html"
cp "$D/page_new.html" "$OUT"
