#Requires -Version 7.0
$ErrorActionPreference='Stop'
$source=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'src/GameActions.cs') -Raw
$start=$source.IndexOf('internal void PrepareCarryForward(')
$end=$source.IndexOf('// 只抑制钱袋外壳渲染',$start)
if($start -lt 0 -or $end -lt 0){throw '跨岛容量保护尚未接入'}
$methods=$source.Substring($start,$end-$start)
# 原生 SetCurrency 的容量截断已由本机 GameAssembly 反汇编核对；这里只模拟该边界，不写真实存档。
Add-Type -TypeDefinition @"
using System;using System.Linq;using System.Collections.Generic;
namespace TravelTest {
 public class Wallet {public int TotalCapacity=1000,Coins,Gems;public bool Alive=true;public static implicit operator bool(Wallet w)=>w!=null&&w.Alive;public void Transfer(int coins,int gems){Coins=Math.Clamp(coins,0,TotalCapacity);Gems=Math.Clamp(gems,0,TotalCapacity);}}
 public class Player {public bool hasLocalAuthority=true;public Wallet wallet=new();public static implicit operator bool(Player p)=>p!=null;}
 public static class NetworkBigBoss {public static bool IsClientPresent;}
 public class Kingdom {public Player playerTwo;public static implicit operator bool(Kingdom k)=>k!=null;}
 public class Managers {public static Managers _Inst=new();public Kingdom kingdom=new();public static implicit operator bool(Managers m)=>m!=null;}
 public class Flag {public bool Value;}
 public class Settings {public Flag InfiniteBag=new();}
 public class Probe {public Settings settings=new();$methods
  public void Run(Player first,Player second,bool fail=false){List<Tuple<Wallet,int>> originals=null;try{PrepareCarryForward(first,second,out originals);first?.wallet.Transfer(1400,19);if(second!=null)second.wallet.Transfer(1800,23);if(fail)throw new InvalidOperationException("test");}finally{RestoreCarryForward(originals);}}
 }
 public static class Cases {
  static int count;static void Check(bool ok,string label){count++;if(!ok)throw new Exception(label);}
  public static int Run(){
   count=0;var p=new Probe();var one=new Player();p.Run(one,null);Check(one.wallet.Coins==1000,"关闭无限容量保留原生截断");
   p.settings.InfiniteBag.Value=true;p.Run(one,null);Check(one.wallet.Coins==1400&&one.wallet.Gems==19,"无限容量保存实际金币及宝石");Check(one.wallet.TotalCapacity==1000,"结束恢复原容量");
   var two=new Player{hasLocalAuthority=false};p.Run(one,two);Check(two.wallet.Coins==1000&&one.wallet.Coins==1400,"不修改远端玩家容量");
   two.hasLocalAuthority=true;p.Run(one,two);Check(two.wallet.Coins==1800&&two.wallet.Gems==23,"本地双人分别继承");
   NetworkBigBoss.IsClientPresent=true;Managers._Inst.kingdom.playerTwo=two;p.Run(one,two);Check(two.wallet.Coins==1000&&one.wallet.Coins==1400,"主机远端 P2 初始权属尚为 true 时仍不得接管");NetworkBigBoss.IsClientPresent=false;Managers._Inst.kingdom.playerTwo=null;
   p.Run(one,one);Check(one.wallet.TotalCapacity==1000,"重复钱包只记录原值一次");
   one.wallet.TotalCapacity=int.MaxValue;p.Run(one,null);Check(one.wallet.TotalCapacity==int.MaxValue,"已启用容量不被错误恢复");
   one.wallet.TotalCapacity=1000;try{p.Run(one,null,true);}catch(InvalidOperationException){}Check(one.wallet.TotalCapacity==1000,"原生失败也恢复临时容量");
   Probe.RestoreCarryForward(null);Check(true,"无活动实例可跳过");
   return count;
  }
 }
}
"@
$count=[TravelTest.Cases]::Run()
if(!$source.Contains('[HarmonyPatch(typeof(CampaignSaveData),"ApplyCarryForwardCurrency")]') -or !$source.Contains('Finalizer(List<System.Tuple<Wallet,int>> __state)')){throw '原生跨岛入口或异常恢复挂钩缺失'}
"通过：$count 项生产跨岛容量保护模拟检查；原生截断复现、正常容量、本地双人、远端隔离与异常恢复。不代表真实乘船验收。"
