using BattleTech;
using BattleTech.UI;
using System.Linq;

namespace BTX_AdvancedMechLab.Patches
{
    /// <summary>
    /// Highlights patchwork locations in the mech lab.
    /// </summary>
    [HarmonyPatch(typeof(MechLabLocationWidget), "ShowHighlightFrame", [typeof(bool), typeof(UIColor)])]
    public static class MechLabLocationWidget_ShowHighlightFrame
    {
        [HarmonyPrefix]
        public static void Prefix(MechLabLocationWidget __instance, ref bool shouldShow, ref UIColor highlightColor)
        {
            if (!shouldShow)
            {
                var patchworkLocations = __instance.mechLab.activeMechDef.MechTags.GetPatchworkLocations();
                bool isPatched = patchworkLocations.Contains(__instance.chassisLocationDef.Location);

                var chassisTags = __instance.mechLab.activeMechDef.Chassis.ChassisTags;
                bool isCASEd = __instance.chassisLocationDef.Location switch
                {
                    ChassisLocations.LeftTorso => chassisTags.Contains("mech_case_left"),
                    ChassisLocations.RightTorso => chassisTags.Contains("mech_case_right"),
                    ChassisLocations.CenterTorso => chassisTags.Contains("mech_case_centre"),
                    _ => false
                };

                if (isPatched && isCASEd)
                {
                    highlightColor = UIColor.StructureUndamaged; // #D0A44E
                    shouldShow = true;
                }
                else if (isPatched)
                {
                    highlightColor = UIColor.StructureDamaged; // #DE6729
                    shouldShow = true;
                }
                else if (isCASEd)
                {
                    highlightColor = UIColor.Green; // #00FF00
                    shouldShow = true;
                }
            }
        }
    }
}