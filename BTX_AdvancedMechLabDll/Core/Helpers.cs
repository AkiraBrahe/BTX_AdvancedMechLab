using BattleTech;
using System;
using System.Linq;
using UnityEngine;

// TODO: Add missing summaries

namespace BTX_AdvancedMechLab.Core
{
    public static class Helpers
    {
        #region Data Extensions

        /// <summary>
        /// Retrieves the StructureInfo for the given mech.
        /// </summary>
        public static StructureData.StructureInfo GetStructureInfo(this MechDef mech)
        {
            var type = StructureData.StructureType.Standard;

            foreach (string tag in mech.Chassis.ChassisTags)
            {
                var match = StructureData.StructureTypes.FirstOrDefault(st => st.Value.Tag == tag);
                if (!string.IsNullOrEmpty(match.Value.Name))
                {
                    type = match.Key;
                    break;
                }
            }

            return StructureData.StructureTypes[type];
        }

        /// <summary>
        /// Retrieves the ArmorInfo for the given mech.
        /// </summary>
        public static ArmorData.ArmorInfo GetArmorInfo(this MechDef mech)
        {
            var type = ArmorData.ArmorType.Standard;

            if (!string.IsNullOrEmpty(mech.ArmorType))
            {
                if (Enum.TryParse<ArmorData.ArmorType>(mech.ArmorType, out var result))
                    type = result;
            }
            else
            {
                foreach (string tag in mech.Chassis.ChassisTags)
                {
                    var match = ArmorData.ArmorTypes.FirstOrDefault(at => at.Value.Tag == tag);
                    if (!string.IsNullOrEmpty(match.Value.Name))
                    {
                        type = match.Key;
                        break;
                    }
                }
            }

            return ArmorData.ArmorTypes[type];
        }

        #endregion

        #region Location Properties

        /// <summary>
        /// Evaluates whether a given chassis location has rear armor.
        /// </summary>
        public static bool HasRearArmor(this LocationDef chassisLocationDef) =>
            chassisLocationDef.Location is ChassisLocations.CenterTorso or
                                           ChassisLocations.LeftTorso or
                                           ChassisLocations.RightTorso;

        #endregion
    }
}