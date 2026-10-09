namespace KingdomAdvisor
{
    // 原生坐骑操作：静止按冲刺键，或移动时双击冲刺键。
    public sealed class CompanionInput
    {
        private bool held, doubleTap;
        public bool Step(int direction,bool pressed,bool tapped,bool allowed)
        {
            bool fire=allowed && ((direction==0 && pressed && !held) || (tapped && !doubleTap));
            held=pressed;doubleTap=tapped;return fire;
        }
    }
}
