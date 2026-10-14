param([string]$img,[double]$x0,[double]$y0,[double]$w,[double]$h,[double]$k,[string]$paths,[string]$out,[double]$ix=1,[double]$iy=1,[double]$ox=0,[double]$oy=0)
# draws M/L/H/V polylines (name|d per line) over a crop of the image; coords of paths are in "scheme units": image px = (u*ix+ox)
Add-Type -AssemblyName System.Drawing
$im=[Drawing.Image]::FromFile($img)
$bm=New-Object Drawing.Bitmap ([int]($w*$k)),([int]($h*$k)); $g=[Drawing.Graphics]::FromImage($bm); $g.InterpolationMode='NearestNeighbor'; $g.PixelOffsetMode='Half'; $g.SmoothingMode='AntiAlias'
$g.DrawImage($im,(New-Object Drawing.Rectangle 0,0,([int]($w*$k)),([int]($h*$k))),($x0*$ix+$ox),($y0*$iy+$oy),($w*$ix),($h*$iy),[Drawing.GraphicsUnit]::Pixel)
$cols=@('Magenta','Lime','OrangeRed','Blue','DarkRed','DarkCyan','Olive','Purple','Gold','DeepPink','Green','Brown')
$f=New-Object Drawing.Font('Arial',[single]8,[Drawing.FontStyle]::Bold)
$ci=0; $grid=New-Object Drawing.Pen ([Drawing.Color]::FromArgb(90,255,0,255)),1; $gf=New-Object Drawing.Font('Arial',[single]7)
for($x=[math]::Ceiling($x0/10)*10;$x -le $x0+$w;$x+=10){ $g.DrawLine($grid,($x-$x0)*$k,0,($x-$x0)*$k,$h*$k); $g.DrawString("$x",$gf,[Drawing.Brushes]::Magenta,($x-$x0)*$k+1,0) }
for($y=[math]::Ceiling($y0/10)*10;$y -le $y0+$h;$y+=10){ $g.DrawLine($grid,0,($y-$y0)*$k,$w*$k,($y-$y0)*$k); $g.DrawString("$y",$gf,[Drawing.Brushes]::Magenta,0,($y-$y0)*$k+1) }
foreach($ln in [IO.File]::ReadAllLines($paths)){
  if($ln.Trim().Length -eq 0){continue}
  $nm=$ln.Split('|')[0]; $d=$ln.Substring($nm.Length+1)
  $col=[Drawing.Color]::FromName($cols[$ci % $cols.Count]); $ci++
  $pen=New-Object Drawing.Pen $col,2; $pen.DashStyle='Dash'
  $tok=[regex]::Matches($d,'[MLHVZ]|-?[\d.]+') | ForEach-Object { $_.Value }
  $pts=New-Object System.Collections.ArrayList; $i=0; $cx=0.0; $cy=0.0; $cmd=''
  while($i -lt $tok.Count){ $t=$tok[$i]
    if($t -match '^[MLHVZ]$'){ $cmd=$t; $i++; continue }
    if($cmd -eq 'M' -or $cmd -eq 'L'){ $cx=[double]$tok[$i]; $cy=[double]$tok[$i+1]; $i+=2; [void]$pts.Add(@($cx,$cy)) }
    elseif($cmd -eq 'H'){ $cx=[double]$tok[$i]; $i++; [void]$pts.Add(@($cx,$cy)) }
    elseif($cmd -eq 'V'){ $cy=[double]$tok[$i]; $i++; [void]$pts.Add(@($cx,$cy)) }
    else { $i++ } }
  for($j=0;$j -lt $pts.Count-1;$j++){ $g.DrawLine($pen,($pts[$j][0]-$x0)*$k,($pts[$j][1]-$y0)*$k,($pts[$j+1][0]-$x0)*$k,($pts[$j+1][1]-$y0)*$k) }
  if($pts.Count -gt 0){ $g.FillEllipse((New-Object Drawing.SolidBrush $col),($pts[0][0]-$x0)*$k-4,($pts[0][1]-$y0)*$k-4,8,8); $g.DrawString($nm,$f,(New-Object Drawing.SolidBrush $col),($pts[0][0]-$x0)*$k+5,($pts[0][1]-$y0)*$k-12)
    $e=$pts[$pts.Count-1]; $g.FillRectangle((New-Object Drawing.SolidBrush $col),($e[0]-$x0)*$k-3,($e[1]-$y0)*$k-3,6,6) }
}
$bm.Save($out)
