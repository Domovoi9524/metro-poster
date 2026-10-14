using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
// ЭПВ: k-means палитра, очистка шума, сглаженная послойная векторизация
public static class Epv {
  public static int[] Pal; public static byte[,] Q; static int W,H; static Bitmap bmp;
  static double D2(Color c,int p){ double dr=c.R-((p>>16)&255), dg=c.G-((p>>8)&255), db=c.B-(p&255); return dr*dr+dg*dg+db*db; }
  public static void Run(string src,string outFile,int K){
    bmp=new Bitmap(src); W=bmp.Width; H=bmp.Height;
    var px=new Color[W*H]; for(int y=0;y<H;y++) for(int x=0;x<W;x++) px[y*W+x]=bmp.GetPixel(x,y);
    // k-means (инициализация по равномерной выборке яркости + частоте)
    var rnd=new Random(7); var cen=new double[K,3];
    var nonw=px.Where(c=>!(c.R>238&&c.G>238&&c.B>238)).ToArray();
    { var c0=nonw[rnd.Next(nonw.Length)]; cen[0,0]=c0.R;cen[0,1]=c0.G;cen[0,2]=c0.B;
      for(int k=1;k<K;k++){ double best=-1; Color bc=nonw[0]; for(int t=0;t<400;t++){ var c=nonw[rnd.Next(nonw.Length)]; double md=1e18; for(int j=0;j<k;j++){ double dr=c.R-cen[j,0],dg=c.G-cen[j,1],db=c.B-cen[j,2]; md=Math.Min(md,dr*dr+dg*dg+db*db); } if(md>best){best=md;bc=c;} } cen[k,0]=bc.R;cen[k,1]=bc.G;cen[k,2]=bc.B; } }
    var asg=new int[px.Length];
    for(int it=0;it<14;it++){ var sum=new double[K,3]; var cnt=new int[K];
      for(int i=0;i<px.Length;i+=2){ var c=px[i]; if(c.R>238&&c.G>238&&c.B>238) continue; int b=0; double bd=1e18; for(int k=0;k<K;k++){ double dr=c.R-cen[k,0],dg=c.G-cen[k,1],db=c.B-cen[k,2]; double d=dr*dr+dg*dg+db*db; if(d<bd){bd=d;b=k;} } sum[b,0]+=c.R;sum[b,1]+=c.G;sum[b,2]+=c.B;cnt[b]++; }
      for(int k=0;k<K;k++) if(cnt[k]>0){ cen[k,0]=sum[k,0]/cnt[k]; cen[k,1]=sum[k,1]/cnt[k]; cen[k,2]=sum[k,2]/cnt[k]; } }
    Pal=new int[K+1]; Pal[K]=0xffffff; for(int k=0;k<K;k++) Pal[k]=((int)cen[k,0]<<16)|((int)cen[k,1]<<8)|(int)cen[k,2];
    // добавить чистые белый и чёрный
    Q=new byte[W,H];
    for(int y=0;y<H;y++) for(int x=0;x<W;x++){ var c=px[y*W+x]; if(c.R>238&&c.G>238&&c.B>238){ Q[x,y]=(byte)K; continue; } int b=0; double bd=1e18; for(int k=0;k<K;k++){ double d=D2(c,Pal[k]); if(d<bd){bd=d;b=k;} } Q[x,y]=(byte)b; }
    Console.WriteLine("k="+K);
    for(int k=0;k<=K;k++) Console.WriteLine(k+" #"+Pal[k].ToString("x6")+" "+Enumerable.Range(0,W).Sum(x=>Enumerable.Range(0,H).Count(y=>Q[x,y]==k)));
    Save(outFile+".q.png");
  }
  public static void Save(string f){ var o=new Bitmap(W,H); for(int y=0;y<H;y++) for(int x=0;x<W;x++) o.SetPixel(x,y,Color.FromArgb(Pal[Q[x,y]]|unchecked((int)0xff000000))); o.Save(f); }
}
public static class EpvTrace {
  public static void Run(string outFile){
    int W=Epv.Q.GetLength(0),H=Epv.Q.GetLength(1); int K=Epv.Pal.Length-1; var q=Epv.Q;
    // стирание: манометр, подписи, наклейка, правая часть (провода, система АРС)
    foreach(var r in new[]{ new[]{0,0,124,117}, new[]{116,119,140,134}, new[]{116,174,140,190}, new[]{146,134,212,160}, new[]{513,0,W,H} })
      for(int y=r[1];y<r[3]&&y<H;y++) for(int x=r[0];x<r[2]&&x<W;x++) q[x,y]=(byte)K;
    // шум: розовая россыпь и голубое облако
    for(int y=0;y<H;y++) for(int x=0;x<W;x++){ int c=Epv.Pal[q[x,y]]; int R=(c>>16)&255,G=(c>>8)&255,B=c&255; bool pink=(R>200&&G<215&&B<215&&R-G>25&&y>195&&y<268&&x<345); bool cyan=(B>200&&R<235&&G>200&&x>478&&x<530&&y>128&&y<175&&!(R<60)); if(pink||cyan) q[x,y]=(byte)K; }
    // порядок: от светлых к тёмным
    var order=Enumerable.Range(0,K).Where(k=>Enumerable.Range(0,W).Any(x=>Enumerable.Range(0,H).Any(y=>q[x,y]==k))).OrderByDescending(k=>{ int c=Epv.Pal[k]; return 0.3*((c>>16)&255)+0.59*((c>>8)&255)+0.11*(c&255); }).ToList();
    var rank=new int[K+1]; for(int i=0;i<order.Count;i++) rank[order[i]]=i+1;
    var sb=new StringBuilder(); Sm.U=4; Sm.Sigma=0.62; Sm.Eps=0.14;
    foreach(int c in order){ var m=new bool[W,H]; bool any=false; for(int y=0;y<H;y++) for(int x=0;x<W;x++){ int cc=q[x,y]; if(cc==K) continue; if(rank[cc]>=rank[c]){ m[x,y]=true; any=true; } }
      if(!any) continue; string d=Sm.Path(m,W,H); if(d.Length==0) continue; sb.Append("<path fill=\"#"+Epv.Pal[c].ToString("x6")+"\" fill-rule=\"evenodd\" d=\""+d+"\"/>\n"); }
    File.WriteAllText(outFile,sb.ToString()); Epv.Save(outFile+".png"); Console.WriteLine("layers "+order.Count+" "+sb.Length);
  }
}
