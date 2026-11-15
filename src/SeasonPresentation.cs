using System;
namespace KingdomAdvisor
{
    public static class SeasonPresentation
    {
        public static string Name(string season)=>season switch {"Spring"=>"春季","Summer"=>"夏季","Autumn" or "Fall"=>"秋季","Winter"=>"冬季",_=>season??""};
        public static string Hint(string season)=>season switch {
            "Spring"=>"草地复苏，恢复农耕。",
            "Summer"=>"农猎旺季，积蓄金币。",
            "Autumn" or "Fall"=>"储备金币，巩固防线。",
            "Winter"=>"农田停产，改采浆果。",_=>""};
        public static (string Next,int Days) Forecast(string current,int day,string tomorrow,Func<int,string> seasonAt)
        {
            if(Hint(current).Length==0||day<0||day>int.MaxValue-128||seasonAt(day)!=current||seasonAt(day+1)!=tomorrow)return ("",-1);
            for(int days=1;days<=128;days++)
            {
                string next=seasonAt(day+days);
                if(Hint(next).Length==0)return ("",-1);
                if(next!=current)return (next,days);
            }
            return ("",-1);
        }
        public static string Header(string current,string next,int days)
        {
            string name=Localization.Text(Name(current));
            if(name.Length==0)return Localization.Text("季节未知");
            return name+" · "+(days>0&&Hint(next).Length>0?string.Format(Localization.Text("距{0} {1} 天"),Localization.Text(Name(next)),days):Localization.Text("换季时间未知"));
        }
    }
}
