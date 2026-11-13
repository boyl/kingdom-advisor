#Requires -Version 7.0
$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $PSScriptRoot 'src/HudTextLayout.cs')
function Check($condition,$message){if(!$condition){throw $message}}
foreach($font in @(18,28)){
 $line=$font*1.2
 $measure=[Func[string,single,single]]{param($text,$width) [Math]::Max(1,[Math]::Ceiling($text.Length*$font*.52/$width))*$line}
 foreach($screen in @(640,960,1280,2560)){
  $map=[KingdomAdvisor.HudRect]::new(18,18,$screen-36,[Math]::Max(350,7*$line+196))
  foreach($language in @('zh','en')){
   $tip=if($language -eq 'zh'){'地图 · 12 处名称密集 · 切换附近查看'}else{'Map · 12 labels overlap · Switch to Nearby'}
   $alert=if($language -eq 'zh'){'王国 · 可招募 4 人 · 13 处工具库存为空'}else{'Kingdom · 4 recruitable · 13 tool stands empty'}
   $r=[KingdomAdvisor.HudTextLayout]::MapFooter($map,$tip,$alert,390,$line,$measure)
   $t=$r.Item1;$a=$r.Item2
   Check (!$t.Overlaps($a)) '地图与王国提示不能互相重叠'
   Check ($a.Right -le $map.Right -and $t.Right -le $map.Right) '页脚不能水平越出地图'
   if($screen -ge 1280){Check ([Math]::Abs($t.Y-($a.Y+4)) -lt .01) '宽屏默认应为相同文字基线'}
   if($screen -eq 640){Check ($a.Y -ge $t.Bottom+8) '窄屏应完整分成两行'}
   Check ($t.Height -ge $measure.Invoke($tip,$t.Width)) '地图提示按实际可用宽度计算全部高度'
   Check ($a.Height -ge $measure.Invoke($alert,$a.Width-32)+8) '王国提示按实际可用宽度计算全部高度'
  }
 }
}
$measure=[Func[string,single,single]]{param($text,$width)22}
$map=[KingdomAdvisor.HudRect]::new(18,18,2524,350)
$r=[KingdomAdvisor.HudTextLayout]::MapFooter($map,'地图 · 名称密集','王国 · 可招募 4 人',390,22,$measure,290,220)
Check ([Math]::Abs($r.Item1.Y-252) -lt .01) '没有敌群时应在实际地图标记下方保留 32 像素适中间距'
$r=[KingdomAdvisor.HudTextLayout]::MapFooter($map,'地图 · 名称密集','王国 · 可招募 4 人',390,22,$measure,290,286)
Check ([Math]::Abs($r.Item1.Y-318) -lt .01) '有敌群时应位于实际敌群数量下方 32 像素'
'通过：地图／王国默认同行，窄屏两行，中英文 18/28 字号完整测量、无重叠及水平越界。'
