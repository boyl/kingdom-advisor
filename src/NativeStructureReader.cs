using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppInterop.Runtime;
using UnityEngine;
namespace KingdomAdvisor
{
    // 只检查对象本体，不遍历父级/子树，避免把附近或被携带对象当成所属设施。
    internal static class NativeStructureReader
    {
        internal static readonly Type[] KnownTypes={
            typeof(MtOlympusGates),
            typeof(ChariotPuzzlePillar),typeof(BunnyStatue),typeof(EggMound),typeof(GreedGolem),typeof(PushableStatue),
            typeof(Monument),typeof(MonumentVolcano),typeof(SpringEdictMonument),typeof(WorldEatingSerpentWeakPoint),
            typeof(MtOlympusStatue),typeof(PayableCrownStatue),
            typeof(LokiPuzzleStaff),typeof(HeimdallPuzzleHorn),typeof(HeimdallPuzzlePillar),
            typeof(ThorPuzzleHammer),typeof(ThorPuzzlePillar),typeof(ThorPuzzleStatue),typeof(HelPuzzleItem),typeof(HelPuzzlePillar),
            typeof(HermesDock),typeof(GiantGoldenAppleTree),typeof(AthenaFarmhouse),typeof(ArtemisCage),typeof(PersephoneCage),typeof(HephaestusForge),
            typeof(TrojanHorseSummoningBell),typeof(ChangeItemOfPowerShop),typeof(ChangeRulerShop),typeof(UnlockNewRulerStatue),typeof(MasonShrine),
            typeof(Oracle),typeof(CerberusPuzzlePillar),typeof(PayableFishingRod),typeof(GreedBuybackTrader),typeof(PayableShieldWallActivator),
            typeof(PayableBorderBanner),typeof(Signpost),typeof(Snowman),typeof(BunnyTholos),typeof(ItemOfPowerReward),
            typeof(Shipyard),typeof(TrojanHorse),typeof(SerpentLevelTrojanHorse),typeof(Lighthouse)
        };
        // 背景纪念碑参与视差/镜头动画，不是道路上的设施。保留图鉴身份，但不扫描为地点。
        internal static readonly Type[] DiscoveryTypes=KnownTypes.Where(t=>!typeof(Payable).IsAssignableFrom(t)&&Catalog.ResolveStructureType(t.Name)?.Key!="monument").ToArray();
        public static Entry Resolve(GameObject obj)
        {
            if(!obj)return null;
            // 明确原生组件优先于可能重复或误导的预制体名称。
            Entry background=null;
            foreach(var component in obj.GetComponents(Il2CppType.Of<MonoBehaviour>()))
            {
                if(!component)continue;
                var entry=Catalog.ResolveStructureType(component.GetIl2CppType().Name);
                if(entry?.Key=="monument")background=entry;
                else if(entry!=null)return entry;
            }
            return background;
        }
        // 扫描与消费缓存时均检查，拾取/挂载后不能继续把角色附属物当地点。
        public static bool IsWorldStructure(Component component)=>component&&component.gameObject.activeInHierarchy
            &&!component.gameObject.GetComponentInParent(Il2CppType.Of<Player>())
            &&!component.gameObject.GetComponentInParent(Il2CppType.Of<Steed>());
        public static IEnumerable<Component> Discover()
        {
            var seen=new HashSet<int>();
            foreach(var type in DiscoveryTypes)
            {
                // 所有Payable子类由游戏交互注册表覆盖；此处补非Payable专用设施。
                foreach(var component in DiscoverType(type))
                {
                    if(seen.Add(component.gameObject.GetInstanceID()))yield return component;
                }
            }
        }
        public static IEnumerable<Component> DiscoverType(Type type)
        {
            foreach(var obj in UnityEngine.Object.FindObjectsOfType(Il2CppType.From(type)))
            {
                var component=obj.TryCast<Component>();
                if(IsWorldStructure(component))yield return component;
            }
        }
        public static string Evidence(GameObject obj)=>string.Join(",",obj.GetComponents(Il2CppType.Of<MonoBehaviour>()).Where(c=>c).Select(c=>c.GetIl2CppType().Name));
    }
}
