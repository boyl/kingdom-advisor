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
        public static Entry ResolveWall(string raw,bool hasHorn)
            =>hasHorn?Entries.First(e=>e.Key=="hornwall"):Resolve(raw);
        public static string WallConversion(string prefab)
        {
            var e=Resolve(prefab);
            return e.Key=="hornwall"?"特殊改造  "+e.Name+"："+e.Description+" 需要带上号角隐士。":"";
        }
        public static string Conversion(Entry target,string passengerTag)
        {
            string kind=target.Key switch{"hornwall"=>"Horn","ballista"=>"Ballista","baker"=>"Baker","knighttower"=>"Knight","stable"=>"Horse","firetower"=>"Fire",_=>null};
            string requirement=kind==null?"乘员条件："+(passengerTag??""):"需要带上"+ResolveHermit("",kind,false).Name+"。";
            return "特殊改造  "+target.Name+"："+target.Description+" "+requirement;
        }
        public static readonly Entry[] Entries = {
            new Entry("hermitfire","火焰隐士","将满足条件的弓箭塔改造成火焰塔。","改造后需要操作工匠。","特殊"),
            new Entry("hermitknight","军队招募隐士","将满足条件的弓箭塔改造成额外军队招募塔。","增加招募入口，不会直接增加人口或自动晋升。","特殊"),
            new Entry("hermitbaker","面包／酿造隐士","将满足条件的弓箭塔改造成吸引游民的面包或酿造设施。","需要购买消耗品；留意游民是否真正完成招募。","特殊"),
            new Entry("hermitballista","弩炮隐士","将满足条件的弓箭塔改造成弩炮塔。","改造后由工匠操作，不再是普通弓手驻塔。","特殊"),
            new Entry("hermithorn","号角隐士","将满足条件的城墙改造成号角城墙，用于召集军队。","召集会集中兵力，注意另一侧防线。","特殊"),
            new Entry("hermitstable","马厩隐士","将满足条件的农舍改造成马厩，用于管理坐骑。","改造会替换农田用途，先考虑粮食收入。","特殊"),
            new Entry("statuepike","重装步兵雕像","奥林匹斯主题提高重装步兵的攻击频率。","先解锁，再支付激活费用；当前费用与状态以实际对象为准。","特殊"),
            new Entry("hourglass","沙漏雕像","付费延长骷髅岛的倒计时，推迟火山喷发。","剩余天数与当前状态由实际对象读取。","特殊"),
            new Entry("hornwall","号角城墙","吹响号角召集军队到这一侧，集中防御。","注意另一侧防线，召集不会增加士兵数量。","防御"),
            new Entry("berserkertower","狂战士改造塔","北欧主题的特殊改造设施，与狂战士升级有关。","具体升级条件需结合当前对象；不能当作普通弓箭塔。","防御"),
            new Entry("firetower","火焰塔","由工匠操作，发射火焰投射物支援防线。","需要火焰隐士和操作工匠；不是弓手驻塔。","防御"),
            new Entry("knighttower","军队招募塔","隐士改造后的额外军队招募点，可购买盾牌招募基础队长。","招募后仍需弓手组成部队，晋升装备需另购。","军队"),
            new Entry("shieldshop","盾牌商店","北欧主题为已有人员提供盾牌，支持持盾防御与部队编组。","这与城堡招募侍从的军队旗不同；不会直接增加人口。","工具"),
            new Entry("sword","骑士升级装备","购买装备供已有侍从领取，晋升为骑士或主题对应的高级队长。","需要已有基础队长；库存表示装备件数，不是骑士人数。","工具"),
            new Entry("bomb","炸弹","由工匠推动并由部队护送，用于炸毁悬崖洞穴。","准备护送人员和金币；进入洞穴后的引爆与撤离以游戏交互提示为准。","军队"),
            new Entry("bombpurchase","炸弹出征旗","购买炸弹并组织部队护送，用于进攻悬崖洞穴。","出征前准备工匠、军队和金币，确认护送路线安全。","军队"),
            new Entry("wall","城墙","阻挡地面敌人，保护城内人员；飞行敌人可越过城墙。","升级需要工匠施工，夜间施工会削弱防守。","防御"),
            new Entry("tower","弓箭塔","供弓箭手驻守射击；容量、遮蔽和特殊改造随等级变化。","驻塔弓手会离开地面狩猎与部队岗位，建设前考虑位置。","防御"),
            new Entry("castle","城堡／营地中心","提升王国建设阶段，开放对应的工具、军队与设施。","先解锁建设科技，再支付升级；升级仍需要工匠施工。","建设"),
            new Entry("campfire","营地中心","王国的中心，也是早期建设入口。","先稳定招募、工具和两侧防线，再向外扩张。","建设"),
            new Entry("farmhouse","农舍","支持周边农田；升级为水车农舍后，农民可在附近留宿。","农田需要空间和农民；改为马厩后不能继续当农舍使用。","经济"),
            new Entry("farm","农田","供农民耕作并产生收入。","建设后还需要人员；数量不等于正在生产的数量。","经济"),
            new Entry("beggarcamp","游民营地","招募新成员的来源。","扩张或清理附近森林前，留意营地是否会受到影响。","人口"),
            new Entry("beggar","游民","获得招募金币后成为待业平民，再领取工具进入对应职业。","仅招募不会自动成为工匠、弓手或农民。","人口"),
            new Entry("citizenhouse","市民之家","在原游民营地遗址建造，提供付费招募待业平民的入口。","这里的居民需向房屋支付招募费用，不会捡地上的招募金币或追逐面包。","人口"),
            new Entry("bow","弓箭工具","供待业平民领取并成为弓箭手，参与狩猎、守城或部队。","工具库存与已装备人员分别统计；已有弓手也可能转入塔楼或部队。","工具"),
            new Entry("hammer","工匠工具","供待业平民领取并成为工匠，负责建造、维修和操作工程设施。","投石车、弩炮与火焰塔使用工匠，不是独立的人口职业。","工具"),
            new Entry("scythe","农民工具","供待业平民领取并成为农民，耕种可用农田或参与季节性采集。","需要农舍、空间和可用地块；买工具本身不会产生农田收入。","工具"),
            new Entry("pike","长枪工具","供待业平民领取并成为长枪兵，参与守城与捕鱼；主题变体可能不同。","武器会损耗；工具库存不等于已装备士兵人数。","工具"),
            new Entry("shield","军队招募旗","购买盾牌供待业平民领取，成为侍从等主题对应的基础队长。","队长会带领弓手；后续领取锻造装备可晋升。出征状态与招募状态不同。","工具"),
            new Entry("ninja","忍者设施","幕府主题出售忍者武器，供待业平民领取并成为忍者。","忍者会在城外森林伏击；森林位置影响可用伏击点。","工具"),
            new Entry("forge","锻造设施","常规主题出售升级装备，由已有侍从领取后晋升为骑士或主题对应的高级队长。","需要已有基础队长；支付购买装备不会直接增加人口。","工具"),
            new Entry("workshop","投石车工坊","购买投石车，由工匠建造、推至对应一侧防线并操作。","先准备工匠与安全路线；投石车不是一种新的职业。","工具"),
            new Entry("catapult","投石车","由工匠操作，向敌群抛射石弹支援防线。","守住操作位置；火焰桶是另行购买的弹药。","防御"),
            new Entry("ballista","弩炮塔","由工匠操作的大型弩炮，用弩箭支援防线。","需要弩炮隐士完成改造；不会招募新职业。","防御"),
            new Entry("baker","面包／酿造设施","制作食物或饮品吸引游民；游民吃下后自动成为待业平民。","可提前用金币招募赶来的游民以节省消耗品；不吸引市民之家的居民。","人口"),
            new Entry("lighthouse","灯塔","保护返航船只，减少在已建灯塔的码头再次触礁损坏。","灯塔属于当前岛屿；升级前检查对应建设科技。","航行"),
            new Entry("wharf","码头","船只与航行的交互位置。","出航前检查人员、资源和当前岛屿防线。","航行"),
            new Entry("boat","船只","用于岛屿间航行或当前主题的船队活动。","施工、登船与出航是不同阶段。","航行"),
            new Entry("shipyard","船坞","奥林匹斯主题建造用于航行与海上作战的船只。","需要施工与人员；不能直接套用常规战役炸弹船队的用途。","航行"),
            new Entry("teleporter","传送门","由已清除的小传送门遗址建造，供君主在本岛快速移动。","先确认出口位置；这与 Mod 的地图传送功能不同。","探索"),
            new Entry("portal","敌方传送门","敌人的出入口；攻破后可能引发反击。","小传送门、码头传送门与悬崖洞穴的处理方式不同。","威胁"),
            new Entry("cave","悬崖洞穴","常规战役的敌方主巢；炸弹队伍可进入并引爆清除。","需要准备炸弹、工匠和护送部队；此流程不适用于奥林匹斯任务战役。","威胁"),
            new Entry("mine","高阶建设科技设施","常规主题的铁矿用于解锁高阶建设科技；北欧以木材科技设施替代。","这是支付解锁，不要求君主携带铁矿石；科技解锁仍不等于建筑已升级。","科技"),
            new Entry("quarry","中阶建设科技设施","常规主题的石矿用于解锁中阶建设科技；北欧使用对应木材科技。","科技解锁后仍要支付各建筑升级费用并完成施工。","科技"),
            new Entry("statuearcher","弓箭手雕像","提高弓箭手的射击精准度，帮助狩猎与防守。","先解锁，再支付激活费用；当前费用与状态以实际对象为准。","特殊"),
            new Entry("statueworker","工匠雕像","强化城墙的耐久，提升防线承受攻击的能力。","先解锁，再支付激活费用；当前费用与状态以实际对象为准。","特殊"),
            new Entry("statuefarmer","农民雕像","增加农场可支持的农田地块；仍需足够空间和农民。","先解锁，再支付激活费用；当前费用与状态以实际对象为准。","特殊"),
            new Entry("statueknight","骑士雕像","赋予骑士突进攻击能力；不作用于未升级的侍从。","先解锁，再支付激活费用；当前费用与状态以实际对象为准。","特殊"),
            new Entry("statue","未细分雕像","该雕像的专属作用尚未核实；请查看游戏提示。","当前费用与状态以实际对象为准。","特殊"),
            new Entry("monument","纪念碑／特殊设施","特殊设施的效果由战役及具体对象决定，当前尚未核实专属作用。","不要仅凭外观判断科技或能力；查看当前任务提示。","特殊"),
            new Entry("hermit","隐士","帮助将满足条件的建筑改造成特殊设施，种类决定改造用途。","解锁后可搭乘君主坐骑；具体种类优先按原生身份识别。","特殊"),
            new Entry("cabin","隐士小屋","解锁对应种类的隐士；隐士可帮助改造特定建筑。","解锁隐士不等于完成建筑改造，还需合适建筑和后续费用。","特殊"),
            new Entry("chest","宝箱","提供资源或任务物品。","货币种类以当前对象为准。","探索"),
            new Entry("bank","银行／银行家","接收金币存入国库，在可用时段可向君主返还存款。","存款与钱袋是不同位置的金币；不要把银行数字当作随身可支付金额。","经济"),
            new Entry("merchant","商人","常规主题付费派遣商人，翌日返回后交付金币。","收入需要等待返回；特殊战役的交付规则可能不同。","经济"),
            new Entry("berry","浆果丛","冬季可付费指派农民采集，提供季节性金币收入。","需要农民；被敌人阻断或人员不足时不会立即获得收入。","经济"),
            new Entry("tree","树木","可以付费砍伐，影响森林和可建设区域。","砍树前检查附近游民营地与经济资源。","环境"),
            new Entry("steed","坐骑","提供移动能力及主题相关特殊能力。","观察耐力、饱食和疲劳状态。","坐骑"),
            new Entry("horse","马匹","提供移动能力。","奔跑消耗耐力；停留、步行和进食状态影响恢复。","坐骑"),
            new Entry("griffin","狮鹫","可用扑翼击退敌人；以进食恢复耐力，不依赖草地。","扑翼消耗耐力；查看当前能力与疲劳状态。","坐骑"),
            new Entry("bear","熊","奔跑时向前扑击，可攻击贪婪怪和野生动物；在森林中移动较快。","在草地进食恢复；连续攻击会使其疲劳，注意退路。","坐骑"),
            new Entry("stag","雄鹿","可吸引野鹿跟随，帮助把猎物引向弓箭手；在森林中奔跑较快。","在草地进食恢复；奥林匹斯中也用于引导任务牡鹿。","坐骑"),
            new Entry("warhorse","战马","发动能力可为附近臣民提供临时防护，帮助部队承受攻击。","在草地进食恢复；防护有持续时间和冷却。","坐骑"),
            new Entry("lizard","蜥蜴","向前喷出火焰，在地面留下火焰伤害区域。","靠日光休息恢复饱食；夜间和冬季应提前规划耐力。","坐骑"),
            new Entry("unicorn","独角兽","在草地进食后可产出金币，提供额外收入。","产币有冷却；冬季缺少草地会限制进食与收入。","坐骑"),
            new Entry("drafthorse","耐力马","拥有较长的奔跑耐力，没有主动攻击能力。","在草地进食恢复，适合长距离往返。","坐骑"),
            new Entry("fasthorse","速度马","马匹的速度变体，没有主动攻击能力。","速度与耐力取舍不同于起始马；在草地进食恢复。","坐骑"),
            new Entry("bursthorse","疾驰马","马匹的疾驰变体，没有主动攻击能力。","奔跑速度与耐力取舍不同；在草地进食恢复。","坐骑"),
            new Entry("undeadhorse","亡灵马","死寂之地的起始马，提供移动能力，没有主动攻击能力。","两位君主中的亡灵马仍需在草地进食；不能套用新大陆的无限耐力。","坐骑"),
            new Entry("wolf","巨狼","可发动连续扑击攻击贪婪怪；北欧变体还可为附近臣民提供防护。","在林外有月光时嚎叫恢复饱食；留意攻击次数和冷却。","坐骑"),
            new Entry("reindeer","驯鹿","北欧的驯鹿具有雄鹿式引鹿能力，适合森林探索与引导猎物。","在草地进食恢复；节日变体的雪橇交互只适用于对应活动。","坐骑"),
            new Entry("gamigin","加米金","死寂之地的恶魔坐骑，可向前冲撞，伤害并击退目标。","可在任意地形进食；冲撞不能保证击杀强敌。","坐骑"),
            new Entry("golem","魔像","站定后可召出树根屏障，阻挡并拖延贪婪怪。","在草地进食恢复；新屏障达到数量限制后会替换旧屏障。","坐骑"),
            new Entry("beetle","甲虫","向身后放置幼虫陷阱，可消灭经过的小型贪婪怪。","陷阱不会消灭巨型怪或飞行怪；在草地进食恢复。","坐骑"),
            new Entry("sleipnir","八足神马","北欧坐骑，可冲锋击退敌人，并在身后留下火焰轨迹。","在草地进食恢复；留意冲锋冷却及地面火焰。","坐骑"),
            new Entry("catcart","猫拉战车","北欧坐骑，可短时高速冲刺，并让附近的猫跟随。","可在任意地形进食；冲刺用于赶路，不是直接伤害攻击。","坐骑"),
            new Entry("kelpie","水马","暖季释放水波击退敌人并灭火；冬季改为召唤冰墙。","可在任意地形吸水恢复；冰墙有数量限制。","坐骑"),
            new Entry("goldenboar","黄金野猪","主动拱地可恢复耐力，有机会挖出金币，并强化附近农田。","恢复需要发动拱地能力；金币不是每次必出。","坐骑"),
            new Entry("daynight","昼夜马","北欧坐骑，随昼夜改变外观和速度，黎明与黄昏速度较高。","在草地进食恢复；外观可帮助判断森林中的时间。","坐骑"),
            new Entry("hippocampus","海马","奥林匹斯坐骑，可发动短时高速冲刺。","可在任意地形以水恢复；冲刺有冷却。","坐骑"),
            new Entry("cerberus","地狱三头犬","奥林匹斯坐骑，可召唤幽灵军队协助战斗。","在草地挖骨头恢复；幽灵部队是能力效果，不计为永久人口。","坐骑"),
            new Entry("spider","阿拉克涅","奥林匹斯蜘蛛坐骑，可放下蛛网减缓贪婪怪。","在森林中捕食昆虫恢复；草地不能代替森林进食。","坐骑"),
            new Entry("chariotday","太阳战车","奥林匹斯坐骑，白天可获得短时高速冲刺。","在草地进食恢复；同一解锁点选择日或月战车会锁定另一选项。","坐骑"),
            new Entry("chariotnight","月亮战车","奥林匹斯坐骑，夜间可获得短时高速冲刺。","在草地进食恢复；同一解锁点选择日或月战车会锁定另一选项。","坐骑"),
            new Entry("pegasus","飞马","奥林匹斯坐骑，可发动长距离跳跃跨越敌群。","在草地进食恢复；跳跃消耗耐力，落点仍需安全。","坐骑"),
            new Entry("donkey","驴","奥林匹斯坐骑，可放下面包招募附近游民，并在进食后产出金币。","在草地进食恢复；面包能力不是直接增加工具职业。","坐骑"),
            new Entry("argos","阿尔戈斯","奥林匹斯猎犬坐骑，可给予附近臣民速度增益，帮助人员撤退。","在森林中恢复饱食；留意增益的持续时间。","坐骑"),
            new Entry("chimera","奇美拉","奥林匹斯坐骑，可发动火焰冲锋攻击敌人。","在草地捕食恢复；留意连续冲锋次数与冷却。","坐骑"),
            new Entry("rainbowpony","彩虹小马","周年活动坐骑，可高速冲刺，并在进食后产出金币。","冲刺不会伤害敌人；产币有冷却。","坐骑"),
            new Entry("stable","马厩","管理已解锁的坐骑，可在马厩更换可用坐骑。","由马厩隐士改造农舍；原农田生产功能会被替换。","坐骑"),
            new Entry("trojan","木马设施","奥林匹斯战役的任务建造设施，需补齐轮子和王冠等任务部件。","查看实际缺少条件；外形像坐骑，但不是普通坐骑解锁点。","特殊"),
            new Entry("serpent","巨蛇／奥林匹斯终局","奥林匹斯任务中的巨蛇会制造贪婪怪威胁，并在终局阻挡通往奥林匹斯的道路。","终局需完成木马部件并集结人员；不能套用普通悬崖炸弹流程。","特殊"),
            new Entry("hephaestus","赫淮斯托斯任务","奥林匹斯任务：取回金锤，清除任务岛的敌方据点并重建神祇锻造设施，获得赫淮斯托斯之锤。","神锤可召出铁砧，为附近部队的武器赋予火焰；此任务设施不同于普通骑士装备锻造。","特殊"),
            new Entry("persephone","珀耳塞福涅任务","奥林匹斯雅典娜任务：解救珀耳塞福涅，带她到永冻之树，协助种出金苹果。","她是任务乘员，不是将普通农舍改造成马厩的隐士；完成金苹果任务可获得雅典娜之盾。","特殊"),
            new Entry("artemis","阿耳忒弥斯任务","奥林匹斯任务：建造捕获笼，骑雄鹿把金鹿引入笼中，获得阿耳忒弥斯之弓。","神弓向附近敌人发射追踪箭；与普通弓箭手工具不同。","特殊"),
            new Entry("thor","雷神谜题／神锤","北欧符文谜题，按岛上的线索调整符文柱，完成后可取得雷神之锤。","装备后可召唤雷击攻击附近敌人；君主能力与坐骑能力是不同操作。","特殊"),
            new Entry("hel","海拉献祭／幽灵部队","北欧献祭谜题，需要部队献出装备；完成后获得可召唤幽灵突击队的海拉战利品。","需有符合条件的侍从和持盾弓手；献出装备后人员需要重新装备。","特殊")
            ,new Entry("bell","召集铃","召集当前主题的相关人员或队伍。","查看实际费用，出航前确认随行人员。","航行")
            ,new Entry("barrel","火焰桶","供投石车使用的火焰弹药，用于点燃敌群。","需要投石车及操作工匠；桶的数量不是士兵人数。","防御")
            ,new Entry("gembank","宝石储存","管理当前战役中的宝石储存。","钱袋持有和存放数量应分别理解。","经济")
            ,new Entry("gemguard","宝石保管员","与宝石存取有关的交互入口。","确认当前交互需要的货币。","经济")
            ,new Entry("grave","君主墓地","涉及君主或主题解锁的特殊交互，当前尚未核实专属作用。","当前费用与状态以实际对象为准。","特殊")
            ,new Entry("border","王国边界","标记王国建设区域与防线边界。","能否建设由实际区域与锁定条件决定；边界并不是城墙。","建设")
        };
        public static string Clean(string raw) => (raw ?? "").Replace("(Clone)","").Replace("_undeveloped"," · 未建造").Replace("_Wood"," · 木制").Replace("_Stone"," · 石制").Replace("_Iron"," · 铁制");
        public static Entry Resolve(string raw)
        {
            var name=(raw??"").ToLowerInvariant().Replace("_","").Replace(" ","");
            if(name.StartsWith("bearcave")||name=="bear"||name=="bear(clone)")return Entries.First(e=>e.Key=="bear");
            if(name.StartsWith("stonemine")||name.StartsWith("stonequarry"))return Entries.First(e=>e.Key=="quarry");
            if(name.StartsWith("ironmine"))return Entries.First(e=>e.Key=="mine");
            if(name.StartsWith("towerknight"))return Entries.First(e=>e.Key=="knighttower");
            if(name.StartsWith("towerfire"))return Entries.First(e=>e.Key=="firetower");
            if(name.StartsWith("towerberserker"))return Entries.First(e=>e.Key=="berserkertower");
            if(name.StartsWith("wallhorn"))return Entries.First(e=>e.Key=="hornwall");
            if(name.StartsWith("shieldshop"))return Entries.First(e=>e.Key=="shieldshop");
            if(name.StartsWith("timestatue"))return Entries.First(e=>e.Key=="hourglass");
            if(name.StartsWith("statue")){var kind=Regex.Match(name,@"^statue(archer|worker|farmer|knight|pike)(\(clone\))?$");if(kind.Success)return Entries.First(e=>e.Key=="statue"+kind.Groups[1].Value);}
            if(name.StartsWith("bombbanner")||name.StartsWith("payablebombleft")||name.StartsWith("payablebombright")||name.StartsWith("payablebombpurchase"))return Entries.First(e=>e.Key=="bombpurchase");
            if(name=="bomb"||name=="bomb(clone)")return Entries.First(e=>e.Key=="bomb");
            if(name.StartsWith("bombableportal"))return Entries.First(e=>e.Key=="cave");
            if(Regex.IsMatch(name,@"^wreck(\([0-9]+\))?(\(clone\))?$"))return Entries.First(e=>e.Key=="boat");
            if(name=="keep"||name=="keep(clone)"||Regex.IsMatch(name,@"^keep[0-9]+(\(clone\))?$"))return Entries.First(e=>e.Key=="castle");
            if(name.Contains("hermithouse"))return Entries.First(e=>e.Key=="cabin");
            if(name.Contains("hermit"))return ResolveHermit(raw,name.Contains("ballista")?"Ballista":name.Contains("baker")?"Baker":name.Contains("knight")?"Knight":name.Contains("horn")?"Horn":name.Contains("horse")?"Horse":name.Contains("fire")?"Fire":"",false);
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
            foreach(var e in Entries) if(e.Key!="hel"&&e.Key!="bomb"&&name.Contains(e.Key))return e;
            if(name.StartsWith("hel"))return Entries.First(e=>e.Key=="hel");
            return new Entry("unknown",Clean(raw),"该对象尚未收录专属说明；费用和条件来自当前游戏。","请以运行时提示为准。","未分类");
        }
        public static Entry ResolveNative(string raw,string nativeType)
        {
            var named=Resolve(raw);
            string key=nativeType switch{
                "Bomb"=>"bomb","PayableBombPurchase" or "PayableBombLeft" or "PayableBombRight"=>"bombpurchase",
                "BoatSailPosition" or "PayableBoat"=>"boat","BoatSummoningBell"=>"bell",
                "Cabin"=>"cabin","CitizenHousePayable"=>"citizenhouse","Hermit"=>"hermit","Merchant"=>"merchant",
                "PayableBush"=>"berry","PayableForge"=>"forge","PayableGemChest"=>"chest","PayableGemGuard"=>"gemguard",
                "PayableShield"=>"shield","PayableTeleporter"=>"teleporter","PayableTree"=>"tree",
                "PayableWorkshop"=>"workshop","PayableWorkshopBarrel"=>"barrel",
                "Steed" or "SteedSpawn"=>"steed","Wharf"=>"wharf","Statue" or "TimedStatue"=>"statue","TimeStatue"=>"hourglass",
                "Castle"=>"castle","Wall"=>"wall","Tower"=>"tower","Farmhouse"=>"farmhouse","Ballista"=>"ballista","Baker"=>"baker",
                _=>null};
            if(key==null)return named;
            if(key=="tower"&&(named.Key=="baker"||named.Key=="ballista"||named.Key=="knighttower"||named.Key=="firetower"||named.Key=="berserkertower"))return named;
            if(key=="farmhouse"&&named.Key=="stable")return named;
            if(key=="wall"&&named.Key=="hornwall")return named;
            if(key=="statue"&&nativeType!="TimeStatue"&&named.Key.StartsWith("statue")&&named.Key!="statue")return named;
            if(key=="statue")return ResolveStatue(raw,nativeType,"");
            if(key=="steed"&&named.Category=="坐骑")return named;
            return Entries.First(e=>e.Key==key);
        }
        public static Entry ResolveStatue(string raw,string nativeType,string deity)
        {
            // 原生身份优先；未知枚举保留对象名称，不猜测增益。
            string key=deity switch{"Archer"=>"statuearcher","Worker"=>"statueworker","Farmer"=>"statuefarmer","Knight"=>"statueknight","Pike"=>"statuepike",_=>null};
            if(key!=null)return Entries.First(e=>e.Key==key);
            return new Entry("statue",Clean(raw),"该雕像的专属作用尚未核实；请查看游戏提示。","当前费用与状态以实际对象为准。","特殊");
        }
        public static Entry ResolveShop(string raw,string shopType,string itemType)
        {
            if(itemType=="DroppableArmor")return Entries.First(e=>e.Key=="sword");
            string key=shopType switch{
                "Bow"=>"bow","Hammer"=>"hammer","Scythe"=>"scythe",
                "Pike" or "PikeLeft" or "PikeRight"=>"pike",
                "Ninja" or "NinjaLeft" or "NinjaRight"=>"ninja",
                "ShieldShop" or "ShieldShopLeft" or "ShieldShopRight"=>"shieldshop",
                "Workshop" or "WorkshopLeft" or "WorkshopRight"=>"workshop","Forge"=>"forge",_=>null};
            return key!=null?Entries.First(e=>e.Key==key):Resolve(raw);
        }
        public static Entry ResolveHermit(string raw,string kind,bool cottage)
        {
            if(kind=="Persephone")return Entries.First(e=>e.Key=="persephone");
            string key=kind switch{"Horse"=>"hermitstable","Horn"=>"hermithorn","Ballista"=>"hermitballista","Baker"=>"hermitbaker","Knight"=>"hermitknight","Fire"=>"hermitfire",_=>null};
            if(key==null)return new Entry(cottage?"cabin":"hermit",Clean(raw),"该隐士种类的专属改造尚未核实。","当前费用与状态以实际对象为准。","特殊");
            var e=Entries.First(x=>x.Key==key);
            return cottage?new Entry("cabin",e.Name+"小屋","解锁"+e.Name+"；"+e.Description,e.Advice,e.Category):e;
        }
        public static Entry ResolveSteed(string raw,string kind)
        {
            string key=kind switch{
                "P1Griffin" or "P2Griffin"=>"griffin",
                "P1Default" or "P2Default"=>"horse",
                "Bear"=>"bear","Lizard"=>"lizard","Stag" or "P2Stag"=>"stag",
                "P1Warhorse" or "P2Warhorse"=>"warhorse","Unicorn"=>"unicorn",
                "HorseStamina"=>"drafthorse","HorseBurst"=>"bursthorse","HorseFast"=>"fasthorse",
                "Spookyhorse"=>"undeadhorse","P1Wolf" or "P2Wolf"=>"wolf",
                "Reindeer" or "Reindeer_Norselands" or "P2Reindeer_Norselands"=>"reindeer",
                "Trap"=>"beetle","Barrier"=>"golem","Bloodstained"=>"gamigin",
                "Sleipnir"=>"sleipnir","CatCart"=>"catcart","Kelpie" or "P2Kelpie"=>"kelpie",
                "Gullinbursti"=>"goldenboar","DayNight"=>"daynight","Hippocampus"=>"hippocampus",
                "Cerberus"=>"cerberus","Spider"=>"spider","TheChariotDay"=>"chariotday","TheChariotNight"=>"chariotnight",
                "Pegasus"=>"pegasus","Donkey"=>"donkey","MolossianHound"=>"argos","Chimera"=>"chimera","RainbowPony"=>"rainbowpony",_=>null};
            if(key!=null)return Entries.First(e=>e.Key==key);
            return new Entry("steed",Clean(raw),"该坐骑的专属能力尚未核实；实时耐力与冷却见状态面板。","观察耐力、饱食和疲劳状态。","坐骑");
        }
        public static Entry ResolveSteedSpawn(string raw,IEnumerable<string> kinds)
        {
            var entries=(kinds??Array.Empty<string>()).Select(k=>ResolveSteed(raw,k)).GroupBy(e=>e.Key).Select(g=>g.First()).ToArray();
            if(entries.Length==1&&entries[0].Key!="steed")
            {
                var e=entries[0];return new Entry(e.Key,e.Name+"解锁点","解锁"+e.Name+"；"+e.Description,"解锁与换乘费用以当前对象为准。"+e.Advice,e.Category);
            }
            if(entries.Length>1&&entries.All(e=>e.Key!="steed"))return new Entry("steed",Clean(raw),"可解锁坐骑："+string.Join(" / ",entries.Select(e=>e.Name))+"。"+string.Join(" ",entries.Select(e=>e.Name+"："+e.Description)),"解锁与换乘费用以当前对象为准。","坐骑");
            return ResolveSteed(raw,"");
        }
        public static string Lock(string reason)
        {
            switch(reason){
                case "Invalid":return "当前交互不可用；原因未提供";
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
    public static class LeaderRole
    {
        // NeedsArmor 是游戏寻找晋升装备的实际资格；rank 是部队槽位，不是职业等级。
        public static string Classify(bool needsArmor)=>needsArmor?"squire":"knight";
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
            Add("hammer","工匠",s.Workers,true);Add("bow","弓手",s.Archers,true);Add("shield","侍从",s.Squires,true);Add("shield","骑士",s.Knights,true);Add("shield","队长（未细分）",s.UnclassifiedLeaders);
            Add("wheat","农民",s.Farmers,true);Add("person","游民",s.Beggars,true);
            Add("spear","长枪兵",s.Pikemen);Add("axe","狂战士",s.Berserkers);Add("person","待业平民",s.Peasants);
            Add("ninja","忍者",s.Ninjas);Add("fish","渔夫",s.Fishers);Add("horse","马厩管理员",s.StableKeepers);Add("person","隐士",s.Hermits);
            return rows;
        }
    }
    public sealed class Snapshot
    {
        public bool Playing,Online; public int Island,Day,Workers,Archers,Knights,Farmers,Beggars;
        public int Squires,UnclassifiedLeaders;public int Pikemen,Berserkers,Peasants,Ninjas,Fishers,StableKeepers,Hermits;
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
