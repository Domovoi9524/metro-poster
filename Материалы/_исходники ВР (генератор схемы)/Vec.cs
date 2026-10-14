using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
public static class Vec {
  public static int[] pal = {0xFFFFFF,0xC0C0C0,0x008080,0x808080,0x00FF80,0x00FFFF,0xFF0000,0xFF00FF,0x0080FF,0x000000,0xFF8040,0x804000,0xFFFF00};
  // индексы: 0 белый,1 серый,2 бирюзово-тёмный(РК),3 тёмно-серый,4 зелёный,5 голубой,6 красный,7 пурпур,8 синий,9 чёрный,10 оранжевый,11 коричневый,12 жёлтый
  public static int Quant(int r,int g,int b){
    int lum=(r*299+g*587+b*114)/1000; int mx=Math.Max(r,Math.Max(g,b)), mn=Math.Min(r,Math.Min(g,b));
    if(mx-mn<40){ // почти серый
      if(lum<70) return 9; if(lum>224) return 0;
      if(lum<150) return 3; return 1;
    }
    int best=0; long bd=long.MaxValue;
    for(int i=0;i<pal.Length;i++){ int pr=(pal[i]>>16)&255, pg=(pal[i]>>8)&255, pb=pal[i]&255; long d=(long)(r-pr)*(r-pr)+(long)(g-pg)*(g-pg)+(long)(b-pb)*(b-pb); if(d<bd){bd=d;best=i;} }
    return best;
  }
  public static string Rects(bool[,] m,int w,int h){
    // жадное объединение: строки-отрезки, одинаковые по x в соседних строках -> один прямоугольник
    var sb=new StringBuilder();
    var open=new Dictionary<long,int[]>(); // key x0*4096+x1 -> [x0,x1,y0]
    for(int y=0;y<=h;y++){
      var cur=new Dictionary<long,int[]>();
      if(y<h){ int x=0; while(x<w){ if(m[x,y]){ int x0=x; while(x<w&&m[x,y]) x++; long k=(long)x0*4096+x; cur[k]=new int[]{x0,x,y}; } else x++; } }
      foreach(var kv in open){ if(cur.ContainsKey(kv.Key)){ cur[kv.Key][2]=kv.Value[2]; } else { var r=kv.Value; sb.Append("M"+r[0]+" "+r[2]+"h"+(r[1]-r[0])+"v"+(y-r[2])+"h-"+(r[1]-r[0])+"z"); } }
      open=cur;
    }
    return sb.ToString();
  }
  public static void Run(string src,string outDir,int cw,int ch){
    Bitmap bmp=new Bitmap(src); int w=cw,h=ch; int[,] q=new int[w,h];
    for(int y=0;y<h;y++) for(int x=0;x<w;x++){ Color c=bmp.GetPixel(x,y); q[x,y]=Quant(c.R,c.G,c.B); }
    File.WriteAllText(outDir+"/q.bin", "");
    // сохраним квантованную карту для дальнейших шагов (ASCII по одному символу)
    var sbq=new StringBuilder(); for(int y=0;y<h;y++){ for(int x=0;x<w;x++) sbq.Append((char)('a'+q[x,y])); sbq.Append('\n'); } File.WriteAllText(outDir+"/qmap.txt",sbq.ToString());
    // компоненты «давленческих» цветов
    int[] pc={2,4,5,6,7,8}; var isP=new bool[13]; foreach(int p in pc) isP[p]=true;
    int[,] comp=new int[w,h]; for(int y=0;y<h;y++) for(int x=0;x<w;x++) comp[x,y]=-1;
    var rep=new StringBuilder(); int id=0; var stack=new Stack<int>();
    for(int y=0;y<h;y++) for(int x=0;x<w;x++){
      if(comp[x,y]!=-1||!isP[q[x,y]]) continue;
      int col=q[x,y]; int cnt=0, minx=x,maxx=x,miny=y,maxy=y; stack.Push(x*10000+y); comp[x,y]=id;
      while(stack.Count>0){ int v=stack.Pop(); int cx=v/10000, cy=v%10000; cnt++; if(cx<minx)minx=cx; if(cx>maxx)maxx=cx; if(cy<miny)miny=cy; if(cy>maxy)maxy=cy;
        int[] dx={1,-1,0,0}, dy={0,0,1,-1};
        for(int k=0;k<4;k++){ int nx=cx+dx[k], ny=cy+dy[k]; if(nx<0||ny<0||nx>=w||ny>=h) continue; if(comp[nx,ny]!=-1||q[nx,ny]!=col) continue; comp[nx,ny]=id; stack.Push(nx*10000+ny); } }
      rep.Append(id+"|"+col+"|"+cnt+"|"+minx+"|"+miny+"|"+maxx+"|"+maxy+"|"+x+"|"+y+"\n"); id++;
    }
    File.WriteAllText(outDir+"/comps.txt",rep.ToString());
    // файлы путей по компонентам (только с площадью >= 12 выводим отдельно; мелкие уходят в статический слой своего цвета)
    var cnts=new Dictionary<int,int>(); for(int y=0;y<h;y++) for(int x=0;x<w;x++){ int c=comp[x,y]; if(c>=0){ int v; cnts.TryGetValue(c,out v); cnts[c]=v+1; } }
    var pathSb=new StringBuilder();
    for(int cid=0;cid<id;cid++){
      if(cnts[cid]<12) continue; var m=new bool[w,h]; for(int y=0;y<h;y++) for(int x=0;x<w;x++) m[x,y]=(comp[x,y]==cid);
      pathSb.Append(cid+"|"+Rects(m,w,h)+"\n");
    }
    File.WriteAllText(outDir+"/comp_paths.txt",pathSb.ToString());
    var big=new HashSet<int>(); foreach(var kv in cnts) if(kv.Value>=12) big.Add(kv.Key);
    // статические слои по цветам (белый не рисуем; давленческие большие компоненты исключены)
    var layers=new StringBuilder();
    for(int col=1;col<13;col++){
      var m=new bool[w,h]; bool any=false;
      for(int y=0;y<h;y++) for(int x=0;x<w;x++){ if(q[x,y]!=col) continue; int c=comp[x,y]; if(c>=0&&big.Contains(c)) continue; m[x,y]=true; any=true; }
      if(any) layers.Append(col+"|"+Rects(m,w,h)+"\n");
    }
    File.WriteAllText(outDir+"/layers.txt",layers.ToString());
  }
}
