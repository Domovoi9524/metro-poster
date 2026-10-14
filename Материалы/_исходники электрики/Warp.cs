using System;
using System.Drawing;
using System.Drawing.Imaging;
public static class Warp {
  // out: изображение, выпрямленное поворотом rot° и сдвигом shear°, область u0..u1 x v0..v1 (координаты «предпросмотра» 1600 px), S пикселей на единицу
  public static void Run(string src,string dst,double rot,double shear,double u0,double v0,double u1,double v1,double S,double pv){
    using(var bmp=new Bitmap(src)){
      int W=bmp.Width,H=bmp.Height; var bd=bmp.LockBits(new Rectangle(0,0,W,H),ImageLockMode.ReadOnly,PixelFormat.Format24bppRgb);
      int stride=bd.Stride; var buf=new byte[stride*H]; System.Runtime.InteropServices.Marshal.Copy(bd.Scan0,buf,0,buf.Length); bmp.UnlockBits(bd);
      double a=rot*Math.PI/180, ca=Math.Cos(a), sa=Math.Sin(a), t=Math.Tan(shear*Math.PI/180);
      int ow=(int)((u1-u0)*S), oh=(int)((v1-v0)*S); var o=new Bitmap(ow,oh,PixelFormat.Format24bppRgb);
      var od=o.LockBits(new Rectangle(0,0,ow,oh),ImageLockMode.WriteOnly,PixelFormat.Format24bppRgb); var ob=new byte[od.Stride*oh];
      double cx=W/2.0, cy=H/2.0, sc=W/pv/1.0*0+ (W/1600.0);
      for(int j=0;j<oh;j++) for(int i=0;i<ow;i++){
        double X=(u0+i/S)*sc, Y=(v0+j/S)*sc;            // выпрямленные координаты в пикселях исходника
        double xr=X+(Y-cy)*t, yr=Y;                      // обратный сдвиг
        double dx=(xr-cx)*ca-(yr-cy)*sa, dy=(xr-cx)*sa+(yr-cy)*ca; // обратный поворот
        double sx=dx+cx, sy=dy+cy;
        if(sx<0||sy<0||sx>=W-1||sy>=H-1){ continue; }
        int x0=(int)sx,y0=(int)sy; double fx=sx-x0, fy=sy-y0;
        for(int c=0;c<3;c++){
          double v00=buf[y0*stride+x0*3+c], v10=buf[y0*stride+(x0+1)*3+c], v01=buf[(y0+1)*stride+x0*3+c], v11=buf[(y0+1)*stride+(x0+1)*3+c];
          double v=v00*(1-fx)*(1-fy)+v10*fx*(1-fy)+v01*(1-fx)*fy+v11*fx*fy; ob[j*od.Stride+i*3+c]=(byte)Math.Round(v); }
      }
      System.Runtime.InteropServices.Marshal.Copy(ob,0,od.Scan0,ob.Length); o.UnlockBits(od); o.Save(dst,ImageFormat.Png); }
  }
}
