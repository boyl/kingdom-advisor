#Requires -Version 7.0
$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $PSScriptRoot 'src/Model.cs')
function Check($value,$label){if(!$value){throw $label}}
function Sample([double]$hour,[double]$real,[double]$speed=1){
 $s=[KingdomAdvisor.ClockObservation]@{Hour=$hour;RealTime=$real;Day=1;SpeedSignature=$speed;Context='solo';Phase=[KingdomAdvisor.ClockPhase]::Day;PhaseStarts=[double[]]@(4,6,18,22);DayStart=5;DayEnd=21.5}
 # 先在旧版本执行同一回归；缺失新字段时仍进入旧生产算法并复现跳动。
 if($s.GetType().GetField('AuthoritativeRate')){$s.AuthoritativeRate=0.1*$speed}
 return $s
}
foreach($speed in @(1,2,4)){
 foreach($peak in @(0.04,0.08,0.48)){
  $tracker=[KingdomAdvisor.DayClockTracker]::new();$real=0.0;$hour=6.0;$next=0.0;$previous=$null;$increases=0;$maxIncrease=0.0
  for($i=0;$i -lt 600;$i++){
   $dt=if($i%120 -eq 80){$peak}elseif($i%120 -eq 81){0.008}else{1.0/60}
   $real+=$dt
   if($real -ge $next){
    $c=$tracker.Observe((Sample ([float]$hour) $real $speed));$next=$real+0.1
    if($null -ne $c.RemainingSeconds){
     $now=[Math]::Ceiling($c.RemainingSeconds)
     if($null -ne $previous -and $now -gt $previous){$increases++;$maxIncrease=[Math]::Max($maxIncrease,$now-$previous)}
     $previous=$now
    }
   }
   $hour+=0.1*$speed*[Math]::Min($dt,1.0/3)
  }
  Check ($increases -eq 0) "倒计时因帧波动增加：speed=$speed peak=$peak count=$increases max=$maxIncrease"
 }
}
$t=[KingdomAdvisor.DayClockTracker]::new();[void]$t.Observe((Sample 10 0));$a=$t.Observe((Sample 10.1 1));
$s=Sample 10.1 1.1;$s.Paused=$true;Check ($t.Observe($s).State.ToString() -eq 'Paused') '原生暂停'
[void]$t.Observe((Sample 10.1 2 2));$b=$t.Observe((Sample 10.2 2.5 2))
Check ($b.RemainingSeconds -lt $a.RemainingSeconds/2+1) '恢复变速立即采用新原生速率'
[void]$t.Observe((Sample 10.2 2.6 1));$slow=$t.Observe((Sample 10.21 2.7 1))
Check ($slow.RemainingSeconds -gt $b.RemainingSeconds) '减速时允许剩余现实时间合理增加'
$back=$t.Observe((Sample 9 2.8));Check ($back.ClockText -eq '09:00' -and $back.State.ToString() -eq 'Synchronizing') '保留真实向后校时并重置估计'
[void]$t.Observe((Sample 9.01 2.9));$stopped=$t.Observe((Sample 9.01 3.8))
Check ($stopped.State.ToString() -eq 'Stalled' -and $null -eq $stopped.RemainingSeconds) '有名义速率但实际时钟停滞不能伪造倒计时'
$s=Sample 9.1 4;$s.AuthoritativeRate=[double]::NaN;Check (!$t.Observe($s).Available) '无效原生速率拒绝'
'通过：3倍率×3类帧波动、暂停/恢复/减速、原生校时、停滞和无效速率。'
