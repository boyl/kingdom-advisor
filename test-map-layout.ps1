#Requires -Version 7.0
$ErrorActionPreference='Stop'
Add-Type -Path @((Join-Path $PSScriptRoot 'src/Model.cs'),(Join-Path $PSScriptRoot 'src/MapPresentation.cs'))
foreach($width in @(280,760,1800)){
 $requests=[Collections.Generic.List[KingdomAdvisor.MapLabelRequest]]::new()
 for($i=0;$i -lt 78;$i++){
  $r=[KingdomAdvisor.MapLabelRequest]::new();$r.Index=$i;$r.Anchor=($i%20)*$width/19;$r.Width=60+($i%5)*24;$r.Priority=0
  if($i -eq 30){$r.Priority=1000}
  $requests.Add($r)
 }
 $placed=[KingdomAdvisor.MapLabelLayout]::Arrange($requests,$width,4)
 if(!($placed | Where-Object Index -eq 30)){throw '焦点地点未保留'}
 foreach($p in $placed){
  if($p.Left -lt 0 -or $p.Left+$p.Width -gt $width){throw '标签越界'}
  foreach($q in $placed){if($p.Index -eq $q.Index -or $p.Row -ne $q.Row){continue}
   if($p.Left -lt $q.Left+$q.Width -and $q.Left -lt $p.Left+$p.Width){throw '同一行名称重叠'}
  }
 }
}
$point=[KingdomAdvisor.MapPoint]::new();$point.Name='塔楼'
$state=[KingdomAdvisor.TargetInfo]::new();$state.Level=4;$state.Locked=$true;$state.Cost=99;$state.Currency='金币';$state.Description='用于防御';$state.Advice='先扩张'
$point.PlayerStates.Add(0,$state)
$label=[KingdomAdvisor.MapText]::Label($point,0)
if($label -ne '塔楼'){throw "地图语义错误：$label"}
if([KingdomAdvisor.MapText]::Detail($point,0) -ne '塔楼 · 等级 4 · 锁定'){throw '聚焦状态错误'}
$state.UnderConstruction=$true;$state.Construction=45
if(![KingdomAdvisor.MapText]::Detail($point,0).Contains('施工 45%')){throw '缺少施工状态'}
$point.CampPeople=3
if([KingdomAdvisor.MapText]::Label($point,0) -ne '塔楼 · 3人'){throw '营地数量错误'}
if([KingdomAdvisor.MapText]::Label($point,0,2) -ne '塔楼 ×2 · 3人'){throw '同类数量丢失'}
if([KingdomAdvisor.MapText]::Detail($point,0).Contains('99')){throw '聚焦状态泄露费用'}
'通过：密集名称避让、边界、焦点优先；常驻名称和数量，聚焦显示状态，排除费用、作用与建议。'

$extent=[KingdomAdvisor.MapExtent]::Fit([float[]]@(100,200,400),-200,500)
if($extent.Item1 -ne 88 -or $extent.Item2 -ne 412){throw '有效范围未裁掉左侧空地'}
$extent=[KingdomAdvisor.MapExtent]::Fit([float[]]@(-180,100,400),-200,500)
if($extent.Item1 -gt -180){throw '敌人或玩家在左侧时被裁掉'}
$extent=[KingdomAdvisor.MapExtent]::Fit([float[]]@(),-200,500)
if($extent.Item1 -ne -200 -or $extent.Item2 -ne 500){throw '空地图范围错误'}
'通过：空地裁剪、边缘玩家与敌人保留、空地图范围。'
$stable=[KingdomAdvisor.StableMapExtent]::new()
if($null -ne $stable.Update([float[]]@(-100,100),-200,200,0)){throw '加载首帧不得呈现未稳定坐标'}
if($null -ne $stable.Update([float[]]@(-150,100),-200,200,1)){throw '加载范围变化应重置等待'}
$fixed=$stable.Update([float[]]@(-150,100),-200,200,2.6)
if($null -eq $fixed){throw '稳定范围应就绪'}
foreach($positions in @(@(-500,500),@(-1,1),@(-150,100,130))){$extent=$stable.Update([float[]]$positions,-1000,1000,3);if($extent.Item1 -ne $fixed.Item1 -or $extent.Item2 -ne $fixed.Item2){throw '刷新不得改变已冻结坐标系'}}
$stable.Reset()
if($null -ne $stable.Update([float[]]@(0,50),-200,200,5)){throw '换岛必须重新等待数据稳定'}
'通过：加载范围变化、稳定等待、刷新坐标冻结与换岛复位。'
$wall=[KingdomAdvisor.MapPoint]::new();$wall.Name='城墙';$wall.X=51
if([KingdomAdvisor.MapGrouping]::Cell($wall.X,0,24) -ne 2){throw '固定格归属错误'}
if([KingdomAdvisor.MapGrouping]::Cell(47.99,0,24) -ne 1 -or [KingdomAdvisor.MapGrouping]::Cell(48,0,24) -ne 2){throw '聚合边界错误'}
if([KingdomAdvisor.MapGrouping]::Cell(-1,0,24) -ne -1){throw '负坐标聚合应使用floor'}
if([KingdomAdvisor.MapGrouping]::Priority($wall) -ne 0){throw '普通建筑优先级错误'}
$wall.CampPeople=2
if([KingdomAdvisor.MapGrouping]::Priority($wall) -ne 100){throw '招募地点优先级错误'}
'通过：固定岛屿聚合边界、静态优先级与负坐标。'
