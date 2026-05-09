using BattleTech;
using BattleTech.UI.TMProWrapper;
using BattleTech.UI.Tooltips;
using BTX_AdvancedMechLab.Features.Armor;
using UnityEngine;
using static BattleTech.SimGameState;

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

            int percent = mech.GetArmorPercentage(out int currentArmor, out int maxArmor);
            string description = $"<b>Current Armor: <color=#85DBF6>{armor.Name} Armor</color></b>";
            description += $"\n<b>Amount: <color=#85DBF6>{currentArmor}/{maxArmor} ({percent}%)</color></b>";
            description += "\n\nA 'Mech's armor is primordial to protect its internals in combat. ";
            description += armor.Description;

            if (simGame != null)
            {
                description += $"\n\nAvailable in Inventory:";
                var availableTypes = ArmorManager.GetAvailableArmorTypes(simGame);
                foreach (var armorType in availableTypes)
                {
                    int availableTonnage = simGame.GetItemCount(armorType.ScrapItemDefID, typeof(UpgradeDef), ItemCountType.ALL);
                    int requiredTonnage = (int)Mathf.Round(currentArmor / (80 * armorType.PptMultiplier));

                    if (availableTonnage >= requiredTonnage)
                        description += $"\n<color=#7FFF00>[<mspace=1em>✓</mspace>] {availableTonnage}t of {armorType.Name} Armor</color>";
                    else
                        description += $"\n<color=#FF7F50>[<mspace=1em> </mspace>] {availableTonnage}t of {armorType.Name} Armor</color>";
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