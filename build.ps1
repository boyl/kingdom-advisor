param([string]$GamePath)
$ErrorActionPreference='Stop'
if($PSVersionTable.PSVersion.Major -lt 7){throw '需要 PowerShell 7。'}
if(!$GamePath){$steamPath=(Get-ItemProperty 'HKCU:/Software/Valve/Steam').SteamPath; $GamePath=Join-Path $steamPath 'steamapps/common/Kingdom Two Crowns'}
# 使用 PowerShell 自带 Roslyn，针对游戏附带的 .NET 6 运行时编译，无需安装 SDK。
Add-Type -Path (Join-Path $PSHOME 'Microsoft.CodeAnalysis.dll')
Add-Type -Path (Join-Path $PSHOME 'Microsoft.CodeAnalysis.CSharp.dll')
$refs=[System.Collections.Generic.List[Microsoft.CodeAnalysis.MetadataReference]]::new()
foreach($dir in @('dotnet','BepInEx/core','BepInEx/interop')){
 foreach($file in Get-ChildItem (Join-Path $GamePath $dir) -Filter '*.dll'){
  try{[void][Reflection.AssemblyName]::GetAssemblyName($file.FullName)}catch [System.BadImageFormatException]{continue}
  $refs.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($file.FullName))
 }
}
$trees=[System.Collections.Generic.List[Microsoft.CodeAnalysis.SyntaxTree]]::new()
foreach($file in Get-ChildItem (Join-Path $PSScriptRoot 'src') -Filter '*.cs'){
 $trees.Add([Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([IO.File]::ReadAllText($file.FullName),$null,$file.FullName))
}
$options=[Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions]::new([Microsoft.CodeAnalysis.OutputKind]::DynamicallyLinkedLibrary).WithOptimizationLevel([Microsoft.CodeAnalysis.OptimizationLevel]::Release)
$compilation=[Microsoft.CodeAnalysis.CSharp.CSharpCompilation]::Create('KingdomAdvisor',$trees,$refs,$options)
$out=Join-Path $PSScriptRoot 'package/BepInEx/plugins/KingdomAdvisor'
New-Item -ItemType Directory -Force $out | Out-Null
$stream=[IO.File]::Create((Join-Path $out 'KingdomAdvisor.dll'))
try{$result=$compilation.Emit($stream)}finally{$stream.Dispose()}
$result.Diagnostics | Where-Object Severity -in @('Error','Warning') | ForEach-Object ToString
if(!$result.Success){throw '编译失败'}
Get-FileHash (Join-Path $out 'KingdomAdvisor.dll') -Algorithm SHA256
