#Requires -Version 7.0
$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $PSScriptRoot 'src/Model.cs')
$bear=[KingdomAdvisor.Catalog]::ResolveSteed('Bearcave','Bear')
if($bear.Key -ne 'bear' -or $bear.Description -notmatch '扑击'){throw 'Bear原生坐骑缺少能力说明'}
if([KingdomAdvisor.Catalog]::Resolve('Bearcave').Key -ne 'bear'){throw '熊洞解锁点漏识别'}
'通过：熊坐骑及熊洞解锁点'
$cases=@{Bear='bear';P1Griffin='griffin';P2Griffin='griffin';Lizard='lizard';Stag='stag';P2Stag='stag';Unicorn='unicorn';P1Warhorse='warhorse';P2Warhorse='warhorse';P1Default='horse';P2Default='horse';HorseStamina='drafthorse';HorseBurst='bursthorse';HorseFast='fasthorse';Spookyhorse='undeadhorse';P1Wolf='wolf';P2Wolf='wolf';Reindeer='reindeer';Reindeer_Norselands='reindeer';P2Reindeer_Norselands='reindeer';Trap='beetle';Barrier='golem';Bloodstained='gamigin';Sleipnir='sleipnir';CatCart='catcart';Kelpie='kelpie';P2Kelpie='kelpie';Gullinbursti='goldenboar';DayNight='daynight';Hippocampus='hippocampus';Cerberus='cerberus';Spider='spider';TheChariotDay='chariotday';TheChariotNight='chariotnight';Pegasus='pegasus';Donkey='donkey';MolossianHound='argos';Chimera='chimera';RainbowPony='rainbowpony'}
foreach($kind in $cases.Keys){$e=[KingdomAdvisor.Catalog]::ResolveSteed('WrongName',$kind);if($e.Key -ne $cases[$kind] -or $e.Description -match '未核实'){throw "坐骑漏收录 $kind"}}
foreach($kind in @('INVALID','Total','FutureMount')){if([KingdomAdvisor.Catalog]::ResolveSteed('UnknownMount',$kind).Key -ne 'steed'){throw '无效/未来身份不得伪装成已知坐骑'}}
$pair=[KingdomAdvisor.Catalog]::ResolveSteedSpawn('UnknownCave',[string[]]@('P1Griffin','P2Griffin'))
if($pair.Key -ne 'griffin' -or $pair.Name -notmatch '解锁点'){throw '成对坐骑身份不能丢失'}
$bear=[KingdomAdvisor.Catalog]::ResolveSteedSpawn('Bearcave',[string[]]@('Bear'))
if($bear.Name -ne '熊解锁点' -or $bear.Description -notmatch '扑击'){throw '熊洞应显示解锁用途和坐骑能力'}
$mixed=[KingdomAdvisor.Catalog]::ResolveSteedSpawn('ChariotShrine',[string[]]@('TheChariotDay','TheChariotNight'))
if($mixed.Description -notmatch '太阳战车' -or $mixed.Description -notmatch '月亮战车'){throw '多选坐骑不得丢掉其中一项'}
Add-Type -Path (Join-Path $PSScriptRoot 'src/Localization.cs')
foreach($e in @($pair,$bear,$mixed)){foreach($s in @($e.Name,$e.Description,$e.Advice)){if([KingdomAdvisor.Localization]::Translate($s,$true) -match '[\u4e00-\u9fff]'){throw "动态解锁说明中英文缺失 $s"}}}
'通过：39个实际坐骑枚举、哨兵/未知值、成对及多选解锁点、动态中英文'
# 与安装游戏的真实枚举对照，新增枚举不得静默落入未知分支。
$game=Join-Path (Get-ItemProperty 'HKCU:\Software\Valve\Steam').SteamPath 'steamapps/common/Kingdom Two Crowns'
Add-Type -AssemblyName System.Reflection.Metadata
$stream=[IO.File]::OpenRead((Join-Path $game 'BepInEx/interop/Assembly-CSharp.dll'))
$pe=[System.Reflection.PortableExecutable.PEReader]::new($stream)
try{
 $metadata=[System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
 $nativeNames=@(foreach($handle in $metadata.TypeDefinitions){$type=$metadata.GetTypeDefinition($handle);if($metadata.GetString($type.Name) -eq 'SteedType'){foreach($fieldHandle in $type.GetFields()){$name=$metadata.GetString($metadata.GetFieldDefinition($fieldHandle).Name);if($name -notin 'value__','INVALID','Total'){$name}}}})
 $difference=@(Compare-Object ($cases.Keys|Sort-Object) ($nativeNames|Sort-Object))
 if($difference.Count){throw ('实际坐骑枚举未完整覆盖: '+($difference|Out-String))}
 "通过：本机Assembly-CSharp真实坐骑值域 $($nativeNames.Count) 项与独立预期一致"
}finally{$pe.Dispose();$stream.Dispose()}
