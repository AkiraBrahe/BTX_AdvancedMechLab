using BattleTech.UI;
using BTX_AdvancedMechLab.Features.Customization;
using BTX_AdvancedMechLab.Features.Customization.Widgets;
using CustomUnits;
using System.Collections;
using UnityEngine;

namespace BTX_AdvancedMechLab.Patches
{
    /// <summary>
    /// Adds widgets to the mech info panel and adjusts the layout to accommodate them.
    /// </summary>
    [HarmonyPatch(typeof(MechLabMechInfoWidget), "RefreshInfo")]
    public static class MechLabMechInfoWidget_RefreshInfo
    {
        internal static bool hasInitialized = false;

        internal static Transform Container;
        internal static Transform LayoutHardpoints;
        internal static Transform StockButton;

        internal static ArmorWidget ArmorWidget;
        internal static CoolingWidget CoolingWidget;

        [HarmonyPostfix]
        [HarmonyWrapSafe]
        public static void Postfix(MechLabMechInfoWidget __instance)
        {
            var mechDef = __instance.mechLab.CreateMechDef();
            if (mechDef == null) return;

            if (ArmorWidget == null || CoolingWidget == null)
            {
                hasInitialized = false;
                var layoutTonnage = __instance.remainingTonnage.transform.parent;
                var objStatus = layoutTonnage.parent;

                Container = objStatus.Find("custom_capacities");
                LayoutHardpoints = objStatus.Find("layout_hardpoints");
                StockButton = LayoutHardpoints?.Find("OBJ_stockBttn");

                UISetup.GetOrSetupWidgets(__instance, objStatus, Container, layoutTonnage, LayoutHardpoints,
                    out ArmorWidget, out CoolingWidget);

                if (Container == null) Container = objStatus.Find("custom_capacities");
                Container?.gameObject.SetActive(false);

                __instance.StartCoroutine(DelayedInit(__instance));
                return;
            }

            if (!hasInitialized)
            {
                Container?.gameObject.SetActive(false);
                return;
            }

            if (StockButton == null) StockButton = LayoutHardpoints?.Find("OBJ_stockBttn");
            StockButton?.gameObject.SetActive(false);

            bool isVehicle = mechDef.IsVehicle();
            Container?.gameObject.SetActive(!isVehicle);

            if (!isVehicle)
            {
                var simGame = __instance.mechLab.sim;
                ArmorWidget.Refresh(mechDef, simGame);
                CoolingWidget.Refresh(mechDef, simGame);
            }
        }

        private static IEnumerator DelayedInit(MechLabMechInfoWidget mechInfoWidget)
        {
            yield return new WaitForEndOfFrame();

            hasInitialized = true;
            mechInfoWidget.RefreshInfo();
        }
    }
}