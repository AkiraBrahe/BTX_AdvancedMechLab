using BattleTech;
using System.Linq;
using UnityEngine;
using static BTX_AdvancedMechLab.Core.Helpers;
using static BTX_AdvancedMechLab.Features.Maintenance;

namespace BTX_AdvancedMechLab.Patches
{
    #region Combat End

    /// <summary>
    /// Creates temporary repair work orders for structure, components, and armor for each mech at the end of combat.
    /// </summary>
    [HarmonyPatch(typeof(SimGameState), "RestoreMechPostCombat")]
    public static class SimGameState_RestoreMechPostCombat
    {
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        public static void Prefix(ref bool __runOriginal, SimGameState __instance, MechDef mech)
        {
            if (!__runOriginal) return;

            WorkOrderEntry_MechLab newWorkOrder = null;
            ProcessStructureRepairs(__instance, mech, ref newWorkOrder);
            ProcessComponentRepairs(__instance, mech, ref newWorkOrder);
            ProcessArmorRepairs(__instance, mech, ref newWorkOrder);

            // If any repair sub-orders were created, submit the main work order.
            if (newWorkOrder?.SubEntryCount > 0)
            {
                SubmitTempWorkOrder(newWorkOrder);
            }

            // Original logic: Reset destroyed components to a functional state.
            foreach (var component in mech.Inventory)
            {
                if (component.DamageLevel == ComponentDamageLevel.NonFunctional)
                {
                    component.DamageLevel = ComponentDamageLevel.Functional;
                }
            }

            __runOriginal = false;
        }
    }

    #endregion

    #region Contract Resolution

    [HarmonyPatch(typeof(SimGameState), "ResolveCompleteContract")]
    public static class SimGameState_ResolveCompleteContract
    {
        /// <summary>
        /// Ensures the temporary queue is cleared before processing a new contract completion.
        /// </summary>
        [HarmonyPrefix]
        public static void Prefix(ref bool __runOriginal, SimGameState __instance)
        {
            if (__runOriginal == false || __instance == null) return;
            Globals.tempMechLabQueue.Clear();
        }

        /// <summary>
        /// Prompts the player to approve or deny the queued mech repairs after completing a contract.
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix(SimGameState __instance)
        {
            int skipMechCount = 0;
            if (Globals.tempMechLabQueue.Count <= 0) return;

            if (!Main.Settings.ArmorRepair.AutoRepairMechsWithDestroyedComponents)
            {
                skipMechCount = FilterMechsWithDestroyedComponents(__instance);
            }

            int mechRepairCount = Globals.tempMechLabQueue.Count;

            // No mechs to repair or report on.
            if (mechRepairCount <= 0 && skipMechCount <= 0)
            {
                Globals.tempMechLabQueue.Clear();
                return;
            }

            if (Main.Settings.ArmorRepair.EnableAutoRepairPrompt)
            {
                ShowRepairPrompt(__instance, mechRepairCount, skipMechCount);
            }
            else
            {
                ProcessRepairsAndClearQueue(__instance);
            }
        }
    }

    #endregion

    #region Work Order Processing

    /// <summary>
    /// Applies the repair cost modifiers for repairing structure in the mech lab.
    /// </summary>
    [HarmonyPatch(typeof(SimGameState), "CreateMechRepairWorkOrder")]
    public static class SimGameState_CreateMechRepairWorkOrder
    {
        [HarmonyPostfix]
        public static void Postfix(SimGameState __instance, string mechSimGameUID, ref WorkOrderEntry_RepairMechStructure __result)
        {
            var mech = __instance.ActiveMechs.Values.FirstOrDefault(md => md.GUID == mechSimGameUID);
            if (mech == null || __result == null)
                return;

            var structureInfo = mech.GetStructureInfo();
            float tpmod = structureInfo.TPCost;
            float cbmod = structureInfo.CBCost;

            __result.Cost = Mathf.CeilToInt(__result.Cost * tpmod);
            __result.CBillCost = Mathf.CeilToInt(__result.CBillCost * cbmod);
        }
    }

    /// <summary>
    /// Applies the repair cost modifiers for repairing armor in the mech lab.
    /// </summary>
    [HarmonyPatch(typeof(SimGameState), "CreateMechArmorModifyWorkOrder")]
    public static class SimGameState_CreateMechArmorModifyWorkOrder
    {
        [HarmonyPostfix]
        public static void Postfix(SimGameState __instance, string mechSimGameUID, int armorDiff, ref WorkOrderEntry_ModifyMechArmor __result)
        {
            if (armorDiff == 0 || __result == null)
                return;

            var mech = __instance.ActiveMechs.Values.FirstOrDefault(md => md.GUID == mechSimGameUID);
            if (mech == null)
                return;

            var armorInfo = mech.GetArmorInfo();
            float tpmod = armorInfo.TPCost;
            float cbmod = armorInfo.CBCost;

            __result.Cost = Mathf.CeilToInt(__result.Cost * tpmod);
            __result.CBillCost = Mathf.CeilToInt(__result.CBillCost * cbmod);
        }
    }

    #endregion

    #region Technical Fixes

    /// <summary>
    /// Prevents structure repair work orders from resetting armor.
    /// </summary>
    [HarmonyPatch(typeof(SimGameState), "ML_RepairMech")]
    public static class SimGameState_ML_RepairMech
    {
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        public static void Prefix(ref bool __runOriginal, SimGameState __instance, WorkOrderEntry_RepairMechStructure order)
        {
            if (__runOriginal == false) return;
            if (order.IsMechLabComplete) return;

            var mechByID = __instance.GetMechByID(order.MechLabParent.MechID);
            if (mechByID == null)
                return;

            var locationLoadoutDef = mechByID.GetLocationLoadoutDef(order.Location);
            locationLoadoutDef.CurrentInternalStructure = mechByID.GetChassisLocationDef(order.Location).InternalStructure;
            mechByID.RefreshBattleValue();
            order.SetMechLabComplete(true);
            __runOriginal = false;
        }
    }

    /// <summary>
    /// Suppresses the default mech repair notification if the auto-repair prompt is enabled.
    /// </summary>
    [HarmonyPatch(typeof(SimGameState), "ShowMechRepairsNeededNotif")]
    public static class SimGameState_ShowMechRepairsNeededNotif
    {
        [HarmonyPrefix]
        public static void Prefix(ref bool __runOriginal, SimGameState __instance)
        {
            if (__runOriginal == false) return;
            if (Main.Settings.ArmorRepair.EnableAutoRepairPrompt)
            {
                __instance.CompanyStats.Set("COMPANY_NotificationViewed_BattleMechRepairsNeeded", __instance.DaysPassed);
                __runOriginal = false;
            }
        }
    }

    #endregion
}