#Requires -Version 7.0
$ErrorActionPreference='Stop'
$source=Get-Content (Join-Path $PSScriptRoot 'src/Plugin.cs') -Raw
$start=$source.IndexOf('private void ReadPlayer(Player p,Camera cam)')
$end=$source.IndexOf('private void ReadPlayerDetails(', $start)
$playerMethod=$source.Substring($start,$end-$start)
$start=$source.IndexOf('private void ReadOptional(')
$end=$source.IndexOf('private int lastIsland', $start)
$optionalMethod=$source.Substring($start,$end-$start)
$start=$source.IndexOf('public void ReadWorld()')
$end=$source.IndexOf('public void WriteCensus(', $start)
$worldMethod=$source.Substring($start,$end-$start)
$start=$source.IndexOf('private static bool Prefix(')
$end=$source.IndexOf('internal sealed class GameReader', $start)
$inputMethod=$source.Substring($start,$end-$start).Trim()
# 去掉方法后 OverlayInputPatch 的类型闭合括号。
$inputMethod=$inputMethod.Substring(0,$inputMethod.LastIndexOf('}')).Trim()
$model=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'src/Model.cs'))
Add-Type -TypeDefinition ($model + @"
namespace Test { using System; using System.Collections.Generic; using KingdomAdvisor;
public class Position {public float x;public Position position=>this;}
public class ObjectState {public bool activeInHierarchy=true;}
public class Player {public int playerId;public bool hasLocalAuthority;public Position transform=new Position();public ObjectState gameObject=new ObjectState();public static implicit operator bool(Player p)=>p!=null;}
public class Rect {public float x,y,width=1,height=1;}
public class Camera {public bool isActiveAndEnabled=true;public Rect rect=new Rect();public static implicit operator bool(Camera c)=>c!=null;}
public class TestLog {public int warnings;public void LogWarning(string message){warnings++;}}
public class Plugin {public static Plugin Instance=new Plugin();public TestLog Log=new TestLog();}
public class Component {}
public class Kingdom {public int GetInstanceID()=>1;public static implicit operator bool(Kingdom k)=>k!=null;}
public class Level {public static bool isGenerating;public int cachedCurrentLevelSeed;public static implicit operator bool(Level l)=>l!=null;}
public class Game {public enum State{Playing,NetworkClientPlaying,Menu};public State state=State.NetworkClientPlaying;public int currentLand;public static implicit operator bool(Game g)=>g!=null;}
public class Managers {public Game game=new Game();public Kingdom kingdom=new Kingdom();public Level level;public static implicit operator bool(Managers m)=>m!=null;}
public class NetworkBigBoss {public static bool IsOnline=true;}
namespace Rewired {public class Player {}}
public class TestController {public int Records;public bool Block;public void RecordInput(int id,Rewired.Player input){Records++;}public bool Blocks(int id)=>Block;public bool ChordHeld(int id)=>false;}
public class AdvisorBehaviour {public static AdvisorBehaviour Active=new AdvisorBehaviour();public TestController Controller=new TestController();public bool InputAvailable=true;}
public class InputProbe {
 $inputMethod
 public static bool Pass(Player p)=>Prefix(p,new Rewired.Player());
}
public class RecoveryProbe {
 public Snapshot State=new Snapshot();public bool FailDetails=true;public int DetailCalls;
 readonly Dictionary<string,OptionalReadCircuit> optionalReads=new Dictionary<string,OptionalReadCircuit>();
 readonly Dictionary<int,Tuple<float,float>> Explored=new Dictionary<int,Tuple<float,float>>();
 $optionalMethod
 $playerMethod
 void ReadPlayerDetails(Player p,PlayerInfo info){DetailCalls++;if(FailDetails)throw new NullReferenceException("模拟联机详情未同步");info.Coins=25;}
 public void Read(Player p,Camera cam){State.Players.Clear();ReadPlayer(p,cam);}
 public void Reset(){optionalReads.Clear();}
}
public class WorldProbe {
 public Snapshot State=new Snapshot();public Managers managers=new Managers();public bool FailDetails=true;public int DetailCalls;
 readonly Dictionary<string,OptionalReadCircuit> optionalReads=new Dictionary<string,OptionalReadCircuit>();
 readonly Dictionary<int,Tuple<float,float>> Explored=new Dictionary<int,Tuple<float,float>>();
 string readContext="";int lastIsland=-1,lastSeed=int.MinValue;public Component[] detailBuildings=Array.Empty<Component>();public float nextBuildingScan;
 Managers GetManagers()=>managers;public void ResetOptionalReads()=>optionalReads.Clear();
 $optionalMethod
 $worldMethod
 void ReadWorldDetails(Managers m,Snapshot s){DetailCalls++;if(FailDetails)throw new NullReferenceException("模拟世界详情未同步");s.Day=12;}
}
}
"@)
$probe=[Test.RecoveryProbe]::new();$local=[Test.Player]@{playerId=2;hasLocalAuthority=$true};$local.transform.x=15
foreach($i in 1..5){$probe.Read($local,$null);if($probe.State.Players.Count -ne 1 -or !$probe.State.Players[0].Local){throw '详情异常导致本机界面入口丢失'};if($probe.State.Players[0].ViewW -ne 1){throw '缺少摄像机未回退到完整视口'}}
if($probe.DetailCalls -ne 3 -or [Test.Plugin]::Instance.Log.warnings -ne 3){throw '连续失败未在三次后停用，或错误未记录'}
$probe.FailDetails=$false;$probe.Read($local,$null);if($probe.DetailCalls -ne 3){throw '停用通道自行无限重试'}
$probe.Reset();$probe.Read($local,$null);if($probe.State.Players[0].Coins -ne 25){throw '显式重试未恢复详情'}
$remote=[Test.Player]@{playerId=1;hasLocalAuthority=$false};$before=$probe.DetailCalls;$probe.Read($remote,$null)
if($probe.DetailCalls -ne $before -or $probe.State.Players[0].Local){throw '查询远端详情或伪造本机控制权'}
$camera=[Test.Camera]::new();$camera.rect.width=0;$probe.Read($local,$camera);if($probe.State.Players[0].ViewW -ne 1){throw '零宽摄像机导致界面隐藏'}
$camera.rect.width=.5;$probe.Read($local,$camera);if($probe.State.Players[0].ViewW -ne .5){throw '有效分屏视口未保留'}
$camera.isActiveAndEnabled=$false;$probe.Read($local,$camera);if($probe.State.Players[0].ViewW -ne 1){throw '停用摄像机导致界面隐藏'}
$local.gameObject.activeInHierarchy=$false;$probe.Read($local,$camera);if($probe.State.Players.Count){throw '非活动角色进入界面'}
'通过：生产玩家读取与异常隔离、三次停用、显式恢复、远端隔离、缺失/零宽/停用摄像机、有效分屏及角色消失。'
$world=[Test.WorldProbe]::new()
foreach($i in 1..5){$world.ReadWorld();if(!$world.State.Playing -or !$world.State.Online -or $world.State.State -ne 'NetworkClientPlaying'){throw '世界详情异常阻止加入者基础游玩状态'}}
if($world.DetailCalls -ne 3){throw '世界详情未在三次失败后停用'}
$world.managers.game.state=[Test.Game+State]::Menu;$world.ReadWorld();if($world.State.Playing){throw '菜单被误判为游玩状态'}
$world.FailDetails=$false;$world.managers.game.state=[Test.Game+State]::NetworkClientPlaying;$world.ReadWorld()
if($world.State.Day -ne 12){throw '重连后可选世界详情未恢复'}
[Test.Level]::isGenerating=$true;$world.ReadWorld();if($world.State.Playing){throw '地图生成期间绕过安全门禁'}
[Test.Level]::isGenerating=$false;$world.managers=$null;$world.ReadWorld();if($world.State.Playing){throw '管理器缺失时仍沿用旧游玩状态'}
'通过：生产世界读取保留加入者基础状态、失败停用、退出及重连恢复、地图生成与管理器缺失门禁。'
$actor=[Test.Player]@{playerId=2;hasLocalAuthority=$false};[Test.AdvisorBehaviour]::Active.Controller.Block=$true
if(![Test.InputProbe]::Pass($actor) -or [Test.AdvisorBehaviour]::Active.Controller.Records){throw '远端输入被阻断或参与本机路由'}
$actor.hasLocalAuthority=$true;if([Test.InputProbe]::Pass($actor)){throw '本机面板焦点未阻断本机玩法输入'}
[Test.AdvisorBehaviour]::Active.InputAvailable=$false;if(![Test.InputProbe]::Pass($actor)){throw '插件故障后未释放玩法输入'}
'通过：生产输入挂钩保留远端输入、本机焦点阻断与故障后放行。'
