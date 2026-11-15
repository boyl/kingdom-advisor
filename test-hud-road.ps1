#Requires -Version 7.0
$ErrorActionPreference='Stop'
$layout=Get-Content (Join-Path $PSScriptRoot 'src/HudTextLayout.cs') -Raw
$view=Get-Content (Join-Path $PSScriptRoot 'src/View.cs') -Raw
$draw=$view.Substring($view.IndexOf('private void DrawHudRows('))
$draw=$draw.Substring(0,$draw.IndexOf('if(pages.Length<=1)return;'))
$initial=$draw.Substring($draw.IndexOf('{')+1)
$initial=$initial.Substring(0,$initial.IndexOf('foreach(var row'))
$position=[regex]::Match($draw,'float x=.*?;').Value
$advance=($draw -split "`n" | Where-Object {$_ -match '\+=row.Height;'}).Trim()
$harness=@"
using System;
using System.Linq;
using System.Collections.Generic;
using KingdomAdvisor;
public static class ActualColumnDrawing {
 public static void Check(){
  var rows=Enumerable.Range(0,12).Select(i=>new HudTextRow(i.ToString()));
  var pages=new[]{HudTextLayout.FlowColumns(rows,104,3,s=>21)};
  float top=800;var rect=new {x=18f,width=1200f};string key="test";
  var hudPages=new Dictionary<string,int>{{key,0}};
  $initial
  var expected=new float[page.Columns];
  foreach(var row in page.Rows){
   $position
   if(Math.Abs(y-top-expected[row.Column])>.01f)throw new Exception("第三列及后续列必须从正文顶部独立排布");
   expected[row.Column]+=row.Height;
   $advance
  }
 }
}
"@
Add-Type -TypeDefinition ($harness+$layout.Replace('using System;','').Replace('using System.Linq;','').Replace('using System.Collections.Generic;',''))
[ActualColumnDrawing]::Check()
foreach($height in @(725,1080,1377)) {
 $road=$height*.575
 $top=[KingdomAdvisor.HudTextLayout]::BelowRoad($height,[float]($road/$height))
 if($top -lt $road+12){throw '顾问必须在路面下方保留12像素'}
 $bottom=$height-66
 $budget=$bottom-$top
 foreach($header in @(120,180)) {
  $rows=[Collections.Generic.List[KingdomAdvisor.HudTextRow]]::new()
  foreach($i in 1..16){$rows.Add([KingdomAdvisor.HudTextRow]::new("row$i"))}
  if($budget-$header-8 -lt 26){continue}
  $result=$null
  foreach($columns in 1..16){$result=[KingdomAdvisor.HudTextLayout]::FlowColumns($rows,($budget-$header-8),$columns,[Func[string,single]]{param($s)21});if($result){break}}
  if(!$result -or $result.Rows.Count -ne 16){throw '横向分栏没有保留全部条目'}
  $h=$header+$result.Height+8
  if($bottom-$h -lt $top-.01){throw '新头部或正文分栏将顾问撑到路面上方'}
  foreach($col in 0..($result.Columns-1)){$columnHeight=($result.Rows | Where-Object Column -eq $col | Measure-Object Height -Sum).Sum;if($columnHeight -gt $budget-$header-8+.01){throw '列内容超出可用高度'}}
 }
}
'通过：路面下方高度预算、全部条目横向分栏，无需分页。'
