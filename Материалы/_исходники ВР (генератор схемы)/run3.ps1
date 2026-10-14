Add-Type -ReferencedAssemblies System.Drawing -Path "$env:TEMP\vr\gen\Gen3.cs"
[Gen3]::Run("$env:TEMP\vr\gen",880,510)
