#Requires -Version 7.0
$ErrorActionPreference='Stop'
Add-Type -TypeDefinition ([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'src/Model.cs')))
function Enemy($id,$kind,$x){$e=[KingdomAdvisor.EnemyPoint]::new();$e.Id=$id;$e.Kind=$kind;$e.X=$x;return $e}
$items=[KingdomAdvisor.EnemyPoint[]]@((Enemy 1 '普通' 0),(Enemy 2 '普通' 5),(Enemy 3 '普通' 11),(Enemy 4 '巨人' 5))
$groups=[KingdomAdvisor.EnemyClusters]::Group($items,10)
if($groups.Count -ne 3){throw '同类聚合、特殊类型隔离或最大跨度失败'}
if(($groups | Measure-Object Count -Sum).Sum -ne 4){throw '聚合丢失敌人'}
if($groups[0].Count -ne 2 -or $groups[0].X -ne 2.5){throw '敌群中心坐标错误'}
$items[1].X=25
$moved=[KingdomAdvisor.EnemyClusters]::Group($items,10)
if($moved.Count -ne 4 -or !($moved | Where-Object X -eq 25)){throw '移动后的拆分和追踪失败'}
$removed=[KingdomAdvisor.EnemyClusters]::Group([KingdomAdvisor.EnemyPoint[]]@($items[0],$items[3]),10)
if(($removed | Measure-Object Count -Sum).Sum -ne 2){throw '死亡移除后的数量错误'}
if([KingdomAdvisor.EnemyClusters]::Group([KingdomAdvisor.EnemyPoint[]]@(),10).Count -ne 0){throw '空集合失败'}
'通过：敌人同类聚合、类型隔离、最大跨度、数量守恒、中心位置、移动拆分、移除与空集合。'
