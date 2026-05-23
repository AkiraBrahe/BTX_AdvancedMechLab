using BattleTech;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using CustomUnits;
using HBS;
using HBS.Collections;
using Localize;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BTX_AdvancedMechLab.Patches.Overrides
{
    /// <summary>
    /// Overrides the tonnage calculation and validation to accommodate the new patchwork system,
    /// which allows for mechs with non-standard armor to use standard plating on certain locations.
    /// </summary>
    #region Tonnage Calculation

    [HarmonyPatch(typeof(MechStatisticsRules), nameof(MechStatisticsRules.CalculateTonnage))]
    public class MechStatisticsRules_CalculateTonnage
    {
        [HarmonyPrefix]
        public static bool Prefix(MechDef mechDef, ref float currentValue, ref float maxValue)
        {
            if (mechDef?.Chassis == null)
            {
                currentValue = 0f;
                maxValue = 0f;
                return false;
            }

            maxValue = mechDef.Chassis.Tonnage;
            currentValue = mechDef.IsVehicle() ? mechDef.Chassis.Tonnage : mechDef.CalculateWeightKG() / 1000.0f;
            return false;
        }
    }

    [HarmonyPatch(typeof(MechLabMechInfoWidget), "CalculateTonnage")]
    public class MechLabMechInfoWidget_CalculateTonnage
    {
        [HarmonyPrefix]
        public static bool Prefix(MechLabMechInfoWidget __instance, MechLabPanel ___mechLab, LocalizableText ___totalTonnage, UIColorRefTracker ___totalTonnageColor,
            LocalizableText ___remainingTonnage, UIColorRefTracker ___remainingTonnageColor)
        {
            if (___mechLab.activeMechDef != null)
            {
                if (___mechLab.activeMechDef.IsVehicle())
                {
                    ___totalTonnage.SetText("{0:0.##}", ___mechLab.activeMechDef.Chassis.Tonnage);
                    ___totalTonnageColor.SetUIColor(UIColor.White);
                    ___remainingTonnage.SetText("");
                    ___remainingTonnageColor.SetUIColor(UIColor.White);
                }
                else
                {
                    int kg = ___mechLab.activeMechDef.CalculateWeightKG(___mechLab);
                    int chassiskg = (int)(___mechLab.activeMechDef.Chassis.Tonnage * 1000.0f);
                    int remaining = chassiskg - kg;
                    __instance.currentTonnage = kg / 1000.0f;
                    ___totalTonnage.SetText("{0:0.##} / {1}", __instance.currentTonnage, ___mechLab.activeMechDef.Chassis.Tonnage);
                    ___totalTonnageColor.SetUIColor(remaining < 0 ? UIColor.Red : UIColor.WhiteHalf);
                    if (remaining < 0)
                    {
                        float t = -remaining / 1000.0f;
                        ___remainingTonnage.SetText("{0:0.##} ton{1} overweight", t, remaining == -1000 ? "" : "s");
                    }
                    else
                    {
                        ___remainingTonnage.SetText("{0:0.##} ton{1} remaining", remaining / 1000.0f, remaining == 1000 ? "" : "s");
                    }
                    ___remainingTonnageColor.SetUIColor(remaining < 0 ? UIColor.Red : (remaining <= 500 ? UIColor.Gold : UIColor.White));
                }
                return false;
            }
            return true;
        }
    }

    #endregion

    #region MechLabPanel Operations

    /// <summary>
    /// Custom patchwork-aware armor solver for the "Maximize Armor" button in the mech lab.
    /// </summary>
    [HarmonyPatch(typeof(MechLabPanel), "OnMaxArmor")]
    public class MechLabPanel_OnMaxArmor
    {
        [HarmonyPrefix]
        public static bool Prefix(MechLabPanel __instance)
        {
            if (!__instance.Initialized || __instance.dragItem != null) return false;

            if (__instance.headWidget.IsDestroyed || __instance.centerTorsoWidget.IsDestroyed ||
                __instance.leftTorsoWidget.IsDestroyed || __instance.rightTorsoWidget.IsDestroyed ||
                __instance.leftArmWidget.IsDestroyed || __instance.rightArmWidget.IsDestroyed ||
                __instance.leftLegWidget.IsDestroyed || __instance.rightLegWidget.IsDestroyed)
            {
                __instance.modifiedDialogShowing = true;
                GenericPopupBuilder.Create("'Mech Location Destroyed", "You cannot auto-assign armor while a 'Mech location is Destroyed.")
                    .AddButton("Okay")
                    .CancelOnEscape()
                    .AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill))
                    .SetAlwaysOnTop()
                    .SetOnClose(() => __instance.modifiedDialogShowing = false)
                    .Render();
                return false;
            }

            var mech = __instance.activeMechDef;
            var armor = mech.GetArmorInfo();

            // Get patchwork locations
            var patchworkLocations = mech.MechTags.GetPatchworkLocations();
            var patchworkMask = ChassisLocations.None;
            foreach (var loc in patchworkLocations) patchworkMask |= loc;

            StripAllArmor(__instance);
            __instance.mechInfoWidget.RefreshInfo();

            // Get available weight without any armor
            float maxTonnage = mech.Chassis.Tonnage;
            int baseWeightKG = mech.CalculateWeightKG(__instance, includeArmor: false);
            float availableWeightKG = (maxTonnage * 1000.0f) - baseWeightKG;

            if (availableWeightKG <= 0f)
            {
                __instance.FlagAsModified();
                __instance.ValidateLoadout(false);
                return false;
            }

            // Initialize or rebuild needed values if the mech has changed
            if (mech.GUID != CurrentMechGUID || !mech.MechTags.SequenceEqual(CurrentMechTags))
            {
                CurrentMechGUID = mech.GUID; CurrentMechTags = mech.MechTags;

                BuildArmorCaps(__instance);
                BuildAllocationTargets(__instance, patchworkMask, armor);
            }

            AllocateArmor(AllocationTargets, availableWeightKG);
            foreach (var target in AllocationTargets)
            {
                target.Widget.SetArmor(target.IsRear, target.CurrentArmor, false);
            }

            __instance.mechInfoWidget.RefreshInfo();
            __instance.FlagAsModified();
            __instance.ValidateLoadout(false);

            return false;
        }

        private static string CurrentMechGUID = string.Empty;
        private static TagSet CurrentMechTags = null;

        private static Dictionary<ChassisLocations, (float FrontCap, float RearCap)> LocationArmorCaps = [];
        private static List<ArmorAllocationTarget> AllocationTargets = [];

        /// <summary>
        /// Strips all armor to prepare for fresh allocation.
        /// </summary>
        private static void StripAllArmor(MechLabPanel instance)
        {
            instance.headWidget.StripArmor();
            instance.centerTorsoWidget.StripArmor();
            instance.leftTorsoWidget.StripArmor();
            instance.rightTorsoWidget.StripArmor();
            instance.leftArmWidget.StripArmor();
            instance.rightArmWidget.StripArmor();
            instance.leftLegWidget.StripArmor();
            instance.rightLegWidget.StripArmor();
        }

        /// <summary>
        /// Builds armor caps for each location using tabletop rules.
        /// </summary>
        private static void BuildArmorCaps(MechLabPanel instance)
        {
            LocationArmorCaps.Clear();

            var locDefs = new[]
            {
                (loc: ChassisLocations.Head, def: instance.headWidget.chassisLocationDef),
                (loc: ChassisLocations.CenterTorso, def: instance.centerTorsoWidget.chassisLocationDef),
                (loc: ChassisLocations.LeftTorso, def: instance.leftTorsoWidget.chassisLocationDef),
                (loc: ChassisLocations.RightTorso, def: instance.rightTorsoWidget.chassisLocationDef),
                (loc: ChassisLocations.LeftArm, def: instance.leftArmWidget.chassisLocationDef),
                (loc: ChassisLocations.RightArm, def: instance.rightArmWidget.chassisLocationDef),
                (loc: ChassisLocations.LeftLeg, def: instance.leftLegWidget.chassisLocationDef),
                (loc: ChassisLocations.RightLeg, def: instance.rightLegWidget.chassisLocationDef),
            };

            foreach (var (loc, def) in locDefs)
            {
                float structure = def.InternalStructure;
                float frontCap = structure * 2f;
                float rearCap = def.HasRearArmor() ? structure : 0f;

                LocationArmorCaps[loc] = (frontCap, rearCap);
            }
        }

        /// <summary>
        /// Builds armor allocation targets for each location, taking into account the type of armor and whether it is patchwork.
        /// </summary>
        private static void BuildAllocationTargets(MechLabPanel instance, ChassisLocations patchworkMask, ArmorInfo armor)
        {
            AllocationTargets.Clear();

            var stats = UnityGameInstance.BattleTechGame.MechStatisticsConstants;
            const float BasePpt = 80f;

            var locationData = new[]
            {
                (chassis: ChassisLocations.Head, widget: instance.headWidget, armorLoc: ArmorLocation.Head, isRear: false, ratio: stats.ArmorAllocationRatioHead),
                (chassis: ChassisLocations.CenterTorso, widget: instance.centerTorsoWidget, armorLoc: ArmorLocation.CenterTorso, isRear: false, ratio: stats.ArmorAllocationRatioCenterTorso),
                (chassis: ChassisLocations.CenterTorso, widget: instance.centerTorsoWidget, armorLoc: ArmorLocation.CenterTorsoRear, isRear: true, ratio: stats.ArmorAllocationRatioCenterTorsoRear),
                (chassis: ChassisLocations.LeftTorso, widget: instance.leftTorsoWidget, armorLoc: ArmorLocation.LeftTorso, isRear: false, ratio: stats.ArmorAllocationRatioLeftTorso),
                (chassis: ChassisLocations.LeftTorso, widget: instance.leftTorsoWidget, armorLoc: ArmorLocation.LeftTorsoRear, isRear: true, ratio: stats.ArmorAllocationRatioLeftTorsoRear),
                (chassis: ChassisLocations.RightTorso, widget: instance.rightTorsoWidget, armorLoc: ArmorLocation.RightTorso, isRear: false, ratio: stats.ArmorAllocationRatioRightTorso),
                (chassis: ChassisLocations.RightTorso, widget: instance.rightTorsoWidget, armorLoc: ArmorLocation.RightTorsoRear, isRear: true, ratio: stats.ArmorAllocationRatioRightTorsoRear),
                (chassis: ChassisLocations.LeftArm, widget: instance.leftArmWidget, armorLoc: ArmorLocation.LeftArm, isRear: false, ratio: stats.ArmorAllocationRatioLeftArm),
                (chassis: ChassisLocations.RightArm, widget: instance.rightArmWidget, armorLoc: ArmorLocation.RightArm, isRear: false, ratio: stats.ArmorAllocationRatioRightArm),
                (chassis: ChassisLocations.LeftLeg, widget: instance.leftLegWidget, armorLoc: ArmorLocation.LeftLeg, isRear: false, ratio: stats.ArmorAllocationRatioLeftLeg),
                (chassis: ChassisLocations.RightLeg, widget: instance.rightLegWidget, armorLoc: ArmorLocation.RightLeg, isRear: false, ratio: stats.ArmorAllocationRatioRightLeg),
            };

            foreach (var (chassisLoc, widget, armorLoc, isRear, ratio) in locationData)
            {
                bool isPatchwork = (patchworkMask & chassisLoc) != 0;
                var (frontCap, rearCap) = LocationArmorCaps[chassisLoc];
                float cap = isRear ? rearCap : frontCap;

                // Calculate kg per armor point
                float effectiveArmorPpt = isPatchwork ? BasePpt : (BasePpt * armor.PptMultiplier);
                float kgPerPoint = 1000.0f / effectiveArmorPpt;

                AllocationTargets.Add(new ArmorAllocationTarget
                {
                    ChassisDef = chassisLoc,
                    ArmorLocation = armorLoc,
                    Widget = widget,
                    IsRear = isRear,
                    MaxArmor = cap,
                    CurrentArmor = 0f,
                    AllocationRatio = ratio,
                    IsPatchwork = isPatchwork,
                    KGPerArmorPoint = kgPerPoint
                });
            }
        }

        /// <summary>
        /// Distributes armor to the various locations in 5-point steps while respecting armor caps and available weight.
        /// </summary>
        private static void AllocateArmor(List<ArmorAllocationTarget> targets, float availableWeightKG)
        {
            float usedWeightKG = 0f;
            const float StepSize = 5f;

            bool madeProgress = true;
            while (madeProgress && usedWeightKG < availableWeightKG)
            {
                madeProgress = false;

                // Prioritize under-allocated locations and weight-efficient armor types
                var candidates = targets
                    .Where(t => t.CurrentArmor < t.MaxArmor)
                    .OrderBy(t => t.CurrentArmor / Mathf.Max(0.0001f, t.AllocationRatio * 100f))
                    .ThenBy(t => t.KGPerArmorPoint)
                    .ToList();

                foreach (var target in candidates)
                {
                    float proposedArmor = Mathf.Min(target.CurrentArmor + StepSize, target.MaxArmor);
                    float additionalWeightKG = (proposedArmor - target.CurrentArmor) * target.KGPerArmorPoint;

                    if (usedWeightKG + additionalWeightKG <= availableWeightKG)
                    {
                        target.CurrentArmor = proposedArmor;
                        usedWeightKG += additionalWeightKG;
                        madeProgress = true;
                        break; // Re-evaluate priorities after each allocation
                    }
                }
            }
        }

        private class ArmorAllocationTarget
        {
            public ChassisLocations ChassisDef { get; set; }
            public ArmorLocation ArmorLocation { get; set; }
            public MechLabLocationWidget Widget { get; set; }
            public bool IsRear { get; set; }
            public float MaxArmor { get; set; }
            public float CurrentArmor { get; set; }
            public float AllocationRatio { get; set; }
            public bool IsPatchwork { get; set; }
            public float KGPerArmorPoint { get; set; }
        }
    }

    #endregion

    #region Tonnage Validation

    [HarmonyPatch(typeof(MechValidationRules), nameof(MechValidationRules.ValidateMechTonnage))]
    public class MechValidationRules_ValidateMechTonnage
    {
        [HarmonyPrefix]
        public static bool Prefix(MechDef mechDef, ref Dictionary<MechValidationType, List<Text>> errorMessages)
        {
            int max = (int)(mechDef.Chassis.Tonnage * 1000.0f);
            int actual = mechDef.CalculateWeightKG();

            if (actual > max)
            {
                errorMessages[MechValidationType.Overweight].Add(new Text("OVERWEIGHT: 'Mech weight exceeds maximum tonnage for the Chassis"));
            }
            else if (actual < max - 500)
            {
                errorMessages[MechValidationType.Underweight].Add(new Text("UNDERWEIGHT: 'Mech has unused tonnage"));
            }

            return false;
        }
    }

    #endregion
}