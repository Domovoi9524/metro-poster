Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -Path "$env:TEMP\vr\gen\Vec.cs"
$src="$env:TEMP\claude\C--Program-Files--x86--Steam-steamapps-common-RimWorld-Mods\fb2ec54a-8b73-413d-9adc-d4c2a66ac979\scratchpad\risunki\word\media\image13.png"
[Vec]::Run($src,"$env:TEMP\vr\gen",880,510)
"done"
