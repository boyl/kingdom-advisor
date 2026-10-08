#Requires -Version 7.0
$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $PSScriptRoot 'src/Model.cs')
if([KingdomAdvisor.PopulationRole]::Supplemental($false,$true,$true,$false,$false) -ne ''){throw '长枪兵不得重复计入渔夫'}
if([KingdomAdvisor.PopulationRole]::Supplemental($true,$false,$true,$false,$false) -ne 'ninja'){throw '忍者不得重复计入渔夫'}
if([KingdomAdvisor.PopulationRole]::Supplemental($false,$false,$true,$false,$false) -ne 'fisher'){throw '独立渔夫必须保留'}
if([KingdomAdvisor.PopulationRole]::Supplemental($false,$false,$false,$false,$true) -ne 'peasant'){throw '待业平民遗漏'}
'通过：派生职业隔离、真实渔夫及平民。'
