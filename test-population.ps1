#Requires -Version 7.0
$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $PSScriptRoot 'src/Model.cs')
$s=[KingdomAdvisor.Snapshot]::new()
$s.Pikemen=3;$s.Berserkers=2;$s.Peasants=4;$s.Ninjas=5;$s.Fishers=1;$s.StableKeepers=1;$s.Hermits=2
$rows=[KingdomAdvisor.PopulationPresentation]::Rows($s)
foreach($pair in @(@('长枪兵',3),@('狂战士',2),@('待业平民',4),@('忍者',5),@('渔夫',1),@('马厩管理员',1),@('隐士',2))){if(($rows | Where-Object {$_.Item2 -eq ($pair[0]+' '+$pair[1])}).Count -ne 1){throw ('遗漏或重复: '+$pair[0])}}
$empty=[KingdomAdvisor.PopulationPresentation]::Rows([KingdomAdvisor.Snapshot]::new())
if($empty.Count -ne 5){throw '零值专属单位应隐藏，基础五类保留'}
'通过：基础人口与七类补充单位、零值隐藏、每类唯一。'
