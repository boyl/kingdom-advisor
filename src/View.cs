using System;
using System.Linq;
using UnityEngine;
using BepInEx;

namespace KingdomAdvisor
{
    internal sealed class AdvisorView
    {
        private readonly Settings settings;private readonly AdvisorController controller;
        private GUIStyle text,title,small,dayStyle,button,fillStyle,hudValue;
        private Font font;private int lastSize;private bool stylesReady;
        private bool opacityDragging;private int activePanelOpacity=-1;
        private string lastCatalogKey="",lastLocationKey="";
        private readonly StableMapExtent stableExtent=new StableMapExtent();private int extentIsland=-1;
        private LayoutPositions positions;
        private string savedPositions,dragKey="";
        private Vector2 dragOffset,guiOrigin;private int guiPlayer;
        private Vector2 PointerPosition=>controller.Device=="手柄"?controller.PadPointer(guiPlayer)-guiOrigin:Event.current.mousePosition;

        private readonly System.Collections.Generic.Dictionary<int,System.Tuple<float,float>> enemyPositions=new System.Collections.Generic.Dictionary<int,System.Tuple<float,float>>();
        private readonly System.Collections.Generic.Dictionary<string,int> hudPages=new System.Collections.Generic.Dictionary<string,int>();
        private readonly System.Collections.Generic.Dictionary<string,string> hudTargets=new System.Collections.Generic.Dictionary<string,string>();
        public string FontEvidence="not initialised";
        internal TargetInfo PreviewTarget;
        internal string PreviewHudDevice;
        internal bool PreviewHudLastPage;
        internal int? PreviewHudColumns;
        internal int? PreviewHudOpacity;
        private Rect hudAdvisor,hudDetail,hudAlert;
        private float hudFooterTop,hudHintHeight,hudLegacyBottom;
        private int hudRowCount,hudPageCount,hudDetailPages,hudColumns,hudOpacity;
        private bool hudPad;
        private string hudDay="";
        internal string[] HudEvidence()=>new[]{"version="+Plugin.Version,"device="+(hudPad?"pad":"keyboard"),"font="+lastSize+" language="+(Localization.English?"en":"zh-CN"),"advisor="+hudAdvisor+" bottom="+hudAdvisor.yMax+" legacyDefaultBottom="+hudLegacyBottom,"detail="+hudDetail+" pages="+hudDetailPages,"alert="+hudAlert,"footerTop="+hudFooterTop+" hintHeight="+hudHintHeight,"rows="+hudRowCount+" pages="+hudPageCount+" columns="+hudColumns,"day="+hudDay.Replace("\n","\\n"),"opacity="+hudOpacity+" identityInHeader=True rowGap="+HudTextLayout.SingleRowGap};
        private readonly Color background=new Color(36/255f,35/255f,42/255f,.94f);
        private readonly Color gold=new Color(195/255f,166/255f,106/255f);
        private readonly Color muted=new Color(182/255f,176/255f,162/255f);
        private readonly Color border=new Color(87/255f,82/255f,75/255f,.5f);
        private readonly Color bodyText=new Color(221/255f,216/255f,203/255f);
        private readonly Color hoverFill=new Color(52/255f,50/255f,59/255f,.65f);
        private readonly Color selectedFill=new Color(69/255f,65/255f,55/255f,.8f);
        private readonly Color green=new Color(.49f,.79f,.67f);
        public AdvisorView(Settings config,AdvisorController input){settings=config;controller=input;}
        private void LoadPositions()
        {
            if(savedPositions!=settings.Layout.Value)
            {
                savedPositions=settings.Layout.Value;
                try{positions=new LayoutPositions(savedPositions);}
                catch(FormatException ex){Plugin.Instance.Log.LogWarning("Invalid saved layout: "+ex.Message);positions=new LayoutPositions("");}
            }
            if(positions==null)positions=new LayoutPositions("");
        }
        private static HudRect Bounds(Rect rect)=>new HudRect(rect.x,rect.y,rect.width,rect.height);
        private static Rect Rectangle(HudRect bounds)=>new Rect(bounds.X,bounds.Y,bounds.Width,bounds.Height);
        private static Rect MainPanelRect(Rect viewport)=>Rectangle(HudTextLayout.MainPanel(viewport.width,viewport.height));
        private Rect Place(string key,Rect rect,Rect viewport,float handleWidth=-1)
        {
            LoadPositions();
            float maxX=Math.Max(0,viewport.width-rect.width-8),maxY=Math.Max(0,viewport.height-rect.height-8);
            var saved=positions.Get(key);if(saved!=null){rect.x=saved.Item1*maxX;rect.y=saved.Item2*maxY;}
            rect.x=Math.Clamp(rect.x,8,Math.Max(8,maxX));rect.y=Math.Clamp(rect.y,8,Math.Max(8,maxY));
            var current=Event.current;
            if(!controller.Open&&current.type==EventType.MouseDown&&current.button==0&&new Rect(rect.x,rect.y,handleWidth>0?handleWidth:rect.width,Math.Min(38,rect.height)).Contains(current.mousePosition))
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
            text=new GUIStyle(GUI.skin.label){font=font,fontSize=size,wordWrap=true,richText=false,padding=new RectOffset(0,0,0,0)};text.normal.textColor=bodyText;
            title=new GUIStyle(text){fontSize=size+4};title.normal.textColor=gold;
            small=new GUIStyle(text){fontSize=Math.Max(14,size-2)};small.normal.textColor=muted;
            dayStyle=new GUIStyle(small){wordWrap=false};
            hudValue=new GUIStyle(small){alignment=TextAnchor.UpperRight};hudValue.normal.textColor=bodyText;
            button=new GUIStyle(text){alignment=TextAnchor.MiddleLeft,padding=new RectOffset(12,12,4,4)};
            fillStyle=new GUIStyle();fillStyle.normal.background=Texture2D.whiteTexture;
            var measure=text.CalcSize(new GUIContent("王国顾问 · 升级条件"));
            FontEvidence=(font?font.name:"missing")+" sampleWidth="+measure.x+" lineHeight="+text.lineHeight;
            Plugin.Instance.Log.LogInfo("Overlay font: "+FontEvidence);
            lastSize=size;stylesReady=true;
        }
        private void Fill(Rect rect,Color color){var old=GUI.color;GUI.color=color;GUI.Box(rect,GUIContent.none,fillStyle);GUI.color=old;}
        private void PanelFill(Rect rect,Color color){color.a=PanelAppearance.Alpha(color.a,activePanelOpacity);Fill(rect,color);}
        private void Card(Rect rect,bool transparent=false,bool decorated=false)
        {
            if(transparent||activePanelOpacity==0)return;
            // 底色保持单层；细边与外侧阴影不叠在内容下面。
            var fill=background;if(activePanelOpacity>=0)fill.a=activePanelOpacity/100f;
            Fill(new Rect(rect.x+1,rect.y+1,rect.width-2,rect.height-2),fill);
            PanelFill(new Rect(rect.x,rect.y,rect.width,1),border);
            PanelFill(new Rect(rect.x,rect.yMax-1,rect.width,1),border);
            PanelFill(new Rect(rect.x,rect.y+1,1,rect.height-2),border);
            PanelFill(new Rect(rect.xMax-1,rect.y+1,1,rect.height-2),border);
            if(decorated){
                PanelFill(new Rect(rect.x+2,rect.yMax,rect.width,2),new Color(0,0,0,.12f));
                PanelFill(new Rect(rect.xMax,rect.y+2,2,rect.height-2),new Color(0,0,0,.12f));
                var accent=gold;accent.a=.55f;
                PanelFill(new Rect(rect.x+16,rect.y,Math.Min(40,rect.width-32),1),accent);
            }
        }
        private bool HudCard(Rect rect,OptionId opacity,bool transparent)
        {
            activePanelOpacity=PreviewHudOpacity??settings.PanelOpacities[opacity].Value;
            bool hidden=PreviewHudOpacity.HasValue?activePanelOpacity==0:transparent||activePanelOpacity==0;
            Card(rect,hidden,true);return !hidden;
        }
        private bool Button(Rect rect,string label,bool transparent=false,bool selectedOverride=false)
        {
            label=Localization.Text(label);
            if(Event.current.type==EventType.MouseDown&&rect.Contains(Event.current.mousePosition))controller.Device="键鼠";
            bool selected=selectedOverride||label.StartsWith("▶")||label.StartsWith("●");
            bool hover=(controller.Device!="手柄"||controller.PadPointerHover(guiPlayer))&&rect.Contains(PointerPosition);

            if(!transparent&&(selected||hover))PanelFill(rect,selected?selectedFill:hoverFill);
            if(selected)Fill(new Rect(rect.x,rect.y+4,2,Math.Max(1,rect.height-8)),gold);
            int originalSize=button.fontSize;
            while(button.fontSize>14&&button.CalcHeight(new GUIContent(label),rect.width)>rect.height)button.fontSize--;
            bool clicked=GUI.Button(rect,label,button)||(GUI.enabled&&controller.PointerButton(rect,guiPlayer,guiOrigin));button.fontSize=originalSize;return clicked;
        }
        private float Label(Rect area,string content,GUIStyle style)
        {content=Localization.Text(content);float h=style.CalcHeight(new GUIContent(content),area.width);var color=style.normal.textColor;style.normal.textColor=new Color(0,0,0,.85f);GUI.Label(new Rect(area.x+1,area.y+1,area.width,h),content,style);style.normal.textColor=color;GUI.Label(new Rect(area.x,area.y,area.width,h),content,style);return h;}
        private float Row(float x,float y,float width,string content,GUIStyle style)=>Label(new Rect(x,y,width,0),content,style)+4;
        private float HudHeight(string content,float width,GUIStyle style)=>style.CalcHeight(new GUIContent(content),width);
        // 仅用于字体行高测量，包含拉丁字母及 CJK 字形，不是界面文案。
        private const string HudLineProbe="Ag\u738b";
        private void HudLabel(Rect area,string content,GUIStyle style)
        {
            var color=style.normal.textColor;style.normal.textColor=new Color(0,0,0,.85f);
            GUI.Label(new Rect(area.x+1,area.y+1,area.width,area.height),content,style);
            style.normal.textColor=color;GUI.Label(area,content,style);
        }
        private HudTextPage[] HudPages(string key,string target,System.Collections.Generic.IEnumerable<HudTextRow> rows,float width,float height,float pagerHeight,System.Collections.Generic.IEnumerable<HudTextRow> secondColumn=null,float rowGap=HudTextLayout.RowGap)
        {
            if(!hudTargets.TryGetValue(key,out var previous)||previous!=target){hudTargets[key]=target;hudPages[key]=0;}
            float Measure(string value)=>HudHeight(value,width,small);
            var pages=secondColumn==null?HudTextLayout.Paged(rows,height,pagerHeight,HudHeight(HudLineProbe,width,small),Measure,rowGap):HudTextLayout.ColumnPages(rows,secondColumn,height,pagerHeight,HudHeight(HudLineProbe,width,small),Measure);
            hudPages[key]=Math.Clamp(hudPages.TryGetValue(key,out int page)?page:0,0,Math.Max(0,pages.Length-1));
            return pages;
        }
        private void DrawCompactHud(Rect viewport,Snapshot s,PlayerInfo player)
        {
            // 只有连一行正文及翻页都放不下时才降级；提示独立使用小字，不缩小实际正文。
            var hintStyle=new GUIStyle(small){fontSize=14};
            float width=viewport.width-24;
            string hint=Localization.Text("显示区域过小，请增大窗口或减小界面字号。");
            float hintH=HudHeight(hint,width,hintStyle);
            if(hintH<=viewport.height-24)HudLabel(new Rect(12,12,width,hintH),hint,hintStyle);
            string open=Localization.Text(controller.Device=="手柄"?controller.PadOpenLabel+" · 面板":"F7 图鉴");
            var entryStyle=new GUIStyle(button){fontSize=14};
            float entryH=Math.Max(30,HudHeight(open,width,entryStyle));
            if(hintH+8+entryH<=viewport.height-24&&Button(new Rect(12,20+hintH,width,entryH),open,true))controller.Toggle(0,s,player.Id);
        }
        private void DrawHudMetric(Rect area,HudTextRow row)
        {
            HudLabel(new Rect(area.x,area.y,row.LabelWidth,row.LabelHeight),row.Label,small);
            HudLabel(new Rect(area.xMax-row.ValueWidth,area.y+row.ValueTop,row.ValueWidth,row.ValueHeight),row.Value,hudValue);
        }
        private void DrawHudRows(Rect rect,float top,string key,HudTextPage[] pages,float pagerHeight,float bottomPadding=16,bool separators=false)
        {
            float leftY=top,rightY=top;
            var page=pages[hudPages[key]];
            foreach(var row in page.Rows)
            {
                float x=HudTextLayout.CellLeft(rect.x,rect.width,page.Columns,row.Column),y=row.Column==0?leftY:rightY;
                if(row.Icon.Length>0)HudIcon(x+8,y+3,row.Icon);
                var area=new Rect(x+28,y,HudTextLayout.CellWidth(rect.width,page.Columns)-28,row.TextHeight);
                if(row.Label!=null)DrawHudMetric(area,row);else HudLabel(area,row.Text,small);
                if(separators&&row.ExtraGap>0)PanelFill(new Rect(x,y+row.TextHeight+HudTextLayout.RowGap+row.ExtraGap/2,HudTextLayout.CellWidth(rect.width,page.Columns),1),border);
                if(row.Column==0)leftY+=row.Height;else rightY+=row.Height;
            }
            if(pages.Length<=1)return;
            float py=rect.yMax-bottomPadding-pagerHeight,bw=(rect.width-32)*.28f;
            // 与既有设置按钮共用鼠标/手柄光标命中，只有超高内容才出现翻页。
            bool enabled=GUI.enabled;GUI.enabled=enabled&&hudPages[key]>0;
            if(Button(new Rect(rect.x+16,py,bw,pagerHeight),"←"))hudPages[key]--;
            GUI.enabled=enabled&&hudPages[key]<pages.Length-1;
            if(Button(new Rect(rect.xMax-16-bw,py,bw,pagerHeight),"→"))hudPages[key]++;
            GUI.enabled=enabled;
            string pageNumber=(hudPages[key]+1)+" / "+pages.Length;
            HudLabel(new Rect(rect.x+20+bw,py+4,rect.width-40-bw*2,HudHeight(pageNumber,rect.width-40-bw*2,small)),pageNumber,small);
        }
        public void Draw(Snapshot state)
        {
            if(Event.current.type==EventType.MouseMove||Event.current.type==EventType.MouseDrag)controller.Device="键鼠";
            if(!controller.OpacityEditing)opacityDragging=false;
            if(!state.Playing){enemyPositions.Clear();hudPages.Clear();hudTargets.Clear();stableExtent.Reset();extentIsland=-1;return;}if(!settings.Enabled.Value)return;
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
        private void DrawClockProgress(Rect rect,DayClockSnapshot clock)
        {
            Fill(new Rect(rect.x-1,rect.y-1,rect.width+2,rect.height+2),new Color(0,0,0,.8f));
            if(!clock.Available){Fill(rect,border);return;}
            var colors=new[]{muted,gold,new Color(.60f,.39f,.24f),new Color(.31f,.39f,.53f)};
            for(int i=0;i<4;i++)
            {
                float left=(float)Math.Round(rect.width*clock.PhasePositions[i]);
                float right=(float)Math.Round(rect.width*(i==3?1:clock.PhasePositions[i+1]));
                Fill(new Rect(rect.x+left,rect.y,Math.Max(0,right-left-1),rect.height),colors[i]);
            }
            float marker=(float)Math.Round((rect.width-3)*clock.Progress);
            Fill(new Rect(rect.x+marker-1,rect.y-2,5,rect.height+4),new Color(0,0,0,1));
            Fill(new Rect(rect.x+marker,rect.y-2,3,rect.height+4),bodyText);
        }
        private void DrawHud(Rect viewport,Snapshot s,PlayerInfo player)
        {
            var mainPanel=Bounds(MainPanelRect(viewport));
            bool panelOpen=controller.Open&&controller.Owner==player.Id;
            bool ShowCard(Rect rect)=>HudTextLayout.CardVisible(Bounds(rect),mainPanel,panelOpen);
            var occupied=new System.Collections.Generic.List<HudRect>(3);
            float pad=18,line=Math.Max(22,text.lineHeight),width=Math.Min(viewport.width-36,viewport.width>=900?390:viewport.width*.44f);
            bool stacked=viewport.width<620;
            if(stacked)width=viewport.width-36;
            // 只为当前设备的实际页脚留位；键鼠不再占用隐藏的手柄说明行。
            float footerWidth=viewport.width-pad*2,helpWidth=Math.Min(560,footerWidth);
            string padHelp=Localization.Text("右摇杆 · 光标 / 呼出键长按 · 显隐");
            string padOpen=Localization.Text(controller.PadOpenLabel+" · 面板");
            string guide=Localization.Text("F7 图鉴"),options=Localization.Text("F6 设置"),hide=Localization.Text("F4 收起");
            float guideW=Math.Max(94,button.CalcSize(new GUIContent(guide)).x+8),optionsW=Math.Max(94,button.CalcSize(new GUIContent(options)).x+8),hideW=Math.Max(94,button.CalcSize(new GUIContent(hide)).x+8);
            float footerScale=Math.Min(1,(footerWidth-16)/(guideW+optionsW+hideW));guideW*=footerScale;optionsW*=footerScale;hideW*=footerScale;
            bool padFooter=(PreviewHudDevice??controller.Device)=="手柄";
            float padButtonW=Math.Min(helpWidth,Math.Max(94,button.CalcSize(new GUIContent(padOpen)).x+8)),hintNaturalWidth=(float)Math.Ceiling(small.CalcSize(new GUIContent(padHelp)).x);
            bool inlinePadHelp=padFooter&&(PreviewHudColumns??settings.AdvisorColumns.Value)==1&&settings.FontSize.Value<=18&&padButtonW+16+hintNaturalWidth<=footerWidth;
            float padHelpW=inlinePadHelp?hintNaturalWidth:helpWidth;
            float footerButtonH=Math.Max(34,padFooter?HudHeight(padOpen,inlinePadHelp?padButtonW:helpWidth,button):Math.Max(HudHeight(guide,guideW,button),Math.Max(HudHeight(options,optionsW,button),HudHeight(hide,hideW,button))));
            float helpH=padFooter?HudHeight(padHelp,padHelpW,small):0;
            if(inlinePadHelp)footerButtonH=Math.Max(footerButtonH,helpH);
            float footerTop=HudTextLayout.FooterTop(viewport.height,footerButtonH,inlinePadHelp?0:helpH);
            var padButtonRect=new Rect(pad,viewport.height-16-footerButtonH,inlinePadHelp?padButtonW:helpWidth,footerButtonH);
            var padHelpRect=new Rect(inlinePadHelp?pad+padButtonW+16:pad,inlinePadHelp?padButtonRect.y+(footerButtonH-helpH)/2:footerTop,padHelpW,helpH);
            var hudViewport=new Rect(0,0,viewport.width,footerTop-8);
            float pagerHeight=Math.Max(34,HudHeight("←",70,button)),maxCardHeight=hudViewport.height-16;
            float y=18;
            // 给两列保留独立图标与文字空间；固定语义探针不随金币数量变化抖动。
            float preferredColumnsWidth=Math.Max(width,52+2*(28+small.CalcSize(new GUIContent(Localization.Text("建设科技")+" 99999")).x));
            float maxColumnsWidth=settings.ShowPlayerPanel.Value?(viewport.width-48)/2:viewport.width-36;
            bool twoColumnStatus=(PreviewHudColumns??settings.AdvisorColumns.Value)==2&&settings.Status.Value&&!stacked&&preferredColumnsWidth<=maxColumnsWidth;
            if(twoColumnStatus)width=preferredColumnsWidth;
            string heading=Localization.Text("王国顾问")+"  ⇄";
            string identity="P"+(player.Id+1);
            string clockHeader=Localization.Text(ClockPresentation.HudHeader(s.Clock,settings.ShowClock.Value));
            string date=Localization.English?"Island "+s.Island+" · Day "+s.Day:"岛 "+s.Island+" · 第 "+s.Day+" 天";
            string day=date+" · "+Localization.Text(Season(s.Season));
            var hudDayStyle=small;
            string countdown=Localization.Text(ClockPresentation.Countdown(s.Clock));
            var rows=new System.Collections.Generic.List<System.Tuple<string,string,string>>();
            void Info(string icon,string label,string value){rows.Add(System.Tuple.Create(icon,label,value));}
            Info("coin","金币",player.Coins.ToString());
            Info("gem","宝石",player.Gems.ToString());
            Info("bag",settings.InfiniteBag.Value?"数字钱袋":"原生钱袋",settings.InfiniteBag.Value?"无限容量":"资源合计 "+player.BagTotal);
            int populationStart=rows.Count;
            if(settings.Status.Value){foreach(var field in PopulationPresentation.Fields(s))Info(field.Item1,field.Item2,field.Item3.ToString());Info("tower","建设科技",(s.Iron?"高阶已解锁":s.Stone?"中阶已解锁":"基础")+(s.Online?" · 联机":""));}
            int populationCount=rows.Count-populationStart;
            if(settings.MountInfo.Value&&player.Mount.Length>0&&viewport.height>=640){Info("horse","坐骑",player.Mount+" · "+player.MountState);Info("stamina","耐力",player.Stamina.ToString("F1")+(player.Fed>0?" · 饱食 "+player.Fed.ToString("F0")+" 秒":""));if(player.Ability.Length>0)Info("stamina","能力",player.Ability);}
            float MeasureWidth(string value)=>small.CalcSize(new GUIContent(value)).x;
            float MeasureHeight(string value,float available)=>HudHeight(value,available,small);
            if(!twoColumnStatus)
            {
                // 固定语义探针决定列宽，不因余额、饱食倒计时或焦点目标变化而抖动。
                float metricWidth=HudTextLayout.MetricWidth(Localization.Text("建设科技"),Localization.Text("高阶已解锁"),MeasureWidth)+60;
                float clockWidth=HudTextLayout.MetricWidth(Localization.Text("黄昏 00:00"),Localization.Text("距入夜约 ")+string.Format(Localization.Text("{0}分{1}秒（现实时间）"),0,"00"),MeasureWidth)+32;
                float maximum=settings.ShowPlayerPanel.Value&&!stacked?(viewport.width-48)/2:viewport.width-36;
                width=Math.Min(maximum,Math.Max(width,(float)Math.Ceiling(Math.Max(metricWidth,clockWidth))));
            }
            var clockRow=HudTextLayout.Metric(clockHeader,countdown,"",width-32,MeasureWidth,MeasureHeight);
            float identityWidth=MeasureWidth(identity)+20;
            float statusTop=8,statusBottom=8,headingGap=6,dayGap=4,clockGap=8,progressStep=16;
            float headingH=Math.Max(HudHeight(heading,width-32-identityWidth,title),HudHeight(identity,identityWidth,hudValue)),dayH=HudHeight(day,width-32,hudDayStyle),clockH=clockRow.TextHeight;
            float statusHeaderH=statusTop+headingH+headingGap+dayH+dayGap+clockH+clockGap+progressStep;
            bool compactStatusHeader=maxCardHeight-statusHeaderH-statusBottom<pagerHeight+8+small.lineHeight+HudTextLayout.RowGap;
            if(compactStatusHeader)
            {
                // 窄分屏里页眉本身可能多行，时间文字随正文分页，避免占尽正文空间。
                rows.Insert(0,System.Tuple.Create("",clockHeader,countdown));rows.Insert(0,System.Tuple.Create("",day,""));
                statusHeaderH=statusTop+headingH+14;
                twoColumnStatus=false;
            }
            string statusKey="p"+player.Id+".status";
            float statusTextWidth=HudTextLayout.CellWidth(width,twoColumnStatus?2:1)-28;
            HudTextRow LocalRow(System.Tuple<string,string,string> r)
            {
                return HudTextLayout.Metric(Localization.Text(r.Item2),Localization.Text(r.Item3),r.Item1,statusTextWidth,MeasureWidth,MeasureHeight);
            }
            var statusRows=twoColumnStatus?rows.Take(populationStart).Concat(rows.Skip(populationStart+populationCount)):rows;
            var secondColumn=twoColumnStatus?rows.Skip(populationStart).Take(populationCount).Select(LocalRow).ToArray():null;
            var measuredStatusRows=statusRows.Select(LocalRow).ToArray();
            if(!compactStatusHeader)
            {
                // 完整保留每一条信息，在资源、人口及坐骑之间增加轻量分组留白。
                if(measuredStatusRows.Length>populationStart)measuredStatusRows[populationStart-1].ExtraGap=HudTextLayout.StatusGroupGap;
                if(!twoColumnStatus&&populationCount>0&&rows.Count>populationStart+populationCount)measuredStatusRows[populationStart+populationCount-1].ExtraGap=HudTextLayout.StatusGroupGap;
            }
            var statusPages=HudPages(statusKey,twoColumnStatus?"status-columns":"status",measuredStatusRows,statusTextWidth,maxCardHeight-statusHeaderH-statusBottom,pagerHeight,secondColumn);
            if(statusPages.Length==0){DrawCompactHud(viewport,s,player);return;}
            if(PreviewHudDevice!=null)hudPages[statusKey]=PreviewHudLastPage?statusPages.Length-1:0;
            float cardH=statusHeaderH+statusPages.Max(p=>p.Height)+statusBottom+(statusPages.Length>1?pagerHeight+8:0);
            y=Math.Max(18,hudViewport.height-cardH-8);
            var advisorRect=new Rect(pad,y,width,cardH);
            if(settings.ShowKingdomAdvisor.Value)advisorRect=Place(statusKey,advisorRect,hudViewport);
            // 整体缩短后，默认详情在共享顶部以下分页，不通过上移补足长正文空间。
            bool detailBeside=!stacked&&advisorRect.xMax+12+width<=viewport.width-18;
            float detailCardHeight=detailBeside?Math.Min(maxCardHeight,hudViewport.height-advisorRect.y-8):maxCardHeight;
            string objectTitle="",objectKey="p"+player.Id+".object";
            float headH=0,objectHeaderH=0,objectTop=statusTop;
            HudTextPage[] objectPages=Array.Empty<HudTextPage>();
            if(settings.ShowPlayerPanel.Value)
            {
                var t=PreviewTarget??player.Target;
                string targetTitle=Localization.Text(t==null?"附近交互":t.Name+(t.Level>=0?" · 等级 "+t.Level:""));
                objectTitle=targetTitle+"  ⇄";
                string body=t==null?"靠近建筑、工具或坐骑，查看用途与升级条件。":t.Description+"\n\n"+(t.Cost>=0?"费用  "+t.Cost+" "+t.Currency+"\n"+(t.Locked?"暂不可用  ":"可交互  ")+t.Lock:"当前无升级交互");
                if(t!=null&&t.Details.Length>0)body+="\n"+string.Join("\n",t.Details.Split('\n').Where(l=>!l.StartsWith("本岛同类交互点")));
                if(t!=null&&settings.Details.Value&&t.Next.Length>0)body+="\n升级目标  "+t.Next;
                headH=HudHeight(objectTitle,width-32,title);objectHeaderH=objectTop+headH+14;
                var detailRows=body.Split('\n').Select(value=>new HudTextRow(Localization.Text(value),value.Length==0?"":value.StartsWith("费用")?"coin":value.StartsWith("耐久")?"shield":"tower")).ToList();
                if(!HudTextLayout.CanPage(detailCardHeight-objectHeaderH-16,pagerHeight,HudHeight(HudLineProbe,width-60,small)))
                {
                    // 超长名称随正文分页，仍保留完整名称，不让标题挤掉所有说明。
                    objectTitle=Localization.Text("附近交互")+"  ⇄";
                    headH=HudHeight(objectTitle,width-32,title);objectHeaderH=objectTop+headH+14;
                    detailRows.Insert(0,new HudTextRow(targetTitle));
                }
                string objectIdentity=t==null?"none":t.Raw+"|"+t.X+"|"+t.Level;
                objectPages=HudPages(objectKey,objectIdentity,detailRows,width-60,detailCardHeight-objectHeaderH-16,pagerHeight);
                if(objectPages.Length==0){DrawCompactHud(viewport,s,player);return;}
                if(PreviewHudDevice!=null)hudPages[objectKey]=PreviewHudLastPage?objectPages.Length-1:0;
            }
            hudAdvisor=advisorRect;hudDetail=default;hudAlert=default;hudDetailPages=0;hudFooterTop=footerTop;hudHintHeight=helpH;hudLegacyBottom=viewport.height-66;hudRowCount=rows.Count+1;hudPageCount=statusPages.Length;hudColumns=statusPages[0].Columns;hudPad=padFooter;hudDay=day;hudOpacity=PreviewHudOpacity??(settings.AdvisorTransparent.Value?0:settings.PanelOpacities[OptionId.AdvisorOpacity].Value);
            if(settings.ShowKingdomAdvisor.Value){
            var statusRect=advisorRect;occupied.Add(Bounds(statusRect));
            hudAdvisor=statusRect;
            if(ShowCard(statusRect)){
            bool statusDecoration=HudCard(statusRect,OptionId.AdvisorOpacity,settings.AdvisorTransparent.Value);
            float cy=statusRect.y+statusTop;HudLabel(new Rect(statusRect.x+16,cy,width-32-identityWidth,headingH),heading,title);
            HudLabel(new Rect(statusRect.xMax-16-identityWidth,cy+(headingH-small.lineHeight)/2,identityWidth,small.lineHeight),identity,hudValue);cy+=headingH+headingGap;
            if(!compactStatusHeader){
            HudLabel(new Rect(statusRect.x+16,cy,width-32,dayH),day,hudDayStyle);cy+=dayH+dayGap;
            DrawHudMetric(new Rect(statusRect.x+16,cy,width-32,clockH),clockRow);cy+=clockH+clockGap;
            DrawClockProgress(new Rect(statusRect.x+16,cy,width-32,6),s.Clock);cy+=progressStep;
            }else cy=statusRect.y+statusHeaderH;
            DrawHudRows(statusRect,cy,statusKey,statusPages,pagerHeight,statusBottom,statusDecoration);
            }
            }
            y+=cardH+12;
            float mapW=settings.MapAtTop.Value||stacked?viewport.width-36:viewport.width-width-66;
            float mapX=settings.MapAtTop.Value||stacked?pad:viewport.width-pad-mapW;
            float mapH=Math.Max(350,small.lineHeight*7+196);
            float mapY=settings.MapAtTop.Value?18:Math.Max(18,hudViewport.height-mapH-8);
            if(settings.Map.Value&&mapH<=maxCardHeight)
            {
                var mapRect=Place("p"+player.Id+".map",new Rect(mapX,mapY,mapW,mapH),hudViewport,mapW-130);occupied.Add(Bounds(mapRect));DrawMap(mapRect,s,player,settings.MapTransparent.Value,true);
            }
            else if(!settings.Map.Value){var mapEntry=new Rect(mapX,mapY,mapW,40);occupied.Add(Bounds(mapEntry));if(Button(mapEntry,"开启小地图"))settings.Map.Value=true;}
            float objectX=advisorRect.xMax+12;
            float objectY=18;
            HudRect detailBounds=default;
            if(settings.ShowPlayerPanel.Value)
            {
                float objectH=objectHeaderH+objectPages.Max(p=>p.Height)+16+(objectPages.Length>1?pagerHeight+8:0);
                objectY=advisorRect.y;
                if(objectX+width>viewport.width-18){objectX=advisorRect.x;objectY=Math.Max(18,advisorRect.y-objectH-12);}
                var objectRect=Place(objectKey,new Rect(objectX,objectY,width,objectH),hudViewport);detailBounds=Bounds(objectRect);occupied.Add(detailBounds);
                hudDetail=objectRect;hudDetailPages=objectPages.Length;
                if(ShowCard(objectRect)){
                HudCard(objectRect,OptionId.PlayerOpacity,settings.PlayerTransparent.Value);
                HudLabel(new Rect(objectRect.x+16,objectRect.y+objectTop,width-32,headH),objectTitle,title);
                DrawHudRows(objectRect,objectRect.y+objectHeaderH,objectKey,objectPages,pagerHeight);
                }
            }
            if(settings.Alerts.Value)
            {
                string alert=player.Coins==0?"钱袋为空 · 留意收入与返程安全":s.Workers==0?"缺少工匠 · 建设和维修可能停滞":s.Phase=="夜晚"&&player.Target?.Category=="建设"?"夜间施工 · 先确认防线安全":"";
                var defense=settings.DefenseAlerts.Value?AdvisorAlerts.Defense(s,player):"";
                var staff=settings.StaffAlerts.Value?AdvisorAlerts.Staff(s,player.Id):"";
                if(defense.Length>0)alert=defense;else if(staff.Length>0)alert=staff;
                string alertText=Localization.Text(alert);
                const float alertPadding=4,alertGap=6;
                float alertTextH=HudHeight(alertText,width-32,small),ah=alertTextH+alertPadding*2;
                if(alert.Length>0&&ah<=maxCardHeight)
                {
                    string alertKey="p"+player.Id+".alert";
                    var above=HudTextLayout.AboveAlert(Bounds(advisorRect),ah,alertGap);
                    // 保留拖动偏好；字号或布局变化导致保存位置与正文相交时，优先恢复顾问上方。
                    var desired=Bounds(Place(alertKey,Rectangle(above),hudViewport));
                    if(HudTextLayout.TryPlaceAlert(desired,new HudRect(8,8,hudViewport.width-16,hudViewport.height-16),detailBounds,occupied,alertGap,out desired,above))
                    {
                        var alertRect=Rectangle(desired);
                        hudAlert=alertRect;
                        if(ShowCard(alertRect)){HudCard(alertRect,OptionId.AlertOpacity,settings.AlertTransparent.Value);HudLabel(new Rect(alertRect.x+16,alertRect.y+alertPadding,width-32,alertTextH),alertText,small);}
                    }
                }
            }
            if(padFooter){
                if(Button(padButtonRect,padOpen,true))controller.Toggle(0,s,player.Id);
                HudLabel(padHelpRect,padHelp,small);
            }else{
                float footerX=pad;
                float footerY=viewport.height-16-footerButtonH;
                if(Button(new Rect(footerX,footerY,guideW,footerButtonH),guide,true))controller.Toggle(0,s,player.Id);footerX+=guideW+8;
                if(Button(new Rect(footerX,footerY,optionsW,footerButtonH),options,true))controller.Toggle(2,s,player.Id);footerX+=optionsW+8;
                if(Button(new Rect(footerX,footerY,hideW,footerButtonH),hide,true)){settings.Enabled.Value=false;controller.Close();}
            }
        }
        private void DrawMap(Rect area,Snapshot s,PlayerInfo player,bool transparent=false,bool hud=false)
        {
            activePanelOpacity=settings.PanelOpacities[OptionId.MapOpacity].Value;
            bool interactive=controller.Open?!hud&&controller.Tab==3:hud;
            if(interactive)controller.BeginMapDraw(player.Id);
            if(!transparent&&(activePanelOpacity<0||hud||settings.InterfaceTransparent.Value))Card(area);float x0=area.x+18,width=area.width-36,rowH=small.lineHeight+5;
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
            var points=s.Points.Where(p=>controller.MapPointVisible(p,!hud)&&p.X>=left&&p.X<=right&&(settings.FullMap.Value||s.ExploredRanges.Any(r=>p.X>=r.Item1&&p.X<=r.Item2)))
                .GroupBy(p=>p.Name+"|"+Math.Round(p.X)).Select(g=>g.First()).OrderBy(p=>p.X).ToArray();
            var pointGroups=points.GroupBy(p=>p.Name+"|"+MapGrouping.Cell(p.X,extent.Item1,length*24/width)).ToArray();
            var counts=pointGroups.Select(g=>g.Count()).ToArray();
            points=pointGroups.Select(g=>g.First()).ToArray();
            var requests=new System.Collections.Generic.List<MapLabelRequest>();
            var labels=new string[points.Length];
            for(int i=0;i<points.Length;i++)
            {
                var point=points[i];labels[i]=MapText.Label(point,player.Id,counts[i],pointGroups[i].Sum(p=>Math.Max(0,p.CampPeople)));
                float measured=small.CalcSize(new GUIContent(Localization.Text(labels[i]))).x;
                requests.Add(new MapLabelRequest{Index=i,Anchor=(point.X-left)/length*width,Width=measured+4,
                    Priority=MapGrouping.Priority(point)});
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
                "spear"=>new[]{"00100","01110","00100","00100","00100"},
                "axe"=>new[]{"01111","11111","00100","00100","00100"},
                "ninja"=>new[]{"01110","10101","11111","00100","01010"},
                "fish"=>new[]{"00000","01010","11111","01110","00000"},
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
            activePanelOpacity=settings.PanelOpacities[OptionId.InterfaceOpacity].Value;
            if(controller.Tab!=0)controller.SearchFocused=false;
            var panel=MainPanelRect(viewport);
            if(!settings.InterfaceTransparent.Value&&activePanelOpacity<0)PanelFill(viewport,new Color(0,0,0,.38f));Card(panel,settings.InterfaceTransparent.Value,true);guiOrigin+=new Vector2(panel.x,panel.y);GUI.BeginGroup(panel);
            float w=panel.width,h=panel.height,line=Math.Max(24,text.lineHeight+7);
            var tabs=new[]{"图鉴","地点","设置","地图","岛屿记录","资源"};for(int i=0;i<tabs.Length;i++){var rect=new Rect(12+i*(w-115)/tabs.Length,10,(w-131)/tabs.Length,line+6);if(Button(rect,(controller.Tab==i?"● ":"")+tabs[i])){controller.ChangeTab(i-controller.Tab);}}
            if(Button(new Rect(w-91,10,79,line+6),Localization.English?"Close":"关闭"))controller.Close();
            string help=controller.Device=="手柄"?controller.PadOpenLabel+" 短按面板／长按显隐 · LB/RB 页签 · LT/RT "+(controller.PadRegion==3&&controller.DetailPages>1?"详情":"列表")+"翻页 · 方向选择 · 确认／返回":"上下选择 · 左右分类 · Enter 选择 · Tab 切页 · Esc 关闭";
            if(controller.Tab==1)help=controller.Device=="手柄"?"确认定位地图 · X 查看图鉴 · LT/RT 列表翻页 · B 返回":"Enter 定位地图 · C 查看图鉴 · PgUp/PgDn 详情 · Esc 关闭";
            if(controller.Tab==0)help+=controller.Device=="手柄"?" · 确认查看本岛地点":" · Enter 查看本岛地点 · PgUp/PgDn 详情";
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
            float railRowH=Math.Min(rowH,(bottom-top)/OptionGroups.Names.Length-6);
            bool priorEnabled=GUI.enabled;if(controller.SettingsDropdown)GUI.enabled=false;
            for(int i=0;i<OptionGroups.Names.Length;i++)if(Button(new Rect(14,top+i*(railRowH+6),rail-14,railRowH),(controller.Section==i?"● ":"")+OptionGroups.Names[i])){controller.Section=i;controller.Focus=0;controller.SettingsDropdown=false;}
            var items=controller.SettingsItems;float x=rail+14,rw=w-x-14;
            if(controller.OpacityEditing){GUI.enabled=priorEnabled;DrawOpacityEditor(new Rect(x,top,rw,bottom-top));return;}
            if(controller.Section==OptionGroups.MapFilterSection)rowH=Math.Max(34,text.lineHeight+10);
            string note=controller.Section==OptionGroups.MapFilterSection?"单击勾选 · 按类别筛选 · 即时保存":controller.Section==3?"即时保存 · 长按购买限单机或房主本机角色 · 松手结束":"即时保存 · 玩法辅助默认关闭 · 单机与联机均开放";float noteH=small.CalcHeight(new GUIContent(Localization.Text(note)),rw),py=bottom-noteH-rowH-12;
            bool appearance=controller.Section==OptionGroups.AppearanceSection,filters=controller.Section==OptionGroups.MapFilterSection;
            int visible=appearance?items.Length:Math.Max(1,(int)((py-top-8)/(rowH+5))),page=controller.Focus/visible,start=page*visible;controller.VisibleRows=visible;
            if(appearance)rowH=Math.Min(rowH,(py-top-8)/7-5);
            if(filters){visible=Math.Max(1,visible-2);page=controller.Focus<5?0:(controller.Focus-5)/visible;start=5+page*visible;controller.VisibleRows=visible;}
            for(int i=filters?0:start;i<Math.Min(start+visible,items.Length);i++){
                if(filters&&i>=5&&i<start)continue;
                var item=items[i];int value=Math.Clamp(controller.OptionValue(item),0,item.Choices.Length-1);
                bool paired=appearance&&i>0&&i<items.Length-1;
                int row=appearance?(i==0?0:i==items.Length-1?6:(i+1)/2):i-start;
                var rowRect=new Rect(x+(paired&&i%2==0?(rw+8)/2:0),top+row*(rowH+5),paired?(rw-8)/2:rw,rowH);
                if(filters){row=i==0?0:i<5?1:2+i-start;rowRect=new Rect(x+(i>0&&i<5?(i-1)*(rw+6)/4:0),top+row*(rowH+5),i>0&&i<5?(rw-18)/4:rw,rowH);}
                if(Button(rowRect,(controller.Focus==i?"▶ ":"")+item.DisplayLabel(item.Kind==OptionKind.Slider?OpacityTextFor(item.Id):item.Choices[value]))){controller.Focus=i;controller.SettingsDropdown=false;controller.Activate();}
            }

            if(!appearance){
            float pageButtonW=Math.Max(100,Math.Max(button.CalcSize(new GUIContent(Localization.Text("上一页"))).x,button.CalcSize(new GUIContent(Localization.Text("下一页"))).x)+8);
            if(Button(new Rect(x,py,pageButtonW,rowH),"上一页")){controller.FlipPage(-1,controller.CurrentSnapshot);}
            Label(new Rect(x+pageButtonW+15,py,rw-pageButtonW*2-30,rowH),(page+1)+" / "+((Math.Max(1,items.Length-(filters?5:0))+visible-1)/visible),small);
            if(Button(new Rect(w-14-pageButtonW,py,pageButtonW,rowH),"下一页")){controller.FlipPage(1,controller.CurrentSnapshot);}
            }
            Label(new Rect(x,py+rowH+8,rw,noteH),note,small);
            GUI.enabled=priorEnabled;
            if(controller.SettingsDropdown){
                var item=items[controller.Focus];int columns=item.Choices.Length*(rowH+3)+16>bottom-top?2:1;int choiceRows=(item.Choices.Length+columns-1)/columns;float dh=choiceRows*(rowH+3)+16;var rect=new Rect(w-Math.Min(300,rw)-20,Math.Clamp(top+(controller.Focus-start)*(rowH+5)+rowH,top,Math.Max(top,bottom-dh)),Math.Min(300,rw),dh);Card(rect);
                for(int i=0;i<item.Choices.Length;i++)if(Button(new Rect(rect.x+8+(i/choiceRows)*(rect.width-16)/columns,rect.y+8+(i%choiceRows)*(rowH+3),(rect.width-16)/columns,rowH),(controller.Choice==i?"▶ ":"")+item.Choices[i])){controller.Choice=i;controller.SetOption(item,i);controller.SettingsDropdown=false;}
            }
        }
        private string OpacityTextFor(OptionId id){int value=controller.GetDisplayedOpacity(id);if(value<0)return "各面板不同";return (controller.GetOpacity(id)<0?"默认 · ":"")+value+"%";}
        private string OpacityText=>OpacityTextFor(controller.SettingsItems[controller.Focus].Id);
        private void DrawOpacityEditor(Rect area)
        {
            float y=area.y,buttonH=Math.Max(40,text.lineHeight+14);
            y+=Row(area.x,y,area.width,controller.OpacityName,title);
            y+=Row(area.x,y,area.width,OpacityText,text);
            string help=controller.Device=="手柄"?"左右微调 1% · 上下选择 · A 确认 · B 返回":"拖动滑动条 · 左右微调 1% · 上下选择 · Enter 确认 · Esc 返回";
            y+=Row(area.x,y,area.width,help,small);
            // 同一矩形用于可见轨道、鼠标拖动和手柄光标点击。
            var track=new Rect(area.x+12,y+10,area.width-24,36);
            if(!Input.GetMouseButton(0))opacityDragging=false;
            var ev=Event.current;
            if(ev.type==EventType.MouseDown&&ev.button==0&&track.Contains(ev.mousePosition)){
                opacityDragging=true;controller.Device="键鼠";controller.Choice=0;
                controller.SetOpacity(PanelAppearance.FromPosition(ev.mousePosition.x,track.x,track.width));ev.Use();
            }
            if(opacityDragging&&ev.type==EventType.MouseDrag){controller.SetOpacity(PanelAppearance.FromPosition(ev.mousePosition.x,track.x,track.width));ev.Use();}
            if(ev.type==EventType.MouseUp&&ev.button==0&&opacityDragging){opacityDragging=false;ev.Use();}
            if(controller.PointerButton(track,guiPlayer,guiOrigin)){controller.Choice=0;controller.SetOpacity(PanelAppearance.FromPosition(PointerPosition.x,track.x,track.width));}
            Fill(new Rect(track.x,track.center.y-2,track.width,4),border);
            float thumbX=track.x+track.width*PanelAppearance.SliderValue(controller.OpacityValue)/100f;
            Fill(new Rect(track.x,track.center.y-2,thumbX-track.x,4),gold);
            if(controller.GetDisplayedOpacity(controller.SettingsItems[controller.Focus].Id)>=0)Fill(new Rect(thumbX-6,track.y+5,12,26),controller.Choice==0?gold:muted);
            y=track.yMax+12;
            Label(new Rect(track.x,y,60,small.lineHeight),"0%",small);float endWidth=small.CalcSize(new GUIContent("100%")).x;Label(new Rect(track.xMax-endWidth,y,endWidth,small.lineHeight),"100%",small);y+=small.lineHeight+12;
            float half=(area.width-8)/2;if(Button(new Rect(area.x,y,half,buttonH),(controller.Choice==1?"▶ ":"")+"恢复默认")){controller.Choice=1;controller.SetOpacity(-1);}
            if(Button(new Rect(area.x+half+8,y,half,buttonH),(controller.Choice==2?"▶ ":"")+"完成"))controller.SettingsDropdown=false;
        }
        private void DrawResources(float w,float top,float bottom,Snapshot s)
        {
            float rowH=Math.Max(42,text.lineHeight+16);
            var p=s.Players.FirstOrDefault(t=>t.Id==controller.ResourcePlayer&&t.Local)??s.Players.FirstOrDefault(t=>t.Local);
            if(p==null)return;controller.ResourcePlayer=p.Id;
            if(!s.ResourceTypes.Contains(controller.ResourceType)&&s.ResourceTypes.Count>0)controller.ResourceType=s.ResourceTypes[0];
            int balance=p.Resources.TryGetValue(controller.ResourceType,out var b)?b:0;
            string[] rows={"玩家：P"+(p.Id+1)+(OptionItem.HasSelection(s.Players.Count(t=>t.Local))?"  ▾":""),"资源："+ResourceRules.Name(controller.ResourceType)+" · 当前 "+balance+(OptionItem.HasSelection(s.ResourceTypes.Count)?"  ▾":""),"数量："+(controller.ResourceEditing?controller.ResourceText+" ▏":settings.ResourceAmount.Value.ToString()),"快捷数量："+settings.ResourceAmount.Value+"  ▾","添加 "+settings.ResourceAmount.Value+" "+ResourceRules.Name(controller.ResourceType),"清空范围："+new[]{"仅金币","当前资源","全部钱袋资源"}[controller.ClearMode]+"  ▾","清空所选资源"};
            bool priorEnabled=GUI.enabled;if(controller.ResourceDropdown)GUI.enabled=false;
            for(int i=0;i<rows.Length;i++){
                bool enabled=GUI.enabled;
                if(i==0&&!OptionItem.HasSelection(s.Players.Count(t=>t.Local))||i==1&&!OptionItem.HasSelection(s.ResourceTypes.Count))GUI.enabled=false;
                if(Button(new Rect(20,top+i*(rowH+9),w-40,rowH),(controller.Focus==i?"▶ ":"")+rows[i])){controller.Focus=i;controller.Activate();}
                GUI.enabled=enabled;
            }
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
            for(int i=start;i<Math.Min(start+visible,entries.Length);i++){var rect=new Rect(14,top+(i-start)*(line+5),listW-20,line+2);if(Button(rect,(settings.Icons.Value?"         ":"")+entries[i].Name,selectedOverride:controller.Selected==i))controller.Selected=i;if(settings.Icons.Value)CatalogIcon(rect.x+15,rect.y+8,entries[i]);}
            Pagination(14,bottom-line-4,listW-20,line,page,Math.Max(1,(entries.Length+visible-1)/visible),visible,entries.Length);
            var selected=entries[controller.Selected];float dx=listW+8,dw=w-dx-14,y=top;
            if(lastCatalogKey!=selected.Key){lastCatalogKey=selected.Key;controller.DetailPage=0;}
            if(settings.Icons.Value)CatalogIcon(dx,y+4,selected);
            y+=Row(dx+(settings.Icons.Value?42:0),y,dw-(settings.Icons.Value?42:0),(controller.Device=="手柄"&&controller.PadRegion==3?"▶ ":"")+selected.Name,title);y+=Row(dx,y,dw,"类别："+selected.Category,small);
            string body=selected.Description+"\n\n建议："+selected.Advice;
            if(state.Counts.TryGetValue(selected.Key,out var count))body+="\n\n本岛同类交互点："+count;
            body+="\n\n费用、等级和锁定原因请靠近对象查看。不同主题以当前游戏数据为准。";
            var linked=state.Points.Any(p=>p.Key==selected.Key);
            var pages=SplitPages(body,dw,Math.Max(line,bottom-y-line*2-20));
            controller.DetailPages=pages.Length;controller.DetailPage%=pages.Length;
            Label(new Rect(dx,y,dw,0),pages[controller.DetailPage],text);
            if(pages.Length>1&&Button(new Rect(dx,bottom-line*2-8,dw,line),"详情 "+(controller.DetailPage+1)+" / "+pages.Length+" · 下一页"))controller.DetailPage++;
            if(linked){if(Button(new Rect(dx,bottom-line,dw,line),"查看本岛地点"))controller.ShowCatalogLocations(selected);}
            else Label(new Rect(dx,bottom-line,dw,line),"本岛可见范围内没有对应地点。",small);
        }
        private void Pagination(float x,float y,float width,float height,int page,int count,int perPage,int total)
        {if(Button(new Rect(x,y,width*.28f,height),"←"))controller.Selected=Math.Max(0,(page-1)*perPage);Label(new Rect(x+width*.30f,y,width*.36f,height),(page+1)+" / "+count,small);if(Button(new Rect(x+width*.70f,y,width*.30f,height),"→"))controller.Selected=Math.Min(total-1,(page+1)*perPage);}
        private void DrawPoints(float w,float top,float bottom,Snapshot state,PlayerInfo player)
        {
            float line=Math.Max(36,text.lineHeight+10);var points=controller.FilteredPoints(state,player);
            int ci=Array.IndexOf(controller.Categories,controller.Category);
            if(Button(new Rect(14,top,w-28,line),controller.PointCatalogKey.Length>0?"清除图鉴筛选":"筛选："+controller.Category)){if(controller.PointCatalogKey.Length>0)controller.PointCatalogKey="";else controller.Category=controller.Categories[(ci+1)%controller.Categories.Length];controller.Selected=0;}
            top+=line+8;if(points.Length==0){Label(new Rect(14,top,w-28,0),"当前筛选没有兴趣点。",text);return;}
            float listW=w*.42f,rowH=Math.Max(48,text.lineHeight*2+8);
            controller.Selected=Math.Clamp(controller.Selected,0,points.Length-1);int visible=Math.Max(1,(int)((bottom-top-line-10)/(rowH+5)));int page=controller.Selected/visible,start=page*visible;
            controller.VisibleRows=visible;
            for(int i=start;i<Math.Min(start+visible,points.Length);i++){var p=points[i];var rect=new Rect(14,top+(i-start)*(rowH+5),listW-22,rowH);if(Button(rect,(controller.Selected==i?"▶ ":"")+p.Name+"\n"+LocationPresentation.Distance(p,player.X)))controller.Selected=i;}
            Pagination(14,bottom-line-4,listW-22,line,page,Math.Max(1,(points.Length+visible-1)/visible),visible,points.Length);
            var selected=points[controller.Selected];float dx=listW+8,dw=w-dx-14,y=top;
            var key=LocationPresentation.Identity(selected);if(lastLocationKey!=key){lastLocationKey=key;controller.DetailPage=0;}
            y+=Row(dx,y,dw,selected.Name,title);y+=Row(dx,y,dw,"类别："+selected.Category,small);if(!controller.PointVisible(selected))y+=Row(dx,y,dw,"已在地图隐藏 · 可临时定位",small);
            var pages=SplitPages(LocationPresentation.Detail(selected,player),dw,Math.Max(line,bottom-y-line*3-20));
            controller.DetailPages=pages.Length;controller.DetailPage%=pages.Length;
            Label(new Rect(dx,y,dw,0),pages[controller.DetailPage],text);
            if(pages.Length>1&&Button(new Rect(dx,bottom-line*3-16,dw,line),"详情 "+(controller.DetailPage+1)+" / "+pages.Length+" · 下一页"))controller.DetailPage++;
            if(Button(new Rect(dx,bottom-line*2-8,dw,line),"在地图查看"))controller.Inspect(selected,player.Id);
            if(LocationPresentation.Entry(selected)!=null&&Button(new Rect(dx,bottom-line,dw,line),"查看图鉴"))controller.ShowPointCatalog(selected);
            else if(LocationPresentation.Entry(selected)==null)Label(new Rect(dx,bottom-line,dw,line),"该地点尚无对应图鉴条目。",small);
        }
        private void CatalogIcon(float x,float y,Entry entry)
        {
            var pattern=CatalogIcons.Pattern(CatalogIcons.Kind(entry));var color=MarkerColor(entry.Category);
            for(int row=0;row<pattern.Length;row++)for(int col=0;col<pattern[row].Length;col++)if(pattern[row][col]=='1')Fill(new Rect(x+col*3,y+row*3,3,3),color);
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
            if(mapH>=Math.Max(350,small.lineHeight*7+196)){DrawMap(new Rect(14,top,w-28,mapH),state,player);activePanelOpacity=settings.PanelOpacities[OptionId.InterfaceOpacity].Value;top+=mapH+16;}
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
