using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
public static class Rect {
  public static float[] Gray(string path,int ds,out int W,out int H){
    using(var bmp=new Bitmap(path)){
      W=bmp.Width/ds; H=bmp.Height/ds; var g=new float[W*H];
      var bd=bmp.LockBits(new Rectangle(0,0,bmp.Width,bmp.Height),ImageLockMode.ReadOnly,PixelFormat.Format24bppRgb);
      int stride=bd.Stride; var buf=new byte[stride*bmp.Height]; System.Runtime.InteropServices.Marshal.Copy(bd.Scan0,buf,0,buf.Length); bmp.UnlockBits(bd);
      for(int y=0;y<H;y++) for(int x=0;x<W;x++){ float s=0; for(int j=0;j<ds;j++) for(int i=0;i<ds;i++){ int o=(y*ds+j)*stride+(x*ds+i)*3; s+=0.114f*buf[o]+0.587f*buf[o+1]+0.299f*buf[o+2]; } g[y*W+x]=s/(ds*ds); }
      return g; }
  }
  // тёмные линии: серость заметно ниже локального фона (большой box-blur)
  public static bool[] Dark(float[] g,int W,int H,int r){
    var bg=new float[W*H]; var tmp=new float[W*H];
    for(int y=0;y<H;y++){ double s=0; int cnt=0; for(int x=0;x<Math.Min(r,W);x++){ s+=g[y*W+x]; cnt++; } for(int x=0;x<W;x++){ int a=x+r, b=x-r-1; if(a<W){ s+=g[y*W+a]; cnt++; } if(b>=0){ s-=g[y*W+b]; cnt--; } tmp[y*W+x]=(float)(s/cnt); } }
    for(int x=0;x<W;x++){ double s=0; int cnt=0; for(int y=0;y<Math.Min(r,H);y++){ s+=tmp[y*W+x]; cnt++; } for(int y=0;y<H;y++){ int a=y+r, b=y-r-1; if(a<H){ s+=tmp[a*W+x]; cnt++; } if(b>=0){ s-=tmp[b*W+x]; cnt--; } bg[y*W+x]=(float)(s/cnt); } }
    var d=new bool[W*H]; for(int i=0;i<W*H;i++) d[i]=g[i]<bg[i]*0.62f; return d;
  }
  public static void Run(string path){
    int W,H; var g=Gray(path,4,out W,out H); var d=Dark(g,W,H,25);
    // область схемы (предпросмотр 1600 px -> масштаб W/1600*... ) берём центр
    double sc=W/1600.0; int x0=(int)(260*sc),x1=(int)(1480*sc),y0=(int)(300*sc),y1=(int)(1100*sc);
    double bestA=0,bestS=-1;
    for(double a=-1.5;a<=1.5001;a+=0.05){
      double ca=Math.Cos(a*Math.PI/180), sa=Math.Sin(a*Math.PI/180); var rows=new double[H*2];
      for(int y=y0;y<y1;y+=1) for(int x=x0;x<x1;x+=1){ if(!d[y*W+x]) continue; double yy=(x-W/2)*sa*-1+(y-H/2)*ca+H/2+H/2; int iy=(int)yy; if(iy>=0&&iy<rows.Length) rows[iy]++; }
      double s=0; foreach(var v in rows) s+=v*v; if(s>bestS){bestS=s;bestA=a;} }
    Console.WriteLine("rot="+bestA);
    // сдвиг по вертикальным линиям после поворота
    double ca2=Math.Cos(bestA*Math.PI/180), sa2=Math.Sin(bestA*Math.PI/180); double bestSh=0, bestS2=-1;
    for(double sh=-2.0;sh<=2.0001;sh+=0.05){
      double t=Math.Tan(sh*Math.PI/180); var cols=new double[W*2];
      for(int y=y0;y<y1;y++) for(int x=x0;x<x1;x++){ if(!d[y*W+x]) continue; double xr=(x-W/2)*ca2+(y-H/2)*sa2+W/2, yr=-(x-W/2)*sa2+(y-H/2)*ca2+H/2; double xx=xr-(yr-H/2)*t+W/2; int ix=(int)xx; if(ix>=0&&ix<cols.Length) cols[ix]++; }
      double s=0; foreach(var v in cols) s+=v*v; if(s>bestS2){bestS2=s;bestSh=sh;} }
    Console.WriteLine("shear="+bestSh);
  }
}
