using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace KingdomAdvisor
{
    [HarmonyPatch(typeof(SteedAbility),"OnRiderActionStateChanged")]
    internal static class CompanionCarrierAbilityPatch
    {
        private static bool Prefix(SteedAbility __instance)
        {
            var rider=__instance._rider;
            if(!rider && __instance._steed)rider=__instance._steed.Rider;
            return !rider || (AdvisorBehaviour.Active?.Mounts?.Selection(rider.playerId)??0)==0;
        }
    }
    // 原生派生回调直接执行；禁用 MonoBehaviour 不保证动画/委托停止调用。
    [HarmonyPatch]
    internal static class CompanionLizardAbilityPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.DeclaredMethod(typeof(SpitSteedAbility),"OnRiderActionStateChanged");
            yield return AccessTools.DeclaredMethod(typeof(SpitSteedAbility),"Activate");
        }
        private static bool Prefix(SpitSteedAbility __instance,MethodBase __originalMethod)
        {
            var steed=__instance._steed;
            var rider=__instance._rider;
            if(!rider && steed)rider=steed.Rider;
            if(!rider || (AdvisorBehaviour.Active?.Mounts?.Selection(rider.playerId)??0)==0)return true;
            if(__originalMethod.Name=="Activate")Plugin.Instance.Log.LogInfo("Companion blocked native lizard Activate player="+rider.playerId);
            return false;
        }
    }
    [HarmonyPatch(typeof(Player),"SetActionState")]
    internal static class CompanionNativeAttackPatch
    {
        private static bool Prefix(Player __instance,Player.ActionState __0)
        {
            var mounts=AdvisorBehaviour.Active?.Mounts;
            if(mounts==null || !__instance || mounts.Selection(__instance.playerId)==0)return true;
            return __0!=Player.ActionState.Spit && __0!=Player.ActionState.ManualAttack && __0!=Player.ActionState.Rear;
        }
    }
}



