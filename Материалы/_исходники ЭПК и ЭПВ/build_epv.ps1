$ep=Split-Path -Parent $MyInvocation.MyCommand.Path
$sd=Split-Path -Parent $ep
$proj="C:\Users\egorb\Documents\Пособие по тормозному оборудованию"
$k013=[IO.File]::ReadAllText("$proj\Кран машиниста 013.html",[Text.Encoding]::UTF8)
$ic=[regex]::Match($k013,'rel="icon" type="image/png" href="([^"]+)"').Groups[1].Value
$fi=$k013.IndexOf("/* ---------- полноэкранный режим"); $fe=$k013.IndexOf("</script>",$fi); $fs=$k013.Substring($fi,$fe-$fi)
$style=[IO.File]::ReadAllText("$sd\dev_style.html",[Text.Encoding]::UTF8)
$body=[IO.File]::ReadAllText("$ep\epv_body.html",[Text.Encoding]::UTF8)
$layers=[IO.File]::ReadAllText("$ep\epv_layers2.txt",[Text.Encoding]::UTF8)
$body=$body.Replace("%%LAYERS%%",$layers).Replace("%%H1%%","Электропневматический вентиль ЭПВ-АРС").Replace("%%SUB%%","Автостопная магистраль · питание от системы АРС, реле давления крана 013 · электропоезда 81-717/714")
$style=$style.Replace("%%TITLE%%","ЭПВ-АРС — интерактивное пособие").Replace("%%ICON%%",$ic)
[IO.File]::WriteAllText("$proj\ЭПВ.html",$style+$body+"`n"+$fs+"</script>`n</body>`n</html>`n",(New-Object Text.UTF8Encoding($false)))
(Get-Item "$proj\ЭПВ.html").Length
