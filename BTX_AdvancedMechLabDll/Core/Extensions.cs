using BattleTech;
using BattleTech.UI;
using CustomComponents;
using HBS.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BTX_AdvancedMechLab.Core
{
    public static class Extensions
    {
        extension(TagSet tags)
        {
            #region Armor Type

            public ArmorType? GetArmorType()
            {
                string tag = tags.FirstOrDefault(t => t.StartsWith(ArmorPrefix));
                return tag != null && Enum.TryParse<ArmorType>(tag.Substring(ArmorPrefix.Length), out var type) ? type : null;
            }

            public void SetArmorType(ArmorType type)
            {
                tags.RemoveRange(tags.Where(t => t.StartsWith(ArmorPrefix)));
                tags.Add($"{ArmorPrefix}{type}");
            }

            #endregion

            #region Cooling Type

            public HeatSinkType? GetCoolingType()
            {
                string tag = tags.FirstOrDefault(t => t.StartsWith(CoolingPrefix));
                return tag != null && Enum.TryParse<HeatSinkType>(tag.Substring(CoolingPrefix.Length), out var type) ? type : null;
            }

            public void SetCoolingType(HeatSinkType type)
            {
                tags.RemoveRange(tags.Where(t => t.StartsWith(CoolingPrefix)));
                tags.Add($"{CoolingPrefix}{type}");
            }

            #endregion

            #region Patchwork Locations

            public ChassisLocations[] GetPatchworkLocations()
            {
                List<ChassisLocations> locations = [ChassisLocations.None];
                var patchworkTags = tags.Where(t => t.StartsWith(PatchworkPrefix));

                foreach (string tag in patchworkTags)
                {
                    if (Enum.TryParse<ChassisLocations>(tag.Substring(PatchworkPrefix.Length), out var location))
                    {
                        locations.Add(location);
                    }
                }
                return [.. locations];
            }

            public void AddPatchworkLocation(ChassisLocations location)
            {
                if (!tags.Contains($"{PatchworkPrefix}{location}")) tags.Add($"{PatchworkPrefix}{location}");
            }

            public void RemovePatchworkLocation(ChassisLocations location) => tags.Remove($"{PatchworkPrefix}{location}");

            #endregion
        }

        extension(MechDef mech)
        {
            #region Structure and Armor Info

            /// <summary>
            /// Retrieves the structure info of a mech.
            /// </summary>
            public StructureInfo GetStructureInfo()
            {
                var type = StructureType.Standard;

                bool isClan = mech.Chassis.ChassisTags.Contains("chassis_clan");
                foreach (string tag in mech.Chassis.ChassisTags)
                {
                    var match = StructureTypes.FirstOrDefault(st => !string.IsNullOrEmpty(st.Value.Tag) && st.Value.Tag == tag);
                    if (match.Value.Tag != null)
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
            public ArmorInfo GetArmorInfo(bool checkMechTags = true)
            {
                if (checkMechTags)
                {
                    var armorType = mech.MechTags.GetArmorType();
                    if (armorType != null) return ArmorTypes[(ArmorType)armorType];
                }

                var type = ArmorType.Standard;
                bool isClan = mech.Chassis.ChassisTags.Contains("chassis_clan");
                foreach (string tag in mech.Chassis.ChassisTags)
                {
                    var match = ArmorTypes.FirstOrDefault(at => !string.IsNullOrEmpty(at.Value.Tag) && at.Value.Tag == tag);
                    if (match.Value.Tag != null)
                    {
                        type = match.Key;
                        break;
                    }
                }

                if (isClan && type == ArmorType.FerroFibrous)
                    type = ArmorType.ClanFerroFibrous;

                return ArmorTypes[type];
            }

            #endregion

            #region Structure and Armor Calculations

            /// <summary>
            /// Calculates the total structure points of a mech.
            /// </summary>
            public int CalculateTotalStructure()
            {
                int structurePoints = 0;
                foreach (var location in mech.Chassis.Locations)
                {
                    structurePoints += (int)location.InternalStructure;
                }

                return structurePoints;
            }

            /// <summary>
            /// Calculates the current armor percentage of a mech.
            /// </summary>
            public int CalculateArmorPercentage(out int currentArmor, out int maxArmor)
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
            /// Calculates the total weight of a mech in tons.
            /// </summary>
            public float CalculateWeight() => CalculateWeightKG(null) / 1000f;

            /// <summary>
            /// Calculates the total weight of a mech in kilograms.
            /// </summary>
            public int CalculateWeightKG(MechLabPanel panel = null)
            {
                if (mech.Chassis == null) return 0;

                int weightKG = (int)(mech.Chassis.InitialTonnage * 1000.0f);
                weightKG += mech.CalculateArmorWeightKG(panel);

                var inventory = panel != null ? [.. panel.activeMechInventory] : mech.Inventory.ToList();
                foreach (var i in inventory)
                {
                    weightKG += (int)(i.Def.Tonnage * 1000.0f);
                }

                return weightKG / 10 == (int)(mech.Chassis.Tonnage * 100.0f)
                    ? (int)(mech.Chassis.Tonnage * 1000.0f) : weightKG;
            }

            /// <summary>
            /// Calculates the total armor weight of a mech in tons.
            /// </summary>
            public float CalculateArmorWeight() => mech.CalculateArmorWeightKG(null) / 1000f;

            /// <summary>
            /// Calculates the total armor weight of a mech in kilograms.
            /// </summary>
            public int CalculateArmorWeightKG(MechLabPanel panel = null)
            {
                var armor = mech.GetArmorInfo();

                var patchworkMask = ChassisLocations.None;
                var patchworkLocations = mech.MechTags.GetPatchworkLocations();
                if (patchworkLocations.Length > 0)
                {
                    for (int i = 0; i < patchworkLocations.Length; i++)
                    {
                        patchworkMask |= patchworkLocations[i];
                    }
                }

                int armorWeightKG = 0;
                foreach (var location in mech.Locations)
                {
                    armorWeightKG += CalcLocationArmorWeightKG(location.Location, armorDensity: armor.PptMultiplier, patchworkMask, location, panel?.GetWidgetForLocation(location.Location));
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

                return armorWeightKG;
            }

            #endregion

            #region Inventory and Equipment

            /// <summary>
            /// Gets the number of free inventory slots in a mech.
            /// </summary>
            public int GetFreeSlots(IEnumerable<MechComponentRef> inventory) =>
                mech.Chassis.Locations.Sum(location => GetFreeSlotsInLoc(mech, inventory, location.Location));

            /// <summary>
            /// Gets the number of free inventory slots in a mech, excluding a specific category.
            /// </summary>
            public int GetFreeSlots(IEnumerable<MechComponentRef> inventory, string excludedCategory) =>
                mech.Chassis.Locations.Sum(location => GetFreeSlotsInLoc(mech, inventory, location.Location, excludedCategory));

            /// <summary>
            /// Determines the number of free inventory slots in a location.
            /// </summary>
            public int GetFreeSlotsInLoc(IEnumerable<MechComponentRef> inventory, ChassisLocations location) =>
                mech.GetChassisLocationDef(location).InventorySlots - inventory.Where(i => i.Def != null && i.MountedLocation == location).Sum(i => i.Def.InventorySize);

            /// <summary>
            /// Determines the number of free inventory slots in a location, taking into account the size of the item being added.
            /// </summary>
            public int GetFreeSlotsInLoc(IEnumerable<MechComponentRef> inventory, ChassisLocations location, int size) =>
                (mech.GetChassisLocationDef(location).InventorySlots - inventory.Where(i => i.Def != null && i.MountedLocation == location).Sum(i => i.Def.InventorySize)) / size;

            /// <summary>
            /// Determines the number of free inventory slots in a location, excluding a specific category.
            /// </summary>
            public int GetFreeSlotsInLoc(IEnumerable<MechComponentRef> inventory, ChassisLocations location, string excludedCategory) =>
                mech.GetChassisLocationDef(location).InventorySlots - inventory.Where(i => i.Def != null && i.MountedLocation == location && !i.IsCategory(excludedCategory)).Sum(i => i.Def.InventorySize);

            #endregion
        }

        extension(MechLabPanel panel)
        {
            /// <summary>
            /// Retrieves the mech lab location widget for a specific chassis location.
            /// </summary>
            public MechLabLocationWidget GetWidgetForLocation(ChassisLocations location)
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
        }

        extension(LocationDef chassisLocationDef)
        {
            /// <summary>
            /// Evaluates whether a chassis location has rear armor.
            /// </summary>
            public bool HasRearArmor() =>
                chassisLocationDef.Location is ChassisLocations.CenterTorso or
                                               ChassisLocations.LeftTorso or
                                               ChassisLocations.RightTorso;
        }
    }
}