#Requires -Version 7.0
$ErrorActionPreference='Stop'
Add-Type -TypeDefinition ([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'src/Model.cs')))
$cases=@{
 'Keep(Clone)'='castle'
 'Keep'='castle'
 'Wreck(Clone)'='boat'
 'Bomb(Clone)'='bomb'
 'Bomb'='bomb'
 'BombBannerLeft'='bombpurchase'
 'BombBannerRight(Clone)'='bombpurchase'
 'PayableBombLeft'='bombpurchase'
 'PayableBombRight'='bombpurchase'
 'BombablePortal(Clone)'='cave'
 'TeleporterRift(Clone)'='teleporter'
 'Portal(Clone)'='portal'
 'Tower Baker(Clone)'='baker'
 'Hermit House Baker(Clone)'='cabin'
 'Hermit House Ballista(Clone)'='cabin'
 'Hermit Baker(Clone)'='hermitbaker'
 'CastleShieldShop(Clone)'='shield'
 'Beggar_Camp_Rubble(Clone)'='beggarcamp'
 'Citizen_House(Clone)'='citizenhouse'
 'oakTree(Clone)'='tree'
 'birchTree(Clone)'='tree'
 'Farmhouse2(Clone)'='farmhouse'
 'Wall4 Wreck(Clone)'='wall'
 'Lighthouse_Stone(Clone)'='lighthouse'
 'GemBankChest'='gembank'
 'SummonBell'='bell'
 'ShelfUnknown(Clone)'='unknown'
 'BrandNewDlcObject(Clone)'='unknown'
}
foreach($case in $cases.GetEnumerator()){
 $entry=[KingdomAdvisor.Catalog]::Resolve($case.Key)
 if($entry.Key -ne $case.Value){throw "分类回归：$($case.Key) → $($entry.Key)，预期 $($case.Value)"}
}
$knownReasons=@('Invalid','InvalidTime','Base','InvalidRegion','NoUpgrade','HermitLocked','StoneTechRequired','IronTechRequired','NeedsPassengerBallista','NeedsPassengerKnight','NeedsPassengerHorse','NeedsPassengerBaker','NeedsPassengerHorn','NeedsPassengerPersephone','NeedsPassengerFire','FleetBoatRequired','TrojanHorseWheelsMissing','AlreadyCarryingCargo','TrojanHorseCrownMissing','SerpentAtGate','GoldNuggetMissing','NotLocked')
foreach($reason in $knownReasons){if([string]::IsNullOrEmpty([KingdomAdvisor.Catalog]::Lock($reason))){throw "锁定说明为空：$reason"}}
if(![KingdomAdvisor.Catalog]::Lock('NewReason').Contains('NewReason')){throw '未知锁定原因被静默吞掉'}
if([KingdomAdvisor.Catalog]::Currency('Gems') -ne '宝石'){throw '宝石货币映射错误'}
if([KingdomAdvisor.Catalog]::Currency('FutureCurrency') -ne 'FutureCurrency'){throw '未知货币被错误映射'}
if(([KingdomAdvisor.Catalog]::Entries | Group-Object Key | Where-Object Count -gt 1)){throw '重复图鉴键'}
"通过：$($cases.Count) 个分类回归、$($knownReasons.Count) 个锁定分支、未知条件与货币行为、图鉴键唯一性。"
