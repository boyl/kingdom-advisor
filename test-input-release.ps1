#Requires -Version 7.0
$ErrorActionPreference='Stop'
Add-Type -TypeDefinition ([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'src/MenuState.cs')))
function Check($actual,$expected,$label){if($actual -ne $expected){throw $label}}
$guard=[KingdomAdvisor.InputReleaseGuard]::new()
$guard.Arm(0,$false,$true,$true)
Check ($guard.Contains(0)) $false '键鼠关闭不得等待旁置手柄回中'
$guard.Arm(0,$true,$true,$true)
Check ($guard.Contains(0)) $true '手柄关闭仍按住时阻止输入穿透'
Check ($guard.Contains(1)) $false '双人输入隔离'
$guard.Observe(0,$true,$true)
Check ($guard.Contains(0)) $true '保持按住不能提前释放'
$guard.Observe(0,$true,$false)
Check ($guard.Contains(0)) $false '松开后立即释放'
$guard.Arm(0,$true,$true,$true)
$guard.Observe(0,$false,$true)
Check ($guard.Contains(0)) $false '拔出或读取失败不得永久锁住'
$guard.Arm(0,$true,$true,$true)
$guard.Arm(0,$false,$true,$true)
Check ($guard.Contains(0)) $false '切换到键鼠关闭清除旧锁'
$guard.Arm(0,$true,$true,$false)
Check ($guard.Contains(0)) $false '已松开的关闭不加锁'
$guard.Arm(0,$true,$false,$true)
Check ($guard.Contains(0)) $false '不可用设备不加锁'
$guard.Arm(0,$true,$true,$true);$guard.Clear()
Check ($guard.Contains(0)) $false '会话结束复位'
'通过：键鼠、手柄按住与释放、设备失败、双人和生命周期。'
