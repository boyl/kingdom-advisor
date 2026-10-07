using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HarmonyLib;
using System.Reflection;

namespace KingdomAdvisor
{
    // 唯一玩法写入边界。默认不写入；单机和联机均按角色控制权执行。
    internal sealed class GameActions
    {
        private readonly Settings settings;
        private sealed class CapacityState {public Wallet Wallet;public int Original;public bool ShowBag;public CurrencyBag Bag;}
        private readonly Dictionary<int,CapacityState> capacities=new Dictionary<int,CapacityState>();
        private readonly HashSet<int> numericBags=new HashSet<int>();
        public bool UsesNumericBag(CurrencyBag bag)=>bag&&numericBags.Contains(bag.GetInstanceID());
        public void ClearResources(Snapshot s,int id,int[] currencies)
        {
            if(!CanAct(s,id,out var p))return;
            try{foreach(int type in currencies.Where(t=>ResourceRules.Supported(t)&&s.ResourceTypes.Contains(t)).Distinct()){
                int before=p.wallet.GetCurrency((CurrencyType)type);p.wallet.SetCurrency((CurrencyType)type,0);if(s.Online)p.SendWalletCurrencyCount((CurrencyType)type);Writes++;
                Plugin.Instance.Log.LogInfo("Resource clear player="+id+" type="+type+" before="+before+" after="+p.wallet.GetCurrency((CurrencyType)type));
            }var bag=p.wallet._currencyBag;if(bag){bag.Clear();bag.Refresh();}Notify("已清空所选钱袋资源");}catch(Exception ex){Notify("清空失败："+ex.Message);Plugin.Instance.Log.LogError(ex);}
        }
        public int NormalCapacity(int id){var p=Player(id);if(!p||!p.wallet)return 0;var w=p.wallet;return capacities.TryGetValue(w.GetInstanceID(),out var saved)?saved.Original:w.TotalCapacity;}
        public string WalletEvidence(int id)
        {var p=Player(id);if(!p||!p.wallet)return "";var w=p.wallet;int original=capacities.TryGetValue(w.GetInstanceID(),out var saved)?saved.Original:w.TotalCapacity;return " original="+original+" capacity="+w.TotalCapacity+" total="+w.TotalCurrency+" gems="+w.Gems+" numeric="+(saved!=null);}
        private bool staminaOwned,oldStamina,timeOwned;private float oldTime=1;
        public int Writes,Teleports,ResourceAdds;public string Message="";public float MessageUntil;
        public GameActions(Settings config){settings=config;}
        public void Notify(string message){Message=message;MessageUntil=Time.unscaledTime+5;}
        private Player Player(int id)
        {var k=Managers._Inst?.kingdom;if(!k)return null;var p=k.playerOne;if(p&&p.playerId==id)return p;p=k.playerTwo;return p&&p.playerId==id?p:null;}
        private bool CanAct(Snapshot s,int id,out Player p)
        {
            p=null;if(!s.Playing){Notify("请先进入存档");return false;}
            p=Player(id);if(!p||!p.hasLocalAuthority){Notify("目标玩家不在本机控制范围");return false;}return true;
        }
        public void Apply(Snapshot state)
        {
            bool active=state.Playing;
            foreach(var info in state.Players.Where(p=>p.Local))
            {
                var player=Player(info.Id);var wallet=player?player.wallet:null;if(!wallet)continue;
                int id=wallet.GetInstanceID();
                if(active&&settings.InfiniteBag.Value)
                {
                                        if(!capacities.ContainsKey(id)){
                        var saved=new CapacityState{Wallet=wallet,Original=wallet.TotalCapacity,ShowBag=wallet.showCurrencyBag,Bag=wallet._currencyBag};capacities[id]=saved;
                        if(saved.Bag){numericBags.Add(saved.Bag.GetInstanceID());saved.Bag.Clear();saved.Bag.HideImmediate();}
                        wallet.showCurrencyBag=false;Writes++;
                    }
                    if(wallet.TotalCapacity!=int.MaxValue){wallet.TotalCapacity=int.MaxValue;Writes++;}
                }
            }
            foreach(var item in capacities.ToArray())
            {
                var w=item.Value.Wallet;if(!w){capacities.Remove(item.Key);continue;}
                if(active&&settings.InfiniteBag.Value)continue;
                int target=item.Value.Original;
                if(w.TotalCapacity!=target){w.TotalCapacity=target;Writes++;}
                {
                    if(item.Value.Bag)numericBags.Remove(item.Value.Bag.GetInstanceID());
                    w.showCurrencyBag=item.Value.ShowBag;if(item.Value.Bag)item.Value.Bag.Refresh();
                    capacities.Remove(item.Key);Writes++;
                }
            }
            bool infinite=active&&settings.InfiniteStamina.Value;
            if(infinite&&!staminaOwned){oldStamina=global::Player.DebugInfiniteStamina;staminaOwned=true;}
            if(staminaOwned&&infinite&&global::Player.DebugInfiniteStamina!=true){global::Player.DebugInfiniteStamina=true;Writes++;}
            if(!infinite&&staminaOwned){global::Player.DebugInfiniteStamina=oldStamina;staminaOwned=false;Writes++;}
            float speed=active?settings.GameSpeed.Value:1;
            if(speed!=1&&!timeOwned){oldTime=Time.timeScale;timeOwned=true;}
            if(timeOwned){float target=speed;if(Time.timeScale!=target){SetSpeed(target);}if(speed==1)timeOwned=false;}
        }
        private void SetSpeed(float value)
        {
            var world=Managers._Inst?.world;
            if(!world)throw new InvalidOperationException("世界时间控制器尚未加载");
            world.SetTimeScale(value,true);
            if(NetworkBigBoss.IsOnline)world.SendTimescale();
            Writes++;
        }
        public void Restore()
        {
            foreach(var item in capacities.Values)if(item.Wallet){item.Wallet.TotalCapacity=item.Original;item.Wallet.showCurrencyBag=item.ShowBag;Writes++;}
            capacities.Clear();numericBags.Clear();if(staminaOwned){global::Player.DebugInfiniteStamina=oldStamina;staminaOwned=false;Writes++;}
            if(timeOwned){var world=Managers._Inst?.world;if(world)SetSpeed(oldTime);else {Time.timeScale=oldTime;Writes++;}timeOwned=false;}
        }
        public void Add(Snapshot s,int id,int currency,int amount)
        {
            if(!CanAct(s,id,out var p))return;
            if(!ResourceRules.Supported(currency)||!s.ResourceTypes.Contains(currency)){Notify("本局未发现此资源类型");return;}
            try
            {
                int before=p.wallet.GetCurrency((CurrencyType)currency),expected=ResourceRules.Addition(before,amount);
                p.wallet.AddCurrency((CurrencyType)currency,amount);Writes++;ResourceAdds++;
                if(s.Online)p.SendWalletCurrencyCount((CurrencyType)currency);
                int after=p.wallet.GetCurrency((CurrencyType)currency);
                Notify(ResourceRules.Name(currency)+"："+before+" → "+after+(after==expected?"":" · 游戏实际处理结果"));
                Plugin.Instance.Log.LogInfo("Resource add player="+id+" type="+currency+" before="+before+" after="+after);
            }
            catch(Exception ex){Notify("添加失败："+ex.Message);Plugin.Instance.Log.LogError("Resource add failed: "+ex);}
        }
        public void Teleport(Snapshot s,int id,MapPoint point)
        {
            if(!settings.Teleport.Value){Notify("地图传送尚未开启");return;}
            if(!CanAct(s,id,out var p)||point==null)return;
            if(point.X<s.Left+8||point.X>s.Right-8){Notify("此地点靠近岛屿边界，无法验证安全落点");return;}
            if(!s.Points.Any(t=>t.Name==point.Name&&Math.Abs(t.X-point.X)<1)){Notify("地点已变化，请重新选择");return;}
            try
            {
                var ground=World.GroundCollider;if(!ground){Notify("地面尚未加载");return;}
                float x=Math.Clamp(point.X,s.Left+8,s.Right-8);
                // 本机 RaycastAll 返回数组包装无效。岛屿地面是轴对齐 BoxCollider，直接验证落点。
                var bounds=ground.bounds;
                if(!ground.OverlapPoint(new Vector2(x,bounds.center.y))){Notify("此处不在已加载地面范围内");return;}
                float floor=bounds.max.y;
                var mover=p.mover;var body=mover?mover.rigidbody:null;if(!body){Notify("玩家移动组件不可用");return;}
                var previous=p.transform.position;float offset=body.position.y-ground.bounds.max.y,offsetX=body.position.x-previous.x;
                p.ReleaseInput();mover.Stop();body.linearVelocity=Vector2.zero;
                if(body.transform!=p.transform)p.transform.position=new Vector3(x,previous.y,previous.z);
                body.position=new Vector2(x+offsetX,floor+offset);Writes++;
                if(s.Online){var sync=p.GetComponent<PositionSync>();if(sync)sync.SendFullPos(false);}
                Teleports++;Notify("已传送："+point.Name);
                Plugin.Instance.Log.LogInfo("Teleport player="+id+" from="+previous.x+" target="+x+" body="+body.position.x);
            }
            catch(Exception ex){Notify("传送失败："+ex.Message);Plugin.Instance.Log.LogError("Teleport failed: "+ex);}
        }
    }
    // 无限余额使用数字袋，避免原生袋内物理资源触发掉落与再拾取循环。
    [HarmonyPatch]
    internal static class NumericBagVisualPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach(string method in new[]{"Refresh","ShowCurrency","AddCurrency","RemoveCurrency"})yield return AccessTools.Method(typeof(CurrencyBag),method);
        }
        private static bool Prefix(CurrencyBag __instance)=>!(AdvisorBehaviour.Active?.Actions?.UsesNumericBag(__instance)??false);
    }
}
