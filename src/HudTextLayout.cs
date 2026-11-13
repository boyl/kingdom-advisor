using System;
using System.Collections.Generic;

namespace KingdomAdvisor
{
    public readonly struct HudRect
    {
        public readonly float X,Y,Width,Height;
        public float Right=>X+Width;
        public float Bottom=>Y+Height;
        public HudRect(float x,float y,float width,float height){X=x;Y=y;Width=width;Height=height;}
        public bool Overlaps(HudRect other)=>Width>0&&Height>0&&other.Width>0&&other.Height>0&&X<other.Right&&Right>other.X&&Y<other.Bottom&&Bottom>other.Y;
    }
    // 输入已经本地化；测量器由运行时实际 GUIStyle 和正文宽度提供。
    public sealed class HudTextRow
    {
        public string Text,Icon;
        public string Label,Value;
        public float LabelWidth,LabelHeight,ValueWidth,ValueHeight,ValueTop;
        public float Height,TextHeight,ExtraGap;
        public int Column;
        public HudTextRow(string text,string icon=""){Text=text??"";Icon=icon??"";}
    }
    public sealed class HudTextPage
    {
        public readonly List<HudTextRow> Rows=new();
        public float Height;
        public int Columns=1;
    }
    public static class HudTextLayout
    {
        public static (HudRect Tip,HudRect Alert) MapFooter(HudRect area,string tip,string alert,float alertWidth,float lineHeight,Func<string,float,float> height,float tipPreferredWidth=390,float contentBottom=float.NaN)
        {
            const float gap=24,padding=4;
            float innerWidth=area.Width-36;
            bool beside=innerWidth>=Math.Max(260,12*lineHeight)+gap+alertWidth;
            float tipWidth=beside?Math.Min(innerWidth-alertWidth-gap,Math.Max(260,tipPreferredWidth)):innerWidth;
            float tipHeight=height(tip,tipWidth),alertHeight=alert.Length==0?0:height(alert,Math.Min(alertWidth,innerWidth)-32)+padding*2;
            // 以实际绘制内容为锚点，不为空敌群行预留整块高度；测量替身未提供时使用最小内容基线。
            float top=(float.IsNaN(contentBottom)?area.Y+78+2*(lineHeight+5)+18:contentBottom)+14;
            var tipRect=new HudRect(area.X+18,top,tipWidth,tipHeight);
            var alertRect=new HudRect(beside?tipRect.Right+gap-16:area.X+2,beside?top-padding:tipRect.Bottom+8,Math.Min(alertWidth,innerWidth),alertHeight);
            return (tipRect,alertRect);
        }
        public const float RowGap=5;
        public const float SingleRowGap=RowGap;
        public const float StatusGroupGap=10;
        public const float ColumnGap=20;
        public const float ValueGap=16;
        private static float GlyphWidth(string value,Func<string,float> measureWidth)=>(float)Math.Ceiling(measureWidth(value))+2;
        public static float MetricWidth(string label,string value,Func<string,float> measureWidth)=>GlyphWidth(label,measureWidth)+ValueGap+GlyphWidth(value,measureWidth);
        // 标签和值在本地化之后分别测量；放不下时整段值换行，不缩字、不截断。
        public static HudTextRow Metric(string label,string value,string icon,float width,Func<string,float> measureWidth,Func<string,float,float> measureHeight)
        {
            // 动态字体的分数像素宽度须向上取整并留出末尾字形余量，否则英文末字母会被挤到裁切行。
            float labelWidth=GlyphWidth(label,measureWidth),valueWidth=GlyphWidth(value,measureWidth);
            bool stacked=labelWidth+ValueGap+valueWidth>width;
            var row=new HudTextRow(label+" "+value,icon){Label=label,Value=value,LabelWidth=stacked?width:labelWidth,ValueWidth=stacked?width:width-labelWidth-ValueGap};
            row.LabelHeight=measureHeight(label,row.LabelWidth);row.ValueHeight=measureHeight(value,row.ValueWidth);
            row.ValueTop=stacked?row.LabelHeight+2:0;
            row.TextHeight=Math.Max(row.LabelHeight,row.ValueTop+row.ValueHeight);
            return row;
        }
        public static float CellWidth(float cardWidth,int columns)=>(cardWidth-32-ColumnGap*(columns-1))/columns;
        public static float CellLeft(float cardLeft,float cardWidth,int columns,int column)=>cardLeft+16+column*(CellWidth(cardWidth,columns)+ColumnGap);
        public static string StatusText(string text,float width,Func<string,float> measureWidth)
        {
            if(measureWidth(text)<=width)return text;
            int separator=text.IndexOf(" · ",StringComparison.Ordinal);
            if(separator<0)return text;
            string first=text.Substring(0,separator),second=text.Substring(separator+3);
            return measureWidth(first)<=width&&measureWidth(second)<=width?first+"\n"+second:text;
        }
        public static HudTextPage[] ColumnPages(IEnumerable<HudTextRow> left,IEnumerable<HudTextRow> right,float availableHeight,float pagerHeight,float minimumLineHeight,Func<string,float> measure)
        {
            if(!CanPage(availableHeight,pagerHeight,minimumLineHeight))return Array.Empty<HudTextPage>();
            var first=Pages(left,availableHeight,measure);var second=Pages(right,availableHeight,measure);
            if(first.Length>1||second.Length>1)
            {
                first=Pages(left,availableHeight-pagerHeight-8,measure);
                second=Pages(right,availableHeight-pagerHeight-8,measure);
            }
            var result=new HudTextPage[Math.Max(first.Length,second.Length)];
            for(int i=0;i<result.Length;i++)
            {
                var page=new HudTextPage{Columns=2};result[i]=page;
                if(i<first.Length){page.Rows.AddRange(first[i].Rows);page.Height=first[i].Height;}
                if(i<second.Length){foreach(var row in second[i].Rows){row.Column=1;page.Rows.Add(row);}page.Height=Math.Max(page.Height,second[i].Height);}
            }
            return result;
        }
        public static HudRect MainPanel(float viewportWidth,float viewportHeight)
        {
            float width=Math.Min(1040,viewportWidth-40),height=Math.Min(720,viewportHeight-40);
            return new HudRect((float)Math.Round((viewportWidth-width)/2),(float)Math.Round((viewportHeight-height)/2),width,height);
        }
        public static bool CardVisible(HudRect card,HudRect mainPanel,bool panelOpen)=>!panelOpen||!card.Overlaps(mainPanel);
        public static HudRect AboveAlert(HudRect advisor,float alertHeight,float gap)
            =>new HudRect(advisor.X,advisor.Y-gap-alertHeight,advisor.Width,alertHeight);
        public static bool TryPlaceAlert(HudRect desired,HudRect safe,HudRect detail,IReadOnlyList<HudRect> occupied,float gap,out HudRect placement,HudRect? preferred=null)
        {
            placement=desired;
            if(Fits(desired))return true;
            if(preferred.HasValue&&Fits(preferred.Value)){placement=preferred.Value;return true;}
            if(detail.Width>0&&detail.Height>0)
            {
                placement=new HudRect(detail.X,detail.Bottom+gap,desired.Width,desired.Height);
                if(Fits(placement))return true;
                placement=new HudRect(detail.X,detail.Y-gap-desired.Height,desired.Width,desired.Height);
                if(Fits(placement))return true;
            }
            // 空位的边缘来自现有卡片矩形；没有空位时本帧不显示可选警报，空间恢复后自动再试。
            var xs=new List<float>{desired.X,safe.X,safe.Right-desired.Width};
            var ys=new List<float>{desired.Y,safe.Y,safe.Bottom-desired.Height};
            foreach(var item in occupied)
            {
                xs.Add(item.X);xs.Add(item.Right+gap);xs.Add(item.X-gap-desired.Width);
                ys.Add(item.Bottom+gap);ys.Add(item.Y-gap-desired.Height);
            }
            foreach(float y in ys)foreach(float x in xs)
            {
                placement=new HudRect(x,y,desired.Width,desired.Height);
                if(Fits(placement))return true;
            }
            placement=default;return false;

            bool Fits(HudRect candidate)
            {
                if(candidate.Width<=0||candidate.Height<=0||candidate.X<safe.X||candidate.Y<safe.Y||candidate.Right>safe.Right||candidate.Bottom>safe.Bottom)return false;
                foreach(var item in occupied)
                    if(candidate.X<item.Right+gap&&candidate.Right+gap>item.X&&candidate.Y<item.Bottom+gap&&candidate.Bottom+gap>item.Y)return false;
                return true;
            }
        }
        public static float FooterTop(float viewportHeight,float buttonHeight,float hintHeight)
            =>viewportHeight-16-buttonHeight-(hintHeight>0?hintHeight+8:0);
        public static string DayHeader(string date,string time,float availableWidth,Func<string,float> measureWidth)
        {
            string single=date+" / "+time;
            return measureWidth(single)<=availableWidth?single:date+"\n"+time;
        }
        public static bool CanPage(float availableHeight,float pagerHeight,float minimumLineHeight,float rowGap=RowGap)
            =>availableHeight>=minimumLineHeight+rowGap+pagerHeight+8;
        // 极矮视口返回明确的不可布局结果，由视图提示用户调整显示区域；不让正文异常停用整个覆盖层。
        public static HudTextPage[] Paged(IEnumerable<HudTextRow> rows,float availableHeight,float pagerHeight,float minimumLineHeight,Func<string,float> measure,float rowGap=RowGap)
        {
            float maxGap=rowGap;foreach(var row in rows)maxGap=Math.Max(maxGap,rowGap+row.ExtraGap);
            if(!CanPage(availableHeight,pagerHeight,minimumLineHeight,maxGap))return Array.Empty<HudTextPage>();
            var pages=Pages(rows,availableHeight,measure,rowGap);
            return pages.Length>1?Pages(rows,availableHeight-pagerHeight-8,measure,rowGap):pages;
        }
        public static HudTextPage[] Pages(IEnumerable<HudTextRow> rows,float availableHeight,Func<string,float> measure,float rowGap=RowGap)
        {
            if(availableHeight<=rowGap)throw new ArgumentOutOfRangeException(nameof(availableHeight));
            var pages=new List<HudTextPage>();var page=new HudTextPage();pages.Add(page);
            foreach(var row in rows)
            {
                float gap=rowGap+row.ExtraGap;
                string remaining=row.Text;
                bool paired=row.Label!=null;
                do
                {
                    float height=(paired?row.TextHeight:measure(remaining))+gap;
                    if(height<=availableHeight)
                    {
                        if(page.Height+height>availableHeight&&page.Rows.Count>0){page=new HudTextPage();pages.Add(page);}
                        Add(remaining,row,height,gap,paired);break;
                    }
                    // 极端长值超过整页时，退回完整文本分片；不丢标签或值，也不保留失效的双区矩形。
                    if(paired){paired=false;continue;}
                    // 单条说明也可能超过一屏，按测量结果拆分，保留全部字符及 Unicode 字符边界。
                    if(page.Rows.Count>0){page=new HudTextPage();pages.Add(page);}
                    int low=1,high=remaining.Length,fit=0;
                    while(low<=high){int mid=(low+high)/2;if(measure(remaining.Substring(0,mid))+gap<=availableHeight){fit=mid;low=mid+1;}else high=mid-1;}
                    if(fit==0)throw new InvalidOperationException("HUD正文空间不足以显示一个字符");
                    if(fit<remaining.Length&&char.IsLowSurrogate(remaining[fit])&&char.IsHighSurrogate(remaining[fit-1]))fit--;
                    if(fit==0)throw new InvalidOperationException("HUD正文空间不足以显示一个字符");
                    if(fit<remaining.Length&&!char.IsWhiteSpace(remaining[fit])&&!char.IsWhiteSpace(remaining[fit-1]))
                    {int space=remaining.LastIndexOf(' ',fit-1,fit);if(space>=0)fit=space+1;}
                    string part=remaining.Substring(0,fit);Add(part,row,measure(part)+gap,gap,false);
                    remaining=remaining.Substring(fit);
                    if(remaining.Length>0){page=new HudTextPage();pages.Add(page);}
                }while(remaining.Length>0);
            }
            return pages.ToArray();

            void Add(string value,HudTextRow source,float height,float gap,bool paired)
            {
                var result=new HudTextRow(value,source.Icon){Height=height,TextHeight=height-gap,ExtraGap=source.ExtraGap};
                if(paired){result.Label=source.Label;result.Value=source.Value;result.LabelWidth=source.LabelWidth;result.LabelHeight=source.LabelHeight;result.ValueWidth=source.ValueWidth;result.ValueHeight=source.ValueHeight;result.ValueTop=source.ValueTop;}
                page.Rows.Add(result);page.Height+=height;
            }
        }
    }
}
