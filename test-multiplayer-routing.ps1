#Requires -Version 7.0
$ErrorActionPreference='Stop'
$source=Get-Content (Join-Path $PSScriptRoot 'src/Plugin.cs') -Raw
$start=$source.IndexOf('public void ReadPlayers()')
$end=$source.IndexOf('private void ReadPlayer(', $start)
$method=$source.Substring($start,$end-$start)
# 执行生产 ReadPlayers 方法体；替身只提供原生对象与读取记录，不复制路由逻辑。
Add-Type -TypeDefinition @"
using System; using System.Collections.Generic;
public class Player { public bool hasLocalAuthority,active=true; public int id; public static implicit operator bool(Player p)=>p!=null; }
public class Camera { public string name; }
public class Game { public Camera _mainCameraComponent=new Camera{name="main"},_secondCameraComponent=new Camera{name="second"}; public static implicit operator bool(Game g)=>g!=null; }
public class Kingdom { public Player playerOne,playerTwo; public static implicit operator bool(Kingdom k)=>k!=null; }
public class Managers { public static bool IsP2Playing; public Kingdom kingdom=new Kingdom(); public Game game=new Game(); public static implicit operator bool(Managers m)=>m!=null; }
public class TestState { public bool Playing=true; public List<string> Players=new List<string>(); public List<int> ExploredRanges=new List<int>(); }
public class RoutingProbe {
 public Managers managers=new Managers(); public TestState State=new TestState(); public Dictionary<int,int> Explored=new Dictionary<int,int>();
 Managers GetManagers()=>managers;
 $method
 void ReadPlayer(Player p,Camera cam){if(!p||!p.active)return;State.Players.Add(p.id+":"+p.hasLocalAuthority+":"+cam.name);}
}
"@
$cases=@(
 @{Name='单人';Coop=$false;One=$true;Two=$null;Expected='1:True:main'},
 @{Name='房主';Coop=$true;One=$true;Two=$false;Expected='1:True:main,2:False:second'},
 @{Name='加入者且P2标志关闭';Coop=$false;One=$false;Two=$true;Expected='1:False:main,2:True:main'},
 @{Name='加入者且P2标志开启';Coop=$true;One=$false;Two=$true;Expected='1:False:main,2:True:main'},
 @{Name='本地双人';Coop=$true;One=$true;Two=$true;Expected='1:True:main,2:True:second'}
)
foreach($case in $cases){
 $probe=[RoutingProbe]::new();[Managers]::IsP2Playing=$case.Coop
 $probe.managers.kingdom.playerOne=[Player]@{id=1;hasLocalAuthority=$case.One}
 if($null -ne $case.Two){$probe.managers.kingdom.playerTwo=[Player]@{id=2;hasLocalAuthority=$case.Two}}
 $probe.ReadPlayers()
 if(($probe.State.Players -join ',') -ne $case.Expected){throw "$($case.Name)失败：$($probe.State.Players -join ',')"}
 $probe.State.Playing=$false;$probe.ReadPlayers();if($probe.State.Players.Count){throw '离开游戏后玩家列表未清空'}
}
'通过：生产玩家路由方法覆盖单人、房主、加入者两种P2标志、本地双人及退出清空。'
$probe=[RoutingProbe]::new();$probe.managers.kingdom.playerTwo=[Player]@{id=2;hasLocalAuthority=$true;active=$false}
$probe.ReadPlayers();if($probe.State.Players.Count){throw '非活动玩家不应进入界面列表'}
$probe.managers.kingdom.playerTwo=[Player]@{id=2;hasLocalAuthority=$false}
$probe.ReadPlayers();if(($probe.State.Players -join ',') -ne '2:False:second'){throw '不能把远端玩家冒认为本机'}
if($source -match 'if\(Managers.IsP2Playing&&k.playerTwo'){throw '地点说明仍错误依赖P2模式标志'}
'通过：非活动角色排除、远端控制权隔离、地点说明联动。'
