#Requires -Version 7.0
$ErrorActionPreference='Stop'
$model=Join-Path $PSScriptRoot 'src/ContinuousPurchaseModel.cs'
if(!(Test-Path -LiteralPath $model)){throw '长按连续购买状态模型尚未实现'}
Add-Type -Path $model
function Check($actual,$expected,$label){if($actual -ne $expected){throw "$label : $actual != $expected"}}
function Input([int]$phase=0,[bool]$ready=$true,[int]$selected=11){
 $i=[KingdomAdvisor.PurchaseInput]::new();$i.Enabled=$true;$i.Held=$true;$i.Allowed=$true;$i.SelectedTarget=$selected;$i.SupportedTarget=11;$i.TargetValid=$true;$i.CanPay=$ready;$i.Phase=[KingdomAdvisor.PurchasePhase]$phase;return $i
}
$s=[KingdomAdvisor.ContinuousPurchaseState]::new();$i=Input;$i.Enabled=$false;$i.Down=$true
Check ($s.Step($i).Override) $false '关闭时完全保留原生输入'
$i=Input;$i.Down=$true;$r=$s.Step($i);Check $r.Held $true '首次按下正常投币';Check $s.Target 11 '锁定目标'
$i=Input 2;$r=$s.Step($i);Check $r.Held $true '交易中保持原生按住';Check $r.Down $false '交易中不重复按下'
$i=Input 3;$r=$s.Step($i);Check $r.Held $false '完成后先经原生释放'
$r=$s.Step($i);Check $r.Held $false '尚未回到空闲时不能重启'
$i=Input 0;$r=$s.Step($i);Check $r.Held $true '回到空闲才继续';Check $r.Down $true '每轮只补一个按下边沿'
$i=Input 1;$r=$s.Step($i);Check $r.Down $false '下一帧不能重复边沿'
$i=Input 3 $false;$r=$s.Step($i);Check $r.Held $false '库存满后释放防丢币'
$i=Input 0 $false;1..20|ForEach-Object {Check ($s.Step($i).Held) $false '库存满持续等待'}
$i=Input 0;$r=$s.Step($i);Check $r.Down $true '同目标恢复条件后继续'
$i=Input 1;$i.Held=$false;$r=$s.Step($i);Check $r.Held $false '松手立即停止';Check $s.Target 0 '松手清理会话'
Check $r.CancelHolding $true '合成按下后立即松手须原生取消，不能变成短按丢币'
$i=Input;$i.Down=$true;[void]$s.Step($i);$i=Input 1;$i.Held=$false
Check ($s.Step($i).CancelHolding) $false '真实首次短按保留原生语义'
$i=Input;$i.Down=$true;[void]$s.Step($i);$i=Input 1 $true 12
Check ($s.Step($i).Held) $false '切换目标后不能自动投币';Check $s.AwaitingRelease $true '切目标锁定等待松手'
$i=Input;Check ($s.Step($i).Held) $false '回到原目标不得自动恢复'
$i.Held=$false;[void]$s.Step($i);$i=Input;$i.Down=$true;[void]$s.Step($i)
$i=Input 1;$i.Allowed=$false;$r=$s.Step($i);Check $r.Held $false '菜单或失焦取消';Check $r.CancelHolding $true '强制中断待投币状态不能触发短按丢币'
$i.Allowed=$true;Check ($s.Step($i).Held) $false '恢复焦点仍需释放'
$s.Reset();$i=Input;$i.Down=$true;[void]$s.Step($i)
Check ($s.AcceptSuccessor(99,22)) $false '拒绝非本目标升级事件'
Check ($s.AcceptSuccessor(11,22)) $true '接受原生证实后继';Check $s.Target 22 '升级后锁定新实例'
$i=Input 3 $false 0;$i.SupportedTarget=22;Check ($s.Step($i).Held) $false '升级替换短暂空目标保持等待'
$i=Input 0 $false 22;$i.SupportedTarget=22;Check ($s.Step($i).Held) $false '施工期间不得重复付费'
$i.CanPay=$true;Check ($s.Step($i).Down) $true '同后继施工完成继续'
$i=Input 0;$i.TargetValid=$false;Check ($s.Step($i).Held) $false '目标失效取消'
$a=[KingdomAdvisor.ContinuousPurchaseState]::new();$b=[KingdomAdvisor.ContinuousPurchaseState]::new();$i=Input;$i.Down=$true;[void]$a.Step($i)
Check $b.Target 0 '双人会话不共享';$a.Cancel();Check $a.AwaitingRelease $true '生命周期取消等待释放'
$s.Reset();$i=Input;$i.Down=$true;$i.SupportedTarget=0;Check ($s.Step($i).Override) $false '未支持对象维持原生'
$s.Reset();$i=Input;$i.Down=$true;[void]$s.Step($i)
$i=Input 4 $false;$r=$s.Step($i);Check $r.Held $false '原生资金不足退币阶段不续投';Check $s.AwaitingRelease $false '原生退币保留同目标等待'
$i=Input 0 $false;Check ($s.Step($i).Down) $false '退币完仍不足整笔价格时等待'
$i.CanPay=$true;Check ($s.Step($i).Down) $true '同目标原生退币完且金额足够才重新开始'
$s.Reset();$i=Input 1;$i.Down=$true;[void]$s.Step($i);Check $s.Target 11 '后置观察原生首轮Holding可建立同目标会话'
$i=Input 3;[void]$s.Step($i);$i=Input 0;Check ($s.Step($i).Down) $true '后置建立的首轮完成后可继续'
# 独立原生契约模拟：None 只接受按下边沿，Completed 延迟结算后自行回 None。
# 这里只模拟已反汇编确认的关键状态，不调用生产状态机计算期望金币数。
$s=[KingdomAdvisor.ContinuousPurchaseState]::new();$phase=0;$timer=0;$coins=50;$stock=0;$paid=0;$purchases=0;$starts=0;$drops=0
for($frame=0;$frame -lt 240;$frame++){
 if($frame -eq 100){$stock-=2}
 $i=Input $phase ($stock -lt 4 -and $coins -gt 0);$i.Down=$frame -eq 0;$i.Held=$frame -lt 200
 $r=$s.Step($i)
 if($r.CancelHolding -and $phase -eq 1){$phase=0}
 switch($phase){
  0 {if($r.Down){if($stock -lt 4 -and $coins -gt 0){$starts++;$phase=1;$timer=0}else{$drops++}}}
  1 {if($r.Held){$timer++;if($timer -ge 2){$phase=2;$timer=0;$paid=0}}else{$drops++;$phase=0}}
  2 {if($r.Held -and $coins -gt 0){$coins--;$paid++;if($paid -eq 3){$phase=3;$timer=0}}}
  3 {$timer++;if($timer -ge 2){$stock++;$purchases++;$phase=0}}
 }
 if($stock -gt 4){throw '自动续投突破原生库存'}
 if($frame -ge 200 -and $r.Down){throw '松手后仍启动交易'}
}
Check $purchases 6 '先补满四件，领取两件后只再买两件'
Check $starts 6 '每笔交易仅启动一次'
Check $coins 32 '六件三金币商品只扣十八金币'
Check $drops 0 '满库存或等待期间不向地面丢币'
'通过：连续购买状态机覆盖原生透传、完成释放再按、等待恢复、松手、目标隔离、焦点、升级后继及双人隔离；原生支付时序须实机验证。'
