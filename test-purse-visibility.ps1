#Requires -Version 7.0
$ErrorActionPreference='Stop'
$source=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'src/GameActions.cs') -Raw
$start=$source.IndexOf('private void ApplyPurseVisibility(')
$end=$source.IndexOf('public void Apply(Snapshot state)', $start)
if($start -lt 0 -or $end -le $start){throw '未找到生产钱袋显示方法'}
$methods=$source.Substring($start,$end-$start)
$fields=([regex]::Matches($source,'(?m)^\s*private readonly [^\r\n]*(?:hiddenPurseRenderers|currentPurseRenderers)[^\r\n]*') | ForEach-Object Value) -join "`n"
# 执行生产视觉方法；替身只提供对象存活、渲染开关和本地玩家身份，不加载游戏或修改存档。
Add-Type -TypeDefinition @"
using System;using System.Linq;using System.Collections.Generic;
namespace PurseVisibilityTest {
 public class NativeObject {static int next;public int Id=++next;public bool Alive=true;public int GetInstanceID()=>Id;public static implicit operator bool(NativeObject o)=>o!=null&&o.Alive;}
 public class Renderer:NativeObject {public bool forceRenderingOff;}
 public class CurrencyBag:NativeObject {public Renderer _front=new(),_back=new(),_closed=new();}
 public class Wallet:NativeObject {public CurrencyBag _currencyBag=new();}
 public class NativePlayer:NativeObject {public Wallet wallet=new();}
 public class PlayerInfo {public int Id;public bool Local=true;}
 public class Snapshot {public bool Playing=true;public List<PlayerInfo> Players=new();}
 public class Flag {public bool Value;}
 public class Settings {public Flag InfiniteBag=new(),HideNativePurse=new();}
 public class Probe {
  public Settings settings=new();public Dictionary<int,NativePlayer> Players=new();
  NativePlayer Player(int id)=>Players.TryGetValue(id,out var p)?p:null;
  $fields
  $methods
  public void Apply(Snapshot state)=>ApplyPurseVisibility(state);
  public void Restore()=>RestorePurseVisibility();
  public int Retained=>hiddenPurseRenderers.Count;
 }
 public static class Cases {
  static int count;
  static void Check(bool condition,string label){count++;if(!condition)throw new Exception(label);}
  static bool Hidden(CurrencyBag b)=>b._front.forceRenderingOff&&b._back.forceRenderingOff&&b._closed.forceRenderingOff;
  public static int Run(){
   count=0;var p=new Probe();var s=new Snapshot();s.Players.Add(new PlayerInfo{Id=0});s.Players.Add(new PlayerInfo{Id=1,Local=false});
   p.Players[0]=new NativePlayer();p.Players[1]=new NativePlayer();var first=p.Players[0].wallet._currencyBag;var remote=p.Players[1].wallet._currencyBag;
   p.Apply(s);Check(p.Retained==0&&!Hidden(first),"默认不得隐藏");
   p.settings.HideNativePurse.Value=true;p.Apply(s);Check(p.Retained==0,"没有无限容量时不生效");
   p.settings.InfiniteBag.Value=true;first._closed.forceRenderingOff=true;p.Apply(s);
   Check(Hidden(first)&&!Hidden(remote)&&p.Retained==3,"只隐藏本地玩家的三个外壳组件");
   for(int i=0;i<20;i++)p.Apply(s);
   Check(p.Retained==3,"反复应用不得增长缓存");
   p.settings.HideNativePurse.Value=false;p.Apply(s);
   Check(!first._front.forceRenderingOff&&!first._back.forceRenderingOff&&first._closed.forceRenderingOff&&p.Retained==0,"关闭恢复各组件原值");
   p.settings.HideNativePurse.Value=true;p.Apply(s);var second=new CurrencyBag();p.Players[0].wallet._currencyBag=second;p.Apply(s);
   Check(!first._front.forceRenderingOff&&Hidden(second)&&p.Retained==3,"换袋恢复旧对象并隐藏新对象");
   p.settings.InfiniteBag.Value=false;p.Apply(s);Check(!Hidden(second)&&p.Retained==0,"关闭无限容量恢复外壳");
   p.settings.InfiniteBag.Value=true;s.Players[1].Local=true;p.Apply(s);Check(Hidden(second)&&Hidden(remote)&&p.Retained==6,"本地双人分别隐藏");
   s.Players[1].Local=false;p.Apply(s);Check(!Hidden(remote)&&p.Retained==3,"玩家失去本地控制权时恢复");
   s.Playing=false;p.Apply(s);Check(!Hidden(second)&&p.Retained==0,"离开游戏时恢复");
   s.Playing=true;p.Apply(s);second._front.Alive=false;p.Players[0].wallet._currencyBag=null;p.Apply(s);
   Check(!second._back.forceRenderingOff&&!second._closed.forceRenderingOff&&p.Retained==0,"已销毁及缺失对象安全清理");
   p.Players[0].wallet._currencyBag=new CurrencyBag{_back=null};p.Apply(s);Check(p.Retained==2,"部分外壳缺失仍可隐藏");
   p.Restore();p.Restore();Check(p.Retained==0&&!p.Players[0].wallet._currencyBag._front.forceRenderingOff,"卸载及重复恢复");
   return count;
  }
 }
}
"@
$count=[PurseVisibilityTest.Cases]::Run()
$plugin=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'src/Plugin.cs') -Raw
$faults=[regex]::Matches($plugin,'faulted=true;[^\r\n]+')
if($faults.Count -ne 2){throw '故障入口已变化，需要复核视觉恢复覆盖'}
foreach($fault in $faults){if(!$fault.Value.Contains('RestorePurseVisibility()')){throw '停止更新前必须恢复钱袋外壳'}}
"通过：$count 项生产钱袋显示逻辑模拟检查（条件、往返、原值、换袋、本地双人、控制权、销毁、退出恢复）。不代表实机视觉验收。"
