using UnityEngine;
namespace KingdomAdvisor
{
    // 只读原生时钟：单人、房主与加入者使用相同入口，客户端由游戏同步。
    internal sealed class NativeClockReader
    {
        private readonly DayClockTracker tracker=new DayClockTracker();
        public void Reset()=>tracker.Reset();
        public DayClockSnapshot Read(double realTime)
        {
            var m=Managers._Inst;
            if(!m||!m.game||!m.director||Level.isGenerating){Reset();return new DayClockSnapshot();}
            var game=m.game;
            if(game.state!=Game.State.Playing&&game.state!=Game.State.NetworkClientPlaying){Reset();return new DayClockSnapshot();}
            var director=m.director;var times=director.CurrentTimesOfDay;
            if(times==null){Reset();return new DayClockSnapshot();}
            float hour=director.currentTime;
            bool client=game.state==Game.State.NetworkClientPlaying;
            double signature=(double)Time.timeScale*director.ClockSpeedModifier;
            return tracker.Observe(new ClockObservation{
                Context=director.GetInstanceID()+":"+game.currentLand+":"+(m.level?m.level.cachedCurrentLevelSeed:0)+":"+game.state,
                Hour=hour,Day=director.TotalDaysInReign,RealTime=realTime,
                Phase=(ClockPhase)(int)times.GetDayPhase(hour),
                PhaseStarts=new double[]{times.dawnStart,times.dayStart,times.eveningStart,times.nightStart},
                DayStart=director.dayStart,DayEnd=director.dayEnd,
                Paused=director.IsTimePaused||Time.timeScale==0,
                SpeedSignature=signature,
                AuthoritativeRate=client?(double?)null:director._gameHoursPerSecond*signature,
                Client=client
            });
        }
    }
}
