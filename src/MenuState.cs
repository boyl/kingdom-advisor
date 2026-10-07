using System;
using System.Collections.Generic;
using System.Globalization;

namespace KingdomAdvisor
{
    // 无引擎依赖的输入时间状态，可单独验证长按、回中和连发。
    public sealed class HudButtonGesture
    {
        public bool Active;private bool fired;private float started;
        public int Step(bool down,float now)
        {
            if(down&&!Active){Active=true;fired=false;started=now;}
            if(!Active)return 0;
            if(down&&!fired&&now>=started+.8f){fired=true;return 2;}
            if(!down){int result=fired?0:1;Reset();return result;}
            return 0;
        }
        public void Reset(){Active=fired=false;}
    }
    public static class MenuPaging
    {
        public static int Flip(int focus,int rows,int count,int direction)=>Math.Clamp(focus/Math.Max(1,rows)+direction,0,Math.Max(0,(count-1)/Math.Max(1,rows)))*Math.Max(1,rows);
    }
    public sealed class MenuRepeat
    {
        private int direction;
        private float next;
        public int Step(float value,float now)
        {
            int current=Math.Abs(value)<.6f?0:value>0?1:-1;
            if(current==0){direction=0;return 0;}
            if(current!=direction){direction=current;next=now+.4f;return current;}
            if(now<next)return 0;
            next=now+.16f;return current;
        }
        public void Reset(){direction=0;next=0;}
    }
    public sealed class LayoutPositions
    {
        private readonly Dictionary<string,System.Tuple<float,float>> points=new Dictionary<string,System.Tuple<float,float>>();
        public LayoutPositions(string encoded)
        {
            if(string.IsNullOrWhiteSpace(encoded))return;
            foreach(var item in encoded.Split(';'))
            {
                var kv=item.Split('=');if(kv.Length!=2)throw new FormatException("布局项格式错误");
                var xy=kv[1].Split(',');if(xy.Length!=2)throw new FormatException("布局坐标格式错误");
                Set(kv[0],float.Parse(xy[0],CultureInfo.InvariantCulture),float.Parse(xy[1],CultureInfo.InvariantCulture));
            }
        }
        public System.Tuple<float,float> Get(string key)=>points.TryGetValue(key,out var value)?value:null;
        public void Set(string key,float x,float y)
        {if(float.IsNaN(x)||float.IsNaN(y)||float.IsInfinity(x)||float.IsInfinity(y))throw new FormatException("布局坐标不是有限数值");points[key]=System.Tuple.Create(Math.Clamp(x,0,1),Math.Clamp(y,0,1));}
        public void Clear()=>points.Clear();
        public string Encode(){var rows=new List<string>();foreach(var item in points)rows.Add(item.Key+"="+item.Value.Item1.ToString("R",CultureInfo.InvariantCulture)+","+item.Value.Item2.ToString("R",CultureInfo.InvariantCulture));return string.Join(";",rows);}
    }
}
