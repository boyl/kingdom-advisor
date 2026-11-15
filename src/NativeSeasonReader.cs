namespace KingdomAdvisor
{
    internal sealed class NativeSeasonReader
    {
        private string cachedKey="";
        private (string Next,int Days) cached=("",-1);
        public (string Next,int Days) Read(Director director,string context)
        {
            int day=director.CurrentSeasonDay;
            string current=director.CurrentSeason.ToString();
            string key=context+":"+director.GetInstanceID()+":"+day+":"+current;
            if(key==cachedKey)return cached;
            var result=SeasonPresentation.Forecast(current,day,director.GetNextDaySeason().ToString(),value=>director.GetSeasonFromDay(value).ToString());
            cached=result;cachedKey=key;return result;
        }
    }
}
