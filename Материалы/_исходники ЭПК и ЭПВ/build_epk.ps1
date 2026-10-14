$ep=Split-Path -Parent $MyInvocation.MyCommand.Path
$sd=Split-Path -Parent $ep
$proj="C:\Users\egorb\Documents\Пособие по тормозному оборудованию"
$k013=[IO.File]::ReadAllText("$proj\Кран машиниста 013.html",[Text.Encoding]::UTF8)
$ic=[regex]::Match($k013,'rel="icon" type="image/png" href="([^"]+)"').Groups[1].Value
$fi=$k013.IndexOf("/* ---------- полноэкранный режим"); $fe=$k013.IndexOf("</script>",$fi); $fs=$k013.Substring($fi,$fe-$fi)
$style=[IO.File]::ReadAllText("$sd\dev_style.html",[Text.Encoding]::UTF8)
$info=Get-Content "$ep\epk_layers.info"; $angle=($info[0] -replace 'angle=','').Replace(',','.')
$body=[IO.File]::ReadAllText("$ep\epk_body.html",[Text.Encoding]::UTF8)
$layers=[IO.File]::ReadAllText("$ep\epk_layers.txt",[Text.Encoding]::UTF8)
$body=$body.Replace("%%LAYERS%%",$layers).Replace("%%ANGLE%%",$angle).Replace("%%H1%%","Электропневматический клапан автостопа (ЭПК)").Replace("%%SUB%%","Автостопная магистраль · работа клапана под питанием и без питания · электропоезда 81-717/714")
$style=$style.Replace("%%TITLE%%","ЭПК — интерактивное пособие").Replace("%%ICON%%",$ic)
[IO.File]::WriteAllText("$proj\ЭПК.html",$style+$body+"`n"+$fs+"</script>`n</body>`n</html>`n",(New-Object Text.UTF8Encoding($false)))
(Get-Item "$proj\ЭПК.html").Length
