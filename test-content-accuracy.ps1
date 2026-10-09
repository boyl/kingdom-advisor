#Requires -Version 7.0
$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $PSScriptRoot 'src/Model.cs')
$s=[KingdomAdvisor.Snapshot]::new();$s.Squires=3;$s.Knights=2
$rows=[KingdomAdvisor.PopulationPresentation]::Rows($s)
if(!($rows.Item2 -contains '侍从 3') -or !($rows.Item2 -contains '骑士 2')){throw '侍从与骑士必须分别显示'}
if([KingdomAdvisor.Catalog]::ResolveNative('Tower Baker(Clone)','Tower').Key -ne 'baker'){throw '基础塔类型不得覆盖面包改造'}
if([KingdomAdvisor.Catalog]::ResolveNative('Tower Knight(Clone)','Tower').Key -ne 'knighttower'){throw '骑士塔不得当成普通弓手塔'}
if(![KingdomAdvisor.Catalog]::Resolve('Forge(Clone)').Description.Contains('侍从')){throw '锻造必须说明晋升关系'}
if(![KingdomAdvisor.Catalog]::Resolve('CastleShieldShop(Clone)').Description.Contains('侍从')){throw '军队招募必须说明基础队长'}
'通过：职业拆分、改造塔、装备晋升说明。'
foreach($needsArmor in @($true,$false)){ $expected=if($needsArmor){'squire'}else{'knight'};if([KingdomAdvisor.LeaderRole]::Classify($needsArmor) -ne $expected){throw '队长晋升资格分类失败'}}
if([KingdomAdvisor.Catalog]::ResolveShop('Forge(Clone)','Forge','DroppableArmor').Key -ne 'sword'){throw '实际出售晋升装备必须优先'}
foreach($type in @('ShieldShopLeft','ShieldShopRight')){if([KingdomAdvisor.Catalog]::ResolveShop('CastleShieldShop(Clone)',$type,'DroppableTool').Key -ne 'shieldshop'){throw '北欧盾牌装备不得伪装成队长招募'}}
foreach($pair in @(@('Horse','hermitstable'),@('Horn','hermithorn'),@('Ballista','hermitballista'),@('Baker','hermitbaker'),@('Knight','hermitknight'),@('Fire','hermitfire'))){$e=[KingdomAdvisor.Catalog]::ResolveHermit('UnknownPrefab',$pair[0],$false);if($e.Key -ne $pair[1]){throw '隐士原生身份错配'}}
if([KingdomAdvisor.Catalog]::ResolveHermit('Hermit Baker','Total',$false).Description -notmatch '尚未核实'){throw '未知隐士不得伪装成面包师'}
if([KingdomAdvisor.Catalog]::Resolve('StoneMine(Clone)').Key -ne 'quarry' -or [KingdomAdvisor.Catalog]::Resolve('IronMine(Clone)').Key -ne 'mine'){throw '石矿铁矿科技阶段混淆'}
if([KingdomAdvisor.Catalog]::ResolveSteed('Griffin','P1Default').Key -ne 'horse'){throw '坐骑实际类型不得被名称覆盖'}
if([KingdomAdvisor.Catalog]::ResolveStatue('Statue Farmer','Statue','Pike').Key -ne 'statuepike'){throw '重装步兵雕像原生身份错误'}
'通过：装备、盾牌主题变体、六类隐士、坐骑身份、科技阶段和步兵雕像。'
if([KingdomAdvisor.Catalog]::Resolve('Wreck (1)(Clone)').Key -ne 'boat'){throw '带实例编号的船只残骸漏识别'}
