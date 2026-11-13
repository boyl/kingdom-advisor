#Requires -Version 7.0
$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $PSScriptRoot 'src/HudTextLayout.cs')
[KingdomAdvisor.HudTextRow[]]$rows=1..15 | ForEach-Object {[KingdomAdvisor.HudTextRow]::new("条目 $_")}
$rows[2].ExtraGap=[KingdomAdvisor.HudTextLayout]::StatusGroupGap
$rows[11].ExtraGap=[KingdomAdvisor.HudTextLayout]::StatusGroupGap
$pages=[KingdomAdvisor.HudTextLayout]::Paged($rows,600,34,21.12,{param($s)21.12},[KingdomAdvisor.HudTextLayout]::SingleRowGap)
if($pages.Count -ne 1 -or [Math]::Abs($pages[0].Height-411.8) -gt .01){throw '15 条完整状态须恢复 5 像素行距、两处分组各 10 像素；不能继续使用压紧的 2 像素行距。'}
'通过：默认单列恢复完整字体、5 像素行距及两处分组留白。'
