#Requires -Version 7.0
param(
    [Parameter(Mandatory)][string]$OfficialRoot,
    [Parameter(Mandatory)][string]$PatchedCore,
    [Parameter(Mandatory)][string]$LicenseRoot,
    [Parameter(Mandatory)][string]$SourceRoot,
    [Parameter(Mandatory)][string]$PatchScript,
    [Parameter(Mandatory)][string]$OutputRoot
)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$mod=Join-Path $repo 'package/BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll'
if((Get-FileHash $mod).Hash -ne 'F5BB22DAD8D83CB0D58171CB94E4D8D266F0B89C1F4EA36B5B4B372F1CEDA8B7'){throw 'Mod 不是已验收的提示移除修正版'}
foreach($entry in @(@('LibCpp2IL.dll','E4A458D6144993336DEE769A66968EC4004B86AED2E22221C1B6B2A38CE7D421'),@('Cpp2IL.Core.dll','2A6C78F3F8987DF17EAB19192AF1F0E19C41D3F0E274BF2F89AC2114426DF502'))){if((Get-FileHash (Join-Path $PatchedCore $entry[0])).Hash -ne $entry[1]){throw '解析器不是已验收候选'}}
$officialCore=Join-Path $OfficialRoot 'BepInEx/core'
foreach($license in @('BepInEx-LGPL-2.1.txt','Cpp2IL-MIT.txt','Il2CppInterop.txt','UnityDoorstop.txt','dotnet-LICENSE.txt','dotnet-THIRD-PARTY-NOTICES.txt','Harmony-MIT.txt','MonoMod-MIT.txt','Mono.Cecil-MIT.txt','AsmResolver-MIT.txt','AssetRipper.Primitives-MIT.txt','uTinyRipper-MIT.txt','AssetRipper.CIL-MIT.txt','Iced-MIT.txt','Dobby-Apache-2.0.txt','Capstone.txt','Gee.External.Capstone.txt','Disarm.txt','SemanticVersioning.txt')){if(-not(Test-Path -LiteralPath (Join-Path $LicenseRoot $license) -PathType Leaf)){throw "缺少第三方许可证：$license"}}
foreach($file in Get-ChildItem -LiteralPath $officialCore -File){if($file.Name -notin @('Cpp2IL.Core.dll','LibCpp2IL.dll') -and (Get-FileHash $file.FullName).Hash -ne (Get-FileHash (Join-Path $PatchedCore $file.Name)).Hash){throw '发现其他核心改动'}}
New-Item -ItemType Directory -Force $OutputRoot | Out-Null
$receipts=@()
foreach($kind in @('ModOnly','Complete')){
    $name='KingdomAdvisor-0.6.7-r1-'+$kind;$stage=Join-Path $OutputRoot $name
    if(Test-Path -LiteralPath $stage){throw "输出已存在，不覆盖：$stage"}
    New-Item -ItemType Directory -Path $stage | Out-Null
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'INSTALL.cmd'),(Join-Path $PSScriptRoot 'install.ps1') -Destination $stage
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot "README.$kind.md") -Destination (Join-Path $stage 'README.md')
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot "README.$kind.en.md") -Destination (Join-Path $stage 'README.en.md')
    Copy-Item -LiteralPath (Join-Path $repo 'CHANGELOG.md') -Destination $stage
    $payload=Join-Path $stage 'payload'
    New-Item -ItemType Directory -Force (Join-Path $payload 'BepInEx/plugins/KingdomAdvisor') | Out-Null
    Copy-Item -LiteralPath $mod -Destination (Join-Path $payload 'BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll')
    if($kind -eq 'Complete'){
        Copy-Item -LiteralPath (Join-Path $OfficialRoot 'dotnet') -Destination $payload -Recurse
        Copy-Item -LiteralPath $PatchedCore -Destination (Join-Path $payload 'BepInEx/core') -Recurse
        foreach($file in @('winhttp.dll','doorstop_config.ini','.doorstop_version','changelog.txt')){Copy-Item -LiteralPath (Join-Path $OfficialRoot $file) -Destination $payload}
        Copy-Item -LiteralPath $LicenseRoot -Destination (Join-Path $stage 'licenses') -Recurse
        $sources=Join-Path $stage 'sources';New-Item -ItemType Directory $sources | Out-Null
        foreach($file in @('BepInEx-5b766a3-source.zip','Il2CppInterop-1.5.3-source.zip','Cpp2IL-558ddd9-code.zip','UnityDoorstop-4.5.0-source.zip')){
            $path=Join-Path $SourceRoot $file
            $archive=[IO.Compression.ZipFile]::OpenRead($path);try{if(-not $archive.Entries.Count){throw '源码包为空'}}finally{$archive.Dispose()}
            Copy-Item -LiteralPath $path -Destination $sources
        }
        Copy-Item -LiteralPath $PatchScript -Destination (Join-Path $sources 'patch-788-stage.ps1')
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'THIRD-PARTY-NOTICES.md'),(Join-Path $PSScriptRoot 'REPRODUCE.md') -Destination $stage
    }
    $files=@(Get-ChildItem -LiteralPath $payload -File -Recurse -Force | Sort-Object FullName | ForEach-Object {[pscustomobject]@{path=[IO.Path]::GetRelativePath($payload,$_.FullName).Replace('\','/');sha256=(Get-FileHash $_.FullName).Hash;size=$_.Length}})
    if(@($files | Where-Object {$_.path -match 'interop/|unity-libs/|GameAssembly|global-metadata|\.cfg$'}).Count){throw '包中包含游戏派生或用户文件'}
    [pscustomobject]@{schema=1;kind=if($kind -eq 'Complete'){'complete'}else{'mod-only'};mod='0.6.7-r1';runtime=if($kind -eq 'Complete'){'BepInEx 6.0.0-be.788+5b766a3 / Interop 1.5.3 / patched Cpp2IL'}else{'external'};files=$files} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $stage 'manifest.json') -Encoding utf8
    $all=@(Get-ChildItem -LiteralPath $stage -File -Recurse -Force | Sort-Object FullName | ForEach-Object {'{0}  {1}' -f (Get-FileHash $_.FullName).Hash,[IO.Path]::GetRelativePath($stage,$_.FullName).Replace('\','/')})
    $all | Set-Content (Join-Path $stage 'SHA256SUMS.txt') -Encoding utf8
    $zipPath=Join-Path $OutputRoot ($name+'.zip');[IO.Compression.ZipFile]::CreateFromDirectory($stage,$zipPath,[IO.Compression.CompressionLevel]::Optimal,$false)
    $receipts+=[pscustomobject]@{name=$name;zip=$zipPath;sha256=(Get-FileHash $zipPath).Hash;size=(Get-Item $zipPath).Length;payloadFiles=$files.Count}
}
$receipts | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $OutputRoot 'bundle-receipts.json') -Encoding utf8
$receipts | Format-Table name,size,payloadFiles,sha256
