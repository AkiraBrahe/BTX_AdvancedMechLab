using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

        #region Mech Properties

        /// <summary>
        /// Calculates the total internal structure of a mech.
        /// </summary>
        public static float GetTotalStructurePoints(this MechDef mech)
        {
            float totalStructure = 0;
            foreach (var location in Globals.repairPriorities.Values)
            {
                totalStructure += mech.GetChassisLocationDef(location).InternalStructure;
            }

            return totalStructure;
        }

        /// <summary>
        /// Calculates the total armor weight in tons of a mech.
        /// </summary>
        public static float GetTotalArmorWeight(this MechDef mech, ArmorInfo armor)
        {
            int totalArmor = 0;
            foreach (var location in mech.Locations)
            {
                totalArmor += (int)location.AssignedArmor;
                totalArmor += Mathf.Max((int)location.AssignedRearArmor, 0);
            }

            return totalArmor / (80 * armor.PptMultiplier);
        }

        /// <summary>
        /// Calculates the current armor percentage of a mech.
        /// </summary>
        public static int GetArmorPercentage(this MechDef mech, out int currentArmor, out int maxArmor)
        {
            var chassis = mech.Chassis;
            if (chassis == null)
            {
                currentArmor = 0;
                maxArmor = 0;
                return 0;
            }

            currentArmor = 0;
            foreach (var location in mech.Locations)
            {
                currentArmor += (int)location.AssignedArmor;
                currentArmor += Mathf.Max((int)location.AssignedRearArmor, 0);
            }

            maxArmor = 0;
            foreach (var location in chassis.Locations)
            {
                maxArmor += (int)location.MaxArmor; // Includes rear armor
            }

            return (int)Math.Round((double)currentArmor / maxArmor * 100);
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