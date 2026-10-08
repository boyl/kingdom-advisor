using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomAdvisor
{
    // 只消费测量结果，独立于 Unity 绘制与游戏数据读取。
    public sealed class MapLabelRequest
    { public int Index,Priority;public float Anchor,Width; }
    public sealed class MapLabelPlacement
    { public int Index,Row;public float Left,Width; }
    public static class MapLabelLayout
    {
        public static List<MapLabelPlacement> Arrange(IEnumerable<MapLabelRequest> requests,float width,int rows)
        {
            var result=new List<MapLabelPlacement>();
            foreach(var request in requests.OrderByDescending(r=>r.Priority).ThenBy(r=>r.Anchor))
            {
                if(request.Width>width)continue;
                float desired=Math.Clamp(request.Anchor-request.Width/2,0,width-request.Width);
                for(int row=0;row<rows;row++)
                {
                    float left=desired;
                    foreach(var other in result.Where(p=>p.Row==row).OrderBy(p=>p.Left))
                    {
                        if(left+request.Width+6<=other.Left)break;
                        if(left<other.Left+other.Width+6)left=other.Left+other.Width+6;
                    }
                    if(left+request.Width>width||Math.Abs(left-desired)>28)continue;
                    result.Add(new MapLabelPlacement{Index=request.Index,Row=row,Left=left,Width=request.Width});break;
                }
            }
            return result;
        }
    }
    public sealed class StableMapExtent
    {
        private int count=-1;private float low,high,since;private System.Tuple<float,float> frozen;
        public void Reset(){count=-1;frozen=null;}
        public System.Tuple<float,float> Update(IEnumerable<float> staticPositions,float groundLeft,float groundRight,float now)
        {
            if(frozen!=null)return frozen;
            var points=staticPositions.ToArray();if(points.Length==0)return null;
            float min=points.Min(),max=points.Max();
            if(count!=points.Length||Math.Abs(min-low)>.25f||Math.Abs(max-high)>.25f){count=points.Length;low=min;high=max;since=now;return null;}
            if(now-since<1.5f)return null;
            frozen=MapExtent.Fit(points,groundLeft,groundRight);return frozen;
        }
    }
    public static class MapGrouping
    {
        // Absolute island coordinates keep group identity independent of a moving viewport.
        public static int Cell(float x,float islandLeft,float unitsPerCell)=>(int)Math.Floor((x-islandLeft)/unitsPerCell);
        public static int Priority(MapPoint p)=>p.CampPeople>=0||p.Name.Contains("城堡")?100:0;
    }
    public static class MapExtent
    {
        public static System.Tuple<float,float> Fit(IEnumerable<float> positions,float groundLeft,float groundRight)
        {
            var xs=positions.Where(x=>x>=groundLeft&&x<=groundRight).ToArray();
            if(xs.Length==0)return System.Tuple.Create(groundLeft,groundRight);
            float low=xs.Min(),high=xs.Max(),pad=Math.Max(12,(high-low)*.02f);
            return System.Tuple.Create(Math.Max(groundLeft,low-pad),Math.Min(groundRight,high+pad));
        }
    }
    public static class MapText
    {
        public static string State(MapPoint point,int player)
        {
            if(point.CampPeople>=0)return "招募 "+point.CampPeople;
            if(!point.PlayerStates.TryGetValue(player,out var t))return "";
            var parts=new List<string>();
            if(t.Level>=0)parts.Add("等级 "+t.Level);
            if(t.UnderConstruction)parts.Add("施工"+(t.Construction>=0?" "+t.Construction.ToString("F0")+"%":""));
            else if(t.HealthMax>0&&t.Health<t.HealthMax)parts.Add("耐久 "+t.Health.ToString("F0")+"/"+t.HealthMax.ToString("F0"));
            if(t.Locked)parts.Add("锁定");
            if(t.Stock>=0)parts.Add("库存 "+t.Stock+"/"+t.StockMax);
            return string.Join(" · ",parts);
        }
        public static string Label(MapPoint point,int player,int count=1,int people=-1)
        {return point.Name+(count>1?" ×"+count:"")+(point.CampPeople>=0?" · "+(people>=0?people:point.CampPeople)+"人":"");}
        public static string Detail(MapPoint point,int player)
        {var state=State(point,player);return point.Name+(state.Length>0?" · "+state:"");}
    }
}
