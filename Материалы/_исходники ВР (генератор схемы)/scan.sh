#!/bin/bash
# scan row|col  idx  from  to  [chars]   -> выводит отрезки подряд идущих пикселей с указанными ключами (по kmap) или цветами q2 (по q2map, если chars маленькие буквы)
mode=$1; idx=$2; a=$3; b=$4; chars=$5; D="$TEMP/vr/gen"
if [[ "$chars" =~ ^[A-Z.]+$ ]]; then F="$D/kmap.txt"; else F="$D/q2map.txt"; fi
if [ "$mode" = row ]; then
  awk -v y=$idx -v a=$a -v b=$b -v ch="$chars" 'NR==y+1{ s=-1; for(x=a;x<=b;x++){ c=substr($0,x+1,1); ok=(index(ch,c)>0); if(ok&&s<0)s=x; if(!ok&&s>=0){printf "%d-%d ",s,x-1; s=-1} } if(s>=0)printf "%d-%d",s,b; print ""}' "$F"
else
  awk -v x=$idx -v a=$a -v b=$b -v ch="$chars" 'NR>=a+1&&NR<=b+1{ c=substr($0,x+1,1); ok=(index(ch,c)>0); y=NR-1; if(ok&&s=="")s=y; if(!ok&&s!=""){printf "%d-%d ",s,y-1; s=""} } END{ if(s!="")printf "%d-%d",s,b; print ""}' "$F"
fi
