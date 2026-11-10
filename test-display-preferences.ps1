#Requires -Version 7.0
$ErrorActionPreference='Stop'
$controller=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'src/Controller.cs'))
$methods=([regex]::Match($controller,'public MapPoint\[\] FilteredPoints[^\r\n]+').Value)+"`n"+([regex]::Match($controller,'public bool MapPointVisible[^\r\n]+').Value)+"`n"+([regex]::Match($controller,'public bool PointVisible\(MapPoint p\)\s*\{[^}]+\}').Value)
$harness=@"
namespace KingdomAdvisor {
 public class TestFlag {public bool Value=true;}
 public class TestSettings {public TestFlag Trees=new(),MapMounts=new(),MapCamps=new(),MapBuildings=new(),MapSpecial=new(),FullMap=new();}
 public class DisplayQueryHarness {
 public bool Open;public int Tab;public string PointCatalogKey="",Category="全部",temporaryMapPoint="";
 public TestSettings settings=new();public MapDisplayPreferences DisplayPreferences=new("wall,tower,farmhouse,berry");
 public static string PointKey(MapPoint p)=>p.Name+"|"+Math.Round(p.X,1);
 $methods
 }
}
"@
$sources=@('Model.cs','AssistModel.cs') | ForEach-Object {[IO.File]::ReadAllText((Join-Path $PSScriptRoot "src/$_"))}
$using=@('using System;','using System.Linq;','using System.Collections.Generic;','using System.Text.RegularExpressions;') -join "`n"
$body=($sources | ForEach-Object {[regex]::Replace($_,'(?m)^using [^;]+;\r?\n','')}) -join "`n"
Add-Type -TypeDefinition ($using+"`n"+$body+"`n"+$harness)
function Check($condition,$label){if(!$condition){throw $label}}
$p=[KingdomAdvisor.MapDisplayPreferences]::new('')
foreach($key in @('wall','tower','farmhouse','berry')){$p.Set($key,$false)}
foreach($key in @('wall','tower','farmhouse','berry')){Check (!$p.Visible($key)) "未隐藏 $key"}
foreach($key in @('hornwall','firetower','ballista','knighttower','stable','castle','beggarcamp')){Check ($p.Visible($key)) "误隐藏 $key"}
$q=[KingdomAdvisor.MapDisplayPreferences]::new($p.Encode())
Check (!$q.Visible('berry')) '重启后偏好丢失'
$q.Set('wall',$true)
Check ($q.Visible('wall') -and !$q.Visible('tower')) '单项恢复影响其他类型'
$r=[KingdomAdvisor.MapDisplayPreferences]::new(' futuretype ,wall,wall,,')
Check ($r.Encode() -eq 'futuretype,wall') '未知类型保留或配置规范化失败'
Check ([KingdomAdvisor.MapDisplayPreferences]::new('').Visible('wall')) '默认隐藏了现有内容'
foreach($entry in [KingdomAdvisor.Catalog]::Entries){
 $items=@([KingdomAdvisor.OptionGroups]::Items | ForEach-Object {$_} | Where-Object MapKey -eq $entry.Key)
 Check ($items.Count -eq 1) "类型选项缺失或重复 $($entry.Key)"
}
foreach($percent in @(25,50,75,100)){
 $alpha=[KingdomAdvisor.PanelAppearance]::Alpha(1,$percent)
 Check ([Math]::Abs($alpha-$percent/100.0) -lt 0.00001) "透明度错误 $percent"
}
Check ([Math]::Abs([KingdomAdvisor.PanelAppearance]::Alpha(0.94,-1)-0.94) -lt 0.00001) '默认外观不兼容'
$s=[KingdomAdvisor.Snapshot]::new();$player=[KingdomAdvisor.PlayerInfo]::new();$player.Id=0
$wall=[KingdomAdvisor.MapPoint]::new();$wall.Key='wall';$wall.X=10
$t=[KingdomAdvisor.TargetInfo]::new();$t.Health=20;$t.HealthMax=100
$wall.PlayerStates[0]=$t;$s.Points.Add($wall)
Check ([KingdomAdvisor.AdvisorAlerts]::Defense($s,$player).Contains('耐久')) '完整快照的城墙提醒失效'
'通过：四种目标独立隐藏、改造设施保留、配置往返、未知键保留、全部类型选项、透明度与默认值、防线提醒。'
$h=[KingdomAdvisor.DisplayQueryHarness]::new();$h.Open=$true;$h.Tab=3
$wall.Name='城墙'
Check ($h.FilteredPoints($s,$player).Count -eq 0) '普通地图未隐藏目标'
$h.Tab=1
Check ($h.FilteredPoints($s,$player).Count -eq 1) '地点查询被地图筛选误伤'
$h.Tab=3;$h.temporaryMapPoint='城墙|10'
Check ($h.FilteredPoints($s,$player).Count -eq 1) '临时定位目标不可选择'
Check (!$h.MapPointVisible($wall,$false)) '临时定位泄漏到 HUD'
$h.settings.FullMap.Value=$false
Check ($h.FilteredPoints($s,$player).Count -eq 0) '临时定位绕过探索范围'
$h.settings.FullMap.Value=$true;$h.Open=$false
Check ($h.FilteredPoints($s,$player).Count -eq 0) '关闭面板后临时定位泄漏'
$h.Open=$true;$h.Tab=3;$h.temporaryMapPoint=''
Check ($h.FilteredPoints($s,$player).Count -eq 0) '清除临时定位无效'
$h.Tab=1;$h.PointCatalogKey='berry'
Check ($h.FilteredPoints($s,$player).Count -eq 0) '查询丢失类型范围'
foreach($pattern in @('public void Close(){temporaryMapPoint=""','public void ChangeTab(int direction){temporaryMapPoint=""','temporaryMapPoint="";Open=true;MapFocus=false;Tab=tab')){Check ($controller.Contains($pattern)) '临时目标生命周期清理缺失'}
'通过：生产查询方法覆盖隐藏、主动查询、临时定位、HUD隔离、探索限制、关闭与清理、类型范围。'
# 滑动条数值域、拖动映射与默认兼容。
foreach($value in 0..100){
 Check ([KingdomAdvisor.PanelAppearance]::SliderValue($value) -eq $value) "任意整数不透明度被量化: $value"
 Check ([KingdomAdvisor.PanelAppearance]::FromPosition(20+$value*4,20,400) -eq $value) "轨道映射错误: $value"
 Check ([KingdomAdvisor.PanelAppearance]::Step($value,1) -eq [Math]::Min(100,$value+1)) '向右微调'
 Check ([KingdomAdvisor.PanelAppearance]::Step($value,-1) -eq [Math]::Max(0,$value-1)) '向左微调'
}
Check ([KingdomAdvisor.PanelAppearance]::FromPosition(-20,20,400) -eq 0) '左越界'
Check ([KingdomAdvisor.PanelAppearance]::FromPosition(500,20,400) -eq 100) '右越界'
Check ([KingdomAdvisor.PanelAppearance]::SliderValue(-1) -eq 94) '默认起点'
Check ([KingdomAdvisor.PanelAppearance]::Step(-1,-1) -eq 93) '默认向左调节'
Check ([KingdomAdvisor.PanelAppearance]::Step(-1,1) -eq 95) '默认向右调节'
foreach($value in @(25,50,75,100)){Check ([KingdomAdvisor.PanelAppearance]::SliderValue($value) -eq $value) '旧档位保持'}
Check ([KingdomAdvisor.PanelAppearance]::Alpha(1,0) -eq 0) '完全透明'
Check ([KingdomAdvisor.PanelAppearance]::Alpha(1,100) -eq 1) '完全不透明'
Check ($controller.Contains('if(OpacityEditing){if(Choice==0)SetOpacity(PanelAppearance.Step(OpacityValue,direction));return;}')) '调节时左右必须截断分类导航'
Check ($controller.Contains('if(OpacityEditing){Choice=Math.Clamp(Choice+delta,0,2);return;}')) '调节时上下必须仅切换控件'
Check ($controller.Contains('if(OpacityEditing){if(Choice==1)SetOpacity(-1);else SettingsDropdown=false;return;}')) '默认恢复与完成路由'
'通过：滑动条101个整数值、拖动映射、边界、1%步进、默认起点、旧值、透明端点及输入路由静态检查。'
