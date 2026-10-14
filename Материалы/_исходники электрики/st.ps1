param([string]$mode)
$sd=Split-Path -Parent $MyInvocation.MyCommand.Path
try{ Add-Type -ReferencedAssemblies System.Drawing -Path "$sd\Straight.cs" }catch{ $_.Exception.Message.Substring(0,[Math]::Min(800,$_.Exception.Message.Length)); return }
if($mode -eq 'fit'){ [Straight]::FitAll($sd) | select -Last 3; [Straight]::Dump() }
if($mode -eq 'render'){ [Straight]::Render($sd,"$sd\heic.png","$sd\rect2.png",3.4,-0.65,-0.5); (Get-Item "$sd\rect2.png").Length }
