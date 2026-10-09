using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rewired;
namespace KingdomAdvisor
{
    internal sealed partial class AdvisorController
    {
        private sealed class PadData {public readonly HudButtonGesture HudButton=new HudButtonGesture();public IGamepadTemplate Pad;public Vector2 Cursor;public int DeviceId=-1;public bool Submit,Down,Back,Pointer;public int ClickFrame=-1;public MapPoint Hit;public Rect HitLabel,HitMarker;public bool Failed;}
        private readonly Dictionary<int,PadData> pads=new Dictionary<int,PadData>();
        private readonly Dictionary<int,List<ActionElementMap>> suppressedMovement=new Dictionary<int,List<ActionElementMap>>();
        public int MovementLeakFrames;public int DisabledMovementMaps;
        public void ResetHudButtons(){foreach(var data in pads.Values)data.HudButton.Reset();}
        public void RestorePadMappings()
        {foreach(var list in suppressedMovement.Values)foreach(var map in list)map.enabled=true;suppressedMovement.Clear();DisabledMovementMaps=0;}
        private void ReservePointerAxes(int owner,Rewired.Player input,Rewired.Controller joystick,IGamepadTemplate pad)
        {
            if(!settings.Enabled.Value||!settings.PadCursor.Value){RestorePadMappings();return;}
            if(!suppressedMovement.TryGetValue(owner,out var saved))suppressedMovement[owner]=saved=new List<ActionElementMap>();
            var maps=input.controllers.maps.GetMaps(ControllerType.Joystick,joystick.id);
            var source=pad.rightStick.horizontal.source;
            var targets=source.splitAxis?new[]{source.positiveTarget,source.negativeTarget}:new[]{source.fullTarget};
            for(int i=0;i<maps.Cast<Il2CppSystem.Collections.Generic.ICollection<ControllerMap>>().Count;i++){var elements=maps[i].ElementMaps;for(int j=0;j<elements.Cast<Il2CppSystem.Collections.Generic.ICollection<ActionElementMap>>().Count;j++){
                var map=elements[j];if(map.actionId!=(int)RewiredAxis.Horizontal)continue;
                bool right=targets.Any(target=>target!=null&&map.IsTarget(target));
                if(!right)continue;
                if(map.enabled){if(!saved.Any(old=>old.id==map.id))saved.Add(map);map.enabled=false;DisabledMovementMaps++;
                    Plugin.Instance.Log.LogInfo("Pointer axis reserved player="+owner+" device="+joystick.id+" element="+map.elementIdentifierName+" action="+map.actionId);}
            }}
            if(pad.leftStick.value.sqrMagnitude<.04f&&Math.Abs(pad.rightStick.value.x)>.2f&&Math.Abs(input.GetAxis((int)RewiredAxis.Horizontal))>.2f){
                MovementLeakFrames++;if(MovementLeakFrames==1||MovementLeakFrames%60==0)Plugin.Instance.Log.LogError("Pointer movement isolation failed player="+owner+" frames="+MovementLeakFrames);
            }
        }
        private readonly MenuRepeat triggerRepeat=new MenuRepeat();
        private PadData GetPad(int id){if(!pads.TryGetValue(id,out var p))pads[id]=p=new PadData{Cursor=new Vector2(Screen.width*.5f,Screen.height*.3f)};return p;}
        public Vector2 PadPointer(int id)=>GetPad(id).Cursor;
        public string PadOpenLabel=>settings.PadOpenButton.Value==1?"按下右摇杆":settings.PadOpenButton.Value==2?"Back／Select":"按下左摇杆";
        public bool PadPointerActive(int id)=>Device=="手柄"&&settings.PadCursor.Value&&GetPad(id).Pad!=null;
        public bool PadPointerHover(int id)=>PadPointerActive(id)&&GetPad(id).Pointer;
        public bool PadBlocks(int id){var p=GetPad(id);return p.Pad!=null&&(p.HudButton.Active||(p.Pad.leftStick.press.justPressed&&settings.PadOpenButton.Value==0)||(p.Pad.rightStick.press.justPressed&&settings.PadOpenButton.Value==1)||(p.Pad.back.justPressed&&settings.PadOpenButton.Value==2)||(settings.Enabled.Value&&Device=="手柄"&&p.Pad.a.value&&p.Pointer&&p.Hit!=null));}
        public bool PointerButton(Rect rect,int id,Vector2 origin)
        {var p=GetPad(id);if(!PadPointerHover(id)||p.ClickFrame!=Time.frameCount||Event.current.type!=EventType.Repaint||!rect.Contains(p.Cursor-origin))return false;p.ClickFrame=-1;return true;}
        public void ClearPadHit(int id){GetPad(id).Hit=null;}
        public void PadMapHit(MapPoint point,int id,Rect label,Rect marker,Vector2 origin){var p=GetPad(id);p.Hit=point;p.HitLabel=new Rect(label.x+origin.x,label.y+origin.y,label.width,label.height);p.HitMarker=new Rect(marker.x+origin.x,marker.y+origin.y,marker.width,marker.height);}
        private void PollPads(Snapshot s)
        {
            foreach(var local in s.Players.Where(p=>p.Local)){
                var data=GetPad(local.Id);if(data.Failed)continue;if(!settings.PadCursor.Value)data.Pointer=false;
                var viewport=new Rect(local.ViewX*Screen.width,(1-local.ViewY-local.ViewH)*Screen.height,local.ViewW*Screen.width,local.ViewH*Screen.height);
                try{
                    if(!routedInputs.TryGetValue(local.Id,out var input))input=ReInput.players.GetPlayer(local.Id);
                    if(input==null)continue;
                    var devices=input.controllers.Joysticks;int count=devices.Cast<Il2CppSystem.Collections.Generic.ICollection<Joystick>>().Count;
                    if(count==0){data.Pad=null;data.DeviceId=-1;releaseGuard.Remove(local.Id);continue;}
                    var joystick=input.controllers.GetLastActiveController(ControllerType.Joystick)??devices[0];
                                        if(data.DeviceId!=joystick.id){data.HudButton.Reset();data.Cursor=viewport.center;var native=joystick.GetTemplate(GamepadTemplate.typeGuid);data.Pad=native?.TryCast<IGamepadTemplate>();data.DeviceId=joystick.id;Plugin.Instance.Log.LogInfo("Pad binding player="+local.Id+" device="+joystick.name+" id="+joystick.id+" gamepadTemplate="+(data.Pad!=null));}
                    if(data.Pad==null){releaseGuard.Remove(local.Id);continue;}
                    var pad=data.Pad;ReservePointerAxes(local.Id,input,joystick,pad);data.Submit=pad.a.value;data.Down=pad.a.justPressed;data.Back=pad.b.justPressed;
                    if((pad.leftStick.value-pad.leftStick.valuePrev).sqrMagnitude>.04f||pad.a.justPressed||pad.b.justPressed||pad.leftBumper.justPressed||pad.rightBumper.justPressed)Device="手柄";
                    var rs=pad.rightStick.value;
                    if(settings.PadCursor.Value&&rs.sqrMagnitude>.04f){data.Cursor=new Vector2(Math.Clamp(data.Cursor.x+rs.x*settings.PadSpeed.Value*Time.unscaledDeltaTime*Screen.height/1080,viewport.x+8,viewport.xMax-8),Math.Clamp(data.Cursor.y-rs.y*settings.PadSpeed.Value*Time.unscaledDeltaTime*Screen.height/1080,viewport.y+8,viewport.yMax-8));data.Pointer=true;Device="手柄";}
                    if(data.Hit!=null&&!data.HitLabel.Contains(data.Cursor)&&!data.HitMarker.Contains(data.Cursor))data.Hit=null;
                    var open=settings.PadOpenButton.Value==1?pad.rightStick.press:settings.PadOpenButton.Value==2?pad.back:pad.leftStick.press;
                    if(open.justPressed){Hold.Cancel();data.Hit=null;}
                    int hudAction=data.HudButton.Step(open.value,Time.unscaledTime);
                    if(hudAction==2){Device="手柄";settings.Enabled.Value=!settings.Enabled.Value;Close();data.Hit=null;Plugin.Instance.Log.LogInfo("Pad HUD visibility="+settings.Enabled.Value);continue;}
                    if(hudAction==1){Device="手柄";settings.Enabled.Value=true;if(Open&&Owner==local.Id)Close();else Toggle(Tab,s,local.Id);data.Hit=null;continue;}
                    if(data.HudButton.Active)continue;
                    if(releaseGuard.Contains(local.Id)&&!data.Submit&&!pad.b.value&&pad.leftStick.value.sqrMagnitude<.16f)releaseGuard.Remove(local.Id);
                    if(!settings.Enabled.Value){data.Hit=null;continue;}
                    if(!Open){
                        if(data.Pointer&&Device=="手柄"&&data.Hit!=null){pressedPoint=data.Hit;if(data.Down&&!Hold.Active){mousePress=false;Hold.Begin(PointKey(data.Hit),local.Id,Time.unscaledTime,settings.Teleport.Value);}if(Hold.Active&&!mousePress)StepMapHold(PointKey(data.Hit),data.Submit);}
                        else if(Hold.Active&&!mousePress&&Hold.Owner==local.Id)StepMapHold("",data.Submit);
                        if(data.Down&&data.Pointer)data.ClickFrame=Time.frameCount;
                        continue;
                    }
                    if(Owner!=local.Id)continue;
                    if(Tab==1&&pad.x.justPressed){Device="手柄";var points=FilteredPoints(s,local);if(points.Length>0)ShowPointCatalog(points[Math.Clamp(Selected,0,points.Length-1)]);continue;}
                    if(data.Back){Device="手柄";if(ResourceDropdown)ResourceDropdown=false;else if(SettingsDropdown)SettingsDropdown=false;else if(ResourceEditing)ResourceEditing=false;else if(SearchEditing){SearchEditing=false;PadRegion=2;}else if(PadRegion==3){PadRegion=0;}else Close();continue;}
                    if(pad.rightBumper.justPressed||pad.leftBumper.justPressed){Device="手柄";ChangeTab(pad.rightBumper.justPressed?1:-1);continue;}
                    int page=triggerRepeat.Step(pad.rightTrigger.value>.5f?1:pad.leftTrigger.value>.5f?-1:0,Time.unscaledTime);
                    if(page!=0){Plugin.Instance.Log.LogInfo("Pad page direction="+page+" tab="+Tab+" region="+PadRegion+" rows="+VisibleRows+" selected="+Selected);Device="手柄";if((Tab==0||Tab==4)&&PadRegion==3&&DetailPages>1)DetailPage=Math.Max(0,DetailPage+page);else {if(Tab==0)PadRegion=0;FlipPage(page,s);} }
                    var direction=pad.dPad.value;if(direction.sqrMagnitude<.1f)direction=pad.leftStick.value;
                    int dx=horizontalRepeat.Step(direction.x,Time.unscaledTime),dy=verticalRepeat.Step(direction.y,Time.unscaledTime);
                    if(dx!=0||dy!=0){data.Pointer=false;Device="手柄";SearchFocused=false;
                        if(SearchEditing)SearchCursor=Math.Clamp(SearchCursor-dy*6+dx,0,SearchKeys.Length-1);
                        else {if(dy!=0){PadRegion=0;Navigate(-dy,s);}if(dx!=0){if(Tab==3)Navigate(dx,s);else Horizontal(dx);}}
                    }
                    if(data.Pointer&&Device=="手柄"){
                        if(data.Hit!=null){pressedPoint=data.Hit;if(data.Down&&!Hold.Active){mousePress=false;Hold.Begin(PointKey(data.Hit),local.Id,Time.unscaledTime,settings.Teleport.Value);}if(Hold.Active&&!mousePress)StepMapHold(PointKey(data.Hit),data.Submit);}
                        else {if(Hold.Active&&!mousePress)StepMapHold("",data.Submit);if(data.Down)data.ClickFrame=Time.frameCount;}
                    }else if(Tab==3&&(Device=="手柄"||data.Down)){if(data.Down||Hold.Active)Device="手柄";UpdatePanelMapHold(data.Submit,data.Down);}
                    else if(data.Down){Device="手柄";if(Tab==0&&SearchEditing)SearchKey(SearchCursor);else if((Tab==0||Tab==4)&&PadRegion==0)PadRegion=3;else Activate();}
                }catch(Exception ex){data.Failed=true;data.Pad=null;Plugin.Instance.Log.LogError("Gamepad template input failed for player "+local.Id+": "+ex);Actions.Notify("手柄输入读取失败，请查看日志");}
            }
        }
    }
}
