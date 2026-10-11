#Requires -Version 7.0
param([Parameter(Mandatory)][string]$BaseBundleRoot,[Parameter(Mandatory)][string]$OutputRoot)
$ErrorActionPreference='Stop'
$docs=$PSScriptRoot
$repo=Split-Path (Split-Path $docs -Parent) -Parent
$approval=Get-Content (Join-Path $repo 'release-acceptance.json') -Raw | ConvertFrom-Json
$version=$approval.version
New-Item -ItemType Directory $OutputRoot -Force|Out-Null
$receipts=@()
foreach($kind in @('ModOnly','Complete')){
 $base=Join-Path $BaseBundleRoot ('KingdomAdvisor-'+$version+'-'+$kind)
 $manifest=Get-Content (Join-Path $base 'manifest.json') -Raw|ConvertFrom-Json
 $expected=if($kind -eq 'Complete'){229}else{1}
 if($manifest.files.Count -ne $expected){throw 'Unexpected base payload count'}
 $name="KingdomAdvisor-$version-Nexus-$kind-Manual";$stage=Join-Path $OutputRoot $name
 if(Test-Path -LiteralPath $stage){throw 'Output exists; do not overwrite'}
 New-Item -ItemType Directory $stage|Out-Null
 foreach($file in $manifest.files){
  if($file.path -match '(^|/)\.\.|^/|[:]|\.(ps1|cmd|bat|zip|7z|rar|gz|exe)$'){throw 'Forbidden payload path'}
  $src=Join-Path (Join-Path $base 'payload') $file.path
  if((Get-FileHash -LiteralPath $src).Hash -ne $file.sha256){throw 'Accepted base payload changed'}
  $dst=Join-Path $stage $file.path;New-Item -ItemType Directory (Split-Path $dst -Parent) -Force|Out-Null
  Copy-Item -LiteralPath $src -Destination $dst
 }
 if((Get-FileHash (Join-Path $stage 'BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll')).Hash -ne $approval.dllHash){throw 'Mod hash mismatch'}
 Copy-Item -LiteralPath (Join-Path $docs 'README.zh-CN.md') -Destination (Join-Path $stage 'README.md')
 Copy-Item -LiteralPath (Join-Path $docs 'README.en.md'),(Join-Path $docs 'INSTALLATION.png') -Destination $stage
 if($kind -eq 'Complete'){
  Copy-Item -LiteralPath (Join-Path $base 'licenses') -Destination $stage -Recurse
  Copy-Item -LiteralPath (Join-Path $docs 'THIRD-PARTY-NOTICES.md') -Destination $stage
 }
 $all=@(Get-ChildItem -LiteralPath $stage -File -Recurse -Force)
 if(@($all|Where-Object Extension -match '^\.(ps1|cmd|bat|sh|zip|7z|rar|gz|tar|nupkg|exe)$').Count){throw 'Scripts or nested archives in distribution'}
 $all|Sort-Object FullName|ForEach-Object {(Get-FileHash $_.FullName).Hash+'  '+[IO.Path]::GetRelativePath($stage,$_.FullName).Replace('\','/')}|Set-Content (Join-Path $stage 'SHA256SUMS.txt') -Encoding utf8
 $zip=Join-Path $OutputRoot ($name+'.zip');[IO.Compression.ZipFile]::CreateFromDirectory($stage,$zip,[IO.Compression.CompressionLevel]::Optimal,$false)
 $receipts+=[pscustomobject]@{name=$name;kind=$kind;zip=$zip;sha256=(Get-FileHash $zip).Hash;size=(Get-Item $zip).Length;payloadFiles=$expected;totalFiles=$all.Count+1}
}
$receipts|ConvertTo-Json -Depth 5|Set-Content (Join-Path $OutputRoot 'bundle-receipts.json') -Encoding utf8
$receipts|Format-Table name,size,payloadFiles,totalFiles
