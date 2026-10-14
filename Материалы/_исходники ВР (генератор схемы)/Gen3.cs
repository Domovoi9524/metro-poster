using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
public static class Gen3 {
  static string[] hex={"#ffffff","#c0c0c0","#008080","#808080","#00ff80","#00ffff","#ff0000","#ff00ff","#0080ff","#000000","#ff8040","#804000","#ffff00"};
  static string[] defKey={null,null,"rk",null,"tc","nv","mk","kdr","ar",null,null,null,null};
  class Seed{public string key;public int x,y;public int rx0=-1,ry0=-1,rx1=99999,ry1=99999;}
  static string Rects(bool[,] m,int w,int h){
    var sb=new StringBuilder(); var open=new Dictionary<long,int[]>();
    for(int y=0;y<=h;y++){
      var cur=new Dictionary<long,int[]>();
      if(y<h){ int x=0; while(x<w){ if(m[x,y]){ int x0=x; while(x<w&&m[x,y]) x++; long k=(long)x0*4096+x; cur[k]=new int[]{x0,x,y}; } else x++; } }
      foreach(var kv in open){ if(cur.ContainsKey(kv.Key)){ cur[kv.Key][2]=kv.Value[2]; } else { var r=kv.Value; sb.Append("M"+r[0]+" "+r[2]+"h"+(r[1]-r[0])+"v"+(y-r[2])+"h-"+(r[1]-r[0])+"z"); } }
      open=cur;
    }
    return sb.ToString();
  }
  public static void Run(string dir,int w,int h){
    string[] lines=File.ReadAllLines(dir+"/qmap.txt"); int[,] q0=new int[w,h];
    for(int y=0;y<h;y++) for(int x=0;x<w;x++) q0[x,y]=lines[y][x]-'a';
    var seeds=new List<Seed>(); foreach(var ln in File.ReadAllLines(dir+"/seeds.txt")){ if(ln.Trim().Length==0||ln.StartsWith("#")) continue; var p=ln.Split('|'); var sd=new Seed{key=p[0],x=int.Parse(p[1]),y=int.Parse(p[2])}; if(p.Length>=7){sd.rx0=int.Parse(p[3]);sd.ry0=int.Parse(p[4]);sd.rx1=int.Parse(p[5]);sd.ry1=int.Parse(p[6]);} seeds.Add(sd); }
    int[,] comp=new int[w,h]; for(int y=0;y<h;y++) for(int x=0;x<w;x++) comp[x,y]=-1;
    var stack=new Stack<int>(); int id=0;
    for(int y=0;y<h;y++) for(int x=0;x<w;x++){
      if(comp[x,y]!=-1) continue; int col=q0[x,y]; if(col==1||col==3||col==9||col==10||col==11||col==12) continue;
      stack.Push(x*10000+y); comp[x,y]=id;
      while(stack.Count>0){ int v=stack.Pop(); int cx=v/10000, cy=v%10000; int[] dx={1,-1,0,0}, dy={0,0,1,-1};
        for(int k=0;k<4;k++){ int nx=cx+dx[k], ny=cy+dy[k]; if(nx<0||ny<0||nx>=w||ny>=h) continue; if(comp[nx,ny]!=-1||q0[nx,ny]!=col) continue; comp[nx,ny]=id; stack.Push(nx*10000+ny); } }
      id++;
    }
    string[,] key0=new string[w,h]; var queue=new Queue<int>(); var seedIdx=new int[w,h];
    for(int si=0;si<seeds.Count;si++){ var s=seeds[si]; if(comp[s.x,s.y]<0){ Console.WriteLine("seed не в компоненте: "+s.key+" "+s.x+","+s.y+" q="+(char)('a'+q0[s.x,s.y])); continue; } key0[s.x,s.y]=s.key; seedIdx[s.x,s.y]=si; queue.Enqueue(s.x*10000+s.y); }
    while(queue.Count>0){ int v=queue.Dequeue(); int cx=v/10000, cy=v%10000; int[] dx={1,-1,0,0}, dy={0,0,1,-1}; var sd=seeds[seedIdx[cx,cy]];
      for(int k=0;k<4;k++){ int nx=cx+dx[k], ny=cy+dy[k]; if(nx<0||ny<0||nx>=w||ny>=h) continue; if(comp[nx,ny]!=comp[cx,cy]||key0[nx,ny]!=null) continue; if(nx<sd.rx0||ny<sd.ry0||nx>sd.rx1||ny>sd.ry1) continue; key0[nx,ny]=key0[cx,cy]; seedIdx[nx,ny]=seedIdx[cx,cy]; queue.Enqueue(nx*10000+ny); } }
    for(int y=0;y<h;y++) for(int x=0;x<w;x++){ if(comp[x,y]>=0&&key0[x,y]==null){ int col=q0[x,y]; if(defKey[col]!=null) key0[x,y]=defKey[col]; } }
    var partMask=new Dictionary<string,bool[,]>(); var partOrder=new List<string>(); var partMode=new Dictionary<string,char>(); var allMask=new bool[w,h]; var fillAt=new int[w,h]; for(int y=0;y<h;y++) for(int x=0;x<w;x++) fillAt[x,y]=-1;
    foreach(var ln in File.ReadAllLines(dir+"/parts.txt")){
      if(ln.Trim().Length==0||ln.StartsWith("#")) continue; var p=ln.Split('|'); string name=p[0];
      int x0=int.Parse(p[1]),y0=int.Parse(p[2]),x1=int.Parse(p[3]),y1=int.Parse(p[4]); string cols=p.Length>5&&p[5].Length>0?p[5]:"djmlk"; char mode=p.Length>6&&p[6].Length>0?p[6][0]:'h'; int fill=(p.Length>7&&p[7].Length>0)?(p[7][0]-'a'):-1;
      if(!partMask.ContainsKey(name)){ partMask[name]=new bool[w,h]; partOrder.Add(name); partMode[name]=mode; }
      for(int y=y0;y<=y1;y++) for(int x=x0;x<=x1;x++){ if(cols.IndexOf((char)('a'+q0[x,y]))>=0){ partMask[name][x,y]=true; allMask[x,y]=true; fillAt[x,y]=fill; } }
    }
    int[,] q=(int[,])q0.Clone(); string[,] key=(string[,])key0.Clone();
    foreach(var name in partOrder){ var m=partMask[name]; char mode=partMode[name];
      for(int y=0;y<h;y++) for(int x=0;x<w;x++){ if(!m[x,y]) continue;
        if(fillAt[x,y]>=0){ q[x,y]=fillAt[x,y]; key[x,y]=defKey[fillAt[x,y]]; continue; }
        int bx=-1,by=-1;
        for(int d=1;d<40&&bx<0;d++){
          if(mode=='h'){ if(x-d>=0&&!allMask[x-d,y]){bx=x-d;by=y;} else if(x+d<w&&!allMask[x+d,y]){bx=x+d;by=y;} }
          else { if(y-d>=0&&!allMask[x,y-d]){bx=x;by=y-d;} else if(y+d<h&&!allMask[x,y+d]){bx=x;by=y+d;} }
        }
        if(bx<0){ q[x,y]=0; key[x,y]=null; } else { q[x,y]=q0[bx,by]; key[x,y]=key0[bx,by]; } } }
    var sbBase=new StringBuilder(); sbBase.Append("<g id=\"base\" shape-rendering=\"crispEdges\">\n");
    for(int col=1;col<13;col++){ if(col==2||col==4||col==5||col==6||col==7||col==8) continue; var m=new bool[w,h]; bool any=false;
      for(int y=0;y<h;y++) for(int x=0;x<w;x++) if(q[x,y]==col){ m[x,y]=true; any=true; }
      if(any) sbBase.Append("<path fill=\""+hex[col]+"\" d=\""+Rects(m,w,h)+"\"/>\n"); }
    foreach(int col in new int[]{2,4,5,6,7,8}){ var m=new bool[w,h]; bool any=false; for(int y=0;y<h;y++) for(int x=0;x<w;x++) if(q[x,y]==col&&key[x,y]==null){ m[x,y]=true; any=true; } if(any) sbBase.Append("<path fill=\""+hex[col]+"\" d=\""+Rects(m,w,h)+"\"/>\n"); }
    sbBase.Append("</g>\n");
    var keys=new SortedSet<string>(); for(int y=0;y<h;y++) for(int x=0;x<w;x++) { if(key[x,y]!=null) keys.Add(key[x,y]); if(key0[x,y]!=null) keys.Add(key0[x,y]); }
    var sbReg=new StringBuilder();
    foreach(string k in keys){ foreach(int col in new int[]{0,2,4,5,6,7,8}){ var m=new bool[w,h]; bool any=false; for(int y=0;y<h;y++) for(int x=0;x<w;x++) if(key[x,y]==k&&q[x,y]==col){ m[x,y]=true; any=true; } if(any) sbReg.Append("<path data-reg=\""+k+"\" fill=\""+hex[col]+"\" d=\""+Rects(m,w,h)+"\"/>\n"); } }
    var sbParts=new StringBuilder();
    foreach(var name in partOrder){ var m=partMask[name]; sbParts.Append("<g id=\""+name+"\" shape-rendering=\"crispEdges\">\n");
      for(int col=1;col<13;col++){ var mm=new bool[w,h]; bool any=false; for(int y=0;y<h;y++) for(int x=0;x<w;x++) if(m[x,y]&&q0[x,y]==col&&(key0[x,y]==null)){ mm[x,y]=true; any=true; } if(any) sbParts.Append("<path fill=\""+hex[col]+"\" d=\""+Rects(mm,w,h)+"\"/>\n"); }
      foreach(string k in keys){ foreach(int col in new int[]{0,2,4,5,6,7,8}){ var mm=new bool[w,h]; bool any=false; for(int y=0;y<h;y++) for(int x=0;x<w;x++) if(m[x,y]&&q0[x,y]==col&&key0[x,y]==k){ mm[x,y]=true; any=true; } if(any) sbParts.Append("<path data-reg=\""+k+"\" fill=\""+hex[col]+"\" d=\""+Rects(mm,w,h)+"\"/>\n"); } }
      sbParts.Append("</g>\n"); }
    File.WriteAllText(dir+"/out_base.txt",sbBase.ToString()); File.WriteAllText(dir+"/out_reg.txt",sbReg.ToString()); File.WriteAllText(dir+"/out_parts.txt",sbParts.ToString());
    { var kc=new Dictionary<string,char>{{"ar",(char)65},{"kdr",(char)68},{"kl",(char)76},{"mk",(char)77},{"nv",(char)78},{"p1",(char)80},{"rk",(char)82},{"tc",(char)84},{"tm",(char)88},{"zr",(char)90}}; var sk=new StringBuilder(); var sq=new StringBuilder(); for(int y=0;y<h;y++){ for(int x=0;x<w;x++){ sk.Append(key[x,y]==null?(char)46:kc[key[x,y]]); sq.Append((char)(97+q[x,y])); } sk.Append((char)10); sq.Append((char)10);} File.WriteAllText(dir+"/kmap.txt",sk.ToString()); File.WriteAllText(dir+"/q2map.txt",sq.ToString()); }
    Console.WriteLine("keys: "+string.Join(",",keys)+"  base="+sbBase.Length+" reg="+sbReg.Length+" parts="+sbParts.Length);
  }
}
