#Requires -Version 7.0
$ErrorActionPreference='Stop'
$source=Get-Content (Join-Path $PSScriptRoot 'src/HudTextLayout.cs') -Raw
$view=Get-Content (Join-Path $PSScriptRoot 'src/View.cs') -Raw
$start=$view.IndexOf('float preferredColumnsWidth=')
$end=$view.IndexOf('string heading=', $start)
if($start -lt 0 -or $end -le $start){throw '两列宽度计算边界已改变，需要复核测试。'}
$widthBlock=$view.Substring($start,$end-$start)
$harness=@"
namespace HudColumnTests {
 public sealed class GUIContent { public GUIContent(string text){} }
 public struct Size {public float x;}
 public sealed class Style {public float LabelWidth;public Size CalcSize(GUIContent text)=>new Size{x=LabelWidth};}
 public sealed class Flag {public bool Value;}
 public sealed class Number {public int Value=1;}
 public sealed class Settings {public Flag Status=new Flag{Value=true},ShowPlayerPanel=new Flag{Value=true};public Number AdvisorColumns=new Number();}
 public sealed class Viewport {public float width;}
 public static class Localization {public static string Text(string value)=>value;}
 public static class Cases {
  static int count;
  static void Check(bool ok,string name){count++;if(!ok)throw new Exception(name);}
  static bool ActualColumns(float screenWidth,float labelWidth,bool showPopulation=true,int preference=2){
   var viewport=new Viewport{width=screenWidth};var settings=new Settings();settings.Status.Value=showPopulation;settings.AdvisorColumns.Value=preference;int? PreviewHudColumns=null;
   var small=new Style{LabelWidth=labelWidth};bool stacked=screenWidth<620;float width=Math.Min(screenWidth-36,screenWidth>=900?390:screenWidth*.44f);
   if(stacked)width=screenWidth-36;
   $widthBlock
   return twoColumnStatus;
  }
  public static string Run(){
   Func<string,float> measure=t=>(float)Math.Ceiling(t.Length/20.0)*20;
   var left=Enumerable.Range(0,7).Select(i=>new HudTextRow("Resource "+i,"coin")).ToArray();
   var right=Enumerable.Range(0,9).Select(i=>new HudTextRow("Population "+i,"person")).ToArray();
   var all=HudTextLayout.ColumnPages(left,right,1000,34,20,measure);
   Check(all.Length==1&&all[0].Columns==2,"常规16项须在两列同页显示");
   Check(all[0].Height==225,"独立预期：九行每行20+5，总高度225，而非16行400");
   Check(all[0].Rows.Count==16&&all[0].Rows.Count(r=>r.Column==0)==7&&all[0].Rows.Count(r=>r.Column==1)==9,"条目独立且左右分类完整");
   foreach(float available in new[]{75f,100f,180f,300f}){
    var pages=HudTextLayout.ColumnPages(left,right,available,34,20,measure);
    Check(pages.All(p=>p.Height+(pages.Length>1?42:0)<=available),"两列共用翻页预留，正文不得越界");
    foreach(int column in new[]{0,1}){
     string actual=string.Concat(pages.SelectMany(p=>p.Rows).Where(r=>r.Column==column).Select(r=>r.Text));
     string expected=string.Concat((column==0?left:right).Select(r=>r.Text));
     Check(actual==expected,"不等长双列分页不得遗漏、重复或重排条目");
    }
   }
   var longText=string.Concat(Enumerable.Repeat("Long English description 🙂 ",20));
   var uneven=HudTextLayout.ColumnPages(new[]{new HudTextRow(longText,"horse")},right,140,34,20,measure);
   Check(string.Concat(uneven.SelectMany(p=>p.Rows).Where(r=>r.Column==0).Select(r=>r.Text))==longText,"单列超长英文仍完整保留Unicode和图标归属");
   Check(uneven.All(p=>p.Height+42<=140),"较长一列决定页面高度，并避开翻页按钮");
   var empty=HudTextLayout.ColumnPages(Array.Empty<HudTextRow>(),right,1000,34,20,measure);
   Check(empty.Length==1&&empty[0].Height==225&&empty[0].Rows.All(r=>r.Column==1),"空列不制造额外高度或改动另一列位置");
   var wrapped=HudTextLayout.ColumnPages(new[]{new HudTextRow(new string('a',55))},new[]{new HudTextRow("One")},1000,34,20,measure);
   Check(wrapped[0].Height==65,"多行条目按实际较高列计高，不强行压成一行或合并");
   Check(HudTextLayout.ColumnPages(left,right,66,34,20,measure).Length==0,"不足一行加翻页时使用已有降级状态");
   foreach(float width in new[]{390f,560f,900f}){
    float first=HudTextLayout.CellLeft(18,width,2,0),second=HudTextLayout.CellLeft(18,width,2,1),cell=HudTextLayout.CellWidth(width,2);
    Check(first==34&&second-first-cell==20,"共享几何保留16像素边距与20像素列间距");
    Check(second+cell==18+width-16&&cell-28>0,"两列图标及文本区域落在卡片内");
   }
   Check(HudTextLayout.CellLeft(18,390,1,0)==34&&HudTextLayout.CellWidth(390,1)-28==330,"单列回退保持原图标和文字几何");
   Check(ActualColumns(2560,140)&&ActualColumns(2560,250)&&ActualColumns(2560,390),"真实宽度分支覆盖中文／英文／大字号");
   Check(!ActualColumns(600,140)&&!ActualColumns(900,250),"窄视口或长英文列宽不足时返回单列");
   Check(!ActualColumns(2560,140,false),"隐藏人口时不保留空的统计列");
   Check(!ActualColumns(2560,140,true,1),"选择单列时不能被宽屏自动改成两列");
   Check(HudTextLayout.StatusText("无限容量 · 数字钱袋",100,t=>t.Length*16)=="无限容量\n数字钱袋","中文状态在语义分隔处分两行，不让钱袋拆成两行");
   Check(HudTextLayout.StatusText("Capacity · Numeric purse",150,t=>t.Length*10)=="Capacity\nNumeric purse","英文两段可容纳时优先按语义分行");
   Check(HudTextLayout.StatusText("Coins 1228",150,t=>t.Length*10)=="Coins 1228","短条目保持原单行");
   Check(HudTextLayout.StatusText("Long label · Very long state",50,t=>t.Length*10)=="Long label · Very long state","两段各自仍放不下时保留原文由测量换行");
   var singleRows=left.Concat(right).ToArray();singleRows[3].ExtraGap=HudTextLayout.StatusGroupGap;singleRows[12].ExtraGap=HudTextLayout.StatusGroupGap;
   var compact=HudTextLayout.Paged(singleRows,500,34,21.12f,t=>21.12f,HudTextLayout.SingleRowGap);
   Check(compact.Length==1&&compact[0].Columns==1&&compact[0].Rows.Count==16,"空间足够时完整保留全部条目");
   Check(Math.Abs(compact[0].Height-437.92f)<.01f&&compact[0].Rows.All(r=>Math.Abs(r.TextHeight-21.12f)<.01f),"单列恢复5像素行距与两处分组各10像素留白，不削减实测文本高度或字号");
   Check(compact[0].Rows.Count(r=>Math.Abs(r.Height-36.12f)<.01f)==2&&compact[0].Rows.Count(r=>Math.Abs(r.Height-26.12f)<.01f)==14,"仅分组交界增加额外留白，其余行保持一致节奏");
   var singlePages=HudTextLayout.Paged(singleRows,150,34,21.12f,t=>21.12f,HudTextLayout.SingleRowGap);
   Check(singlePages.All(p=>p.Height+42<=150)&&singlePages.SelectMany(p=>p.Rows).Count()==16,"单列带分组留白分页仍保留全部条目并避开翻页区");
   Check(HudTextLayout.Paged(singleRows,66,34,21.12f,t=>21.12f,HudTextLayout.SingleRowGap).Length==0,"极矮视口须将分组留白纳入最小空间判断，不因正文放不下而报错");
   Check(all[0].Rows.All(r=>r.TextHeight==20&&r.Height==25),"两列保留原行距，绘制文本高度独立于行间空白");
   var advisor=new HudRect(18,838.68f,390,443.2f);var shortDetail=new HudRect(420,838.68f,390,114);
   var alert=HudTextLayout.AboveAlert(advisor,29.12f,6);
   Check(alert.X==advisor.X&&Math.Abs(alert.Bottom-(advisor.Y-6))<.01f,"提醒优先放在顾问上方，与顾问左边缘对齐并保留6像素间距");
   var tallDetail=new HudRect(420,838.68f,390,402.48f);
   alert=HudTextLayout.AboveAlert(advisor,29.12f,6);
   Check(alert.X==advisor.X&&Math.Abs(alert.Bottom-(advisor.Y-6))<.01f,"附近详情长短不能导致上方提醒跳到正文下方或右侧");
   Check(!alert.Overlaps(advisor)&&!alert.Overlaps(tallDetail),"上方提醒不能覆盖已有正文");
   Check(HudTextLayout.TryPlaceAlert(alert,new HudRect(8,8,2544,1303),shortDetail,new[]{advisor,shortDetail},6,out var placed)&&Math.Abs(placed.Y-alert.Y)<.01f&&placed.X==advisor.X,"实际避让必须接受6像素上方间距，不得又退回详情下方");
   var defaultAdvisor=new HudRect(18,827.8f,390,483.2f);
   var defaultAlert=HudTextLayout.AboveAlert(defaultAdvisor,29.12f,6);
   Check(Math.Abs(defaultAlert.Y-792.68f)<.01f&&Math.Abs(defaultAlert.Bottom-821.8f)<.01f,"当前默认字号场景使用标题上方空隙，保留提醒实测文本高度与内边距");
   return "通过："+count+" 项两列正文、分页、独立条目、共享绘制几何及实际视图宽度回退检查。";
  }
 }
}
"@
Add-Type -TypeDefinition ("using System;using System.Linq;using System.Collections.Generic;using KingdomAdvisor;`n"+[regex]::Replace($source,'(?m)^using [^;]+;\r?\n','')+$harness)
[HudColumnTests.Cases]::Run()
