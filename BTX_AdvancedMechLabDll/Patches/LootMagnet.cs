using BattleTech;
using BTX_AdvancedMechLab.Features.Armor;
using LootMagnet;
using System;
using System.Collections.Generic;
using static LootMagnet.Helper;

namespace BTX_AdvancedMechLab.Patches
{
    /// <summary>
    /// Bypasses the blacklist for double heat sinks from 3052 onwards so they can be rolled up in the salvage pool.
    /// </summary>
    [HarmonyPatch(typeof(Helper), "IsBlacklisted")]
    public static class LootMagnet_Helper_IsBlacklisted
    {
        [HarmonyPrefix]
        public static bool Prefix(SalvageDef salvageDef) => ScrapManager.CurrentDate.Year < 3052 || salvageDef.Description.Id != "Gear_HeatSink_Generic_Double";
    }

    /// <summary>
    /// Prevents excessive armor scrap stacking when rolling up salvage.
    /// </summary>
    [HarmonyPatch(typeof(Helper), "RollupSalvageDef")]
    public static class Helper_RollupSalvageDef
    {
        [HarmonyPrepare]
        public static bool Prepare() => Main.Settings.ArmorSalvage.EnableArmorStackLimit;

        [HarmonyPrefix]
        public static bool Prefix(SalvageDef salvageDef, float threshold, List<SalvageDef> salvage)
        {
            if (salvageDef.RewardID == null || !salvageDef.RewardID.Contains("Lootable_Armor"))
                return true;

            Main.Log.LogDebug($"Rolling up armor scrap: {salvageDef.Description.Id} (Count: {salvageDef.Count})");
            float cappedThreshold = Math.Min(threshold, salvageDef.Description.Cost * Main.Settings.ArmorSalvage.MaxTonsPerStack);

            // Original logic with capped threshold
            int sDefCost = salvageDef.Description.Cost;
            int rollupCount = (int)Math.Ceiling(cappedThreshold / sDefCost);

            if (IsBlacklisted(salvageDef))
            {
                salvage.Add(salvageDef);
            }
            else if (rollupCount > 1)
            {
                int buckets = (int)Math.Floor(salvageDef.Count / (double)rollupCount);
                int remainder = salvageDef.Count % rollupCount;

                int i;
                for (i = 0; i < buckets; i++)
                {
                    var bucketDef = CloneToXName(salvageDef, rollupCount, i);
                    salvage.Add(bucketDef);
                }

                if (remainder != 0)
                {
                    var remainderDef = CloneToXName(salvageDef, remainder, i + 1);
                    salvage.Add(remainderDef);
                }
            }
            else
            {
                salvage.Add(salvageDef);
            }

            return false;
        }
    }
}