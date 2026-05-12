using BattleTech;
using CustomComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Extended_CE.BTComponents;

namespace BTX_AdvancedMechLab.Features.EngineHeatSinks
{
    /// <summary>
    /// Handles engine heat sink calculations, conversions, and processing of engine crits to damage internal heat sinks.
    /// </summary>
    public static class HeatSinkManager
    {
        #region Engine Specs

        public struct EngineSpecs
        {
            public string Type;
            public int Rating;
            public int MinInternal;
            public int MaxInternal;
            public int AdditionalSlots;
            public HeatSinkType HSType;

            public readonly string InternalDefID => HeatSinkTypes[HSType].InternalDefID;
            public readonly string ExternalDefID => HeatSinkTypes[HSType].ExternalDefID;
            public readonly string Abbreviation => HeatSinkTypes[HSType].Abbreviation;
        }

        public static Dictionary<string, string> EngineIDs = new()
            {
                { "Gear_Compact_Engine", "Compact Fusion" },
                { "Gear_Fission_Engine", "Fission" },
                { "Gear_FuelCell_Engine", "Fuel Cell" },
                { "Gear_ICE_Engine", "ICE" },
                { "Gear_Light_Engine", "Light Fusion" },
                { "Gear_XL_Engine_Clan", "Clan XL Fusion" },
                { "Gear_XL_Engine", "XL Fusion" },
                { "Gear_XXL_Engine", "XXL Fusion" },
            };

        public static EngineSpecs GetEngineSpecs(ChassisDef chassis, HeatSinkType? coolingType)
        {
            int.TryParse(System.Text.RegularExpressions.Regex.Match(chassis.movementCapDefID, @"\d+$").Value, out int walkMp);
            int rating = (int)chassis.Tonnage * walkMp;
            int maxInternal = (int)Mathf.Floor(rating / 25);
            int minInternal = Mathf.Min(10, maxInternal);
            int additionalSlots = Mathf.Max(0, maxInternal - 10);

            var hsType = coolingType == null ? HeatSinkType.Single : (HeatSinkType)coolingType;
            if (coolingType == null && chassis.ChassisTags.Contains("chassis_DHS"))
            {
                hsType = chassis.ChassisTags.Contains("chassis_clan") ? HeatSinkType.ClanDouble : HeatSinkType.Double;
            }

            var fixedInv = chassis.FixedEquipment?.ToList();
            if (fixedInv != null && fixedInv.Count > 0)
            {
                foreach (var item in fixedInv)
                {
                    if (EngineIDs.TryGetValue(item.ComponentDefID, out string type))
                    {
                        return new EngineSpecs
                        {
                            Type = type,
                            Rating = rating,
                            MinInternal = minInternal,
                            MaxInternal = maxInternal,
                            AdditionalSlots = additionalSlots,
                            HSType = hsType
                        };
                    }
                }
            }

            return new EngineSpecs()
            {
                Type = "Fusion",
                Rating = rating,
                MinInternal = minInternal,
                MaxInternal = maxInternal,
                AdditionalSlots = additionalSlots,
                HSType = hsType
            };
        }

        #endregion

        #region Heat Sink Counts and Conversions

        public static int GetBaseHeatSinkCount(MechDef mech) => mech.Chassis.Heatsinks;

        public static int GetInternalHeatSinkCount(MechDef mech) => mech.Inventory.Count(i => i.ComponentDefType == ComponentType.HeatSink && i.IsCategory("Internal"));

        public static int GetExternalHeatSinkCount(MechDef mech) => mech.Inventory.Count(i => i.ComponentDefType == ComponentType.HeatSink && !i.IsCategory("Internal"));

        public static bool IsInternalHeatSink(string id) => HeatSinkTypes.Values.Any(v => v.InternalDefID == id);

        public static string GetExternalID(string internalID) => HeatSinkTypes.Values.FirstOrDefault(v => v.InternalDefID == internalID).ExternalDefID;

        /// <summary>
        /// Returns a list of available heat sink types at current date.
        /// </summary>
        public static List<HeatSinkInfo> GetAvailableHeatSinks(SimGameState simGame)
        {
            var currentDate = simGame.CurrentDate;
            var heatSinks = new List<HeatSinkInfo>();

            foreach (var hsType in HeatSinkTypes.Values)
            {
                if (currentDate >= hsType.IntroDate)
                {
                    heatSinks.Add(hsType);
                }
            }
            return heatSinks;
        }

        /// <summary>
        /// Converts all internal heat sinks in the salvage pool to external heat sinks.
        /// </summary>
        public static void ConvertInternalHeatSinksToExternalInSalvage(List<SalvageDef> salvagePool, SimGameState simGame)
        {
            if (salvagePool == null || simGame == null) return;

            foreach (var salvage in salvagePool)
            {
                if (salvage.Type == SalvageDef.SalvageType.COMPONENT && IsInternalHeatSink(salvage.RewardID))
                {
                    string externalID = GetExternalID(salvage.RewardID);
                    if (externalID != null)
                    {
                        Main.Log.LogDebug($"Converting salvaged internal heat sink {salvage.RewardID} to {externalID}");
                        salvage.RewardID = externalID;

                        // Attempt to update description
                        // if (simGame.DataManager.HeatSinkDefs.TryGet(externalID, out var def))
                        // {
                        //     salvage.Description = new DescriptionDef(def.Description);
                    }
                }
            }
        }

        /// <summary>
        /// Converts all internal heat sinks in the mech to external heat sinks.
        /// </summary>
        public static void ConvertInternalHeatSinksToExternalInMech(MechDef mech)
        {
            if (mech == null) return;

            foreach (var component in mech.Inventory)
            {
                if (IsInternalHeatSink(component.ComponentDefID))
                {
                    string externalID = GetExternalID(component.ComponentDefID);
                    if (externalID != null)
                    {
                        Main.Log.LogDebug($"Converting internal heat sink {component.ComponentDefID} to {externalID} in mech {mech.Name} inventory for scraping/storage.");
                        component.ComponentDefID = externalID;
                    }
                }
            }
        }

        #endregion

        #region Engine Crit Processing

        /// <summary>
        /// Processes engine critical hits on a mech by damaging or destroying internal heat sinks after battle.
        /// </summary>
        public static void ProcessEngineCrits(MechDef mech)
        {
            int engineCrits = GetEngineCrits(mech.GUID);
            if (engineCrits <= 0) return;

            var internalHS = mech.Inventory.Where(i => IsInternalHeatSink(i.ComponentDefID) && i.DamageLevel != ComponentDamageLevel.Destroyed).ToList();
            if (internalHS.Count == 0) return;

            Main.Log.LogDebug($"Mech {mech.Description.UIName} suffered {engineCrits} engine crits. Processing internal heat sinks...");

            int destroyChance = engineCrits > 1 ? 50 : 0;
            if (engineCrits >= 3) destroyChance = 100;

            for (int i = 0; i < engineCrits && i < internalHS.Count; i++)
            {
                if (destroyChance > 0 && UnityEngine.Random.Range(0, 100) < destroyChance)
                {
                    internalHS[i].DamageLevel = ComponentDamageLevel.Destroyed;
                    Main.Log.LogDebug($"Destroyed internal heat sink {internalHS[i].ComponentDefID} due to engine crit.");
                }
                else
                {
                    Main.Log.LogDebug($"Damaged internal heat sink {internalHS[i].ComponentDefID} due to engine crit.");
                    internalHS[i].DamageLevel = ComponentDamageLevel.Penalized;
                }
            }
        }

        /// <summary>
        /// Gets the number of engine critical hits a mech sustained in battle.
        /// </summary>
        private static int GetEngineCrits(string mechGUID)
        {
            return string.IsNullOrEmpty(mechGUID)
                ? 0
                : MechTTRuleInfo.MechTTStatStore != null
                  && MechTTRuleInfo.MechTTStatStore.TryGetValue(mechGUID, out var info)
                    ? info.EngineCrits
                    : 0;
        }

        #endregion
    }
}