param([string]$src,[double]$px0,[double]$py0,[double]$pw,[double]$ph,[double]$k,[string]$out,[double]$scale=3.57,[int]$grid=25,[int]$labelEvery=50)
# crops region given in "preview px" (1600-wide scale) from full-res image; k = output pixels per preview px; draws grid with preview coordinates
Add-Type -AssemblyName System.Drawing
$im=[Drawing.Image]::FromFile($src)
$w=[int]($pw*$k); $h=[int]($ph*$k)
$bm=New-Object Drawing.Bitmap $w,$h; $g=[Drawing.Graphics]::FromImage($bm); $g.InterpolationMode='HighQualityBicubic'
$g.DrawImage($im,(New-Object Drawing.Rectangle 0,0,$w,$h),[single]($px0*$scale),[single]($py0*$scale),[single]($pw*$scale),[single]($ph*$scale),[Drawing.GraphicsUnit]::Pixel)
$pen=New-Object Drawing.Pen ([Drawing.Color]::FromArgb(70,255,0,200)),1; $pen2=New-Object Drawing.Pen ([Drawing.Color]::FromArgb(120,255,0,200)),1
$f=New-Object Drawing.Font('Arial',[single]9,[Drawing.FontStyle]::Bold); $br=New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(230,200,0,160))
for($x=[math]::Ceiling($px0/$grid)*$grid;$x -le $px0+$pw;$x+=$grid){ $gx=($x-$px0)*$k; $isL=($x % $labelEvery -eq 0); $g.DrawLine($(if($isL){$pen2}else{$pen}),$gx,0,$gx,$h); if($isL){ $g.DrawString("$x",$f,$br,$gx+1,0); $g.DrawString("$x",$f,$br,$gx+1,$h-14) } }
for($y=[math]::Ceiling($py0/$grid)*$grid;$y -le $py0+$ph;$y+=$grid){ $gy=($y-$py0)*$k; $isL=($y % $labelEvery -eq 0); $g.DrawLine($(if($isL){$pen2}else{$pen}),0,$gy,$w,$gy); if($isL){ $g.DrawString("$y",$f,$br,0,$gy+1); $g.DrawString("$y",$f,$br,$w-30,$gy+1) } }
$bm.Save($out)
