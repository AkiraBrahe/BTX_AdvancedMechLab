using BattleTech;
using Localize;
using System.Collections.Generic;

namespace BTX_AdvancedMechLab.Patches
{
    /// <summary>
    /// Flags up a warning in the mech lab when a mech has destroyed components.
    /// </summary>
    [HarmonyPatch(typeof(MechValidationRules), "ValidateMechStructure")]
    public static class MechValidationRules_ValidateMechStructure
    {
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        public static void Postfix(MechDef mechDef, MechValidationLevel validationLevel, WorkOrderEntry_MechLab baseWorkOrder, ref Dictionary<MechValidationType, List<Text>> errorMessages)
        {
            for (int i = 0; i < mechDef.Inventory.Length; i++)
            {
                var component = mechDef.Inventory[i];
                if (component.DamageLevel == ComponentDamageLevel.Destroyed && !MechValidationRules.MechComponentUnderMaintenance(component, validationLevel, baseWorkOrder))
                {
                    errorMessages[MechValidationType.StructureDamaged].Add(new Text("DESTROYED COMPONENT: 'Mech has destroyed components"));
                    break;
                }
            }
        }
    }
}