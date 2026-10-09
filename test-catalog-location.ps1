#Requires -Version 7.0
$ErrorActionPreference='Stop'
Add-Type -Path @((Join-Path $PSScriptRoot 'src/Model.cs'),(Join-Path $PSScriptRoot 'src/CatalogPresentation.cs'),(Join-Path $PSScriptRoot 'src/Localization.cs'))
$entries=[KingdomAdvisor.Catalog]::Entries
if($entries.Count -ne 104){throw '图鉴范围变化，必须更新全表验收。'}
$patterns=@{}
foreach($entry in $entries){
 $kind=[KingdomAdvisor.CatalogIcons]::Kind($entry)
 if($kind -eq 'question'){throw "已知条目落入未知图标：$($entry.Key)"}
 $pattern=[KingdomAdvisor.CatalogIcons]::Pattern($kind)
 if($pattern.Count -ne 5 -or @($pattern|Where-Object {$_ -notmatch '^[01]{11}$'}).Count){throw "非法图案尺寸：$($entry.Key)"}
 $fingerprint=$pattern -join ''
 if($patterns.ContainsKey($fingerprint)){throw "不同图鉴条目完全相同：$($patterns[$fingerprint]) / $($entry.Key)"}
 $patterns[$fingerprint]=$entry.Key
}
# 独立语义契约：对象主体不能因为作用词被覆盖。
foreach($entry in $entries|Where-Object Key -Like 'hermit*'){if([KingdomAdvisor.CatalogIcons]::Kind($entry) -notlike 'person+*'){throw '隐士主体不是人物'}}
foreach($entry in $entries|Where-Object Key -Like 'statue*'){if([KingdomAdvisor.CatalogIcons]::Kind($entry) -notlike 'statue+*'){throw '雕像主体被效果取代'}}
$expected=@{wall='wall';tower='tower+bow';bomb='bomb';bombpurchase='flag+bomb';farm='field';farmhouse='house+wheat';beggar='person';beggarcamp='camp+person';chariotday='wheel+sun';chariotnight='wheel+moon';stable='house+horse';lizard='reptile+flame';barrel='barrel+flame'}
foreach($key in $expected.Keys){$entry=$entries|Where-Object Key -eq $key;if([KingdomAdvisor.CatalogIcons]::Kind($entry) -ne $expected[$key]){throw "身份语义错误：$key"}}
$future=[KingdomAdvisor.Entry]::new('future','未来条目','','','防御')
if([KingdomAdvisor.CatalogIcons]::Kind($future) -ne 'question'){throw '未知身份不得装成已知对象'}
if(([KingdomAdvisor.CatalogIcons]::Pattern('question') -join '').Replace('0','').Length -lt 5){throw '未知图标不可见'}
'通过：104条全量图标覆盖、图案互异、尺寸、身份语义与未知身份回退。'
$a=[KingdomAdvisor.MapPoint]::new();$a.Key='wall';$a.Name='城墙';$a.X=10
$b=[KingdomAdvisor.MapPoint]::new();$b.Key='wall';$b.Name='城墙';$b.X=30
if([KingdomAdvisor.LocationPresentation]::Find(@($a,$b),$b) -ne 1){throw '同名地点链接串位'}
if([KingdomAdvisor.LocationPresentation]::Find(@($b,$a),$b) -ne 0){throw '排序后地点链接失效'}
$player=[KingdomAdvisor.PlayerInfo]::new();$player.X=20;$player.Id=0
if([KingdomAdvisor.LocationPresentation]::Distance($a,20) -notmatch '左侧' -or [KingdomAdvisor.LocationPresentation]::Distance($b,20) -notmatch '右侧'){throw '相对方向错误'}
if([KingdomAdvisor.LocationPresentation]::Entry($a).Key -ne 'wall'){throw '地点未关联图鉴'}
$unknown=[KingdomAdvisor.MapPoint]::new();$unknown.Key='future'
if([KingdomAdvisor.LocationPresentation]::Entry($unknown)){throw '未知地点不得错连图鉴'}
foreach($point in @($a,$b,$unknown)){$content=[KingdomAdvisor.LocationPresentation]::Detail($point,$player);if([KingdomAdvisor.Localization]::Translate($content,$true) -match '[\p{IsCJKUnifiedIdeographs}]'){throw '地点说明英文未完整覆盖'}}
'通过：语义图标、同名地点/排序联动、距离方向、未知身份与中英文。'

foreach($pair in @(@('bomb','bombpurchase'),@('wall','tower'),@('farmhouse','farm'),@('beggarcamp','beggar'))){
 $patterns=foreach($key in $pair){$entry=[KingdomAdvisor.Catalog]::Entries|Where-Object Key -eq $key; [string]::Join('',[KingdomAdvisor.CatalogIcons]::Pattern([KingdomAdvisor.CatalogIcons]::Kind($entry)))}
 if($patterns[0] -eq $patterns[1]){throw ('不同对象共用图标：'+($pair -join ','))}
}
'通过：截图四组不同对象的像素图案互不相同。'
