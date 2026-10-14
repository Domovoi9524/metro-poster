using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
public static class Trace {
  public static void Run(string rect,string dir){
    int W,H; float[] nr,ng,nb; var cls=Seg.Classify(rect,out W,out H,out nr,out ng,out nb);
    double k=W/1600.0;
    int cx0=(int)(18*k), cx1=(int)(1578*k), cy0=(int)(108*k), cy1=(int)(1172*k);
    for(int y=0;y<H;y++) for(int x=0;x<W;x++) if(x<cx0||x>=cx1||y<cy0||y>=cy1) cls[y*W+x]=0;
    // мелкий мусор: компоненты неблые размером < 7 пикселей -> белые (8-связность по классу)
    var seen=new bool[W*H]; var st=new Stack<int>(); var comp=new List<int>();
    for(int s=0;s<W*H;s++){ if(seen[s]||cls[s]==0) continue; byte c=cls[s]; comp.Clear(); st.Push(s); seen[s]=true;
      while(st.Count>0){ int p=st.Pop(); comp.Add(p); int px=p%W, py=p/W; for(int dy=-1;dy<=1;dy++) for(int dx=-1;dx<=1;dx++){ int qx=px+dx,qy=py+dy; if(qx<0||qy<0||qx>=W||qy>=H) continue; int q=qy*W+qx; if(seen[q]||cls[q]!=c) continue; seen[q]=true; st.Push(q); } }
      int minSz = (int)((c==1?8:12)*Seg.Sc*Seg.Sc); if(comp.Count<minSz) foreach(int p in comp) cls[p]=0; }
    // однородные красные/оранжевые заливки (БВ, резисторы): класс компоненты = преобладающий из {2,3,4}
    { var seen2=new bool[W*H]; var st2=new Stack<int>(); var cm=new List<int>();
      for(int s=0;s<W*H;s++){ if(seen2[s]||cls[s]<2||cls[s]>4) continue; cm.Clear(); st2.Push(s); seen2[s]=true;
        while(st2.Count>0){ int p=st2.Pop(); cm.Add(p); int px=p%W, py=p/W; for(int dy=-1;dy<=1;dy++) for(int dx=-1;dx<=1;dx++){ int qx=px+dx,qy=py+dy; if(qx<0||qy<0||qx>=W||qy>=H) continue; int q=qy*W+qx; if(seen2[q]||cls[q]<2||cls[q]>4) continue; seen2[q]=true; st2.Push(q); } }
        if(cm.Count>=(int)(150*Seg.Sc*Seg.Sc)){ var h3=new int[5]; foreach(int p in cm) h3[cls[p]]++; byte best=2; for(byte c=3;c<=4;c++) if(h3[c]>h3[best]) best=c; foreach(int p in cm) cls[p]=best; } } }    // сетка таблицы РК: самая большая чёрная компонента в области таблицы -> класс 9 (серо-лиловые тонкие линии)
    { int tx0=(int)(1244*k), tx1=(int)(1576*k), ty0=(int)(126*k), ty1=(int)(486*k); var seen3=new bool[W*H]; var st3=new Stack<int>(); var cm3=new List<int>(); List<int> bestC=null;
      for(int s=0;s<W*H;s++){ int sx=s%W, sy=s/W; if(sx<tx0||sx>=tx1||sy<ty0||sy>=ty1||seen3[s]||cls[s]!=1) continue; cm3=new List<int>(); st3.Push(s); seen3[s]=true;
        while(st3.Count>0){ int p=st3.Pop(); cm3.Add(p); int px=p%W, py=p/W; for(int dy=-1;dy<=1;dy++) for(int dx=-1;dx<=1;dx++){ int qx=px+dx,qy=py+dy; if(qx<tx0||qy<ty0||qx>=tx1||qy>=ty1) continue; int q=qy*W+qx; if(seen3[q]||cls[q]!=1) continue; seen3[q]=true; st3.Push(q); } }
        if(bestC==null||cm3.Count>bestC.Count) bestC=cm3; }
      if(bestC!=null) foreach(int p in bestC) cls[p]=9; }    // средний цвет классов
    var sum=new double[10,3]; var cnt=new long[10];
    for(int i=0;i<W*H;i++){ int c=cls[i]; if(c==0) continue; sum[c,0]+=nr[i]; sum[c,1]+=ng[i]; sum[c,2]+=nb[i]; cnt[c]++; }
    var hex=new string[10]; for(int c=0;c<10;c++){ if(cnt[c]==0){hex[c]="#000000";continue;} hex[c]="#"+((int)Math.Min(255,sum[c,0]/cnt[c]*255)).ToString("x2")+((int)Math.Min(255,sum[c,1]/cnt[c]*255)).ToString("x2")+((int)Math.Min(255,sum[c,2]/cnt[c]*255)).ToString("x2"); Console.WriteLine("class "+c+" "+(c<Seg.Names.Length?Seg.Names[c]:(char)103)+" n="+cnt[c]+" "+hex[c]); }
    File.WriteAllBytes(dir+"/cls.bin",cls);
    string dots1=Dots.Run(cls,W,H,4.6*Seg.Sc/1.5); string rects1=Snap.Run(cls,W,H,1,(int)(40*Seg.Sc*Seg.Sc),14,(int)(40*Seg.Sc*Seg.Sc)), rects9=Snap.Run(cls,W,H,9,(int)(30*Seg.Sc),6,(int)(30*Seg.Sc));
    // слои: чёрный снизу, затем остальные
    int[] order={1,9,8,7,6,5,4,3,2}; int[] rank=new int[10]; for(int i=0;i<order.Length;i++) rank[order[i]]=i+1;
    var sb=new StringBuilder();
    Sm.U=2; Sm.Sigma=0.5; Sm.Eps=0.3;
    foreach(int c in order){ if(cnt[c]==0) continue; var m=new bool[W,H]; for(int y=0;y<H;y++) for(int x=0;x<W;x++){ int cc=cls[y*W+x]; m[x,y]= c==9 ? cc==9 : (cc!=0 && cc!=9 && rank[cc]>=rank[c]); }
      string d=Sm.Path(m,W,H); sb.Append("<path fill=\""+Fill(c,hex)+"\" fill-rule=\"evenodd\" d=\""+d+"\"/>\n"); Console.WriteLine("layer "+c+" len="+d.Length); if(c==1) sb.Append("<path fill=\"#111\" d=\""+rects1+dots1+"\"/>\n"); if(c==9) sb.Append("<path fill=\"#837888\" d=\""+rects9+"\"/>\n"); }
    File.WriteAllText(dir+"/layers.txt",sb.ToString());
  }
  static string Fill(int c,string[] hex){
    switch(c){ case 1: return "#111"; case 2: return "#8a2030"; case 3: return "#d42a26"; case 4: return "#e0592a"; case 5: return "#f5d21c"; case 6: return "#1e8a3e"; case 7: return "#a9d6f0"; case 8: return "#b9a9b9"; case 9: return "#837888"; }
    return hex[c];
  }
}
