#Requires -Version 7.0
param([string]$TestRoot)
$ErrorActionPreference='Stop'
if(-not $TestRoot){$TestRoot=Join-Path ([IO.Path]::GetTempPath()) ('KingdomAdvisor-installer-'+[guid]::NewGuid().ToString('N'))}
$TestRoot=[IO.Path]::GetFullPath($TestRoot)
New-Item -ItemType Directory -Force $TestRoot | Out-Null
$bundle=Join-Path $TestRoot '安装包 空格';$game=Join-Path $TestRoot '游戏目录 空格'
New-Item -ItemType Directory -Force $bundle,$game | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'install.ps1') -Destination $bundle
Set-Content -LiteralPath (Join-Path $game 'KingdomTwoCrowns.exe') 'fixture executable'
$paths=@('winhttp.dll','doorstop_config.ini','dotnet/coreclr.dll','BepInEx/core/BepInEx.Unity.IL2CPP.dll','BepInEx/core/Cpp2IL.Core.dll','BepInEx/core/LibCpp2IL.dll','BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll')
$files=@();foreach($path in $paths){$source=Join-Path (Join-Path $bundle 'payload') $path;New-Item -ItemType Directory -Force (Split-Path $source -Parent) | Out-Null;Set-Content -LiteralPath $source ('new '+$path);$files+=[pscustomobject]@{path=$path;sha256=(Get-FileHash $source).Hash}}
$manifest=[pscustomobject]@{schema=1;kind='complete';files=$files};$manifest | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $bundle 'manifest.json')
function Assert($condition,$message){if(-not $condition){throw "FAIL: $message"};Write-Output "PASS: $message"}
function Run([string[]]$Extra=@()) {& (Join-Path $PSHOME 'pwsh.exe') -NoProfile -File (Join-Path $bundle 'install.ps1') -GamePath $game -NoPause @Extra;return $LASTEXITCODE}
function LatestBackup {Get-ChildItem (Join-Path $game '.install-backups') -Directory | Where-Object { (Get-Content (Join-Path $_.FullName 'installation.json') -Raw | ConvertFrom-Json).status -eq 'installed' } | Sort-Object LastWriteTime -Descending | Select-Object -First 1 -ExpandProperty FullName}
function VerifyPayload {foreach($f in $files){Assert ((Get-FileHash (Join-Path $game $f.path)).Hash -eq $f.sha256) ('payload '+$f.path)}}
$code=@(Run)[-1];Assert ($code -eq 0) 'fresh installation in Unicode/space paths';VerifyPayload
$first=LatestBackup;$code=@(Run @('-Rollback',$first))[-1];Assert ($code -eq 0) 'rollback fresh installation';Assert (-not(Test-Path (Join-Path $game 'winhttp.dll'))) 'new root files retired on rollback'
foreach($path in @('BepInEx/core/old-extra.dll','dotnet/old-runtime.dll','BepInEx/interop/old.dll','BepInEx/cache/old-cache.bin','BepInEx/config/user.cfg','BepInEx/plugins/OtherMod/other.dll','BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll','winhttp.dll')){$target=Join-Path $game $path;New-Item -ItemType Directory -Force (Split-Path $target -Parent) | Out-Null;Set-Content $target ('old '+$path)}
$oldMod=(Get-FileHash (Join-Path $game 'BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll')).Hash
$code=@(Run)[-1];Assert ($code -eq 0) 'upgrade old loader';VerifyPayload
Assert (-not(Test-Path (Join-Path $game 'BepInEx/core/old-extra.dll'))) 'old extra core not mixed'
Assert (-not(Test-Path (Join-Path $game 'BepInEx/interop'))) 'old interop retired'
Assert (Test-Path (Join-Path $game 'BepInEx/config/user.cfg')) 'configuration preserved'
Assert (Test-Path (Join-Path $game 'BepInEx/plugins/OtherMod/other.dll')) 'other plugin preserved'
$second=LatestBackup;$code=@(Run @('-Rollback',$second))[-1];Assert ($code -eq 0) 'upgrade rollback'
Assert ((Get-FileHash (Join-Path $game 'BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll')).Hash -eq $oldMod) 'previous mod restored'
Assert (Test-Path (Join-Path $game 'BepInEx/core/old-extra.dll')) 'previous exact core restored'
Assert (Test-Path (Join-Path $game 'BepInEx/interop/old.dll')) 'previous cache restored'
$badFile=Join-Path $bundle 'payload/winhttp.dll';$oldSource=Get-Content $badFile -Raw;Set-Content $badFile 'tampered';$code=@(Run)[-1];Assert ($code -ne 0) 'corrupt source rejected';Set-Content -LiteralPath $badFile -Value $oldSource -NoNewline
Assert ((Get-FileHash (Join-Path $game 'BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll')).Hash -eq $oldMod) 'rejected install did not alter mod'
# 故障注入仅在测试作用域改写 Hash，不向用户脚本加入故障开关。
. (Join-Path $bundle 'install.ps1') -GamePath $game -NoPause
$script:Root=$bundle;$script:GamePath=$game;$script:failOnce=$true
function Hash([string]$Path){if($script:failOnce -and $Path -eq (Join-Path $game 'winhttp.dll') -and (Get-Content -LiteralPath $Path -Raw) -like 'new*'){$script:failOnce=$false;return 'injected hash mismatch'};(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash}
$caught=$false;try{Main}catch{$caught=$true;Write-Output $_.Exception.Message}
Assert $caught 'post-copy verification failure injected'
Assert ((Get-FileHash (Join-Path $game 'BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll')).Hash -eq $oldMod) 'failure restores previous mod'
Assert (Test-Path (Join-Path $game 'BepInEx/core/old-extra.dll')) 'failure restores previous core'
Assert (Test-Path (Join-Path $game 'BepInEx/interop/old.dll')) 'failure restores interop'
$modOnly=[pscustomobject]@{schema=1;kind='mod-only';files=@($files | Where-Object path -eq 'BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll')}
$modOnly | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $bundle 'manifest.json')
$code=@(Run)[-1];Assert ($code -eq 0) 'mod-only installation'
Assert (Test-Path (Join-Path $game 'BepInEx/core/old-extra.dll')) 'mod-only preserves loader'
$third=LatestBackup
Set-Content -LiteralPath (Join-Path $game 'BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll') 'later update'
$code=@(Run @('-Rollback',$third))[-1];Assert ($code -ne 0) 'rollback refuses later modifications'
Assert ((Get-Content (Join-Path $game 'BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll') -Raw).Trim() -eq 'later update') 'later modification preserved'
$modOnly.files+= $modOnly.files[0];$modOnly | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $bundle 'manifest.json')
$code=@(Run)[-1];Assert ($code -ne 0) 'duplicate manifest rejected'
$modOnly.files=@([pscustomobject]@{path='../escape.dll';sha256='invalid'})
$modOnly | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $bundle 'manifest.json')
$code=@(Run)[-1];Assert ($code -ne 0) 'path traversal rejected'
Write-Output "Test evidence retained: $TestRoot"
