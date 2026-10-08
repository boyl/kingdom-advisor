using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace KingdomAdvisor
{
    public sealed class Entry
    {
        public readonly string Key, Name, Description, Advice, Category;
        public Entry(string key,string name,string description,string advice,string category)
        { Key=key;Name=name;Description=description;Advice=advice;Category=category; }
    }
    public static class Catalog
    {
        public static readonly Entry[] Entries = {
            new Entry("bombpurchase","炸弹出征旗","购买炸弹并组织部队护送，用于进攻悬崖洞穴。","出征前准备工匠、军队和金币，确认护送路线安全。","军队"),
            new Entry("wall","城墙","阻挡敌人，保护城内人员。","升级会进入施工阶段；危险时段先确认防线仍能承受攻击。","防御"),
            new Entry("tower","塔楼","提供驻守位置，协助防守附近区域。","不同等级与特殊改造用途不同；升级目标以当前对象为准。","防御"),
            new Entry("castle","城堡／营地中心","提升王国建设阶段，解锁对应设施。","科技解锁与城堡等级是不同前置条件。","建设"),
            new Entry("campfire","营地中心","王国的中心，也是早期建设入口。","先稳定招募、工具和两侧防线，再向外扩张。","建设"),
            new Entry("farmhouse","农舍","支持周边农田生产，升级后用途随主题和等级变化。","需要农民和可用农田；冬季应留意收入来源。","经济"),
            new Entry("farm","农田","供农民耕作并产生收入。","建设后还需要人员；数量不等于正在生产的数量。","经济"),
            new Entry("beggarcamp","游民营地","招募新成员的来源。","扩张或清理附近森林前，留意营地是否会受到影响。","人口"),
            new Entry("beggar","游民","可招募成为王国成员。","招募后需要合适的工具才能从事对应职业。","人口"),
            new Entry("citizenhouse","市民之家","提供招募人口的入口。","费用及招募状态以实际对象为准。","人口"),
            new Entry("bow","弓箭工具","用于配置弓箭手，支持狩猎和防御。","工具库存与已配置人员是两项不同统计。","工具"),
            new Entry("hammer","工匠工具","用于配置工匠，承担建造和维修。","扩张前保证工匠能安全抵达施工位置。","工具"),
            new Entry("scythe","农民工具","用于配置农民，支持农田生产。","应同时检查可用农田与农民人数。","工具"),
            new Entry("pike","长枪工具","用于配置对应近战守卫。","用途与可用性随主题变化。","工具"),
            new Entry("shield","盾牌／军队设施","用于军队相关配置或招募。","确认左右防线和军队配置后再投入资源。","工具"),
            new Entry("ninja","忍者设施","幕府主题的特殊人员设施。","观察人员行为与当前防线需求。","工具"),
            new Entry("forge","锻造设施","提供当前主题的装备或军队相关升级。","具体购买结果以运行时升级目标为准。","工具"),
            new Entry("workshop","工坊","生产防御或攻城相关设施。","留意人员需求与设施移动到防线的时间。","工具"),
            new Entry("catapult","投石车","支援防线的重型设施。","建设与使用需要相应人员；保持移动路线安全。","防御"),
            new Entry("ballista","弩炮设施","提供特殊防御能力。","特殊改造可能需要对应隐士。","防御"),
            new Entry("baker","面包设施","与吸引游民和人口补充有关。","关注消耗与实际招募结果。","人口"),
            new Entry("lighthouse","灯塔","改善岛屿航行相关设施。","不同等级需要对应科技；先看升级前置条件。","航行"),
            new Entry("wharf","码头","船只与航行的交互位置。","出航前检查人员、资源和当前岛屿防线。","航行"),
            new Entry("boat","船只","用于岛屿间航行或当前主题的船队活动。","施工、登船与出航是不同阶段。","航行"),
            new Entry("shipyard","船坞","与船只建造和船队有关。","费用和任务条件随战役变化。","航行"),
            new Entry("teleporter","传送设施","用于快速移动或建立传送连接。","先确认目的地安全与当前费用。","探索"),
            new Entry("portal","敌方传送门","敌人相关的重要据点。","进攻前检查兵力、资源及反击风险。","威胁"),
            new Entry("cave","洞穴／山体据点","战役推进相关的重要位置。","先确认当前战役的攻城条件。","威胁"),
            new Entry("mine","科技设施","解锁更高阶段的建设科技。","金币支付与科技解锁不同于携带矿石材料。","科技"),
            new Entry("quarry","科技设施","解锁对应建设阶段。","主题不同，设施名称与外观可能不同。","科技"),
            new Entry("statue","雕像","提供增益、任务或特殊交互。","确认支付货币、持续状态和当前主题。","特殊"),
            new Entry("monument","纪念碑／特殊设施","与特殊能力、科技或任务有关。","先看实际锁定条件和支付结果。","特殊"),
            new Entry("hermit","隐士","与建筑特殊改造相关。","改造时可能需要带上对应隐士。","特殊"),
            new Entry("cabin","隐士小屋","特殊角色的解锁或招募入口。","解锁费用与后续改造费用需要分别考虑。","特殊"),
            new Entry("chest","宝箱","提供资源或任务物品。","货币种类以当前对象为准。","探索"),
            new Entry("bank","银行设施","与金币储存和经济管理有关。","钱袋金币与银行储存应分别理解。","经济"),
            new Entry("merchant","商人","提供主题相关的经济交互。","观察当前费用与交付状态。","经济"),
            new Entry("berry","浆果丛","季节性经济资源。","留意人员需求与当前季节。","经济"),
            new Entry("tree","树木","可以付费砍伐，影响森林和可建设区域。","砍树前检查附近游民营地与经济资源。","环境"),
            new Entry("steed","坐骑","提供移动能力及主题相关特殊能力。","观察耐力、饱食和疲劳状态。","坐骑"),
            new Entry("horse","马匹","提供移动能力。","奔跑消耗耐力；停留、步行和进食状态影响恢复。","坐骑"),
            new Entry("griffin","狮鹫","具有特殊能力的坐骑。","能力可用性以当前玩家状态为准。","坐骑"),
            new Entry("stable","马厩","与坐骑管理有关。","具体容量与可用条件以当前对象为准。","坐骑"),
            new Entry("trojan","木马设施","奥林匹斯战役相关任务设施。","按实际缺少条件准备轮子、王冠等任务要素。","特殊"),
            new Entry("serpent","巨蛇相关设施","对应战役的特殊目标。","以当前任务和锁定提示为准。","特殊"),
            new Entry("hephaestus","赫淮斯托斯设施","奥林匹斯主题的特殊设施。","查看当前任务条件和费用。","特殊"),
            new Entry("persephone","珀耳塞福涅设施","奥林匹斯主题的特殊设施。","查看任务条件与角色要求。","特殊"),
            new Entry("artemis","阿耳忒弥斯设施","奥林匹斯主题的特殊设施。","查看任务条件与角色要求。","特殊"),
            new Entry("thor","雷神相关设施","北欧主题的特殊设施。","按当前任务提示操作。","特殊"),
            new Entry("hel","海拉相关设施","北欧主题的特殊设施。","按当前任务提示操作。","特殊")
            ,new Entry("bell","召集铃","召集当前主题的相关人员或队伍。","查看实际费用，出航前确认随行人员。","航行")
            ,new Entry("barrel","火焰桶","用于对应防御设施的消耗品。","购买后还需要人员与相应设施。","防御")
            ,new Entry("gembank","宝石储存","管理当前战役中的宝石储存。","钱袋持有和存放数量应分别理解。","经济")
            ,new Entry("gemguard","宝石保管员","与宝石存取有关的交互入口。","确认当前交互需要的货币。","经济")
            ,new Entry("grave","君主墓地","与君主、能力或主题解锁有关。","具体结果取决于当前战役。","特殊")
            ,new Entry("border","边界设施","与王国扩张或对应主题的边界能力有关。","先确认防线与施工区域。","建设")
        };
        public static string Clean(string raw) => (raw ?? "").Replace("(Clone)","").Replace("_undeveloped"," · 未建造").Replace("_Wood"," · 木制").Replace("_Stone"," · 石制").Replace("_Iron"," · 铁制");
        public static Entry Resolve(string raw)
        {
            var name=(raw??"").ToLowerInvariant().Replace("_","").Replace(" ","");
            if(name.StartsWith("bombbanner")||name.StartsWith("payablebombleft")||name.StartsWith("payablebombright")||name.StartsWith("payablebombpurchase"))return Entries.First(e=>e.Key=="bombpurchase");
            if(name.StartsWith("bombableportal"))return Entries.First(e=>e.Key=="cave");
            if(name=="wreck"||name=="wreck(clone)")return Entries.First(e=>e.Key=="boat");
            if(name=="keep"||name=="keep(clone)"||Regex.IsMatch(name,@"^keep[0-9]+(\(clone\))?$"))return Entries.First(e=>e.Key=="castle");
            if(name.Contains("hermithouse"))return Entries.First(e=>e.Key=="cabin");
            if(name.Contains("hermit"))return Entries.First(e=>e.Key=="hermit");
            if(name.Contains("castleshieldshop"))return Entries.First(e=>e.Key=="shield");
            if(name.StartsWith("stag")||name.StartsWith("mountarea")||name.StartsWith("meadow")||name.StartsWith("waterfall"))return Entries.First(e=>e.Key=="steed");
            if(name.StartsWith("beggarcamp"))return Entries.First(e=>e.Key=="beggarcamp");
            if(name.StartsWith("gembank"))return Entries.First(e=>e.Key=="gembank");
            if(name.StartsWith("gemguard"))return Entries.First(e=>e.Key=="gemguard");
            if(name.StartsWith("summonbell"))return Entries.First(e=>e.Key=="bell");
            if(name.StartsWith("kinggrave"))return Entries.First(e=>e.Key=="grave");
            if(name.Contains("birch")||name.Contains("oak")||name.Contains("pine")) return Entries.First(e=>e.Key=="tree");
            // 先匹配具体设施，避免 Tower Baker、Teleporter 等被宽泛名称抢先命中。
            string[] specific={"baker","ballista","teleporter","beggarcamp","farmhouse","catapult","lighthouse","shipyard","workshop","hephaestus","persephone","artemis","trojan"};
            foreach(var key in specific) if(name.Contains(key)) return Entries.First(e=>e.Key==key);
            foreach(var e in Entries) if(e.Key!="hel"&&name.Contains(e.Key))return e;
            if(name.StartsWith("hel"))return Entries.First(e=>e.Key=="hel");
            return new Entry("unknown",Clean(raw),"该对象尚未收录专属说明；费用和条件来自当前游戏。","请以运行时提示为准。","未分类");
        }
        public static string Lock(string reason)
        {
            switch(reason){
                case "NotLocked":return "条件已满足";
                case "StoneTechRequired":return "需要解锁对应的中阶建设科技";
                case "IronTechRequired":return "需要解锁对应的高阶建设科技";
                case "InvalidRegion":return "当前区域不允许建设；检查王国边界和森林";
                case "InvalidTime":return "当前时机不允许；等待可用时段或冷却结束";
                case "NoUpgrade":return "当前没有可用升级";
                case "HermitLocked":return "需要解锁或带上对应隐士";
                case "NeedsPassengerBallista":return "需要带上弩炮隐士";
                case "NeedsPassengerKnight":return "需要带上骑士隐士";
                case "NeedsPassengerHorse":return "需要带上马厩隐士";
                case "NeedsPassengerBaker":return "需要带上面包师隐士";
                case "NeedsPassengerHorn":return "需要带上号角隐士";
                case "NeedsPassengerPersephone":return "需要对应的珀耳塞福涅乘员";
                case "NeedsPassengerFire":return "需要对应的火焰乘员";
                case "FleetBoatRequired":return "需要可用的船队船只";
                case "TrojanHorseWheelsMissing":return "缺少木马轮子";
                case "AlreadyCarryingCargo":return "已携带货物，需先处理现有货物";
                case "TrojanHorseCrownMissing":return "缺少木马所需王冠";
                case "SerpentAtGate":return "巨蛇在门口，当前交互受阻";
                case "GoldNuggetMissing":return "缺少所需金块";
                case "Base":return "对象暂时不可用；检查当前任务及施工状态";
                default:return "尚未解释的条件："+reason;
            }
        }
        public static string Currency(string raw)
        {switch(raw){case "Coins":return "金币";case "Gems":return "宝石";case "Crown":return "王冠";case "Skulls":return "骷髅";case "Shades":return "幽魂";case "Merchandise":return "货物";case "Candle":return "蜡烛";case "Egg":return "蛋";default:return raw;}}
    }
    public static class TargetProximity
    {
        public const float Radius=1.5f;
        public static bool Contains(float playerX,float targetX)=>Math.Abs(playerX-targetX)<=Radius;
    }
    public sealed class TargetInfo
    {
        public string Raw="",Name="",Description="",Advice="",Category="",Currency="",Lock="",Next="",Details="";
        public int Cost; public float X; public bool Locked; public int Level=-1;
        public bool UnderConstruction;public float Health=-1,HealthMax=-1,Construction=-1;public int Stock=-1,StockMax=-1;
    }
    public sealed class PlayerInfo
    {
        public int Id,Coins,Gems,Capacity; public bool Local; public float X;
        public readonly Dictionary<int,int> Resources=new Dictionary<int,int>();
        public float ViewX,ViewY,ViewW=1,ViewH=1;
        public string Mount="",Ability="",MountState="";
        public int BagTotal,BagCapacity;public float Stamina,Fed; public TargetInfo Target;
    }
    public sealed class MapPoint
    {public string Key="unknown",Name,Category;public float X;public bool Locked;public int Cost,CampPeople=-1;public readonly Dictionary<int,TargetInfo> PlayerStates=new Dictionary<int,TargetInfo>();}
    public sealed class EnemyPoint
    {public int Id;public string Kind;public float X;}
    public sealed class EnemyGroup
    {public int Id,Count;public string Kind;public float X,Min,Max;}
    public static class EnemyClusters
    {
        public static List<EnemyGroup> Group(IEnumerable<EnemyPoint> enemies,float span)
        {
            var result=new List<EnemyGroup>();
            foreach(var kind in enemies.GroupBy(e=>e.Kind))
            {
                EnemyGroup group=null;
                foreach(var enemy in kind.OrderBy(e=>e.X))
                {
                    if(group==null||enemy.X-group.Min>span){group=new EnemyGroup{Id=enemy.Id,Kind=enemy.Kind,Min=enemy.X,Max=enemy.X};result.Add(group);}
                    group.X=(group.X*group.Count+enemy.X)/(group.Count+1);group.Count++;group.Max=enemy.X;group.Id=Math.Min(group.Id,enemy.Id);
                }
            }
            return result.OrderBy(g=>g.X).ToList();
        }
    }
    public static class PopulationRole
    {
        public static string Supplemental(bool ninja,bool pikeman,bool fisher,bool stable,bool peasant)
        {if(ninja)return "ninja";if(pikeman)return "";if(fisher)return "fisher";if(stable)return "stable";return peasant?"peasant":"";}
    }
    public static class PopulationPresentation
    {
        public static List<System.Tuple<string,string>> Rows(Snapshot s)
        {
            var rows=new List<System.Tuple<string,string>>();
            void Add(string icon,string name,int count,bool always=false){if(always||count>0)rows.Add(System.Tuple.Create(icon,name+" "+count));}
            Add("hammer","工匠",s.Workers,true);Add("bow","弓手",s.Archers,true);Add("shield","骑士",s.Knights,true);
            Add("wheat","农民",s.Farmers,true);Add("person","游民",s.Beggars,true);
            Add("spear","长枪兵",s.Pikemen);Add("axe","狂战士",s.Berserkers);Add("person","待业平民",s.Peasants);
            Add("ninja","忍者",s.Ninjas);Add("fish","渔夫",s.Fishers);Add("horse","马厩管理员",s.StableKeepers);Add("person","隐士",s.Hermits);
            return rows;
        }
    }
    public sealed class Snapshot
    {
        public bool Playing,Online; public int Island,Day,Workers,Archers,Knights,Farmers,Beggars;
        public int Pikemen,Berserkers,Peasants,Ninjas,Fishers,StableKeepers,Hermits;
        public bool Stone,Iron;public string Season="",Phase="",State="";
        public float Left=-1,Right=1;
        public int Camps;public bool EnemiesAvailable;
        public string CampaignKey="";
        public readonly List<int> ResourceTypes=new List<int>();
        public readonly List<EnemyPoint> Enemies=new List<EnemyPoint>();
        public readonly List<PlayerInfo> Players=new List<PlayerInfo>();
        public readonly List<MapPoint> Points=new List<MapPoint>();
        public readonly Dictionary<string,int> Counts=new Dictionary<string,int>();
        public readonly List<System.Tuple<float,float>> ExploredRanges=new List<System.Tuple<float,float>>();
    }
}
