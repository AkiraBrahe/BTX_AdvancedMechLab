using BattleTech;
using BTX_AdvancedMechLab.Features.EngineHeatSinks;
using CustomComponents;
using System;
using System.Linq;

namespace BTX_AdvancedMechLab.Patches
{
    /// <summary>
    /// Cleans up chassis data and stores it in the cache for the autofixer to use.
    /// </summary>
    [HarmonyPatch(typeof(ChassisDef), nameof(ChassisDef.FromJSON))]
    [HarmonyAfter("BEX.BattleTech.Extended_CE")]
    public static class ChassisDef_FromJSON
    {
        [HarmonyPostfix]
        public static void Postfix(ChassisDef __instance)
        {
            if (__instance == null) return;

            // Step A: Identify and migrate blockers
            var fixedInv = __instance.FixedEquipment?.ToList() ?? [];
            for (int i = 0; i < fixedInv.Count; i++)
            {
                if (fixedInv[i].ComponentDefID.StartsWith("Gear_EndoSteel") ||
                    fixedInv[i].ComponentDefID.StartsWith("Gear_FerroFibrous") ||
                    fixedInv[i].ComponentDefID.StartsWith("Gear_EndoFerroCombo"))
                {
                    fixedInv[i].ComponentDefID = fixedInv[i].ComponentDefID.Replace("Gear_", "Gear_Armor_");
                }
            }

            var stockBlockers = fixedInv
                .Where(c => c.ComponentDefID.StartsWith("Gear_Armor_"))
                .Select(c => new DefaultsInfoRecord { DefID = c.ComponentDefID, Location = c.MountedLocation, Type = c.ComponentDefType })
                .ToArray();

            if (stockBlockers.Length > 0)
            {
                fixedInv.RemoveAll(c => c.ComponentDefID.StartsWith("Gear_Armor_"));
                __instance.fixedEquipment = [.. fixedInv];
            }

            // Step B: Normalize heat dissipation and adjust tonnage
            var specs = HeatSinkManager.GetEngineSpecs(__instance, null);
            int heatSinkingPerHS = HeatSinkTypes[specs.HSType].Dissipation;

            int stockHSCount = (__instance.Heatsinks + 30) / heatSinkingPerHS;
            int baseHSCount = specs.MinInternal;
            int extraHSCount = Math.Max(0, stockHSCount - baseHSCount);

            __instance.Heatsinks = baseHSCount;
            __instance.InitialTonnage -= extraHSCount;

            // Step C: Store data in cache for the autofixer to use
            var cache = __instance.GetComponent<AdvancedChassisData>();
            if (cache == null)
            {
                cache = new AdvancedChassisData();
                __instance.AddComponent(cache);
            }

            cache.StockBlockers = stockBlockers;
            cache.BaseHSCount = baseHSCount;
            cache.ExtraHSCount = extraHSCount;
        }
    }
}