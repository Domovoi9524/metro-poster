using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;
// Выпрямление: находит длинные горизонтальные/вертикальные линии на cls.bin (rect, 2620x1930, 1.6375 px на d-единицу),
// подгоняет полиномиальное поле смещения (в d-единицах), делающее линии прямыми, и перерисовывает исходное фото с этим полем.
public static class Straight {
  const int RW=2620, RH=1930; const double RK=2620/1600.0;
  const int DEG=4;
  static List<double[]> Terms(){ var t=new List<double[]>(); for(int i=0;i<=DEG;i++) for(int j=0;i+j<=DEG;j++) t.Add(new double[]{i,j}); return t; }
  static readonly List<double[]> T=Terms();
  static double[] Phi(double x,double y){ double xn=(x-800)/800.0, yn=(y-589)/589.0; var xp=new double[DEG+1]; var yp=new double[DEG+1]; xp[0]=1; yp[0]=1; for(int i=1;i<=DEG;i++){ xp[i]=xp[i-1]*xn; yp[i]=yp[i-1]*yn; } var r=new double[T.Count]; for(int k=0;k<T.Count;k++) r[k]=xp[(int)T[k][0]]*yp[(int)T[k][1]]; return r; }
  class Line { public List<double[]> P=new List<double[]>(); public bool Alive=true; }
  // горизонтальные линии: swap=false; вертикальные: swap=true (меняем оси)
  static List<Line> Detect(byte[] cls,bool swap,int minRun,int minLen){
    int W=swap?RH:RW, H=swap?RW:RH;
    Func<int,int,bool> on=(x,y)=> swap? cls[x*RW+y]!=0 : cls[y*RW+x]!=0;
    var lines=new List<List<int[]>>(); var active=new List<List<int[]>>(); // run: y,x0,x1
    for(int y=0;y<H;y++){
      var runs=new List<int[]>(); int x=0; while(x<W){ if(on(x,y)){ int s=x; while(x<W&&on(x,y)) x++; if(x-s>=minRun) runs.Add(new int[]{y,s,x-1}); } else x++; }
      var next=new List<List<int[]>>();
      foreach(var r in runs){ List<int[]> best=null; double bo=0;
        foreach(var a in active){ var l=a[a.Count-1]; int ov=Math.Min(l[2],r[2])-Math.Max(l[1],r[1]); int mn=Math.Min(l[2]-l[1],r[2]-r[1]); if(ov>0.5*mn&&ov>bo){ bo=ov; best=a; } }
        if(best!=null&&!next.Contains(best)){ best.Add(r); next.Add(best); } else { var n=new List<int[]>(); n.Add(r); lines.Add(n); next.Add(n); } }
      active=next; }
    var res=new List<Line>();
    foreach(var ln in lines){ int mnx=int.MaxValue,mxx=-1; foreach(var r in ln){ mnx=Math.Min(mnx,r[1]); mxx=Math.Max(mxx,r[2]); }
      if(mxx-mnx<minLen) continue;
      // толщина не должна быть большой (исключаем сплошные заливки)
      if(ln.Count>40) continue;
      var L=new Line(); for(int cx=mnx;cx<=mxx;cx+=6){ double s=0; int n=0; foreach(var r in ln) if(cx>=r[1]&&cx<=r[2]){ s+=r[0]; n++; } if(n>0){ double a=cx/RK, b=(s/n+0.5)/RK; L.P.Add(swap? new double[]{b,a}: new double[]{a,b}); } }
      if(L.P.Count>=12) res.Add(L); }
    return res;
  }
  static double[] Solve(double[,] A,double[] b){ int n=b.Length; var M=(double[,])A.Clone(); var v=(double[])b.Clone();
    for(int i=0;i<n;i++){ int p=i; for(int r=i+1;r<n;r++) if(Math.Abs(M[r,i])>Math.Abs(M[p,i])) p=r; if(p!=i){ for(int c=0;c<n;c++){ var t=M[i,c];M[i,c]=M[p,c];M[p,c]=t;} var tv=v[i];v[i]=v[p];v[p]=tv; }
      for(int r=i+1;r<n;r++){ double f=M[r,i]/M[i,i]; if(f==0) continue; for(int c=i;c<n;c++) M[r,c]-=f*M[i,c]; v[r]-=f*v[i]; } }
    var x=new double[n]; for(int i=n-1;i>=0;i--){ double s=v[i]; for(int c=i+1;c<n;c++) s-=M[i,c]*x[c]; x[i]=s/M[i,i]; } return x; }
  // подгонка поля: ось vert=false -> dy по горизонтальным линиям (y), vert=true -> dx по вертикальным (x)
  static double[] Fit(List<Line> ls,bool vert,double lam,out double rmsBefore,out double rmsAfter){
    int m=T.Count; var A=new double[m,m]; var b=new double[m];
    double sb=0,sa=0; long cnt=0; int it=0;
    foreach(var L in ls){ if(!L.Alive) continue; int n=L.P.Count; var mp=new double[m]; double mv=0; foreach(var p in L.P){ var f=Phi(p[0],p[1]); for(int k=0;k<m;k++) mp[k]+=f[k]/n; mv+=p[vert?0:1]/n; }
      foreach(var p in L.P){ var f=Phi(p[0],p[1]); var c=new double[m]; for(int k=0;k<m;k++) c[k]=f[k]-mp[k]; double t=-(p[vert?0:1]-mv);
        for(int i=0;i<m;i++){ b[i]+=c[i]*t; for(int j=0;j<m;j++) A[i,j]+=c[i]*c[j]; } } it++; }
    double tr=0; for(int i=0;i<m;i++) tr+=A[i,i]; for(int i=0;i<m;i++) A[i,i]+=lam*tr/m*(1+T[i][0]+T[i][1]) + ((vert? T[i][1]<1 : T[i][0]<1)?1e12:0);
    var co=Solve(A,b);
    foreach(var L in ls){ if(!L.Alive) continue; int n=L.P.Count; double mv=0,ma=0; var after=new double[n]; int q=0;
      foreach(var p in L.P){ var f=Phi(p[0],p[1]); double d=0; for(int k=0;k<m;k++) d+=co[k]*f[k]; after[q++]=p[vert?0:1]+d; mv+=p[vert?0:1]/n; }
      for(int i=0;i<n;i++) ma+=after[i]/n; q=0; foreach(var p in L.P){ double e0=p[vert?0:1]-mv, e1=after[q++]-ma; sb+=e0*e0; sa+=e1*e1; cnt++; } }
    rmsBefore=Math.Sqrt(sb/Math.Max(1,cnt)); rmsAfter=Math.Sqrt(sa/Math.Max(1,cnt)); return co; }
  static void Prune(List<Line> ls,double[] co,bool vert,double thr){
    int m=T.Count; foreach(var L in ls){ int n=L.P.Count; double ma=0; var af=new double[n]; int q=0;
      foreach(var p in L.P){ var f=Phi(p[0],p[1]); double d=0; for(int k=0;k<m;k++) d+=co[k]*f[k]; af[q++]=p[vert?0:1]+d; }
      for(int i=0;i<n;i++) ma+=af[i]/n; double s=0; for(int i=0;i<n;i++) s+=(af[i]-ma)*(af[i]-ma); L.Alive = Math.Sqrt(s/n)<thr; } }
  public static double[] CoX, CoY;
  public static void Disp(double x,double y,out double dx,out double dy){ var f=Phi(x,y); dx=0; dy=0; for(int k=0;k<T.Count;k++){ dx+=CoX[k]*f[k]; dy+=CoY[k]*f[k]; } }
  public static void FitAll(string dir){
    var cls=File.ReadAllBytes(dir+"/cls.bin");
    var H=Detect(cls,false,70,160); var V=Detect(cls,true,70,160); Console.WriteLine("lines H="+H.Count+" V="+V.Count);
    double a,b;
    for(int pass=0;pass<5;pass++){
      CoY=Fit(H,false,1e-4,out a,out b); Console.WriteLine("H pass "+pass+" rms "+a.ToString("0.000")+" -> "+b.ToString("0.000")); if(pass<4) Prune(H,CoY,false,Math.Max(0.5,b*2.2));
      CoX=Fit(V,true,1e-4,out a,out b); Console.WriteLine("V pass "+pass+" rms "+a.ToString("0.000")+" -> "+b.ToString("0.000")); if(pass<4) Prune(V,CoX,true,Math.Max(0.5,b*2.2)); }
    int ah=0,av=0; foreach(var l in H) if(l.Alive) ah++; foreach(var l in V) if(l.Alive) av++; Console.WriteLine("alive H="+ah+" V="+av);
    var sb=new StringBuilder(); sb.AppendLine(string.Join(" ",Array.ConvertAll(CoX,v=>v.ToString("R",CultureInfo.InvariantCulture)))); sb.AppendLine(string.Join(" ",Array.ConvertAll(CoY,v=>v.ToString("R",CultureInfo.InvariantCulture))));
    File.WriteAllText(dir+"/field.txt",sb.ToString());
    double mx=0; for(double x=60;x<=1560;x+=50) for(double y=100;y<=1170;y+=50){ double dx,dy; Disp(x,y,out dx,out dy); mx=Math.Max(mx,Math.Max(Math.Abs(dx),Math.Abs(dy))); } Console.WriteLine("max disp d-units "+mx.ToString("0.0"));
    // отчёт: выжившие линии для проверки
  }
  public static void Dump(){ for(int y=100;y<=1100;y+=200){ var sb=new StringBuilder(); for(int x=60;x<=1500;x+=288){ double dx,dy; Disp(x,y,out dx,out dy); sb.Append("("+dx.ToString("0.0")+","+dy.ToString("0.0")+") "); } Console.WriteLine(sb); } }
  public static void Load(string dir){ var ls=File.ReadAllLines(dir+"/field.txt"); CoX=Array.ConvertAll(ls[0].Split(' '),s=>double.Parse(s,CultureInfo.InvariantCulture)); CoY=Array.ConvertAll(ls[1].Split(' '),s=>double.Parse(s,CultureInfo.InvariantCulture)); }
  // new d (n) -> old d (o):  n = o + disp(o)
  public static void Inv(double nx,double ny,out double ox,out double oy){ ox=nx; oy=ny; for(int i=0;i<12;i++){ double dx,dy; Disp(ox,oy,out dx,out dy); ox=nx-dx; oy=ny-dy; } }
  // рендер: исходное фото (heic.png) -> новый кадр в d-единицах, Kn пикселей на единицу
  public static void Render(string dir,string src,string dst,double Kn,double rot,double shear){
    Load(dir);
    using(var bmp=new Bitmap(src)){
      int W=bmp.Width,H=bmp.Height; var bd=bmp.LockBits(new Rectangle(0,0,W,H),ImageLockMode.ReadOnly,PixelFormat.Format24bppRgb);
      int stride=bd.Stride; var buf=new byte[stride*H]; System.Runtime.InteropServices.Marshal.Copy(bd.Scan0,buf,0,buf.Length); bmp.UnlockBits(bd);
      double a=rot*Math.PI/180, ca=Math.Cos(a), sa=Math.Sin(a), t=Math.Tan(shear*Math.PI/180);
      int ow=(int)Math.Round(1600*Kn), oh=(int)Math.Round(RH/RK*Kn); var o=new Bitmap(ow,oh,PixelFormat.Format24bppRgb);
      var od=o.LockBits(new Rectangle(0,0,ow,oh),ImageLockMode.WriteOnly,PixelFormat.Format24bppRgb); var ob=new byte[od.Stride*oh];
      double cx=W/2.0, cy=H/2.0, sc=W/1600.0;
      for(int j=0;j<oh;j++) for(int i=0;i<ow;i++){
        double ox,oy; Inv(i/Kn,j/Kn,out ox,out oy);           // старые d-единицы
        double u=215+ox*RK/2.0, v=175+oy*RK/2.0;               // координаты «предпросмотра»
        double X=u*sc, Y=v*sc; double xr=X+(Y-cy)*t, yr=Y;
        double dx2=(xr-cx)*ca-(yr-cy)*sa, dy2=(xr-cx)*sa+(yr-cy)*ca; double sx=dx2+cx, sy=dy2+cy;
        if(sx<0||sy<0||sx>=W-1||sy>=H-1) continue;
        int x0=(int)sx,y0=(int)sy; double fx=sx-x0, fy=sy-y0;
        for(int c=0;c<3;c++){ double v00=buf[y0*stride+x0*3+c], v10=buf[y0*stride+(x0+1)*3+c], v01=buf[(y0+1)*stride+x0*3+c], v11=buf[(y0+1)*stride+(x0+1)*3+c];
          ob[j*od.Stride+i*3+c]=(byte)Math.Round(v00*(1-fx)*(1-fy)+v10*fx*(1-fy)+v01*(1-fx)*fy+v11*fx*fy); }
      }
      System.Runtime.InteropServices.Marshal.Copy(ob,0,od.Scan0,ob.Length); o.UnlockBits(od); o.Save(dst,ImageFormat.Png); }
  }
}
