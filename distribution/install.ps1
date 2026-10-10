#Requires -Version 7.0
[CmdletBinding()]
param([string]$GamePath,[switch]$NoPause,[switch]$Elevated,[string]$Rollback)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$script:Root=$PSScriptRoot
function SafePath([string]$Base,[string]$Relative) {
    if([IO.Path]::IsPathRooted($Relative) -or $Relative.Contains(':')){throw "需要相对路径 / Relative path required: $Relative"}
    $baseFull=[IO.Path]::GetFullPath($Base).TrimEnd('\','/')
    $result=[IO.Path]::GetFullPath((Join-Path $baseFull $Relative))
    if(-not $result.StartsWith($baseFull+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw '路径越界 / Path escapes root'}
    $cursor=$result
    while($cursor){if((Test-Path -LiteralPath $cursor) -and ((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw "不支持链接路径 / Linked path unsupported: $cursor"};$cursor=Split-Path $cursor -Parent}
    return $result
}
function Hash([string]$Path){(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash}
function SaveRecord($Record,[string]$Backup){$Record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $Backup 'installation.json') -Encoding utf8}
function FindGame {
    $roots=[Collections.Generic.List[string]]::new()
    $steamKey=Get-ItemProperty 'HKCU:/Software/Valve/Steam' -ErrorAction SilentlyContinue
    $steam=if($steamKey){$steamKey.SteamPath}else{$null}
    if($steam){$roots.Add($steam);$vdf=Join-Path $steam 'steamapps/libraryfolders.vdf';if(Test-Path -LiteralPath $vdf){foreach($line in Get-Content -LiteralPath $vdf){if($line -match '"path"\s+"([^"]+)"'){$roots.Add($Matches[1].Replace('\\','\'))}}}}
    $found=@($roots | ForEach-Object {Join-Path $_ 'steamapps/common/Kingdom Two Crowns'} | Where-Object {Test-Path -LiteralPath (Join-Path $_ 'KingdomTwoCrowns.exe')} | Select-Object -Unique)
    if($found.Count -eq 1){return $found[0]}
    if($NoPause){throw '请用 -GamePath 指定游戏目录 / Specify -GamePath'}
    return (Read-Host '请输入 KingdomTwoCrowns.exe 所在目录 / Enter game directory').Trim().Trim('"')
}
function ReadBundle {
    $manifest=Get-Content -LiteralPath (Join-Path $script:Root 'manifest.json') -Raw | ConvertFrom-Json
    if($manifest.schema -ne 1 -or $manifest.kind -notin @('mod-only','complete')){throw '安装清单格式错误 / Invalid manifest'}
    $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach($f in $manifest.files){
        if(-not $seen.Add($f.path)){throw '重复文件 / Duplicate file'}
        $source=SafePath (Join-Path $script:Root 'payload') $f.path
        if(-not(Test-Path -LiteralPath $source -PathType Leaf) -or (Hash $source) -ne $f.sha256){throw "源文件校验失败 / Payload verification failed: $($f.path)"}
        $allowed=$f.path -eq 'BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll'
        if($manifest.kind -eq 'complete'){$allowed=$allowed -or $f.path -match '^(BepInEx/core/|dotnet/)' -or $f.path -in @('winhttp.dll','doorstop_config.ini','.doorstop_version','changelog.txt')}
        if(-not $allowed){throw "不允许的安装文件 / Unapproved payload: $($f.path)"}
    }
    if(-not $seen.Contains('BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll')){throw '缺少 Mod / Missing mod'}
    if($manifest.kind -eq 'complete'){foreach($required in @('winhttp.dll','doorstop_config.ini','dotnet/coreclr.dll','BepInEx/core/BepInEx.Unity.IL2CPP.dll','BepInEx/core/Cpp2IL.Core.dll','BepInEx/core/LibCpp2IL.dll')){if(-not $seen.Contains($required)){throw "缺少前置 / Missing loader: $required"}}}
    return $manifest
}
function CheckAccess($Plan,[string]$Destination) {
    foreach($file in $Plan){if(Test-Path -LiteralPath $file.target){$item=Get-Item -LiteralPath $file.target;if($item.PSIsContainer -or $item.IsReadOnly){throw "目标不可替换 / Target not replaceable: $($file.path)"};$stream=[IO.File]::Open($file.target,'Open','Write','ReadWrite');$stream.Dispose()}}
    $probe=SafePath $Destination ('.install-probe-'+[guid]::NewGuid().ToString('N'))
    try{$stream=[IO.File]::Open($probe,'CreateNew','Write','None');$stream.Dispose()}finally{if(Test-Path -LiteralPath $probe){Remove-Item -LiteralPath $probe -Force}}
}
function Elevate {
    if($Elevated){throw '提权后仍无权限 / Access denied after elevation'}
    $exe=(Get-Command pwsh.exe -CommandType Application | Select-Object -First 1).Source
    $args='-NoLogo -NoProfile -ExecutionPolicy Bypass -File "{0}" -GamePath "{1}" -NoPause -Elevated' -f $PSCommandPath,$GamePath
    if($Rollback){$args+=' -Rollback "{0}"' -f $Rollback}
    $child=Start-Process -FilePath $exe -ArgumentList $args -Verb RunAs -WindowStyle Hidden -Wait -PassThru
    if($child.ExitCode -ne 0){throw "提权进程失败 / Elevated process failed: $($child.ExitCode)"}
    if($Rollback){
        $updated=Get-Content -LiteralPath (Join-Path $Rollback 'installation.json') -Raw | ConvertFrom-Json
        if($updated.status -ne 'restored'){throw '提权回滚状态不一致 / Elevated rollback status mismatch'}
        Write-Host '回滚完成 / Rollback completed.' -ForegroundColor Green
    }else{
        $backupParent=SafePath $GamePath '.install-backups'
        $latest=Get-ChildItem -LiteralPath $backupParent -Directory -Filter 'KingdomAdvisor-*' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if(-not $latest){throw '找不到提权安装记录 / Elevated install record missing'}
        Write-Host "备份 / Backup: $($latest.FullName)"
    }
}
function Restore($record,[string]$backup,[bool]$VerifyCurrent) {
    if($VerifyCurrent){foreach($f in $record.files){$target=SafePath $GamePath $f.path;if(-not(Test-Path -LiteralPath $target) -or (Hash $target) -ne $f.sha256){throw "安装后文件已更改，停止回滚 / Changed installed file; rollback stopped: $($f.path)"}}}
    # 新版目录移入本次备份；不删除用户文件或跟随链接。
    foreach($group in $record.groups){$target=SafePath $GamePath $group.path;$archive=SafePath $backup ('after/'+$group.path);if(Test-Path -LiteralPath $target){New-Item -ItemType Directory -Force (Split-Path $archive -Parent) | Out-Null;Move-Item -LiteralPath $target -Destination $archive};if($group.existed){Move-Item -LiteralPath (SafePath $backup ('before/'+$group.path)) -Destination $target}}
    foreach($f in $record.files){if($f.grouped -or (-not $VerifyCurrent -and -not $f.written)){continue};$target=SafePath $GamePath $f.path;$old=SafePath $backup ('before/'+$f.path);if($f.previous){New-Item -ItemType Directory -Force (Split-Path $target -Parent) | Out-Null;Copy-Item -LiteralPath $old -Destination $target -Force;if((Hash $target) -ne $f.previous){throw "回滚校验失败 / Rollback verification failed: $($f.path)"}}elseif(Test-Path -LiteralPath $target){$archive=SafePath $backup ('after/'+$f.path);New-Item -ItemType Directory -Force (Split-Path $archive -Parent) | Out-Null;Move-Item -LiteralPath $target -Destination $archive}}
    foreach($old in $record.groupFiles){$target=SafePath $GamePath $old.path;if((Hash $target) -ne $old.sha256){throw '旧目录恢复校验失败 / Previous directory verification failed'}}
    $record.status='restored';SaveRecord $record $backup
}
function Install($manifest,$plan) {
    $backup=SafePath $GamePath ('.install-backups/KingdomAdvisor-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $backup -Force | Out-Null
    $groups=@();if($manifest.kind -eq 'complete'){$groups=@('BepInEx/core','dotnet','BepInEx/interop','BepInEx/cache')}
    $record=[pscustomobject]@{schema=1;status='preparing';kind=$manifest.kind;game=$GamePath;created=(Get-Date).ToUniversalTime().ToString('o');files=@();groups=@();groupFiles=@()}
    foreach($file in $plan){$file.grouped=@($groups | Where-Object {$file.path.StartsWith($_+'/',[StringComparison]::OrdinalIgnoreCase)}).Count -gt 0;$record.files+= $file;if($file.previous -and -not $file.grouped){$old=SafePath $backup ('before/'+$file.path);New-Item -ItemType Directory -Force (Split-Path $old -Parent) | Out-Null;Copy-Item -LiteralPath $file.target -Destination $old;if((Hash $old) -ne $file.previous){throw '备份文件发生变化 / Backup changed'}}}
    SaveRecord $record $backup
    try{
        foreach($name in $groups){$target=SafePath $GamePath $name;$exists=Test-Path -LiteralPath $target;if($exists){$null=SafePath $GamePath $name;foreach($file in Get-ChildItem -LiteralPath $target -File -Recurse -Force){$relative=[IO.Path]::GetRelativePath($GamePath,$file.FullName).Replace('\','/');$null=SafePath $GamePath $relative;$record.groupFiles+=[pscustomobject]@{path=$relative;sha256=Hash $file.FullName}};$old=SafePath $backup ('before/'+$name);New-Item -ItemType Directory -Force (Split-Path $old -Parent) | Out-Null;Move-Item -LiteralPath $target -Destination $old};$record.groups+=[pscustomobject]@{path=$name;existed=$exists};SaveRecord $record $backup}
        foreach($file in $plan){$current=if(Test-Path -LiteralPath $file.target){Hash $file.target}else{$null};if(-not $file.grouped -and $current -ne $file.previous){throw "目标发生变化 / Target changed: $($file.path)"};if((Hash $file.source) -ne $file.sha256){throw '源文件变化 / Payload changed'};New-Item -ItemType Directory -Force (Split-Path $file.target -Parent) | Out-Null;$file.written=$true;Copy-Item -LiteralPath $file.source -Destination $file.target -Force;if((Hash $file.target) -ne $file.sha256){throw "安装校验失败 / Install verification failed: $($file.path)"}}
        foreach($file in $plan){if((Hash $file.target) -ne $file.sha256){throw '最终验证失败 / Final verification failed'}}
        $record.status='installed';SaveRecord $record $backup
    }catch{$failure=$_.Exception.Message;try{Restore $record $backup $false}catch{throw "安装失败 / Install failed: $failure; 恢复失败 / Restore failed: $($_.Exception.Message); $backup"};throw "安装失败，已恢复 / Install failed; restored: $failure; $backup"}
    Write-Host "安装成功，SHA256 校验通过 / Installation verified." -ForegroundColor Green
    Write-Host "备份 / Backup: $backup"
    Write-Host '首次启动会联网下载 Unity 基础库并生成 interop，请等待。/ First launch downloads Unity libraries and generates interop; please wait.'
}
function Main {
    if(-not $GamePath){$script:GamePath=FindGame}
    $script:GamePath=[IO.Path]::GetFullPath($GamePath.TrimEnd('\','/'))
    $null=SafePath $GamePath 'KingdomTwoCrowns.exe'
    if(-not(Test-Path -LiteralPath (Join-Path $GamePath 'KingdomTwoCrowns.exe') -PathType Leaf)){throw '未找到游戏 / Game executable not found'}
    if(Get-Process -Name KingdomTwoCrowns -ErrorAction SilentlyContinue){throw '请先保存退出游戏 / Save and exit the game first'}
    if($Rollback){
        $parent=SafePath $GamePath '.install-backups';$backup=[IO.Path]::GetFullPath($Rollback)
        if(-not $backup.StartsWith($parent+'\',[StringComparison]::OrdinalIgnoreCase)){throw '回滚路径必须位于游戏备份目录 / Rollback must use a game backup'}
        $null=SafePath $GamePath ([IO.Path]::GetRelativePath($GamePath,$backup))
        $record=Get-Content -LiteralPath (Join-Path $backup 'installation.json') -Raw | ConvertFrom-Json
        if($record.game -ne $GamePath -or $record.status -ne 'installed'){throw '备份身份或状态不匹配 / Backup identity or state mismatch'}
        $plan=@($record.files | ForEach-Object {[pscustomobject]@{path=$_.path;target=SafePath $GamePath $_.path}})
        try{CheckAccess $plan $GamePath}catch [UnauthorizedAccessException]{Elevate;return}
        Restore $record $backup $true;Write-Host '回滚完成并校验 / Rollback verified.' -ForegroundColor Green;return
    }
    $manifest=ReadBundle
    $plan=@($manifest.files | ForEach-Object {$target=SafePath $GamePath $_.path;[pscustomobject]@{path=$_.path;source=SafePath (Join-Path $script:Root 'payload') $_.path;target=$target;sha256=$_.sha256;previous=if(Test-Path -LiteralPath $target -PathType Leaf){Hash $target}else{$null};grouped=$false;written=$false}})
    try{CheckAccess $plan $GamePath}catch [UnauthorizedAccessException]{Elevate;foreach($file in $plan){if((Hash $file.target) -ne $file.sha256){throw '提权安装结果不一致 / Elevated installation mismatch'}};Write-Host '提权结果已独立验证 / Elevated result verified.';return}
    Install $manifest $plan
}
if($MyInvocation.InvocationName -ne '.') {
    $code=0
    try{Main}catch{Write-Host $_.Exception.Message -ForegroundColor Red;$code=1}
    if(-not $NoPause){$null=Read-Host '按 Enter 关闭 / Press Enter to close'}
    exit $code
}
