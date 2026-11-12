#Requires -Version 7.0
$ErrorActionPreference='Stop'
$game=Join-Path (Get-ItemProperty 'HKCU:/Software/Valve/Steam').SteamPath 'steamapps/common/Kingdom Two Crowns'
Add-Type -Path (Join-Path $game 'BepInEx/core/Mono.Cecil.dll')
$native=[Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $game 'BepInEx/interop/Assembly-CSharp.dll'))
$adapter=Get-Content (Join-Path $PSScriptRoot 'src/NativeStructureReader.cs') -Raw
$names=[regex]::Matches($adapter,'typeof\((\w+)\)')|ForEach-Object {$_.Groups[1].Value}|Where-Object {$_ -ne 'Payable'}|Select-Object -Unique
$classes=foreach($name in $names){
 $type=$native.MainModule.Types|Where-Object Name -eq $name
 if(!$type){throw "游戏程序集缺少组件 $name"}
 $cursor=$type;$payable=$false
 while($cursor){if($cursor.Name -eq 'Payable'){$payable=$true;break};$parent=$cursor.BaseType.Name;$cursor=$native.MainModule.Types|Where-Object Name -eq $parent}
 $base=if($payable){'Payable'}else{'UnityEngine.MonoBehaviour'}
 "public class $name : $base {}"
}
$body=Get-Content (Join-Path $PSScriptRoot 'src/Model.cs') -Raw
$adapter=[regex]::Replace($adapter,'(?m)^using [^;]+;\r?\n','').Replace('internal static class NativeStructureReader','public static class NativeStructureReader').Replace('internal static readonly Type[]','public static readonly Type[]')
$stubs=@'
namespace Il2CppInterop.Runtime {public static class Il2CppType {public static Type Of<T>()=>typeof(T);public static Type From(Type t)=>t;}}
namespace UnityEngine {
 public class Object {
  public static readonly List<Object> Scene=new();public static bool Fail;
  public static implicit operator bool(Object value)=>value!=null;
  public T TryCast<T>() where T:class=>this as T;
  public Type GetIl2CppType()=>GetType();
  public static Object[] FindObjectsOfType(Type t){if(Fail)throw new Exception("scene changed");return Scene.Where(o=>t.IsAssignableFrom(o.GetType())).ToArray();}
 }
 public class Component:Object {public GameObject gameObject;}
 public class MonoBehaviour:Component {}
 public class GameObject:Object {
  static int serial;readonly int id=++serial;public bool activeInHierarchy=true;public string name="";
  public List<Component> Components=new();public List<GameObject> Children=new();
  public int GetInstanceID()=>id;
  public Component[] GetComponents(Type t)=>Components.Where(c=>t.IsAssignableFrom(c.GetType())).ToArray();
  public void Add(Component c){c.gameObject=this;Components.Add(c);Scene.Add(c);}
 }
}
public class Payable:UnityEngine.MonoBehaviour {}
public class PayableComponent:Payable {}
public class Unrelated:UnityEngine.MonoBehaviour {}
'@
Add-Type -TypeDefinition ("using Il2CppInterop.Runtime;using UnityEngine;`n"+$body+"`n"+$adapter+"`n"+$stubs+"`n"+($classes -join "`n"))
function Check($ok,$why){if(!$ok){throw $why}}
$registered=[KingdomAdvisor.NativeStructureReader]::KnownTypes.Name | Sort-Object
$mapped=[KingdomAdvisor.Catalog]::StructureTypes|Sort-Object
Check (!(Compare-Object $registered $mapped)) '组件映射与扫描入口不一致'
$dock=[UnityEngine.GameObject]::new();$dock.name='Payable';$dock.Add([PayableComponent]::new());$dock.Add([HermesDock]::new())
Check ([KingdomAdvisor.NativeStructureReader]::Resolve($dock).Key -eq 'hermesdock') '通用付费组件遮盖专用组件'
$parent=[UnityEngine.GameObject]::new();$parent.name='Hermes_Dock';$parent.Add([Unrelated]::new());$parent.Children.Add($dock)
Check ($null -eq [KingdomAdvisor.NativeStructureReader]::Resolve($parent)) '不允许通过邻近子物体或名称伪造组件身份'
$inactive=[UnityEngine.GameObject]::new();$inactive.activeInHierarchy=$false;$inactive.Add([HephaestusForge]::new())
$puzzle=[UnityEngine.GameObject]::new();$puzzle.Add([LokiPuzzleStaff]::new())
$cage=[UnityEngine.GameObject]::new();$cage.Add([PersephoneCage]::new());$cage.Add([MtOlympusStatue]::new())
$discovered=@([KingdomAdvisor.NativeStructureReader]::Discover())
Check ($discovered.Count -eq 2) '扫描应仅包含两个活动非Payable设施，并按本体去重'
Check (@($discovered|Where-Object gameObject -eq $dock).Count -eq 1) '带通用Payable的专用组件不可遗漏'
Check (@($discovered|Where-Object gameObject -eq $cage).Count -eq 1) '无付费交互任务设施遗漏/重复'
[UnityEngine.Object]::Scene.Clear()
Check (@([KingdomAdvisor.NativeStructureReader]::Discover()).Count -eq 0) '退出场景后扫描不应沿用旧对象'
'通过：真实组件继承契约、45组件注册一致、同本体身份、父子隔离、活动过滤、去重、付费扫描分工及场景退出。'
