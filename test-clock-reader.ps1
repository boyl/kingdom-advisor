#Requires -Version 7.0
$ErrorActionPreference='Stop'
# 编译原生适配器与纯模型；原生替身只提供明确读契约，不复制任何时间计算。
$sources=@('Model.cs','NativeClockReader.cs','ClockTrace.cs') | ForEach-Object {[IO.File]::ReadAllText((Join-Path $PSScriptRoot "src/$_"))}
$body=($sources | ForEach-Object {[regex]::Replace($_,'(?m)^using [^;]+;\r?\n','')}) -join "`n"
$body=$body.Replace('internal sealed class NativeClockReader','public sealed class NativeClockReader')
$plugin=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'src/Plugin.cs'))
$start=$plugin.IndexOf('public void ReadClock(double realTime)');$end=$plugin.IndexOf('public void ResetOptionalReads()', $start)
$readClock=$plugin.Substring($start,$end-$start)
$start=$plugin.IndexOf('private void ReadOptional(string channel,Action read)');$end=$plugin.IndexOf('private int lastIsland=', $start)
$readOptional=$plugin.Substring($start,$end-$start)
$stubs=@"
namespace UnityEngine { public static class Time { public static float timeScale=1; } }
public class NativeObject {public static implicit operator bool(NativeObject value)=>value!=null;}
public class Managers:NativeObject {public static Managers _Inst;public Game game=new();public Director director=new();public Level level=new();}
public class Game:NativeObject {public enum State {Menu,Playing,NetworkClientPlaying};public State state=State.Playing;public int currentLand;}
public class Level:NativeObject {public static bool isGenerating;public int cachedCurrentLevelSeed=42;}
public enum DayPhase {Dawn,Day,Evening,Night}
public class TimesOfDay {public readonly float dawnStart=4,dayStart=6,eveningStart=18,nightStart=22;public DayPhase Phase=DayPhase.Day;public float LastArgument;public DayPhase GetDayPhase(float hour){LastArgument=hour;return Phase;}}
public class Director:NativeObject {
 public float Hour=10,Modifier=1,_gameHoursPerSecond=0.1f;public bool Pause,Fail;public int Day=7,Id=99;public TimesOfDay Times=new();
 public int GetInstanceID()=>Id;public float currentTime=>Hour;public int TotalDaysInReign=>Day;
 public bool IsTimePaused=>Pause;public float ClockSpeedModifier=>Modifier;
 public float dayStart=>5;public float dayEnd=>21.5f;
 public TimesOfDay CurrentTimesOfDay=>Fail?throw new InvalidOperationException("clock-unavailable"):Times;
}
namespace KingdomAdvisor {
 public class LogProbe {public int Warnings;public void LogWarning(string value){Warnings++;}}
 public class Plugin {public static Plugin Instance=new();public LogProbe Log=new();}
 public class ClockChannelProbe {
 public Snapshot State=new();private NativeClockReader clockReader=new();private ClockTrace clockTrace=new();
 private readonly Dictionary<string,OptionalReadCircuit> optionalReads=new();
 public void ResetOptionalReads()=>optionalReads.Clear();
 $readClock
 $readOptional
 }
}
"@
Add-Type -TypeDefinition ("using System;using System.Linq;using System.Collections.Generic;using System.Globalization;using System.Text.RegularExpressions;using UnityEngine;`n"+$body+"`n"+$stubs)
function Check($ok,$label){if(!$ok){throw $label}}
function Near($actual,$expected,$label){Check ([Math]::Abs($actual-$expected) -lt 0.02) "$label : $actual != $expected"}
foreach($role in @('Playing','NetworkClientPlaying')){
 $m=[Managers]::new();[Managers]::_Inst=$m;$m.game.state=[Game+State]::$role
 $reader=[KingdomAdvisor.NativeClockReader]::new();[void]$reader.Read(0)
 $m.director.Hour=10.1;$c=$reader.Read(1)
 Near $c.RemainingSeconds 114 "角色 $role 原生时钟"
 Check ($c.Client -eq ($role -eq 'NetworkClientPlaying')) "角色标志 $role"
 Check ($m.director.Times.LastArgument -eq $m.director.Hour) '精确原生阶段查询参数'
 # 游戏玩法21:30入夜；视觉22:00入夜，不能误用视觉阈值。
 $m.director.Hour=21.5;$c=$reader.Read(1.1);Check ($c.UntilDawn) '昼夜阈值来自Director'
 $m.director.Hour=10;$m.director.Pause=$true;$c=$reader.Read(1.2);Check ($c.State.ToString() -eq 'Paused') '原生暂停'
 $m.director.Pause=$false;[UnityEngine.Time]::timeScale=0;$c=$reader.Read(1.3);Check ($c.State.ToString() -eq 'Paused') '世界暂停'
 [UnityEngine.Time]::timeScale=1;[void]$reader.Read(2);$m.director.Hour=10.1;[void]$reader.Read(3)
 $m.game.currentLand++;$c=$reader.Read(3.1);Check ($c.State.ToString() -eq 'Synchronizing') '换岛清除速率'
 $m.level.cachedCurrentLevelSeed++;$c=$reader.Read(3.2);Check ($c.State.ToString() -eq 'Synchronizing') '同岛换档种子清除速率'
 $m.game.state=[Game+State]::Menu;$c=$reader.Read(4);Check (!$c.Available) '菜单清空'
 $m.game.state=[Game+State]::$role;$c=$reader.Read(5);Check ($c.State.ToString() -eq 'Synchronizing') '重新进入不沿用历史'
 [Level]::isGenerating=$true;Check (!$reader.Read(6).Available) '地图生成不读未就绪对象';[Level]::isGenerating=$false
}
$m=[Managers]::new();[Managers]::_Inst=$m;$probe=[KingdomAdvisor.ClockChannelProbe]::new()
$probe.ReadClock(0);$m.director.Hour=10.1;$probe.ReadClock(1);Check ($probe.State.Clock.Available) '通道读取成功'
$m.director.Fail=$true
foreach($i in 2..6){$probe.ReadClock($i);Check (!$probe.State.Clock.Available) '读取失败须清除旧倒计时'}
Check ([KingdomAdvisor.Plugin]::Instance.Log.Warnings -eq 3) '三次失败熔断'
$m.director.Fail=$false;$probe.ReadClock(7);Check (!$probe.State.Clock.Available) '熔断后不无界重试'
$probe.ResetOptionalReads();$probe.ReadClock(8);Check ($probe.State.Clock.State.ToString() -eq 'Synchronizing') '明确重试恢复且重新采样'
[Managers]::_Inst=$null;$probe.ReadClock(9);Check (!$probe.State.Clock.Available) '原生管理器缺失清空'
'通过：房主/单人与加入者的生产适配器、玩法/视觉阈值隔离、暂停、换岛读档、菜单/生成、异常隔离与熔断恢复。替身测试不等于两机联机验收。'
