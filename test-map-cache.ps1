#Requires -Version 7.0
param([switch]$Benchmark)
$ErrorActionPreference='Stop'
$sources=@('Model.cs','MapPresentation.cs','MapRenderCache.cs' | ForEach-Object {[regex]::Replace([IO.File]::ReadAllText((Join-Path $PSScriptRoot "src/$_")),'(?m)^using [^;]+;\r?\n','')})
$tests=@'
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using KingdomAdvisor;
public static class MapCacheTests {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static List<MapLabelPlacement> LegacyArrange(IEnumerable<MapLabelRequest> requests,float width,int rows){
  var result=new List<MapLabelPlacement>();
  foreach(var request in requests.OrderByDescending(r=>r.Priority).ThenBy(r=>r.Anchor)){
   if(request.Width>width)continue;
   float desired=Math.Clamp(request.Anchor-request.Width/2,0,width-request.Width);
   for(int row=0;row<rows;row++){
    float left=desired;
    foreach(var other in result.Where(p=>p.Row==row).OrderBy(p=>p.Left)){
     if(left+request.Width+6<=other.Left)break;
     if(left<other.Left+other.Width+6)left=other.Left+other.Width+6;
    }
    if(left+request.Width>width||Math.Abs(left-desired)>28)continue;
    result.Add(new MapLabelPlacement{Index=request.Index,Row=row,Left=left,Width=request.Width});break;
   }
  }
  return result;
 }
 static void LegacyBuild(Snapshot s,MapProjectionKey key){
  var points=s.Points.Where(p=>p.X>=key.Left&&p.X<=key.Right).GroupBy(p=>p.Name+"|"+Math.Round(p.X)).Select(g=>g.First()).OrderBy(p=>p.X).ToArray();
  var groups=points.GroupBy(p=>p.Name+"|"+MapGrouping.Cell(p.X,key.IslandLeft,(key.Right-key.Left)*24/key.Width)).ToArray();
  var counts=groups.Select(g=>g.Count()).ToArray();points=groups.Select(g=>g.First()).ToArray();
  var requests=new List<MapLabelRequest>();var labels=new string[points.Length];
  for(int i=0;i<points.Length;i++){
   labels[i]=MapText.Label(points[i],0,counts[i],groups[i].Sum(p=>Math.Max(0,p.CampPeople)));
   requests.Add(new MapLabelRequest{Index=i,Anchor=(points[i].X-key.Left)/(key.Right-key.Left)*key.Width,Width=labels[i].Length*18+4,Priority=MapGrouping.Priority(points[i])});
  }
  var placed=LegacyArrange(requests,key.Width,4);var indices=new HashSet<int>(placed.Select(p=>p.Index));
 }
 public static string Benchmark(){
  var s=new Snapshot();for(int i=0;i<600;i++)s.Points.Add(new MapPoint{Key="wall",Name="地点"+(i%13),Category="防御",X=i});
  var key=new MapProjectionKey{Snapshot=s,Left=0,Right=600,Width=900,FullMap=true};
  var cache=new MapProjectionCache<PreparedMapLayout>();Func<string,float> measure=t=>t.Length*18;
  for(int i=0;i<30;i++){LegacyBuild(s,key);PreparedMapLayout.Build(s,key,p=>true,measure);}
  long bytes=GC.GetAllocatedBytesForCurrentThread();var timer=System.Diagnostics.Stopwatch.StartNew();
  for(int i=0;i<1200;i++)LegacyBuild(s,key);
  timer.Stop();double oldMs=timer.Elapsed.TotalMilliseconds;long oldBytes=GC.GetAllocatedBytesForCurrentThread()-bytes;
  bytes=GC.GetAllocatedBytesForCurrentThread();timer.Restart();
  for(int i=0;i<1200;i++){
   // 十秒、60帧、每帧两次GUI：每秒新世界快照，其余调用应复用。
   if(i%120==0){var next=new Snapshot();next.Points.AddRange(s.Points);key.Snapshot=next;}
   if(!cache.TryGet(key,s.ExploredRanges,out var layout)){layout=PreparedMapLayout.Build(key.Snapshot,key,p=>true,measure);cache.Store(key,s.ExploredRanges,layout);}
  }
  timer.Stop();long newBytes=GC.GetAllocatedBytesForCurrentThread()-bytes;
  Check(cache.Builds==10&&cache.Hits==1190,"世界刷新频率或同帧复用错误");
  return FormattableString.Invariant($"synthetic points=600 calls=1200 oldMs={oldMs:F3} newMs={timer.Elapsed.TotalMilliseconds:F3} oldBytes={oldBytes} newBytes={newBytes} builds={cache.Builds} hits={cache.Hits}");
 }
 public static string Run(){
  var s=new Snapshot();s.ExploredRanges.Add(Tuple.Create(-100f,100f));
  for(int i=0;i<300;i++)s.Points.Add(new MapPoint{Key=i%3==0?"tree":"wall",Name=i%3==0?"树木":"城墙",Category="防御",X=-100+i,CampPeople=i%27==0?2:-1});
  var key=new MapProjectionKey{Snapshot=s,Left=-100,Right=200,IslandLeft=-100,Width=900,FontSize=18,Hidden="",Temporary="",FullMap=true};
  int measured=0;Func<string,float> measure=text=>{measured++;return text.Length*18;};
  var cache=new MapProjectionCache<PreparedMapLayout>();
  Check(!cache.TryGet(key,s.ExploredRanges,out _),"首次读取不能命中");
  var layout=PreparedMapLayout.Build(s,key,p=>true,measure);
  cache.Store(key,s.ExploredRanges,layout);int initial=measured;
  for(int i=0;i<600;i++)Check(cache.TryGet(key,s.ExploredRanges,out var same)&&ReferenceEquals(same,layout),"重复绘制没有复用布局");
  Check(measured==initial,"命中缓存仍测量文字");
  var expected=s.Points.GroupBy(p=>p.Name+"|"+Math.Round(p.X)).Select(g=>g.First()).OrderBy(p=>p.X)
   .GroupBy(p=>p.Name+"|"+MapGrouping.Cell(p.X,key.IslandLeft,(key.Right-key.Left)*24/key.Width)).ToArray();
  Check(layout.Points.Length==expected.Length,"缓存分组与原算法不符");
  for(int i=0;i<expected.Length;i++){
   Check(ReferenceEquals(layout.Points[i],expected[i].First()),"代表地点变化");
   Check(layout.Groups[i].SequenceEqual(expected[i]),"合并成员变化");
   Check(layout.Labels[i]==MapText.Label(expected[i].First(),0,expected[i].Count(),expected[i].Sum(p=>Math.Max(0,p.CampPeople))),"标签/营地人数变化");
  }
  foreach(var field in typeof(MapProjectionKey).GetFields()){
   object boxed=key;
   object value=field.FieldType==typeof(Snapshot)?new Snapshot():field.FieldType==typeof(float)?(float)field.GetValue(boxed)+1:
    field.FieldType==typeof(int)?(int)field.GetValue(boxed)+1:field.FieldType==typeof(bool)?!(bool)field.GetValue(boxed):"changed";
   field.SetValue(boxed,value);Check(!cache.TryGet((MapProjectionKey)boxed,s.ExploredRanges,out _),"输入变化未失效："+field.Name);
  }
  s.ExploredRanges[0]=Tuple.Create(-100f,150f);
  Check(cache.TryGet(key,s.ExploredRanges,out _),"全图模式不应因探索边界变化失效");
  key.FullMap=false;cache.Store(key,s.ExploredRanges,layout);s.ExploredRanges[0]=Tuple.Create(-100f,160f);
  Check(!cache.TryGet(key,s.ExploredRanges,out _),"探索范围变化未失效");
  var explored=PreparedMapLayout.Build(s,key,p=>p.Key!="tree",measure);
  Check(explored.Points.All(p=>p.X<=160&&p.Key!="tree"),"探索边界/隐藏分类失效");
  var other=new MapProjectionCache<PreparedMapLayout>();Check(!other.TryGet(key,s.ExploredRanges,out _),"玩家/面板缓存必须隔离");
  cache.Clear();Check(!cache.TryGet(key,s.ExploredRanges,out _),"退出场景未清理缓存");
  var random=new Random(17);
  foreach(float width in new[]{280f,760f,1800f})for(int round=0;round<15;round++){
   var requests=Enumerable.Range(0,100).Select(i=>new MapLabelRequest{Index=i,Anchor=(float)random.NextDouble()*width,Width=20+random.Next(300),Priority=random.Next(3)}).ToArray();
   var before=LegacyArrange(requests,width,4);var after=MapLabelLayout.Arrange(requests,width,4);
   Check(before.Count==after.Count,"行缓存改变标签放置数量");
   for(int i=0;i<before.Count;i++)Check(before[i].Index==after[i].Index&&before[i].Row==after[i].Row&&before[i].Left==after[i].Left&&before[i].Width==after[i].Width,"行缓存改变标签几何");
  }
  return "通过：旧算法分组/标签等价、600次命中不重测、全部缓存键失效、探索/全图、筛选、实例隔离及退出复位。";
 }
}
'@
Add-Type -TypeDefinition ($tests.Substring(0,$tests.IndexOf('public static class'))+($sources -join "`n")+$tests.Substring($tests.IndexOf('public static class')))
[MapCacheTests]::Run()
if($Benchmark){[MapCacheTests]::Benchmark()}
