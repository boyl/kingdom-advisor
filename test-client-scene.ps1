#Requires -Version 7.0
$ErrorActionPreference='Stop'
$source=Get-Content (Join-Path $PSScriptRoot 'src/Plugin.cs') -Raw
$a=$source.IndexOf('        private static void ReadClientPopulation(');$b=$source.IndexOf('        public void ReadEnemies()', $a)
$method=$source.Substring($a,$b-$a)
$types='Character','Hermit','Knight','Ninja','Pikeman','Berserker','Worker','Archer','Farmer','Beggar','StableKeeper','Fisher','Peasant'
$defs=($types | ForEach-Object {if($_ -eq 'Knight'){'public class Knight:Unit {public bool NeedsArmor;}'}else{"public class $_ :Unit {}"}}) -join "`n"
$model=Get-Content (Join-Path $PSScriptRoot 'src/Model.cs') -Raw
Add-Type -TypeDefinition ($model+@"
namespace Probe {using System;using System.Linq;using System.Collections.Generic;using KingdomAdvisor;
public class Obj {public int id;public bool activeInHierarchy=true;public int GetInstanceID()=>id;}
public class Unit {public Obj gameObject=new Obj();public Dictionary<Type,Unit> roles=new Dictionary<Type,Unit>();public bool isActiveAndEnabled=false;public static implicit operator bool(Unit u)=>u!=null;}
$defs
public class Reader {
public static List<Character> characters=new List<Character>();
static IEnumerable<T> NativeObjects<T>() where T:Unit => characters.OfType<T>();
static T NativeComponent<T>(Unit c) where T:Unit => c.roles.TryGetValue(typeof(T),out var r)?(T)r:null;
static bool ActiveComponent<T>(Unit c) where T:Unit {var r=NativeComponent<T>(c);return r!=null&&r.isActiveAndEnabled;}
$method
public static Snapshot Run(){var s=new Snapshot();ReadClientPopulation(s);return s;}
public static void Add(int id,string[] roles,bool active=true){var c=new Character();c.gameObject.id=id;c.gameObject.activeInHierarchy=active;foreach(var role in roles){var t=typeof(Reader).Assembly.GetType("Probe."+role);c.roles[t]=(Unit)Activator.CreateInstance(t);}characters.Add(c);}
}}
"@)
[Probe.Reader]::Add(1,@('Worker'));[Probe.Reader]::Add(1,@('Worker'))
[Probe.Reader]::Add(2,@('Ninja','Fisher','Peasant'));[Probe.Reader]::Add(3,@('Pikeman','Fisher','Peasant'))
[Probe.Reader]::Add(4,@('Archer'),$false);[Probe.Reader]::Add(5,@('Knight','Peasant'))
$s=[Probe.Reader]::Run()
if($s.Workers -ne 1 -or $s.Ninjas -ne 1 -or $s.Pikemen -ne 1 -or $s.Knights -ne 1 -or $s.Fishers -ne 0 -or $s.Peasants -ne 0 -or $s.Archers -ne 0){throw '客户端人口去重、职业优先级或停用实体过滤失败'}
[Probe.Reader]::characters.Clear();$s=[Probe.Reader]::Run();if($s.Workers -ne 0){throw '场景清空保留旧人口'}
$cases=@{Worker='Workers';Archer='Archers';Farmer='Farmers';Beggar='Beggars';StableKeeper='StableKeepers';Fisher='Fishers';Peasant='Peasants';Berserker='Berserkers';Ninja='Ninjas';Pikeman='Pikemen';Knight='Knights'}
foreach($role in $cases.Keys){
 [Probe.Reader]::characters.Clear();[Probe.Reader]::Add(10,@($role));$s=[Probe.Reader]::Run()
 if($s.($cases[$role]) -ne 1){throw "禁用 AI 的真实职业遗漏：$role"}
}
# 职业替换：旧实体回池，新的工人实体保留，即使两者 AI 都禁用。
[Probe.Reader]::characters.Clear();[Probe.Reader]::Add(20,@('Peasant'),$false);[Probe.Reader]::Add(21,@('Worker'))
$s=[Probe.Reader]::Run();if($s.Workers -ne 1 -or $s.Peasants -ne 0){throw '职业切换重复计数'}
[Probe.Reader]::characters.Clear();[Probe.Reader]::Add(22,@('Knight'));[Probe.Reader]::characters[0].roles[[Probe.Knight]].NeedsArmor=$true
$s=[Probe.Reader]::Run();if($s.Squires -ne 1 -or $s.Knights -ne 0){throw '侍从与骑士分类失败'}
'通过：禁用 AI 的全部职业、实体去重、派生职业、职业替换、侍从、停用实体、空场景。模拟测试，不代表双机实测。'
