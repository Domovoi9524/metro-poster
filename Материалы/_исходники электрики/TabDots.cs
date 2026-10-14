using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
public static class TabDots {
  public static void Run(string dir,int W,int H){
    var cls=File.ReadAllBytes(dir+"/cls.bin"); double K=W/1600.0;
    int x0=(int)(1240*K), x1=(int)(1580*K), y0=(int)(170*K), y1=(int)(480*K);
    int bw=x1-x0, bh=y1-y0; var d=new float[bw*bh];
    for(int y=0;y<bh;y++) for(int x=0;x<bw;x++) d[y*bw+x]= cls[(y+y0)*W+x+x0]==1?1e4f:0f;
    for(int y=0;y<bh;y++) for(int x=0;x<bw;x++){ int i=y*bw+x; float v=d[i]; if(v==0) continue; if(x>0) v=Math.Min(v,d[i-1]+1); if(y>0) v=Math.Min(v,d[i-bw]+1); if(x>0&&y>0) v=Math.Min(v,d[i-bw-1]+1.414f); if(x<bw-1&&y>0) v=Math.Min(v,d[i-bw+1]+1.414f); d[i]=v; }
    for(int y=bh-1;y>=0;y--) for(int x=bw-1;x>=0;x--){ int i=y*bw+x; float v=d[i]; if(v==0) continue; if(x<bw-1) v=Math.Min(v,d[i+1]+1); if(y<bh-1) v=Math.Min(v,d[i+bw]+1); if(x<bw-1&&y<bh-1) v=Math.Min(v,d[i+bw+1]+1.414f); if(x>0&&y<bh-1) v=Math.Min(v,d[i+bw-1]+1.414f); d[i]=v; }
    var pts=new List<double[]>(); int R=7;
    for(int y=R;y<bh-R;y++) for(int x=R;x<bw-R;x++){ float v=d[y*bw+x]; if(v<6) continue; bool mx=true;
      for(int dy=-R;dy<=R&&mx;dy++) for(int dx=-R;dx<=R;dx++){ float u=d[(y+dy)*bw+x+dx]; if(u>v||(u==v&&(dy<0||(dy==0&&dx<0)))){ mx=false; break; } }
      if(mx) pts.Add(new double[]{(x+x0+0.5)/K,(y+y0+0.5)/K,v/K}); }
    Console.WriteLine("dots "+pts.Count);
    var sb=new System.Text.StringBuilder(); foreach(var p in pts) sb.AppendLine(p[0].ToString("0.0",CultureInfo.InvariantCulture)+" "+p[1].ToString("0.0",CultureInfo.InvariantCulture)+" "+p[2].ToString("0.0",CultureInfo.InvariantCulture));
    File.WriteAllText(dir+"/tabdots.txt",sb.ToString());
  }
}
