#Requires -Version 7.0
$ErrorActionPreference='Stop'
$source=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'src/HudTextLayout.cs') -Raw
# 测量替身覆盖不同换行结果；真实 GUIStyle 字体测量和画面仍需游戏内验收。
$harness=@"
public static class HudLayoutCases {
 static void Check(bool value,string name){if(!value)throw new Exception(name);}
 static float Measure(string value)=>value.Length==0?0:(float)Math.Ceiling(value.Length/32.0)*20;
 public static string Run(){
  var original=new[]{"Defense tower for archers with a very long English explanation.","Cost 12 Coins","Available","Health 100 / 100","Upgrade requirement: requires the correct hermit and castle technology.","Next upgrade available","Tower upgrade description"};
  var rows=original.Select(s=>new HudTextRow(s,"tower")).ToArray();
  var full=HudTextLayout.Pages(rows,1000,Measure);
  Check(full.Length==1,"普通详情不应强制分页");
  // 独立预期：三个20px行、两个40px行、一个60px行及一个20px行，加七个5px间隔。
  float rendered=0;foreach(var row in full[0].Rows){Check(row.Height==Measure(row.Text)+5,"每行预算必须包含真实绘制间隔");rendered+=row.Height;}
  Check(full[0].Height==rendered,"实际行末必须等于预算，不能再次少算4px");
  foreach(float height in new[]{45f,85f,140f,300f}){
   var pages=HudTextLayout.Pages(rows,height,Measure);
   Check(pages.All(p=>p.Height<=height),"正文页不能超过可用高度");
   Check(pages.All(p=>p.Rows.Count>0),"非空内容不能产生空页");
   Check(string.Concat(pages.SelectMany(p=>p.Rows).Select(r=>r.Text))==string.Concat(original),"分页不得遗漏或重复英文文字");
  }
  var longText=string.Concat(Enumerable.Repeat("EnglishWithoutSpaces",40))+"🙂结束";
  var longPages=HudTextLayout.Pages(new[]{new HudTextRow(longText)},65,Measure);
  Check(longPages.Length>1&&string.Concat(longPages.SelectMany(p=>p.Rows).Select(r=>r.Text))==longText,"单条超高说明仍可完整读到");
  Check(longPages.SelectMany(p=>p.Rows).All(r=>r.Text.Length==0||(!char.IsLowSurrogate(r.Text[0])&&!char.IsHighSurrogate(r.Text[r.Text.Length-1]))),"不能截断Unicode代理对");
  var blank=HudTextLayout.Pages(new[]{new HudTextRow(""),new HudTextRow("Short")},45,Measure);
  Check(blank.Length==1&&blank[0].Height==30,"空白行留白须参与预算");
  foreach(float viewport in new[]{150f,180f,240f,300f,360f}){
   float available=HudTextLayout.FooterTop(viewport,42,40)-8-16-66-16;
   var result=HudTextLayout.Paged(rows,available,42,20,Measure);
   if(available<20+5+42+8)Check(result.Length==0,"不能容纳一行及翻页时须返回降级状态，不能使HUD异常停用");
   else Check(result.Length>0&&result.All(p=>p.Height+42+8<=available),"小视口可分页时每页仍须在安全区内");
  }
  Check(HudTextLayout.Paged(rows,-120,42,20,Measure).Length==0,"超高标题产生负正文空间时必须明确降级");
  Check(HudTextLayout.Paged(rows,74,42,20,Measure).Length==0,"差一个像素放不下一行时不能调用正文分页");
  Check(HudTextLayout.Paged(rows,75,42,20,Measure).Length>0,"恰好一行时必须保留分页正文");
  Check(HudTextLayout.DayHeader("岛 3 / 第 125 天","黄昏 · 18:26 · 冬季",358,t=>t.Length*8)=="岛 3 / 第 125 天 / 黄昏 · 18:26 · 冬季","短页眉保持原单行与分隔符");
  string date="Island 3 / Day 125",time="Dusk · Game time 18:26 · Winter";
  string dayHeader=HudTextLayout.DayHeader(date,time,358,t=>t.Length*9);
  Check(dayHeader==date+"\n"+time,"受限宽度下长英文优先日期与时间两行，保留完整文字");
  Check(HudTextLayout.DayHeader(date,time,dayHeader.Replace("\n"," / ").Length*9,t=>t.Length*9)==date+" / "+time,"恰好容纳时不额外换行");
  string tallTitle=string.Concat(Enumerable.Repeat("Long English building name ",8));
  float safeCard=HudTextLayout.FooterTop(360,42,40)-8-16;
  Check(!HudTextLayout.CanPage(safeCard-30-Measure(tallTitle)-16,42,20),"多行标题应触发短页眉方案");
  var titleRows=new[]{new HudTextRow(tallTitle)}.Concat(rows).ToArray();
  var titlePages=HudTextLayout.Paged(titleRows,safeCard-66-16,42,20,Measure);
  Check(titlePages.Length>0&&string.Concat(titlePages.SelectMany(p=>p.Rows).Select(r=>r.Text))==tallTitle+string.Concat(original),"转入正文的长标题仍须可分页完整读取");
  foreach(float viewport in new[]{360f,480f,720f,1080f})foreach(float hint in new[]{20f,40f,76f}){
   float top=HudTextLayout.FooterTop(viewport,42,hint);
   Check(top+hint+8+42+16==viewport,"设备页脚须按实测高度完整落屏");
   float safeBottom=top-8;
   // 保存为最底部的卡片，依现有Place规则仍须留在安全区内。
   float cardHeight=Math.Min(200,safeBottom-16),cardY=Math.Max(8,safeBottom-cardHeight-8);
   Check(cardY+cardHeight<top,"拖到最底部的卡片不得覆盖设备提示");
  }
  // 2560x1377、英文28号截图场景：警报文字撞地图底部提示；详情右列尚有实际空位。
  var safe=new HudRect(8,8,2544,1254);
  var advisor=new HudRect(18,458,728,804);
  var map=new HudRect(18,18,2524,434);
  var detail=new HudRect(758,458,728,164);
  var desired=new HudRect(18,382,728,64);
  var occupied=new[]{advisor,map,detail};
  Check(desired.Overlaps(map),"回归场景必须确实重现旧默认警报与地图相撞");
  Check(HudTextLayout.TryPlaceAlert(desired,safe,detail,occupied,12,out var placed),"详情下方有空位时不得丢弃警报");
  Check(placed.X==detail.X&&placed.Y==detail.Bottom+12,"地图冲突时优先使用详情下方实测空位");
  Check(occupied.All(item=>!placed.Overlaps(item))&&placed.Bottom<=safe.Bottom,"警报不得覆盖其他卡片或页脚");
  Check(HudTextLayout.TryPlaceAlert(placed,safe,detail,occupied,12,out var unchanged)&&unchanged.X==placed.X&&unchanged.Y==placed.Y,"原位置没有冲突时不得无故移动");
  var lowDetail=new HudRect(758,safe.Bottom-164,728,164);
  Check(HudTextLayout.TryPlaceAlert(desired,safe,lowDetail,new[]{advisor,map,lowDetail},12,out var above)&&above.Y==lowDetail.Y-12-desired.Height,"详情下方被屏边限制时使用上方空位");
  Check(!HudTextLayout.TryPlaceAlert(desired,safe,detail,new[]{safe},12,out _),"视口没有完整空位时警报必须暂不绘制");
  Check(HudTextLayout.TryPlaceAlert(desired,safe,detail,occupied,12,out var restored)&&restored.Y==placed.Y,"空位恢复后警报必须自动恢复");
  var compactSafe=new HudRect(8,8,984,584);
  var compactDetail=new HudRect(350,200,300,100);
  var compactOccupied=new[]{new HudRect(8,8,984,150),compactDetail,new HudRect(350,312,300,70)};
  Check(HudTextLayout.TryPlaceAlert(new HudRect(8,8,300,50),compactSafe,compactDetail,compactOccupied,12,out var gap),"详情上下都受阻时仍须寻找剩余真实空隙");
  Check(compactOccupied.All(item=>!gap.Overlaps(item))&&gap.X>=compactSafe.X&&gap.Y>=compactSafe.Y&&gap.Right<=compactSafe.Right&&gap.Bottom<=compactSafe.Bottom,"空隙搜索不得越界或覆盖已占区域");
  var panel=HudTextLayout.MainPanel(2560,1377);
  Check(panel.X==760&&panel.Y==328&&panel.Width==1040&&panel.Height==720,"主面板保持已有居中几何");
  Check(!HudTextLayout.CardVisible(detail,panel,true),"打开主面板时不能透出相交详情文字");
  Check(!HudTextLayout.CardVisible(placed,panel,true),"打开主面板时不能透出相交警报文字");
  Check(HudTextLayout.CardVisible(advisor,panel,true),"未相交的顾问卡片仍须显示");
  Check(HudTextLayout.CardVisible(detail,panel,false)&&HudTextLayout.CardVisible(placed,panel,false),"关闭主面板后原卡片自动恢复");
  return "通过：生产分页与小视口降级；2560x1377英文28号警报冲突回放、详情上下及剩余空隙、无空位与恢复；主面板共享几何及相交卡片遮挡。";
 }
}
"@
Add-Type -TypeDefinition ("using System;using System.Linq;using System.Collections.Generic;using KingdomAdvisor;`n"+[regex]::Replace($source,'(?m)^using [^;]+;\r?\n','')+$harness)
[HudLayoutCases]::Run()
