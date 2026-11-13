#Requires -Version 7.0
$ErrorActionPreference='Stop'
$view=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'src/View.cs'))
function Block([string]$text,[string]$marker){
 $start=$text.IndexOf($marker);if($start -lt 0){throw "Missing production block: $marker"}
 $open=$text.IndexOf('{',$start);$depth=1;$end=$open+1
 while($depth -gt 0 -and $end -lt $text.Length){if($text[$end] -eq '{'){$depth++}elseif($text[$end] -eq '}'){$depth--};$end++}
 $text.Substring($start,$end-$start)
}
$place=Block $view 'private Rect Place('
$alert=Block $view 'if(alert.Length>0&&ah<=maxCardHeight)'
$layout=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'src/HudTextLayout.cs'))
$positions=Block ([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'src/MenuState.cs'))) 'public sealed class LayoutPositions'
$probe=@"
namespace HudDragTests {
 public struct Vector2 {public float x,y;public Vector2(float a,float b){x=a;y=b;}public static Vector2 operator -(Vector2 a,Vector2 b)=>new(a.x-b.x,a.y-b.y);}
 public struct Rect {public float x,y,width,height;public Rect(float a,float b,float w,float h){x=a;y=b;width=w;height=h;}public bool Contains(Vector2 p)=>p.x>=x&&p.y>=y&&p.x<x+width&&p.y<y+height;}
 public enum EventType {Repaint,MouseDown,MouseDrag,MouseUp,Used}
 public sealed class Event {public static Event current=new();public EventType type;public int button;public Vector2 mousePosition;public void Use(){type=EventType.Used;}}
 public sealed class Field<T> {public T Value;}
 public sealed class Settings {public Field<string> Layout=new(){Value=""};public Field<bool> AlertTransparent=new(){Value=true};}
 public sealed class Controller {public bool Open;}
 public sealed class Player {public int Id=0;}
 public enum OptionId {AlertOpacity}
 public sealed class Normal {public int textColor;}
 public sealed class Style {public Normal normal=new();}
 $positions
 public sealed class Harness {
  Settings settings=new();Controller controller=new();LayoutPositions positions=new("");string savedPositions="",dragKey="";Vector2 dragOffset;Rect hudAlert,hudMapAlertDefault=default;Style small=new();int gold=1;
  public Rect advisor=new(18,300,390,400);
  void LoadPositions(){if(savedPositions!=settings.Layout.Value){savedPositions=settings.Layout.Value;positions=new(savedPositions);}}
  static HudRect Bounds(Rect r)=>new(r.x,r.y,r.width,r.height);static Rect Rectangle(HudRect r)=>new(r.X,r.Y,r.Width,r.Height);
  bool ShowCard(Rect r)=>true;void HudCard(Rect r,OptionId id,bool transparent){}void HudLabel(Rect r,string text,object style){}
  $place
  Rect Draw(){
   var player=new Player();float ah=30,width=390,maxCardHeight=700,alertGap=6,alertPadding=4,alertTextH=22;string alert="可招募 4 人",alertText=alert;
   Rect advisorRect=advisor,hudViewport=new(0,0,1000,800);var detailBounds=new HudRect(420,300,390,100);
   var occupied=new List<HudRect>{Bounds(advisorRect),detailBounds,new(8,8,984,192)};hudAlert=default;
   $alert
   return hudAlert;
  }
  static void Check(bool ok,string label){if(!ok)throw new Exception(label);}
  Rect At(EventType kind,float x=0,float y=0){Event.current=new(){type=kind,button=0,mousePosition=new(x,y)};return Draw();}
  public static string Run(){
   var h=new Harness();h.settings.Layout.Value="p0.alert=0.03,0.55;p0.advisor=0.1,0.2";
   var shown=h.At(EventType.Repaint);Check(shown.y==264,"旧保存位置冲突时应先在顾问上方显示");
   h.At(EventType.MouseDown,shown.x+22,shown.y+11);
   Check(h.dragKey=="p0.alert","可见提醒必须能够点击拖动，不能仍在旧坐标接收输入");
   var crossing=h.At(EventType.MouseDrag,100,350);
   var repaint=h.At(EventType.Repaint);
   Check(crossing.x==repaint.x&&crossing.y==repaint.y,"拖过其他卡片时不得吸回顾问上方");
   var moved=h.At(EventType.MouseDrag,650,250);var released=h.At(EventType.MouseUp,650,250);
   Check(released.x==moved.x&&released.y==moved.y&&h.dragKey=="","松手保留最终位置并结束拖动");
   var reloaded=new Harness();reloaded.settings.Layout.Value=h.settings.Layout.Value;
   reloaded.advisor=new(18,450,390,250);var stable=reloaded.At(EventType.Repaint);
   Check(Math.Abs(stable.x-released.x)<0.01f&&Math.Abs(stable.y-released.y)<0.01f,"重载和移动顾问后，自定义提醒位置必须独立保留");
   Check(new LayoutPositions(h.settings.Layout.Value).Get("p0.advisor").Item1==0.1f,"拖动提醒不能修改顾问位置");
   reloaded.controller.Open=true;reloaded.At(EventType.MouseDown,stable.x+22,stable.y+11);Check(reloaded.dragKey=="","主界面打开时保持拖动隔离");
   return "通过：执行生产提醒绘制与拖动代码，验证旧位置冲突后的可见命中、跨卡片拖动、松手保存、重载及顾问移动后的独立位置与菜单隔离。";
  }
 }
}
"@
Add-Type -TypeDefinition ("using System;using System.Collections.Generic;using System.Globalization;using KingdomAdvisor;`n"+[regex]::Replace($layout,'(?m)^using [^;]+;\r?\n','')+$probe)
[HudDragTests.Harness]::Run()
