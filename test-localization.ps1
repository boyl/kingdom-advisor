#Requires -Version 7.0
$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $PSScriptRoot 'src/Localization.cs')
function Check($actual,$expected,$label){if($actual -ne $expected){throw "$label : $actual != $expected"}}
foreach($locale in @('zh','zh-CN','zh-TW','zh_Hans','ChineseSimplified','ChineseTraditional')){Check ([KingdomAdvisor.Localization]::IsEnglish(0,$locale,'en-US')) $false "中文自动选择:$locale"}
foreach($locale in @('en','en-US','fr','ja','de','ko','es','ru','unknown')){Check ([KingdomAdvisor.Localization]::IsEnglish(0,$locale,'zh-CN')) $true "非中文回退英文:$locale"}
Check ([KingdomAdvisor.Localization]::IsEnglish(1,'en','en')) $false '手动中文'
Check ([KingdomAdvisor.Localization]::IsEnglish(2,'zh-CN','zh-CN')) $true '手动英文'
Check ([KingdomAdvisor.Localization]::IsEnglish(0,'','zh-CN')) $false '游戏未就绪采用系统中文'
Check ([KingdomAdvisor.Localization]::IsEnglish(0,'',$null)) $true '无法识别回退英文'
Check ([KingdomAdvisor.Localization]::Translate('金币 36 · 宝石 19',$true)) 'Coins 36 · Gems 19' '动态数字保持'
Check ([KingdomAdvisor.Localization]::Translate('农田',$true)) 'Farm' '目录名称'
Check ([KingdomAdvisor.Localization]::Translate('Wall4(Clone) 10 / 100',$true)) 'Wall4(Clone) 10 / 100' '原生名称和数值保留'
Check ([KingdomAdvisor.Localization]::Translate('金币 36',$false)) '金币 36' '中文原样'
$missing=[Collections.Generic.HashSet[string]]::new()
foreach($name in @('Model.cs','View.cs','AssistModel.cs','MapPresentation.cs','IslandJournal.cs','GameActions.cs','Controller.cs','PadInput.cs','Plugin.cs','CompanionMounts.cs','CompanionMountNetwork.cs')){
    $source=[IO.File]::ReadAllText((Join-Path $PSScriptRoot "src/$name"))
    if($name -eq 'Plugin.cs'){$source=$source.Substring($source.IndexOf('        private void ReadPlayer('))}
    foreach($match in [regex]::Matches($source,'"(?:[^"\\]|\\.)*"')){
        $literal=[regex]::Unescape($match.Value.Substring(1,$match.Value.Length-2))
        if($literal -notmatch '[\u4e00-\u9fff]'){continue}
        $english=[KingdomAdvisor.Localization]::Translate($literal,$true)
        if($english -match '[\u4e00-\u9fff]'){[void]$missing.Add("${name}: $literal => $english")}
    }
}
if($missing.Count){$missing | Sort-Object;throw "存在 $($missing.Count) 条未完成英文翻译"}
'通过：中文变体、英文及其他语言回退、手动覆盖、动态数字、原生名称、全部显示文案覆盖。'
Add-Type -Path (Join-Path $PSScriptRoot 'src/Model.cs')
foreach($entry in [KingdomAdvisor.Catalog]::Entries){
 foreach($value in @($entry.Name,$entry.Description,$entry.Advice,$entry.Category)){
  if([string]::IsNullOrWhiteSpace($value)){throw ('图鉴内容缺失: '+$entry.Key)}
  if([KingdomAdvisor.Localization]::Translate($value,$true) -match '[\u4e00-\u9fff]'){throw ('图鉴翻译缺失: '+$entry.Key)}
 }
}
'通过：全部图鉴条目名称、作用、建议及分类的内容和英文覆盖。'
