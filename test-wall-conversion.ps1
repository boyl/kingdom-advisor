#Requires -Version 7.0
$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $PSScriptRoot 'src/Model.cs')
Add-Type -Path (Join-Path $PSScriptRoot 'src/Localization.cs')
if([KingdomAdvisor.Catalog]::ResolveWall('Wall5(Clone)',$true).Key -ne 'hornwall'){throw '真实号角组件不能被普通墙名称覆盖'}
if([KingdomAdvisor.Catalog]::ResolveWall('Wall5(Clone)',$false).Key -ne 'wall'){throw '普通墙不得伪装成已改造号角墙'}
$conversion=[KingdomAdvisor.Catalog]::WallConversion('WallHorn5(Clone)')
if($conversion -notmatch '召集军队' -or $conversion -notmatch '需要带上号角隐士'){throw '缺少改造用途或乘员条件'}
if([KingdomAdvisor.Catalog]::WallConversion('Wall5(Clone)') -ne ''){throw '普通升级不得伪装成号角改造'}
$english=[KingdomAdvisor.Localization]::Translate($conversion,$true)
if($english -match '[\p{IsCJKUnifiedIdeographs}]'){throw '改造说明英文未完整覆盖'}
'通过：号角墙原生身份、普通墙隔离、隐士改造用途与中英文。'
foreach($key in @('hornwall','ballista','baker','knighttower','stable','firetower')){
 $target=[KingdomAdvisor.Catalog]::Entries|Where-Object Key -eq $key
 $text=[KingdomAdvisor.Catalog]::Conversion($target,'NativePassengerTag')
 if(!$text.Contains($target.Description) -or $text -notmatch '需要带上'){throw "遗漏特殊改造作用或前置：$key"}
 if([KingdomAdvisor.Localization]::Translate($text,$true) -match '[\p{IsCJKUnifiedIdeographs}]'){throw "特殊改造英文遗漏：$key"}
}
$unknown=[KingdomAdvisor.Catalog]::Conversion([KingdomAdvisor.Catalog]::Resolve('FutureBuilding'),'FuturePassengerTag')
if($unknown -notmatch 'FuturePassengerTag'){throw '未知乘员条件不能丢失或假装已满足'}
'通过：六类隐士改造说明、动态英文与未知乘员条件。'
