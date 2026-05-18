using BattleTech;
using BattleTech.UI.TMProWrapper;
using BattleTech.UI.Tooltips;
using BTX_AdvancedMechLab.Features.Armor;
using System;
using System.Linq;
using UnityEngine;

namespace BTX_AdvancedMechLab.Features.Customization.Widgets
{
    public class ArmorWidget : MonoBehaviour
    {
        private LocalizableText _label;
        private HBSTooltip _tooltip;

        private void Awake()
        {
            _label = GetComponentInChildren<LocalizableText>(true);
            _tooltip = GetComponent<HBSTooltip>();
        }

        public void Refresh(MechDef mech, SimGameState simGame = null)
        {
            var armor = mech.GetArmorInfo();

            _label.SetText($"{armor.Name}");

            int percent = mech.CalculateArmorPercentage(out int currentArmor, out int maxArmor);
            float weight = currentArmor / (80f * armor.PptMultiplier);

            string description = $"<b>Current Armor: <color=#85DBF6>{armor.Name} Armor</color></b>";
            description += $"\n<b>Amount: <color=#85DBF6>{currentArmor}/{maxArmor} ({percent}%)</color></b>";
            description += $"\t<b>Weight: <color=#85DBF6>{weight:F1}t</color></b>";
            description += "\n\nA 'Mech's armor is primordial to protect its internals in combat. ";
            description += armor.Description;

            bool hasPatchwork = !mech.MechTags.GetPatchworkLocations().Contains(ChassisLocations.None);
            if (hasPatchwork) description += "\n\n<color=#DE6729><b>PATCHWORK:</b> Locations highlighted in orange have standard plating and offer no benefits until repaired.</color>";

            if (simGame != null)
            {
                description += $"\n\nAvailable in Inventory:";
                var availableTypes = ArmorManager.GetAvailableArmorTypes(simGame);
                foreach (var armorType in availableTypes)
                {
                    int availableKG = ScrapManager.GetTotalScrapKG(simGame, armorType.Type);
                    int requiredKG = (int)Math.Ceiling(currentArmor / (80 * armorType.PptMultiplier) * 1000);

                    if (availableKG >= requiredKG)
                        description += $"\n<color=#7FFF00>[<mspace=1em>x</mspace>] {availableKG / 1000f:F1}t of {armorType.Name} Armor</color>";
                    else
                        description += $"\n<color=#FF7F50>[<mspace=1em> </mspace>] {availableKG / 1000f:F1}t of {armorType.Name} Armor</color>";
                }
            }

            string help = "\n\n• <b>Click</b> to access the widget menu and manage armor.";

            _tooltip.defaultStateData.SetObject(new BaseDescriptionDef(
                "ArmorTypeTooltip",
                "Armor Type",
                description + help,
                "uixSvgIcon_action_end"
            ));
        }
    }
}