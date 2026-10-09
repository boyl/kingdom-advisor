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
$candidate=Join-Path $out ('KingdomAdvisor-build-'+[guid]::NewGuid().ToString('N')+'.tmp')
$stream=[IO.File]::Create($candidate)
$mountResources=[Collections.Generic.List[Microsoft.CodeAnalysis.ResourceDescription]]::new()
foreach($mountName in @('cat','dog','cat-vfx')){
 $mountResourcePath=Join-Path $PSScriptRoot ('assets/mounts/'+$mountName+'.png')
 $mountProvider=[Func[IO.Stream]]({[IO.File]::OpenRead($mountResourcePath)}.GetNewClosure())
 $mountResources.Add([Microsoft.CodeAnalysis.ResourceDescription]::new('KingdomAdvisor.Mounts.'+$mountName+'.png',$mountProvider,$true))
}
try{$result=$compilation.Emit($stream,$null,$null,$null,$mountResources)}finally{$stream.Dispose()}
$result.Diagnostics | Where-Object Severity -in @('Error','Warning') | ForEach-Object ToString
if(!$result.Success){Remove-Item -LiteralPath $candidate;throw '编译失败，旧包保留'}
Move-Item -LiteralPath $candidate -Destination (Join-Path $out 'KingdomAdvisor.dll') -Force
Get-FileHash (Join-Path $out 'KingdomAdvisor.dll') -Algorithm SHA256
