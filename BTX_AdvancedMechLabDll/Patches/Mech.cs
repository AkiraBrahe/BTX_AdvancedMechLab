using BattleTech;

namespace BTX_AdvancedMechLab.Patches
{
    /// <summary>
    /// Gives a heat penalty to DHS engines in battle to match BEX.
    /// </summary>
    [HarmonyPatch(typeof(Mech), "InitStats")]
    public static class Mech_InitStats
    {
        [HarmonyPostfix]
        public static void Postfix(Mech __instance)
        {
            if (__instance.MechDef.MechTags.GetCoolingType() is HeatSinkType.Double or HeatSinkType.ClanDouble)
            {
                var effectManager = UnityGameInstance.BattleTechGame.Combat.EffectManager;
                effectManager.CreateEffect(StatusEffects.DHSHeatPenaltyEffect, "DHSHeatPenalty", StatusEffects.RandomEffectID(), __instance, __instance, default, 0, false);
            }
        }
    }
}