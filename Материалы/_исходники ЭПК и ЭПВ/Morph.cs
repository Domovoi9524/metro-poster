using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
public static class Morph {
  public static readonly int[] PAL={0xffffff,0x000000,0xc3c3c3,0xed1c24,0xefe4b0,0x00a2e8,0xff8000,0xfff200,0x7f7f7f,0x880015,0x22b14c};
  public static byte[,] Quant(Bitmap b,int x0,int y0,int w,int h){
    var q=new byte[w,h];
    for(int y=0;y<h;y++) for(int x=0;x<w;x++){ int xx=x0+x,yy=y0+y; if(xx<0||yy<0||xx>=b.Width||yy>=b.Height){ q[x,y]=0; continue; } var c=b.GetPixel(xx,yy); int bi=0; double bd=1e18; for(int i=0;i<PAL.Length;i++){ int p=PAL[i]; double dr=c.R-((p>>16)&255), dg=c.G-((p>>8)&255), db=c.B-(p&255); double d=dr*dr+dg*dg+db*db; if(d<bd){bd=d;bi=i;} } q[x,y]=(byte)bi; }
    return q; }
  public static void Save(byte[,] q,string f){ int w=q.GetLength(0),h=q.GetLength(1); var o=new Bitmap(w,h); for(int y=0;y<h;y++) for(int x=0;x<w;x++) o.SetPixel(x,y,Color.FromArgb(PAL[q[x,y]]|unchecked((int)0xff000000))); o.Save(f); }
  public static void Run(string src,string dir){
    var b=new Bitmap(src);
    var A=Quant(b,0,150,562,b.Height-150); var B=Quant(b,562,150,b.Width-562,b.Height-150);
    Console.WriteLine("A "+A.GetLength(0)+"x"+A.GetLength(1));
    // смещение B относительно A по чёрному контуру
    int bestx=0,besty=0; long best=-1;
    for(int dy=-12;dy<=12;dy++) for(int dx=-30;dx<=30;dx++){ long s=0; for(int y=0;y<A.GetLength(1);y+=1) for(int x=0;x<A.GetLength(0);x+=1){ int xb=x+dx,yb=y+dy; if(xb<0||yb<0||xb>=B.GetLength(0)||yb>=B.GetLength(1)) continue; if(A[x,y]==1&&B[xb,yb]==1) s++; } if(s>best){best=s;bestx=dx;besty=dy;} }
    Console.WriteLine("offset B-A = "+bestx+","+besty+" match "+best);
    Save(A,dir+"/qA.png"); Save(B,dir+"/qB.png");
  }
}
