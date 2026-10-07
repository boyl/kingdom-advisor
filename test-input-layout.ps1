#Requires -Version 7.0
$ErrorActionPreference='Stop'
Add-Type -TypeDefinition ([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'src/MenuState.cs')))
function Assert-Equal($actual,$expected,$label){if($actual -ne $expected){throw "$label : $actual，预期 $expected"}}
$repeat=[KingdomAdvisor.MenuRepeat]::new()
Assert-Equal ($repeat.Step(0.59,0)) 0 '死区'
Assert-Equal ($repeat.Step(1,0.01)) 1 '首次按下立即响应'
Assert-Equal ($repeat.Step(1,0.2)) 0 '长按延迟'
Assert-Equal ($repeat.Step(1,0.42)) 1 '长按连发'
Assert-Equal ($repeat.Step(1,0.45)) 0 '同窗口不重复'
Assert-Equal ($repeat.Step(-1,0.46)) -1 '反向立即响应'
Assert-Equal ($repeat.Step(0,0.47)) 0 '回中'
Assert-Equal ($repeat.Step(-1,0.48)) -1 '回中后再次按下'
$repeat.Reset()
Assert-Equal ($repeat.Step(1,0.49)) 1 '页面切换复位'
$layout=[KingdomAdvisor.LayoutPositions]::new('')
$layout.Set('p0.map',0.75,0.25)
$layout.Set('p1.map',0.1,0.9)
$copy=[KingdomAdvisor.LayoutPositions]::new($layout.Encode())
Assert-Equal ($copy.Get('p0.map').Item1) 0.75 '保存后恢复横坐标'
Assert-Equal ($copy.Get('p1.map').Item2) ([float]0.9) '双人位置独立'
$copy.Set('p0.status',-1,2)
Assert-Equal ($copy.Get('p0.status').Item1) 0 '左边界限制'
Assert-Equal ($copy.Get('p0.status').Item2) 1 '下边界限制'
$invalidRejected=$false
try{$copy.Set('p0.map',[float]::NaN,0)}catch{$invalidRejected=$true}
if(!$invalidRejected){throw '非法配置未拒绝'}
$copy.Clear()
Assert-Equal ($copy.Encode()) '' '一键复位清空位置'
'通过：9 个摇杆时序场景、双人布局保存恢复、边界限制、非法坐标和复位。'
foreach($focus in 0..10){Assert-Equal ([KingdomAdvisor.MenuPaging]::Flip($focus,10,11,1)) 10 "11项下一页:$focus";Assert-Equal ([KingdomAdvisor.MenuPaging]::Flip($focus,10,11,-1)) 0 "11项上一页:$focus"}
Assert-Equal ([KingdomAdvisor.MenuPaging]::Flip(9,10,30,1)) 10 '第一页尾部到第二页首'
Assert-Equal ([KingdomAdvisor.MenuPaging]::Flip(19,10,30,1)) 20 '第二页尾部到第三页首'
Assert-Equal ([KingdomAdvisor.MenuPaging]::Flip(29,10,30,1)) 20 '末页不回绕'
Assert-Equal ([KingdomAdvisor.MenuPaging]::Flip(0,10,3,-1)) 0 '首屏不回绕'
'通过：设置分页首尾、部分末页、多页和末页边界。'
$gesture=[KingdomAdvisor.HudButtonGesture]::new()
Assert-Equal ($gesture.Step($true,0)) 0 '呼出键按下不抢先打开面板'
Assert-Equal ($gesture.Step($false,0.1)) 1 '短按释放打开面板'
Assert-Equal ($gesture.Step($false,0.2)) 0 '短按不重复'
Assert-Equal ($gesture.Step($true,1)) 0 '长按开始'
Assert-Equal ($gesture.Step($true,1.81)) 2 '长按切换HUD'
Assert-Equal ($gesture.Step($true,2.5)) 0 '持续长按不得重复'
Assert-Equal ($gesture.Step($false,3)) 0 '长按释放不得打开面板'
Assert-Equal ($gesture.Step($true,4)) 0 '再次按下'
Assert-Equal ($gesture.Step($true,4.81)) 2 '隐藏后仍可切回'
$gesture.Reset()
Assert-Equal $gesture.Active $false '生命周期复位'
'通过：呼出键短长按分离、持续锁存、长按释放不打开、再次切回。'
