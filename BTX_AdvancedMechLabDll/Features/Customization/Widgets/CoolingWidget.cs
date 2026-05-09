using BattleTech;
using BattleTech.UI.TMProWrapper;
using BattleTech.UI.Tooltips;
using System;
using UnityEngine;
using static BattleTech.SimGameState;
using static BTX_AdvancedMechLab.Features.EngineHeatSinks.HeatSinkManager;

namespace BTX_AdvancedMechLab.Features.Customization.Widgets
{
    public class CoolingWidget : MonoBehaviour
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
            Enum.TryParse<HeatSinkType>(mech.CoolingType, out var coolingType);
            var specs = GetEngineSpecs(mech.Chassis, coolingType);

            int baseCount = GetBaseHeatSinkCount(mech, specs);
            int internalCount = GetInternalHeatSinkCount(mech);
            int externalCount = GetExternalHeatSinkCount(mech);

            int total = baseCount + internalCount + externalCount;
            int max = specs.MaxInternal;
            bool isOverfilled = internalCount > specs.MaxInternal;

            _label.SetText($"{total}/{max} {specs.Abbreviation}");
            _label.color = isOverfilled ? Color.red : total >= max ? Color.cyan : total < 10 ? Color.red : Color.white;

            string description = $"<b>Current Cooling: <color=#85DBF6>{internalCount}x {HeatSinkTypes[coolingType].Name} Heat Sinks</color></b>";
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

            if (simGame != null)
            {
                description += $"\n\nAvailable in Inventory:";
                var availableHeatSinks = GetAvailableHeatSinks(simGame);
                foreach (var hsType in availableHeatSinks)
                {
                    int availableCount = simGame.GetItemCount(hsType.ExternalDefID, typeof(HeatSinkDef), ItemCountType.ALL);
                    int requiredCount = specs.MinInternal;

                    if (availableCount >= requiredCount)
                    {
                        description += $"\n<color=#7FFF00>[<mspace=1em>✓</mspace>] {availableCount}x {hsType.Name} Heat Sinks</color>";
                    }
                    else
                    {
                        description += $"\n<color=#FF7F50>[<mspace=1em> </mspace>] {availableCount}x {hsType.Name} Heat Sinks</color>";
                    }
                }
            }

            string help = "\n\n• <b>Click</b> to access the widget menu and manage heat sinks.";

            _tooltip.defaultStateData.SetObject(new BaseDescriptionDef(
                "EngineHeatSinkTooltip",
                "Engine Heat Sinks",
                description + help,
                "uixSvgIcon_equipment_Heatsink"
            ));
        }
    }
}