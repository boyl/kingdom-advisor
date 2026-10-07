using System;
using System.Linq;
using UnityEngine;
using BepInEx;

namespace KingdomAdvisor
{
    internal sealed class AdvisorView
    {
        private readonly Settings settings;private readonly AdvisorController controller;
        private GUIStyle text,title,small,button,fillStyle;
        private Font font;private int lastSize;private bool stylesReady;
        private string lastCatalogKey="";
        private readonly StableMapExtent stableExtent=new StableMapExtent();private int extentIsland=-1;
        private LayoutPositions positions;
        private string savedPositions,dragKey="";
        private Vector2 dragOffset,guiOrigin;private int guiPlayer;
        private Vector2 PointerPosition=>controller.Device=="手柄"?controller.PadPointer(guiPlayer)-guiOrigin:Event.current.mousePosition;

        private readonly System.Collections.Generic.Dictionary<int,System.Tuple<float,float>> enemyPositions=new System.Collections.Generic.Dictionary<int,System.Tuple<float,float>>();
        private readonly System.Collections.Generic.Dictionary<int,int> hudPages=new System.Collections.Generic.Dictionary<int,int>();
        private readonly System.Collections.Generic.Dictionary<int,string> hudTargets=new System.Collections.Generic.Dictionary<int,string>();
        public string FontEvidence="not initialised";
        private readonly Color background=new Color(.045f,.065f,.085f,.94f);
        private readonly Color gold=new Color(.87f,.73f,.45f);
        private readonly Color muted=new Color(.65f,.73f,.77f);
        private readonly Color border=new Color(.23f,.29f,.32f,.9f);
        private readonly Color green=new Color(.49f,.79f,.67f);
        public AdvisorView(Settings config,AdvisorController input){settings=config;controller=input;}
        private Rect Place(string key,Rect rect,Rect viewport,float handleWidth=-1)
        {
            if(savedPositions!=settings.Layout.Value)
            {
                savedPositions=settings.Layout.Value;
                try{positions=new LayoutPositions(savedPositions);}
                catch(FormatException ex){Plugin.Instance.Log.LogWarning("Invalid saved layout: "+ex.Message);positions=new LayoutPositions("");}
            }
            if(positions==null)positions=new LayoutPositions("");
            float maxX=Math.Max(0,viewport.width-rect.width-8),maxY=Math.Max(0,viewport.height-rect.height-8);
            var saved=positions.Get(key);if(saved!=null){rect.x=saved.Item1*maxX;rect.y=saved.Item2*maxY;}
            rect.x=Math.Clamp(rect.x,8,Math.Max(8,maxX));rect.y=Math.Clamp(rect.y,8,Math.Max(8,maxY));
            var current=Event.current;
            if(!controller.Open&&current.type==EventType.MouseDown&&current.button==0&&new Rect(rect.x,rect.y,handleWidth>0?handleWidth:rect.width,38).Contains(current.mousePosition))
            {dragKey=key;dragOffset=current.mousePosition-new Vector2(rect.x,rect.y);current.Use();}
            if(dragKey==key&&(current.type==EventType.MouseDrag||current.type==EventType.MouseUp))
            {
                rect.x=Math.Clamp(current.mousePosition.x-dragOffset.x,8,Math.Max(8,maxX));rect.y=Math.Clamp(current.mousePosition.y-dragOffset.y,8,Math.Max(8,maxY));
                positions.Set(key,maxX>0?rect.x/maxX:0,maxY>0?rect.y/maxY:0);
                if(current.type==EventType.MouseUp){settings.Layout.Value=positions.Encode();savedPositions=settings.Layout.Value;dragKey="";}
                current.Use();
            }
            return rect;
        }
        private void Styles()
        {
            int size=Math.Clamp(settings.FontSize.Value,14,28);
            if(stylesReady&&lastSize==size)return;
            if(!font)
            {
                font=new Font();
                Font.Internal_CreateDynamicFont(font,new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStringArray(new[]{"Microsoft YaHei"}),size);
                if(!font)throw new InvalidOperationException("Microsoft YaHei font unavailable");
            }
            text=new GUIStyle(GUI.skin.label){font=font,fontSize=size,wordWrap=true,richText=false,padding=new RectOffset(0,0,0,0)};text.normal.textColor=new Color(.91f,.93f,.91f);
            title=new GUIStyle(text){fontSize=size+4};title.normal.textColor=gold;
            small=new GUIStyle(text){fontSize=Math.Max(14,size-2)};small.normal.textColor=muted;
            button=new GUIStyle(text){alignment=TextAnchor.MiddleLeft,padding=new RectOffset(12,12,4,4)};
            fillStyle=new GUIStyle();fillStyle.normal.background=Texture2D.whiteTexture;
            var measure=text.CalcSize(new GUIContent("王国顾问 · 升级条件"));
            FontEvidence=(font?font.name:"missing")+" sampleWidth="+measure.x+" lineHeight="+text.lineHeight;
            Plugin.Instance.Log.LogInfo("Overlay font: "+FontEvidence);
            lastSize=size;stylesReady=true;
        }
        private void Fill(Rect rect,Color color){var old=GUI.color;GUI.color=color;GUI.Box(rect,GUIContent.none,fillStyle);GUI.color=old;}
        private void Card(Rect rect,bool transparent=false)
        {

            if(transparent)return;
            Fill(new Rect(rect.x+3,rect.y+4,rect.width,rect.height),new Color(0,0,0,.22f));
            Fill(rect,border);Fill(new Rect(rect.x+1,rect.y+1,rect.width-2,rect.height-2),background);
            Fill(new Rect(rect.x+16,rect.y,Math.Min(48,rect.width-32),2),gold);
        }
        private bool Button(Rect rect,string label,bool transparent=false)
        {
            label=Localization.Text(label);
            if(Event.current.type==EventType.MouseDown&&rect.Contains(Event.current.mousePosition))controller.Device="键鼠";
            bool selected=label.StartsWith("▶")||label.StartsWith("●");
            bool hover=(controller.Device!="手柄"||controller.PadPointerHover(guiPlayer))&&rect.Contains(PointerPosition);

            if(!transparent&&(!settings.InterfaceTransparent.Value||selected||hover))Fill(rect,selected?new Color(.22f,.24f,.20f,.98f):hover?new Color(.16f,.22f,.25f,.98f):new Color(.095f,.13f,.16f,.96f));
            if(selected)Fill(new Rect(rect.x,rect.y,3,rect.height),gold);
            int originalSize=button.fontSize;
            while(button.fontSize>14&&button.CalcHeight(new GUIContent(label),rect.width)>rect.height)button.fontSize--;
            bool clicked=GUI.Button(rect,label,button)||controller.PointerButton(rect,guiPlayer,guiOrigin);button.fontSize=originalSize;return clicked;
        }
        private float Label(Rect area,string content,GUIStyle style)
        {content=Localization.Text(content);float h=style.CalcHeight(new GUIContent(content),area.width);var color=style.normal.textColor;style.normal.textColor=new Color(0,0,0,.85f);GUI.Label(new Rect(area.x+1,area.y+1,area.width,h),content,style);style.normal.textColor=color;GUI.Label(new Rect(area.x,area.y,area.width,h),content,style);return h;}
        private float Row(float x,float y,float width,string content,GUIStyle style)=>Label(new Rect(x,y,width,0),content,style)+4;
        public void Draw(Snapshot state)
        {
            if(Event.current.type==EventType.MouseMove||Event.current.type==EventType.MouseDrag)controller.Device="键鼠";
            if(!state.Playing){enemyPositions.Clear();stableExtent.Reset();extentIsland=-1;return;}if(!settings.Enabled.Value)return;
            Styles();int previousDepth=GUI.depth;GUI.depth=-200;
            try
            {
            var locals=state.Players.Where(p=>p.Local).ToArray();
            if(locals.Length==0)return;
            foreach(var player in locals)
            {
                var viewport=new Rect(Mathf.Round(player.ViewX*Screen.width),Mathf.Round((1-player.ViewY-player.ViewH)*Screen.height),Mathf.Round(player.ViewW*Screen.width),Mathf.Round(player.ViewH*Screen.height));
                if(viewport.width<200||viewport.height<150)continue;
                guiPlayer=player.Id;guiOrigin=new Vector2(viewport.x,viewport.y);GUI.BeginGroup(viewport);

                DrawHud(new Rect(0,0,viewport.width,viewport.height),state,player);
                if(controller.Open&&controller.Owner==player.Id)DrawPanel(new Rect(0,0,viewport.width,viewport.height),state,player);
                var mouse=Input.mousePosition;
                var pointer=new Vector2(mouse.x-viewport.x,Screen.height-mouse.y-viewport.y);
                if(controller.Device=="键鼠"){var position=PointerPosition;DrawPointer(new Vector2(Math.Clamp(position.x,0,viewport.width-1),Math.Clamp(position.y,0,viewport.height-1)));}
                if(controller.PadPointerActive(player.Id))DrawPointer(controller.PadPointer(player.Id)-guiOrigin);
                GUI.EndGroup();
            }
            }
            finally {GUI.depth=previousDepth;}
        }
        private void DrawHud(Rect viewport,Snapshot s,PlayerInfo player)
        {
            float pad=18,line=Math.Max(22,text.lineHeight),width=Math.Min(viewport.width-36,viewport.width>=900?390:viewport.width*.44f);
            bool stacked=viewport.width<620;
            if(stacked)width=viewport.width-36;
            float y=18;
            string heading="王国顾问";
            string day="岛 "+s.Island+"   /   第 "+s.Day+" 天   /   "+s.Phase+" · "+Season(s.Season);
            if(Localization.English)day="Island "+s.Island+" / Day "+s.Day+" / "+Localization.Text(s.Phase)+" · "+Season(s.Season);
            var rows=new System.Collections.Generic.List<System.Tuple<string,string>>();
            void Info(string icon,string value){rows.Add(System.Tuple.Create(icon,value));}
            Info("person","P"+(player.Id+1));
            Info("coin","金币 "+player.Coins);
            Info("gem","宝石 "+player.Gems);
            Info("bag",settings.InfiniteBag.Value?"无限容量 · 数字钱袋":"资源合计 "+player.BagTotal+" · 原生钱袋");
            if(settings.Status.Value){Info("hammer","工匠 "+s.Workers);Info("bow","弓手 "+s.Archers);Info("shield","骑士 "+s.Knights);Info("wheat","农民 "+s.Farmers);Info("person","游民 "+s.Beggars);Info("tower","建设科技 · "+(s.Iron?"高阶已解锁":s.Stone?"中阶已解锁":"基础")+(s.Online?" · 联机":""));}
            if(settings.MountInfo.Value&&player.Mount.Length>0&&viewport.height>=640){Info("horse","坐骑 "+player.Mount+" · "+player.MountState);Info("stamina","耐力 "+player.Stamina.ToString("F1")+(player.Fed>0?" · 饱食 "+player.Fed.ToString("F0")+" 秒":""));if(player.Ability.Length>0)Info("stamina",player.Ability);}
            float headingH=title.CalcHeight(new GUIContent(Localization.Text(heading)),width-32);
            float dayH=small.CalcHeight(new GUIContent(Localization.Text(day)),width-32);
            float statusH=rows.Sum(r=>small.CalcHeight(new GUIContent(Localization.Text(r.Item2)),width-60)+5);
            float cardH=16+headingH+6+dayH+18+statusH+16;
            y=Math.Max(18,viewport.height-cardH-66);
            if(settings.ShowKingdomAdvisor.Value){
            var statusRect=Place("p"+player.Id+".status",new Rect(pad,y,width,cardH),viewport);Card(statusRect,settings.AdvisorTransparent.Value);
            float cy=statusRect.y+16;cy+=Row(statusRect.x+16,cy,width-32,heading+"  ⇄",title)+2;
            cy+=Row(statusRect.x+16,cy,width-32,day,small)+10;
            Fill(new Rect(statusRect.x+16,cy-5,width-32,1),border);
            foreach(var row in rows){HudIcon(statusRect.x+24,cy+3,row.Item1);cy+=Row(statusRect.x+44,cy,width-60,row.Item2,small)+1;}
            }
            y+=cardH+12;
            float mapW=settings.MapAtTop.Value||stacked?viewport.width-36:viewport.width-width-66;
            float mapX=settings.MapAtTop.Value||stacked?pad:viewport.width-pad-mapW;
            float mapH=Math.Max(350,small.lineHeight*7+196);
            float mapY=settings.MapAtTop.Value?18:Math.Max(18,viewport.height-mapH-66);
            if(settings.Map.Value)
            {
                var mapRect=Place("p"+player.Id+".map",new Rect(mapX,mapY,mapW,mapH),viewport,mapW-130);DrawMap(mapRect,s,player,settings.MapTransparent.Value,true);
            }
            else if(Button(new Rect(mapX,mapY,mapW,40),"开启小地图"))settings.Map.Value=true;
            float objectX=stacked?pad:viewport.width-pad-width;
            float objectY=18;
            float objectRoom=settings.MapAtTop.Value?viewport.height-mapH-100:mapY-30;
            if(settings.ShowPlayerPanel.Value&&objectRoom>line*3)
            {
                var t=player.Target;
                string objectTitle=t==null?"附近交互":t.Name+(t.Level>=0?" · 等级 "+t.Level:"");
                string body=t==null?"靠近建筑、工具或坐骑，查看用途与升级条件。":t.Description+"\n\n费用  "+t.Cost+" "+t.Currency+"\n"+(t.Locked?"暂不可用  ":"可交互  ")+t.Lock;
                if(t!=null&&t.Details.Length>0)body+="\n"+t.Details;
                if(t!=null&&settings.Details.Value){if(t.Next.Length>0)body+="\n\n升级目标  "+t.Next;body+="\n\n"+t.Advice;}
                string key=t?.Raw??"none";
                if(!hudTargets.TryGetValue(player.Id,out var last)||last!=key){hudTargets[player.Id]=key;hudPages[player.Id]=0;}
                float headH=title.CalcHeight(new GUIContent(Localization.Text(objectTitle)),width-32);
                float maxBody=Math.Max(line,Math.Min(320,objectRoom)-headH-88);
                var pages=SplitPages(body,width-32,maxBody);
                int page=hudPages[player.Id]%pages.Length;
                float bodyH=text.CalcHeight(new GUIContent(Localization.Text(pages[page])),width-32);
                float objectH=headH+bodyH+42+(pages.Length>1?42:0);
                objectY=settings.MapAtTop.Value?Math.Max(mapH+30,viewport.height-objectH-66):Math.Max(18,mapY-objectH-12);
                var objectRect=Place("p"+player.Id+".object",new Rect(objectX,objectY,width,objectH),viewport);Card(objectRect,settings.PlayerTransparent.Value);
                float by=objectRect.y+16+Row(objectRect.x+16,objectRect.y+16,width-32,objectTitle+"  ⇄",title)+10;
                Label(new Rect(objectRect.x+16,by,width-32,0),pages[page],text);
                if(pages.Length>1&&Button(new Rect(objectRect.x+16,objectRect.y+objectH-46,width-32,34),"详情 "+(page+1)+" / "+pages.Length+"   下一页 →"))hudPages[player.Id]++;
            }
            if(settings.Alerts.Value)
            {
                string alert=player.Coins==0?"钱袋为空 · 留意收入与返程安全":s.Workers==0?"缺少工匠 · 建设和维修可能停滞":s.Phase=="夜晚"&&player.Target?.Category=="建设"?"夜间施工 · 先确认防线安全":"";
                var defense=settings.DefenseAlerts.Value?AdvisorAlerts.Defense(s,player):"";
                var staff=settings.StaffAlerts.Value?AdvisorAlerts.Staff(s,player.Id):"";
                if(defense.Length>0)alert=defense;else if(staff.Length>0)alert=staff;
                float ah=small.CalcHeight(new GUIContent(Localization.Text(alert)),width-32)+24;
                if(alert.Length>0){var alertRect=Place("p"+player.Id+".alert",new Rect(pad,Math.Max(18,viewport.height-cardH-66-ah-12),width,ah),viewport);Card(alertRect,settings.AlertTransparent.Value);Label(new Rect(alertRect.x+16,alertRect.y+12,width-32,0),alert,small);}
            }
            if(controller.Device=="手柄"){
                float hw=Math.Min(560,viewport.width-pad*2);
                if(Button(new Rect(pad,viewport.height-50,hw,34),controller.PadOpenLabel+" · 面板",true))controller.Toggle(0,s,player.Id);
                Label(new Rect(pad,viewport.height-84,hw,0),"右摇杆 · 光标 / 呼出键长按 · 显隐",small);
            }else{
                float footerX=pad;
                string guide=Localization.Text("F7 图鉴"),options=Localization.Text("F6 设置"),hide=Localization.Text("F4 收起");
                float guideW=Math.Max(94,button.CalcSize(new GUIContent(guide)).x+8);
                float optionsW=Math.Max(94,button.CalcSize(new GUIContent(options)).x+8);
                float hideW=Math.Max(94,button.CalcSize(new GUIContent(hide)).x+8);
                if(Button(new Rect(footerX,viewport.height-50,guideW,34),guide,true))controller.Toggle(0,s,player.Id);footerX+=guideW+8;
                if(Button(new Rect(footerX,viewport.height-50,optionsW,34),options,true))controller.Toggle(2,s,player.Id);footerX+=optionsW+8;
                if(Button(new Rect(footerX,viewport.height-50,hideW,34),hide,true)){settings.Enabled.Value=false;controller.Close();}
            }
            if(controller.Actions.MessageUntil>Time.unscaledTime){float tw=Math.Min(680,viewport.width-36);var toast=new Rect((viewport.width-tw)/2,viewport.height-98,tw,38);Fill(toast,background);Label(new Rect(toast.x+8,toast.y+8,tw-16,0),controller.Actions.Message,small);}
        }
        private void DrawMap(Rect area,Snapshot s,PlayerInfo player,bool transparent=false,bool hud=false)
        {
            bool interactive=controller.Open?!hud&&controller.Tab==3:hud;
            if(interactive)controller.BeginMapDraw(player.Id);
            if(!transparent)Card(area);float x0=area.x+18,width=area.width-36,rowH=small.lineHeight+5;
            MapLabel(new Rect(x0,area.y+14,width-108,0),"岛屿地图 ⇄",title);
            string scope=Localization.Text(controller.MapNearby?"附近 →":"全岛 →");float scopeWidth=Math.Max(98,button.CalcSize(new GUIContent(scope)).x+8);
            if(Button(new Rect(area.x+area.width-scopeWidth-18,area.y+12,scopeWidth,34),scope,transparent))controller.MapNearby=!controller.MapNearby;
            MapLabel(new Rect(x0,area.y+50,width,0),"可招募 "+s.Beggars+" · 营地 "+s.Camps+(s.EnemiesAvailable?"":" · 敌情未知"),small);
            if(extentIsland!=s.Island){stableExtent.Reset();extentIsland=s.Island;}
            // 岛屿固定地点决定比例；移动对象与显示筛选不改变坐标系。
            var extent=stableExtent.Update(s.Points.Select(p=>p.X),s.Left,s.Right,Time.unscaledTime);
            if(extent==null){MapLabel(new Rect(x0,area.y+100,width,0),"地图加载中…",small);return;}
            float left=extent.Item1,right=extent.Item2,focusX=player.X;
            MapPoint focused=null;
            if((controller.MapFocus||controller.Open&&controller.Tab==3)&&controller.Owner==player.Id){var choices=controller.FilteredPoints(s,player);if(choices.Length>0){focused=choices[Math.Clamp(controller.Selected,0,choices.Length-1)];focusX=focused.X;}}
            if(controller.MapNearby&&right-left>260){left=Math.Max(extent.Item1,Math.Min(focusX-130,extent.Item2-260));right=Math.Min(extent.Item2,left+260);}
            float length=Math.Max(1,right-left),axisY=area.y+78+rowH*2+10;
            Fill(new Rect(x0,axisY,width,2),border);
            var points=s.Points.Where(p=>controller.PointVisible(p)&&p.X>=left&&p.X<=right&&(settings.FullMap.Value||s.ExploredRanges.Any(r=>p.X>=r.Item1&&p.X<=r.Item2)))
                .GroupBy(p=>p.Name+"|"+Math.Round(p.X)).Select(g=>g.First()).OrderBy(p=>p.X).ToArray();
            var pointGroups=points.GroupBy(p=>p.Name+"|"+(int)((p.X-left)/length*width/24)).ToArray();
            var counts=pointGroups.Select(g=>g.Count()).ToArray();
            points=pointGroups.Select(g=>g.First()).ToArray();
            var requests=new System.Collections.Generic.List<MapLabelRequest>();
            var labels=new string[points.Length];
            for(int i=0;i<points.Length;i++)
            {
                var point=points[i];labels[i]=MapText.Label(point,player.Id,counts[i],pointGroups[i].Sum(p=>Math.Max(0,p.CampPeople)));
                float measured=small.CalcSize(new GUIContent(Localization.Text(labels[i]))).x;
                requests.Add(new MapLabelRequest{Index=i,Anchor=(point.X-left)/length*width,Width=measured+4,
                    Priority=pointGroups[i].Any(p=>p==focused)?1000:Math.Abs(point.X-player.X)<35?200:point.CampPeople>=0||point.Name.Contains("城堡")?100:0});
            }
            var placements=MapLabelLayout.Arrange(requests,width,4);
            string tip=focused!=null?PointSummary(focused,player):"";
            foreach(var item in placements)
            {
                var point=points[item.Index];point.PlayerStates.TryGetValue(player.Id,out var status);
                var color=MarkerColor(point.Category);
                float px=Mathf.Round(x0+(point.X-left)/length*width);
                float ly=item.Row==0?axisY-rowH-10:item.Row==1?axisY+13:item.Row==2?axisY-rowH*2-10:axisY+rowH+13;
                var rect=new Rect(Mathf.Round(x0+item.Left),Mathf.Round(ly),item.Width,rowH);
                float anchor=Math.Clamp(px,rect.x+2,rect.xMax-2),connector=item.Row%2==0?rect.yMax-4:rect.y-2;
                Fill(new Rect(px-1,Math.Min(connector,axisY),2,Math.Abs(axisY-connector)),border);
                Fill(new Rect(Math.Min(px,anchor),connector,Math.Max(2,Math.Abs(px-anchor)),1),border);
                var oldColor=small.normal.textColor;small.normal.textColor=color;MapLabel(rect,labels[item.Index],small);small.normal.textColor=oldColor;
                Fill(new Rect(px-2,axisY-4,4,9),color);
                bool hovered=(controller.Device=="键鼠"||controller.PadPointerHover(player.Id))&&(rect.Contains(PointerPosition)||new Rect(px-5,axisY-5,10,14).Contains(PointerPosition));
                bool selected=focused!=null&&pointGroups[item.Index].Any(p=>p==focused);
                if(hovered||selected)
                {
                    Fill(new Rect(rect.x,rect.yMax-2,rect.width,1),gold);
                    if(status!=null&&status.HealthMax>0){Fill(new Rect(px-7,axisY+6,14,2),border);Fill(new Rect(px-7,axisY+6,14*Math.Clamp(status.Health/status.HealthMax,0,1),2),gold);}
                }
                if(hovered)
                {
                    tip=PointSummary(point,player);
                    if(interactive){if(controller.Device=="手柄")controller.PadMapHit(point,player.Id,rect,new Rect(px-5,axisY-5,10,14),guiOrigin);else controller.MapHit(point,player.Id);}
                }
                if(controller.Hold.Active&&controller.Hold.Owner==player.Id&&controller.Hold.Target==AdvisorController.PointKey(point)&&controller.Hold.CanTeleport&&!controller.Hold.Latched){ProgressRing(new Vector2(px,axisY-24),controller.Hold.Progress(Time.unscaledTime));tip="传送 · "+Math.Max(0,.8f-(Time.unscaledTime-controller.Hold.Started)).ToString("F1")+" 秒 · 松开取消";}
            }
            var placed=new System.Collections.Generic.HashSet<int>(placements.Select(p=>p.Index));
            for(int i=0;i<points.Length;i++)
            {
                if(placed.Contains(i))continue;
                var point=points[i];float px=Mathf.Round(x0+(point.X-left)/length*width);
                Fill(new Rect(px,axisY-3,1,6),muted);
                if((controller.Device=="键鼠"||controller.PadPointerHover(player.Id))&&new Rect(px-3,axisY-5,6,12).Contains(PointerPosition)){tip=PointSummary(point,player);if(interactive){if(controller.Device=="手柄")controller.PadMapHit(point,player.Id,new Rect(px-3,axisY-5,6,12),new Rect(px-3,axisY-5,6,12),guiOrigin);else controller.MapHit(point,player.Id);}}
            }
            var groups=EnemyClusters.Group(s.Enemies.Where(e=>settings.MapEnemies.Value&&(!controller.MapNearby||e.X>=left&&e.X<=right)&&(settings.FullMap.Value||s.ExploredRanges.Any(r=>e.X>=r.Item1&&e.X<=r.Item2))),length*34/width);
            var ids=new System.Collections.Generic.HashSet<int>(groups.Select(g=>g.Id));
            foreach(var old in enemyPositions.Keys.Where(id=>!ids.Contains(id)).ToArray())enemyPositions.Remove(old);
            float occupied=float.NegativeInfinity;
            foreach(var group in groups)
            {
                float wx=group.X,now=Time.unscaledTime;
                if(enemyPositions.TryGetValue(group.Id,out var previous))wx=Mathf.Lerp(previous.Item1,group.X,Math.Clamp((now-previous.Item2)*14,0,1));
                if(Event.current.type==EventType.Repaint)enemyPositions[group.Id]=System.Tuple.Create(wx,now);
                float actual=Mathf.Round(Math.Clamp(x0+(wx-left)/length*width,x0+18,x0+width-18));
                float px=Math.Max(actual,occupied+38);px=Math.Min(x0+width-18,px);occupied=px;
                float ey=axisY+rowH*2+38;
                EnemyIcon(px,ey,group.Kind);var alignment=small.alignment;small.alignment=TextAnchor.UpperCenter;MapLabel(new Rect(px-18,ey+24,36,0),group.Count.ToString(),small);small.alignment=alignment;
                if(px!=actual){Fill(new Rect(actual,ey-5,1,3),muted);Fill(new Rect(Math.Min(actual,px),ey-3,Math.Abs(px-actual),1),border);}
                if(new Rect(px-18,ey-3,36,52).Contains(Event.current.mousePosition))tip=group.Kind+" · "+group.Count+" 只";
            }
            foreach(var p in s.Players.Where(p=>p.X>=left&&p.X<=right))
            {
                float px=Mathf.Round(x0+(p.X-left)/length*width);
                Fill(new Rect(px-5,axisY-9,10,13),p.Id==player.Id?gold:green);
                Fill(new Rect(px-8,axisY-13,16,4),p.Id==player.Id?gold:green);
            }
            if(tip.Length==0&&placements.Count<points.Length)tip=(points.Length-placements.Count)+" 处名称密集 · 切换附近查看";
            if(tip.Length>0)
            {
                float tipY=area.y+area.height-small.lineHeight-12;
                if(!transparent)Fill(new Rect(x0,tipY-4,width,small.lineHeight+8),background);
                MapLabel(new Rect(x0,tipY,width,small.lineHeight),tip,small);
            }
        }
        private void MapLabel(Rect area,string content,GUIStyle style)
        {
            var original=style.normal.textColor;
            style.normal.textColor=new Color(0,0,0,.95f);
            Label(new Rect(area.x+1,area.y+1,area.width,area.height),content,style);
            style.normal.textColor=original;
            Label(area,content,style);
        }
        private void Landmark(float x,float y,string category,Color color)
        {
            if(category=="人口")
            {for(int row=0;row<10;row++)Fill(new Rect(x-row,y+row,row*2+1,1),color);Fill(new Rect(x-10,y+10,21,9),color);Fill(new Rect(x-3,y+12,6,7),background);}
            else if(category=="防御"||category=="建设")
            {Fill(new Rect(x-8,y+4,16,15),color);for(int i=0;i<3;i++)Fill(new Rect(x-8+i*6,y,4,7),color);Fill(new Rect(x-2,y+11,4,8),background);}
            else if(category=="航行")
            {Fill(new Rect(x-11,y+12,22,5),color);Fill(new Rect(x,y,2,15),color);Fill(new Rect(x+3,y+2,8,8),color);}
            else{Fill(new Rect(x-7,y+4,14,14),color);Fill(new Rect(x-3,y,6,4),color);}
        }
        private void HudIcon(float x,float y,string kind)
        {
            string[] pattern=kind switch{
                "coin"=>new[]{"01110","11111","11011","11111","01110"},
                "gem"=>new[]{"01110","11111","11111","01110","00100"},
                "hammer"=>new[]{"11110","11110","00100","00100","00100"},
                "bow"=>new[]{"01100","01010","01001","01010","01100"},
                "shield"=>new[]{"11111","10101","10101","01110","00100"},
                "wheat"=>new[]{"10101","01110","10101","01110","00100"},
                "person"=>new[]{"01110","01110","00100","11111","01010"},
                "horse"=>new[]{"00011","00111","11110","11110","10010"},
                "stamina"=>new[]{"00110","01100","11111","00110","01100"},
                "tower"=>new[]{"10101","11111","11111","11011","11011"},
                _=>new[]{"01110","00100","01110","11111","11111"}
            };
            var color=kind=="gem"?new Color(.77f,.56f,.86f):kind=="wheat"?green:gold;
            for(int row=0;row<pattern.Length;row++)for(int col=0;col<pattern[row].Length;col++)if(pattern[row][col]=='1'){
                Fill(new Rect(x+col*3+1,y+row*3+1,3,3),new Color(0,0,0,.85f));Fill(new Rect(x+col*3,y+row*3,3,3),color);
            }
        }
        private void EnemyIcon(float x,float y,string kind)
        {
            var red=new Color(.94f,.42f,.36f);
            if(kind=="飞行怪"){Fill(new Rect(x-14,y+5,28,3),red);Fill(new Rect(x-7,y+1,14,14),red);}
            else{float w=kind=="贪婪怪"?14:20;Fill(new Rect(x-w/2,y+4,w,14),red);Fill(new Rect(x-w/2-2,y,4,8),red);Fill(new Rect(x+w/2-2,y,4,8),red);}
            Fill(new Rect(x-4,y+8,2,3),background);Fill(new Rect(x+2,y+8,2,3),background);
        }
        private void DrawPanel(Rect viewport,Snapshot state,PlayerInfo player)
        {
            if(controller.Tab!=0)controller.SearchFocused=false;
            float pw=Math.Min(1040,viewport.width-40),ph=Math.Min(720,viewport.height-40);var panel=new Rect(Mathf.Round((viewport.width-pw)/2),Mathf.Round((viewport.height-ph)/2),pw,ph);
            if(!settings.InterfaceTransparent.Value)Fill(viewport,new Color(0,0,0,.38f));Card(panel,settings.InterfaceTransparent.Value);guiOrigin+=new Vector2(panel.x,panel.y);GUI.BeginGroup(panel);
            float w=panel.width,h=panel.height,line=Math.Max(24,text.lineHeight+7);
            var tabs=new[]{"图鉴","兴趣点","设置","地图","岛屿记录","资源"};for(int i=0;i<tabs.Length;i++){var rect=new Rect(12+i*(w-115)/tabs.Length,10,(w-131)/tabs.Length,line+6);if(Button(rect,(controller.Tab==i?"● ":"")+tabs[i])){controller.ChangeTab(i-controller.Tab);}}
            if(Button(new Rect(w-91,10,79,line+6),Localization.English?"Close":"关闭"))controller.Close();
            string help=controller.Device=="手柄"?controller.PadOpenLabel+" 短按面板／长按显隐 · LB/RB 页签 · LT/RT "+(controller.PadRegion==3&&controller.DetailPages>1?"详情":"列表")+"翻页 · 方向选择 · 确认／返回":"上下选择 · 左右分类 · Enter 选择 · Tab 切页 · Esc 关闭";
            float helpH=small.CalcHeight(new GUIContent(Localization.Text(help)),w-28),helpY=h-helpH-14;
            float top=line+42,bottom=helpY-14;
            if(controller.Tab==2)DrawSettings(w,top,bottom);
            else if(controller.Tab==0)DrawCatalog(w,top,bottom,state);
            else if(controller.Tab==1)DrawPoints(w,top,bottom,state,player);
            else if(controller.Tab==3)DrawMapPage(w,top,bottom,state,player);
            else if(controller.Tab==4)DrawJournal(w,top,bottom,state);
            else DrawResources(w,top,bottom,state);

            Label(new Rect(14,helpY,w-28,helpH),help,small);
            GUI.EndGroup();guiOrigin-=new Vector2(panel.x,panel.y);
        }
        private void ProgressRing(Vector2 center,float progress)
        {
            for(int i=0;i<32;i++){float angle=i*Mathf.PI*2/32-Mathf.PI/2;Fill(new Rect(center.x+Mathf.Cos(angle)*13-2,center.y+Mathf.Sin(angle)*13-2,4,4),i<progress*32?gold:border);}
        }
        private void DrawSettings(float w,float top,float bottom)
        {
            float rail=Localization.English?210:180,rowH=Math.Max(42,text.lineHeight*2+8);
            bool priorEnabled=GUI.enabled;if(controller.SettingsDropdown)GUI.enabled=false;
            for(int i=0;i<OptionGroups.Names.Length;i++)if(Button(new Rect(14,top+i*(rowH+6),rail-14,rowH),(controller.Section==i?"● ":"")+OptionGroups.Names[i])){controller.Section=i;controller.Focus=0;controller.SettingsDropdown=false;}
            var items=OptionGroups.Items[controller.Section];float x=rail+14,rw=w-x-14;
            string note="即时保存 · 玩法辅助默认关闭 · 单机与联机均开放";float noteH=small.CalcHeight(new GUIContent(Localization.Text(note)),rw),py=bottom-noteH-rowH-12;
            int visible=Math.Max(1,(int)((py-top-8)/(rowH+5))),page=controller.Focus/visible,start=page*visible;controller.VisibleRows=visible;
            for(int i=start;i<Math.Min(start+visible,items.Length);i++){
                var item=items[i];int value=Math.Clamp(controller.OptionValue(item.Id),0,item.Choices.Length-1);
                if(Button(new Rect(x,top+(i-start)*(rowH+5),rw,rowH),(controller.Focus==i?"▶ ":"")+item.Name+"    "+item.Choices[value]+"  ▾")){controller.Focus=i;controller.SettingsDropdown=false;controller.Activate();}
            }

            float pageButtonW=Math.Max(100,Math.Max(button.CalcSize(new GUIContent(Localization.Text("上一页"))).x,button.CalcSize(new GUIContent(Localization.Text("下一页"))).x)+8);
            if(Button(new Rect(x,py,pageButtonW,rowH),"上一页")){controller.FlipPage(-1,controller.CurrentSnapshot);}
            Label(new Rect(x+pageButtonW+15,py,rw-pageButtonW*2-30,rowH),(page+1)+" / "+((items.Length+visible-1)/visible),small);
            if(Button(new Rect(w-14-pageButtonW,py,pageButtonW,rowH),"下一页")){controller.FlipPage(1,controller.CurrentSnapshot);}
            Label(new Rect(x,py+rowH+8,rw,noteH),note,small);
            GUI.enabled=priorEnabled;
            if(controller.SettingsDropdown){
                var item=items[controller.Focus];int columns=item.Choices.Length*(rowH+3)+16>bottom-top?2:1;int choiceRows=(item.Choices.Length+columns-1)/columns;float dh=choiceRows*(rowH+3)+16;var rect=new Rect(w-Math.Min(300,rw)-20,Math.Clamp(top+(controller.Focus-start)*(rowH+5)+rowH,top,Math.Max(top,bottom-dh)),Math.Min(300,rw),dh);Card(rect);
                for(int i=0;i<item.Choices.Length;i++)if(Button(new Rect(rect.x+8+(i/choiceRows)*(rect.width-16)/columns,rect.y+8+(i%choiceRows)*(rowH+3),(rect.width-16)/columns,rowH),(controller.Choice==i?"▶ ":"")+item.Choices[i])){controller.Choice=i;controller.SetOption(item.Id,i);controller.SettingsDropdown=false;}
            }
        }
        private void DrawResources(float w,float top,float bottom,Snapshot s)
        {
            float rowH=Math.Max(42,text.lineHeight+16);
            var p=s.Players.FirstOrDefault(t=>t.Id==controller.ResourcePlayer&&t.Local)??s.Players.FirstOrDefault(t=>t.Local);
            if(p==null)return;controller.ResourcePlayer=p.Id;
            if(!s.ResourceTypes.Contains(controller.ResourceType)&&s.ResourceTypes.Count>0)controller.ResourceType=s.ResourceTypes[0];
            int balance=p.Resources.TryGetValue(controller.ResourceType,out var b)?b:0;
            string[] rows={"玩家：P"+(p.Id+1)+"  ▾","资源："+ResourceRules.Name(controller.ResourceType)+" · 当前 "+balance+"  ▾","数量："+(controller.ResourceEditing?controller.ResourceText+" ▏":settings.ResourceAmount.Value.ToString()),"快捷数量："+settings.ResourceAmount.Value+"  ▾","添加 "+settings.ResourceAmount.Value+" "+ResourceRules.Name(controller.ResourceType),"清空范围："+new[]{"仅金币","当前资源","全部钱袋资源"}[controller.ClearMode]+"  ▾","清空所选资源"};
            bool priorEnabled=GUI.enabled;if(controller.ResourceDropdown)GUI.enabled=false;
            for(int i=0;i<rows.Length;i++)if(Button(new Rect(20,top+i*(rowH+9),w-40,rowH),(controller.Focus==i?"▶ ":"")+rows[i])){controller.Focus=i;controller.Activate();}
            GUI.enabled=priorEnabled;
            Label(new Rect(20,top+rows.Length*(rowH+9)+8,w-40,0),"上下选择，左右调整；数量可直接输入，范围 1–10000。只显示当前主题支持的资源。无限钱袋只扩充容量，需在资源辅助设置中开启。",small);
            if(controller.ResourceDropdown){var choices=controller.ResourceChoices();float dh=choices.Length*(rowH+3)+16;var rect=new Rect(w-340,Math.Clamp(top+controller.Focus*(rowH+9)+rowH,top,Math.Max(top,bottom-dh)),320,dh);Card(rect);for(int i=0;i<choices.Length;i++)if(Button(new Rect(rect.x+8,rect.y+8+i*(rowH+3),rect.width-16,rowH),(controller.Choice==i?"▶ ":"")+choices[i]))controller.SelectResourceChoice(i);}

        }
        private void DrawJournal(float w,float top,float bottom,Snapshot s)
        {
            var records=controller.Journal.Records.OrderBy(r=>r.Island).ToArray();
            if(records.Length==0){Label(new Rect(20,top,w-40,0),settings.Journal.Value?"进入岛屿后自动记录已读取的情报。":"岛屿进度记录已关闭，可在设置中开启。",text);return;}
            controller.Selected=Math.Clamp(controller.Selected,0,records.Length-1);float rowH=Math.Max(42,text.lineHeight+16),rail=Math.Min(240,w*.3f);
            int visible=Math.Max(1,(int)((bottom-top-rowH)/(rowH+6))),page=controller.Selected/visible,start=page*visible;controller.VisibleRows=visible;
            for(int i=start;i<Math.Min(start+visible,records.Length);i++)if(Button(new Rect(14,top+(i-start)*(rowH+6),rail-20,rowH),(controller.Selected==i?"▶ ":"")+(Localization.English?"Island "+records[i].Island+" · Day "+records[i].Day:"岛 "+records[i].Island+" · 第 "+records[i].Day+" 天")))controller.Selected=i;
            Pagination(14,bottom-rowH,rail-20,rowH,page,(records.Length+visible-1)/visible,visible,records.Length);
            var record=records[controller.Selected];float x=rail+12,dw=w-x-20,y=top;
            y+=Row(x,y,dw,(Localization.English?"Island "+record.Island+" · Last observed on day "+record.Day:"岛 "+record.Island+" · 最近记录第 "+record.Day+" 天"),title);
            y+=Row(x,y,dw,"这是最近观察记录，离岛后不会实时更新。",small);
            var pages=SplitPages(string.Join("\n",record.Lines??Array.Empty<string>()),dw,Math.Max(rowH,bottom-y-rowH-8));controller.DetailPages=pages.Length;controller.DetailPage%=pages.Length;
            Label(new Rect(x,y,dw,0),pages[controller.DetailPage],text);
            if(pages.Length>1&&Button(new Rect(x,bottom-rowH,dw,rowH),"详情 "+(controller.DetailPage+1)+" / "+pages.Length))controller.DetailPage++;
        }
        private void DrawCatalog(float w,float top,float bottom,Snapshot state)
        {
            float line=Math.Max(36,text.lineHeight+10);
            var searchRect=new Rect(14,top,w*.60f-18,line+5);
            var current=Event.current;
            if(current.type==EventType.MouseDown&&!searchRect.Contains(current.mousePosition))controller.SearchFocused=false;
            string searchLabel=controller.Search.Length==0?"点击搜索 · 中文 / 英文":controller.Search;
            if(controller.SearchFocused)searchLabel+=" ▏";
            if(controller.Device=="手柄"&&controller.PadRegion==2)searchLabel="▶ "+searchLabel;
            if(Button(searchRect,searchLabel)){if(controller.Device=="手柄"){controller.SearchEditing=true;controller.SearchFocused=false;}else controller.SearchFocused=true;}
            int ci=Array.IndexOf(controller.Categories,controller.Category);
            if(Button(new Rect(w*.60f+4,top,w*.40f-18,line+5),(controller.Device=="手柄"&&controller.PadRegion==1?"▶ ":"")+"类别："+controller.Category)){controller.Category=controller.Categories[(ci+1)%controller.Categories.Length];controller.Selected=0;}
            top+=line+12;var entries=controller.FilteredEntries();
            if(controller.SearchEditing){DrawSearchKeyboard(w,top,bottom);return;}
            if(entries.Length==0){Label(new Rect(14,top,w-28,0),"没有匹配条目。清空搜索或切换类别。",text);return;}
            controller.Selected=Math.Clamp(controller.Selected,0,entries.Length-1);
            float listW=w*.38f;int visible=Math.Max(1,(int)((bottom-top-line-10)/(line+5)));int page=controller.Selected/visible;int start=page*visible;
            controller.VisibleRows=visible;
            for(int i=start;i<Math.Min(start+visible,entries.Length);i++){var rect=new Rect(14,top+(i-start)*(line+5),listW-20,line+2);if(Button(rect,(controller.Selected==i?"▶ ":"")+(settings.Icons.Value?"      ":"")+entries[i].Name))controller.Selected=i;if(settings.Icons.Value)Landmark(rect.x+25,rect.y+8,entries[i].Category,MarkerColor(entries[i].Category));}
            Pagination(14,bottom-line-4,listW-20,line,page,Math.Max(1,(entries.Length+visible-1)/visible),visible,entries.Length);
            var selected=entries[controller.Selected];float dx=listW+8,dw=w-dx-14,y=top;
            if(lastCatalogKey!=selected.Key){lastCatalogKey=selected.Key;controller.DetailPage=0;}
            y+=Row(dx,y,dw,(controller.Device=="手柄"&&controller.PadRegion==3?"▶ ":"")+selected.Name,title);y+=Row(dx,y,dw,"类别："+selected.Category,small);
            string body=selected.Description+"\n\n建议："+selected.Advice;
            if(state.Counts.TryGetValue(selected.Key,out var count))body+="\n\n本岛同类交互点："+count;
            body+="\n\n费用、等级和锁定原因请靠近对象查看。不同主题以当前游戏数据为准。";
            var pages=SplitPages(body,dw,Math.Max(line,bottom-y-line-10));
            controller.DetailPages=pages.Length;controller.DetailPage%=pages.Length;
            Label(new Rect(dx,y,dw,0),pages[controller.DetailPage],text);
            if(pages.Length>1&&Button(new Rect(dx,bottom-line,dw,line),"详情 "+(controller.DetailPage+1)+" / "+pages.Length+" · 下一页"))controller.DetailPage++;
        }
        private void Pagination(float x,float y,float width,float height,int page,int count,int perPage,int total)
        {if(Button(new Rect(x,y,width*.28f,height),"←"))controller.Selected=Math.Max(0,(page-1)*perPage);Label(new Rect(x+width*.30f,y,width*.36f,height),(page+1)+" / "+count,small);if(Button(new Rect(x+width*.70f,y,width*.30f,height),"→"))controller.Selected=Math.Min(total-1,(page+1)*perPage);}
        private void DrawPoints(float w,float top,float bottom,Snapshot state,PlayerInfo player)
        {
            float line=Math.Max(36,text.lineHeight+10);var points=controller.FilteredPoints(state,player);
            int ci=Array.IndexOf(controller.Categories,controller.Category);
            if(Button(new Rect(14,top,w-28,line),"筛选："+controller.Category)){controller.Category=controller.Categories[(ci+1)%controller.Categories.Length];controller.Selected=0;}
            top+=line+8;if(points.Length==0){Label(new Rect(14,top,w-28,0),"当前筛选没有兴趣点。",text);return;}
            controller.Selected=Math.Clamp(controller.Selected,0,points.Length-1);int visible=Math.Max(1,(int)((bottom-top-line-10)/(line+5)));int page=controller.Selected/visible,start=page*visible;
            controller.VisibleRows=visible;
            for(int i=start;i<Math.Min(start+visible,points.Length);i++){var p=points[i];var rect=new Rect(14,top+(i-start)*(line+5),w-28,line+2);if(Button(rect,(controller.Selected==i?"▶ ":"")+p.Name+" · "+p.Category))controller.Selected=i;}
            Pagination(14,bottom-line-4,w-28,line,page,Math.Max(1,(points.Length+visible-1)/visible),visible,points.Length);
        }
        private static string On(bool value)=>value?"开启":"关闭";
        private void DrawPointer(Vector2 position)
        {
            // 鼠标与手柄共用高对比指针，尖端保持命中坐标。
            float x=Mathf.Round(position.x),y=Mathf.Round(position.y),scale=Math.Clamp(Screen.height/1080f,1f,1.5f);
            var outline=new Color(.015f,.02f,.025f,1);var highlight=new Color(1f,.97f,.72f,1);var accent=new Color(.20f,.90f,1f,1);
            for(int row=0;row<24;row++){
                float width=Math.Max(2,row*.62f)*scale;
                Fill(new Rect(x-2*scale,y+(row-2)*scale,width+4*scale,5*scale),outline);
            }
            Fill(new Rect(x+8*scale,y+19*scale,8*scale,13*scale),outline);
            for(int row=0;row<23;row++)Fill(new Rect(x,y+row*scale,Math.Max(2,row*.62f)*scale,scale),highlight);
            Fill(new Rect(x+10*scale,y+21*scale,4*scale,9*scale),accent);
            Fill(new Rect(x,y,3*scale,12*scale),accent);
        }
        private string PointSummary(MapPoint point,PlayerInfo player)=>MapText.Detail(point,player.Id);
        private void DrawMapPage(float w,float top,float bottom,Snapshot state,PlayerInfo player)
        {
            float mapH=Math.Min(Math.Max(350,small.lineHeight*7+196),bottom-top-115);
            if(mapH>=Math.Max(350,small.lineHeight*7+196)){DrawMap(new Rect(14,top,w-28,mapH),state,player);top+=mapH+16;}
            var points=controller.FilteredPoints(state,player);
            if(points.Length==0){Label(new Rect(14,top,w-28,0),"当前筛选没有地点。切换兴趣点分类或地图情报范围。",text);return;}
            controller.Selected=Math.Clamp(controller.Selected,0,points.Length-1);
            var point=points[controller.Selected];
            float summaryH=text.CalcHeight(new GUIContent(Localization.Text(PointSummary(point,player))),w-28);
            Label(new Rect(14,top,w-28,0),PointSummary(point,player),text);top+=summaryH+10;
            if(Button(new Rect(14,top,(w-36)/2,36),"← 上一地点"))controller.Selected=Math.Max(0,controller.Selected-1);
            if(Button(new Rect(w/2+4,top,(w-36)/2,36),"下一地点 →"))controller.Selected=Math.Min(points.Length-1,controller.Selected+1);
            controller.VisibleRows=5;
        }
        private void DrawSearchKeyboard(float w,float top,float bottom)
        {
            Label(new Rect(14,top,w-28,0),"手柄搜索 · 输入英文关键词；中文条目也可按类别浏览。",small);
            top+=small.lineHeight+16;
            float cellW=(w-58)/6,cellH=Math.Min(42,(bottom-top)/7-5);
            for(int i=0;i<controller.SearchKeys.Length;i++)
            {
                var rect=new Rect(14+(i%6)*(cellW+6),top+(i/6)*(cellH+5),cellW,cellH);
                if(Button(rect,(controller.SearchCursor==i?"▶ ":"")+controller.SearchKeys[i]))controller.SearchKey(i);
            }
        }
        private string[] SplitPages(string content,float width,float height)
        {
            content=Localization.Text(content);
            var pages=new System.Collections.Generic.List<string>();int start=0;
            while(start<content.Length)
            {
                int count=1;
                while(start+count<content.Length&&text.CalcHeight(new GUIContent(content.Substring(start,count+1)),width)<=height)count++;
                if(start+count<content.Length&&count>1&&!char.IsWhiteSpace(content[start+count])&&!char.IsWhiteSpace(content[start+count-1])){int space=content.LastIndexOf(' ',start+count-1,count);if(space>=start)count=space-start+1;}
                pages.Add(content.Substring(start,count));start+=count;
            }
            return pages.Count>0?pages.ToArray():new[]{""};
        }
        private static string Season(string season){switch(season){case "Spring":return "春季";case "Summer":return "夏季";case "Autumn":case "Fall":return "秋季";case "Winter":return "冬季";default:return season;}}
        private Color MarkerColor(string category)=>category=="威胁"?new Color(.95f,.42f,.36f):category=="经济"?green:category=="科技"?gold:category=="防御"?new Color(.52f,.69f,.85f):category=="人口"?new Color(.74f,.61f,.84f):muted;
    }
}
