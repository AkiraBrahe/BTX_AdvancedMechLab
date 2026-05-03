using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BTX_AdvancedMechLab.Features.Armor
{
    /// <summary>
    /// Handles salvage generation of armor scrap items and conversion of mech armor to scrap items in the inventory.
    /// </summary>
    internal class ScrapManager
    {
        #region Salvage Generation

        public static DateTime CurrentDate = new(1999, 1, 1);

        /// <summary>
        /// Generates armor scrap items for the salvage pool based on the amount of armor tonnage recovered from the mech parts.
        /// </summary>
        public static void GenerateArmorScrapItems(List<SalvageDef> salvagePool, SimGameState simGame)
        {
            if (salvagePool == null || simGame == null) return;

            var mechPartsInSalvage = salvagePool.Where(s => s.Type == SalvageDef.SalvageType.MECH_PART).ToList();
            if (!mechPartsInSalvage.Any()) return;

            var mechPartsByMech = mechPartsInSalvage.GroupBy(m => m.Description.Id);
            foreach (var mechPartGroup in mechPartsByMech)
            {
                if (simGame.DataManager.MechDefs.TryGet(mechPartGroup.Key, out var mechDef))
                {
                    var armor = mechDef.GetArmorInfo();
                    if (!string.IsNullOrEmpty(armor.ScrapItemDefID))
                    {
                        float armorTonnage = mechDef.CalculateArmorWeight();
                        int partCount = mechPartGroup.Count();
                        float maxParts = simGame.Constants.Story.DefaultMechPartMax;

                        float recoveredArmorTonnage = armorTonnage * (partCount / maxParts);

                        // Random weight between 20% and 50% of the recovered tonnage.
                        float yieldPercent = (simGame.NetworkRandom.Float() * (0.5f - 0.2f)) + 0.2f;
                        int scrapItems = (int)Math.Round(recoveredArmorTonnage * yieldPercent);

                        if (scrapItems > 0)
                        {
                            AddScrapComponentToPool(armor.ScrapItemDefID, salvagePool, simGame, scrapItems);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds armor scrap items to the salvage pool.
        /// </summary>
        private static void AddScrapComponentToPool(string scrapID, List<SalvageDef> salvagePool, SimGameState simGame, int count)
        {
            if (!simGame.DataManager.UpgradeDefs.TryGet(scrapID, out var scrapDef))
                return;

            var salvage = new SalvageDef
            {
                Type = SalvageDef.SalvageType.COMPONENT,
                RewardID = scrapID,
                Description = new DescriptionDef(scrapDef.Description),
                ComponentType = ComponentType.Upgrade,
                Count = count,
                Damaged = false,
                Weight = simGame.Constants.Salvage.DefaultComponentWeight
            };

            Main.Log.LogDebug($"Added {count}x scrap item {scrapID} to salvage pool.");
            salvagePool.Add(salvage);
        }

        #endregion

        #region Armor Scrap Conversion

        /// <summary>
        /// Converts the armor of a mech into scrap items for the inventory.
        /// </summary>
        public static void ConvertArmorToScraps(MechDef def, SimGameState sim)
        {
            var armor = def.GetArmorInfo();
            if (!string.IsNullOrEmpty(armor.ScrapItemDefID))
            {
                int armorScrapValue = GetArmorScrapValue(def, armor.PptMultiplier);
                for (int i = 0; i < armorScrapValue; i++)
                {
                    sim.AddItemStat(armor.ScrapItemDefID, typeof(UpgradeDef), false);
                }
            }
        }

        /// <summary>
        /// Calculates the armor scrap value of a mech.
        /// </summary>
        public static int GetArmorScrapValue(MechDef mech, float density)
        {
            float value = 0;
            foreach (var armorLocation in mech.Locations)
            {
                value += armorLocation.CurrentArmor;
                value += Math.Max(0, armorLocation.CurrentRearArmor);
            }

            float pointsPerTon = 80 * density;
            return Mathf.RoundToInt(value / pointsPerTon);
        }

        #endregion
    }
}