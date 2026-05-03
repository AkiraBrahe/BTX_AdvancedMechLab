using BattleTech;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using CustomUnits;
using Localize;
using System.Collections.Generic;

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