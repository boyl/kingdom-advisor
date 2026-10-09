#Requires -Version 7.0
param([string]$GamePath)
$ErrorActionPreference='Stop'
if(!$GamePath){$steamRoot=(Get-ItemProperty 'HKCU:/Software/Valve/Steam').SteamPath;$GamePath=Join-Path $steamRoot 'steamapps/common/Kingdom Two Crowns'}
Add-Type -Path (Join-Path $GamePath 'BepInEx/core/Mono.Cecil.dll')
$assembly=[Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $PSScriptRoot 'package/BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll'))
try{
 foreach($name in @('cat','dog','cat-vfx')){
  $resource=@($assembly.MainModule.Resources | Where-Object Name -eq ('KingdomAdvisor.Mounts.'+$name+'.png'))
  if($resource.Count -ne 1){throw "缺失或重复嵌入资源：$name"}
  $bytes=$resource[0].GetResourceData()
  $source=[IO.File]::ReadAllBytes((Join-Path $PSScriptRoot ('assets/mounts/'+$name+'.png')))
  if([Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)) -ne [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($source))){throw "嵌入资源与源图不一致：$name"}
  $width=([int]$bytes[16] -shl 24) -bor ([int]$bytes[17] -shl 16) -bor ([int]$bytes[18] -shl 8) -bor [int]$bytes[19]
  $height=([int]$bytes[20] -shl 24) -bor ([int]$bytes[21] -shl 16) -bor ([int]$bytes[22] -shl 8) -bor [int]$bytes[23]
  if($bytes[25] -ne 6 -or $width -lt 256 -or $height -lt 128 -or [Math]::Abs($width/$height-2) -gt .05){throw "资源尺寸、布局或透明格式不符：$name"}
  "通过：$name 嵌入源图哈希一致，RGBA $width x $height，4×2布局。"
 }
}finally{$assembly.Dispose()}
