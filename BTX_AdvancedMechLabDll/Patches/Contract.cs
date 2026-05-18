using BattleTech;
using BTX_AdvancedMechLab.Features.Armor;
using BTX_AdvancedMechLab.Features.EngineHeatSinks;
using System.Collections.Generic;

namespace BTX_AdvancedMechLab.Patches
{
    /// <summary>
    /// Converts internal heat sinks to external in the salvage pool.
    /// Also adds armor scraps for every mech with non-standard armor.
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

            if (simGame.CurrentDate.Year != ScrapManager.CurrentDate.Year)
            {
                ScrapManager.CurrentDate = simGame.CurrentDate;
            }

            HeatSinkManager.ConvertInternalHeatSinksToExternalInSalvage(___finalPotentialSalvage, simGame);
            ScrapManager.GenerateScrapItemsForSalvage(___finalPotentialSalvage, simGame);
        }
    }
}