$d="$env:TEMP\vr\gen"
$t=[IO.File]::ReadAllText("$d\Gen3.cs",[Text.Encoding]::UTF8)
$a=$t.IndexOf("    var sbBase=new StringBuilder();")
$b=$t.IndexOf("    File.WriteAllText(dir+`"/out_base.txt`"")
$e=[IO.File]::ReadAllText("$d\emit4.txt",[Text.Encoding]::UTF8)
$t=$t.Substring(0,$a)+$e+$t.Substring($b)
$t=$t.Replace("public static class Gen3 {","public static class Gen4 {")
$fw="  static void FillWild(int[,] c,bool[,] wild,bool[,] dom,int w,int h){ var qu=new Queue<int>(); var done=new bool[w,h]; for(int y=0;y<h;y++) for(int x=0;x<w;x++) if(dom[x,y]&&!wild[x,y]){ done[x,y]=true; qu.Enqueue(x*10000+y); } int[] dx={1,-1,0,0}, dy={0,0,1,-1}; while(qu.Count>0){ int v=qu.Dequeue(); int cx=v/10000, cy=v%10000; for(int k=0;k<4;k++){ int nx=cx+dx[k], ny=cy+dy[k]; if(nx<0||ny<0||nx>=w||ny>=h||!dom[nx,ny]||done[nx,ny]) continue; done[nx,ny]=true; c[nx,ny]=c[cx,cy]; qu.Enqueue(nx*10000+ny); } } }`n"
$t=$t.Replace("  public static void Run(string dir,int w,int h){",$fw+"  public static void Run(string dir,int w,int h){")
[IO.File]::WriteAllText("$d\Gen4.cs",$t,(New-Object Text.UTF8Encoding($true)))
"ok"
