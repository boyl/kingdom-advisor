#Requires -Version 7.0
$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $PSScriptRoot 'src/Model.cs')
foreach($pair in @(@(0,0,$true),@(10,11,$true),@(10,11.5,$true),@(10,11.51,$false),@(10,16,$false),@(-10,-16,$false))){
 if([KingdomAdvisor.TargetProximity]::Contains([float]$pair[0],[float]$pair[1]) -ne $pair[2]){throw ('目标距离门禁失败: '+($pair -join ','))}
}
'通过：站在目标处、近距离容差、离开边界、旧六单位误命中及负坐标。'
