#Requires -Version 7.0
$ErrorActionPreference='Stop'
Add-Type -Path @((Join-Path $PSScriptRoot 'src/Model.cs'),(Join-Path $PSScriptRoot 'src/ClockPresentation.cs'),(Join-Path $PSScriptRoot 'src/Localization.cs'))
function Check($ok,$label){if(!$ok){throw $label}}
function Near($actual,$expected,$label){Check ([Math]::Abs($actual-$expected) -lt 0.02) "$label : $actual != $expected"}
function Sample([double]$hour,[double]$real,[int]$day=1,[double]$speed=1,[bool]$paused=$false,[string]$context='solo'){
 [KingdomAdvisor.ClockObservation]@{Hour=$hour;RealTime=$real;Day=$day;SpeedSignature=$speed;Paused=$paused;Context=$context;Phase=[KingdomAdvisor.ClockPhase]::Day;PhaseStarts=[double[]]@(4,6,18,22);DayStart=5;DayEnd=21.5}
}
$t=[KingdomAdvisor.DayClockTracker]::new()
$c=$t.Observe((Sample 10 0))
Check ($c.State.ToString() -eq 'Synchronizing') '首次读数不得虚报秒数'
$c=$t.Observe((Sample 10.1 1))
Near $c.RemainingSeconds 114 '按原生速率估计到21:30'
Check (!$c.UntilDawn) '白天应倒计时入夜'
Near $c.Progress 0.2541667 '从黎明开始的全天进度'
Check ($c.ClockText -eq '10:06') 'HH:mm 转换'
foreach($speed in @(1,2,4)){
 $t.Reset();[void]$t.Observe((Sample 10 0 1 $speed));$c=$t.Observe((Sample (10+0.1*$speed) 1 1 $speed))
 Near $c.RemainingSeconds ((11.5-0.1*$speed)/(0.1*$speed)) "$speed 倍现实秒数"
}
$c=$t.Observe((Sample 10.4 1.1 1 0 $true));Check ($c.State.ToString() -eq 'Paused' -and $null -eq $c.RemainingSeconds) '暂停不继续倒计时'
$c=$t.Observe((Sample 10.4 2 1 1));Check ($c.State.ToString() -eq 'Synchronizing') '恢复先清除旧倍率'
$c=$t.Observe((Sample 10.5 3));Near $c.RemainingSeconds 110 '恢复正常倍率'
$c=$t.Observe((Sample 10.5 4));Check ($c.State.ToString() -eq 'Stalled') '未报告暂停但时间停滞'
$c=$t.Observe((Sample 10.6 4.1));Check ($c.State.ToString() -eq 'Synchronizing') '停滞后重新采样'
$t.Reset();[void]$t.Observe((Sample 23.95 0));$c=$t.Observe((Sample 0.05 1 2));Near $c.RemainingSeconds 49.5 '午夜不产生负数或24小时尖峰'
Check ($c.UntilDawn) '午夜指向黎明'
$c=$t.Observe((Sample 5 1.1 2));Check (!$c.UntilDawn -and $c.State.ToString() -eq 'Synchronizing') '黎明整点切目标并丢弃跳时速率'
$c=$t.Observe((Sample 21.5 1.2 2));Check ($c.UntilDawn) '入夜整点切黎明'
$c=$t.Observe((Sample 12 1.3 2));Check ($c.State.ToString() -eq 'Synchronizing') '向后校时清除估计'
$c=$t.Observe((Sample 12.01 1.4 2 1 $false 'joiner'));Check ($c.State.ToString() -eq 'Synchronizing') '换角色/场景清除历史'
foreach($phase in @('Dawn','Day','Evening','Night')){
 $s=Sample 12 2;$s.Phase=[KingdomAdvisor.ClockPhase]::$phase;$c=$t.Observe($s);Check ($c.Phase.ToString() -eq $phase) "原生阶段 $phase"
}
foreach($bad in @([double]::NaN,[double]::PositiveInfinity,-1,25)){
 $c=$t.Observe((Sample $bad 3));Check (!$c.Available) "无效时间 $bad"
}
$s=Sample 12 4;$s.PhaseStarts=[double[]]@(4,3,18,22);Check (!$t.Observe($s).Available) '乱序边界拒绝'
$s=Sample 12 5;$s.Phase=[Enum]::ToObject([KingdomAdvisor.ClockPhase],99);Check (!$t.Observe($s).Available) '未知阶段不能默认白昼'
$s=Sample 12 6;$s.DayStart=$s.DayEnd;Check (!$t.Observe($s).Available) '零长昼夜拒绝'
$t.Reset();[void]$t.Observe((Sample 10 0));[void]$t.Observe((Sample 10.1 1));$c=$t.Observe((Sample 10.2 10));Check ($c.State.ToString() -eq 'Synchronizing') '长读取间隔重采样'
$t.Reset();[void]$t.Observe((Sample 10 0));[void]$t.Observe((Sample 10.1 1));$c=$t.Observe((Sample 10.11 1.1 1 2));Check ($null -eq $c.RemainingSeconds) '变速立刻丢弃上一速度'
'通过：原生阶段、现实速率、暂停恢复、午夜、昼夜边界、停滞、校时、场景和无效输入。'
$t.Reset();[void]$t.Observe((Sample 10 0));[void]$t.Observe((Sample 10.1 1));$c=$t.Observe((Sample 10.31 1.1))
Check ($c.State.ToString() -eq 'Synchronizing') '小幅正向网络校时不能冒充21倍速'
$t.Reset();[void]$t.Observe((Sample 10 0));$c=$t.Observe((Sample 10.1 1));$before=$c.RemainingSeconds
$c=$t.Observe((Sample 10.1 1.2));Near $c.RemainingSeconds $before '原生时钟不动时不自行外推'
$s=Sample 23 2;$s.PhaseStarts=[double[]]@(22,0,12,18);$s.DayStart=22;$s.DayEnd=10;$c=$t.Observe($s)
Check ($c.Available -and !$c.UntilDawn) '跨午夜的原生白昼与阶段边界'
Near $c.Progress (1.0/24) '跨午夜四阶段条'
$s=Sample 10.25 3;$s.Phase=[KingdomAdvisor.ClockPhase]::Day;$t.Reset();$c=$t.Observe($s)
Check ([KingdomAdvisor.ClockPresentation]::Header($c,$true) -eq '白昼 · 游戏时间 10:15') '时钟明确显示游戏时间'
Check ([KingdomAdvisor.ClockPresentation]::Header($c,$false) -eq '白昼') '关时钟保留阶段'
Check ([KingdomAdvisor.ClockPresentation]::Countdown($c) -eq '距入夜 · 正在同步') '关闭钟点不隐藏倒计时'
foreach($state in @('Synchronizing','Running','Paused','Stalled','Unavailable')){
 $c.State=[KingdomAdvisor.ClockState]::$state;$c.Client=$true;$c.RemainingSeconds=90
 $text=[KingdomAdvisor.ClockPresentation]::Countdown($c)
 Check ($text -notmatch 'NaN|Infinity|00:00') "状态不能伪造数值 $state"
 $english=[KingdomAdvisor.Localization]::Translate($text,$true)
 Check ($english -notmatch '[\u4e00-\u9fff]') "状态完整英译 $state"
}
$c.State=[KingdomAdvisor.ClockState]::Running;$c.RemainingSeconds=90
Check ([KingdomAdvisor.Localization]::Translate([KingdomAdvisor.ClockPresentation]::Countdown($c),$true) -eq 'Nightfall in ~ 1m 30s (real time)') '现实倒计时英文语义'
[KingdomAdvisor.Localization]::English=$false
Check ([KingdomAdvisor.ClockPresentation]::Countdown($c) -eq '距入夜约 1分30秒（现实时间）') '现实倒计时明确分钟和秒'
'通过：小幅校时、无外推、跨午夜配置、钟点开关及全部状态双语。'
