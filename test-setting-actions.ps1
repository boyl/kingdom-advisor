#Requires -Version 7.0
$ErrorActionPreference='Stop'
$controller=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'src/Controller.cs'))
$branch=[regex]::Match($controller,'if\(Tab==2\)\{var item=SettingsItems\[Focus\];[^\r\n]+').Value
if(!$branch){throw '未找到生产设置激活分支'}
$resourceBranch=[regex]::Match($controller,'if\(Focus==0\|\|Focus==1\|\|Focus==3\|\|Focus==5\)\{[^\r\n]+?return;\}').Value
$plugin=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'src/Plugin.cs'))
$previewBranch=[regex]::Match($plugin,'if\(scenario<8\)\{[^\r\n]+').Value
$opacityExpression=[regex]::Match($controller,'public bool OpacityEditing=>([^;]+);').Groups[1].Value
if(!$previewBranch -or !$opacityExpression){throw '未找到生产预览设置分支或不透明度状态表达式'}
$harness=@"
namespace KingdomAdvisor {
 public class TestLayout {public string Value="custom";}
 public class TestActionSettings {public TestLayout Layout=new();}
 public class ActionHarness {
 public int Tab=2,Section,Focus,Choice,Owner,Calls,OpenedTab=-1;public bool SettingsDropdown,Open=true;
 public OptionItem[] SettingsItems=>OptionGroups.Items[Section];public object current=new();public TestActionSettings settings=new();public OptionId LastAction;
 public void Toggle(int tab,object state,int owner=0){OpenedTab=tab;}
 public bool OpacityEditing=>$opacityExpression;
 public int OptionValue(OptionItem item)=>0;
 public void SetOption(OptionItem item,int value){Calls++;LastAction=item.Id;}
 public void Activate(){ $branch }
public bool ResourceDropdown;public string[] Available=Array.Empty<string>();public string[] ResourceChoices()=>Available;public int ResourceChoiceIndex()=>0;public void ResourceActivate(){ $resourceBranch }
 }
 public class PreviewReader {public object State=new();}
 public class PreviewHarness {
 public ActionHarness Controller=new();public PreviewReader reader=new();
 public void Prepare(int scenario){ $previewBranch }
 public bool ReadOpacityEditing()=>Controller.OpacityEditing;
 }
}
"@
$sources=@('Model.cs','AssistModel.cs') | ForEach-Object {[IO.File]::ReadAllText((Join-Path $PSScriptRoot "src/$_"))}
$body=($sources | ForEach-Object {[regex]::Replace($_,'(?m)^using [^;]+;\r?\n','')}) -join "`n"
Add-Type -TypeDefinition ("using System;using System.Linq;using System.Collections.Generic;using System.Text.RegularExpressions;`n"+$body+"`n"+$harness)
function Check($condition,$message){if(!$condition){throw $message}}
# 直接执行 Plugin 的场景分支，再经过 Controller 的生产状态表达式，复现旧索引的实际崩溃路径。
$preview=[KingdomAdvisor.PreviewHarness]::new();$preview.Prepare(7)
Check (!$preview.ReadOpacityEditing()) '字号预览不应误入不透明度编辑'
$previewItem=$preview.Controller.SettingsItems[$preview.Controller.Focus]
Check ($previewItem.Id.ToString() -eq 'Font' -and $preview.Controller.SettingsDropdown) '字号预览必须定位实际字号设置'
Check ($preview.Controller.Choice -eq $previewItem.Choices.Length-1) '字号预览必须选择实际最后一项'
foreach($scenario in 0..6){
 $preview=[KingdomAdvisor.PreviewHarness]::new();$preview.Prepare($scenario)
 Check ($preview.Controller.Focus -ge 0 -and $preview.Controller.Focus -lt $preview.Controller.SettingsItems.Length) "预览场景 $scenario 设置索引越界"
 if($scenario -eq 6){Check ($preview.Controller.Focus -eq $preview.Controller.SettingsItems.Length-1) '顾问末项预览必须定位实际最后一项'}
}
'通过：生产设置预览分支在重组后正确定位字号下拉与顾问末项。'
function Harness($id){
 $h=[KingdomAdvisor.ActionHarness]::new()
 for($section=0;$section -lt [KingdomAdvisor.OptionGroups]::Items.Length;$section++){
  $items=[KingdomAdvisor.OptionGroups]::Items[$section]
  for($row=0;$row -lt $items.Length;$row++){if($items[$row].Id.ToString() -eq $id){$h.Section=$section;$h.Focus=$row;return $h}}
 }
 throw "设置项缺失 $id"
}
foreach($id in @('ShowAllTypes','ResetTypes')){
 $h=Harness $id;$h.Activate()
 Check ($h.Calls -eq 1 -and !$h.SettingsDropdown -and $h.LastAction.ToString() -eq $id) "$id 应首次确认即执行一次，不应打开单项下拉"
}
$h=Harness ResetLayout;$h.Activate();Check ($h.settings.Layout.Value -eq '' -and !$h.SettingsDropdown) '恢复布局应直接执行'
$h=Harness Resources;$h.Activate();Check ($h.OpenedTab -eq 5 -and !$h.SettingsDropdown) '资源入口应直接打开'
$h=Harness Map;$h.Activate();Check ($h.Calls -eq 0 -and $h.SettingsDropdown) '多值设置应先选择'
$h.Choice=1;$h.Activate();Check ($h.Calls -eq 1 -and !$h.SettingsDropdown) '多值设置确认一次'
$h=Harness AdvisorColumns;$h.Activate();Check ($h.Calls -eq 0 -and $h.SettingsDropdown) '顾问布局先打开单列／两列选择'
$h.Choice=1;$h.Activate();Check ($h.Calls -eq 1 -and !$h.SettingsDropdown -and $h.LastAction.ToString() -eq 'AdvisorColumns') '布局选择确认后生效并关闭下拉'
'通过：生产设置分支直接动作、导航入口、多值选择与确认。'
foreach($group in [KingdomAdvisor.OptionGroups]::Items){foreach($item in $group){
 $expected=switch($item.Id.ToString()){
  {$_ -in 'ShowAllTypes','ResetTypes','ResetLayout','ShowFiltered','HideFiltered'} {'Action'}
  'Resources' {'Navigation'}
  {$_ -in 'PanelOpacity','MapOpacity','AdvisorOpacity','PlayerOpacity','AlertOpacity','InterfaceOpacity'} {'Slider'}
  {$_ -in 'MapType','Enemies','HideNativePurse','ContinuousPurchase'} {'Toggle'}
  default {'Choice'}
 }
 Check ($item.Kind.ToString() -eq $expected) "错误控件类型 $($item.Id)"
 $label=$item.DisplayLabel('50%')
 if($expected -eq 'Action'){Check ($label -eq $item.Name) '动作不得附加执行/下拉'}
 elseif($expected -in 'Navigation','Slider'){Check ($label.EndsWith(' →') -and !$label.Contains('▾')) '入口不得误画成下拉'}
 elseif($expected -eq 'Toggle'){Check ($label.StartsWith('[ ] ') -and !$label.Contains('▾')) '勾选项不应出现下拉'}
 else{Check ($item.Choices.Length -gt 1 -and $label.EndsWith('▾')) '选择项必须有多值'}
}}
foreach($focus in @(0,1,3,5)){
 foreach($count in @(0,1,2,4)){
  $h=[KingdomAdvisor.ActionHarness]::new();$h.Focus=$focus;$h.Available=[string[]]@(for($i=0;$i -lt $count;$i++){"item$i"})
  $h.ResourceActivate()
  Check ($h.ResourceDropdown -eq ($count -gt 1)) "资源选择下拉错误：focus=$focus count=$count"
 }
}
$view=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'src/View.cs'))
Check ($view.Contains('GUI.enabled&&controller.PointerButton')) '禁用控件手柄指针绕过限制'
'通过：所有设置项动作/导航/滑动条/多值分类及标签；生产资源选择分支0/1/2/4选项；禁用手柄命中静态检查。'
foreach($id in @('MapType','Enemies','HideNativePurse','ContinuousPurchase')){$h=Harness $id;$h.Activate();Check ($h.Calls -eq 1 -and !$h.SettingsDropdown) '勾选一次直接生效'}
'通过：地图标记及敌人勾选不弹下拉。'
