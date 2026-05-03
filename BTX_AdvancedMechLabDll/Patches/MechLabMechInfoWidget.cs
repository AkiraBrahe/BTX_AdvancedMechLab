using BattleTech.UI;
using BTX_AdvancedMechLab.Features.Customization;
using CustomUnits;

namespace BTX_AdvancedMechLab.Patches
{
    /// <summary>
    /// Adds widgets to the mech info panel and adjusts the layout to accommodate them.
    /// </summary>
    [HarmonyPatch(typeof(MechLabMechInfoWidget), "RefreshInfo")]
    public static class MechLabMechInfoWidget_RefreshInfo
    {
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        public static void Postfix(MechLabMechInfoWidget __instance)
        {
            var mechDef = __instance.mechLab.CreateMechDef();
            if (mechDef == null) return;

            bool isVehicle = mechDef.IsVehicle();

            var layoutTonnage = __instance.remainingTonnage.transform.parent;
            var objStatus = layoutTonnage.parent;
            var container = objStatus.Find("custom_capacities");
            var layoutHardpoints = objStatus.Find("layout_hardpoints");

            UISetup.GetOrSetupWidgets(__instance, objStatus, container, layoutTonnage, layoutHardpoints,
                out var armorWidget, out var coolingWidget);

            container.gameObject.SetActive(!isVehicle);

            var button = layoutHardpoints.Find("OBJ_stockBttn");
            button?.gameObject.SetActive(false);

            var simGame = __instance.mechLab.sim;
            if (simGame != null && !isVehicle)
            {
                armorWidget.Refresh(mechDef, simGame);
                coolingWidget.Refresh(mechDef);
            }
        }
    }
}