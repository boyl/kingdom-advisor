using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace KingdomAdvisor
{
    // 原生投币适配：仅提供一次新的按下边沿，扣款、飞行金币、库存与升级仍由游戏完成。
    internal static class ContinuousPurchaseRuntime
    {
        private sealed class Session
        {
            public Player Player;
            public Payable Target;
            public readonly ContinuousPurchaseState State=new();
            public readonly PurchaseInput Input=new();
            public PayableUpgrade Watched;
            public PayableUpgrade.PayableUpgradeEvent Handler;
        }
        private static readonly Dictionary<int,Session> sessions=new();
        private static readonly List<int> deadPlayers=new();
        private static bool faulted;
        private static int worldId;
        private static readonly ContinuousPurchaseTrace trace=new();
        private static int rangeTarget;
        private static float rangeDistance,rangeLimit;
        private static long traceReadErrors;
        public static int Restarts {get;private set;}
        public static int ActiveCount=>sessions.Values.Count(s=>s.State.Target!=0);
        public static string[] Evidence()=>trace.Evidence().Concat(new[]{"continuousPurchaseTraceReadErrors="+traceReadErrors}).ToArray();
        public static string[] TraceLines()=>trace.Lines();
        private static bool Enabled=>Plugin.Instance?.Settings?.ContinuousPurchase.Value??false;
        public static PurchaseNativeObservation BeforeNative(Player player,bool held,bool down,bool touch)
        {
            var observation=new PurchaseNativeObservation{PhysicalPress=held&&down&&!touch,Held=held,Down=down,Touch=touch};
            try
            {
                if(!Enabled||!player||!player.hasLocalAuthority)return observation;
                observation.Observed=true;observation.Before=(int)player._payState;rangeTarget=0;
                trace.ObserveCall(held,down,touch);
            }
            catch{traceReadErrors++;observation.Observed=false;}
            return observation;
        }
        public static void AfterNative(Player player,PurchaseNativeObservation observation)
        {
            try
            {
                if(!observation.Observed||!player)return;
                trace.ObserveAfter();sessions.TryGetValue(player.GetInstanceID(),out var session);
                Record(PurchaseTraceStage.NativeAfter,PurchaseTraceReason.NativeResult,player,session,observation.Held,observation.Down,observation.Touch,observation.ForwardHeld,observation.ForwardDown,observation.Before);
            }
            catch{traceReadErrors++;}
        }
        private static void Record(PurchaseTraceStage stage,PurchaseTraceReason reason,Player player,Session session,bool held=false,bool down=false,bool touch=false,bool forwardHeld=false,bool forwardDown=false,int before=-1)
        {
            try
            {
            if(!Enabled||!player)return;
            var selected=player.selectedPayable;int after=(int)player._payState;
            var value=new PurchaseTraceEvent{Frame=Time.frameCount,Player=player.playerId,Stage=stage,Reason=reason,Before=before<0?after:before,After=after,Held=held,Down=down,Touch=touch,ForwardHeld=forwardHeld,ForwardDown=forwardDown,Selected=selected?selected.GetInstanceID():0,Target=session?.State.Target??0,AwaitingRelease=session?.State.AwaitingRelease??false};
            if(!trace.NeedsRecord(value))return;
            // 字符串与距离只为实际状态变化保存，所有文件写入由既有十秒诊断周期执行。
            value.SelectedName=selected?selected.name:"none";value.TargetName=session?.Target?session.Target.name:"none";
            value.SelectedType=selected?selected.GetIl2CppType().Name:"none";value.TargetType=session?.Target?session.Target.GetIl2CppType().Name:"none";
            bool measured=rangeTarget!=0&&(rangeTarget==value.Selected||rangeTarget==value.Target);
            value.Distance=measured?rangeDistance:float.NaN;value.Limit=measured?rangeLimit:float.NaN;
            trace.Record(value);
            }
            // 只读诊断缺失单列计数，不能导致游戏付款辅助的控制流变化。
            catch{traceReadErrors++;}
        }
        private static bool Playing()
        {
            var m=Managers._Inst;
            return m&&m.game&&m.kingdom&&m.game.state==Game.State.Playing;
        }
        private static bool IsNetworkClient()
        {
            var m=Managers._Inst;
            return m&&m.game&&m.game.state==Game.State.NetworkClientPlaying;
        }
        private static bool Allowed(Player player,bool usingTouch)
        {
            var active=AdvisorBehaviour.Active;var controller=active?.Controller;
            return !usingTouch&&Playing()&&Application.isFocused&&Time.timeScale>0&&active?.InputAvailable==true&&controller!=null&&!controller.Blocks(player.playerId)&&player.CanInteractWithWorld&&!player.isOnBoat&&player.actionState!=Player.ActionState.Run;
        }
        public static void Tick()
        {
            if(!Enabled){ResetAll();faulted=false;return;}
            var m=Managers._Inst;
            int current=Playing()?m.kingdom.GetInstanceID():0;
            if(current!=worldId){CancelAll();worldId=current;}
            bool valid=current!=0&&Application.isFocused&&Time.timeScale>0&&AdvisorBehaviour.Active?.InputAvailable==true;
            deadPlayers.Clear();
            foreach(var pair in sessions)
            {
                var session=pair.Value;
                if(!session.Player){Detach(session);deadPlayers.Add(pair.Key);continue;}
                if(!valid||!session.Player.hasLocalAuthority||AdvisorBehaviour.Active.Controller.Blocks(session.Player.playerId))Cancel(session);
            }
            foreach(int id in deadPlayers)sessions.Remove(id);
            if(faulted)CancelAll();
        }
        public static void CancelAll(){foreach(var session in sessions.Values)Cancel(session);}
        public static void ResetAll(){foreach(var session in sessions.Values)Detach(session);sessions.Clear();worldId=0;}
        private static void Cancel(Session session)
        {
            session.State.Cancel();Record(PurchaseTraceStage.Lifecycle,PurchaseTraceReason.Cancelled,session.Player,session);Detach(session);session.Target=null;CancelPendingHold(session);
        }
        private static void CancelPendingHold(Session session)
        {
            // 原生 Holding+松手会丢一枚金币。只取消尚未开始交易的 Holding；已扣款的交易仍原生结算。
            try {if(session.Player&&session.Player._payState==Player.PayState.Holding)session.Player.CancelTransaction();}
            catch(Exception ex){RecordFault(ex);}
        }
        private static void Detach(Session session)
        {
            var watched=session.Watched;var handler=session.Handler;
            session.Watched=null;session.Handler=null;
            // 清引用必须先于原生解绑；销毁中的对象即使解绑失败，也不能让 Fail 再次解绑而递归抛错。
            try {if(watched&&handler!=null)watched.remove_OnPayableUpgraded(handler);}
            catch(Exception ex){RecordFault(ex);}
        }
        private static void Bind(Session session,Payable target)
        {
            Detach(session);if(faulted)return;session.Target=target;
            var upgrade=target?target.TryCast<PayableUpgrade>():null;
            if(!upgrade)return;
            int oldId=target.GetInstanceID();session.Watched=upgrade;
            session.Handler=(Action<GameObject>)(created=>{
                try
                {
                    if(!created||session.State.Target!=oldId)return;
                    var component=created.GetComponent(Il2CppType.Of<Payable>());
                    var successor=component?component.TryCast<Payable>():null;
                    if(!Supported(successor,session.Player)||!session.State.AcceptSuccessor(oldId,successor.GetInstanceID())){Record(PurchaseTraceStage.Upgrade,PurchaseTraceReason.UpgradeRejected,session.Player,session);Cancel(session);return;}
                    Bind(session,successor);
                    Record(PurchaseTraceStage.Upgrade,PurchaseTraceReason.UpgradeAccepted,session.Player,session);
                    if(faulted)CancelAll();
                }
                catch(Exception ex){Fail(ex);}
            });
            upgrade.add_OnPayableUpgraded(session.Handler);
        }
        private static bool Has<T>(GameObject target) where T:Component=>target.GetComponent(Il2CppType.Of<T>());
        private static bool Supported(Payable target,Player player)
        {
            if(!target||target.Currency!=CurrencyType.Coins)return false;
            // 循环共用原生 UpdatePayState；这里仅集中界定可重复购买/招募的入口。
            var shop=target.TryCast<PayableShop>();
            if(shop)return OrdinaryShop(shop);
            if(target.TryCast<CitizenHousePayable>()||target.TryCast<PayableWorkshop>()||target.TryCast<PayableWorkshopBarrel>()||target.TryCast<PayableShield>())return true;
            var wharf=target.TryCast<Wharf>();
            if(wharf)return WharfParts(wharf);
            var forge=target.TryCast<PayableForge>();
            if(forge)return ArmorForge(forge);
            // PayableComponent 同时承载弹药、祭坛、钓鱼和航行，须按真实 owner 区分。
            var component=target.TryCast<PayableComponent>();
            if(component)return SuppliesOrShipyard(component);
            var upgrade=target.TryCast<PayableUpgrade>();
            if(!upgrade)return false;
            // 有隐士乘员时可能选择改造分支；本功能不替玩家选择该分支。
            if(player&&player.passenger&&upgrade.passengerUpgrades!=null&&upgrade.passengerUpgrades.Length>0)return false;
            var obj=target.gameObject;
            return Has<Wall>(obj)||Has<Tower>(obj)||Has<Farmhouse>(obj)||Has<Castle>(obj)||Has<WorkableBuilding>(obj);
        }
        private static bool WharfParts(Wharf wharf)=>wharf&&!wharf._boatLaunched&&wharf._availableParts!=null&&wharf._availableParts.Count>0;
        private static bool SuppliesOrShipyard(PayableComponent component)=>component&&component._owner!=null&&(component._owner.TryCast<FireTower>()||component._owner.TryCast<Shipyard>());
        private static bool OrdinaryShop(PayableShop shop)
        {
            if(!shop||shop.TryCast<ChangeRulerShop>()||shop.TryCast<ChangeItemOfPowerShop>())return false;
            var sided=shop.TryCast<PayableSidedShop>();
            return !sided||sided.shopType==PayableShop.UnsidedShopType.Pike||sided.shopType==PayableShop.UnsidedShopType.Ninja||sided.shopType==PayableShop.UnsidedShopType.Workshop||sided.shopType==PayableShop.UnsidedShopType.ShieldShop;
        }
        private static bool ArmorForge(PayableForge forge)=>forge&&forge.side==Side.Left&&forge.armorPrefab;
        private static bool InRange(Payable target,Player player)
        {
            if(!target||!target.gameObject.activeInHierarchy)return false;
            // 0.5 是保留旧选择的快捷条件，不是付款上限。原生邻居查询使用支付距离加 0.5；
            // 首候选还可能在此范围之外，因此以原生仍选中锁定对象作为后备。
            // 暂时不可选（例如施工）的近处目标仍有效，只等待 CanPay 恢复。
            rangeTarget=target.GetInstanceID();rangeDistance=Math.Abs(target.PlayerPayPoint()-player.transform.position.x);rangeLimit=target.playerPayDistance+.5f;
            return rangeDistance<=rangeLimit||NativeSelectsTarget(target,player);
        }
        private static bool NativeSelectsTarget(Payable target,Player player)
        {
            if(!target||!player||player.actionState==Player.ActionState.Run)return false;
            float x=player.transform.position.x;
            var selected=player.selectedPayable;
            if(selected&&selected.CanSelect(player))
            {
                float distance=Math.Abs(selected.PlayerPayPoint()-x);
                if(distance<=.5f&&distance<=selected.playerPayDistance)return selected.GetInstanceID()==target.GetInstanceID();
            }
            var m=Managers._Inst;
            if(!m||!m.payables)return false;
            var candidate=m.payables.GetClosestPayable(x,.5f,player);
            return candidate&&candidate.GetInstanceID()==target.GetInstanceID();
        }
        private static PurchasePhase Phase(Player player)=>player._payState switch {
            Player.PayState.None=>PurchasePhase.None,Player.PayState.Holding=>PurchasePhase.Holding,
            Player.PayState.Transaction=>PurchasePhase.Transaction,Player.PayState.Completed=>PurchasePhase.Completed,
            Player.PayState.Cancelling=>PurchasePhase.Cancelling,
            _=>PurchasePhase.Other};
        public static void Filter(Player player,ref bool held,ref bool down,bool usingTouch)
        {
            if(!player||!player.hasLocalAuthority)return;
            try
            {
                int id=player.GetInstanceID();
                sessions.TryGetValue(id,out var session);
                if(session!=null&&!held)
                {
                    var release=session.Input;release.Enabled=true;release.Held=false;release.Down=down;release.Phase=Phase(player);
                    if(session.State.Step(release).CancelHolding)CancelPendingHold(session);
                    Record(PurchaseTraceStage.Filter,PurchaseTraceReason.Released,player,session,held,down,usingTouch,held,down);
                    Detach(session);sessions.Remove(id);return;
                }
                if(!Enabled)return;
                if(faulted){Record(PurchaseTraceStage.Filter,PurchaseTraceReason.Faulted,player,session,held,down,usingTouch,held,down);return;}
                if(IsNetworkClient()){Record(PurchaseTraceStage.Filter,PurchaseTraceReason.Client,player,session,held,down,usingTouch,held,down);return;}
                if(session==null)
                {
                    var initial=player.selectedPayable;
                    if(!held||!down){Record(PurchaseTraceStage.Filter,PurchaseTraceReason.NoPress,player,session,held,down,usingTouch,held,down);return;}
                    if(usingTouch){Record(PurchaseTraceStage.Filter,PurchaseTraceReason.Touch,player,session,held,down,usingTouch,held,down);return;}
                    if(!Supported(initial,player)){Record(PurchaseTraceStage.Filter,PurchaseTraceReason.Unsupported,player,session,held,down,usingTouch,held,down);return;}
                    if(!InRange(initial,player)){Record(PurchaseTraceStage.Filter,PurchaseTraceReason.OutsideRange,player,session,held,down,usingTouch,held,down);return;}
                    if(!initial.CanSelect(player)||!NativeSelectsTarget(initial,player)){Record(PurchaseTraceStage.Filter,PurchaseTraceReason.CannotSelect,player,session,held,down,usingTouch,held,down);return;}
                    session=new Session{Player=player};sessions[id]=session;
                    Record(PurchaseTraceStage.Filter,PurchaseTraceReason.Created,player,session,held,down,usingTouch,held,down);
                }
                bool allowed=Allowed(player,usingTouch);
                var selected=player.selectedPayable;
                if(session.State.Target==0&&!session.State.AwaitingRelease&&down&&Supported(selected,player))Bind(session,selected);
                if(faulted){held=down=false;CancelAll();return;}
                var target=session.Target;var phase=Phase(player);
                bool valid=InRange(target,player)&&SupportedForContinuation(target,player);
                bool selectedSame=target&&selected&&target.GetInstanceID()==selected.GetInstanceID();
                // CanSelect 也必须满足，否则原生会在同一次调用内改选最近目标。
                var interactor=target?target.interactingPlayer:null;
                bool available=!interactor||interactor==player||!interactor.gameObject.activeInHierarchy;
                int coins=player.wallet?player.wallet.GetCurrency(CurrencyType.Coins):0;
                // 真实首轮保留原生部分投入；辅助重启须付得起整笔，避免不足款反复进入原生退币。
                bool funded=coins>0&&(down||phase!=PurchasePhase.None||target&&coins>=target.Price);
                bool canPay=valid&&selectedSame&&available&&funded&&target.isActiveAndEnabled&&target.CanSelect(player)&&!target.forceBlockPayment&&target.CanPay(player)&&NativeSelectsTarget(target,player);
                var input=session.Input;input.Enabled=true;input.Held=held;input.Down=down;input.Allowed=allowed&&phase!=PurchasePhase.Other;
                input.SelectedTarget=selected?selected.GetInstanceID():0;input.SupportedTarget=target?target.GetInstanceID():0;
                input.TargetValid=valid;input.CanPay=canPay;input.Phase=phase;
                bool realHeld=held,realDown=down;var result=session.State.Step(input);
                if(result.CancelHolding)CancelPendingHold(session);
                if(result.Override){held=result.Held;down=result.Down;if(down&&!realDown)Restarts++;}
                var reason=!input.Allowed?PurchaseTraceReason.Blocked:!valid?PurchaseTraceReason.InvalidTarget:session.State.AwaitingRelease?PurchaseTraceReason.AwaitingRelease:down&&!realDown?PurchaseTraceReason.Restart:!selectedSame?PurchaseTraceReason.NoSelection:!available?PurchaseTraceReason.Occupied:!funded?PurchaseTraceReason.Funds:phase==PurchasePhase.Completed||phase==PurchasePhase.Cancelling?PurchaseTraceReason.NativeWait:!canPay?PurchaseTraceReason.CannotPay:PurchaseTraceReason.Original;
                Record(PurchaseTraceStage.Filter,reason,player,session,realHeld,realDown,usingTouch,held,down);
                if(session.State.AwaitingRelease){Detach(session);session.Target=null;}
            }
            catch(Exception ex){held=down=false;Fail(ex);}
        }
        public static void ObserveFirstPress(Player player,bool physicalPress)
        {
            if(!physicalPress||!Enabled||faulted||!player||!player.hasLocalAuthority||IsNetworkClient())return;
            try
            {
                int id=player.GetInstanceID();
                if(sessions.ContainsKey(id)||player._payState!=Player.PayState.Holding||!Allowed(player,false))return;
                var target=player.selectedPayable;
                if(!Supported(target,player)||!InRange(target,player))return;
                // 入范围与按下可能发生同帧。只观察原生已选中且已开始的首轮，绝不自行找最近目标。
                var session=new Session{Player=player};sessions[id]=session;Bind(session,target);
                if(faulted){CancelAll();return;}
                var input=session.Input;input.Enabled=input.Held=input.Down=input.Allowed=input.TargetValid=input.CanPay=true;
                input.SelectedTarget=input.SupportedTarget=target.GetInstanceID();input.Phase=PurchasePhase.Holding;
                session.State.Step(input);
                Record(PurchaseTraceStage.Filter,PurchaseTraceReason.FirstPressAdopted,player,session,true,true,false,true,true);
            }
            catch(Exception ex){Fail(ex);}
        }
        private static bool SupportedForContinuation(Payable target,Player player)
        {
            if(!target||target.Currency!=CurrencyType.Coins)return false;
            // 会话已核实目标类型；续投只复核会随上下文改变的分支，避免改投炸弹或特殊交互。
            var shop=target.TryCast<PayableShop>();
            if(shop)return OrdinaryShop(shop);
            var wharf=target.TryCast<Wharf>();
            if(wharf)return WharfParts(wharf);
            var forge=target.TryCast<PayableForge>();
            if(forge)return ArmorForge(forge);
            var component=target.TryCast<PayableComponent>();
            if(component)return SuppliesOrShipyard(component);
            var upgrade=target.TryCast<PayableUpgrade>();
            return !upgrade||!player.passenger||upgrade.passengerUpgrades==null||upgrade.passengerUpgrades.Length==0;
        }
        private static void Fail(Exception ex)
        {
            RecordFault(ex);CancelAll();
        }
        private static void RecordFault(Exception ex)
        {
            if(faulted)return;
            faulted=true;Plugin.Instance.Log.LogError("Continuous purchase stopped: "+ex);
        }
    }
    [HarmonyPatch(typeof(Player),"UpdatePayState")]
    internal static class ContinuousPurchaseInputPatch
    {
        private static void Prefix(Player __instance,ref bool __0,ref bool __1,bool __2,out PurchaseNativeObservation __state)
        {
            __state=ContinuousPurchaseRuntime.BeforeNative(__instance,__0,__1,__2);
            ContinuousPurchaseRuntime.Filter(__instance,ref __0,ref __1,__2);
            __state.ForwardHeld=__0;__state.ForwardDown=__1;
        }
        private static void Postfix(Player __instance,PurchaseNativeObservation __state)
        {
            ContinuousPurchaseRuntime.ObserveFirstPress(__instance,__state.PhysicalPress);
            ContinuousPurchaseRuntime.AfterNative(__instance,__state);
        }
    }
}
