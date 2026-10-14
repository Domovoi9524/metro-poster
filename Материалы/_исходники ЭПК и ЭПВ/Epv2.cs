using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
public static class Epv2 {
  static int W,H,K; static byte[,] q;
  static bool Tan(int c){ int R=(c>>16)&255,G=(c>>8)&255,B=c&255; return (R>170&&G>140&&B>100&&R-B>35&&R-B<110)||(R>100&&R<145&&G>80&&G<115&&B>40&&B<75); }
  static string Lay(byte[,] a,bool[,] only,List<int> order){
    var rank=new int[K+1]; for(int i=0;i<order.Count;i++) rank[order[i]]=i+1; var sb=new StringBuilder(); Sm.U=4; Sm.Sigma=0.62; Sm.Eps=0.14;
    foreach(int c in order){ var m=new bool[W,H]; bool any=false; for(int y=0;y<H;y++) for(int x=0;x<W;x++){ if(only!=null&&!only[x,y]) continue; int cc=a[x,y]; if(cc==K) continue; if(rank[cc]>=rank[c]){ m[x,y]=true; any=true; } }
      if(!any) continue; string d=Sm.Path(m,W,H); if(d.Length==0) continue; sb.Append("<path fill=\"#"+Epv.Pal[c].ToString("x6")+"\" fill-rule=\"evenodd\" d=\""+d+"\"/>\n"); }
    return sb.ToString(); }
  static bool[,] Dil(bool[,] m,int r){ var o=new bool[W,H]; for(int y=0;y<H;y++) for(int x=0;x<W;x++){ if(!m[x,y]) continue; for(int j=-r;j<=r;j++) for(int i=-r;i<=r;i++){ int xx=x+i,yy=y+j; if(xx>=0&&yy>=0&&xx<W&&yy<H) o[xx,yy]=true; } } return o; }
  public static void Run(string outFile){
    W=Epv.Q.GetLength(0); H=Epv.Q.GetLength(1); K=Epv.Pal.Length-1; q=Epv.Q; var P=Epv.Pal;
    // серый цвет корпуса: самый частый серый
    int gray=Enumerable.Range(0,K).Where(k=>{ int c=P[k]; int R=(c>>16)&255,G=(c>>8)&255,B=c&255; return Math.Abs(R-G)<8&&Math.Abs(G-B)<8&&R>120&&R<200; }).OrderByDescending(k=>Enumerable.Range(0,W).Sum(x=>Enumerable.Range(0,H).Count(y=>q[x,y]==k))).First();
    foreach(var r in new[]{ new[]{0,0,124,117}, new[]{116,119,140,134}, new[]{116,174,140,190}, new[]{513,0,W,H} }) for(int y=r[1];y<r[3]&&y<H;y++) for(int x=r[0];x<r[2]&&x<W;x++) q[x,y]=(byte)K;
    for(int y=134;y<160;y++) for(int x=146;x<212;x++) q[x,y]=(byte)gray;           // наклейка «0 кГс/см²» была на сером корпусе
    // красные цифры/надпись «ТМ» внутри белой коробки
    for(int y=200;y<222;y++) for(int x=236;x<262;x++){ int c=P[q[x,y]]; if(((c>>16)&255)>180&&((c>>8)&255)<120) q[x,y]=(byte)K; }
    for(int y=0;y<H;y++) for(int x=0;x<W;x++){ int c=P[q[x,y]]; int R=(c>>16)&255,G=(c>>8)&255,B=c&255;
      bool pink=(R>190&&G>110&&B>110&&R-G>12&&R-B>8&&y>195&&y<270&&x<372&&!(R<140)); bool cyan=(B>190&&R<240&&G>190&&x>478&&x<530&&y>128&&y<175); if(pink||cyan) q[x,y]=(byte)K; }
    foreach(var z in new[]{ new[]{120,205,222,268}, new[]{280,196,372,262} }) for(int y=z[1];y<z[3];y++) for(int x=z[0];x<z[2];x++){ if(q[x,y]==K) continue; int c=P[q[x,y]]; int R=(c>>16)&255,G=(c>>8)&255,B=c&255; if(R-B>14||(R>235&&G>225&&B>225)) q[x,y]=(byte)K; }
    { var seen=new bool[W,H]; var st=new Stack<int>(); for(int y0=0;y0<H;y0++) for(int x0=0;x0<W;x0++){ if(seen[x0,y0]||q[x0,y0]==K) continue; var comp=new List<int>(); st.Push(y0*W+x0); seen[x0,y0]=true; while(st.Count>0){ int p=st.Pop(); comp.Add(p); int px=p%W,py=p/W; for(int j=-1;j<=1;j++) for(int i=-1;i<=1;i++){ int nx=px+i,ny=py+j; if(nx<0||ny<0||nx>=W||ny>=H||seen[nx,ny]||q[nx,ny]==K) continue; seen[nx,ny]=true; st.Push(ny*W+nx); } }
        if(comp.Count<45){ foreach(int p in comp) q[p%W,p/W]=(byte)K; } } }
    // детали
    var rod=new bool[W,H]; var box=new bool[W,H];
    for(int y=56;y<=176;y++) for(int x=462;x<=481;x++) if(Tan(P[q[x,y]])) rod[x,y]=true;
    var rodD=Dil(rod,2); for(int y=0;y<H;y++) for(int x=0;x<W;x++) if(rodD[x,y]&&q[x,y]!=K&&x>=460&&x<=483&&y>=54&&y<=178){ int c=P[q[x,y]]; int R=(c>>16)&255,G=(c>>8)&255,B=c&255; if(Tan(c)||R+G+B<260) rod[x,y]=true; }
    for(int y=196;y<=234;y++) for(int x=228;x<=281;x++) if(q[x,y]!=K) box[x,y]=true;
    var stat=(byte[,])q.Clone();
    Action<bool[,],int> vac=(m,dy)=>{ // вертикальная заливка вакантных пикселей ближайшим статическим
      for(int x=0;x<W;x++) for(int y=0;y<H;y++) if(m[x,y]){ int best=K,bd=999; for(int d=1;d<40;d++){ foreach(int yy in new[]{y+d,y-d}){ if(yy<0||yy>=H||m[x,yy]) continue; best=q[x,yy]; bd=d; break; } if(bd<999) break; } stat[x,y]=(byte)best; } };
    vac(rod,-6); vac(box,-8);
    var order=Enumerable.Range(0,K).Where(k=>Enumerable.Range(0,W).Any(x=>Enumerable.Range(0,H).Any(y=>q[x,y]==k))).OrderByDescending(k=>{ int c=P[k]; return 0.3*((c>>16)&255)+0.59*((c>>8)&255)+0.11*(c&255); }).ToList();
    var sb=new StringBuilder(); sb.Append("<g id=\"stat\">\n"+Lay(stat,null,order)+"</g>\n");
    sb.Append("<g class=\"mv\" data-p=\"rod\">\n"+Lay(q,rod,order)+"</g>\n"); sb.Append("<g class=\"mv\" data-p=\"box\">\n"+Lay(q,box,order)+"</g>\n");
    File.WriteAllText(outFile,sb.ToString()); Console.WriteLine("ok "+sb.Length);
    var o=new Bitmap(W*2+10,H); for(int y=0;y<H;y++) for(int x=0;x<W;x++){ o.SetPixel(x,y,Color.FromArgb(P[stat[x,y]]|unchecked((int)0xff000000))); o.SetPixel(W+10+x,y,rod[x,y]||box[x,y]?Color.Red:Color.FromArgb(P[q[x,y]]|unchecked((int)0xff000000))); } o.Save(outFile+".png");
  }
}
