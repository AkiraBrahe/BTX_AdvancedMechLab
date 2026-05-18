using BattleTech;
using BattleTech.UI;
using BTX_AdvancedMechLab.Features.Armor;
using HBS;
using Localize;
using System;
using UnityEngine;

namespace BTX_AdvancedMechLab.Patches
{
    /// <summary>
    /// Displays how many armor scraps the player will receive when scrapping a mech.
    /// </summary>
    [HarmonyPatch(typeof(MechBayMechInfoWidget), "OnScrapClicked")]
    public static class MechBayMechInfoWidget_OnScrapClicked
    {
        [HarmonyPrefix]
        public static bool Prefix(ref bool __runOriginal, MechBayMechInfoWidget __instance)
        {
            if (!__runOriginal || __instance.selectedMech == null)
                return false;

            if (__instance.selectedMechElement.inMaintenance)
            {
                GenericPopupBuilder.Create("Cannot Scrap BattleMech", Strings.T("This 'Mech is already under maintenance. You must first cancel the existing task in order to scrap this 'Mech.")).AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true).Render();
                return false;
            }

            // string nameString = __instance.selectedMech.Description.Name;
            string cbillString = SimGameState.GetCBillString(Mathf.RoundToInt(__instance.selectedMech.Chassis.Description.Cost * __instance.sim.Constants.Finances.MechScrapModifier));

            var armor = __instance.selectedMech.GetArmorInfo();
            if (!string.IsNullOrEmpty(armor.ScrapItemDefID))
            {
                int armorKG = ScrapManager.GetScrapWeightKGFromMech(__instance.selectedMech, armor, out float totalArmorPoints);
                GenericPopupBuilder.Create(Strings.T($"Scrap 'Mech?"), Strings.T($"Are you sure you want to scrap this 'Mech?\n\nThis 'Mech's components and armor will be stored and its chassis removed permanently from your inventory." +
                    $"\n\nSCRAP VALUE: <color=#F79B26FF>{cbillString} + {armorKG / 1000f:F1}t of {armor.Name} armor</color>")).AddButton("Cancel", null, true, null).AddButton("Scrap", new Action(__instance.ConfirmScrapClicked), true, null)
                    .CancelOnEscape()
                    .AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true)
                    .Render();
            }
            else
            {
                GenericPopupBuilder.Create(Strings.T($"Scrap 'Mech?"), Strings.T($"Are you sure you want to scrap this 'Mech?\n\nThis 'Mech's components will be stored and its chassis removed permanently from your inventory." +
                    $"\n\nSCRAP VALUE: <color=#F79B26FF>{cbillString}</color>")).AddButton("Cancel", null, true, null).AddButton("Scrap", new Action(__instance.ConfirmScrapClicked), true, null)
                    .CancelOnEscape()
                    .AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true)
                    .Render();
            }

            return false;
        }
    }
}