#Requires -Version 7.0
$ErrorActionPreference='Stop'
$source=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'src/Controller.cs'))
$methods=(@('public void SetOpacity','private void ApplyOpacity','public int GetOpacity','public int GetDisplayedOpacity') | ForEach-Object {[regex]::Match($source,[regex]::Escape($_)+'[^\r\n]+').Value}) -join "`n"
$harness=@"
namespace KingdomAdvisor {
 public class IntSetting {public int Value=-1;}
 public class BoolSetting {public bool Value=true;}
 public class AppearanceSettings {public IntSetting PanelOpacity=new();public Dictionary<OptionId,IntSetting> PanelOpacities=new();}
 public class AppearanceHarness {
 public AppearanceSettings settings=new();public OptionItem[] SettingsItems;public int Focus;
 public Dictionary<OptionId,BoolSetting> Backgrounds=new();
 public BoolSetting Boolean(OptionId id)=>Backgrounds[id];
 public AppearanceHarness(){foreach(var id in new[]{OptionId.MapOpacity,OptionId.AdvisorOpacity,OptionId.PlayerOpacity,OptionId.AlertOpacity,OptionId.InterfaceOpacity}){settings.PanelOpacities[id]=new();Backgrounds[PanelAppearance.BackgroundFor(id)]=new();}}
 $methods
 }
}
"@
$sources=@('Model.cs','AssistModel.cs') | ForEach-Object {[IO.File]::ReadAllText((Join-Path $PSScriptRoot "src/$_"))}
$body=($sources | ForEach-Object {[regex]::Replace($_,'(?m)^using [^;]+;\r?\n','')}) -join "`n"
Add-Type -TypeDefinition ("using System;using System.Linq;using System.Collections.Generic;using System.Text.RegularExpressions;`n"+$body+"`n"+$harness)
function Check($condition,$message){if(!$condition){throw $message}}
$targets=@('MapOpacity','AdvisorOpacity','PlayerOpacity','AlertOpacity','InterfaceOpacity')
foreach($target in $targets){
 $h=[KingdomAdvisor.AppearanceHarness]::new();$id=[KingdomAdvisor.OptionId]::$target
 $h.SettingsItems=@([KingdomAdvisor.OptionItem]::new($id,'test'))
 $h.SetOpacity(37)
 foreach($other in $targets){$expected=if($other -eq $target){37}else{-1};Check ($h.GetOpacity([KingdomAdvisor.OptionId]::$other) -eq $expected) "独立值串扰: $target / $other"}
 Check (!$h.Backgrounds[[KingdomAdvisor.PanelAppearance]::BackgroundFor($id)].Value) '调节必须启用背景'
 $h.SetOpacity(-1);$default=if($target -eq 'InterfaceOpacity'){71}else{-1};Check ($h.GetOpacity($id) -eq $default) '恢复默认'
}
$h.SettingsItems=@([KingdomAdvisor.OptionItem]::new([KingdomAdvisor.OptionId]::PanelOpacity,'all'))
foreach($value in @(0,63,100,-1)){
 $h.SetOpacity($value)
 foreach($target in $targets){
  $id=[KingdomAdvisor.OptionId]::$target
  $stored=if($value -lt 0 -and $target -eq 'InterfaceOpacity'){71}else{$value};Check ($h.GetOpacity($id) -eq $stored) '统一应用漏项'
  $expected=$value -lt 0 -and $target -ne 'InterfaceOpacity'
  Check ($h.Backgrounds[[KingdomAdvisor.PanelAppearance]::BackgroundFor($id)].Value -eq $expected) '统一背景模式/默认错误'
 }
}
$all=[KingdomAdvisor.OptionGroups]::Items
$appearance=[KingdomAdvisor.OptionGroups]::AppearanceSection
foreach($id in @('Background','AdvisorBackground','PlayerBackground','AlertBackground','InterfaceBackground')+$targets+@('PanelOpacity')){
 $found=@(for($i=0;$i -lt $all.Length;$i++){foreach($item in $all[$i]){if($item.Id.ToString() -eq $id){$i}}})
 Check ($found.Count -eq 1 -and $found[0] -eq $appearance) "外观设置分散/重复: $id"
}
$common=@([KingdomAdvisor.MapFilterMenu]::Items(0) | Where-Object MapKey | ForEach-Object MapKey)
foreach($key in @('wall','tower','farmhouse','berry')){Check ($common -contains $key) '常用标记遗漏'}
$categories=[KingdomAdvisor.MapFilterMenu]::Categories
foreach($index in 2..($categories.Length-1)){
 foreach($item in [KingdomAdvisor.MapFilterMenu]::Items($index)){
  if($item.MapKey){$entry=[KingdomAdvisor.Catalog]::Entries | Where-Object Key -eq $item.MapKey;Check ($entry.Category -eq $categories[$index]) '跨类别批量误操作'}
 }
}
foreach($category in @('建设','防御','经济','工具','环境')){Check (![KingdomAdvisor.MapFilterMenu]::LegacyVisible($category,$false,$true,$true,$true)) '旧建筑总开关迁移错误'}
Check (![KingdomAdvisor.MapFilterMenu]::LegacyVisible('人口',$true,$false,$true,$true)) '人口迁移'
Check (![KingdomAdvisor.MapFilterMenu]::LegacyVisible('坐骑',$true,$true,$false,$true)) '坐骑迁移'
Check (![KingdomAdvisor.MapFilterMenu]::LegacyVisible('科技',$true,$true,$true,$false)) '特殊地点迁移'
'通过：生产透明度方法五项隔离、统一应用、自动启用与默认恢复；外观集中唯一；常用标记、分类范围与旧大类转换。'
# 回归：旧绘制直接将默认哨兵-1投影成94，忽略透明背景模式。
$h.SettingsItems=@([KingdomAdvisor.OptionItem]::new([KingdomAdvisor.OptionId]::MapOpacity,'map'))
$h.SetOpacity(-1)
Check ($h.GetDisplayedOpacity([KingdomAdvisor.OptionId]::MapOpacity) -eq 0) '无背景地图恢复默认后滑动条必须显示0%'
$h.SettingsItems=@([KingdomAdvisor.OptionItem]::new([KingdomAdvisor.OptionId]::PanelOpacity,'all'));$h.SetOpacity(-1)
foreach($target in $targets){$expected=if($target -eq 'InterfaceOpacity'){71}else{0};Check ($h.GetDisplayedOpacity([KingdomAdvisor.OptionId]::$target) -eq $expected) "默认显示数值错误 $target"}
Check ($h.GetDisplayedOpacity([KingdomAdvisor.OptionId]::PanelOpacity) -eq -1) '不同默认值应显示混合，不能伪装为统一94%'
$h.SetOpacity(42)
foreach($target in $targets){Check ($h.GetDisplayedOpacity([KingdomAdvisor.OptionId]::$target) -eq 42) '自定义显示值'}
Check ($h.GetDisplayedOpacity([KingdomAdvisor.OptionId]::PanelOpacity) -eq 42) '统一值显示'
$h.Backgrounds[[KingdomAdvisor.OptionId]::Background].Value=$true
Check ($h.GetDisplayedOpacity([KingdomAdvisor.OptionId]::MapOpacity) -eq 0) '关闭背景时不得显示记忆数值42'
Check ($h.GetOpacity([KingdomAdvisor.OptionId]::MapOpacity) -eq 42) '显示修复不应覆盖记忆数值'
Check ([KingdomAdvisor.PanelAppearance]::Step($h.GetDisplayedOpacity([KingdomAdvisor.OptionId]::MapOpacity),1) -eq 1) '从无背景微调应从0到1'
'通过：默认信息面板0/主界面71、混合态、统一值、背景模式切换、保留记忆值、微调起点。'
