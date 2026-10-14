using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
public static class Seg {
  static float[] Box(float[] a,int W,int H,int r){
    var t=new float[W*H]; var o=new float[W*H];
    for(int y=0;y<H;y++){ double s=0; int cnt=0; for(int x=0;x<Math.Min(r,W);x++){ s+=a[y*W+x]; cnt++; } for(int x=0;x<W;x++){ int p=x+r, q=x-r-1; if(p<W){ s+=a[y*W+p]; cnt++; } if(q>=0){ s-=a[y*W+q]; cnt--; } t[y*W+x]=(float)(s/cnt); } }
    for(int x=0;x<W;x++){ double s=0; int cnt=0; for(int y=0;y<Math.Min(r,H);y++){ s+=t[y*W+x]; cnt++; } for(int y=0;y<H;y++){ int p=y+r, q=y-r-1; if(p<H){ s+=t[p*W+x]; cnt++; } if(q>=0){ s-=t[q*W+x]; cnt--; } o[y*W+x]=(float)(s/cnt); } }
    return o;
  }
  static float[] MaxF(float[] a,int W,int H,int r){
    var t=new float[W*H]; var o=new float[W*H];
    for(int y=0;y<H;y++) for(int x=0;x<W;x++){ float m=0; int x0=Math.Max(0,x-r),x1=Math.Min(W-1,x+r); for(int i=x0;i<=x1;i++) if(a[y*W+i]>m) m=a[y*W+i]; t[y*W+x]=m; }
    for(int y=0;y<H;y++) for(int x=0;x<W;x++){ float m=0; int y0=Math.Max(0,y-r),y1=Math.Min(H-1,y+r); for(int j=y0;j<=y1;j++) if(t[j*W+x]>m) m=t[j*W+x]; o[y*W+x]=m; }
    return o;
  }
  public static double Sc=1.0;
  public static readonly string Names="wkRrOYGBP"; // 0 белый,1 чёрный,2 тёмно-красный/бордовый,3 красный,4 оранжевый,5 жёлтый,6 зелёный,7 голубой,8 серо-лиловый
  public static byte[] Classify(string path,out int W,out int H,out float[] nr,out float[] ng,out float[] nb){
    using(var bmp=new Bitmap(path)){
      W=bmp.Width; H=bmp.Height; var bd=bmp.LockBits(new Rectangle(0,0,W,H),ImageLockMode.ReadOnly,PixelFormat.Format24bppRgb);
      int stride=bd.Stride; var buf=new byte[stride*H]; System.Runtime.InteropServices.Marshal.Copy(bd.Scan0,buf,0,buf.Length); bmp.UnlockBits(bd);
      var ch=new float[3][]; for(int c=0;c<3;c++){ ch[c]=new float[W*H]; for(int y=0;y<H;y++) for(int x=0;x<W;x++) ch[c][y*W+x]=buf[y*stride+x*3+(2-c)]; } // c=0 R
      var bg=new float[3][]; for(int c=0;c<3;c++){ bg[c]=Box(MaxF(ch[c],W,H,(int)(12*Sc)),W,H,(int)(45*Sc)); }
      nr=new float[W*H]; ng=new float[W*H]; nb=new float[W*H]; var cls=new byte[W*H];
      for(int i=0;i<W*H;i++){
        double x1k=(i%W)/(W/1600.0), y1k=(i/W)/(W/1600.0); float r=Math.Min(1,ch[0][i]/bg[0][i]), g=Math.Min(1,ch[1][i]/bg[1][i]), b=Math.Min(1,ch[2][i]/bg[2][i]); nr[i]=r; ng[i]=g; nb[i]=b;
        float v=Math.Max(r,Math.Max(g,b)), mn=Math.Min(r,Math.Min(g,b)); float s=v<1e-4?0:(v-mn)/v;
        double h=0; if(v-mn>1e-4){ if(v==r) h=60*(((g-b)/(v-mn))%6); else if(v==g) h=60*((b-r)/(v-mn)+2); else h=60*((r-g)/(v-mn)+4); if(h<0) h+=360; }
        byte k=0;
        bool inTab=(x1k>=1244&&x1k<=1576&&y1k>=126&&y1k<=486); if(v>(inTab?0.88:0.80)&&s<(inTab?0.2:0.16)) k=0;
        else if(v<0.36) k=1;
        else if(s>=0.30){
          if(h>=335||h<14){ k= (v>0.62&&s>0.6)?(byte)3:(byte)2; }
          else if(h<35){ k=4; }
          else if(h<75){ k=5; }
          else if(h<175){ k=6; }
          else if(h<260){ k=(s>0.14&&v>0.6)?(byte)7:(byte)1; }
          else k=(v>0.5)?(byte)8:(byte)2;
        }
        else if(s>=0.12&&v>0.62){ if(h>=170&&h<260) k=7; else if(h>=260&&h<345) k=8; else k=0; }
        else if(v<0.62) k=1;
        else k=(inTab&&v<0.86)?(byte)1:(byte)0;
        cls[i]=k; }
      var vv=new float[W*H]; for(int i=0;i<W*H;i++) vv[i]=Math.Max(nr[i],Math.Max(ng[i],nb[i])); var vm=MaxF(vv,W,H,(int)(4*Sc));
      for(int i=0;i<W*H;i++){ if(cls[i]>=2&&cls[i]<=8&&vv[i]<0.78f*vm[i]&&vv[i]<0.5f) cls[i]=1; }
      return cls; }
  }
  public static void Preview(string path,string outp){
    int W,H; float[] a,b,c; var cls=Classify(path,out W,out H,out a,out b,out c);
    var pal=new Color[]{Color.White,Color.Black,Color.Maroon,Color.Red,Color.Orange,Color.Gold,Color.Green,Color.DeepSkyBlue,Color.Plum};
    var o=new Bitmap(W,H,PixelFormat.Format24bppRgb); var cnt=new long[9];
    for(int y=0;y<H;y++) for(int x=0;x<W;x++){ o.SetPixel(x,y,pal[cls[y*W+x]]); cnt[cls[y*W+x]]++; }
    o.Save(outp,ImageFormat.Png); Console.WriteLine(string.Join(" ",cnt));
  }
}
