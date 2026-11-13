#Requires -Version 7.0
$ErrorActionPreference='Stop'
$source=Join-Path $PSScriptRoot 'src/PerformanceScanCycle.cs'
if(!(Test-Path -LiteralPath $source)){throw '缺少分帧扫描实现'}
$cases=@'
namespace PerformanceScanTests {using System;using System.Linq;using KingdomAdvisor;
public static class ScanCases {
 public static void Check(bool value,string why){if(!value)throw new Exception(why);}
 public static void Run(){
  var scan=new PerformanceScanCycle<int>(4,2);int calls=0;int[] data={10,20,30,40};
  Func<int,int[]> read=i=>{calls++;return new[]{data[i]};};
  scan.Initialize(1,0,read);Check(calls==4&&scan.Initialized,"首次索引必须完整");
  Check(string.Join(",",scan.Items())=="10,20,30,40","首次结果顺序");
  Check(!scan.Step(1,1,read)&&calls==4,"首次索引同帧不得再次全扫");
  Check(!scan.Step(2,.49,read)&&calls==4,"未到切片时间不得扫描");
  data[0]=11;Check(scan.Step(3,.5,read)&&calls==5,"每次仅扫描一个类型");
  Check(string.Join(",",scan.Items())=="11,20,30,40","新结果必须立即发布，其余类型保留");
  Check(!scan.Step(3,10,read)&&calls==5,"同帧多入口不能追赶扫描");
  scan.Step(4,1,read);scan.Step(5,1.5,read);scan.Step(6,2,read);
  Check(calls==8,"两秒内所有类型各更新一次");
  scan.Step(7,20,read);Check(calls==9,"长卡顿后不得同帧补齐全部扫描");
  scan.Step(8,20.016,read);Check(calls==10,"低帧率仍每帧至多一次");
  var previous=string.Join(",",scan.Items());bool threw=false;
  try{scan.Step(9,21,i=>throw new InvalidOperationException("scan failed"));}catch(InvalidOperationException){threw=true;}
  Check(threw&&string.Join(",",scan.Items())==previous,"单次查询失败不得清空既有结果");
  scan.Reset();Check(!scan.Initialized&&!scan.Items().Any(),"换岛清空全部旧对象");
  data[0]=100;scan.Initialize(10,22,read);Check(scan.Items().First()==100,"换岛重新完整建索引");
  scan.Step(11,23,i=>Array.Empty<int>());Check(!scan.Items().Contains(100),"类型查无对象时必须移除旧结果");
 }
}
}
'@
Add-Type -TypeDefinition ((Get-Content -LiteralPath $source -Raw)+"`n"+$cases)
[PerformanceScanTests.ScanCases]::Run()
'通过：完整首次索引、按类型保留/即时替换、两秒轮询、每帧上限、低帧率、异常保留及场景重置。'
