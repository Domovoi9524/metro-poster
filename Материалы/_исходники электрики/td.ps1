$sd=Split-Path -Parent $MyInvocation.MyCommand.Path
Add-Type -ReferencedAssemblies System.Drawing -Path "$sd\TabDots.cs"
[TabDots]::Run($sd,3930,2895)
