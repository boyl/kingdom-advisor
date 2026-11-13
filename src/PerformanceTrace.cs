using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace KingdomAdvisor
{
    public enum PerformanceStage { World,Population,Payables,StructureScan,Structures,FastRead,Gui,CensusCollect,CensusWrite,Diagnostics,Clock,Journal,Capture }

    // 固定长度内存记录；在下一帧开始时把实际帧间隔关联到上一帧的工作。
    // 子阶段（人口/交互/扫描/普查）属于父阶段耗时，不能再次相加作为总耗时。
    public sealed class PerformanceTrace
    {
        public static PerformanceTrace Active=new PerformanceTrace();
        public struct FrameSample
        {
            public int Frame,Island,Gc0;
            public double Started,FrameMs;
            public bool Playing,Focused,Stable;
            public double World,Population,Payables,StructureScan,Structures,FastRead,Gui,CensusCollect,CensusWrite,Diagnostics,Clock,Journal,Capture;
            public double MeasuredMs=>World+FastRead+Gui+Diagnostics+Clock+Journal+Capture;
        }
        public readonly struct Scope:IDisposable
        {
            private readonly PerformanceTrace trace;private readonly PerformanceStage stage;private readonly long started;
            internal Scope(PerformanceTrace owner,PerformanceStage name){trace=owner;stage=name;started=Stopwatch.GetTimestamp();}
            public void Dispose()=>trace.Record(stage,(Stopwatch.GetTimestamp()-started)*1000.0/Stopwatch.Frequency);
        }
        private readonly FrameSample[] samples;
        private readonly double warmupSeconds;
        private FrameSample current;
        private bool hasCurrent;
        private int count,next,gcAtStart;
        private double stableSince=-1;
        public PerformanceTrace(int capacity=2400,double warmupSeconds=5)
        {
            if(capacity<1||warmupSeconds<0)throw new ArgumentOutOfRangeException();
            samples=new FrameSample[capacity];this.warmupSeconds=warmupSeconds;
        }
        public Scope Measure(PerformanceStage stage)=>new Scope(this,stage);
        public void BeginFrame(int frame,double now,bool playing,int island,bool focused,int managedGc0)
        {
            if(double.IsNaN(now)||double.IsInfinity(now))throw new ArgumentOutOfRangeException(nameof(now));
            if(hasCurrent&&now<current.Started){count=next=0;hasCurrent=false;stableSince=-1;}
            if(hasCurrent&&frame==current.Frame)return;
            if(hasCurrent)
            {
                current.FrameMs=(now-current.Started)*1000;
                current.Gc0=Math.Max(0,managedGc0-gcAtStart);
                if(!playing||island!=current.Island||!focused)current.Stable=false;
                samples[next]=current;next=(next+1)%samples.Length;count=Math.Min(count+1,samples.Length);
            }
            if(!playing)stableSince=-1;
            else if(!hasCurrent||!current.Playing||current.Island!=island||stableSince<0)stableSince=now;
            current=new FrameSample{Frame=frame,Started=now,Playing=playing,Island=island,Focused=focused,Stable=playing&&focused&&now-stableSince>=warmupSeconds};
            gcAtStart=managedGc0;hasCurrent=true;
        }
        public void Record(PerformanceStage stage,double milliseconds)
        {
            if(!hasCurrent)return;
            if(milliseconds<0||double.IsNaN(milliseconds)||double.IsInfinity(milliseconds))throw new ArgumentOutOfRangeException(nameof(milliseconds));
            switch(stage)
            {
                case PerformanceStage.World:current.World+=milliseconds;break;
                case PerformanceStage.Population:current.Population+=milliseconds;break;
                case PerformanceStage.Payables:current.Payables+=milliseconds;break;
                case PerformanceStage.StructureScan:current.StructureScan+=milliseconds;break;
                case PerformanceStage.Structures:current.Structures+=milliseconds;break;
                case PerformanceStage.FastRead:current.FastRead+=milliseconds;break;
                case PerformanceStage.Gui:current.Gui+=milliseconds;break;
                case PerformanceStage.CensusCollect:current.CensusCollect+=milliseconds;break;
                case PerformanceStage.CensusWrite:current.CensusWrite+=milliseconds;break;
                case PerformanceStage.Diagnostics:current.Diagnostics+=milliseconds;break;
                case PerformanceStage.Clock:current.Clock+=milliseconds;break;
                case PerformanceStage.Journal:current.Journal+=milliseconds;break;
                case PerformanceStage.Capture:current.Capture+=milliseconds;break;
                default:throw new ArgumentOutOfRangeException(nameof(stage));
            }
        }
        public FrameSample[] Frames()
        {
            var result=new FrameSample[count];int start=(next-count+samples.Length)%samples.Length;
            for(int i=0;i<count;i++)result[i]=samples[(start+i)%samples.Length];
            return result;
        }
        private static string Number(double value)=>value.ToString("F3",CultureInfo.InvariantCulture);
        public string[] Evidence()
        {
            var durations=new double[count];int stableCount=0,gc0=0;double world=0,gui=0,diagnostics=0;
            for(int i=0;i<count;i++)
            {
                var f=samples[(next-count+i+samples.Length)%samples.Length];if(!f.Stable)continue;
                durations[stableCount++]=f.FrameMs;gc0+=f.Gc0;
                world=Math.Max(world,f.World);gui=Math.Max(gui,f.Gui);diagnostics=Math.Max(diagnostics,f.Diagnostics);
            }
            Array.Sort(durations,0,stableCount);
            return new[]{"performanceFrames="+count,"performanceStableFrames="+stableCount,
                "performanceStableFrameP95Ms="+Number(stableCount==0?0:durations[(int)Math.Ceiling(stableCount*.95)-1]),
                "performanceStableFrameMaxMs="+Number(stableCount==0?0:durations[stableCount-1]),
                "performanceStableWorldMaxMs="+Number(world),"performanceStableGuiMaxMs="+Number(gui),
                "performanceStableDiagnosticsMaxMs="+Number(diagnostics),
                "performanceManagedGc0="+gc0,"performanceWarmupSeconds="+Number(warmupSeconds)};
        }
        public string[] Tsv()
        {
            var rows=new List<string>{"frame\tseconds\tphase\tfocused\tisland\tframeMs\tmanagedGc0\tworldMs\tpopulationMs\tpayablesMs\tstructureScanMs\tstructuresMs\tfastReadMs\tguiMs\tcensusCollectMs\tcensusWriteMs\tdiagnosticsMs\tclockMs\tjournalMs\tcaptureMs"};
            var selected=new List<int>(256);
            for(int i=1;i<=count&&selected.Count<256;i++)
            {
                int index=(next-i+samples.Length)%samples.Length;var f=samples[index];
                if(f.FrameMs>=33.333||f.World>0||f.Diagnostics>0||f.StructureScan>0)selected.Add(index);
            }
            for(int i=selected.Count-1;i>=0;i--)
            {
                var f=samples[selected[i]];
                rows.Add(f.Frame+"\t"+Number(f.Started)+"\t"+(f.Stable?"stable":f.Playing?"warmup-or-background":"not-playing")+"\t"+f.Focused+"\t"+f.Island+"\t"+Number(f.FrameMs)+"\t"+f.Gc0+"\t"+
                    string.Join("\t",new[]{f.World,f.Population,f.Payables,f.StructureScan,f.Structures,f.FastRead,f.Gui,f.CensusCollect,f.CensusWrite,f.Diagnostics,f.Clock,f.Journal,f.Capture}.Select(Number)));
            }
            return rows.ToArray();
        }
    }
}
