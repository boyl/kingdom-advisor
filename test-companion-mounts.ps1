#Requires -Version 7.0
$ErrorActionPreference='Stop'
Add-Type -Path @((Join-Path $PSScriptRoot 'src/CompanionMountModel.cs'),(Join-Path $PSScriptRoot 'src/CompanionMountWire.cs'),(Join-Path $PSScriptRoot 'src/CompanionInput.cs'))
function Assert($value,$label){if(!$value){throw $label}}
$state=[KingdomAdvisor.CompanionMountState]::new()
Assert (!$state.Begin(0,$true,$false,$false)) '原生坐骑不得发动'
$state.Select([KingdomAdvisor.CompanionMountKind]::Cat)
Assert (!$state.Begin(0,$true,$true,$false)) '联机不得写入'
Assert (!$state.Begin(0,$true,$false,$true)) '暂停不得发动'
Assert (!$state.Begin(0,$false,$false,$false)) '未进游戏不得发动'
Assert ($state.Begin(0,$true,$false,$false)) '猫首次发动'
Assert ($state.ClaimHit(123)) '首次命中'
Assert (!$state.ClaimHit(123)) '同次技能不得重复命中'
Assert (!$state.Step(.79)) '抛投尚未结束'
Assert ($state.Step(.8)) '抛投在边界结束'
Assert ($state.Cooldown -eq 0.5) '猫冷却0.5秒'
Assert ($state.Begin(1,$true,$false,$false)) '猫动作结束可再次发动'
$state.Select([KingdomAdvisor.CompanionMountKind]::Dog)
Assert (!$state.Begin(1.49,$true,$false,$false)) '切换不得洗掉冷却'
Assert ($state.Begin(1.5,$true,$false,$false)) '冷却边界允许发动'
Assert (!$state.Step(3.49)) '冲锋持续2秒'
Assert ($state.Step(3.5)) '冲锋结束'
Assert ([KingdomAdvisor.CompanionMountState]::InRadius(7,0,0,0,7)) '范围边界包含'
Assert (![KingdomAdvisor.CompanionMountState]::InRadius(7.01,0,0,0,7)) '范围外排除'
Assert (![KingdomAdvisor.CompanionMountState]::InRadius(0,8,0,0,7)) '飞行高度限制'
$state.Reset()
Assert ($state.Kind -eq 'Native' -and !$state.Active -and $state.ReadyAt -eq 0) '离局清理'
'通过：离局、原生、联机、暂停门禁；猫狗持续与冷却边界；防切换刷新；单次命中；二维范围。'
$state.Select([KingdomAdvisor.CompanionMountKind]::Cat)
Assert ($state.Begin(1,$true,$true,$false,$true)) '联机主机可发动'
Assert ($state.ClaimHit(45)) '主机一次命中'
Assert (!$state.ClaimHit(45)) '主机不能重复伤害'
$state.Synchronize([KingdomAdvisor.CompanionMountKind]::Dog,100,7,1,$true)
Assert ($state.Active -and $state.ReadyAt -eq 107 -and $state.StartedAt -eq 99) '客户端复现主机状态'
Assert (!$state.Begin(100,$true,$true,$false,$false)) '客户端不得自行施法'
$slot=$null
Assert ([KingdomAdvisor.CompanionMountWire]::TrySlot('2,23,7.5,0.5',[ref]$slot)) '有效同步槽'
Assert ($slot.Kind -eq 2 -and $slot.Serial -eq 23 -and $slot.Cooldown -eq 7.5 -and $slot.Age -eq .5) '独立预期值解析'
foreach($bad in @('3,1,0,0','1,-1,0,0','1,1,NaN,0','1,1,Infinity,0','1,1,0,NaN','1,1,12.01,0','1,1,0,-1','1,1,0,600.01','1,1,0','1,1,0,0,0')){Assert (![KingdomAdvisor.CompanionMountWire]::TrySlot($bad,[ref]$slot)) "拒绝非法同步：$bad"}
'通过：联机主机权威、客户端复制、同步值独立预期、非法字段与非有限数值拒绝。'
$once=[KingdomAdvisor.CompanionMountState]::new()
$once.Select([KingdomAdvisor.CompanionMountKind]::Dog)
Assert ($once.Begin(100,$true,$false,$false)) '单次冲锋开始'
Assert ($once.Step(102)) '冲锋到期只产生一次结束'
Assert (!$once.Step(102.1)) '后续帧不得重复结束'
Assert (!$once.Active -and !$once.ClaimHit(99)) '结束后不能继续冲锋伤害'
Assert ($once.Cooldown -eq 0.5) '猫狗统一0.5秒冷却'
Assert (!$once.Step(120) -and !$once.Active) '冷却结束不自动重新发动'
'通过：单次发动、到期结束、冷却结束无自动重发。'
$input=[KingdomAdvisor.CompanionInput]::new()
Assert ($input.Step(0,$true,$false,$true)) '静止按原生冲刺键发动一次'
Assert (!$input.Step(0,$true,$false,$true)) '持续按住不连发'
Assert (!$input.Step(0,$false,$false,$true)) '松开不发动'
Assert (!$input.Step(1,$true,$false,$true)) '移动单按保留冲刺'
Assert ($input.Step(1,$true,$true,$true)) '移动双击发动一次'
Assert (!$input.Step(1,$true,$true,$true)) '同一双击信号不重复'
$blocked=[KingdomAdvisor.CompanionInput]::new()
Assert (!$blocked.Step(0,$true,$false,$false)) '面板阻断技能'
Assert (!$blocked.Step(0,$true,$false,$true)) '关闭面板仍按住不误发动'
'通过：原生静止按键、移动双击、按住不连发与面板阻断；键鼠和手柄共用同一信号契约。'