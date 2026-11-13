namespace KingdomAdvisor
{
    public enum PurchasePhase { None,Holding,Transaction,Completed,Cancelling,Other }
    public sealed class PurchaseInput
    {
        public bool Enabled,Held,Down,Allowed,TargetValid,CanPay;
        public int SelectedTarget,SupportedTarget;
        public PurchasePhase Phase;
    }
    public readonly struct PurchaseDecision
    {
        public readonly bool Override,Held,Down,CancelHolding;
        public PurchaseDecision(bool replace,bool held,bool down,bool cancelHolding=false)
        {Override=replace;Held=held;Down=down;CancelHolding=cancelHolding;}
    }
    // 每位本机玩家独立。模型只产生原生按键语义，不修改支付状态、金币或库存。
    public sealed class ContinuousPurchaseState
    {
        public int Target {get;private set;}
        public bool AwaitingRelease {get;private set;}
        private bool waiting;
        private bool syntheticHolding;
        private int previousTarget;
        public void Reset(){Target=previousTarget=0;AwaitingRelease=waiting=syntheticHolding=false;}
        public void Cancel(){Target=previousTarget=0;waiting=syntheticHolding=false;AwaitingRelease=true;}
        public bool AcceptSuccessor(int oldTarget,int newTarget)
        {
            if(Target==0||Target!=oldTarget||newTarget==0||AwaitingRelease)return false;
            previousTarget=Target;Target=newTarget;waiting=true;return true;
        }
        public PurchaseDecision Step(PurchaseInput input)
        {
            if(!input.Enabled){Reset();return new PurchaseDecision(false,input.Held,input.Down);}
            bool holding=input.Phase==PurchasePhase.Holding;
            if(!input.Held){bool cancel=syntheticHolding&&holding;Reset();return new PurchaseDecision(false,false,input.Down,cancel);}
            if(!input.Allowed){Cancel();return Stop(holding);}
            if(AwaitingRelease)return Stop(holding);
            if(input.Phase!=PurchasePhase.None&&!holding)syntheticHolding=false;
            if(Target==0)
            {
                if(!input.Down||input.SupportedTarget==0||input.SelectedTarget!=input.SupportedTarget)
                    return new PurchaseDecision(false,input.Held,input.Down);
                Target=input.SupportedTarget;waiting=!input.CanPay;
            }
            if(!input.TargetValid||(input.SelectedTarget!=0&&input.SelectedTarget!=Target&&input.SelectedTarget!=previousTarget))
            {Cancel();return Stop(holding);}
            if(input.SelectedTarget==Target)previousTarget=0;
            // Completed 包含尚未结算的飞行金币，必须让原生自己完成，不能强制 SetPayState(None)。
            if(input.Phase==PurchasePhase.Completed||input.Phase==PurchasePhase.Cancelling){waiting=true;return Stop();}
            if(input.Phase==PurchasePhase.None&&!input.Down)waiting=true;
            if(waiting)
            {
                if(input.Phase!=PurchasePhase.None||!input.CanPay||input.SelectedTarget!=Target)return Stop(holding);
                waiting=false;syntheticHolding=!input.Down;return new PurchaseDecision(true,true,true);
            }
            if(input.Phase==PurchasePhase.None&&!input.CanPay){waiting=true;return Stop();}
            return new PurchaseDecision(true,input.Held,input.Down);
        }
        private static PurchaseDecision Stop(bool cancelHolding=false)=>new PurchaseDecision(true,false,false,cancelHolding);
    }
}
