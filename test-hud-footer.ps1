#Requires -Version 7.0
$ErrorActionPreference='Stop'
$view=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'src/View.cs') -Raw
$start=$view.IndexOf('float footerWidth=')
$end=$view.IndexOf('float y=18;', $start)
if($start -lt 0 -or $end -le $start){throw '生产 HUD 页脚测量边界已改变，需要复核测试。'}
$footer=$view.Substring($start,$end-$start)
$dayStart=$view.IndexOf('string date=Localization.English?')
$dayEnd=$view.IndexOf('string countdown=', $dayStart)
if($dayStart -lt 0 -or $dayEnd -le $dayStart){throw '生产日期标题边界已改变，需要复核测试。'}
$dayBlock=$view.Substring($dayStart,$dayEnd-$dayStart)
$layout=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'src/HudTextLayout.cs') -Raw
# 直接执行生产 View 中的页脚测量；外部字体高度由独立场景给定，避免漏测接线。
$probe=@"
namespace HudFooterTests {
 public sealed class GUIContent { public string Text;public GUIContent(string text){Text=text;} }
 public struct Size {public float x;public Size(float width){x=width;} }
 public sealed class Style {public bool Button;public Size CalcSize(GUIContent content)=>new Size(content.Text.Length*9+24);}
 public struct Rect {public float x,y,width,height;public Rect(float a,float b,float w,float h){x=a;y=b;width=w;height=h;} }
 public static class Localization {public static bool English;public static string Text(string value)=>English?value.Replace("春季","Spring"):value;}
 public sealed class Snapshot {public int Island=3,Day=131;public string Season="春季";}
 public sealed class Controller {public string Device,PadOpenLabel="手柄键";}
 public sealed class Number {public int Value;}
 public sealed class Settings {public Number AdvisorColumns=new Number(),FontSize=new Number();}
 public static class Probe {
  static float keyHeight,padHeight,hintHeight;static string PreviewHudDevice=null;static int? PreviewHudColumns=null;
  static float HudHeight(string value,float width,Style style)=>!style.Button?hintHeight:value.Contains("面板")?padHeight:value=="←"?34:keyHeight;
  static string Season(string value)=>value;
  public static string DayText(float screenWidth){
   Localization.English=true;var s=new Snapshot();var viewport=new Rect(0,0,screenWidth,725);
   var small=new Style();
   $dayBlock
   Localization.English=false;return day;
  }
  public static float[] Run(bool padDevice,float width,float height,float keyboardH,float padH,float hintH,int font=28,int columns=1){
   var controller=new Controller{Device=padDevice?"手柄":"键鼠"};
   var settings=new Settings();settings.FontSize.Value=font;settings.AdvisorColumns.Value=columns;
   var viewport=new Rect(0,0,width,height);float pad=18;
   var button=new Style{Button=true};var small=new Style();
   keyHeight=keyboardH;padHeight=padH;hintHeight=hintH;
   $footer
   return new[]{footerTop,hudViewport.height-8,footerButtonH,helpH,guideW+optionsW+hideW+16,inlinePadHelp?1:0,padButtonRect.x+padButtonRect.width,padHelpRect.x,padHelpRect.x+padHelpRect.width,padHelpRect.y+padHelpRect.height};
  }
 }
 public static class Cases {
  public static string Run(){
   var failures=new System.Collections.Generic.List<string>();int cases=0;
   void Check(bool ok,string name){cases++;if(!ok)failures.Add(name);}
   foreach(float height in new[]{360f,725f,1080f,1377f}){
    var keyboard=Probe.Run(false,1290,height,34,34,22);
    Check(keyboard[1]==height-66,"键鼠默认底边恢复旧版66像素留白: "+height);
    Check(keyboard[3]==0,"键鼠不占用隐藏手柄说明行: "+height);
    var pad=Probe.Run(true,1290,height,34,34,44);
    Check(pad[0]+44+8+34+16==height,"手柄两行长说明与按钮完整落屏: "+height);
    Check(pad[1]<pad[0],"手柄正文不得覆盖说明: "+height);
   }
   var hiddenPad=Probe.Run(false,1290,725,34,110,70);
   Check(hiddenPad[2]==34&&hiddenPad[1]==659,"隐藏的大字号手柄按钮不抬高键鼠HUD");
   var hiddenKeys=Probe.Run(true,1290,725,120,34,44);
   Check(hiddenKeys[2]==34,"手柄不采用隐藏键鼠按钮的高度");
   var large=Probe.Run(false,400,725,52,100,80);
   Check(large[2]==52&&large[1]==725-16-52-16,"可见大字号按钮按真实高度避让");
   Check(large[4]<=400-36+.01f,"窄视口三个键鼠按钮仍可完整排列");
   var singlePad=Probe.Run(true,1290,1377,34,34,22,18);
   Check(singlePad[5]==1&&singlePad[1]==1311,"默认字号单列的宽屏手柄提示横排，底边与键鼠一致");
   Check(singlePad[7]>=singlePad[6]+16&&singlePad[8]<=1290-18&&singlePad[9]<=1377-16,"横排提示与按钮不重叠并完整落屏");
   var narrowPad=Probe.Run(true,300,725,34,34,44,18);
   Check(narrowPad[5]==0&&narrowPad[0]+44+8+34+16==725,"单列窄屏放不下完整提示时保持两行，不截断文字");
   var doublePad=Probe.Run(true,1290,1377,34,34,22,18,2);
   Check(doublePad[5]==0&&doublePad[0]+22+8+34+16==1377,"两列模式保持既有两行手柄页脚");
   string english=Probe.DayText(1291);
   Check(!english.Contains("春季")&&english.Contains("Spring"),"实际标题的季节须先本地化再测量和绘制");
   Check(Probe.DayText(400).Contains("Island 3 · Day 131 · Spring"),"日期与季节完整保留；时钟独立测量，不再与日期拼接挤满一行");
   if(failures.Count>0)throw new Exception("HUD页脚回归失败: "+string.Join("; ",failures));
   return "通过："+cases+" 项实际生产页脚测量回归，键鼠旧版默认底边、手柄两行提示、隐藏设备不占位及窄视口。";
  }
 }
}
"@
Add-Type -TypeDefinition ("using System;using System.Collections.Generic;using KingdomAdvisor;`n"+[regex]::Replace($layout,'(?m)^using [^;]+;\r?\n','')+$probe)
[HudFooterTests.Cases]::Run()
