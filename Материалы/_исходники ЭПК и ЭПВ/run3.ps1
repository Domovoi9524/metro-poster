$sd=Split-Path -Parent $MyInvocation.MyCommand.Path
$vr="$env:TEMP\vr\gen\Smooth.cs"
Add-Type -ReferencedAssemblies System.Drawing -Path "$sd\Morph.cs","$vr","$sd\Epk.cs"
[Epk]::Run("$sd\..\pl25\x\word\media\image26.png","$sd\epk_layers.txt")
