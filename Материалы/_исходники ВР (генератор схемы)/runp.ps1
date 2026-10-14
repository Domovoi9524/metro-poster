Add-Type -ReferencedAssemblies System.Drawing -Path "$env:TEMP\vr\gen\Paths.cs"
[Paths]::Run("$env:TEMP\vr\gen",880,510)
