using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BepInEx;

namespace KingdomAdvisor
{
    internal sealed class IslandJournal
    {
        private string campaign="";private bool dirty;private DateTime nextSave;
        public readonly List<JournalRecord> Records=new List<JournalRecord>();
        private string FilePath=>Path.Combine(Paths.ConfigPath,"KingdomAdvisor","islands",campaign+".json");
        public void Observe(Snapshot s)
        {
            if(!s.Playing||string.IsNullOrEmpty(s.CampaignKey))return;
            if(campaign!=s.CampaignKey)
            {
                Flush();campaign=s.CampaignKey;Records.Clear();
                if(File.Exists(FilePath)){try{Records.AddRange(JsonSerializer.Deserialize<List<JournalRecord>>(File.ReadAllText(FilePath))??new List<JournalRecord>());}catch(JsonException ex){File.Move(FilePath,FilePath+".invalid-"+DateTime.UtcNow.ToString("yyyyMMddHHmmss"));Plugin.Instance.Log.LogWarning("Island journal invalid, preserved: "+ex.Message);}}
            }
            var record=Records.FirstOrDefault(r=>r.Island==s.Island);if(record==null){record=new JournalRecord{Island=s.Island};Records.Add(record);}
            record.Day=s.Day;record.Utc=DateTime.UtcNow.ToString("O");
            record.Lines=new[]{"科技："+(s.Iron?"高阶":s.Stone?"中阶":"基础"),"营地："+s.Camps+" · 可招募："+s.Beggars}
                .Concat(s.Points.Where(p=>p.Category=="科技"||p.Category=="坐骑"||p.Category=="探索"||p.Category=="特殊"||p.Category=="航行")
                .GroupBy(p=>p.Name).Select(g=>g.Key+" ×"+g.Count())).ToArray();dirty=true;
            if(DateTime.UtcNow>=nextSave){Flush();nextSave=DateTime.UtcNow.AddSeconds(30);}
        }
        public void Flush()
        {if(!dirty||campaign.Length==0)return;Directory.CreateDirectory(Path.GetDirectoryName(FilePath));File.WriteAllText(FilePath,JsonSerializer.Serialize(Records));dirty=false;}
    }
}
