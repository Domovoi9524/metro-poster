using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
public static class Paths {
  static List<double[]> Rdp(List<int[]> pts,double eps){
    if(pts.Count<3){ var r=new List<double[]>(); foreach(var p in pts) r.Add(new double[]{p[0]+0.5,p[1]+0.5}); return r; }
    var keep=new bool[pts.Count]; keep[0]=true; keep[pts.Count-1]=true; var st=new Stack<int[]>(); st.Push(new int[]{0,pts.Count-1});
    while(st.Count>0){ var s=st.Pop(); int a=s[0], b=s[1]; double maxd=0; int mi=-1;
      for(int i=a+1;i<b;i++){ double dx=pts[b][0]-pts[a][0], dy=pts[b][1]-pts[a][1]; double len=Math.Sqrt(dx*dx+dy*dy); double d = len==0?Math.Sqrt(Math.Pow(pts[i][0]-pts[a][0],2)+Math.Pow(pts[i][1]-pts[a][1],2)) : Math.Abs(dy*(pts[i][0]-pts[a][0])-dx*(pts[i][1]-pts[a][1]))/len; if(d>maxd){maxd=d;mi=i;} }
      if(maxd>eps&&mi>0){ keep[mi]=true; st.Push(new int[]{a,mi}); st.Push(new int[]{mi,b}); } }
    var res=new List<double[]>(); for(int i=0;i<pts.Count;i++) if(keep[i]) res.Add(new double[]{pts[i][0]+0.5,pts[i][1]+0.5}); return res;
  }
  public static void Run(string dir,int w,int h){
    string[] kl=File.ReadAllLines(dir+"/kmap.txt"); string[] ql=File.ReadAllLines(dir+"/q2map.txt");
    var outSb=new StringBuilder();
    foreach(var ln in File.ReadAllLines(dir+"/paths_spec.txt")){
      if(ln.Trim().Length==0||ln.StartsWith("#")) continue; var p=ln.Split('|'); string name=p[0]; string mode=p[1]; string keys=p[2]; bool white=p[3]=="1";
      var wps=new List<int[]>(); foreach(var t in p[4].Split(';')){ var xy=t.Split(','); wps.Add(new int[]{int.Parse(xy[0]),int.Parse(xy[1])}); }
      if(mode=="F"){ var sb=new StringBuilder(); for(int i=0;i<wps.Count;i++) sb.Append((i==0?"M":"L")+(wps[i][0]+0.5).ToString("0.#",System.Globalization.CultureInfo.InvariantCulture)+" "+(wps[i][1]+0.5).ToString("0.#",System.Globalization.CultureInfo.InvariantCulture)+" "); outSb.Append(name+"|"+sb.ToString().Trim()+"\n"); continue; }
      var allowedKeys=new HashSet<char>(); foreach(var k in keys.Split(',')){ switch(k){case "ar":allowedKeys.Add('A');break;case "kdr":allowedKeys.Add('D');break;case "kl":allowedKeys.Add('L');break;case "mk":allowedKeys.Add('M');break;case "nv":allowedKeys.Add('N');break;case "p1":allowedKeys.Add('P');break;case "rk":allowedKeys.Add('R');break;case "tc":allowedKeys.Add('T');break;case "tm":allowedKeys.Add('X');break;case "zr":allowedKeys.Add('Z');break;} }
      var ok=new bool[w,h]; for(int y=0;y<h;y++) for(int x=0;x<w;x++){ char kc=kl[y][x]; bool a=allowedKeys.Contains(kc); if(!a&&white&&ql[y][x]=='a'&&kc=='.') a=true; ok[x,y]=a; }
      var pass=new bool[w,h]; for(int y=0;y<h;y++) for(int x=0;x<w;x++) pass[x,y]=true;
      var dist=new int[w,h]; var qq=new Queue<int>(); for(int y=0;y<h;y++) for(int x=0;x<w;x++){ if(!ok[x,y]){dist[x,y]=0;qq.Enqueue(x*10000+y);} else dist[x,y]=1000; }
      while(qq.Count>0){ int v=qq.Dequeue(); int cx=v/10000, cy=v%10000; int[] dx={1,-1,0,0}, dy={0,0,1,-1}; for(int k=0;k<4;k++){ int nx=cx+dx[k], ny=cy+dy[k]; if(nx<0||ny<0||nx>=w||ny>=h) continue; if(dist[nx,ny]>dist[cx,cy]+1){ dist[nx,ny]=dist[cx,cy]+1; qq.Enqueue(nx*10000+ny); } } }
      // привязка путевых точек к ближайшему допустимому пикселю
      var snapped=new List<int[]>();
      foreach(var wp in wps){ int bx=-1,by=-1; double bd=1e9; for(int dy=-8;dy<=8;dy++) for(int dx=-8;dx<=8;dx++){ int x=wp[0]+dx,y=wp[1]+dy; if(x<0||y<0||x>=w||y>=h||!ok[x,y]) continue; double d=dx*dx+dy*dy-0.5*Math.Min(dist[x,y],4); if(d<bd){bd=d;bx=x;by=y;} } if(bx<0){ Console.WriteLine("точка вне области: "+name+" "+wp[0]+","+wp[1]); continue; } snapped.Add(new int[]{bx,by}); }
      var full=new List<int[]>();
      for(int s=0;s+1<snapped.Count;s++){
        var A=snapped[s]; var B=snapped[s+1]; var cost=new double[w,h]; var prev=new int[w,h]; for(int y=0;y<h;y++) for(int x=0;x<w;x++){cost[x,y]=1e18;prev[x,y]=-1;}
        var pq=new SortedSet<Tuple<double,int>>(Comparer<Tuple<double,int>>.Create((a,b)=>{int c=a.Item1.CompareTo(b.Item1); return c!=0?c:a.Item2.CompareTo(b.Item2);}));
        cost[A[0],A[1]]=0; pq.Add(Tuple.Create(0.0,A[0]*10000+A[1]));
        while(pq.Count>0){ var t=pq.Min; pq.Remove(t); int v=t.Item2; int cx=v/10000, cy=v%10000; if(t.Item1>cost[cx,cy]) continue; if(cx==B[0]&&cy==B[1]) break;
          for(int dy=-1;dy<=1;dy++) for(int dx=-1;dx<=1;dx++){ if(dx==0&&dy==0) continue; int nx=cx+dx, ny=cy+dy; if(nx<0||ny<0||nx>=w||ny>=h||!pass[nx,ny]) continue; double step=(dx!=0&&dy!=0)?1.414:1.0; double c=ok[nx,ny]?step*(1.0+14.0/(1.0+Math.Min(dist[nx,ny],6))):step*25.0; double nc=cost[cx,cy]+c; if(nc<cost[nx,ny]){ cost[nx,ny]=nc; prev[nx,ny]=cx*10000+cy; pq.Add(Tuple.Create(nc,nx*10000+ny)); } } }
        if(prev[B[0],B[1]]<0&&!(A[0]==B[0]&&A[1]==B[1])){ Console.WriteLine("нет пути: "+name+" "+A[0]+","+A[1]+" -> "+B[0]+","+B[1]); continue; }
        var seg=new List<int[]>(); int cur=B[0]*10000+B[1]; while(cur!=-1){ seg.Add(new int[]{cur/10000,cur%10000}); if(cur==A[0]*10000+A[1]) break; cur=prev[cur/10000,cur%10000]; }
        seg.Reverse(); if(full.Count>0) seg.RemoveAt(0); full.AddRange(seg);
      }
      var poly=Rdp(full,1.1); var sb2=new StringBuilder(); for(int i=0;i<poly.Count;i++) sb2.Append((i==0?"M":"L")+poly[i][0].ToString("0.#",System.Globalization.CultureInfo.InvariantCulture)+" "+poly[i][1].ToString("0.#",System.Globalization.CultureInfo.InvariantCulture)+" ");
      outSb.Append(name+"|"+sb2.ToString().Trim()+"\n");
    }
    File.WriteAllText(dir+"/out_paths.txt",outSb.ToString()); Console.WriteLine("paths done");
  }
}
