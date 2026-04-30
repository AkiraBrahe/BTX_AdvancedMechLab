using BattleTech;
using BTX_AdvancedMechLab.Features.Armor;
using System.Collections.Generic;

namespace BTX_AdvancedMechLab.Patches
{
    /// <summary>
    /// Adds armor scraps for every recovered mech with non-standard armor.
    /// </summary>
    [HarmonyPatch(typeof(Contract), "GenerateSalvage")]
    [HarmonyAfter("BTSimpleMechAssembly")]
    public static class Contract_GenerateSalvage
    {
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        public static void Postfix(Contract __instance, ref List<SalvageDef> ___finalPotentialSalvage)
        {
            var simGame = __instance.BattleTechGame.Simulation;
            if (simGame == null) return;

            ScrapManager.GenerateArmorScrapItems(___finalPotentialSalvage, simGame);
        }
    }
}