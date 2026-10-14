$sd=Split-Path -Parent $MyInvocation.MyCommand.Path
$vr="$env:TEMP\vr\gen\Smooth.cs"
Add-Type -ReferencedAssemblies System.Drawing -Path "$vr","$sd\Epv.cs","$sd\Epv2.cs"
[Epv]::Run("$sd\epv_raw.png","$sd\epv",36) | Out-Null
[Epv2]::Run("$sd\epv_layers2.txt")
