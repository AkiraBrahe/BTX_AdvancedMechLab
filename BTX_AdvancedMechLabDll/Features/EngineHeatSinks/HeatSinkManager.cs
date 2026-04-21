using BattleTech;
using CustomComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BTX_AdvancedMechLab.Features.EngineHeatSinks
{
    /// <summary>
    /// Handles heat sink management for engines.
    /// </summary>
    public static class HeatSinkManager
    {
        #region Engine Specs

        public enum EngineHSType
        {
            Single,
            Double,
            ClanDouble
        }

        public struct EngineSpecs
        {
            public string Type;
            public int Rating;
            public int MinInternal;
            public int MaxInternal;
            public int AdditionalSlots;
            public EngineHSType HSType;

            public readonly string InternalDefID => HSType switch
            {
                EngineHSType.Single => "Gear_HeatSink_Internal_Standard",
                EngineHSType.Double => "Gear_HeatSink_Internal_Double",
                EngineHSType.ClanDouble => "Gear_HeatSink_Internal_Double_Clan",
                _ => "Gear_HeatSink_Internal_Standard",
            };

            public readonly string ExternalDefID => HSType switch
            {
                EngineHSType.Single => "Gear_HeatSink_Generic_Standard",
                EngineHSType.Double => "Gear_HeatSink_Generic_Double",
                EngineHSType.ClanDouble => "Gear_HeatSink_Clan_Double",
                _ => "Gear_HeatSink_Generic_Standard",
            };

            public readonly string Abbreviation => HSType switch
            {
                EngineHSType.Single => "SHS",
                EngineHSType.Double => "DHS",
                EngineHSType.ClanDouble => "cDHS",
                _ => "SHS",
            };
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

        public static EngineSpecs GetEngineSpecs(ChassisDef chassis, string coolingType = null)
        {
            int.TryParse(System.Text.RegularExpressions.Regex.Match(chassis.movementCapDefID, @"\d+$").Value, out int walkMp);
            int rating = (int)chassis.Tonnage * walkMp;
            int maxInternal = (int)Mathf.Floor(rating / 25);
            int minInternal = Mathf.Min(10, maxInternal);
            int additionalSlots = Mathf.Max(0, maxInternal - 10);

            var hsType = EngineHSType.Single;
            if (!string.IsNullOrEmpty(coolingType))
            {
                Enum.TryParse<EngineHSType>(coolingType, out hsType);
            }
            else if (chassis.ChassisTags.Contains("chassis_DHS"))
            {
                hsType = chassis.ChassisTags.Contains("chassis_clan") ? EngineHSType.ClanDouble : EngineHSType.Double;
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

        public static int GetBaseHeatSinkCount(MechDef mech, EngineSpecs? specs = null) => specs.HasValue ? specs.Value.MinInternal : GetEngineSpecs(mech.Chassis).MinInternal;

        public static int GetInternalHeatSinkCount(MechDef mech) => mech.Inventory.Count(i => i.ComponentDefType == ComponentType.HeatSink && i.IsCategory("Internal"));

        public static int GetExternalHeatSinkCount(MechDef mech) => mech.Inventory.Count(i => i.ComponentDefType == ComponentType.HeatSink && !i.IsCategory("Internal"));
    }
}