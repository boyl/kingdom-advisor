#Requires -Version 7.0
param([string]$GamePath)
$ErrorActionPreference='Stop'
if(!$GamePath){$steamPath=(Get-ItemProperty 'HKCU:/Software/Valve/Steam').SteamPath;$GamePath=Join-Path $steamPath 'steamapps/common/Kingdom Two Crowns'}
# 使用实际构建的 Settings 和实际 BepInEx 配置库；只写仓库 work 下本轮创建的测试文件。
[void][Reflection.Assembly]::LoadFrom((Join-Path $GamePath 'BepInEx/core/BepInEx.Core.dll'))
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot 'package/BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll'))
$type=$assembly.GetType('KingdomAdvisor.Settings',$true)
$ctor=$type.GetConstructors()[0]
$work=Join-Path $PSScriptRoot 'work';[void](New-Item -ItemType Directory -Path $work -Force)
$path=Join-Path $work ('clock-config-'+[guid]::NewGuid().ToString('N')+'.cfg')
function Check($ok,$label){if(!$ok){throw $label}}
function ReadSettings {
 $script:config=[BepInEx.Configuration.ConfigFile]::new($path,$true)
 $arguments=[object[]]::new(1);$arguments[0]=$config
 $script:settings=$ctor.Invoke($arguments)
}
function Entry($name){$type.GetField($name).GetValue($settings)}
try {
 [IO.File]::WriteAllText($path,"[界面]`n语言 = 2`n字号 = 24`n`n[地图筛选]`n隐藏类型 = wall,tower`n细分迁移完成 = true`n")
 ReadSettings
 Check (Entry 'ShowClock').Value '旧配置首次升级默认显示钟点'
 Check ((Entry 'UiLanguage').Value -eq 2 -and (Entry 'FontSize').Value -eq 24) '不得重置旧语言/字号'
 Check ((Entry 'HiddenMapTypes').Value -eq 'wall,tower') '不得重置旧筛选'
 (Entry 'ShowClock').Value=$false;$config.Save();ReadSettings
 Check (!(Entry 'ShowClock').Value) '重载后保持关闭'
 Check ((Entry 'UiLanguage').Value -eq 2 -and (Entry 'HiddenMapTypes').Value -eq 'wall,tower') '新设置不得覆盖旧偏好'
 (Entry 'ShowClock').Value=$true;$config.Save();ReadSettings
 Check (Entry 'ShowClock').Value '重载后保持开启'
 $groups=$assembly.GetType('KingdomAdvisor.OptionGroups',$true).GetField('Items').GetValue($null)
 $items=@($groups[4] | Where-Object {$_.Id.ToString() -eq 'Clock'})
 Check ($items.Count -eq 1 -and $items[0].Choices.Count -eq 2) '移动与时间中时钟开关可达且唯一'
 '通过：实际候选DLL + BepInEx ConfigFile，旧配置升级默认开启、关闭/开启落盘重载、既有偏好保留和设置入口。'
} finally { if(Test-Path -LiteralPath $path){Remove-Item -LiteralPath $path} }
