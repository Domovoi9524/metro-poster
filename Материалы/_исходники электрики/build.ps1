$sd=Split-Path -Parent $MyInvocation.MyCommand.Path
$proj="C:\Users\egorb\Documents\Пособие по тормозному оборудованию"
$t=[IO.File]::ReadAllText("$sd\power_js.html",[Text.Encoding]::UTF8)
$k013=[IO.File]::ReadAllText("$proj\Кран машиниста 013.html",[Text.Encoding]::UTF8)
$ic=[regex]::Match($k013,'rel="icon" type="image/png" href="([^"]+)"').Groups[1].Value
$fi=$k013.IndexOf("/* ---------- полноэкранный режим"); $fe=$k013.IndexOf("</script>",$fi); $fs=$k013.Substring($fi,$fe-$fi)
$head=[IO.File]::ReadAllText("$sd\power_head.html",[Text.Encoding]::UTF8).Replace("%%ICON%%",$ic).Replace("%%LAYERS%%",[IO.File]::ReadAllText("$sd\layers.txt"))
$segs=([IO.File]::ReadAllLines("$sd\routes2_out.txt") | Where-Object { $_.Trim() } | ForEach-Object { $p=$_.Split('|'); "  `"$($p[0])`":`"$($p[1])`"" }) -join ",`n"
$fld=[IO.File]::ReadAllText("$sd\fld.js")
[IO.File]::WriteAllText("$proj\Силовая схема.html",$head+$t.Replace("%%SEGS%%",$segs).Replace("%%FLD%%",$fld)+"`n"+$fs+"</script>`n</body>`n</html>`n",(New-Object Text.UTF8Encoding($false)))
(Get-Item "$proj\Силовая схема.html").Length
