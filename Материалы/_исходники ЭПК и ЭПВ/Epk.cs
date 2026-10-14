using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
// Строит слои ЭПК: два состояния (A — под питанием, B — без питания), подвижные детали, векторные контуры
public static class Epk {
  const int X0=0, Y0=100, W=562, BX=574, BY=104;      // область A; B выровнено сдвигом (BX,BY)
  static int H;
  static byte[,] A,B;
  static readonly int[] ORDER={2,4,5,3,10,6,7,8,9,1};  // нижние слои — светлые, чёрный сверху
  // ---------- вспомогательные ----------
  static byte[,] Clone(byte[,] a){ return (byte[,])a.Clone(); }
  static void Inpaint(byte[,] q,bool[,] unk){
    int w=q.GetLength(0),h=q.GetLength(1); var qu=new Queue<int>(); var known=new bool[w,h];
    for(int y=0;y<h;y++) for(int x=0;x<w;x++){ known[x,y]=!unk[x,y]; if(known[x,y]) qu.Enqueue(y*w+x); }
    int[] dx={1,-1,0,0}, dy={0,0,1,-1};
    while(qu.Count>0){ int p=qu.Dequeue(); int x=p%w,y=p/w; for(int k=0;k<4;k++){ int nx=x+dx[k],ny=y+dy[k]; if(nx<0||ny<0||nx>=w||ny>=h||known[nx,ny]) continue; known[nx,ny]=true; q[nx,ny]=q[x,y]; qu.Enqueue(ny*w+nx); } } }
  static bool[,] Dilate(bool[,] m,int r){ int w=m.GetLength(0),h=m.GetLength(1); var o=new bool[w,h]; for(int y=0;y<h;y++) for(int x=0;x<w;x++){ if(!m[x,y]) continue; for(int j=-r;j<=r;j++) for(int i=-r;i<=r;i++){ if(i*i+j*j>r*r) continue; int xx=x+i,yy=y+j; if(xx>=0&&yy>=0&&xx<w&&yy<h) o[xx,yy]=true; } } return o; }
  static bool[,] Erode(bool[,] m,int r){ int w=m.GetLength(0),h=m.GetLength(1); var o=new bool[w,h]; for(int y=0;y<h;y++) for(int x=0;x<w;x++){ if(!m[x,y]) continue; bool ok=true; for(int j=-r;j<=r&&ok;j++) for(int i=-r;i<=r;i++){ if(i*i+j*j>r*r) continue; int xx=x+i,yy=y+j; if(xx<0||yy<0||xx>=w||yy>=h||!m[xx,yy]){ ok=false; break; } } o[x,y]=ok; } return o; }
  static List<List<int>> Comps(byte[,] q,Func<byte,bool> sel){
    int w=q.GetLength(0),h=q.GetLength(1); var seen=new bool[w,h]; var res=new List<List<int>>(); var st=new Stack<int>();
    for(int y=0;y<h;y++) for(int x=0;x<w;x++){ if(seen[x,y]||!sel(q[x,y])) continue; var c=new List<int>(); st.Push(y*w+x); seen[x,y]=true;
      while(st.Count>0){ int p=st.Pop(); c.Add(p); int px=p%w,py=p/w; for(int j=-1;j<=1;j++) for(int i=-1;i<=1;i++){ int nx=px+i,ny=py+j; if(nx<0||ny<0||nx>=w||ny>=h||seen[nx,ny]||!sel(q[nx,ny])) continue; seen[nx,ny]=true; st.Push(ny*w+nx); } }
      res.Add(c); }
    return res; }
  // убрать плавающие чёрные компоненты (подписи) вне корпуса
  static void RemoveLabels(byte[,] q){
    foreach(var r in new[]{ new[]{15,530,100,585}, new[]{245,605,325,650}, new[]{440,290,515,335} }) for(int y=r[1]-Y0;y<r[3]-Y0;y++) for(int x=r[0]-X0;x<r[2]-X0;x++) if(x>=0&&y>=0&&x<q.GetLength(0)&&y<q.GetLength(1)) q[x,y]=0;
    int w=q.GetLength(0),h=q.GetLength(1);
    foreach(var c in Comps(q,v=>v==1)){ int mnx=w,mxx=0,mny=h,mxy=0; foreach(int p in c){ int x=p%w,y=p/w; mnx=Math.Min(mnx,x);mxx=Math.Max(mxx,x);mny=Math.Min(mny,y);mxy=Math.Max(mxy,y);}
      bool outside = mxx<(160-X0) || mnx>(445-X0) || mny>(612-Y0) || mxy<(165-Y0);
      if(outside && c.Count<3000) foreach(int p in c) q[p%w,p/w]=0; } }
  // убрать красные стрелки (тонкие красные линии) в состоянии B
  static void RemoveArrows(byte[,] q){
    int w=q.GetLength(0),h=q.GetLength(1); var red=new bool[w,h]; for(int y=0;y<h;y++) for(int x=0;x<w;x++) red[x,y]=q[x,y]==3;
    var keep=new bool[w,h]; for(int y=436;y<472;y++) for(int x=90;x<180;x++) keep[x,y]=red[x,y]; var unk=new bool[w,h]; bool any=false;
    for(int y=0;y<h;y++) for(int x=0;x<w;x++) if(red[x,y]&&!keep[x,y]){ unk[x,y]=true; any=true; }
    { var red2=new bool[w,h]; for(int y=0;y<h;y++) for(int x=0;x<w;x++) red2[x,y]=q[x,y]==3; foreach(var cc in Comps(q,v=>v==3)) if(cc.Count<2500) foreach(int pp in cc){ unk[pp%w,pp/w]=true; any=true; } }
    if(any){ // закрасить ближайшим не-красным
      var tmp=Clone(q); for(int y=0;y<h;y++) for(int x=0;x<w;x++) if(unk[x,y]) tmp[x,y]=255; var u2=new bool[w,h]; for(int y=0;y<h;y++) for(int x=0;x<w;x++) u2[x,y]=unk[x,y]; Inpaint(tmp,u2); for(int y=0;y<h;y++) for(int x=0;x<w;x++) q[x,y]=tmp[x,y]; } }
  // ---------- вывод слоёв ----------
  static string Layers(byte[,] q,bool[,] only){
    int w=q.GetLength(0),h=q.GetLength(1); var sb=new StringBuilder(); Sm.U=4; Sm.Sigma=0.38; Sm.Eps=0.12;
    int[] rank=new int[16]; for(int i=0;i<ORDER.Length;i++) rank[ORDER[i]]=i+1;
    foreach(int c in ORDER){ var m=new bool[w,h]; bool any=false; for(int y=0;y<h;y++) for(int x=0;x<w;x++){ if(only!=null&&!only[x,y]) continue; int cc=q[x,y]; if(cc==0||cc>=16) continue; if(rank[cc]>=rank[c]){ m[x,y]=true; any=true; } }
      if(!any) continue; string d=Sm.Path(m,w,h); if(d.Length==0) continue; sb.Append("<path fill=\"#"+Morph.PAL[c].ToString("x6")+"\" fill-rule=\"evenodd\" d=\""+d+"\"/>\n"); }
    return sb.ToString(); }
  class Part { public int FA=0,FB=0; public bool Other=true; public string Name; public int[] R; public int Dx,Dy; public bool[,] MA,MB; }
  public static void Run(string src,string outFile){
    var b=new Bitmap(src); H=b.Height-Y0;
    A=Morph.Quant(b,X0,Y0,W,H); B=Morph.Quant(b,BX,BY,W,H);
    RemoveLabels(A); RemoveLabels(B); RemoveArrows(B); for(int y=441;y<466;y++) for(int x=120;x<185;x++) B[x,y]=3;
    // детали: прямоугольник (в координатах оригинала) и сдвиг B относительно A
    var parts=new List<Part>{
      new Part{Name="arm",   R=new[]{266,150,316,290}, Dx=0, Dy=-18},
      new Part{Name="plate", R=new[]{218,290,352,400}, Dx=0, Dy=-18},
      new Part{Name="low",   R=new[]{196,450,388,575}, Dx=0, Dy=-16, FA=3, FB=0, Other=false} };
    var uA=new bool[W,H]; var uB=new bool[W,H];
    var zone=new bool[W,H]; { var d=new float[W,H]; for(int y=0;y<H;y++) for(int x=0;x<W;x++) d[x,y]=A[x,y]==1?1e4f:0f;
      for(int y=0;y<H;y++) for(int x=0;x<W;x++){ if(d[x,y]==0) continue; float v=d[x,y]; if(x>0) v=Math.Min(v,d[x-1,y]+1); if(y>0) v=Math.Min(v,d[x,y-1]+1); if(x>0&&y>0) v=Math.Min(v,d[x-1,y-1]+1.414f); if(x<W-1&&y>0) v=Math.Min(v,d[x+1,y-1]+1.414f); d[x,y]=v; }
      for(int y=H-1;y>=0;y--) for(int x=W-1;x>=0;x--){ if(d[x,y]==0) continue; float v=d[x,y]; if(x<W-1) v=Math.Min(v,d[x+1,y]+1); if(y<H-1) v=Math.Min(v,d[x,y+1]+1); if(x<W-1&&y<H-1) v=Math.Min(v,d[x+1,y+1]+1.414f); if(x>0&&y<H-1) v=Math.Min(v,d[x-1,y+1]+1.414f); d[x,y]=v; }
      for(int y=8;y<H-8;y++) for(int x=8;x<W-8;x++){ float v=d[x,y]; if(v<6.5f||(x>250&&x<335)) continue; bool mx=true; for(int j=-5;j<=5&&mx;j++) for(int i=-5;i<=5;i++){ if(d[x+i,y+j]>v){ mx=false; break; } } if(!mx) continue; int rr=(int)v+3; for(int j=-rr;j<=rr;j++) for(int i=-rr;i<=rr;i++) if(i*i+j*j<=rr*rr) zone[x+i,y+j]=true; } }
    var small=new bool[W,H]; foreach(var cc in Comps(A,v=>v==1)) if(cc.Count<600) foreach(int pp in cc) small[pp%W,pp/W]=true;
    foreach(var p in parts){ p.MA=new bool[W,H]; p.MB=new bool[W,H];
      {
        int rx0=p.R[0]-X0+3, ry0=p.R[1]-Y0+3, rx1=p.R[2]-X0-3, ry1=p.R[3]-Y0-3; var st=new Stack<int>();
        Func<int,int,bool> ok=(x,y)=>{ if(x<rx0||y<ry0||x>=rx1||y>=ry1||small[x,y]||(p.Name=="plate"&&zone[x,y])) return false; int a=A[x,y]; if(a==0||a==3||a==2) return false; int xb=x+p.Dx,yb=y+p.Dy; if(xb<0||yb<0||xb>=W||yb>=H) return false; return A[x,y]==B[xb,yb]; };
        for(int y=ry0;y<ry1;y++) for(int x=rx0;x<rx1;x++) if(ok(x,y)&&A[x,y]!=B[x,y]){ p.MA[x,y]=true; st.Push(y*W+x); }
        while(st.Count>0){ int q=st.Pop(); int qx=q%W,qy=q/W; for(int j=-1;j<=1;j++) for(int i=-1;i<=1;i++){ int nx=qx+i,ny=qy+j; if(nx<0||ny<0||nx>=W||ny>=H||p.MA[nx,ny]||!ok(nx,ny)) continue; p.MA[nx,ny]=true; st.Push(ny*W+nx); } }
        for(int it=0;it<2;it++){ var gr=(bool[,])p.MA.Clone(); for(int y=ry0;y<ry1;y++) for(int x=rx0;x<rx1;x++){ if(p.MA[x,y]) continue; int a=A[x,y]; if(!(a==5||a==6||a==7||a==8||a==9||a==10)) continue; bool nb=false; for(int j=-1;j<=1&&!nb;j++) for(int i=-1;i<=1;i++) if(p.MA[x+i,y+j]){ nb=true; break; } if(nb) gr[x,y]=true; } p.MA=gr; }
        for(int y=0;y<H;y++) for(int x=0;x<W;x++) if(p.MA[x,y]){ p.MB[x+p.Dx,y+p.Dy]=true; uA[x,y]=true; uB[x+p.Dx,y+p.Dy]=true; } }
      Console.WriteLine(p.Name+" pixels "+Enumerable.Range(0,W).Sum(x=>Enumerable.Range(0,H).Count(y=>p.MA[x,y]))); }
    // рычаг: вращение A -> B вокруг шарнира
    double pvx=395-X0, pvy=160-Y0; double bestAng=0; int bestSc=-1; var LA=new bool[W,H]; var LB=new bool[W,H];
    { var g=new bool[W,H]; for(int y=150-Y0;y<176-Y0;y++) for(int x=235;x<402;x++) if(A[x,y]==2) g[x,y]=true; var d=Dilate(g,4); for(int y=150-Y0;y<176-Y0;y++) for(int x=235;x<404;x++) if(d[x,y]&&(A[x,y]==1||A[x,y]==2)) LA[x,y]=true; }
    { var g=new bool[W,H]; for(int y=118-Y0;y<176-Y0;y++) for(int x=225;x<402;x++) if(B[x,y]==2) g[x,y]=true; var d=Dilate(g,4); for(int y=118-Y0;y<176-Y0;y++) for(int x=225;x<404;x++) if(d[x,y]&&(B[x,y]==1||B[x,y]==2)) LB[x,y]=true; }
    for(double ang=0;ang<=16;ang+=0.5){ int sc=0; double ca=Math.Cos(ang*Math.PI/180), sa=Math.Sin(ang*Math.PI/180);
      for(int y=0;y<H;y++) for(int x=0;x<W;x++){ if(!LA[x,y]) continue; double dx=x-pvx, dy=y-pvy; int xr=(int)Math.Round(pvx+dx*ca-dy*sa), yr=(int)Math.Round(pvy+dx*sa+dy*ca); if(xr<0||yr<0||xr>=W||yr>=H) continue; if(LB[xr,yr]) sc++; }
      if(sc>bestSc){bestSc=sc;bestAng=ang;} }
    Console.WriteLine("lever angle "+bestAng+" score "+bestSc);
    for(int y=0;y<H;y++) for(int x=0;x<W;x++){ uA[x,y]|=LA[x,y]; uB[x,y]|=LB[x,y]; }
    // статические слои
    var sA=Clone(A); var sB=Clone(B);
    Action<byte[,],bool[,],byte[,],bool[,],int,bool> fill=(st,mk,oraw,omask,def,useOther)=>{ for(int y=0;y<H;y++) for(int x=0;x<W;x++) if(mk[x,y]) st[x,y]=(byte)((useOther&&!omask[x,y])?oraw[x,y]:def); };
    foreach(var p in parts){ fill(sA,p.MA,B,uB,p.FA,p.Other); fill(sB,p.MB,A,uA,p.FB,p.Other); }
    fill(sA,LA,B,uB,4,true); fill(sB,LB,A,uA,4,true);
    var sb=new StringBuilder();
    foreach(var st2 in new[]{sA,sB}) foreach(var cc in Comps(st2,v=>v!=0)){ if(cc.Count<700 && cc.Any(pp=>{ int v=st2[pp%W,pp/W]; return v!=1; })){ foreach(int pp in cc) st2[pp%W,pp/W]=0; } }
    sb.Append("<g id=\"SA\">\n"+Layers(sA,null)+"</g>\n");
    sb.Append("<g id=\"SB\" opacity=\"0\"><rect x=\"0\" y=\"0\" width=\""+W+"\" height=\""+H+"\" fill=\"#fff\"/>\n"+Layers(sB,null)+"</g>\n");
    var names=parts.Select(p=>p.Name).ToList(); names.Add("lever");
    foreach(var nm in names){ bool lv=nm=="lever"; var ma=lv?LA:parts.First(p=>p.Name==nm).MA; sb.Append("<g class=\"pa\" data-p=\""+nm+"\">\n"+Layers(A,ma)+"</g>\n"); }
    foreach(var nm in names){ bool lv=nm=="lever"; var mb=lv?LB:parts.First(p=>p.Name==nm).MB; sb.Append("<g class=\"pb\" data-p=\""+nm+"\" opacity=\"0\">\n"+Layers(B,mb)+"</g>\n"); }
    File.WriteAllText(outFile,sb.ToString());
    File.WriteAllText(Path.ChangeExtension(outFile,".info"),"angle="+bestAng.ToString(CultureInfo.InvariantCulture)+"\nW="+W+" H="+H+"\n");
    Morph.Save(sA,Path.ChangeExtension(outFile,".sA.png")); Morph.Save(sB,Path.ChangeExtension(outFile,".sB.png"));
    Console.WriteLine("ok "+sb.Length);
  }
}
