using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomAdvisor
{
    public enum GestureResult { None, Inspect, Teleport }
    public sealed class MapHold
    {
        public string Target="";public int Owner;public float Started;public bool Active,Latched,CanTeleport;
        public float Progress(float now)=>Active&&!Latched&&CanTeleport?Math.Clamp((now-Started)/.8f,0,1):0;
        public void Begin(string target,int owner,float now,bool teleport)
        {if(Active)return;Target=target;Owner=owner;Started=now;CanTeleport=teleport;Active=true;Latched=false;}
        public GestureResult Step(string target,bool held,float now,bool valid)
        {
            if(!Active)return GestureResult.None;
            if(!valid||target!=Target)Latched=true;
            if(!held){bool inspect=!Latched&&(!CanTeleport||now-Started<.18f);Reset();return inspect?GestureResult.Inspect:GestureResult.None;}
            if(!Latched&&CanTeleport&&now>=Started+.8f){Latched=true;return GestureResult.Teleport;}
            return GestureResult.None;
        }
        public void Cancel(){if(Active)Latched=true;}
        public void Reset(){Target="";Active=Latched=CanTeleport=false;}
    }
    public enum OptionId { Language, Map,Background,Position,FullMap,Trees,Buildings,Camps,Enemies,Mounts,Special,Kingdom,Player,AdvisorBackground,PlayerBackground,AlertBackground,InterfaceBackground,Population,Details,Alerts,Defense,Staff,MountInfo,Icons,Journal,Font,ResetLayout,InfiniteBag,Resources,Teleport,InfiniteStamina,Speed,PadCursor,PadSpeed,PadOpen }
    public sealed class OptionItem
    {
        public OptionId Id;public string Name;public string[] Choices;
        public OptionItem(OptionId id,string name,params string[] choices){Id=id;Name=name;Choices=choices.Length==0?new[]{"关闭","开启"}:choices;}
    }
    public static class OptionGroups
    {
        public static readonly string[] Names={"地图","顾问与提醒","图鉴与进度","资源辅助","移动与时间","手柄"};
        public static readonly OptionItem[][] Items={
            new[]{new OptionItem(OptionId.Map,"小地图"),new OptionItem(OptionId.Background,"地图背景","透明","面板"),new OptionItem(OptionId.Position,"默认位置","顶部","底部"),new OptionItem(OptionId.FullMap,"情报范围","本次探索","全岛"),new OptionItem(OptionId.Trees,"树木"),new OptionItem(OptionId.Buildings,"建筑"),new OptionItem(OptionId.Camps,"营地与人口"),new OptionItem(OptionId.Enemies,"敌人"),new OptionItem(OptionId.Mounts,"坐骑"),new OptionItem(OptionId.Special,"特殊地点")},
            new[]{new OptionItem(OptionId.Kingdom,"王国顾问卡片"),new OptionItem(OptionId.Player,"附近建筑详情"),new OptionItem(OptionId.AdvisorBackground,"顾问背景","透明","面板"),new OptionItem(OptionId.PlayerBackground,"附近详情背景","透明","面板"),new OptionItem(OptionId.AlertBackground,"提醒背景","透明","面板"),new OptionItem(OptionId.Population,"人口与科技"),new OptionItem(OptionId.Details,"用途与升级建议"),new OptionItem(OptionId.Alerts,"情境提醒"),new OptionItem(OptionId.Defense,"防线风险提醒"),new OptionItem(OptionId.Staff,"建设与招募缺口"),new OptionItem(OptionId.MountInfo,"坐骑能力状态")},
            new[]{new OptionItem(OptionId.Language,"语言 / Language","自动","简体中文","English"),new OptionItem(OptionId.InterfaceBackground,"主界面背景","透明","面板"),new OptionItem(OptionId.Icons,"图鉴图标"),new OptionItem(OptionId.Journal,"岛屿进度记录"),new OptionItem(OptionId.Font,"字号","14","16","18","20","22","24","26","28"),new OptionItem(OptionId.ResetLayout,"恢复卡片默认位置","执行")},
            new[]{new OptionItem(OptionId.InfiniteBag,"无限钱袋容量"),new OptionItem(OptionId.Resources,"手动添加资源","打开")},
            new[]{new OptionItem(OptionId.Teleport,"地图长按传送"),new OptionItem(OptionId.InfiniteStamina,"无限耐力"),new OptionItem(OptionId.Speed,"游戏速度","暂停","正常","2 倍","4 倍")},
            new[]{new OptionItem(OptionId.PadCursor,"常驻手柄光标"),new OptionItem(OptionId.PadSpeed,"光标速度","慢","标准","快","很快"),new OptionItem(OptionId.PadOpen,"呼出按键","左摇杆按压 L3","右摇杆按压 R3","Back／Select")}
        };
        public static bool IsGameplay(OptionId id)=>id==OptionId.InfiniteBag||id==OptionId.Teleport||id==OptionId.InfiniteStamina||id==OptionId.Speed||id==OptionId.Resources;
    }
    public static class ResourceRules
    {
        public static int Addition(int current,int amount)
        {if(amount<1||amount>10000)throw new ArgumentOutOfRangeException(nameof(amount),"单次数量为 1–10000");return checked(current+amount);}
        public static string Name(int id)=>id==0?"金币":id==1?"宝石":id==3?"骷髅":id==4?"灵魂":"未知资源";
        public static bool Supported(int id)=>id==0||id==1||id==3||id==4;
    }
    public sealed class JournalRecord
    {public int Island{get;set;}public int Day{get;set;}public string Utc{get;set;}public string[] Lines{get;set;}}
    public static class AdvisorAlerts
    {
        public static string Defense(Snapshot s,PlayerInfo player)
        {
            var walls=s.Points.Where(p=>p.Key=="wall"&&p.PlayerStates.TryGetValue(player.Id,out var t)&&t.HealthMax>0&&!t.UnderConstruction).OrderBy(p=>p.X).ToArray();
            if(walls.Length==0)return "";
            foreach(var wall in new[]{walls[0],walls[walls.Length-1]}.Distinct())
            {
                var t=wall.PlayerStates[player.Id];string side=wall.X<player.X?"左侧":"右侧";
                if(t.Health<=0)return side+"防线城墙已失守";
                if(t.Health<t.HealthMax*.5f)return side+"最外城墙耐久不足一半";
                if(s.Enemies.Any(e=>Math.Abs(e.X-wall.X)<18))return side+"敌群接近最外城墙";
            }
            return "";
        }
        public static string Staff(Snapshot s,int player)
        {
            int works=s.Points.Count(p=>p.PlayerStates.TryGetValue(player,out var t)&&t.UnderConstruction);
            if(works>0&&s.Workers==0)return "施工 "+works+" 处 · 缺少工匠";
            int empty=s.Points.Count(p=>p.PlayerStates.TryGetValue(player,out var t)&&t.Stock==0);
            if(empty>0&&s.Beggars>0)return "可招募 "+s.Beggars+" 人 · "+empty+" 处工具库存为空";
            if(s.Workers==0)return "缺少工匠 · 建设和维修可能停滞";
            return "";
        }
    }
}
