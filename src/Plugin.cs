using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime;

namespace KingdomAdvisor
{
    [BepInPlugin("local.kingdom.advisor","王国顾问 Kingdom Advisor",Version)]
    public sealed class Plugin : BasePlugin
    {
        public const string Version="0.6.7-preview.1";
        internal static Plugin Instance;
        internal Settings Settings;
        public override void Load()
        {
            Instance=this;Settings=new Settings(Config);
            AddComponent<AdvisorBehaviour>();
            new Harmony("local.kingdom.advisor").PatchAll(typeof(Plugin).Assembly);
            Log.LogInfo("Kingdom Advisor "+Version+" loaded; automatic in-game overlay; gameplay rule writes=0.");
        }
    }
    internal sealed class Settings
    {
        public ConfigEntry<bool> Enabled,Map,Status,Details,Alerts,FullMap,Trees,PreviewCapture,MapTransparent,AdvisorTransparent,PlayerTransparent,AlertTransparent,InterfaceTransparent,MapAtTop,ShowKingdomAdvisor,ShowPlayerPanel;
        public ConfigEntry<int> FontSize,UiLanguage;
        public ConfigEntry<string> Layout;
        public ConfigEntry<bool> Teleport,InfiniteBag,InfiniteStamina,Icons,Journal,DefenseAlerts,StaffAlerts,MountInfo,MapBuildings,MapCamps,MapEnemies,MapMounts,MapSpecial;
        public ConfigEntry<float> GameSpeed,PadSpeed;public ConfigEntry<bool> PadCursor;public ConfigEntry<int> PadOpenButton;
        public ConfigEntry<int> ResourceAmount;
        public Settings(ConfigFile config)
        {
            UiLanguage=config.Bind("界面","语言",0,new ConfigDescription("0 自动跟随游戏语言；中文使用简体中文，其他语言回退英文。1 简体中文，2 English。",new AcceptableValueList<int>(0,1,2)));
            Enabled=config.Bind("界面","启用",true,"游戏启动后自动显示。F4 切换。");
            Map=config.Bind("界面","地图",true,"默认顶部透明小地图；名称和数量常驻，状态聚焦显示，敌人图标下显示数量。");
            MapTransparent=config.Bind("地图","透明背景",true,"开启使用透明背景，关闭显示面板背景。");
            AdvisorTransparent=config.Bind("界面样式","顾问透明",true,"透明文字或面板背景。");
            PlayerTransparent=config.Bind("界面样式","附近详情透明",true,"透明文字或面板背景。");
            AlertTransparent=config.Bind("界面样式","提醒透明",true,"透明文字或面板背景。");
            InterfaceTransparent=config.Bind("界面样式","主界面透明",false,"图鉴、设置与资源等主界面使用透明背景。");
            MapAtTop=config.Bind("地图","默认置顶",true,"开启默认放在顶部，关闭默认放在底部；F6 切换时仅复位地图的拖动位置。");
            ShowKingdomAdvisor=config.Bind("界面","显示王国顾问",true,"显示左下王国顾问整张概况卡片。");
            ShowPlayerPanel=config.Bind("界面","显示Player详情",true,"显示顾问旁的附近建筑／交互物详情，可拖动并记忆位置。");
            Status=config.Bind("界面","王国概况",true,"显示人口、科技与时间。");
            Details=config.Bind("界面","对象详情",true,"显示升级目标与使用建议。");
            Alerts=config.Bind("界面","情境提醒",true,"仅在风险条件成立时显示提醒。");
            FullMap=config.Bind("地图","全情报",true,"显示当前已加载岛屿的所有兴趣点；不会伪造其他岛屿信息。");
            Trees=config.Bind("地图","显示树木",false,"普通树木默认隐藏，避免地图拥挤。");
            FontSize=config.Bind("界面","字号",18,"字号范围 14–28。");
            Layout=config.Bind("布局","卡片位置","","按玩家分别保存拖动后的相对坐标；可在设置中复位。");
            PreviewCapture=config.Bind("Diagnostics","PreviewCapture",false,"开发验收：首次进入游戏后短暂展示设置和图鉴并截图，随后关闭。不写入存档。");
            Teleport=config.Bind("操作便利","地图传送",false,"普通地图短按查看，长按 0.8 秒传送。单机与联机均可使用。");
            InfiniteBag=config.Bind("资源辅助","无限钱袋容量",false,"解除容量限制，并使用数字钱袋代替会溢出的物理钱袋。不自动增加金币。关闭后恢复原生钱袋、原容量及拾取溢出行为。单机与联机均可使用。");
            InfiniteStamina=config.Bind("操作便利","无限耐力",false,"使用原生无限耐力标志，关闭恢复原值。单机与联机均可使用。");
            GameSpeed=config.Bind("操作便利","游戏速度",1f,new ConfigDescription("0 暂停、1 正常、2／4 加速；单机与联机均可使用。",new AcceptableValueList<float>(0,1,2,4)));
            PadCursor=config.Bind("手柄","常驻光标",true,"右摇杆移动常驻光标；左摇杆控制角色。");
            PadSpeed=config.Bind("手柄","光标速度",700f,new ConfigDescription("光标每秒移动像素，按屏幕高度缩放。",new AcceptableValueRange<float>(200,1800)));
            PadOpenButton=config.Bind("手柄","呼出按键",0,new ConfigDescription("标准手柄模板：0 左摇杆按压、1 右摇杆按压、2 Back／Select。短按面板，长按0.8秒切换HUD显隐。",new AcceptableValueList<int>(0,1,2)));
            ResourceAmount=config.Bind("资源辅助","添加数量",10,new ConfigDescription("单次手动添加数量。",new AcceptableValueRange<int>(1,10000)));
            Icons=config.Bind("图鉴","显示图标",true,"图鉴列表和详情显示统一像素图标。");
            Journal=config.Bind("图鉴","岛屿记录",true,"记录当前存档各岛已见情报；离岛后明确标记为上次记录。");
            DefenseAlerts=config.Bind("提醒","防线风险",true,"最外城墙受损、失守或敌群接近时提醒。");
            StaffAlerts=config.Bind("提醒","人员缺口",true,"显示施工缺工匠及招募工具缺口。");
            MountInfo=config.Bind("提醒","坐骑状态",true,"显示耐力、饱食与能力冷却状态。");
            MapBuildings=config.Bind("地图筛选","建筑",true,"显示建筑及工具。");
            MapCamps=config.Bind("地图筛选","营地与人口",true,"显示人口地点。");
            MapEnemies=config.Bind("地图筛选","敌人",true,"显示敌群图标与数量。");
            MapMounts=config.Bind("地图筛选","坐骑",true,"显示坐骑地点。");
            MapSpecial=config.Bind("地图筛选","特殊地点",true,"显示科技、探索、航行和其他特殊地点。");
        }
    }
    public sealed class AdvisorBehaviour : MonoBehaviour
    {
        public AdvisorBehaviour(IntPtr pointer):base(pointer){}
        internal static AdvisorBehaviour Active;
        internal AdvisorController Controller;
        internal GameActions Actions;
        internal IslandJournal Journal;
        internal bool InputAvailable=>!faulted;
        private GameReader reader;
        private AdvisorView view;
        private float nextFast,nextSlow,nextEvidence,nextJournal;
        private int failures;
        private bool faulted;
        private string lastError="";
        private readonly Stopwatch stopwatch=new Stopwatch();
        private double maxReadMs;
        private float captureAt=-1;
        private bool captured;
        private bool captureUnavailable;
        private int previewStage,previewLanguage=-1,previewFont=-1;
        private float previewAt=-1;
        public void Awake()
        {
            Active=this;reader=new GameReader();Controller=new AdvisorController(Plugin.Instance.Settings);
            Actions=new GameActions(Plugin.Instance.Settings);Journal=new IslandJournal();Controller.Actions=Actions;Controller.Journal=Journal;
            view=new AdvisorView(Plugin.Instance.Settings,Controller); nextEvidence=0;
        }
        public void Update()
        {
            if(faulted)return;
            try
            {
                var language=global::Language.current;
                Localization.English=Localization.IsEnglish(previewLanguage<0?Plugin.Instance.Settings.UiLanguage.Value:previewLanguage,language?language.languageCode:null,Application.systemLanguage.ToString());
                var now=Time.unscaledTime;
                if(now>=nextSlow){stopwatch.Restart();reader.ReadWorld();stopwatch.Stop();maxReadMs=Math.Max(maxReadMs,stopwatch.Elapsed.TotalMilliseconds);nextSlow=now+1;nextFast=now;}
                if(now>=nextFast){reader.ReadPlayers();reader.ReadEnemies();nextFast=now+.1f;}
                Controller.Update(reader.State);
                try{Actions.Apply(reader.State);}catch(Exception ex){Plugin.Instance.Log.LogError("Assist update failed: "+ex);Actions.Notify("操作辅助出错，已关闭");Plugin.Instance.Settings.InfiniteBag.Value=false;Plugin.Instance.Settings.InfiniteStamina.Value=false;Plugin.Instance.Settings.GameSpeed.Value=1;Actions.Restore();}
                if(Plugin.Instance.Settings.Journal.Value&&now>=nextJournal){Journal.Observe(reader.State);nextJournal=now+1;}
                if(now>=nextEvidence){WriteEvidence();nextEvidence=now+10;}
                if(reader.State.Playing&&!captured&&reader.State.Players.Count>0)
                {
                    if(captureAt<0)captureAt=now+4;
                    if(now>=captureAt){Capture("first-gameplay.png");captured=true;}
                }
                if(Input.GetKeyDown(KeyCode.F9))Capture("manual-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".png");
                if(captured&&Plugin.Instance.Settings.PreviewCapture.Value&&reader.State.Playing)
                {
                    if(previewAt<0)previewAt=now+2;
                    if(now>=previewAt)
                    {
                        if(previewFont<0)previewFont=Plugin.Instance.Settings.FontSize.Value;
                        int variant=previewStage/32,scenario=(previewStage%32)/2;
                        if(previewStage>=232){previewLanguage=-1;Plugin.Instance.Settings.FontSize.Value=previewFont;Controller.Close();Controller.Tab=0;Controller.Section=0;Plugin.Instance.Settings.PreviewCapture.Value=false;}
                        else if(previewStage>=128)
                        {
                            int auditVariant=(previewStage-128)/26,page=((previewStage-128)%26)/2;
                            previewLanguage=auditVariant<2?2:1;Plugin.Instance.Settings.FontSize.Value=auditVariant%2==0?18:28;
                            if(previewStage%2==0){Controller.Close();Controller.Toggle(0,reader.State);Controller.Category="全部";Controller.Selected=page*Math.Max(1,Controller.VisibleRows);Controller.DetailPage=0;}
                            else if(page*Math.Max(1,Controller.VisibleRows)<Catalog.Entries.Length)Capture((auditVariant<2?"english":"chinese")+"-font"+(auditVariant%2==0?18:28)+"-catalog-page"+(page+1).ToString("D2")+".png");
                        }
                        else if(previewStage%2==0){
                            previewLanguage=variant<2?2:1;Plugin.Instance.Settings.FontSize.Value=variant%2==0?18:28;
                            Controller.Close();Controller.SettingsDropdown=false;Controller.ResourceDropdown=false;Controller.Focus=0;Controller.Selected=0;Controller.DetailPage=0;
                            if(scenario<8){Controller.Toggle(2,reader.State);Controller.Section=scenario<6?scenario:scenario==6?1:2;if(scenario==6)Controller.Focus=10;if(scenario==7){Controller.Focus=4;Controller.Choice=7;Controller.SettingsDropdown=true;}}
                            else if(scenario<10){Controller.Toggle(0,reader.State);if(scenario==9)Controller.Selected=12;}
                            else if(scenario==10)
                            {
                                Controller.Toggle(0,reader.State);
                                var point=reader.State.Points.FirstOrDefault(p=>LocationPresentation.Entry(p)!=null&&Controller.PointVisible(p));
                                if(point!=null){Controller.ShowCatalogLocations(LocationPresentation.Entry(point));Plugin.Instance.Log.LogInfo("Preview catalog-to-locations: tab="+Controller.Tab+" key="+Controller.PointCatalogKey);}
                                else Plugin.Instance.Log.LogInfo("Preview catalog-to-locations: skipped, no visible known location");
                            }
                            else if(scenario==11)
                            {
                                Controller.Toggle(1,reader.State);
                                var player=reader.State.Players.FirstOrDefault(p=>p.Id==Controller.Owner);
                                var points=player==null?Array.Empty<MapPoint>():Controller.FilteredPoints(reader.State,player);
                                if(points.Length>0){var point=points[0];Controller.Inspect(point,Controller.Owner);var mapped=Controller.FilteredPoints(reader.State,player);if(LocationPresentation.Find(mapped,point)!=Controller.Selected)throw new InvalidOperationException("Preview map link selected a different location");Plugin.Instance.Log.LogInfo("Preview locations-to-map: matched="+LocationPresentation.Identity(point));}
                            }
                            else if(scenario==12)Controller.Toggle(4,reader.State);
                            else if(scenario<15){Controller.Toggle(5,reader.State);if(scenario==14){Controller.Focus=5;Controller.Choice=2;Controller.ResourceDropdown=true;}}
                        }
                        else Capture((variant<2?"english":"chinese")+"-font"+(variant%2==0?18:28)+"-"+new[]{"settings-map","settings-advisor","settings-catalog","settings-resource","settings-time","settings-pad","settings-advisor-last","settings-font-dropdown","catalog-first","catalog-later","locations","map","journal","resources","resources-dropdown","gameplay"}[scenario]+".png");
                        previewStage++;previewAt=previewStage>232?float.PositiveInfinity:now+1;
                    }
                }
                failures=0;
            }
            catch(Exception ex)
            {
                Plugin.Instance.Log.LogError("Read failure: "+ex);
                if(++failures>=3){faulted=true;lastError=ex.ToString();Controller.Close();WriteEvidence();Plugin.Instance.Log.LogError("Overlay stopped after 3 consecutive failures. Gameplay left unchanged.");}
            }
        }
        public void OnGUI()
        {
            if(faulted){GUI.Label(new Rect(20,20,650,30),"Kingdom Advisor stopped: see BepInEx/LogOutput.log");return;}
            try{if(view!=null)view.Draw(reader.State);}
            catch(Exception ex){faulted=true;lastError=ex.ToString();Controller?.Close();Plugin.Instance.Log.LogError("Rendering stopped: "+ex);WriteEvidence();}
        }
        public void OnDestroy(){Controller?.Close();Controller?.RestorePadMappings();Actions?.Restore();Journal?.Flush();if(Active==this)Active=null;}
        private void Capture(string filename)
        {
            if(captureUnavailable)return;
            Texture2D texture=null;
            try
            {
                var folder=Path.Combine(Paths.ConfigPath,"KingdomAdvisor");Directory.CreateDirectory(folder);
                texture=ScreenCapture.CaptureScreenshotAsTexture();
                if(!texture)throw new InvalidOperationException("Screenshot texture unavailable");
                var native=ImageConversion.EncodeToPNG(texture);
                var bytes=new byte[native.Length];for(int i=0;i<bytes.Length;i++)bytes[i]=native[i];
                File.WriteAllBytes(Path.Combine(folder,filename),bytes);
                Plugin.Instance.Log.LogInfo("Screenshot saved: "+filename);
            }
            catch(Exception ex)
            {
                captureUnavailable=true;
                Plugin.Instance.Log.LogWarning("Diagnostic screenshot channel disabled; HUD remains active: "+ex);
            }
            finally {if(texture)UnityEngine.Object.Destroy(texture);}
        }
        private void WriteEvidence()
        {
            var s=reader.State;
            var lines=new List<string>{"version="+Plugin.Version,"utc="+DateTime.UtcNow.ToString("O"),"gameState="+s.State,"overlayOpen="+(Controller?.Open??false),"mapFocus="+(Controller?.MapFocus??false),"holdActive="+(Controller?.Hold.Active??false),"releaseGuardCount="+(Controller?.ReleaseGuardCount??0),"playing="+s.Playing,"online="+s.Online,"island="+s.Island,"day="+s.Day,"points="+s.Points.Count,"worldReadMaxMs="+maxReadMs.ToString("F3"),"font="+view.FontEvidence,"faulted="+faulted,"disabledMovementMaps="+(Controller?.DisabledMovementMaps??0),"movementLeakFrames="+(Controller?.MovementLeakFrames??0),"languagePreference="+Plugin.Instance.Settings.UiLanguage.Value,"languageCode="+(global::Language.current?global::Language.current.languageCode:""),"displayLanguage="+(Localization.English?"en":"zh-CN"),"configuredSpeed="+Plugin.Instance.Settings.GameSpeed.Value,"actualTimeScale="+Time.timeScale,"gameplayRuleWrites="+(Actions?.Writes??0),"teleports="+(Actions?.Teleports??0),"resourceAdds="+(Actions?.ResourceAdds??0),"error="+lastError};
            foreach(var p in s.Players)lines.Add("player="+p.Id+" local="+p.Local+" coins="+p.Coins+" target="+(p.Target?.Raw??"none")+" targetName="+(p.Target?.Name??"none")+" description="+(p.Target?.Description??"")+" rect="+p.ViewX+","+p.ViewY+","+p.ViewW+","+p.ViewH+(Actions?.WalletEvidence(p.Id)??""));
            foreach(var row in PopulationPresentation.Rows(s))lines.Add("population="+row.Item2);
            lines.Add("populationTotal="+(s.Workers+s.Archers+s.Squires+s.Knights+s.UnclassifiedLeaders+s.Farmers+s.Beggars+s.Pikemen+s.Berserkers+s.Peasants+s.Ninjas+s.Fishers+s.StableKeepers));
            lines.Add("enemiesAvailable="+s.EnemiesAvailable+" enemies="+s.Enemies.Count+" camps="+s.Camps);
            foreach(var enemy in s.Enemies.Take(12))lines.Add("enemy="+enemy.Id+" kind="+enemy.Kind+" x="+enemy.X.ToString("F2"));
            Directory.CreateDirectory(Path.Combine(Paths.ConfigPath,"KingdomAdvisor"));
            File.WriteAllLines(Path.Combine(Paths.ConfigPath,"KingdomAdvisor","runtime.txt"),lines);
            if(s.Playing)File.WriteAllLines(Path.Combine(Paths.ConfigPath,"KingdomAdvisor","object-census.tsv"),reader.Census());
        }
    }
    // 只在本 Mod 的详情面板拥有该玩家焦点时阻断其玩法输入。
    [HarmonyPatch(typeof(Player),"IControllable_ReceiveInput")]
    internal static class OverlayInputPatch
    {


        private static bool Prefix(Player __instance,Rewired.Player __0)
        {
            var active=AdvisorBehaviour.Active;
            var controller=active?.Controller;
            if(controller==null||!active.InputAvailable)return true;
            controller.RecordInput(__instance.playerId,__0);
            bool allowed=!controller.Blocks(__instance.playerId)&&!controller.ChordHeld(__instance.playerId);

            return allowed;
        }
    }
    internal sealed class GameReader
    {
        public Snapshot State=new Snapshot();
        private int lastIsland=-1,lastSeed=int.MinValue;
        private Component[] detailBuildings=Array.Empty<Component>();private float nextBuildingScan;
        internal readonly Dictionary<int,System.Tuple<float,float>> Explored=new Dictionary<int,System.Tuple<float,float>>();
                private Managers GetManagers()=>Managers._Inst;
        private static T NativeParent<T>(Component obj) where T:Component
        {var c=obj.gameObject.GetComponentInParent(Il2CppType.Of<T>());return c?c.TryCast<T>():null;}
        private static T NativeChild<T>(GameObject obj) where T:Component
        {var c=obj.GetComponentInChildren(Il2CppType.Of<T>());return c?c.TryCast<T>():null;}
        private static IEnumerable<T> NativeObjects<T>() where T:Component
        {foreach(var c in UnityEngine.Object.FindObjectsOfType(Il2CppType.Of<T>())){var typed=c.TryCast<T>();if(typed)yield return typed;}}
        private static bool ActiveComponent<T>(Component obj) where T:Behaviour {var c=NativeComponent<T>(obj);return c&&c.isActiveAndEnabled;}
        private static T NativeComponent<T>(Component obj) where T:Component=>NativeComponent<T>(obj.gameObject);
        private static Entry ResolvePayable(Payable payable)
        {
            var horn=payable.TryCast<PayableHorn>();if(horn)return Catalog.ResolveWall(payable.name,true);
            var wall=NativeComponent<Wall>(payable.gameObject);if(wall&&wall.isHornUpgrade)return Catalog.ResolveWall(payable.name,true);
            var statue=payable.TryCast<Statue>();
            if(statue)return Catalog.ResolveStatue(payable.name,payable.GetIl2CppType().Name,statue.deity.ToString());
            var cabin=payable.TryCast<Cabin>();if(cabin)return Catalog.ResolveHermit(cabin.name,cabin.hermitType.ToString(),true);
            var hermit=payable.TryCast<Hermit>();if(hermit)return Catalog.ResolveHermit(hermit.name,hermit.Type.ToString(),false);
            var steed=payable.TryCast<Steed>();if(steed)return Catalog.ResolveSteed(steed.name,steed.steedType.ToString());
            var spawn=payable.TryCast<SteedSpawn>();
            if(spawn&&spawn.steeds!=null)return Catalog.ResolveSteedSpawn(spawn.name,spawn.steeds.Where(s=>s).Select(s=>s.steedType.ToString()));
            var shop=payable.TryCast<PayableShop>();
            if(shop){var sided=payable.TryCast<PayableSidedShop>();return Catalog.ResolveShop(shop.name,sided?sided.SidedShopType.ToString():"",shop.itemPrefab?shop.itemPrefab.GetIl2CppType().Name:"");}
            return Catalog.ResolveNative(payable.name,payable.GetIl2CppType().Name);
        }
        private static Entry ResolveBuilding(GameObject obj)
        {
            var payable=NativeComponent<Payable>(obj);
            var entry=payable?ResolvePayable(payable):Catalog.Resolve(obj.name);
            var wall=NativeComponent<Wall>(obj);if(wall&&wall.isHornUpgrade)return Catalog.ResolveWall(obj.name,true);
            var farm=NativeComponent<Farmhouse>(obj);if(farm&&farm.isStable)return Catalog.Entries.First(e=>e.Key=="stable");
            if(NativeChild<Ballista>(obj))return Catalog.Entries.First(e=>e.Key=="ballista");
            if(NativeChild<Baker>(obj))return Catalog.Entries.First(e=>e.Key=="baker");
            if(NativeChild<FireTower>(obj))return Catalog.Entries.First(e=>e.Key=="firetower");
            return entry;
        }
        private static string PayableIdentity(Payable p)
        {
            var statue=p.TryCast<Statue>();if(statue)return "deity="+statue.deity+" status="+statue.deityStatus;
            var shop=p.TryCast<PayableShop>();if(shop)return "item="+(shop.itemPrefab?shop.itemPrefab.name:"none")+" itemType="+(shop.itemPrefab?shop.itemPrefab.GetIl2CppType().Name:"none");
            var cabin=p.TryCast<Cabin>();if(cabin)return "hermit="+cabin.hermitType;
            var hermit=p.TryCast<Hermit>();if(hermit)return "hermit="+hermit.Type;
            var steed=p.TryCast<Steed>();if(steed)return "steed="+steed.steedType;
            var spawn=p.TryCast<SteedSpawn>();if(spawn&&spawn.steeds!=null)return "steeds="+string.Join(",",spawn.steeds.Where(s=>s).Select(s=>s.steedType.ToString()));
            return "";
        }
        public IEnumerable<string> Census()
        {
            yield return "kind\traw\tnativeType\tcategory\tentry\tx\tidentity\tdescription";
            var all=Managers._Inst?.payables?.AllPayables;
            if(all!=null)foreach(var p in all)
            {
                if(!p||!p.gameObject.activeInHierarchy)continue;
                var e=ResolvePayable(p);
                yield return "payable\t"+p.name+"\t"+p.GetIl2CppType().Name+"\t"+e.Category+"\t"+e.Key+"\t"+p.transform.position.x+"\t"+PayableIdentity(p)+"\t"+e.Description;
            }
            var kingdom=Managers._Inst?.kingdom;
            if(kingdom&&kingdom._characters!=null)foreach(var c in kingdom._characters)
            {
                if(!c||!c.gameObject.activeInHierarchy)continue;
                var leader=NativeComponent<Knight>(c);var peasant=NativeComponent<Peasant>(c);var fisher=NativeComponent<Fisher>(c);var ninja=NativeComponent<Ninja>(c);var pike=NativeComponent<Pikeman>(c);
                yield return "character\t"+c.name+"\t"+c.GetIl2CppType().Name+"\tpeasant="+(peasant&&peasant.isActiveAndEnabled)+"\tfisher="+(fisher&&fisher.isActiveAndEnabled)+" ninja="+(ninja&&ninja.isActiveAndEnabled)+" pike="+(pike&&pike.isActiveAndEnabled)+" worker="+ActiveComponent<Worker>(c)+" archer="+ActiveComponent<Archer>(c)+" farmer="+ActiveComponent<Farmer>(c)+" knight="+ActiveComponent<Knight>(c)+" beggar="+ActiveComponent<Beggar>(c)+" berserker="+ActiveComponent<Berserker>(c)+" stable="+ActiveComponent<StableKeeper>(c)+"\t"+c.transform.position.x+"\t"+(leader&&leader.isActiveAndEnabled?"needsArmor="+leader.NeedsArmor+" rank="+leader.rank:"")+"\t";
            }
            foreach(var c in detailBuildings)if(c&&c.gameObject.activeInHierarchy){var e=Catalog.Resolve(c.name);yield return "building\t"+c.name+"\t"+c.GetIl2CppType().Name+"\t"+e.Category+"\t"+e.Key+"\t"+c.transform.position.x+"\t\t"+e.Description;}
        }
        private static T NativeComponent<T>(GameObject obj) where T:Component
        {var c=obj.GetComponent(Il2CppType.Of<T>());return c?c.TryCast<T>():null;}
        public void ReadWorld()
        {
            var m=GetManagers();
            if(!m||!m.game||!m.kingdom){State=new Snapshot();lastIsland=-1;return;}
            var game=m.game;
            bool playable=game.state==Game.State.Playing||game.state==Game.State.NetworkClientPlaying;
            var s=new Snapshot{Playing=playable,State=game.state.ToString(),Island=game.currentLand+1,Online=NetworkBigBoss.IsOnline};
            if(!playable){State=s;return;}
            if(Level.isGenerating){State=s;State.Playing=false;return;}
            if(lastIsland!=s.Island||(m.level&&lastSeed!=m.level.cachedCurrentLevelSeed))
            {Explored.Clear();lastIsland=s.Island;lastSeed=m.level?m.level.cachedCurrentLevelSeed:0;}
            var k=m.kingdom;
            var campaign=CampaignSaveData.current;if(campaign!=null)s.CampaignKey=campaign.realStartDateTime+"-"+campaign.biomeIndex;
            var currencies=CurrencyManager.AllCurrencyTypes;if(currencies!=null)for(int ci=0;ci<currencies.Length;ci++)if(ResourceRules.Supported((int)currencies[ci]))s.ResourceTypes.Add((int)currencies[ci]);
            s.Workers=k.Workers?.Count??0;s.Archers=k.ArcherCount;
            if(k._knights!=null)foreach(var leader in k._knights){if(!leader||!leader.isActiveAndEnabled)continue;if(LeaderRole.Classify(leader.NeedsArmor)=="squire")s.Squires++;else s.Knights++;}
            s.UnclassifiedLeaders=Math.Max(0,k.KnightCount-s.Squires-s.Knights);
            s.Farmers=k.Farmers?.Count??0;s.Beggars=k.Beggars?.Count??0;
            s.Pikemen=0;s.Berserkers=0;
            if(k._pikemen!=null)foreach(var unit in k._pikemen)if(unit&&unit.isActiveAndEnabled)s.Pikemen++;
            if(k.Berserkers!=null)foreach(var unit in k.Berserkers)if(unit&&unit.isActiveAndEnabled)s.Berserkers++;
            if(k.hermits!=null)foreach(var h in k.hermits)if(h&&h.gameObject.activeInHierarchy)s.Hermits++;
            // Ninja derives from Fisher: classify the specialised component first.
            if(k._characters!=null)foreach(var c in k._characters)
            {
                if(!c||!c.gameObject.activeInHierarchy)continue;
                var ninja=NativeComponent<Ninja>(c);var pike=NativeComponent<Pikeman>(c);var fisher=NativeComponent<Fisher>(c);var stable=NativeComponent<StableKeeper>(c);var peasant=NativeComponent<Peasant>(c);
                switch(PopulationRole.Supplemental(ninja&&ninja.isActiveAndEnabled,pike&&pike.isActiveAndEnabled,fisher&&fisher.isActiveAndEnabled,stable&&stable.isActiveAndEnabled,peasant&&peasant.isActiveAndEnabled))
                {case "ninja":s.Ninjas++;break;case "fisher":s.Fishers++;break;case "stable":s.StableKeepers++;break;case "peasant":s.Peasants++;break;}
            }
            s.Stone=k.StoneBuildingUnlocked;s.Iron=k.IronBuildingUnlocked;
            if(m.director){s.Day=m.director.TotalDaysInReign;s.Season=m.director.CurrentSeason.ToString();s.Phase=m.director.IsDaytime?"白昼":"夜晚";}
            if(m.world&&World.GroundCollider){var b=World.GroundCollider.bounds;s.Left=b.min.x;s.Right=b.max.x;}
            else if(m.level&&m.level.GroundCollider){var b=m.level.GroundCollider.bounds;s.Left=b.min.x;s.Right=b.max.x;}
            var all=m.payables?.AllPayables;
            if(all!=null)
            {
                for(int i=0;i<all.Length;i++)
                {
                    var p=all[i];if(!p||!p.gameObject.activeInHierarchy)continue;
                    // Monarch and ridden mount already have player markers.
                    if(NativeParent<Player>(p))continue;
                    var ridden=NativeParent<Steed>(p);
                    if(ridden&&((k.playerOne&&k.playerOne.steed&&ridden==k.playerOne.steed)||(k.playerTwo&&k.playerTwo.steed&&ridden==k.playerTwo.steed)))continue;
                    var entry=ResolvePayable(p);var x=p.transform.position.x;
                    s.Left=Math.Min(s.Left,x);s.Right=Math.Max(s.Right,x);
                    if(!s.Counts.ContainsKey(entry.Key))s.Counts[entry.Key]=0;s.Counts[entry.Key]++;
                    if(entry.Category=="环境"&&!Plugin.Instance.Settings.Trees.Value)continue;
                    if(entry.Key=="unknown"&&Catalog.Clean(p.name).StartsWith("Player ",StringComparison.OrdinalIgnoreCase))continue;
                    var point=new MapPoint{Key=entry.Key,Name=entry.Key=="unknown"?Catalog.Clean(p.name):entry.Name,Category=entry.Category,X=x,Cost=p.Price};
                    if(k.playerOne&&k.playerOne.hasLocalAuthority)point.PlayerStates[k.playerOne.playerId]=ReadTarget(p,k.playerOne);
                    if(Managers.IsP2Playing&&k.playerTwo&&k.playerTwo.hasLocalAuthority)point.PlayerStates[k.playerTwo.playerId]=ReadTarget(p,k.playerTwo);
                    s.Points.Add(point);
                }
            }
            var camps=k.BeggarCamps;
            if(camps!=null)
            {
                var iterator=camps.GetEnumerator();
                while(iterator.MoveNext())
                {
                    var camp=iterator.Current;if(!camp||!camp.gameObject.activeInHierarchy)continue;
                    s.Camps++;float cx=camp.transform.position.x;
                    var point=s.Points.FirstOrDefault(p=>p.Name.Contains("营地")&&Math.Abs(p.X-cx)<1);
                    if(point==null){point=new MapPoint{Key="beggarcamp",Name="难民营",Category="人口",X=cx};s.Points.Add(point);}
                    point.CampPeople=0;var beggars=camp._beggars;
                    if(beggars!=null)for(int i=0;i<beggars.Count;i++)if(beggars[i]&&beggars[i].gameObject.activeInHierarchy)point.CampPeople++;
                }
            }
            s.Points.Sort((a,b)=>a.X.CompareTo(b.X));
            State=s;
        }
        public void ReadEnemies()
        {
            State.Enemies.Clear();State.EnemiesAvailable=false;if(!State.Playing)return;
            var m=GetManagers();if(!m||!m.enemies)return;
            var enemies=m.enemies.AllEnemies;if(enemies==null)return;
            var enemyArray=new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Enemy>(enemies.Count);
            enemies.CopyTo(enemyArray,0);
            for(int ei=0;ei<enemyArray.Length;ei++)
            {
                var enemy=enemyArray[ei];if(!enemy||!enemy.gameObject.activeInHierarchy||enemy.IsDespawning)continue;
                if(enemy.damageable&&enemy.damageable.hitPoints<=0)continue;
                string kind;
                switch(enemy.Type)
                {
                    case EnemyType.TrollWeak:case EnemyType.TrollMedium:case EnemyType.ToughTroll:kind="贪婪怪";break;
                    case EnemyType.Ogre:kind="巨人";break;
                    case EnemyType.Squid:kind="飞行怪";break;
                    case EnemyType.Stealer:kind="夺冠者";break;
                    case EnemyType.Archer:kind="敌弓手";break;
                    case EnemyType.Knight:kind="敌骑士";break;
                    case EnemyType.Crusher:kind="碾压者";break;
                    default:kind=enemy.Type.ToString();break;
                }
                State.Enemies.Add(new EnemyPoint{Id=enemy.GetInstanceID(),Kind=kind,X=enemy.transform.position.x});
            }
            State.EnemiesAvailable=true;
        }
        public void ReadPlayers()
        {
            State.Players.Clear();State.ExploredRanges.Clear();if(!State.Playing)return;
            var m=GetManagers();if(!m||!m.kingdom)return;
            var k=m.kingdom;ReadPlayer(k.playerOne,m.game._mainCameraComponent);
            if(Managers.IsP2Playing)ReadPlayer(k.playerTwo,m.game._secondCameraComponent);
            foreach(var range in Explored.Values)State.ExploredRanges.Add(range);
        }
        private void ReadPlayer(Player p,Camera cam)
        {
            if(!p||!p.gameObject.activeInHierarchy)return;
            var info=new PlayerInfo{Id=p.playerId,Local=p.hasLocalAuthority,X=p.transform.position.x,Coins=p.coins,Gems=p.gems,Capacity=p.wallet?p.wallet.TotalCapacity:0};
            if(p.wallet)foreach(var currency in State.ResourceTypes)info.Resources[currency]=p.wallet.GetCurrency((CurrencyType)currency);
            if(p.wallet){info.BagTotal=p.wallet.TotalCurrency;info.BagCapacity=AdvisorBehaviour.Active.Actions.NormalCapacity(p.playerId);}
            if(cam){var rect=cam.rect;info.ViewX=rect.x;info.ViewY=rect.y;info.ViewW=rect.width;info.ViewH=rect.height;}
            if(!info.Local){info.ViewX=0;info.ViewY=0;info.ViewW=1;info.ViewH=1;}
            var steed=p.steed;
            if(steed){info.Mount=Catalog.Clean(steed.name);info.Stamina=steed.Stamina;info.Fed=steed.WellFedTimer;info.MountState=steed.IsTired?"疲劳":steed.WellFedTimer>0?"饱食":"正常";info.Ability=p.IsManualAttackActivable?"攻击可用":p.IsSpitActivable?"特殊能力可用":"";}
            if(steed&&steed.steedAbilities!=null){var abilities=steed.steedAbilities;for(int ai=0;ai<abilities.Length;ai++){var ability=abilities[ai];if(ability&&!ability.IsAutomaticAbility)info.Ability+=(info.Ability.Length>0?" · ":"")+(ability.IsAbilityReady?"能力就绪":"能力冷却中");}}
                        bool OwnActor(Payable candidate)
            {
                if(candidate.gameObject==p.gameObject||NativeParent<Player>(candidate))return true;
                var mount=NativeParent<Steed>(candidate);
                return mount&&steed&&mount==steed;
            }
            var target=p.selectedPayable;
            if(target&&(OwnActor(target)||!TargetProximity.Contains(info.X,target.transform.position.x)))target=null;
            if(!target&&info.Local)
            {
                float dist=TargetProximity.Radius;var all=Managers._Inst.payables?.AllPayables;
                if(all!=null)for(int i=0;i<all.Length;i++){var candidate=all[i];if(!candidate||!candidate.gameObject.activeInHierarchy||OwnActor(candidate))continue;float d=Math.Abs(candidate.transform.position.x-info.X);if(d<dist){dist=d;target=candidate;}}
            }
            GameObject nearest=target?target.gameObject:null;
            float buildingDistance=nearest?Math.Abs(nearest.transform.position.x-info.X):TargetProximity.Radius;
            void Consider(Component building)
            {
                if(!building||!building.gameObject.activeInHierarchy||!TargetProximity.Contains(info.X,building.transform.position.x))return;
                float distance=Math.Abs(building.transform.position.x-info.X);
                if(distance<buildingDistance){buildingDistance=distance;nearest=building.gameObject;}
            }
            var kingdom=Managers._Inst.kingdom;
            if(info.Local&&kingdom)
            {
                if(Time.unscaledTime>=nextBuildingScan)
                {
                    var buildings=new List<Component>();
                    buildings.AddRange(NativeObjects<WorkableBuilding>().Select(b=>(Component)b));
                    buildings.AddRange(NativeObjects<Ballista>().Select(b=>b.tower?(Component)b.tower:(Component)b));
                    buildings.AddRange(NativeObjects<Baker>().Select(b=>(Component)b));
                    detailBuildings=buildings.ToArray();nextBuildingScan=Time.unscaledTime+2;
                }
                foreach(var building in detailBuildings)Consider(building);
                Consider(kingdom.castle);
                if(kingdom._walls!=null)foreach(var wall in kingdom._walls)Consider(wall);
                if(kingdom._towers!=null)foreach(var tower in kingdom._towers)Consider(tower);
                if(kingdom._farmHouses!=null)foreach(var farm in kingdom._farmHouses)Consider(farm);
            }
            if(nearest&&TargetProximity.Contains(info.X,nearest.transform.position.x))info.Target=ReadBuildingTarget(nearest,p);
            if(Explored.TryGetValue(info.Id,out var range))Explored[info.Id]=System.Tuple.Create(Math.Min(range.Item1,info.X-15),Math.Max(range.Item2,info.X+15));
            else Explored[info.Id]=System.Tuple.Create(info.X-15,info.X+15);
            State.Players.Add(info);
        }
        private TargetInfo ReadTarget(Payable payable,Player player)
        {
                        return ReadBuildingTarget(payable.gameObject,player);
        }
        private TargetInfo ReadBuildingTarget(GameObject obj,Player player)
        {
            var payable=NativeComponent<Payable>(obj);
                        var raw=obj.name;var entry=ResolveBuilding(obj);
            var stableFarm=NativeComponent<Farmhouse>(obj);if(stableFarm&&stableFarm.isStable)entry=Catalog.Entries.First(e=>e.Key=="stable");
            if(NativeChild<Ballista>(obj))entry=Catalog.Entries.First(e=>e.Key=="ballista");
            else if(NativeChild<Baker>(obj))entry=Catalog.Entries.First(e=>e.Key=="baker");
            else if(NativeChild<FireTower>(obj))entry=Catalog.Entries.First(e=>e.Key=="firetower");
            var t=new TargetInfo{Raw=raw,Name=entry.Name,Description=entry.Description,Advice=entry.Advice,Category=entry.Category,Cost=payable?payable.Price:-1,Currency=payable?Catalog.Currency(payable.Currency.ToString()):"",X=obj.transform.position.x};
            if(payable){LockIndicator.LockReason reason;t.Locked=payable.IsLocked(player,out reason);t.Lock=Catalog.Lock(reason.ToString());}
            var upgrade=payable?payable.TryCast<PayableUpgrade>():null;
            if(upgrade&&upgrade.nextPrefab){var nextEntry=ResolveBuilding(upgrade.nextPrefab);t.Next=nextEntry.Name+"："+nextEntry.Description;}
            var wall=NativeComponent<Wall>(obj);
            if(wall){t.Level=wall.level;t.Details="驻防：弓箭手 "+wall.archerCount+" · 长枪兵 "+wall.pikemanCount;var damage=wall._damageable;if(damage){t.Health=damage.hitPoints;t.HealthMax=damage.initialHitPoints;t.Details+="\n耐久 "+damage.hitPoints+" / 初始 "+damage.initialHitPoints;}}
            if(upgrade&&upgrade.passengerUpgrades!=null)
                foreach(var option in upgrade.passengerUpgrades)
                {
                    if(option==null||!option.prefab)continue;
                    var targetEntry=ResolveBuilding(option.prefab);
                    if(wall&&wall.isHornUpgrade&&targetEntry.Key=="hornwall")continue;
                    var conversion=Catalog.Conversion(targetEntry,option.tag);
                    if(conversion.Length>0)t.Details+="\n"+conversion;
                }
            var tower=NativeComponent<Tower>(obj);if(tower)t.Level=tower.level;
            var farm=NativeComponent<Farmhouse>(obj);if(farm)t.Level=farm.level;
            var castle=NativeComponent<Castle>(obj);if(castle)t.Level=(int)castle.level+1;
            if(farm){t.Details=farm.isStable?"马厩坐骑 "+(farm.stabledSteeds?.Count??0)+" / "+farm.maxNumSteeds:"农田地块 "+(farm.farmlands?.Count??0)+" · 基础容量 "+farm.maxFarmlands;}
            var shop=payable?payable.TryCast<PayableShop>():null;if(shop){int stocked=0;var items=shop._items;if(items!=null)for(int i=0;i<items.Length;i++)if(items[i]&&items[i].gameObject.activeInHierarchy&&!items[i].pickedUp)stocked++;t.Stock=stocked;t.StockMax=shop.maxItems;t.Details="工具库存 "+stocked+" / "+shop.maxItems;}
            var building=NativeComponent<WorkableBuilding>(obj);
            if(building&&building.UnderConstruction){t.UnderConstruction=true;var construction=building._constructionBuilding;if(construction&&construction._buildPoints>0){t.Construction=Math.Clamp(construction._currentBuildPoints/construction._buildPoints*100,0,100);t.Details+="\n施工进度 "+t.Construction.ToString("F0")+"%";}}
            var statue=payable?payable.TryCast<Statue>():null;
            if(statue){t.Details+=(t.Details.Length>0?"\n":"")+"雕像状态："+(statue.deityStatus==Statue.DeityStatus.Activated?"已激活":statue.deityStatus==Statue.DeityStatus.GemLocked?"待解锁":"待激活");var timed=payable.TryCast<TimedStatue>();if(timed)t.Details+="\n剩余天数 "+timed._daysLeft;}
            var timeStatue=payable?payable.TryCast<TimeStatue>():null;if(timeStatue)t.Details+="\n剩余天数 "+timeStatue._daysRemaining;
            if(State.Counts.TryGetValue(entry.Key,out var count)&&entry.Key!="unknown")t.Details+=(t.Details.Length>0?"\n":"")+"本岛同类交互点："+count;
            return t;
        }
    }
}
