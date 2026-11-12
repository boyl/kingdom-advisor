#Requires -Version 7.0
$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $PSScriptRoot 'src/Model.cs')
function Check($ok,$label){if(!$ok){throw $label}}
# 独立预期，来自本机资源中的实际GameObject名称与MonoBehaviour组件组合。
$cases=@(
 @('staff','LokiPuzzleStaff','lokipuzzle'),@('water_statues','HeimdallPuzzleHorn','heimdallpuzzle'),@('Payable','HeimdallPuzzlePillar','heimdallpuzzle'),
 @('hammer','ThorPuzzleHammer','thor'),@('pillar1','ThorPuzzlePillar','thor'),@('statue','ThorPuzzleStatue','thor'),@('puzzle_Hel_pillar1_norselands','HelPuzzlePillar','hel'),
 @('Hermes_Dock','HermesDock','hermesdock'),@('Athena_Tree_Greece','GiantGoldenAppleTree','appletree'),@('Athena_Farmhouse2_greece','AthenaFarmhouse','athenafarm'),
 @('Artemis_Cage_Greece','ArtemisCage','artemiscage'),@('Athena_Persephone_Cage_greece','PersephoneCage','persephonecage'),@('Hephaestus_Forge_Built_Greece','HephaestusForge','hephaestusforge'),
 @('TrojanHorse_SummoningBell','TrojanHorseSummoningBell','trojanbell'),@('ShopChangeItem_norselands','ChangeItemOfPowerShop','powershop'),@('ShopChangeItem_greece','ChangeItemOfPowerShop','powershop'),
 @('ShopChangeRuler','ChangeRulerShop','rulershop'),@('Statue Unlock Ruler','UnlockNewRulerStatue','rulerstatue'),@('Mason Shrine Stone','MasonShrine','masonshrine'),
 @('Oracle_Greece','Oracle','oracle'),@('PillarCenter','CerberusPuzzlePillar','cerberuspuzzle'),@('FishingRod','PayableFishingRod','fishingrod'),
 @('Buyback tent','GreedBuybackTrader','buyback'),@('ActiveShieldWallTotem','PayableShieldWallActivator','shieldwall'),@('BBB','PayableBorderBanner','formationbanner'),
 @('Signpost','Signpost','signpost'),@('Snowman','Snowman','snowman'),
 @('BindingStatue_Athena_MtOlympus_Greece','MtOlympusStatue','bindingstatue'),@('CrownStatue_MtOlympus_Greece','PayableCrownStatue','crownstatue'),
 @('StatueDay','ChariotPuzzlePillar','chariotpuzzle'),@('StatueNight','ChariotPuzzlePillar','chariotpuzzle'),
 @('Bunny Statue','BunnyStatue','bunnystatue'),@('Egg Mound','EggMound','eggmound'),@('GreedGolem','GreedGolem','greedgolem'),
 @('Statue_Pushable_Party','PushableStatue','pushablestatue'),@('Bonfire','SpringEdictMonument','monument'),@('MtOlympus_Gates','MtOlympusGates','olympusgates'))
foreach($c in $cases){
 $e=[KingdomAdvisor.Catalog]::ResolveNative($c[0],$c[1]);Check ($e.Key -eq $c[2]) "真实资源身份错误：$($c[0]) / $($c[1]) => $($e.Key)，应为 $($c[2])"
}
$aliases=@(
 @('Artemis_Cage_Unbuilt_Greece','artemiscage'),@('Hephaestus_Forge_Unbuilt_Greece','hephaestusforge'),@('Athena_Farmhouse0_greece','athenafarm'),
 @('BindingStatue_Hermes_Unbuilt_Greece Variant','bindingstatue'),@('CrownStatue_Unbuilt_MtOlympus_Greece','crownstatue'),
 @('Idol_Oracle_greece','questidol'),@('Idol_Artemis_greece','questidol'),@('Bunny_Idol','questidol'),
 @('Logging_tier1_undeveloped_norselands','logging1'),@('Logging_tier2_norselands','logging2'),@('Mason Shrine Wood','masonshrine'),
 @('Temple_Oracle_Unbuilt_Greece','templeoracle'),@('Tholos_Athena_Greece','templeathena'),@('Tholos_Hermes_Unbuilt_Greece','templehermes'),
 @('Tholos_Artemis_Greece','templeartemis'),@('Tholos_Hephaestus_Unbuilt_Greece','templehephaestus'),@('Tholos_Base_Greece','temple'),
 @('Tower_upgrade_Berzerker_norselands','berserkertower'),@('Tower_upgrade_Bread_greece','baker'),@('Tower_upgrade_Mead_norselands','baker'),
 @('Tower_upgrade_Fire_greece','firetower'),@('Wall5_horn_greece','hornwall'),@('Wreck_Greece','boat'),@('ShopBerserker_norselands','berserkershop'))
foreach($c in $aliases){foreach($suffix in @('','(Clone)',' (2)(Clone)')){Check ([KingdomAdvisor.Catalog]::Resolve($c[0]+$suffix).Key -eq $c[1]) "已确认预制体/实例别名漏识别：$($c[0])$suffix"}}
foreach($raw in @('staff','hammer','statue','pillar1','Payable','UnknownFutureBuilding')){
 Check ($null -eq [KingdomAdvisor.Catalog]::ResolveStructureName($raw)) "通用名称不得凭名字猜DLC设施：$raw"
}
Check ([KingdomAdvisor.Catalog]::ResolveNative('ThorPuzzleHammer','PayableTree').Key -eq 'tree') '明确普通原生类型不可被名称覆盖'
Check ([KingdomAdvisor.Catalog]::ResolveStructureType('FutureQuestFacility') -eq $null) '未知专用组件不能默认为某神祇'
foreach($c in $cases){Check ([KingdomAdvisor.Catalog]::ResolveStructureType($c[1]).Key -eq $c[2]) '专用组件判断与原生Payable判断共用语义'}
"通过：$($cases.Count)组实际组件身份、$($aliases.Count)组资源别名×3实例后缀、通用名称冲突、未来组件回退。"
