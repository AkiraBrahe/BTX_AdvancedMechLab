using BattleTech;
using BattleTech.UI.TMProWrapper;
using BattleTech.UI.Tooltips;
using UnityEngine;
using static BTX_AdvancedMechLab.Features.EngineHeatSinks.HeatSinkManager;

namespace BTX_AdvancedMechLab.Features.Customization.Widgets
{
    public class CoolingWidget : MonoBehaviour
    {
        private LocalizableText _label;
        private HBSTooltip _tooltip;

        private void Awake()
        {
            _label = GetComponentInChildren<LocalizableText>();
            _tooltip = GetComponent<HBSTooltip>();
        }

        public void Refresh(MechDef mech)
        {
            var specs = GetEngineSpecs(mech.Chassis, mech.CoolingType);

            int internalCount = GetBaseHeatSinkCount(mech, specs) + GetInternalHeatSinkCount(mech);
            int externalCount = GetExternalHeatSinkCount(mech);

            int total = internalCount + externalCount;
            int maxPossible = Mathf.Max(10, specs.MaxInternal);
            bool isOverfilled = internalCount > specs.MaxInternal;

            _label.SetText($"{total}/{specs.MaxInternal} {specs.Abbreviation}");
            _label.color = isOverfilled ? Color.red : total >= maxPossible ? Color.cyan : total < 10 ? Color.red : Color.white;

            string hsTypeText = specs.HSType == EngineHSType.ClanDouble ? "Clan Double" : specs.HSType.ToString();
            string description = $"<b>Current Cooling: <color=#85DBF6>{internalCount} {hsTypeText} Heat Sinks</color></b>";
            description += $"\n<b>Engine: <color=#85DBF6>{specs.Rating}-rated {specs.Type} Engine</color></b>";
            description += "\n\nA 'Mech's engine requires a minimum of 10 heat sinks. ";
            if (specs.Rating < 250)
            {
                description += "Engines rated below 250 must install additional heat sinks elsewhere on the 'Mech to cool the engine.";
                if (total < 10)
                {
                    description += $"\n\n<color=#FF0000><b>CRITICAL:</b> {10 - total} more heat sink(s) are required.</color>";
                }
            }
            else if (specs.Rating == 250)
            {
                description += "Engines rated 250 have just enough space for the required heat sinks.";
            }
            else if (specs.Rating >= 275)
            {
                description += "Engines rated 275 and above have additional space for more internal heat sinks.";
            }

            // TODO: Add logic to check inventory for heat sinks
            // description += $"\n\nAvailable in Inventory:";
            // description += $"\n<color=#7FFF00>[<mspace=1em>✓</mspace>] 10x Double Heat Sinks</color>";
            // description += $"\n<color=#FF7F50>[<mspace=1em> </mspace>] 0x Clan Double Heat Sinks</color>";

            // string help = "\n\n<size=80%>---</size>" + "\n• <b>Click</b> to open the widget menu to manage engine heat sinks." +
            // $"\n<size=80%>• <b>Drag</b> a {hsTypeText} Heat Sink into any 'Mech location to add it to the engine.</size>";

            _tooltip.defaultStateData.SetObject(new BaseDescriptionDef(
                "EngineHeatSinkTooltip",
                "Engine Heat Sinks",
                description,
                "uixSvgIcon_equipment_Heatsink"
            ));
        }
    }
}