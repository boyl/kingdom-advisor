#Requires -Version 7.0
param([string]$Census)
$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $PSScriptRoot 'src/Model.cs')
$cases=@{Bomb='bomb';PayableBombLeft='bombpurchase';PayableBombRight='bombpurchase';PayableBombPurchase='bombpurchase';BoatSailPosition='boat';PayableBoat='boat';BoatSummoningBell='bell';Cabin='cabin';CitizenHousePayable='citizenhouse';Hermit='hermit';Merchant='merchant';PayableBush='berry';PayableForge='forge';PayableGemChest='chest';PayableGemGuard='gemguard';PayableShield='shield';PayableTeleporter='teleporter';PayableTree='tree';PayableWorkshop='workshop';PayableWorkshopBarrel='barrel';Steed='steed';SteedSpawn='steed';Wharf='wharf';Statue='statue';TimedStatue='statue';TimeStatue='statue';Castle='castle';Wall='wall';Tower='tower';Farmhouse='farmhouse';Ballista='ballista';Baker='baker'}
foreach($type in $cases.Keys){if([KingdomAdvisor.Catalog]::ResolveNative('UnmappedPrefab(Clone)',$type).Key -ne $cases[$type]){throw ('类型映射缺失 '+$type)}}
if([KingdomAdvisor.Catalog]::ResolveNative('Portal(Clone)','Bomb').Key -ne 'bomb'){throw '名称不得覆盖明确原生身份'}
if([KingdomAdvisor.Catalog]::ResolveNative('Griffin(Clone)','Steed').Key -ne 'griffin'){throw '坐骑具体种类丢失'}
if([KingdomAdvisor.Catalog]::ResolveNative('BrandNewObject','PayableUpgrade').Key -ne 'unknown'){throw '通用组件不得伪造说明'}
if($Census){
 $unmapped=@(Import-Csv -LiteralPath $Census -Delimiter "`t"|Where-Object {$_.kind -eq 'payable' -and $_.nativeType -ne 'PayablePlayer' -and [KingdomAdvisor.Catalog]::ResolveNative($_.raw,$_.nativeType).Key -eq 'unknown'})
 if($unmapped.Count){$unmapped|Select-Object raw,nativeType|ConvertTo-Json;throw '场景存在未收录交互物'}
}
'通过：32个明确原生类型、名称冲突、坐骑子类、通用类型未知保留与提供的场景清单。'
