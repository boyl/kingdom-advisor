using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomAdvisor
{
    // 仅缓存相对地图几何；原点、鼠标/手柄焦点和玩家位置不进入静态标签缓存。
    public struct MapProjectionKey : IEquatable<MapProjectionKey>
    {
        public Snapshot Snapshot;
        public int Player,FontSize,Revision;
        public float Left,Right,IslandLeft,Width;
        public bool English,FullMap,Trees,Enemies,Nearby;
        public string Hidden,Temporary;
        public bool Equals(MapProjectionKey other)=>ReferenceEquals(Snapshot,other.Snapshot)
            &&Player==other.Player&&FontSize==other.FontSize&&Revision==other.Revision
            &&Left==other.Left&&Right==other.Right&&IslandLeft==other.IslandLeft&&Width==other.Width
            &&English==other.English&&FullMap==other.FullMap&&Trees==other.Trees&&Enemies==other.Enemies&&Nearby==other.Nearby
            &&Hidden==other.Hidden&&Temporary==other.Temporary;
    }
    // 每个玩家的 HUD/面板各持有一份；只保存最新结果，不按历史岛屿或每种配置累积。
    public sealed class MapProjectionCache<T> where T:class
    {
        private MapProjectionKey key;
        private T value;
        private System.Tuple<float,float>[] explored=Array.Empty<System.Tuple<float,float>>();
        public long Hits,Builds;
        public bool TryGet(MapProjectionKey current,IReadOnlyList<System.Tuple<float,float>> ranges,out T result)
        {
            result=null;
            if(value==null||!key.Equals(current))return false;
            if(!current.FullMap){
                if(explored.Length!=ranges.Count)return false;
                for(int i=0;i<explored.Length;i++)if(explored[i].Item1!=ranges[i].Item1||explored[i].Item2!=ranges[i].Item2)return false;
            }
            Hits++;result=value;return true;
        }
        public void Store(MapProjectionKey current,IReadOnlyList<System.Tuple<float,float>> ranges,T result)
        {key=current;value=result;explored=current.FullMap?Array.Empty<System.Tuple<float,float>>():ranges.ToArray();Builds++;}
        public void Clear(){key=default;value=null;explored=Array.Empty<System.Tuple<float,float>>();}
    }
    public sealed class PreparedMapLayout
    {
        public MapPoint[] Points;
        public MapPoint[][] Groups;
        public string[] Labels;
        public List<MapLabelPlacement> Placements;
        public bool[] Placed;
        public static bool Explored(Snapshot s,float x)=>s.ExploredRanges.Any(r=>x>=r.Item1&&x<=r.Item2);
        public static PreparedMapLayout Build(Snapshot s,MapProjectionKey key,Func<MapPoint,bool> visible,Func<string,float> measure)
        {
            float length=Math.Max(1,key.Right-key.Left);
            var points=s.Points.Where(p=>visible(p)&&p.X>=key.Left&&p.X<=key.Right&&(key.FullMap||Explored(s,p.X)))
                .GroupBy(p=>(p.Name,Math.Round(p.X))).Select(g=>g.First()).OrderBy(p=>p.X);
            var groups=points.GroupBy(p=>(p.Name,MapGrouping.Cell(p.X,key.IslandLeft,length*24/key.Width))).Select(g=>g.ToArray()).ToArray();
            var layout=new PreparedMapLayout{Groups=groups,Points=new MapPoint[groups.Length],Labels=new string[groups.Length],Placed=new bool[groups.Length]};
            var requests=new List<MapLabelRequest>(groups.Length);
            for(int i=0;i<groups.Length;i++){
                var point=groups[i][0];layout.Points[i]=point;
                string label=MapText.Label(point,key.Player,groups[i].Length,groups[i].Sum(p=>Math.Max(0,p.CampPeople)));
                layout.Labels[i]=label;
                requests.Add(new MapLabelRequest{Index=i,Anchor=(point.X-key.Left)/length*key.Width,Width=measure(label)+4,Priority=MapGrouping.Priority(point)});
            }
            layout.Placements=MapLabelLayout.Arrange(requests,key.Width,4);
            foreach(var item in layout.Placements)layout.Placed[item.Index]=true;
            return layout;
        }
    }
}

