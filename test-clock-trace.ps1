#Requires -Version 7.0
$ErrorActionPreference='Stop'
Add-Type -Path @((Join-Path $PSScriptRoot 'src/Model.cs'),(Join-Path $PSScriptRoot 'src/ClockTrace.cs'))
$trace=[KingdomAdvisor.ClockTrace]::new()
$before=[Threading.Thread]::CurrentThread.CurrentCulture
try{
 [Threading.Thread]::CurrentThread.CurrentCulture=[Globalization.CultureInfo]::GetCultureInfo('de-DE')
 foreach($i in 0..604){$trace.Record(($i+0.25),2,[KingdomAdvisor.DayClockSnapshot]@{Hour=12.5;State=[KingdomAdvisor.ClockState]::Running;RemainingSeconds=100.5})}
 $rows=@($trace.Lines())
 if($rows.Count -ne 601 -or !$rows[1].StartsWith("5.25`t2`tRunning`t12.5`t") -or $rows[-1] -notmatch '100\.5'){throw '固定容量或跨区域小数格式错误'}
 $trace.Record(606,0,[KingdomAdvisor.DayClockSnapshot]@{State=[KingdomAdvisor.ClockState]::Paused})
 if(@($trace.Lines())[-1] -notmatch "Paused.*`t`tFalse$"){throw '暂停不得伪造剩余秒数'}
}finally{[Threading.Thread]::CurrentThread.CurrentCulture=$before}
'通过：时钟证据容量上限、旧样本淘汰、区域无关数值与暂停空值。'
