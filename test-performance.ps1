#Requires -Version 7.0
$ErrorActionPreference='Stop'
$source=Join-Path $PSScriptRoot 'src/PerformanceTrace.cs'
if(!(Test-Path -LiteralPath $source)){throw '缺少性能采样器'}
Add-Type -Path $source
function Check($condition,$why){if(!$condition){throw $why}}
$p=[KingdomAdvisor.PerformanceTrace]::new(8,5)
$p.BeginFrame(1,0,$true,1,$true,0)
$p.Record([KingdomAdvisor.PerformanceStage]::World,280)
$p.BeginFrame(2,.3,$true,1,$true,0)
$frames=$p.Frames()
Check ($frames.Length -eq 1 -and $frames[0].Frame -eq 1) '帧工作必须关联前一帧'
Check ([Math]::Abs($frames[0].FrameMs-300) -lt .001 -and $frames[0].World -eq 280) '帧间隔或扫描耗时丢失'
Check (!$frames[0].Stable) '加载阶段尖峰不得混入稳定帧'
$p.BeginFrame(3,5.1,$true,1,$true,0)
$p.Record([KingdomAdvisor.PerformanceStage]::Payables,7)
$p.BeginFrame(4,5.15,$true,1,$true,1)
$frames=$p.Frames();$last=$frames[-1]
Check ($last.Frame -eq 3 -and $last.Stable -and $last.Gc0 -eq 1 -and $last.Payables -eq 7) '稳定帧与GC次数关联'
$p.BeginFrame(5,5.17,$true,2,$true,1)
$p.BeginFrame(6,5.19,$true,2,$true,1)
Check (!$p.Frames()[-1].Stable) '换岛必须重新区分稳定阶段'
$p.BeginFrame(7,11,$true,2,$false,1)
$p.BeginFrame(8,11.1,$true,2,$false,1)
Check (!$p.Frames()[-1].Stable -and !$p.Frames()[-1].Focused) '后台帧不得作为前台稳定帧'
foreach($i in 9..30){$p.BeginFrame($i,12+$i*.02,$true,2,$true,1)}
Check ($p.Frames().Length -eq 8) '采样缓存必须有界'
Check ($p.Frames()[0].Frame -eq 22 -and $p.Frames()[-1].Frame -eq 29) '环形样本顺序错误'
$p.Record([KingdomAdvisor.PerformanceStage]::Gui,2)
$p.Record([KingdomAdvisor.PerformanceStage]::Gui,3)
$p.BeginFrame(31,12.62,$true,2,$true,1)
Check ($p.Frames()[-1].Gui -eq 5) '同帧多次OnGUI必须累计'
$p.BeginFrame(31,12.62,$true,2,$true,1)
Check ($p.Frames()[-1].Frame -eq 30) '重复帧号不能制造零长度样本'
Check (($p.Evidence() -join '|').Contains('performanceStableFrames=')) '缺少稳定帧计数'
Check ($p.Tsv().Length -le 257) '导出必须有界'
$p.BeginFrame(32,1,$false,2,$true,1)
Check ($p.Frames().Length -eq 0) '时钟回退必须清理旧时间轴'
'通过：帧关联、加载/稳定/换岛/后台分类、GC关联、同帧累计、环形边界及时间轴重置。性能结论仍需实机采样。'
