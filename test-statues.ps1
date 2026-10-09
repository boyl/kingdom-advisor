#Requires -Version 7.0
$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $PSScriptRoot 'src/Model.cs')
$cases=@{'Statue Archer(Clone)'='statuearcher';'Statue Worker(Clone)'='statueworker';'Statue Farmer(Clone)'='statuefarmer';'Statue Knight(Clone)'='statueknight'}
foreach($raw in $cases.Keys){foreach($type in @('Statue','TimedStatue')){if([KingdomAdvisor.Catalog]::ResolveNative($raw,$type).Key -ne $cases[$raw]){throw "雕像具体身份丢失：$raw / $type"}}}
'通过：四种雕像名称与原生类型覆盖冲突。'
foreach($type in @('Statue','TimedStatue')){
 $expected=@{Archer='statuearcher';Worker='statueworker';Farmer='statuefarmer';Knight='statueknight'}
 foreach($deity in $expected.Keys){if([KingdomAdvisor.Catalog]::ResolveStatue('Statue Farmer(Clone)',$type,$deity).Key -ne $expected[$deity]){throw '原生身份必须覆盖冲突名称'}}
 foreach($deity in @('Time','Total','Future')){$e=[KingdomAdvisor.Catalog]::ResolveStatue('Mystery Shrine',$type,$deity);if($e.Key -ne 'statue' -or $e.Name -ne 'Mystery Shrine' -or !$e.Description.Contains('尚未核实')){throw '未知身份不得猜测增益'}}
}
if([KingdomAdvisor.Catalog]::ResolveNative('Statue Farmer(Clone)','TimeStatue').Key -ne 'hourglass'){throw '计时雕像不得套用农民增益'}
'通过：原生身份优先、限时派生类型、未知枚举及计时雕像不伪造增益。'
