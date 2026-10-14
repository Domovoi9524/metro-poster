using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
// Замена длинных горизонтальных/вертикальных линий одного класса идеальными прямоугольниками.
public static class Snap {
  static string F(double v){ return Math.Round(v,2).ToString("0.##",CultureInfo.InvariantCulture); }
  // возвращает path-данные из прямоугольников; обнуляет соответствующие пиксели в cls
  public static string Run(byte[] cls,int W,int H,byte c,int minRun,int maxThick,int minLen){
    var sb=new StringBuilder(); int cnt=0;
    var erase=new List<int>();
    for(int pass=0;pass<2;pass++){
      bool vert=pass==1; int A=vert?H:W, B=vert?W:H; // A — вдоль линии, B — поперёк
      Func<int,int,int> idx=(a,b)=> vert? a*W+b : b*W+a;   // (вдоль, поперёк) -> индекс
      var lines=new List<List<int[]>>(); var active=new List<List<int[]>>();
      for(int b=0;b<B;b++){
        var runs=new List<int[]>(); int a=0; while(a<A){ if(cls[idx(a,b)]==c){ int s=a; while(a<A&&cls[idx(a,b)]==c) a++; if(a-s>=minRun) runs.Add(new int[]{b,s,a-1}); } else a++; }
        var next=new List<List<int[]>>();
        foreach(var r in runs){ List<int[]> best=null; double bo=0;
          foreach(var ac in active){ var l=ac[ac.Count-1]; int ov=Math.Min(l[2],r[2])-Math.Max(l[1],r[1]); int mn=Math.Min(l[2]-l[1],r[2]-r[1]); if(ov>0.5*mn&&ov>bo){ bo=ov; best=ac; } }
          if(best!=null&&!next.Contains(best)){ best.Add(r); next.Add(best); } else { var n=new List<int[]>(); n.Add(r); lines.Add(n); next.Add(n); } }
        active=next; }
      foreach(var ln in lines){
        int mx=0; foreach(var r in ln) mx=Math.Max(mx,r[2]-r[1]+1);
        if(mx<minLen) continue;
        var core=new List<int[]>(); foreach(var r in ln) if(r[2]-r[1]+1>=0.6*mx) core.Add(r);
        if(core.Count==0||core.Count>maxThick) continue;
        // концы: медианы
        var s0=new List<int>(); var e0=new List<int>(); double cb=0; foreach(var r in core){ s0.Add(r[1]); e0.Add(r[2]); cb+=r[0]+0.5; } s0.Sort(); e0.Sort(); cb/=core.Count;
        double a0=s0[s0.Count/2], a1=e0[e0.Count/2]+1; double th=core.Count;
        foreach(var r in core) for(int a=r[1];a<=r[2];a++) erase.Add(idx(a,r[0]));
        double pad=0.6; // выступ на концах и по толщине, чтобы не было швов
        double xa=a0-pad, xb=a1+pad, ya=cb-th/2-0.25, yb=cb+th/2+0.25;
        if(!vert) sb.Append("M"+F(xa)+" "+F(ya)+"H"+F(xb)+"V"+F(yb)+"H"+F(xa)+"Z");
        else sb.Append("M"+F(ya)+" "+F(xa)+"H"+F(yb)+"V"+F(xb)+"H"+F(ya)+"Z");
        cnt++; } }
    foreach(int i in erase) cls[i]=0;
    Console.WriteLine("snap class "+c+": rects "+cnt);
    return sb.ToString();
  }
}
public static class Dots {
  static string F(double v){ return Math.Round(v,2).ToString("0.##",CultureInfo.InvariantCulture); }
  // вписанные круги в чёрные пятна (узлы соединения проводов)
  public static string Run(byte[] cls,int W,int H,double minR){
    var d=new float[W*H]; for(int i=0;i<W*H;i++) d[i]=cls[i]==1?1e4f:0f;
    for(int y=0;y<H;y++) for(int x=0;x<W;x++){ int i=y*W+x; float v=d[i]; if(v==0) continue; if(x>0) v=Math.Min(v,d[i-1]+1); if(y>0) v=Math.Min(v,d[i-W]+1); if(x>0&&y>0) v=Math.Min(v,d[i-W-1]+1.414f); if(x<W-1&&y>0) v=Math.Min(v,d[i-W+1]+1.414f); d[i]=v; }
    for(int y=H-1;y>=0;y--) for(int x=W-1;x>=0;x--){ int i=y*W+x; float v=d[i]; if(v==0) continue; if(x<W-1) v=Math.Min(v,d[i+1]+1); if(y<H-1) v=Math.Min(v,d[i+W]+1); if(x<W-1&&y<H-1) v=Math.Min(v,d[i+W+1]+1.414f); if(x>0&&y<H-1) v=Math.Min(v,d[i+W-1]+1.414f); d[i]=v; }
    var sb=new StringBuilder(); int n=0; int R=(int)(4*Seg.Sc/1.5);
    for(int y=R;y<H-R;y++) for(int x=R;x<W-R;x++){ float v=d[y*W+x]; if(v<minR) continue; bool mx=true;
      for(int dy=-R;dy<=R&&mx;dy++) for(int dx=-R;dx<=R;dx++){ float u=d[(y+dy)*W+x+dx]; if(u>v||(u==v&&(dy<0||(dy==0&&dx<0)))){ mx=false; break; } }
      if(!mx) continue; double r=v-0.4, cx=x+0.5, cy=y+0.5;
      sb.Append("M"+F(cx-r)+" "+F(cy)+"a"+F(r)+" "+F(r)+" 0 1 1 "+F(2*r)+" 0a"+F(r)+" "+F(r)+" 0 1 1 "+F(-2*r)+" 0Z"); n++; }
    Console.WriteLine("dots "+n); return sb.ToString(); }
}
