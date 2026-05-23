using BattleTech;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using static BattleTech.SimGameState;

namespace BTX_AdvancedMechLab.Features.Armor
{
    /// <summary>
    /// Handles the accumulation and consumption of armor scrap items. 
    /// Armor scrap items are generated from mech parts that are salvaged from destroyed mechs.
    /// </summary>
    internal class ScrapManager
    {
        #region Ledger Persistence

        public static Dictionary<ArmorType, int> ArmorScrapLedger = [];
        private const string LedgerStatName = "AML_ArmorScrapLedger";

        /// <summary>
        /// Deserializes the armor scrap ledger.
        /// </summary>
        public static void DeserializeLedger(SimGameState simGame)
        {
            if (simGame.CompanyStats.ContainsStatistic(LedgerStatName))
            {
                string json = simGame.CompanyStats.GetValue<string>(LedgerStatName);
                try
                {
                    ArmorScrapLedger = JsonConvert.DeserializeObject<Dictionary<ArmorType, int>>(json) ?? [];
                    Main.Log.LogDebug($"Deserialized Armor Scrap Ledger: {json}");
                }
                catch (Exception ex)
                {
                    Main.Log.LogException(ex);
                    ArmorScrapLedger = [];
                }
            }
            else
            {
                ArmorScrapLedger = [];
                simGame.CompanyStats.AddStatistic<string>(LedgerStatName, "{}");
            }
        }

        /// <summary>
        /// Serializes the armor scrap ledger.
        /// </summary>
        public static void SerializeLedger(SimGameState simGame)
        {
            string json = JsonConvert.SerializeObject(ArmorScrapLedger);
            simGame.CompanyStats.Set(LedgerStatName, json);
            Main.Log.LogDebug($"Serialized Armor Scrap Ledger: {json}");
        }

        #endregion

        #region Ledger Operations

        /// <summary>
        /// Gets the combined total of armor scrap in kilograms, including both the granular ledger and whole-ton inventory items.
        /// </summary>
        public static int GetTotalScrapKG(SimGameState simGame, ArmorType type)
        {
            if (!ArmorScrapLedger.TryGetValue(type, out int ledgerKG))
                ledgerKG = 0;

            var armor = ArmorTypes[type];
            if (string.IsNullOrEmpty(armor.ScrapItemDefID))
                return ledgerKG;

            int itemTons = simGame.GetItemCount(armor.ScrapItemDefID, ItemCountType.ALL);
            return (itemTons * 1000) + ledgerKG;
        }

        /// <summary>
        /// Adds armor scrap kilograms to the ledger and automatically mints 1-ton inventory items for every 1000kg accumulated.
        /// </summary>
        public static void AddScrapKG(SimGameState simGame, ArmorType type, int kg)
        {
            if (!ArmorScrapLedger.ContainsKey(type))
                ArmorScrapLedger[type] = 0;

            ArmorScrapLedger[type] += kg;

            // Mint items if ledger exceeds 1000kg
            var armor = ArmorTypes[type];
            if (!string.IsNullOrEmpty(armor.ScrapItemDefID))
            {
                while (ArmorScrapLedger[type] >= 1000)
                {
                    ArmorScrapLedger[type] -= 1000;
                    simGame.AddItemStat(armor.ScrapItemDefID, typeof(UpgradeDef), false);
                }
            }
        }

        /// <summary>
        /// Consumes armor scrap kilograms from the ledger, automatically removing whole-ton inventory items if the ledger balance goes negative.
        /// </summary>
        /// <returns>False if the total scrap (ledger + inventory) is insufficient to cover the cost.</returns>
        public static bool ConsumeScrapKG(SimGameState simGame, ArmorType type, int kg)
        {
            int total = GetTotalScrapKG(simGame, type);
            if (total < kg) return false;

            if (!ArmorScrapLedger.ContainsKey(type))
                ArmorScrapLedger[type] = 0;

            ArmorScrapLedger[type] -= kg;

            // Consume items if ledger goes negative
            var armor = ArmorTypes[type];
            if (!string.IsNullOrEmpty(armor.ScrapItemDefID))
            {
                while (ArmorScrapLedger[type] < 0)
                {
                    ArmorScrapLedger[type] += 1000;
                    simGame.RemoveItemStat(armor.ScrapItemDefID, typeof(UpgradeDef), false);
                }
            }
            return true;
        }

        #endregion

        #region Salvage Generation

        public static DateTime CurrentDate = new(1999, 1, 1);

        /// <summary>
        /// Generates armor scrap items for the salvage pool based on the amount of armor tonnage recovered from mech parts.
        /// </summary>
        public static void GenerateScrapItemsForSalvage(List<SalvageDef> salvagePool, SimGameState simGame)
        {
            if (salvagePool == null || simGame == null) return;

            var mechPartsInSalvage = salvagePool.Where(s => s.Type == SalvageDef.SalvageType.MECH_PART).ToList();
            if (!mechPartsInSalvage.Any()) return;

            var scrapYields = new Dictionary<string, int>();

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
                            if (scrapYields.ContainsKey(armor.ScrapItemDefID))
                            {
                                scrapYields[armor.ScrapItemDefID] += scrapItems;
                            }
                            else
                            {
                                scrapYields[armor.ScrapItemDefID] = scrapItems;
                            }
                        }
                    }
                }
            }

            foreach (var yield in scrapYields)
            {
                AddScrapItemsToSalvagePool(yield.Key, salvagePool, simGame, yield.Value);
            }
        }

        /// <summary>
        /// Adds armor scrap items to the salvage pool.
        /// </summary>
        private static void AddScrapItemsToSalvagePool(string scrapID, List<SalvageDef> salvagePool, SimGameState simGame, int count)
        {
            if (!simGame.DataManager.UpgradeDefs.TryGet(scrapID, out var scrapDef))
                return;

            var salvage = new SalvageDef
            {
                Type = SalvageDef.SalvageType.COMPONENT,
                RewardID = scrapID,
                Description = new DescriptionDef(scrapDef.Description),
                MechComponentDef = scrapDef,
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
        /// Calculates the scrap weight in kilograms for a given amount of armor points.
        /// </summary>
        public static int GetScrapWeightKGFromPoints(ArmorInfo armor, float armorPoints) => (int)Math.Round(armorPoints / (80 * armor.PptMultiplier) * 1000);

        /// <summary>
        /// Calculates the total scrap weight in kilograms for the mech's current armor.
        /// </summary>
        public static int GetScrapWeightKGFromMech(MechDef mech, ArmorInfo armor, out float totalArmorPoints)
        {
            totalArmorPoints = 0;
            foreach (var location in mech.Locations)
            {
                totalArmorPoints += location.CurrentArmor;
                totalArmorPoints += Math.Max(0, location.CurrentRearArmor);
            }

            return GetScrapWeightKGFromPoints(armor, totalArmorPoints);
        }

        /// <summary>
        /// Converts mech armor into scraps, adding them to the ledger.
        /// </summary>
        public static void ConvertArmorToScraps(MechDef mech, SimGameState simGame)
        {
            var armor = mech.GetArmorInfo();
            if (!string.IsNullOrEmpty(armor.ScrapItemDefID))
            {
                int armorKG = GetScrapWeightKGFromMech(mech, armor, out float totalArmorPoints);
                Main.Log.Log($"Converting {totalArmorPoints} points of {armor.Name} armor to {armorKG}kg of scrap.");
                AddScrapKG(simGame, armor.Type, armorKG);
            }
        }

        #endregion

        #region Armor Scrap Consumption

        /// <summary>
        /// Tracks a repair intention for a mech, recording the amount of scrap required for the repair.
        /// </summary>
        public static void TrackRepairIntention(MechDef mech, ChassisLocations location, int armorDifference)
        {
            var armor = mech.GetArmorInfo();
            if (string.IsNullOrEmpty(armor.ScrapItemDefID)) return;

            int requiredScraps = GetScrapWeightKGFromPoints(armor, armorDifference);

            tempRepairIntentions.Add(new RepairIntention
            {
                MechID = mech.GUID,
                Location = location,
                ArmorType = armor.Type,
                RequiredScrapKG = requiredScraps
            });
        }

        /// <summary>
        /// Validates all tracked repair intentions and determines if scraps are sufficient or if patchwork is required.
        /// </summary>
        public static void ValidateRepairIntentions(SimGameState simGame)
        {
            var intentionsByArmorType = tempRepairIntentions
                .GroupBy(i => i.ArmorType)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var armorType in intentionsByArmorType.Keys)
            {
                var intentions = intentionsByArmorType[armorType];
                int totalRequiredKG = intentions.Sum(i => i.RequiredScrapKG);
                int availableKG = GetTotalScrapKG(simGame, armorType);

                if (totalRequiredKG <= availableKG)
                {
                    // Enough scraps for all locations of this armor type
                    foreach (var intention in intentions)
                    {
                        intention.UsesPatchwork = false;
                    }

                    bool scrapsDepleted = (availableKG - totalRequiredKG) < (totalRequiredKG / 2f);
                    if (TempRepairResult != ScrapConsumptionResult.Failed_InsufficientScrap)
                        TempRepairResult = scrapsDepleted ? ScrapConsumptionResult.Success_Depleted : ScrapConsumptionResult.Success;
                }
                else
                {
                    // Insufficient scraps: patch the remaining locations
                    int remainingKG = availableKG;
                    for (int i = 0; i < intentions.Count; i++)
                    {
                        if (remainingKG >= intentions[i].RequiredScrapKG)
                        {
                            intentions[i].UsesPatchwork = false;
                            remainingKG -= intentions[i].RequiredScrapKG;
                        }
                        else
                        {
                            intentions[i].UsesPatchwork = true;
                        }
                    }

                    TempRepairResult = ScrapConsumptionResult.Failed_InsufficientScrap;
                }
            }
        }

        /// <summary>
        /// Applies all tracked repair intentions, consuming scraps if available or applying patchwork if not.
        /// </summary>
        public static void ApplyRepairIntentions(SimGameState simGame)
        {
            foreach (var intention in tempRepairIntentions)
            {
                var mech = simGame.GetMechByID(intention.MechID);
                if (mech == null) continue;

                if (intention.UsesPatchwork)
                {
                    mech.MechTags.AddPatchworkLocation(intention.Location);
                    Main.Log.Log($"Applying patchwork to {intention.Location} of {mech.Name} (insufficient scraps).");
                }
                else
                {
                    bool consumed = ScrapManager.ConsumeScrapKG(simGame, intention.ArmorType, intention.RequiredScrapKG);
                    if (consumed)
                    {
                        Main.Log.LogDebug($"Consumed {intention.RequiredScrapKG} kg of {intention.ArmorType} scraps for {intention.Location} of {mech.Name}.");
                    }
                }
            }
        }

        #endregion
    }
}