#Requires -Version 7.0
param([Parameter(Mandatory)][string]$AssetIndex,[Parameter(Mandatory)][string]$NativeDump,[Parameter(Mandatory)][string]$OutputDirectory,[string]$GamePath)
$ErrorActionPreference='Stop'
if(!$GamePath){$GamePath=Join-Path (Get-ItemProperty 'HKCU:/Software/Valve/Steam').SteamPath 'steamapps/common/Kingdom Two Crowns'}
Add-Type -Path (Join-Path $GamePath 'BepInEx/core/Mono.Cecil.dll')
Add-Type -Path (Join-Path $PSScriptRoot 'src/Model.cs')
$assembly=[Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $GamePath 'BepInEx/interop/Assembly-CSharp.dll'))
$types=@{};foreach($t in $assembly.MainModule.Types){$types[$t.Name]=$t}
function Derives($type,$base){while($type){if($type.Name -eq $base){return $true};if(!$type.BaseType){break};$type=$types[$type.BaseType.Name]};return $false}
$candidates=[Collections.Generic.HashSet[string]]::new()
foreach($t in $assembly.MainModule.Types){if((Derives $t 'Payable') -or (Derives $t 'WorkableBuilding')){[void]$candidates.Add($t.Name)}}
# 原生接口不会完整保留在互操作程序集；用同一游戏构建的原生元数据补齐组件式交互入口。
foreach($line in [IO.File]::ReadLines($NativeDump)){if($line -match '^public class (\w+) : .*IPayableComponentOwner'){[void]$candidates.Add($Matches[1])}}
foreach($name in [KingdomAdvisor.Catalog]::StructureTypes){[void]$candidates.Add($name)}
foreach($name in @('EraUpgradeBuilding','QuestLocationTag')){[void]$candidates.Add($name)}
$assets=Get-Content -LiteralPath $AssetIndex -Raw|ConvertFrom-Json
$dispositions=@{
 Payable='通用基类';PayableComponent='通用交互：由同本体专用组件或精确资源名称分发';PayableUpgrade='通用升级：按资源名称/升级目标识别';WorkableBuilding='通用建筑：按专用组件、名称及建造状态分发';EraUpgradeBuilding='科技变体：按已核实的采石场、矿场、北欧木材资源名称识别';
 PayableShop='商店：运行时商品类型分发';PayableSidedShop='商店：运行时商品与侧边商店枚举分发';Cabin='隐士类型枚举分发';Hermit='人物，原生类型分发';Persephone='人物，保留既有原生人物分发';Steed='坐骑枚举分发';SteedSpawn='生成点内坐骑枚举分发';Statue='原生神祇枚举分发';TimedStatue='原生神祇枚举分发';PayableHorn='号角墙：专用读取器识别';
 PayablePlayer='排除：玩家内部交互，不是建筑';PayableXbox='保留未知：平台交互类，本机资源无实例';CurrencyBagPayable='排除建筑：货币袋物件';HermesShade='排除建筑：任务幽魂';PlayerCargo='排除建筑：搬运物件；已确认神像供品另用精确名称识别';StagSpawnerHelper='排除建筑：坐骑生成辅助';
 Telescope='证据不足：只有原生类，本机资源无实例';GreedGemTrader='证据不足：只有原生类，本机资源无实例';
 AssembledBuildProject='通用组装工程：按最终设施组件及已确认资源名称识别';PayableAssembledBuildSpawner='内部组装生成入口：以生成后的设施身份为准';QuestLocationTag='排除：任务定位标签，不能单独确定设施身份';FireTower='既有专用读取器：火焰塔组件';
}
$typeRows=foreach($name in ($candidates|Sort-Object)){
 $instances=@($assets|Where-Object components -Contains $name)
 $entry=[KingdomAdvisor.Catalog]::ResolveStructureType($name)
 if(!$entry){$entry=[KingdomAdvisor.Catalog]::ResolveNative($name,$name)}
 $disposition=if($dispositions.ContainsKey($name)){$dispositions[$name]}elseif($entry.Key -ne 'unknown'){'明确类型或既有名称识别'}else{throw "候选类型尚未审查：$name"}
 [ordered]@{type=$name;entry=$entry.Key;disposition=$disposition;resourceNames=@($instances.name|Sort-Object -Unique)}
}
$seen=[Collections.Generic.HashSet[string]]::new()
$rows=foreach($r in $assets){
 $matched=@($r.components|Where-Object {$candidates.Contains($_)})
 if(!$matched.Count){continue}
 $identity=$r.name+'|'+(($matched|Sort-Object)-join ',');if(!$seen.Add($identity)){continue}
 $entry=$null
 foreach($c in $r.components){$entry=[KingdomAdvisor.Catalog]::ResolveStructureType($c);if($entry){break}}
 if(!$entry){$payable=$matched|Where-Object {$types.ContainsKey($_) -and (Derives $types[$_] 'Payable')}|Select-Object -First 1;$entry=[KingdomAdvisor.Catalog]::ResolveNative($r.name,$payable)}
 [ordered]@{raw=$r.name;components=$matched;entry=$entry.Key;resource=$r.file}
}
New-Item -ItemType Directory -Force -Path $OutputDirectory|Out-Null
$report=[ordered]@{date='2026-11-12';gameAssemblyHash=(Get-FileHash (Join-Path $GamePath 'GameAssembly.dll')).Hash;assetIndexHash=(Get-FileHash $AssetIndex).Hash;resourceGameObjects=$assets.Count;candidateTypes=$typeRows.Count;distinctCandidates=$rows.Count;types=$typeRows;resources=$rows}
$report|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputDirectory 'structure-coverage.json')
"完成：$($assets.Count)个带脚本资源对象；$($typeRows.Count)种候选类型已逐类标记；$($rows.Count)种资源名称/组件组合。静态覆盖不等于DLC实机验收。"
