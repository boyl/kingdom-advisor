#Requires -Version 7.0
$ErrorActionPreference='Stop'
$source=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'src/ContinuousPurchaseRuntime.cs') -Raw
$start=$source.IndexOf('private static bool Supported(Payable target,Player player)')
$end=if($start -ge 0){$source.IndexOf('private static bool InRange(', $start)}else{-1}
if($start -lt 0 -or $end -le $start){throw '连续购买的生产目标白名单方法已变化，需要复核测试适配边界'}
$method=$source.Substring($start,$end-$start)
$rangeStart=$end
$rangeEnd=$source.IndexOf('private static PurchasePhase Phase(', $rangeStart)
if($rangeEnd -le $rangeStart){throw '连续购买的生产距离方法已变化，需要复核测试适配边界'}
$rangeMethod=$source.Substring($rangeStart,$rangeEnd-$rangeStart)
$continuationStart=$source.IndexOf('private static bool SupportedForContinuation(Payable target,Player player)')
$continuationEnd=if($continuationStart -ge 0){$source.IndexOf('private static void Fail(', $continuationStart)}else{-1}
if($continuationStart -lt 0 -or $continuationEnd -le $continuationStart){throw '连续购买的生产后续目标方法已变化，需要复核测试适配边界'}
$continuationMethod=$source.Substring($continuationStart,$continuationEnd-$continuationStart)
$armorMethod=''
$armorSignature='private static bool ArmorForge('
$armorStart=$source.IndexOf($armorSignature)
if($armorStart -ge 0 -and -not ($method+$rangeMethod+$continuationMethod).Contains($armorSignature)){
 $armorEnd=$source.IndexOf(';',$armorStart)
 if($armorEnd -lt $armorStart){throw '连续购买的生产护甲工坊方法已变化，需要复核测试适配边界'}
 $armorMethod=$source.Substring($armorStart,$armorEnd-$armorStart+1)
}
$filterStart=$source.IndexOf('public static void Filter(')
$filterEnd=if($filterStart -ge 0){$source.IndexOf('public static void ObserveFirstPress(', $filterStart)}else{-1}
if($filterStart -lt 0 -or $filterEnd -le $filterStart){throw '连续购买的生产付款入口已变化，需要复核测试适配边界'}
$filter=$source.Substring($filterStart,$filterEnd-$filterStart)
function Get-ProductionStatement([string]$name){
 $matches=[regex]::Matches($filter,"\bbool $name=[^;]+;")
 if($matches.Count -ne 1){throw "连续购买的生产 $name 表达式已变化，需要复核测试适配边界"}
 $matches[0].Value
}
$validStatement=Get-ProductionStatement 'valid'
$selectedStatement=Get-ProductionStatement 'selectedSame'
$canPayStatement=Get-ProductionStatement 'canPay'
$model=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'src/ContinuousPurchaseModel.cs') -Raw
# 直接执行生产分类、距离、原生目标确认方法及付款资格表达式；替身只提供外部游戏类型和返回值。
# GetClosestPayable 替身返回独立安排的原生选择，不自行重写原生距离算法。
# 期望来自已确认的类别和原生场景；状态序列执行真实模型，不模拟招募或扣款。
Add-Type -TypeDefinition @"
using System;using System.Collections.Generic;
using KingdomAdvisor;
namespace ContinuousPurchaseAdapterTests {
 public class NativeObject {private static int sequence;private readonly int id=++sequence;public bool Alive=true;public int GetInstanceID()=>id;public T TryCast<T>() where T:NativeObject=>this as T;public static implicit operator bool(NativeObject obj)=>obj!=null&&obj.Alive;}
 public class Component:NativeObject {}
 public class Wall:Component {} public class Tower:Component {} public class Farmhouse:Component {} public class Castle:Component {} public class WorkableBuilding:Component {} public class UnrelatedComponent:Component {}
 public class GameObject {public readonly HashSet<Type> Components=new();public bool activeInHierarchy=true;}
 public class Vector3 {public float x;}
 public class Transform {public Vector3 position=new();}
 public enum CurrencyType {Coins=0,Gems=1,Other=2}
 public enum Side {Left,Right}
 public class Payable:NativeObject {
  public CurrencyType Currency;public GameObject gameObject=new();public float PayPoint;public float playerPayDistance=1f;
  public bool Selectable=true,PayableNow=true,isActiveAndEnabled=true,forceBlockPayment;
  public float PlayerPayPoint()=>PayPoint;public bool CanSelect(Player player)=>Selectable;public bool CanPay(Player player)=>PayableNow;
 }
 public class PayableShop:Payable {public enum UnsidedShopType {Pike=0,Ninja=1,ChangeRuler=2,Workshop=3,ShieldShop=4,ChangeItem=5}}
 public class PayableSidedShop:PayableShop {public UnsidedShopType shopType=UnsidedShopType.Pike;}
 public class ChangeRulerShop:PayableShop {} public class ChangeItemOfPowerShop:PayableShop {}
 public class CitizenHousePayable:Payable {}
 public class PayableWorkshopBarrel:Payable {} public class PayableWorkshop:Payable {}
 public class PayableShield:Payable {}
 public class PayableForge:Payable {public Side side;public NativeObject armorPrefab;}
 public class Wharf:Payable {public List<object> _availableParts;public bool _boatLaunched;}
 public class PayableComponent:Payable {public Component _owner;}
 public class FireTower:Component {} public class Shipyard:Component {} public class PayableFishingRod:Component {}
 public class PayableUpgrade:Payable {public object[] passengerUpgrades;}
 public class Player:NativeObject {public enum ActionState {Idle,Run}public ActionState actionState;public NativeObject passenger;public Transform transform=new();public Payable selectedPayable;}
 public class PayablesManager:NativeObject {
  public Payable Candidate;public int Calls;public float LastX,LastRange;public Player LastPlayer;
  public Payable GetClosestPayable(float x,float range,Player player){Calls++;LastX=x;LastRange=range;LastPlayer=player;return Candidate;}
 }
 public class Managers:NativeObject {public static Managers _Inst=new();public PayablesManager payables=new();}
 public static class Probe {
  private static bool Has<T>(GameObject target) where T:Component=>target.Components.Contains(typeof(T));
  private static int rangeTarget;private static float rangeDistance,rangeLimit;
  $method
  $rangeMethod
  $continuationMethod
  $armorMethod
  public static bool Accept(Payable target,Player player)=>Supported(target,player);
  public static bool Continue(Payable target,Player player)=>SupportedForContinuation(target,player);
  public static bool Range(Payable target,Player player)=>InRange(target,player);
  public static bool NativeSelection(Payable target,Player player)=>NativeSelectsTarget(target,player);
  public static bool Valid(Payable target,Player player){$validStatement return valid;}
  public static bool Ready(Payable target,Player player,bool available=true,bool funded=true){
   var selected=player.selectedPayable;
   $validStatement
   $selectedStatement
   $canPayStatement
   return canPay;
  }
 }
 public static class Cases {
  private static PurchaseInput Input(Payable target,Player player,PurchasePhase phase,bool down=false)=>new PurchaseInput{
   Enabled=true,Held=true,Down=down,Allowed=true,Phase=phase,
   SelectedTarget=player.selectedPayable?player.selectedPayable.GetInstanceID():0,
   SupportedTarget=target?target.GetInstanceID():0,TargetValid=Probe.Valid(target,player),CanPay=Probe.Ready(target,player)
  };
  public static string[] Run(){
   var failures=new List<string>();int count=0;var player=new Player();
   void Expect(bool actual,bool expected,string label){count++;if(actual!=expected)failures.Add(label+": actual="+actual+", expected="+expected);}
   void Check(Payable target,Player actor,bool expected,string label)=>Expect(Probe.Accept(target,actor),expected,label);
   Check(new CitizenHousePayable{Currency=CurrencyType.Coins},player,true,"金币市民之家应支持连续招募");
   foreach(var currency in new[]{CurrencyType.Gems,CurrencyType.Other})
    Check(new CitizenHousePayable{Currency=currency},player,false,"市民之家非金币货币不自动支付: "+currency);
   Check(new PayableShop{Currency=CurrencyType.Coins},player,true,"普通金币装备商店");
   Check(new PayableSidedShop{Currency=CurrencyType.Coins},player,true,"原生装备商店子类");
   Check(new PayableShop{Currency=CurrencyType.Gems},player,false,"宝石商店不纳入续购");
   Check(new ChangeRulerShop{Currency=CurrencyType.Coins},player,false,"君主切换商店不得重复自动切换");
   Check(new ChangeItemOfPowerShop{Currency=CurrencyType.Coins},player,false,"力量道具切换商店不得重复自动切换");
   Expect(Probe.Continue(new ChangeRulerShop{Currency=CurrencyType.Coins},player),false,"持续付款阶段也排除君主切换派生商店");
   Expect(Probe.Continue(new ChangeItemOfPowerShop{Currency=CurrencyType.Coins},player),false,"持续付款阶段也排除力量道具切换派生商店");
   foreach(var shopType in new[]{PayableShop.UnsidedShopType.Pike,PayableShop.UnsidedShopType.Ninja,PayableShop.UnsidedShopType.Workshop,PayableShop.UnsidedShopType.ShieldShop}){
    var ordinary=new PayableSidedShop{Currency=CurrencyType.Coins,shopType=shopType};
    Check(ordinary,player,true,"普通侧向商店分支可连续购买: "+shopType);
    Expect(Probe.Continue(ordinary,player),true,"普通侧向商店分支可保持连续付款: "+shopType);
   }
   foreach(var shopType in new[]{PayableShop.UnsidedShopType.ChangeRuler,PayableShop.UnsidedShopType.ChangeItem,(PayableShop.UnsidedShopType)99}){
    var choice=new PayableSidedShop{Currency=CurrencyType.Coins,shopType=shopType};
    Check(choice,player,false,"选择或未知侧向商店分支不得自动付款: "+shopType);
    Expect(Probe.Continue(choice,player),false,"持续付款阶段也拒绝选择或未知侧向分支: "+shopType);
   }
   var changingShop=new PayableSidedShop{Currency=CurrencyType.Coins,shopType=PayableShop.UnsidedShopType.Pike};
   var shopActor=new Player{selectedPayable=changingShop};var shopState=new ContinuousPurchaseState();
   shopState.Step(Input(changingShop,shopActor,PurchasePhase.Holding,true));shopState.Step(Input(changingShop,shopActor,PurchasePhase.Completed));
   changingShop.shopType=PayableShop.UnsidedShopType.ChangeRuler;
   Expect(Probe.Continue(changingShop,shopActor),false,"已锁定装备商店变为君主选择分支后必须拒绝续投");
   Expect(Probe.Ready(changingShop,shopActor),false,"生产付款资格不能沿用改变前的普通商店许可");
   Expect(shopState.Step(Input(changingShop,shopActor,PurchasePhase.None)).Down,false,"切换为君主选择分支后不得合成下一轮付款按下");
   Check(new PayableWorkshopBarrel{Currency=CurrencyType.Coins},player,true,"金币火焰桶工坊应支持连续补购");
   Check(new PayableWorkshop{Currency=CurrencyType.Coins},player,true,"金币工坊应支持连续制作");
   foreach(var currency in new[]{CurrencyType.Gems,CurrencyType.Other}){
    Check(new PayableWorkshopBarrel{Currency=currency},player,false,"火焰桶工坊非金币货币不自动支付: "+currency);
    Check(new PayableWorkshop{Currency=currency},player,false,"工坊非金币货币不自动支付: "+currency);
   }
   Check(new PayableShield{Currency=CurrencyType.Coins},player,true,"金币盾牌架应支持连续补购");
   Check(new PayableForge{Currency=CurrencyType.Coins,side=Side.Left,armorPrefab=new NativeObject()},player,true,"有有效护甲预制体的左侧铁匠铺应支持连续制作");
   Check(new PayableComponent{Currency=CurrencyType.Coins,_owner=new FireTower()},player,true,"火焰塔的金币补给组件应支持连续付款");
   foreach(var currency in new[]{CurrencyType.Gems,CurrencyType.Other}){
    Check(new PayableShield{Currency=currency},player,false,"盾牌架非金币货币不自动支付: "+currency);
    Check(new PayableForge{Currency=currency,side=Side.Left,armorPrefab=new NativeObject()},player,false,"护甲铁匠铺非金币货币不自动支付: "+currency);
    Check(new PayableComponent{Currency=currency,_owner=new FireTower()},player,false,"火焰塔组件非金币货币不自动支付: "+currency);
   }
   Check(new PayableForge{side=Side.Right,armorPrefab=new NativeObject()},player,false,"右侧炸弹分支不得自动连购，即使存在护甲预制体");
   Check(new PayableForge{side=(Side)99,armorPrefab=new NativeObject()},player,false,"未知铁匠铺方向不得被视为护甲分支");
   Check(new PayableForge{side=Side.Left},player,false,"左侧铁匠铺没有护甲预制体时不得自动付款");
   Check(new PayableForge{side=Side.Left,armorPrefab=new NativeObject{Alive=false}},player,false,"左侧铁匠铺护甲预制体失效时不得自动付款");
   Check(new PayableComponent{_owner=new PayableFishingRod()},player,false,"钓鱼组件不得被通用PayableComponent自动接管");
   Check(new PayableComponent{_owner=new UnrelatedComponent()},player,false,"未知支付组件所有者不得自动接管");
   Check(new PayableComponent(),player,false,"缺少所有者的支付组件不得自动接管");
   Check(new PayableComponent{_owner=new FireTower{Alive=false}},player,false,"火焰塔所有者失效时不得自动付款");
   var changingForge=new PayableForge{side=Side.Left,armorPrefab=new NativeObject()};
   var forgeActor=new Player{selectedPayable=changingForge};Managers._Inst.payables.Candidate=changingForge;
   Expect(Probe.Continue(changingForge,forgeActor),true,"有效护甲分支持续付款阶段仍可继续");
   var forgeState=new ContinuousPurchaseState();forgeState.Step(Input(changingForge,forgeActor,PurchasePhase.Holding,true));forgeState.Step(Input(changingForge,forgeActor,PurchasePhase.Completed));
   changingForge.side=Side.Right;
   Expect(Probe.Continue(changingForge,forgeActor),false,"持续付款阶段切到右侧炸弹必须重新拒绝");
   Expect(Probe.Ready(changingForge,forgeActor),false,"生产付款资格不得沿用先前护甲分支许可");
   Expect(forgeState.Step(Input(changingForge,forgeActor,PurchasePhase.None)).Down,false,"护甲分支切到炸弹后不得合成下一轮付款按下");
   changingForge.side=Side.Left;changingForge.armorPrefab=null;
   Expect(Probe.Continue(changingForge,forgeActor),false,"持续付款阶段失去护甲预制体也必须拒绝");
   var changingComponent=new PayableComponent{_owner=new FireTower()};var componentActor=new Player{selectedPayable=changingComponent};
   Expect(Probe.Continue(changingComponent,componentActor),true,"有效火焰塔组件在持续付款阶段仍可继续");
   changingComponent._owner=new PayableFishingRod();
   Expect(Probe.Continue(changingComponent,componentActor),false,"持续付款阶段所有者不再是火焰塔时必须拒绝");
   Expect(Probe.Ready(changingComponent,componentActor),false,"生产付款资格不能把火焰塔许可沿用到钓鱼组件");
   changingComponent._owner=null;Expect(Probe.Continue(changingComponent,componentActor),false,"持续付款阶段丢失火焰塔所有者时必须拒绝");
   changingComponent._owner=new FireTower{Alive=false};Expect(Probe.Continue(changingComponent,componentActor),false,"持续付款阶段火焰塔所有者失效时必须拒绝");
   var shipyardComponent=new PayableComponent{Currency=CurrencyType.Coins,_owner=new Shipyard()};
   var shipyardActor=new Player{selectedPayable=shipyardComponent};
   Check(shipyardComponent,shipyardActor,true,"船厂所有者的金币支付组件允许连续生产船只");
   Expect(Probe.Continue(shipyardComponent,shipyardActor),true,"有效船厂支付组件允许保持连续付款");
   Expect(Probe.Ready(shipyardComponent,shipyardActor),true,"船厂原生可付款时满足生产付款资格");
   foreach(var currency in new[]{CurrencyType.Gems,CurrencyType.Other})
    Check(new PayableComponent{Currency=currency,_owner=new Shipyard()},shipyardActor,false,"船厂非金币货币不自动支付: "+currency);
   shipyardComponent.PayableNow=false;
   Expect(Probe.Ready(shipyardComponent,shipyardActor),false,"船厂施工中或达到原生船数上限时保留CanPay限制");
   shipyardComponent.PayableNow=true;shipyardComponent._owner=new Shipyard{Alive=false};
   Check(shipyardComponent,shipyardActor,false,"失活船厂所有者不得建立连续付款会话");
   Expect(Probe.Continue(shipyardComponent,shipyardActor),false,"船厂所有者失活后必须拒绝续购");
   Expect(Probe.Ready(shipyardComponent,shipyardActor),false,"船厂所有者失活后不满足生产付款资格");
   shipyardComponent._owner=new Shipyard();var shipyardState=new ContinuousPurchaseState();
   shipyardState.Step(Input(shipyardComponent,shipyardActor,PurchasePhase.Holding,true));shipyardState.Step(Input(shipyardComponent,shipyardActor,PurchasePhase.Completed));
   shipyardComponent._owner=new PayableFishingRod();
   Expect(Probe.Continue(shipyardComponent,shipyardActor),false,"原船厂支付组件改为钓鱼所有者后必须拒绝续购");
   Expect(Probe.Ready(shipyardComponent,shipyardActor),false,"不能把船厂生产许可沿用到钓鱼组件");
   Expect(shipyardState.Step(Input(shipyardComponent,shipyardActor,PurchasePhase.None)).Down,false,"船厂组件改为钓鱼所有者后不得合成下一轮付款按下");
   Check(new Wharf{Currency=CurrencyType.Coins,_availableParts=new List<object>{new object()}},player,true,"未下水且有船体零件时允许连续购买零件");
   foreach(var currency in new[]{CurrencyType.Gems,CurrencyType.Other})
    Check(new Wharf{Currency=currency,_availableParts=new List<object>{new object()}},player,false,"船体零件非金币货币不自动支付: "+currency);
   Check(new Wharf{_availableParts=new List<object>()},player,false,"无剩余零件的造船对象不得被当作可连购入口");
   Check(new Wharf(),player,false,"造船零件列表为空引用时不得自动付款");
   Check(new Wharf{_availableParts=new List<object>{new object()},_boatLaunched=true},player,false,"已经下水的造船对象不得继续自动付款");
   var finishingWharf=new Wharf{Currency=CurrencyType.Coins,_availableParts=new List<object>{new object()}};
   var wharfActor=new Player{selectedPayable=finishingWharf};var wharfState=new ContinuousPurchaseState();
   Expect(Probe.Continue(finishingWharf,wharfActor),true,"未下水且有剩余零件时可保持连续付款");
   wharfState.Step(Input(finishingWharf,wharfActor,PurchasePhase.Holding,true));wharfState.Step(Input(finishingWharf,wharfActor,PurchasePhase.Completed));
   finishingWharf._availableParts.Clear();
   Expect(Probe.Continue(finishingWharf,wharfActor),false,"最后一个船体零件买完后必须拒绝续购");
   Expect(Probe.Ready(finishingWharf,wharfActor),false,"零件耗尽后生产付款资格不能进入下水分支");
   var wharfDecision=wharfState.Step(Input(finishingWharf,wharfActor,PurchasePhase.None));
   Expect(!wharfDecision.Down&&wharfState.AwaitingRelease,true,"零件耗尽后取消会话，不能合成船下水的付款按下");
   finishingWharf._availableParts=null;Expect(Probe.Continue(finishingWharf,wharfActor),false,"持续付款阶段丢失零件列表时必须拒绝");
   finishingWharf._availableParts=new List<object>{new object()};finishingWharf._boatLaunched=true;
   Expect(Probe.Continue(finishingWharf,wharfActor),false,"持续付款阶段船已下水时必须拒绝");
   foreach(var component in new[]{typeof(Wall),typeof(Tower),typeof(Farmhouse),typeof(Castle),typeof(WorkableBuilding)}){
    var building=new PayableUpgrade{Currency=CurrencyType.Coins};building.gameObject.Components.Add(component);
    Check(building,player,true,"普通建筑金币升级: "+component.Name);
    building.Currency=CurrencyType.Gems;Check(building,player,false,"建筑非金币升级不纳入: "+component.Name);
   }
   var unknown=new Payable{Currency=CurrencyType.Coins};unknown.gameObject.Components.Add(typeof(WorkableBuilding));
   Check(unknown,player,false,"未知Payable即使含建筑组件也不能泛化接管");
   var unrelatedUpgrade=new PayableUpgrade{Currency=CurrencyType.Coins};unrelatedUpgrade.gameObject.Components.Add(typeof(UnrelatedComponent));
   Check(unrelatedUpgrade,player,false,"无已批准建筑组件的升级对象");
   var tower=new PayableUpgrade{Currency=CurrencyType.Coins};tower.gameObject.Components.Add(typeof(Tower));
   tower.passengerUpgrades=new object[]{new object()};
   Check(tower,player,true,"无人乘坐时仍可进行普通建筑升级");
   player.passenger=new NativeObject();
   Check(tower,player,false,"有乘员和特殊改造分支时不得自动选择");
   tower.passengerUpgrades=Array.Empty<object>();Check(tower,player,true,"无特殊改造分支的乘员不阻止普通升级");
   tower.passengerUpgrades=null;Check(tower,player,true,"无改造列表时保持普通升级");
   Check(null,player,false,"空目标");
   Check(new CitizenHousePayable{Alive=false,Currency=CurrencyType.Coins},player,false,"失效市民之家");
   Check(new PayableShop{Alive=false,Currency=CurrencyType.Coins},player,false,"失效装备商店");
   // 2.4.2 原生 UpdatePayState 的已验证反例：旧 selected 快捷判定失败后，
   // GetClosestPayable 仍可选中距离 0.6、playerPayDistance 为 1 的候选并进入 Holding。
   // 因此适配器不能把 min(0.5, playerPayDistance) 当作所有付款目标的距离边界。
   var nativeAccepted=new CitizenHousePayable{PayPoint=.6f,playerPayDistance=1f};
   Expect(Probe.Range(nativeAccepted,player),true,"原生可在距离0.6、付款距离1进入Holding，适配器须保留该目标");
   var actor=new Player{selectedPayable=nativeAccepted};var manager=Managers._Inst.payables;
   var other=new PayableShop{PayPoint=.2f};manager.Candidate=nativeAccepted;
   Expect(Probe.NativeSelection(nativeAccepted,actor),true,"超过旧选择快捷距离时接受原生返回的同目标");
   Expect(manager.Calls==1&&manager.LastX==0f&&manager.LastRange==.5f&&manager.LastPlayer==actor,true,"候选查询保留原生位置、0.5范围与玩家参数");
   Expect(Probe.Ready(nativeAccepted,actor),true,"距离0.6的市民之家满足原生条件后可再次付款");
   manager.Calls=0;nativeAccepted.PayPoint=.4f;manager.Candidate=other;
   Expect(Probe.NativeSelection(nativeAccepted,actor),true,"原生快捷保留已有近处选择，不能被附近其他候选替代");
   Expect(manager.Calls==0,true,"有效原生旧选择不额外重选最近目标");
   actor.selectedPayable=other;manager.Candidate=nativeAccepted;
   Expect(Probe.NativeSelection(nativeAccepted,actor),false,"原生保留其他已选目标时拒绝锁定旧目标");
   Expect(manager.Calls==0,true,"已选其他近处目标不能通过邻居查询重新抢回");
   actor.selectedPayable=nativeAccepted;nativeAccepted.playerPayDistance=.3f;manager.Candidate=other;
   Expect(Probe.NativeSelection(nativeAccepted,actor),false,"旧选择超出自身付款距离时必须遵循原生重新选择");
   Expect(manager.Calls==1,true,"距离虽小于0.5仍须满足目标自身付款距离");
   nativeAccepted.playerPayDistance=1f;nativeAccepted.PayPoint=.6f;manager.Candidate=nativeAccepted;
   var state=new ContinuousPurchaseState();state.Step(Input(nativeAccepted,actor,PurchasePhase.Holding,true));state.Step(Input(nativeAccepted,actor,PurchasePhase.Completed));
   manager.Candidate=other;
   Expect(Probe.Ready(nativeAccepted,actor),false,"selected尚未更新但原生本帧将选其他目标时禁止付款");
   var decision=state.Step(Input(nativeAccepted,actor,PurchasePhase.None));
   Expect(!decision.Down&&!decision.Held,true,"预测原生目标切换时不得合成下一轮按下");
   actor.selectedPayable=other;decision=state.Step(Input(nativeAccepted,actor,PurchasePhase.None));
   Expect(!decision.Down&&state.AwaitingRelease,true,"实际切到其他目标后终止连购并等待玩家松手");
   actor.selectedPayable=nativeAccepted;manager.Candidate=nativeAccepted;
   Expect(state.Step(Input(nativeAccepted,actor,PurchasePhase.None)).Down,false,"切目标后直接回到原目标仍不自动重启");
   state=new ContinuousPurchaseState();state.Step(Input(nativeAccepted,actor,PurchasePhase.Holding,true));state.Step(Input(nativeAccepted,actor,PurchasePhase.Completed));
   nativeAccepted.Selectable=false;manager.Candidate=null;
   Expect(Probe.Range(nativeAccepted,actor),true,"近处目标临时不可选时仍保留有效等待范围");
   Expect(Probe.Ready(nativeAccepted,actor),false,"临时不可选时不满足生产付款资格");
   bool waited=true;for(int frame=0;frame<20;frame++){
    decision=state.Step(Input(nativeAccepted,actor,PurchasePhase.None));
    waited&=!decision.Held&&!decision.Down&&state.Target==nativeAccepted.GetInstanceID()&&!state.AwaitingRelease;
   }
   Expect(waited,true,"暂时不可选连续20帧只等待，不取消锁定也不补按下");
   nativeAccepted.Selectable=true;manager.Candidate=nativeAccepted;
   Expect(state.Step(Input(nativeAccepted,actor,PurchasePhase.None)).Down,true,"同目标恢复原生条件后无需玩家松手即继续");
   nativeAccepted.PayPoint=3f;
   Expect(Probe.Range(nativeAccepted,actor),true,"几何范围外但原生明确仍选择同目标时不能假想距离取消");
   manager.Candidate=other;
   Expect(Probe.Range(nativeAccepted,actor),false,"离开几何范围且原生不再选择锁定目标才失效");
   nativeAccepted.PayPoint=.6f;manager.Candidate=nativeAccepted;
   actor.actionState=Player.ActionState.Run;
   Expect(Probe.NativeSelection(nativeAccepted,actor),false,"原生Run状态不能自动启动付款");
   Expect(Probe.Ready(nativeAccepted,actor),false,"生产付款表达式包含原生Run状态约束");
   actor.actionState=Player.ActionState.Idle;
   Expect(Probe.Ready(nativeAccepted,actor,false),false,"目标被占用不能自动付款");
   Expect(Probe.Ready(nativeAccepted,actor,true,false),false,"资金不足不能自动付款");
   nativeAccepted.PayableNow=false;Expect(Probe.Ready(nativeAccepted,actor),false,"原生CanPay拒绝仍须等待");nativeAccepted.PayableNow=true;
   nativeAccepted.forceBlockPayment=true;Expect(Probe.Ready(nativeAccepted,actor),false,"原生强制阻止付款不能绕过");nativeAccepted.forceBlockPayment=false;
   nativeAccepted.gameObject.activeInHierarchy=false;Expect(Probe.Range(nativeAccepted,actor),false,"目标所在游戏对象失活不能沿用原生候选后备");nativeAccepted.gameObject.activeInHierarchy=true;
   Expect(Probe.NativeSelection(null,actor),false,"原生确认拒绝空锁定目标");
   Expect(Probe.NativeSelection(nativeAccepted,null),false,"原生确认拒绝空玩家");
   manager.Candidate=null;Expect(Probe.NativeSelection(nativeAccepted,actor),false,"原生候选为空时不能合成付款");
   Managers._Inst=null;Expect(Probe.NativeSelection(nativeAccepted,actor),false,"原生管理器不存在时不能合成付款");
   failures.Insert(0,"cases="+count);return failures.ToArray();
  }
 }
}
$model
"@
$result=[ContinuousPurchaseAdapterTests.Cases]::Run()
if($result.Length -gt 1){throw ("连续购买原生目标适配回归失败（$($result[0])）：`n"+($result[1..($result.Length-1)] -join "`n"))}
"通过：$($result[0]) 项生产适配方法与状态模型回归；目标类别、货币限制、隐士特殊改造、原生选择距离、目标切换付款保护及临时不可选等待恢复。不代表真实招募或扣款验收。"
