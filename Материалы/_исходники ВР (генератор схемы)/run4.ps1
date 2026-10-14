$d="$env:TEMP\vr\gen"
Add-Type -ReferencedAssemblies System.Drawing -Path "$d\Smooth.cs","$d\Gen4.cs"
$sw=[Diagnostics.Stopwatch]::StartNew()
[Gen4]::Run($d,880,510)
"время: "+$sw.Elapsed.TotalSeconds
