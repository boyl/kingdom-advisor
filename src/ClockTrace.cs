using System;
using System.Collections.Generic;
using System.Globalization;
namespace KingdomAdvisor
{
    // 固定容量快照，便于核验亚秒跳动；不持续追加日志，不写入存档。
    public sealed class ClockTrace
    {
        private readonly Queue<string> samples=new Queue<string>();
        public void Record(double realTime,double scale,DayClockSnapshot clock)
        {
            string N(double value)=>value.ToString("R",CultureInfo.InvariantCulture);
            samples.Enqueue(N(realTime)+"\t"+N(scale)+"\t"+clock.State+"\t"+N(clock.Hour)+"\t"+clock.Phase+"\t"+clock.UntilDawn+"\t"+(clock.RemainingSeconds.HasValue?N(clock.RemainingSeconds.Value):"")+"\t"+clock.Client);
            while(samples.Count>600)samples.Dequeue();
        }
        public IEnumerable<string> Lines()
        {
            yield return "realSeconds\ttimeScale\tstate\tgameHour\tphase\tuntilDawn\tremainingSeconds\tclient";
            foreach(var sample in samples)yield return sample;
        }
    }
}
