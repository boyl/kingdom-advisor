#Requires -Version 7.0
param([Parameter(Mandatory)][string]$Census,[Parameter(Mandatory)][string]$Runtime)
$ErrorActionPreference='Stop'
$counts=@{worker=0;archer=0;knight=0;farmer=0;beggar=0;pike=0;berserker=0;peasant=0;ninja=0;fisher=0;stable=0}
$characters=@(Import-Csv -LiteralPath $Census -Delimiter "`t" | Where-Object kind -eq character)
if(!$characters.Count){throw '没有真实角色证据'}
foreach($c in $characters){
 $flags=$c.category+' '+$c.entry
 $active=@($counts.Keys|Where-Object {$flags -match ('(^| )'+$_+'=True( |$)')})
 if($active -contains 'pike' -or $active -contains 'ninja'){$active=@($active|Where-Object {$_ -ne 'fisher'})}
 if($active.Count -ne 1){throw ('角色身份缺失或重复: '+$c.raw+' '+$flags)}
 $counts[$active[0]]++
}
$labels=@{worker='工匠';archer='弓手';knight='骑士';farmer='农民';beggar='游民';pike='长枪兵';berserker='狂战士';peasant='待业平民';ninja='忍者';fisher='渔夫';stable='马厩管理员'}
$lines=Get-Content -LiteralPath $Runtime
foreach($role in $counts.Keys){
 $match=@($lines|Where-Object {$_ -match ('^population='+[regex]::Escape($labels[$role])+' \d+$')})
 $displayed=if($match.Count){[int]($match[0] -replace '^.* ','')}else{0}
 if($displayed -ne $counts[$role]){throw ('实机计数不符 '+$labels[$role]+': '+$displayed+' / '+$counts[$role])}
}
[pscustomobject]@{Status='Passed';Characters=$characters.Count;Counts=$counts;Scope='仅当前场景逐角色证据，与实际HUD快照核对'}|ConvertTo-Json -Depth 4
