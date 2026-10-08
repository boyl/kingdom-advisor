#Requires -Version 7.0
param([string]$GamePath)
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$approval=Get-Content -LiteralPath (Join-Path $root 'release-acceptance.json') -Raw | ConvertFrom-Json
$dll=Join-Path $root 'package/BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll'
if((Get-FileHash -LiteralPath $dll).Hash -ne $approval.dllHash){throw '发布负载已改变，验收失效。'}
foreach($item in $approval.sourceHashes.PSObject.Properties){if((Get-FileHash -LiteralPath (Join-Path $root $item.Name)).Hash -ne $item.Value){throw "源码已改变，验收失效：$($item.Name)"}}
$pwsh=(Get-Command pwsh.exe -CommandType Application | Select-Object -First 1).Source
foreach($script in @('test.ps1','test-assist.ps1','test-input-layout.ps1','test-map.ps1','test-map-layout.ps1','test-localization.ps1','test-population.ps1','test-population-role.ps1','test-target-proximity.ps1','audit.ps1')){
    & $pwsh -NoProfile -File (Join-Path $root $script)
    if($LASTEXITCODE -ne 0){throw "自动门禁失败：$script"}
}
$pending=@($approval.manual.PSObject.Properties | Where-Object Value -ne 'passed')
if($pending.Count){throw ('人工验收未完成，禁止发布：'+(($pending | ForEach-Object Name)-join ', '))}
if(!$GamePath){$steamPath=(Get-ItemProperty 'HKCU:/Software/Valve/Steam').SteamPath;$GamePath=Join-Path $steamPath 'steamapps/common/Kingdom Two Crowns'}
$installed=Join-Path $GamePath 'BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll'
if((Get-FileHash -LiteralPath $installed).Hash -ne $approval.dllHash){throw '已安装副本不是已验收制品。'}
$runtime=Get-Content -LiteralPath (Join-Path $GamePath 'BepInEx/config/KingdomAdvisor/runtime.txt')
if($runtime -notcontains ('version='+$approval.version) -or $runtime -notcontains 'faulted=False' -or $runtime -notcontains 'movementLeakFrames=0'){throw '运行版本、故障或输入隔离门禁失败。'}
Add-Type -Path (Join-Path $root 'src/Localization.cs')
$values=@{};foreach($line in $runtime){$parts=$line.Split('=',2);if($parts.Length -eq 2){$values[$parts[0]]=$parts[1]}}
$expected=if([KingdomAdvisor.Localization]::IsEnglish([int]$values.languagePreference,$values.languageCode,'')){'en'}else{'zh-CN'}
if($values.displayLanguage -ne $expected){throw '语言解析与运行时显示不一致，或诊断预览未结束。'}
if($values.configuredSpeed -eq '1' -and $values.actualTimeScale -ne '1'){throw '正常速度配置与游戏倍率不一致。'}
'发布门禁通过；远端源码与项目身份必须在上传前另行验证。'
