
using BattleTech;
using BattleTech.UI;
using CustomComponents;
using CustomComponents.Changes;
using Localize;
using System.Collections.Generic;
using static BTX_AdvancedMechLab.Features.EngineHeatSinks.HeatSinkManager;

namespace BTX_AdvancedMechLab.Features.EngineHeatSinks
{
    internal class HeatSinkValidators
    {
        public static void Register()
        {
            Validator.RegisterMechValidator(ValidateMech, ValidateMechCanBeFielded);
            Validator.RegisterDropValidator(null, ReplaceValidateDrop, null);
        }

        #region Hook Methods

        public static void ValidateMech(Dictionary<MechValidationType, List<Text>> errors, MechValidationLevel _, MechDef mechDef)
        {
            string message = ValidateMinimumHeatSinks(mechDef);
            if (!string.IsNullOrEmpty(message))
            {
                errors[MechValidationType.InvalidInventorySlots].Add(new Text(message));
            }
        }

        public static bool ValidateMechCanBeFielded(MechDef mechDef) => string.IsNullOrEmpty(ValidateMinimumHeatSinks(mechDef));

        public static string ReplaceValidateDrop(MechLabItemSlotElement drop_item, ChassisLocations location, Queue<IChange> changes) => ValidateHeatSinkDropToEngine(drop_item, location, changes);

        #endregion

        #region Validation Logic

        /// <summary>
        /// Validates that the specified mech has a minimum of ten heat sinks.
        /// </summary>
        private static string ValidateMinimumHeatSinks(MechDef mech)
        {
            int baseCount = GetBaseHeatSinkCount(mech);
            int internalCount = GetInternalHeatSinkCount(mech);
            int externalCount = GetExternalHeatSinkCount(mech);

            int total = baseCount + internalCount + externalCount;

            return total < 10
                ? $"ENGINE HEAT SINKS: This 'Mech does not have the minimum amount of 10 heat sinks required by the engine."
                : string.Empty;
        }

        /// <summary>
        /// Validates whether the dropped heat sink can be moved into the engine.
        /// </summary>
        private static string ValidateHeatSinkDropToEngine(MechLabItemSlotElement drop_item, ChassisLocations location, Queue<IChange> changes)
        {
            if (drop_item.ComponentRef.Def.ComponentSubType != MechComponentType.Heatsink)
                return string.Empty;

            var mech = MechLabHelper.CurrentMechLab.ActiveMech;

            var hsType = mech.MechTags.GetCoolingType();
            var specs = GetEngineSpecs(mech.Chassis, hsType);
            if (drop_item.ComponentRef.ComponentDefID == specs.ExternalDefID)
            {
                int internalCount = GetInternalHeatSinkCount(mech);
                if (internalCount < specs.AdditionalSlots)
                {
                    changes.Enqueue(new Change_Remove(drop_item.ComponentRef, location));
                    changes.Enqueue(new Change_Add(specs.InternalDefID, ComponentType.HeatSink, ChassisLocations.CenterTorso));
                    return string.Empty;
                }
            }

            return string.Empty;
        }

        #endregion
    }
}