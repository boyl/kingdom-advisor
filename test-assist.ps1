#Requires -Version 7.0
$ErrorActionPreference='Stop'
Add-Type -Path @((Join-Path $PSScriptRoot 'src/Model.cs'),(Join-Path $PSScriptRoot 'src/AssistModel.cs'))
function Equal($actual,$expected,$message){if($actual -ne $expected){throw "$message : $actual != $expected"}}
$h=[KingdomAdvisor.MapHold]::new()
$h.Begin('wall',0,10,$true)
Equal ($h.Step('wall',$false,10.1,$true)) Inspect '短按查看'
$h.Begin('wall',0,20,$true)
Equal ($h.Step('wall',$true,20.8,$true)) Teleport '阈值传送'
Equal ($h.Step('wall',$true,21.8,$true)) None '长按不得重复'
Equal ($h.Step('wall',$false,22,$true)) None '完成松手不得查看'
$h.Begin('wall',0,30,$true)
Equal ($h.Step('wall',$false,30.4,$true)) None '读秒后松手取消'
$h.Begin('wall',0,40,$true)
Equal ($h.Step('camp',$true,40.9,$true)) None '目标改变取消'
Equal ($h.Step('wall',$true,41.9,$true)) None '回到原目标不得恢复'
[void]$h.Step('wall',$false,42,$true)
$h.Begin('wall',0,50,$false)
Equal ($h.Step('wall',$false,51,$true)) Inspect '传送关闭保持查看'
$h.Begin('wall',0,60,$true);$h.Cancel()
Equal ($h.Step('wall',$true,61,$true)) None '菜单取消'
$h.Reset();Equal $h.Active $false '离岛复位'
Equal ([KingdomAdvisor.ResourceRules]::Addition(50,100)) 150 '添加余额'
foreach($amount in @(0,-1,10001)){try{[void][KingdomAdvisor.ResourceRules]::Addition(0,$amount);throw '未拒绝无效数量'}catch{if($_.Exception.Message -eq '未拒绝无效数量'){throw}}}
try{[void][KingdomAdvisor.ResourceRules]::Addition([int]::MaxValue,1);throw '未拒绝溢出'}catch{if($_.Exception.Message -eq '未拒绝溢出'){throw}}
$ids=@([KingdomAdvisor.OptionGroups]::Items|ForEach-Object {$_|ForEach-Object {$_.Id}})
Equal ($ids|Select-Object -Unique).Count ([Enum]::GetValues([KingdomAdvisor.OptionId]).Count) '配置覆盖且唯一'
'通过：短按、长按一次触发、取消锁存、释放复位、资源边界、配置覆盖。'
