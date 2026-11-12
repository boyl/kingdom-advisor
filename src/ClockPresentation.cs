using System;
namespace KingdomAdvisor
{
    public static class ClockPresentation
    {
        public static string PhaseName(ClockPhase phase)=>phase switch {
            ClockPhase.Dawn=>"黎明",ClockPhase.Day=>"白昼",ClockPhase.Evening=>"黄昏",ClockPhase.Night=>"夜晚",
            _=>throw new ArgumentOutOfRangeException(nameof(phase))};
        public static string Header(DayClockSnapshot clock,bool showClock)=>clock.Available
            ?PhaseName(clock.Phase)+(showClock?" · 游戏时间 "+clock.ClockText:""):"时间暂不可用";
        public static string Countdown(DayClockSnapshot clock)
        {
            if(!clock.Available)return "F6 / F7 重试时间读取";
            string target=clock.UntilDawn?"距黎明":"距入夜";
            if(clock.State==ClockState.Paused)return target+" · 已暂停";
            if(clock.State==ClockState.Stalled)return target+(clock.Client?" · 时间停滞／等待同步":" · 时间停滞");
            if(!clock.RemainingSeconds.HasValue)return target+" · 正在同步";
            var seconds=(long)Math.Ceiling(clock.RemainingSeconds.Value);
            return target+"约 "+string.Format(Localization.Text("{0}分{1}秒（现实时间）"),seconds/60,(seconds%60).ToString("D2"));
        }
    }
}
