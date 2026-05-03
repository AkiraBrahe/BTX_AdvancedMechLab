using BattleTech;
using BattleTech.UI.TMProWrapper;
using BattleTech.UI.Tooltips;
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

        public void Refresh(MechDef mech)
        {
            var armor = mech.GetArmorInfo();

            _label.SetText($"{armor.Name}");

            int percent = mech.GetArmorPercentage(out int currentArmor, out int maxArmor);
            string description = $"<b>Current Armor: <color=#85DBF6>{armor.Name} Armor</color></b>";
            description += $"\n<b>Amount: <color=#85DBF6>{currentArmor}/{maxArmor} ({percent}%)</color></b>";
            description += "\n\nA 'Mech's armor is primordial to protect its internals in combat. ";
            description += armor.Description;

            // TODO: Add logic to check inventory for armor scraps
            // description += $"\n\nAvailable in Inventory:";
            // description += $"\n<color=#7FFF00>[<mspace=1em>✓</mspace>] 10t of Ferro-Fibrous Armor</color>";
            // description += $"\n<color=#FF7F50>[<mspace=1em> </mspace>] 0t of Stealth Armor</color>";

            // string help = "\n\n<size=80%>---</size>" + "\n• <b>Click</b> to open the widget menu to manage armor.";

            _tooltip.defaultStateData.SetObject(new BaseDescriptionDef(
                "ArmorTypeTooltip",
                "Armor Type",
                description,
                "uixSvgIcon_action_end"
            ));
        }
    }
}