$sd=Split-Path -Parent $MyInvocation.MyCommand.Path
Add-Type -ReferencedAssemblies System.Drawing -Path "$sd\Straight.cs"
[Straight]::Load($sd)
$o=@()
foreach($ln in Get-Content "$sd\routes.txt" -Encoding UTF8){
  if($ln.Trim().Length -eq 0 -or $ln.StartsWith('#')){ continue }
  $p=$ln.Split('|'); $pts=@()
  foreach($t in $p[1].Split(';')){ $q=$t.Split(','); $x=[double]::Parse($q[0],[Globalization.CultureInfo]::InvariantCulture); $y=[double]::Parse($q[1],[Globalization.CultureInfo]::InvariantCulture); $dx=0.0;$dy=0.0; [Straight]::Disp($x,$y,[ref]$dx,[ref]$dy); $pts+=([string]::Format([Globalization.CultureInfo]::InvariantCulture,"{0:0.0},{1:0.0}",$x+$dx,$y+$dy)) }
  $o+=($p[0]+'|'+($pts -join ';'))
}
[IO.File]::WriteAllLines("$sd\routes2.txt",$o)
$o.Count
