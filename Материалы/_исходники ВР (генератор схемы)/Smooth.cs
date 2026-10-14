using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
/* Сглаженная векторизация маски пикселей: маска увеличивается в U раз, размывается по Гауссу, по порогу 0,5
   выделяются контуры (marching squares), затем контуры упрощаются. Лесенка превращается в гладкие линии. */
public static class Sm {
  public static double Sigma=0.38, Eps=0.12; public static int U=4;
  static string F(double v){ return Math.Round(v,1).ToString("0.#",CultureInfo.InvariantCulture); }
  public static string Path(bool[,] m,int w,int h){
    int x0=w,y0=h,x1=-1,y1=-1;
    for(int y=0;y<h;y++) for(int x=0;x<w;x++) if(m[x,y]){ if(x<x0)x0=x; if(x>x1)x1=x; if(y<y0)y0=y; if(y>y1)y1=y; }
    if(x1<0) return "";
    int mg=3, bx0=x0-mg, by0=y0-mg, bw=x1-x0+1+2*mg, bh=y1-y0+1+2*mg, W=bw*U, H=bh*U;
    float[] a=new float[W*H], tmp=new float[W*H];
    for(int y=0;y<bh;y++) for(int x=0;x<bw;x++){ int sx=bx0+x, sy=by0+y; if(sx>=0&&sy>=0&&sx<w&&sy<h&&m[sx,sy]){ for(int j=0;j<U;j++){ int o=(y*U+j)*W+x*U; for(int i=0;i<U;i++) a[o+i]=1f; } } }
    double sg=Sigma*U; int r=(int)Math.Ceiling(3*sg); var k=new float[2*r+1]; float ks=0; for(int i=-r;i<=r;i++){ k[i+r]=(float)Math.Exp(-i*i/(2*sg*sg)); ks+=k[i+r]; } for(int i=0;i<k.Length;i++) k[i]/=ks;
    for(int y=0;y<H;y++){ int o=y*W; for(int x=0;x<W;x++){ float s=0; for(int i=-r;i<=r;i++){ int xx=x+i; if(xx<0||xx>=W) continue; s+=a[o+xx]*k[i+r]; } tmp[o+x]=s; } }
    for(int y=0;y<H;y++) for(int x=0;x<W;x++){ float s=0; for(int i=-r;i<=r;i++){ int yy=y+i; if(yy<0||yy>=H) continue; s+=tmp[yy*W+x]*k[i+r]; } a[y*W+x]=s; }
    const float T=0.5f;
    var adj=new Dictionary<int,int[]>();
    Action<int,int> link=(p,q)=>{ int[] v; if(!adj.TryGetValue(p,out v)){ v=new int[]{-1,-1}; adj[p]=v; } if(v[0]<0) v[0]=q; else v[1]=q; };
    for(int j=0;j<H-1;j++) for(int i=0;i<W-1;i++){
      float tl=a[j*W+i], tr=a[j*W+i+1], br=a[(j+1)*W+i+1], bl=a[(j+1)*W+i];
      int c=(tl>T?1:0)|(tr>T?2:0)|(br>T?4:0)|(bl>T?8:0); if(c==0||c==15) continue;
      int eT=(j*W+i)*2, eB=((j+1)*W+i)*2, eL=(j*W+i)*2+1, eR=(j*W+i+1)*2+1;
      switch(c){
        case 1: case 14: link(eL,eT); link(eT,eL); break;
        case 2: case 13: link(eT,eR); link(eR,eT); break;
        case 3: case 12: link(eL,eR); link(eR,eL); break;
        case 4: case 11: link(eR,eB); link(eB,eR); break;
        case 6: case 9: link(eT,eB); link(eB,eT); break;
        case 7: case 8: link(eL,eB); link(eB,eL); break;
        case 5: case 10: { float ce=(tl+tr+br+bl)/4; bool inside=ce>T; bool two = (c==5)==inside;   // c==5: TL,BR внутри
            if(two){ link(eT,eR); link(eR,eT); link(eL,eB); link(eB,eL); } else { link(eL,eT); link(eT,eL); link(eR,eB); link(eB,eR); } break; }
      }
    }
    Func<int,double[]> pt=(id)=>{ int vert=id&1; int cell=id>>1; int jj=cell/W, ii=cell%W; double fx,fy;
      if(vert==0){ float v0=a[jj*W+ii], v1=a[jj*W+ii+1]; fx=ii+(T-v0)/(v1-v0); fy=jj; } else { float v0=a[jj*W+ii], v1=a[(jj+1)*W+ii]; fx=ii; fy=jj+(T-v0)/(v1-v0); }
      return new double[]{bx0+(fx+0.5)/U, by0+(fy+0.5)/U}; };
    var seen=new HashSet<int>(); var sb=new StringBuilder();
    foreach(var kv in adj){
      if(seen.Contains(kv.Key)) continue;
      var loop=new List<double[]>(); int prev=-1, cur=kv.Key;
      int start=cur;
      while(true){ seen.Add(cur); loop.Add(pt(cur)); int[] nb=adj[cur]; int nx; if(prev==-1) nx=nb[0]; else nx=(nb[0]==prev)?nb[1]:nb[0]; if(nx<0||nx==start||seen.Contains(nx)) break; prev=cur; cur=nx; }
      if(loop.Count<3) continue;
      var poly=Simplify(loop); if(poly.Count<3) continue;
      for(int i=0;i<poly.Count;i++) sb.Append((i==0?"M":"L")+F(poly[i][0])+" "+F(poly[i][1])+" ");
      sb.Append("Z");
    }
    return sb.ToString();
  }
  static List<double[]> Simplify(List<double[]> p){
    int n=p.Count; int far=0; double bd=-1; for(int i=1;i<n;i++){ double d=(p[i][0]-p[0][0])*(p[i][0]-p[0][0])+(p[i][1]-p[0][1])*(p[i][1]-p[0][1]); if(d>bd){bd=d;far=i;} }
    var a=new List<double[]>(); for(int i=0;i<=far;i++) a.Add(p[i]);
    var b=new List<double[]>(); for(int i=far;i<n;i++) b.Add(p[i]); b.Add(p[0]);
    var ra=Rdp(a); var rb=Rdp(b); var res=new List<double[]>(ra); for(int i=1;i<rb.Count-1;i++) res.Add(rb[i]); return res;
  }
  static List<double[]> Rdp(List<double[]> pts){
    int n=pts.Count; if(n<3) return new List<double[]>(pts);
    var keep=new bool[n]; keep[0]=true; keep[n-1]=true; var st=new Stack<int[]>(); st.Push(new int[]{0,n-1});
    while(st.Count>0){ var s=st.Pop(); int a=s[0], b=s[1]; double md=0; int mi=-1;
      double dx=pts[b][0]-pts[a][0], dy=pts[b][1]-pts[a][1], len=Math.Sqrt(dx*dx+dy*dy);
      for(int i=a+1;i<b;i++){ double d= len<1e-9 ? Math.Sqrt(Math.Pow(pts[i][0]-pts[a][0],2)+Math.Pow(pts[i][1]-pts[a][1],2)) : Math.Abs(dy*(pts[i][0]-pts[a][0])-dx*(pts[i][1]-pts[a][1]))/len; if(d>md){md=d;mi=i;} }
      if(md>Eps&&mi>0){ keep[mi]=true; st.Push(new int[]{a,mi}); st.Push(new int[]{mi,b}); } }
    var r=new List<double[]>(); for(int i=0;i<n;i++) if(keep[i]) r.Add(pts[i]); return r;
  }
}
