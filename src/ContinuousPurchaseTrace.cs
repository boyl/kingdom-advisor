using System;
using System.Collections.Generic;
using System.Globalization;

namespace KingdomAdvisor
{
    public enum PurchaseTraceStage { Filter,NativeAfter,Upgrade,Lifecycle }
    public enum PurchaseTraceReason { NoPress,Touch,Unsupported,OutsideRange,CannotSelect,Created,Released,Client,Faulted,Blocked,AwaitingRelease,InvalidTarget,TargetChanged,NoSelection,Occupied,Funds,NativeWait,CannotPay,Original,Restart,NativeResult,FirstPressAdopted,UpgradeAccepted,UpgradeRejected,Cancelled }
    public struct PurchaseTraceEvent
    {
        public int Frame,Player,Before,After,Selected,Target;
        public PurchaseTraceStage Stage;
        public PurchaseTraceReason Reason;
        public bool Held,Down,Touch,ForwardHeld,ForwardDown,AwaitingRelease;
        public float Distance,Limit;
        public string SelectedName,TargetName,SelectedType,TargetType;
        public bool SameState(PurchaseTraceEvent other)=>Player==other.Player&&Stage==other.Stage&&Reason==other.Reason&&Before==other.Before&&After==other.After&&Selected==other.Selected&&Target==other.Target&&Held==other.Held&&Down==other.Down&&Touch==other.Touch&&ForwardHeld==other.ForwardHeld&&ForwardDown==other.ForwardDown&&AwaitingRelease==other.AwaitingRelease;
    }
    public struct PurchaseNativeObservation
    {
        public bool PhysicalPress,Observed,Held,Down,Touch,ForwardHeld,ForwardDown;
        public int Before;
    }
    // 只保留输入/状态变化；不逐帧格式化字符串或写盘。按玩家槽位去重，场景重建不会累积旧实例键。
    public sealed class ContinuousPurchaseTrace
    {
        private readonly PurchaseTraceEvent[] events;
        private readonly Dictionary<int,PurchaseTraceEvent> latest=new();
        private int count,next;
        public long Calls,HeldCalls,DownCalls,TouchCalls,AfterCalls,Recorded;
        public int Count=>count;
        public ContinuousPurchaseTrace(int capacity=128)
        {if(capacity<1||capacity>128)throw new ArgumentOutOfRangeException(nameof(capacity));events=new PurchaseTraceEvent[capacity];}
        public void ObserveCall(bool held,bool down,bool touch){Calls++;if(held)HeldCalls++;if(down)DownCalls++;if(touch)TouchCalls++;}
        public void ObserveAfter()=>AfterCalls++;
        private static int Key(PurchaseTraceEvent value)=>value.Player*4+(int)value.Stage;
        public bool NeedsRecord(PurchaseTraceEvent value)=>!latest.TryGetValue(Key(value),out var previous)||!value.SameState(previous);
        public bool Record(PurchaseTraceEvent value)
        {
            if(!NeedsRecord(value))return false;
            // 原生玩家槽位应为 0/1；异常槽位仍可观测，但不得形成无界诊断缓存。
            if(latest.Count>=16&&!latest.ContainsKey(Key(value)))latest.Clear();
            latest[Key(value)]=value;events[next]=value;next=(next+1)%events.Length;count=Math.Min(count+1,events.Length);Recorded++;return true;
        }
        public PurchaseTraceEvent[] Snapshot()
        {var result=new PurchaseTraceEvent[count];int start=(next-count+events.Length)%events.Length;for(int i=0;i<count;i++)result[i]=events[(start+i)%events.Length];return result;}
        public string[] Evidence()=>new[]{"continuousPurchaseCalls="+Calls+" held="+HeldCalls+" down="+DownCalls+" touch="+TouchCalls+" nativeAfter="+AfterCalls+" traceCount="+count+" recorded="+Recorded};
        public string[] Lines()
        {
            var rows=new List<string>{"frame\tplayer\tstage\treason\tbefore\tafter\trawHeld\trawDown\ttouch\tforwardHeld\tforwardDown\tselected\tselectedName\tlockedTarget\tlockedName\tawaitRelease\tdistance\tlimit\tselectedType\tlockedType"};
            foreach(var value in Snapshot())rows.Add(value.Frame+"\t"+value.Player+"\t"+value.Stage+"\t"+value.Reason+"\t"+value.Before+"\t"+value.After+"\t"+value.Held+"\t"+value.Down+"\t"+value.Touch+"\t"+value.ForwardHeld+"\t"+value.ForwardDown+"\t"+value.Selected+"\t"+Clean(value.SelectedName)+"\t"+value.Target+"\t"+Clean(value.TargetName)+"\t"+value.AwaitingRelease+"\t"+value.Distance.ToString("F3",CultureInfo.InvariantCulture)+"\t"+value.Limit.ToString("F3",CultureInfo.InvariantCulture)+"\t"+Clean(value.SelectedType)+"\t"+Clean(value.TargetType));
            return rows.ToArray();
        }
        private static string Clean(string value)=>(value??"").Replace('\t',' ').Replace('\r',' ').Replace('\n',' ');
    }
}
