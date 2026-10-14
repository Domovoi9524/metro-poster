using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
public static class Route {
  static int W=2620, H=1930; static double K=W/1600.0;
  static byte[] cls; static float[] dist;
  class Heap { List<long> a=new List<long>(); public int Count{get{return a.Count;}}
    public void Push(long v){ a.Add(v); int i=a.Count-1; while(i>0){ int p=(i-1)/2; if(a[p]<=a[i]) break; var t=a[p];a[p]=a[i];a[i]=t;i=p; } }
    public long Pop(){ long r=a[0]; a[0]=a[a.Count-1]; a.RemoveAt(a.Count-1); int i=0; while(true){ int l=i*2+1, rr=l+1, m=i; if(l<a.Count&&a[l]<a[m]) m=l; if(rr<a.Count&&a[rr]<a[m]) m=rr; if(m==i) break; var t=a[m];a[m]=a[i];a[i]=t;i=m; } return r; } }
  static int[] Snap(double ux,double uy){
    int px=(int)(ux*K), py=(int)(uy*K); int bx=px,by=py; double bd=1e9; if(px>=0&&py>=0&&px<W&&py<H&&cls[py*W+px]>1) return new int[]{px,py};
    for(int dy=-36;dy<=36;dy++) for(int dx=-36;dx<=36;dx++){ int x=px+dx,y=py+dy; if(x<0||y<0||x>=W||y>=H) continue; if(cls[y*W+x]!=1) continue; double d=dx*dx+dy*dy; if(d<bd){bd=d;bx=x;by=y;} }
    if(bd>1e8){ for(int dy=-36;dy<=36;dy++) for(int dx=-36;dx<=36;dx++){ int x=px+dx,y=py+dy; if(x<0||y<0||x>=W||y>=H) continue; if(cls[y*W+x]==0) continue; double d=dx*dx+dy*dy; if(d<bd){bd=d;bx=x;by=y;} } }
    return new int[]{bx,by};
  }
  static List<int[]> Astar(int[] a,int[] b){
    int x0=Math.Max(0,Math.Min(a[0],b[0])-450), x1=Math.Min(W-1,Math.Max(a[0],b[0])+450), y0=Math.Max(0,Math.Min(a[1],b[1])-450), y1=Math.Min(H-1,Math.Max(a[1],b[1])+450);
    int bw=x1-x0+1, bh=y1-y0+1; var g=new float[bw*bh]; var prev=new int[bw*bh]; for(int i=0;i<g.Length;i++){ g[i]=1e9f; prev[i]=-1; }
    var hp=new Heap(); int s=(a[1]-y0)*bw+(a[0]-x0), t=(b[1]-y0)*bw+(b[0]-x0); g[s]=0;
    Func<int,double> hf=(idx)=>{ int x=idx%bw+x0, y=idx/bw+y0; return Math.Sqrt((x-b[0])*(x-b[0])+(y-b[1])*(y-b[1])); };
    hp.Push(((long)(int)(hf(s)*100))<<32|(uint)s);
    int[] dxs={1,-1,0,0,1,1,-1,-1}, dys={0,0,1,-1,1,-1,1,-1};
    while(hp.Count>0){ long v=hp.Pop(); int cur=(int)(v&0xffffffff); if(cur==t) break; int cx=cur%bw, cy=cur/bw; float gc=g[cur];
      for(int k=0;k<8;k++){ int nx=cx+dxs[k], ny=cy+dys[k]; if(nx<0||ny<0||nx>=bw||ny>=bh) continue; int n=ny*bw+nx; byte c=cls[(ny+y0)*W+(nx+x0)];
        float step=(k<4)?1f:1.414f; float dd=dist[(ny+y0)*W+(nx+x0)]; float cost = c==0?9f : (1f+ (c==1?1.0f:1.6f)*0+ 7f/(1f+Math.Min(dd,12f)*1.2f)); float ng=gc+step*cost; if(ng<g[n]){ g[n]=ng; prev[n]=cur; hp.Push(((long)(int)((ng+hf(n))*100))<<32|(uint)n); } } }
    var path=new List<int[]>(); int p=t; if(prev[p]==-1&&p!=s) return path; while(p!=-1){ path.Add(new int[]{p%bw+x0,p/bw+y0}); p=prev[p]; } path.Reverse(); return path;
  }
  static List<double[]> Rdp(List<double[]> pts,double eps){
    int n=pts.Count; if(n<3) return new List<double[]>(pts); var keep=new bool[n]; keep[0]=true; keep[n-1]=true; var st=new Stack<int[]>(); st.Push(new int[]{0,n-1});
    while(st.Count>0){ var s=st.Pop(); int a=s[0],b=s[1]; double md=0; int mi=-1; double dx=pts[b][0]-pts[a][0], dy=pts[b][1]-pts[a][1], len=Math.Sqrt(dx*dx+dy*dy);
      for(int i=a+1;i<b;i++){ double d= len<1e-9?Math.Sqrt(Math.Pow(pts[i][0]-pts[a][0],2)+Math.Pow(pts[i][1]-pts[a][1],2)):Math.Abs(dy*(pts[i][0]-pts[a][0])-dx*(pts[i][1]-pts[a][1]))/len; if(d>md){md=d;mi=i;} }
      if(md>eps&&mi>0){ keep[mi]=true; st.Push(new int[]{a,mi}); st.Push(new int[]{mi,b}); } }
    var r=new List<double[]>(); for(int i=0;i<n;i++) if(keep[i]) r.Add(pts[i]); return r;
  }
  public static void Run(string dir,string routesFile,string outFile,int w,int h){ W=w; H=h; K=w/1600.0;
    cls=File.ReadAllBytes(dir+"/cls.bin");
    dist=new float[W*H]; for(int i=0;i<W*H;i++) dist[i]=cls[i]==0?0f:60f;
    for(int y=0;y<H;y++) for(int x=0;x<W;x++){ int i=y*W+x; float d=dist[i]; if(x>0) d=Math.Min(d,dist[i-1]+1); if(y>0) d=Math.Min(d,dist[i-W]+1); if(x>0&&y>0) d=Math.Min(d,dist[i-W-1]+1.414f); if(x<W-1&&y>0) d=Math.Min(d,dist[i-W+1]+1.414f); dist[i]=d; }
    for(int y=H-1;y>=0;y--) for(int x=W-1;x>=0;x--){ int i=y*W+x; float d=dist[i]; if(x<W-1) d=Math.Min(d,dist[i+1]+1); if(y<H-1) d=Math.Min(d,dist[i+W]+1); if(x<W-1&&y<H-1) d=Math.Min(d,dist[i+W+1]+1.414f); if(x>0&&y<H-1) d=Math.Min(d,dist[i+W-1]+1.414f); dist[i]=d; }
    var sb=new StringBuilder();
    foreach(var ln in File.ReadAllLines(routesFile)){
      if(ln.Trim().Length==0||ln.StartsWith("#")) continue; var p=ln.Split('|'); string name=p[0]; var wps=new List<int[]>(); var raw=new List<double[]>();
      foreach(var t in p[1].Split(';')){ var q=t.Split(','); double ux=double.Parse(q[0],CultureInfo.InvariantCulture), uy=double.Parse(q[1],CultureInfo.InvariantCulture); raw.Add(new double[]{ux,uy}); wps.Add(Snap(ux,uy)); }
      var full=new List<double[]>(); bool ok=true;
      for(int i=0;i+1<wps.Count;i++){ var seg=Astar(wps[i],wps[i+1]); if(seg.Count==0){ Console.WriteLine("нет пути: "+name+" "+raw[i][0]+","+raw[i][1]+" -> "+raw[i+1][0]+","+raw[i+1][1]); ok=false; continue; } int st=full.Count>0?1:0; for(int j=st;j<seg.Count;j++) full.Add(new double[]{(seg[j][0]+0.5)/K,(seg[j][1]+0.5)/K}); }
      var poly=Rdp(full,0.9); var s2=new StringBuilder(); for(int i=0;i<poly.Count;i++) s2.Append((i==0?"M":"L")+Math.Round(poly[i][0],1).ToString("0.#",CultureInfo.InvariantCulture)+" "+Math.Round(poly[i][1],1).ToString("0.#",CultureInfo.InvariantCulture)+" ");
      sb.Append(name+"|"+s2.ToString().Trim()+"\n"); Console.WriteLine(name+" точек "+poly.Count+(ok?"":" (с пропусками)")); }
    File.WriteAllText(outFile,sb.ToString());
  }
}
