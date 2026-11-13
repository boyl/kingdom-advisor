#Requires -Version 7.0
$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $PSScriptRoot 'src/ContinuousPurchaseTrace.cs')
$trace=[KingdomAdvisor.ContinuousPurchaseTrace]::new(3)
function Event([int]$frame,[int]$phase,[int]$player=0,[int]$stage=0){$e=[KingdomAdvisor.PurchaseTraceEvent]::new();$e.Frame=$frame;$e.Player=$player;$e.Stage=[KingdomAdvisor.PurchaseTraceStage]$stage;$e.Before=$phase;$e.After=$phase;$e.Held=$true;return $e}
function Check($condition,$why){if(!$condition){throw $why}}
$trace.ObserveCall($true,$true,$false);$trace.ObserveAfter()
Check ($trace.Calls -eq 1 -and $trace.HeldCalls -eq 1 -and $trace.DownCalls -eq 1 -and $trace.TouchCalls -eq 0 -and $trace.AfterCalls -eq 1) '原生调用与按键计数'
Check ($trace.Record((Event 1 0))) '首个状态应记录'
1..100 | ForEach-Object {Check (!$trace.Record((Event $_ 0))) '同状态连续帧必须去重'}
Check ($trace.Record((Event 102 0 0 1))) '前后原生阶段分别记录'
Check (!$trace.Record((Event 103 0))) '交替原生阶段不得导致逐帧重复'
Check ($trace.Record((Event 104 0 1))) '本地双人隔离'
Check ($trace.Record((Event 105 1))) '状态迁移记录'
$s=$trace.Snapshot();Check ($trace.Count -eq 3 -and $s[0].Frame -eq 102 -and $s[2].Frame -eq 105) '环形容量与时间顺序'
$e=Event 106 1;$e.Reason=[KingdomAdvisor.PurchaseTraceReason]::OutsideRange;$e.SelectedName="Tower`t6`n";$e.Distance=1.2;$e.Limit=.5
Check ($trace.Record($e)) '早退原因变化应记录';Check ($trace.Lines()[-1].Split("`t").Length -eq 20) '日志字符串清理及列数'
$snapshot=$trace.Snapshot();$snapshot[0].Frame=999;Check ($trace.Snapshot()[0].Frame -ne 999) '导出快照不能修改内部缓存'
'通过：原生调用计数、逐阶段去重、双人隔离、128以内固定容量、顺序及导出；不代表实际原生输入验收。'
