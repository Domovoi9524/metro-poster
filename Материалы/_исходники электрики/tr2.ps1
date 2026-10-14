param([string]$mode)
$sd=Split-Path -Parent $MyInvocation.MyCommand.Path
$vr="$env:TEMP\vr\gen\Smooth.cs"
try{ Add-Type -ReferencedAssemblies System.Drawing -Path "$sd\Seg.cs","$sd\Snap.cs","$vr","$sd\Trace.cs","$sd\Route.cs" }catch{ $_.Exception.Message.Substring(0,[Math]::Min(1500,$_.Exception.Message.Length)); return }
[Seg]::Sc=3.4/1.6375
$sw=[Diagnostics.Stopwatch]::StartNew()
if($mode -eq 'trace'){ [Trace]::Run("$sd\rect2.png",$sd); (Get-Item "$sd\layers.txt").Length }
if($mode -eq 'route'){ [Route]::Run($sd,"$sd\routes2.txt","$sd\routes2_out.txt",5440,4007) }
$sw.Elapsed.TotalSeconds
