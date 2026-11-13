using System;
using System.Collections.Generic;

namespace KingdomAdvisor
{
    // 首次完整索引后把相同周期的类型扫描分散到不同帧；每帧最多推进一个类型。
    // 低帧率或暂停后不在一帧补齐欠账，实际全周期会随可用帧数延长。
    public sealed class PerformanceScanCycle<T>
    {
        private readonly int sourceCount;
        private readonly double sliceSeconds;
        private T[][] parts;
        private int nextSource,lastFrame=int.MinValue;
        public bool Initialized{get;private set;}
        public double NextDue{get;private set;}
        public PerformanceScanCycle(int sourceCount,double cycleSeconds)
        {
            if(sourceCount<1||cycleSeconds<=0)throw new ArgumentOutOfRangeException();
            this.sourceCount=sourceCount;sliceSeconds=cycleSeconds/sourceCount;parts=new T[sourceCount][];
        }
        public void Initialize(int frame,double now,Func<int,T[]> read)
        {
            var initial=new T[sourceCount][];
            for(int i=0;i<sourceCount;i++)initial[i]=read(i)??throw new InvalidOperationException("Missing scan result");
            parts=initial;nextSource=0;lastFrame=frame;NextDue=now+sliceSeconds;Initialized=true;
        }
        public bool Step(int frame,double now,Func<int,T[]> read)
        {
            if(!Initialized)throw new InvalidOperationException("Scan must be initialized");
            if(frame==lastFrame||now<NextDue)return false;
            lastFrame=frame;
            parts[nextSource]=read(nextSource)??throw new InvalidOperationException("Missing scan result");
            nextSource=(nextSource+1)%sourceCount;
            NextDue=Math.Max(NextDue+sliceSeconds,now);
            return true;
        }
        public IEnumerable<T> Items(int start=0,int length=-1)
        {
            if(length<0)length=sourceCount-start;
            if(start<0||length<0||start+length>sourceCount)throw new ArgumentOutOfRangeException();
            for(int i=start;i<start+length;i++)if(parts[i]!=null)foreach(var item in parts[i])yield return item;
        }
        public void Reset()
        {
            parts=new T[sourceCount][];Initialized=false;nextSource=0;lastFrame=int.MinValue;NextDue=0;
        }
    }
}
