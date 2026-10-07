using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using BepInEx;
using BepInEx.Configuration;

namespace KingdomAdvisor
{
    internal sealed partial class AdvisorController
    {
        public bool Open;public int Tab,Focus,Selected,Owner,DetailPage;public string Search="",Category="全部";
        public string Device="键鼠";
        public Snapshot CurrentSnapshot=>current;
        public GameActions Actions;public IslandJournal Journal;private Snapshot current=new Snapshot();
        public int Section,Choice,ResourcePlayer,ResourceType,ClearMode;public bool SettingsDropdown,ResourceDropdown,ResourceEditing,MapFocus;
        public string ResourceText="10";
        public readonly MapHold Hold=new MapHold();private MapPoint pressedPoint;private string mouseTarget="";
        private bool mousePress;public MapPoint FocusedMapPoint;
        public static string PointKey(MapPoint point)=>point.Name+"|"+Math.Round(point.X,1);
        public void BeginMapDraw(int id){mouseTarget="";ClearPadHit(id);}
        public void MapHit(MapPoint point,int owner){mouseTarget=PointKey(point);if(Event.current.type==EventType.MouseDown&&Event.current.button==0){Device="键鼠";pressedPoint=point;mousePress=true;Hold.Begin(mouseTarget,owner,Time.unscaledTime,settings.Teleport.Value);Event.current.Use();}}
        public void Inspect(MapPoint point,int owner)
        {if(!Open||Tab!=3)Toggle(3,current,owner);Category="全部";var p=current.Players.FirstOrDefault(p=>p.Id==owner);if(p!=null){var items=FilteredPoints(current,p);int found=Array.FindIndex(items,x=>x.Name==point.Name&&Math.Abs(x.X-point.X)<1);if(found>=0)Selected=found;}}
        private void StepMapHold(string target,bool held)
        {var point=pressedPoint;int owner=Hold.Owner;var action=Hold.Step(target,held,Time.unscaledTime,current.Playing&&(!Hold.CanTeleport||settings.Teleport.Value));if(action==GestureResult.Teleport)Actions.Teleport(current,owner,point);else if(action==GestureResult.Inspect)Inspect(point,owner);}
        public readonly string[] Categories={"全部","建设","防御","经济","人口","工具","航行","科技","探索","特殊","坐骑","环境","未分类"};
        private readonly Settings settings;
        private bool discovered;
        public bool SearchFocused;
        public bool SearchEditing,MapNearby=false;
        public int PadRegion,SearchCursor,VisibleRows=1,DetailPages=1;
        public readonly string[] SearchKeys="abcdefghijklmnopqrstuvwxyz0123456789".Select(c=>c.ToString()).Concat(new[]{"删除","清空","完成"}).ToArray();
        private readonly MenuRepeat verticalRepeat=new MenuRepeat(),horizontalRepeat=new MenuRepeat();
        private Vector3 lastMouse;
        public float PointerUntil=-1;
        private readonly Dictionary<int,Rewired.Player> routedInputs=new Dictionary<int,Rewired.Player>();
        private readonly Dictionary<int,float> chordStarted=new Dictionary<int,float>();
        private readonly HashSet<int> chordLatched=new HashSet<int>();
        private readonly HashSet<int> releaseGuard=new HashSet<int>();
        
        public void RecordInput(int player,Rewired.Player input){if(input!=null)routedInputs[player]=input;}
        public bool ChordHeld(int player)=>false;
        public AdvisorController(Settings config){settings=config;}
        public bool Blocks(int id)=>((Open||MapFocus)&&Owner==id)||(Hold.Active&&Hold.Owner==id)||releaseGuard.Contains(id)||PadBlocks(id);
        public void Close(){if(Open||MapFocus)releaseGuard.Add(Owner);Open=MapFocus=false;Hold.Cancel();SettingsDropdown=ResourceDropdown=ResourceEditing=false;Focus=0;Search="";SearchFocused=false;SearchEditing=false;PadRegion=0;verticalRepeat.Reset();horizontalRepeat.Reset();}
        public void Toggle(int tab,Snapshot state,int? player=null)
        {
            if(Open&&Tab==tab){Close();return;}
            Owner=player??state.Players.FirstOrDefault(p=>p.Local)?.Id??0;ClearPadHit(Owner);
            Open=true;MapFocus=false;Tab=tab;Focus=0;PadRegion=0;SettingsDropdown=ResourceDropdown=ResourceEditing=false;SearchEditing=false;SearchFocused=false;verticalRepeat.Reset();horizontalRepeat.Reset();
            var m=Managers._Inst;
            if(m&&m.kingdom){var p=m.kingdom.playerOne;if(p&&p.playerId==Owner)p.ReleaseInput();p=m.kingdom.playerTwo;if(p&&p.playerId==Owner)p.ReleaseInput();}
        }
        public void Update(Snapshot state)
        {
            current=state;
            if(!discovered&&Rewired.ReInput.isReady)DiscoverActions();
            if(!state.Playing){ResetHudButtons();RestorePadMappings();Close();Hold.Reset();routedInputs.Clear();chordStarted.Clear();chordLatched.Clear();releaseGuard.Clear();return;}
            PollPads(state);
            if(Input.GetKeyDown(KeyCode.F4)){settings.Enabled.Value=!settings.Enabled.Value;if(!settings.Enabled.Value)Close();Device="键鼠";}
            if(Input.GetKeyDown(KeyCode.F6)){settings.Enabled.Value=true;Toggle(2,state);Device="键鼠";}
            if(Input.GetKeyDown(KeyCode.F7)){settings.Enabled.Value=true;Toggle(0,state);Device="键鼠";}
            if(Input.mousePosition!=lastMouse||Input.GetMouseButtonDown(0)){lastMouse=Input.mousePosition;if(MapFocus){MapFocus=false;releaseGuard.Add(Owner);Hold.Cancel();}Device="键鼠";PointerUntil=Time.unscaledTime+8;}
            if(mousePress&&Hold.Active){StepMapHold(mouseTarget,Input.GetMouseButton(0));if(!Input.GetMouseButton(0))mousePress=false;}
            if(MapFocus){UpdateMapPad(state);return;}
            if(!Open)return;
            if(!state.Players.Any(p=>p.Local&&p.Id==Owner)){Close();return;}
            if(Input.GetKeyDown(KeyCode.Escape)){if(ResourceDropdown)ResourceDropdown=false;else if(SettingsDropdown)SettingsDropdown=false;else if(ResourceEditing)ResourceEditing=false;else Close();return;}
            if(Input.GetKeyDown(KeyCode.Tab)){ChangeTab(1);Device="键鼠";}
            if(ResourceEditing&&Tab==5){foreach(char c in Input.inputString){if(c=='\b'){if(ResourceText.Length>0)ResourceText=ResourceText.Substring(0,ResourceText.Length-1);}else if(char.IsDigit(c)&&ResourceText.Length<5)ResourceText+=c;}if(Input.GetKeyDown(KeyCode.Return))CommitAmount();return;}
            if(SearchFocused&&Tab==0)
            {
                // 每个 Update 只处理一次提交字符，避开 IL2CPP 中未还原的 TextEditor。
                string submitted=Input.inputString;
                foreach(char character in submitted)
                {
                    if(character=='\b'){if(Search.Length>0){int count=Search.Length>1&&char.IsLowSurrogate(Search[Search.Length-1])&&char.IsHighSurrogate(Search[Search.Length-2])?2:1;Search=Search.Substring(0,Search.Length-count);}}
                    else if(character=='\n'||character=='\r')SearchFocused=false;
                    else if(!char.IsControl(character)&&Search.Length<256)Search+=character;
                }
                if(submitted.Length>0){Selected=0;Device="键鼠";}
                if(Input.GetKeyDown(KeyCode.Return)){SearchFocused=false;return;}
            }
            if(!SearchFocused)
            {
                if(Input.GetKeyDown(KeyCode.UpArrow)){Navigate(-1,state);Device="键鼠";}
                if(Input.GetKeyDown(KeyCode.DownArrow)){Navigate(1,state);Device="键鼠";}
                if(Input.GetKeyDown(KeyCode.LeftArrow)){Horizontal(-1);Device="键鼠";}
                if(Input.GetKeyDown(KeyCode.RightArrow)){Horizontal(1);Device="键鼠";}
                if(Input.GetKeyDown(KeyCode.Return)&&Tab!=3){Device="键鼠";Activate();}
                if(Tab==3&&Device=="键鼠")UpdatePanelMapHold(Input.GetKey(KeyCode.Return),Input.GetKeyDown(KeyCode.Return));
            }
        }
        public void ChangeTab(int direction){ClearPadHit(Owner);Tab=(Tab+direction+6)%6;Focus=0;Selected=0;DetailPage=0;PadRegion=0;SettingsDropdown=ResourceDropdown=ResourceEditing=false;Hold.Cancel();SearchEditing=false;SearchFocused=false;verticalRepeat.Reset();horizontalRepeat.Reset();}
        public void SearchKey(int index)
        {string key=SearchKeys[index];if(key=="删除"){if(Search.Length>0)Search=Search.Substring(0,Search.Length-1);}else if(key=="清空")Search="";else if(key=="完成"){SearchEditing=false;PadRegion=0;}else if(Search.Length<256)Search+=key;Selected=0;}
        private void DiscoverActions()
        {
            var list=Rewired.ReInput.mapping.Actions;var lines=new List<string>();
            var count=list.Cast<Il2CppSystem.Collections.Generic.ICollection<Rewired.InputAction>>().Count;
            for(int i=0;i<count;i++){var a=list[i];lines.Add(a.id+"\t"+a.name+"\t"+a.descriptiveName);}
            Directory.CreateDirectory(Path.Combine(Paths.ConfigPath,"KingdomAdvisor"));
            File.WriteAllLines(Path.Combine(Paths.ConfigPath,"KingdomAdvisor","input-actions.tsv"),lines);
            discovered=true;Plugin.Instance.Log.LogInfo("Rewired actions: "+string.Join(", ",lines));
        }
        public Entry[] FilteredEntries()=>Catalog.Entries.Where(e=>(Category=="全部"||e.Category==Category)&&(string.IsNullOrWhiteSpace(Search)||(e.Name+e.Description+e.Key+Localization.Translate(e.Name+" "+e.Description,true)).IndexOf(Search,StringComparison.OrdinalIgnoreCase)>=0)).ToArray();
        public MapPoint[] FilteredPoints(Snapshot state,PlayerInfo player)=>state.Points.Where(p=>PointVisible(p)&&(Category=="全部"||p.Category==Category)&&(settings.FullMap.Value||state.ExploredRanges.Any(r=>p.X>=r.Item1&&p.X<=r.Item2))).OrderBy(p=>Math.Abs(p.X-player.X)).ToArray();
        public void FlipPage(int direction,Snapshot s)
        {
            if(Tab==2){SettingsDropdown=false;int rows=Math.Max(1,VisibleRows),count=OptionGroups.Items[Section].Length;Focus=MenuPaging.Flip(Focus,rows,count,direction);return;}
            Navigate(direction*Math.Max(1,VisibleRows),s);
        }
        public void Horizontal(int direction)
        {if(Tab==2){if(SettingsDropdown)Choice=Math.Clamp(Choice+direction,0,OptionGroups.Items[Section][Focus].Choices.Length-1);else {Section=(Section+direction+OptionGroups.Names.Length)%OptionGroups.Names.Length;Focus=0;}}else if(Tab==5){if(ResourceDropdown)Choice=Math.Clamp(Choice+direction,0,ResourceChoices().Length-1);else ResourceAdjust(direction);}else{int index=Array.IndexOf(Categories,Category);Category=Categories[(index+direction+Categories.Length)%Categories.Length];Selected=0;DetailPage=0;}}
        public void Navigate(int delta,Snapshot state)
        {Hold.Cancel();if(Tab==2){if(SettingsDropdown)Choice=Math.Clamp(Choice+delta,0,OptionGroups.Items[Section][Focus].Choices.Length-1);else {int length=OptionGroups.Items[Section].Length;Focus=(Focus+delta+length)%length;}}else if(Tab==5){if(ResourceDropdown)Choice=Math.Clamp(Choice+delta,0,ResourceChoices().Length-1);else Focus=(Focus+delta+7)%7;}else {var player=state.Players.FirstOrDefault(p=>p.Id==Owner);int count=Tab==4?Journal.Records.Count:Tab==0?FilteredEntries().Length:player==null?0:FilteredPoints(state,player).Length;Selected=Math.Clamp(Selected+delta,0,Math.Max(0,count-1));}}
        private void UpdateMapPad(Snapshot s)
        {
            if(!routedInputs.TryGetValue(Owner,out var input))return;
            var p=s.Players.FirstOrDefault(p=>p.Id==Owner&&p.Local);if(p==null){Close();return;}
            if(ChordHeld(Owner))return;
            if(input.GetButtonDown(RewiredAxis.Cancel)){Device="手柄";Close();return;}
            if(input.GetButtonDown(RewiredAxis.MenuNextTab)){Toggle(0,s,Owner);return;}
            if(input.GetButtonDown(RewiredAxis.MenuPrevTab)){Toggle(2,s,Owner);return;}
            var points=FilteredPoints(s,p);if(points.Length==0){Hold.Cancel();return;}
            int delta=horizontalRepeat.Step(input.GetAxis(RewiredAxis.Horizontal),Time.unscaledTime)-verticalRepeat.Step(input.GetAxis(RewiredAxis.Vertical),Time.unscaledTime);
            if(delta!=0){Selected=Math.Clamp(Selected+delta,0,points.Length-1);Hold.Cancel();}
            FocusedMapPoint=points[Math.Clamp(Selected,0,points.Length-1)];Device="手柄";
            UpdatePanelMapHold(input.GetButton(RewiredAxis.Submit),input.GetButtonDown(RewiredAxis.Submit));
        }
        private void UpdatePanelMapHold(bool held,bool down)
        {
            var player=current.Players.FirstOrDefault(p=>p.Id==Owner);if(player==null)return;
            var points=FilteredPoints(current,player);if(points.Length==0)return;
            var point=points[Math.Clamp(Selected,0,points.Length-1)];FocusedMapPoint=point;
            if(down&&!Hold.Active){pressedPoint=point;mousePress=false;Hold.Begin(PointKey(point),Owner,Time.unscaledTime,settings.Teleport.Value);}
            if(Hold.Active&&!mousePress)StepMapHold(PointKey(point),held);
        }
        private ConfigEntry<bool> Boolean(OptionId id)
        {
            switch(id){case OptionId.Map:return settings.Map;case OptionId.Background:return settings.MapTransparent;case OptionId.Position:return settings.MapAtTop;case OptionId.FullMap:return settings.FullMap;case OptionId.Trees:return settings.Trees;
            case OptionId.Buildings:return settings.MapBuildings;case OptionId.Camps:return settings.MapCamps;case OptionId.Enemies:return settings.MapEnemies;case OptionId.Mounts:return settings.MapMounts;case OptionId.Special:return settings.MapSpecial;
            case OptionId.AdvisorBackground:return settings.AdvisorTransparent;case OptionId.PlayerBackground:return settings.PlayerTransparent;case OptionId.AlertBackground:return settings.AlertTransparent;case OptionId.InterfaceBackground:return settings.InterfaceTransparent;case OptionId.Kingdom:return settings.ShowKingdomAdvisor;case OptionId.Player:return settings.ShowPlayerPanel;case OptionId.Population:return settings.Status;case OptionId.Details:return settings.Details;case OptionId.Alerts:return settings.Alerts;
            case OptionId.Defense:return settings.DefenseAlerts;case OptionId.Staff:return settings.StaffAlerts;case OptionId.MountInfo:return settings.MountInfo;case OptionId.Icons:return settings.Icons;case OptionId.Journal:return settings.Journal;
            case OptionId.PadCursor:return settings.PadCursor;case OptionId.InfiniteBag:return settings.InfiniteBag;case OptionId.Teleport:return settings.Teleport;case OptionId.InfiniteStamina:return settings.InfiniteStamina;default:return null;}
        }
        public int OptionValue(OptionId id)
        {if(id==OptionId.Language)return settings.UiLanguage.Value;if(id==OptionId.PadOpen)return settings.PadOpenButton.Value;if(id==OptionId.PadSpeed)return Math.Max(0,Array.IndexOf(new[]{300f,700f,1000f,1400f},settings.PadSpeed.Value));if(id==OptionId.Font)return Math.Clamp((settings.FontSize.Value-14)/2,0,7);if(id==OptionId.Speed)return Array.IndexOf(new[]{0f,1f,2f,4f},settings.GameSpeed.Value);var item=Boolean(id);if(item==null)return 0;bool invert=id==OptionId.Background||id==OptionId.AdvisorBackground||id==OptionId.PlayerBackground||id==OptionId.AlertBackground||id==OptionId.InterfaceBackground||id==OptionId.Position;return item.Value?(invert?0:1):(invert?1:0);}
        public void SetOption(OptionId id,int value)
        {
            if(id==OptionId.Language){settings.UiLanguage.Value=value;Localization.English=Localization.IsEnglish(value,global::Language.current?global::Language.current.languageCode:null,Application.systemLanguage.ToString());DetailPage=0;return;}
            if(id==OptionId.PadOpen){settings.PadOpenButton.Value=value;return;}
            if(id==OptionId.PadSpeed){settings.PadSpeed.Value=new[]{300f,700f,1000f,1400f}[value];return;}
            if(id==OptionId.Font){settings.FontSize.Value=14+value*2;return;}
            if(id==OptionId.Speed){settings.GameSpeed.Value=new[]{0f,1f,2f,4f}[value];return;}
            var item=Boolean(id);if(item==null)return;item.Value=id==OptionId.Background||id==OptionId.AdvisorBackground||id==OptionId.PlayerBackground||id==OptionId.AlertBackground||id==OptionId.InterfaceBackground||id==OptionId.Position?value==0:value==1;
            if(id==OptionId.Position)settings.Layout.Value=string.Join(";",settings.Layout.Value.Split(';').Where(x=>!x.Split('=')[0].EndsWith(".map",StringComparison.Ordinal)));
        }
        public void CommitAmount()
        {if(int.TryParse(ResourceText,out var amount)&&amount>=1&&amount<=10000){settings.ResourceAmount.Value=amount;ResourceEditing=false;}else Actions.Notify("数量必须是 1–10000");}
        public void ResourceAdjust(int direction)
        {
            if(Focus==0){var players=current.Players.Where(p=>p.Local).ToArray();if(players.Length>0){int i=Array.FindIndex(players,p=>p.Id==ResourcePlayer);ResourcePlayer=players[(i+direction+players.Length)%players.Length].Id;}}
            else if(Focus==1&&current.ResourceTypes.Count>0){int i=current.ResourceTypes.IndexOf(ResourceType);ResourceType=current.ResourceTypes[(i+direction+current.ResourceTypes.Count)%current.ResourceTypes.Count];}
            else if(Focus==2)settings.ResourceAmount.Value=Math.Clamp(settings.ResourceAmount.Value+direction,1,10000);
            else if(Focus==3){int[] presets={1,5,20,100};int i=Array.IndexOf(presets,settings.ResourceAmount.Value);settings.ResourceAmount.Value=presets[(i+direction+presets.Length)%presets.Length];}
        }
        public int[] ClearTypes()=>ClearMode==0?new[]{0}:ClearMode==1?new[]{ResourceType}:current.ResourceTypes.ToArray();
        public string[] ResourceChoices()
        {if(Focus==5)return new[]{"仅金币","当前资源","全部钱袋资源"};if(Focus==0)return current.Players.Where(p=>p.Local).Select(p=>"P"+(p.Id+1)).ToArray();if(Focus==1)return current.ResourceTypes.Select(ResourceRules.Name).ToArray();return new[]{"1","5","20","100"};}
        private int ResourceChoiceIndex()
        {if(Focus==5)return ClearMode;if(Focus==0)return Math.Max(0,Array.FindIndex(current.Players.Where(p=>p.Local).ToArray(),p=>p.Id==ResourcePlayer));if(Focus==1)return Math.Max(0,current.ResourceTypes.IndexOf(ResourceType));return Math.Max(0,Array.IndexOf(new[]{1,5,20,100},settings.ResourceAmount.Value));}
        public void SelectResourceChoice(int index)
        {if(Focus==5){ClearMode=index;ResourceDropdown=false;return;}if(Focus==0){var ps=current.Players.Where(p=>p.Local).ToArray();if(index>=0&&index<ps.Length)ResourcePlayer=ps[index].Id;}else if(Focus==1){if(index>=0&&index<current.ResourceTypes.Count)ResourceType=current.ResourceTypes[index];}else if(Focus==3)settings.ResourceAmount.Value=new[]{1,5,20,100}[Math.Clamp(index,0,3)];ResourceDropdown=false;}
        public void AddResources()
        {if(ResourceEditing)CommitAmount();if(ResourceEditing)return;Actions.Add(current,ResourcePlayer,ResourceType,settings.ResourceAmount.Value);}
        public bool PointVisible(MapPoint p)
        {if(p.Category=="坐骑")return settings.MapMounts.Value;if(p.Category=="人口")return settings.MapCamps.Value;if(p.Category=="建设"||p.Category=="防御"||p.Category=="经济"||p.Category=="工具"||p.Category=="环境")return settings.MapBuildings.Value;return settings.MapSpecial.Value;}
        public void Activate()
        {
            if(Tab==5){if(Focus==6){Actions.ClearResources(current,ResourcePlayer,ClearTypes());return;}if(ResourceDropdown){SelectResourceChoice(Choice);return;}if(Focus==0||Focus==1||Focus==3||Focus==5){Choice=ResourceChoiceIndex();ResourceDropdown=true;return;}if(Focus==4)AddResources();else if(Focus==2){if(Device=="手柄")ResourceAdjust(1);else{ResourceEditing=!ResourceEditing;ResourceText=settings.ResourceAmount.Value.ToString();}}return;}
            if(Tab==2){var item=OptionGroups.Items[Section][Focus];if(item.Id==OptionId.Resources){Toggle(5,current,Owner);return;}if(item.Id==OptionId.ResetLayout){settings.Layout.Value="";return;}if(SettingsDropdown){SetOption(item.Id,Choice);SettingsDropdown=false;}else{Choice=OptionValue(item.Id);SettingsDropdown=true;}return;}
            if(Tab==0){DetailPage++;return;}
            if(Tab==3){MapNearby=!MapNearby;return;}
        }
    }
}
