#Requires -Version 7.0
$ErrorActionPreference='Stop'
$layout=Get-Content (Join-Path $PSScriptRoot 'src/HudTextLayout.cs') -Raw
$model=Get-Content (Join-Path $PSScriptRoot 'src/Model.cs') -Raw
$view=Get-Content (Join-Path $PSScriptRoot 'src/View.cs') -Raw
$start=$view.IndexOf('private void Card(')
$end=$view.IndexOf('private bool HudCard(', $start)
$card=$view.Substring($start,$end-$start)
$harness=@"
namespace HudMetricTests {
 public struct Rect {public float x,y,width,height;public float xMax=>x+width;public float yMax=>y+height;public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;}}
 public struct Color {public float a;public Color(float r,float g,float b,float alpha){a=alpha;}}
 public sealed class CardProbe {
  int activePanelOpacity;Color background=new Color(0,0,0,.94f),border=new Color(0,0,0,.5f),gold=new Color(0,0,0,1);
  public List<float> Alphas=new List<float>();
  void Fill(Rect rect,Color c){Alphas.Add(c.a);}
  void PanelFill(Rect rect,Color c){c.a=activePanelOpacity<0?c.a:c.a*activePanelOpacity/100f;Fill(rect,c);}
  $card
  public void Render(int opacity,bool transparent){activePanelOpacity=opacity;Alphas.Clear();Card(new Rect(18,400,390,550),transparent,true);}
 }
 public static class Cases {
  public static string Run(){
   int count=0;void Check(bool ok,string message){count++;if(!ok)throw new Exception(message);}
   Func<string,float> width=t=>t.Length*10;
   Func<string,float,float> height=(t,w)=>Math.Max(1,(float)Math.Ceiling(t.Length*10/Math.Max(1,w)))*20;
   var row=HudTextLayout.Metric("金币","1228","coin",330,width,height);
   Check(row.ValueTop==0&&row.LabelWidth==22&&row.ValueWidth==292&&row.TextHeight==20,"短名称和值同排，右值与标签间留16像素，标签末尾保留2像素字形余量");
   var fractional=HudTextLayout.Metric("Builders","45","hammer",330,t=>t.Length*9.25f,height);
   Check(fractional.LabelWidth==76,"动态字体分数像素向上取整，避免实机英文词尾或钟点被裁切");
   float preferred=HudTextLayout.MetricWidth("Construction technology","Iron technology unlocked",width);
   var exact=HudTextLayout.Metric("Construction technology","Iron technology unlocked","tower",preferred,width,height);
   Check(exact.ValueTop==0&&preferred==490,"首选宽度与分区共用字形余量，英文默认字号不因新增余量意外折行");
   var wider=HudTextLayout.Metric("金币","12345678","coin",330,width,height);
   Check(wider.ValueWidth==row.ValueWidth&&wider.TextHeight==20,"数字增长不改变右对齐列和行高");
   var longValue=HudTextLayout.Metric("Construction technology","Iron technology unlocked","tower",240,width,height);
   Check(longValue.ValueTop==22&&longValue.ValueWidth==240&&longValue.TextHeight==42,"英文名称和值分为两行，保留完整文字");
   foreach(float available in new[]{90f,180f,330f,600f}){
    var measured=HudTextLayout.Metric("Long label","Long state that must wrap across multiple lines","tower",available,width,height);
    Check(measured.LabelWidth<=available&&measured.ValueWidth<=available&&measured.ValueWidth>0,"横向范围全部落在文本区域内");
    Check(measured.TextHeight>=measured.ValueTop+height(measured.Value,measured.ValueWidth),"正文高度含完整值和换行");
    Check(measured.ValueTop>0||measured.LabelWidth+16+measured.ValueWidth<=available,"同一行名称和值不重叠");
   }
   row.ExtraGap=10;
   var pages=HudTextLayout.Paged(new[]{row,longValue},250,34,20,t=>height(t,240));
   Check(pages.Length==1&&pages[0].Height==82,"分页按分区实际高度20+15及42+5累计");
   Check(pages[0].Rows[0].Label=="金币"&&pages[0].Rows[0].Value=="1228"&&pages[0].Rows[0].ExtraGap==10,"分页保留绘制所需名称、值、分组元数据");
   var huge=HudTextLayout.Metric("能力",string.Concat(Enumerable.Repeat("Long 🙂 ability ",40)),"stamina",100,width,height);
   var split=HudTextLayout.Paged(new[]{huge},120,34,20,t=>height(t,100));
   Check(split.Length>1&&split.All(p=>p.Height+42<=120),"极长值按页拆分，避开页脚");
   Check(string.Concat(split.SelectMany(p=>p.Rows).Select(r=>r.Text))==huge.Text,"分页完整保留标签、值、空格和Unicode");
   Check(split.SelectMany(p=>p.Rows).All(r=>r.Label==null),"分片移除旧双区几何，按实际文本重新测量");
   var population=new Snapshot{Workers=45,Archers=81,Hermits=4,Pikemen=66};
   var fields=PopulationPresentation.Fields(population);
   Check(fields.Count==8&&fields.Single(f=>f.Item2=="隐士").Item3==4,"结构化人口保留各类别和隐士独立数量");
   Check(fields.Select(f=>f.Item2+" "+f.Item3).SequenceEqual(PopulationPresentation.Rows(population).Select(r=>r.Item2)),"HUD字段与既有诊断报告数据完全一致");
   var card=new CardProbe();card.Render(0,false);
   Check(card.Alphas.Count==0,"真实Card在透明度0时不绘制底色、边框、阴影、顶端强调线");
   card.Render(-1,true);Check(card.Alphas.Count==0,"旧透明开关保持无背景");
   card.Render(45,false);Check(card.Alphas.Count==8&&Math.Abs(card.Alphas[0]-.45f)<.001&&card.Alphas.All(a=>a>0),"45%底色与装饰正常显示");
   var advisor=new HudRect(18,778,390,533);var detail=new HudRect(420,778,390,410);var above=HudTextLayout.AboveAlert(advisor,29.12f,6);
   Check(HudTextLayout.TryPlaceAlert(new HudRect(20,785.68f,390,29.12f),new HudRect(8,8,2544,1303),detail,new[]{advisor,detail},6,out var placed,above)&&placed.X==18&&Math.Abs(placed.Bottom-772)<.01f,"本机保存的提醒位置与新页眉重叠时优先回到顾问上方");
   var custom=new HudRect(1000,900,390,29.12f);
   Check(HudTextLayout.TryPlaceAlert(custom,new HudRect(8,8,2544,1303),detail,new[]{advisor,detail},6,out placed,above)&&placed.X==1000&&placed.Y==900,"合法自定义提醒位置仍保留");
   return "通过："+count+" 项名称／值布局、完整分页、人口来源与实际Card透明行为检查。";
  }
 }
}
"@
$body=@($layout,$model) | ForEach-Object {[regex]::Replace($_,'(?m)^using [^;]+;\r?\n','')}
Add-Type -TypeDefinition ("using System;using System.Linq;using System.Collections.Generic;using System.Text.RegularExpressions;using KingdomAdvisor;`n"+($body -join "`n")+$harness)
[HudMetricTests.Cases]::Run()
