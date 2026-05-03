using BattleTech;
using CustomComponents;
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
        /// Retrieves the structure info of a mech.
        /// </summary>
        public static StructureInfo GetStructureInfo(this MechDef mech)
        {
            var type = StructureType.Standard;
            bool isClan = mech.Chassis.ChassisTags.Contains("chassis_clan");

            foreach (string tag in mech.Chassis.ChassisTags)
            {
                var match = StructureTypes.FirstOrDefault(st => st.Value.Tag == tag);
                if (!string.IsNullOrEmpty(match.Value.Name))
                {
                    type = match.Key;
                    break;
                }
            }

            if (isClan && type == StructureType.EndoSteel)
                type = StructureType.ClanEndoSteel;

            return StructureTypes[type];
        }

        /// <summary>
        /// Retrieves the armor info of a mech.
        /// </summary>
        public static ArmorInfo GetArmorInfo(this MechDef mech)
        {
            var type = ArmorType.Standard;

            if (!string.IsNullOrEmpty(mech.ArmorType))
            {
                if (Enum.TryParse<ArmorType>(mech.ArmorType, out var result))
                    type = result;
            }
            else
            {
                bool isClan = mech.Chassis.ChassisTags.Contains("chassis_clan");
                foreach (string tag in mech.Chassis.ChassisTags)
                {
                    var match = ArmorTypes.FirstOrDefault(at => at.Value.Tag == tag);
                    if (!string.IsNullOrEmpty(match.Value.Name))
                    {
                        type = match.Key;
                        break;
                    }
                }

                if (isClan && type == ArmorType.FerroFibrous)
                    type = ArmorType.ClanFerroFibrous;
            }

            return ArmorTypes[type];
        }

        #endregion

        #region Mech Properties

        /// <summary>
        /// Retrieves the total structure points of a mech.
        /// </summary>
        public static int GetStructurePoints(this MechDef mech)
        {
            int structurePoints = 0;
            foreach (var location in mech.Chassis.Locations)
            {
                structurePoints += (int)location.InternalStructure;
            }

            return structurePoints;
        }

        /// <summary>
        /// Calculates the armor weight of a mech in tons.
        /// </summary>
        public static float CalculateArmorWeight(this MechDef mech) => mech.CalculateArmorWeightKG(null) / 1000f;

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


        /// <summary>
        /// Calculates the weight of a mech in KG.
        /// </summary>
        public static int CalculateWeightKG(this MechDef m, MechLabPanel panel = null)
        {
            if (m.Chassis == null) return 0;

            int weightKG = (int)(m.Chassis.InitialTonnage * 1000.0f);
            weightKG += m.CalculateArmorWeightKG(panel);

            var inventory = panel != null ? [.. panel.activeMechInventory] : m.Inventory.ToList();
            foreach (var i in inventory)
            {
                weightKG += (int)(i.Def.Tonnage * 1000.0f);
            }

            return weightKG / 10 == (int)(m.Chassis.Tonnage * 100.0f)
                ? (int)(m.Chassis.Tonnage * 1000.0f) : weightKG;
        }

        /// <summary>
        /// Calculates the armor weight of a mech in KG.
        /// </summary>
        public static int CalculateArmorWeightKG(this MechDef mech, MechLabPanel panel = null)
        {
            var armor = mech.GetArmorInfo();

            var patchworkMask = ChassisLocations.None;
            if (mech.PatchworkLocations != null)
            {
                for (int i = 0; i < mech.PatchworkLocations.Length; i++)
                {
                    patchworkMask |= mech.PatchworkLocations[i];
                }
            }

            int totalArmorWeightKG = 0;
            foreach (var location in mech.Locations)
            {
                totalArmorWeightKG += CalcLocationArmorWeightKG(location.Location, armorDensity: armor.PptMultiplier, patchworkMask, location, panel?.GetWidgetForLocation(location.Location));
            }

            static int CalcLocationArmorWeightKG(ChassisLocations location, float armorDensity, ChassisLocations patchworkMask, LocationLoadoutDef locDef, MechLabLocationWidget widget)
            {
                float assignedArmor = widget != null ? widget.currentArmor : locDef.AssignedArmor;
                float assignedRearArmor = widget != null ? widget.currentRearArmor : locDef.AssignedRearArmor;
                long points = (long)(assignedArmor * 1000f) + (long)(Mathf.Max(0, assignedRearArmor) * 1000f);

                bool isPatchwork = (patchworkMask & location) != 0; // Use bitwise AND (&) to check for overlap
                long armorPpt = isPatchwork ? 800L : (long)(800 * armorDensity);

                return (int)(points * 10L / armorPpt);
            }

            return totalArmorWeightKG;
        }

        #endregion

        #region UI Extensions

        public static MechLabLocationWidget GetWidgetForLocation(this MechLabPanel panel, ChassisLocations location)
        {
            return panel == null ? null : location switch
            {
                ChassisLocations.Head => panel.headWidget,
                ChassisLocations.CenterTorso => panel.centerTorsoWidget,
                ChassisLocations.LeftTorso => panel.leftTorsoWidget,
                ChassisLocations.RightTorso => panel.rightTorsoWidget,
                ChassisLocations.LeftArm => panel.leftArmWidget,
                ChassisLocations.RightArm => panel.rightArmWidget,
                ChassisLocations.LeftLeg => panel.leftLegWidget,
                ChassisLocations.RightLeg => panel.rightLegWidget,
                _ => null
            };
        }

        #endregion

        #region Location Properties

        /// <summary>
        /// Evaluates whether a chassis location has rear armor.
        /// </summary>
        public static bool HasRearArmor(this LocationDef chassisLocationDef) =>
            chassisLocationDef.Location is ChassisLocations.CenterTorso or
                                           ChassisLocations.LeftTorso or
                                           ChassisLocations.RightTorso;


        /// <summary>
        /// Determines the number of free inventory slots in a location.
        /// </summary>
        public static int GetFreeSlotsInLoc(this MechDef mech, IEnumerable<MechComponentRef> inventory, ChassisLocations location) =>
            mech.GetChassisLocationDef(location).InventorySlots - inventory.Where(i => i.MountedLocation == location).Sum(i => i.Def.InventorySize);

        /// <summary>
        /// Determines the number of free inventory slots in a location, taking into account the size of the item being added.
        /// </summary>
        public static int GetFreeSlotsInLoc(this MechDef mech, IEnumerable<MechComponentRef> inventory, ChassisLocations location, int size) =>
            (mech.GetChassisLocationDef(location).InventorySlots - inventory.Where(i => i.MountedLocation == location).Sum(i => i.Def.InventorySize)) / size;

        /// <summary>
        /// Determines the number of free inventory slots in a location, excluding a specific category.
        /// </summary
        public static int GetFreeSlotsInLoc(this MechDef mech, IEnumerable<MechComponentRef> inventory, ChassisLocations location, string excludedCategory) =>
            mech.GetChassisLocationDef(location).InventorySlots - inventory.Where(i => i.MountedLocation == location && !i.IsCategory(excludedCategory)).Sum(i => i.Def.InventorySize);

        #endregion
    }
}