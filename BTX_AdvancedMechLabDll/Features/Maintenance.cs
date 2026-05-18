using BattleTech;
using BTX_AdvancedMechLab.Features.Armor;
using BTX_AdvancedMechLab.Features.EngineHeatSinks;
using Quirks;
using System;
using System.Linq;
using UnityEngine;
using static BattleTech.SimGameState;

namespace BTX_AdvancedMechLab.Features
{
    /// <summary>
    /// Handles the automatic repair of mechs.
    /// </summary>
    public static class Maintenance
    {
        #region Work Orders

        /// <summary>
        /// Processes the structure repair of a mech.
        /// </summary>
        public static void ProcessStructureRepairs(SimGameState simGame, MechDef mech, ref WorkOrderEntry_MechLab workOrder)
        {
            if (!Main.Settings.ArmorRepair.AutoRepairStructure || !mech.NeedsStructureRepair())
                return;

            foreach (var location in repairPriorities.Values)
            {
                var locationLoadout = mech.GetLocationLoadoutDef(location);
                float currentStructure = locationLoadout.CurrentInternalStructure;
                float definedStructure = mech.GetChassisLocationDef(location).InternalStructure;

                if (currentStructure < definedStructure)
                {
                    workOrder ??= CreateBaseMechLabOrder(simGame, mech);

                    int structureDifference = (int)Mathf.Abs(currentStructure - definedStructure);
                    var repairWorkOrder = simGame.CreateMechRepairWorkOrder(mech.GUID, location, structureDifference);
                    workOrder.AddSubEntry(repairWorkOrder);
                }
            }
        }

        /// <summary>
        /// Processes the armor repair of a mech.
        /// </summary>
        public static void ProcessArmorRepairs(SimGameState simGame, MechDef mech, ref WorkOrderEntry_MechLab workOrder)
        {
            if (!mech.NeedArmorRepair())
                return;

            foreach (var location in repairPriorities.Values)
            {
                var locationLoadout = mech.GetLocationLoadoutDef(location);
                var chassisLocationDef = mech.GetChassisLocationDef(location);

                int armorDifference = (int)Mathf.Abs(locationLoadout.CurrentArmor - locationLoadout.AssignedArmor);
                if (chassisLocationDef.HasRearArmor())
                {
                    armorDifference += (int)Mathf.Abs(locationLoadout.CurrentRearArmor - locationLoadout.AssignedRearArmor);
                }

                if (armorDifference > 0)
                {
                    // Use scraps if location was completely destroyed
                    if (Mathf.Approximately(armorDifference, locationLoadout.CurrentArmor))
                    {
                        ScrapManager.ConsumeScrapsForRepairs(simGame, mech, location, armorDifference);
                    }

                    workOrder ??= CreateBaseMechLabOrder(simGame, mech);

                    var armorWorkOrder = simGame.CreateMechArmorModifyWorkOrder(
                        mech.GUID,
                        location,
                        armorDifference,
                        (int)locationLoadout.AssignedArmor,
                        (int)locationLoadout.AssignedRearArmor
                    );

                    // Reset assigned armor to prevent free armor reset.
                    locationLoadout.AssignedArmor = Mathf.CeilToInt(locationLoadout.CurrentArmor);
                    locationLoadout.AssignedRearArmor = Mathf.CeilToInt(locationLoadout.CurrentRearArmor);

                    workOrder.AddSubEntry(armorWorkOrder);
                }
            }
        }

        /// <summary>
        /// Processes the component repair of a mech.
        /// </summary>
        public static void ProcessComponentRepairs(SimGameState simGame, MechDef mech, ref WorkOrderEntry_MechLab workOrder)
        {
            if (!mech.HasDamagedComponents() && !mech.HasDestroyedComponents())
                return;

            try
            {
                foreach (var component in mech.Inventory)
                {
                    if (component.DamageLevel == ComponentDamageLevel.Destroyed && HeatSinkManager.IsInternalHeatSink(component.ComponentDefID))
                    {
                        string externalID = HeatSinkManager.GetExternalID(component.ComponentDefID);
                        if (!string.IsNullOrEmpty(externalID) && simGame.GetItemCount(externalID, ItemCountType.UNDAMAGED_ONLY) > 0)
                        {
                            Main.Log.Log($"Auto-replacing destroyed internal heat sink. Consuming 1x {externalID} from inventory.");
                            simGame.RemoveItemStat(externalID, typeof(HeatSinkDef), false);
                            component.DamageLevel = ComponentDamageLevel.Penalized;
                        }
                    }

                    if (component.DamageLevel == ComponentDamageLevel.Penalized)
                    {
                        workOrder ??= CreateBaseMechLabOrder(simGame, mech);

                        var repairWorkOrder = simGame.CreateComponentRepairWorkOrder(component, true);
                        workOrder.AddSubEntry(repairWorkOrder);
                    }
                }
            }
            catch (Exception ex)
            {
                Main.Log.LogException(ex);
            }
        }

        /// <summary>
        /// Submits a mech lab work order to the temporary queue, which will be processed later by the player.
        /// </summary>
        public static void SubmitTempWorkOrder(WorkOrderEntry_MechLab workOrder)
        {
            try
            {
                tempMechLabQueue.Add(workOrder);
            }
            catch (Exception ex)
            {
                Main.Log.LogException(ex);
            }
        }

        /// <summary>
        /// Submits a mech lab work order to the game's mech lab queue to actually be processed.
        /// </summary>
        public static void SubmitWorkOrder(SimGameState simGame, WorkOrderEntry_MechLab workOrder)
        {
            try
            {
                simGame.MechLabQueue.Insert(0, workOrder);
                simGame.InitializeMechLabEntry(workOrder, workOrder.GetCBillCost());
                simGame.UpdateMechLabWorkQueue(false);
                simGame.AddFunds(-workOrder.GetCBillCost(), "ArmorRepair", true);
            }
            catch (Exception ex)
            {
                Main.Log.LogException(ex);
            }
        }

        /// <summary>
        /// Creates a base mech lab work order for a given mech.
        /// </summary>
        public static WorkOrderEntry_MechLab CreateBaseMechLabOrder(SimGameState simGame, MechDef mech)
        {
            try
            {
                string mechGUID = mech.GUID;
                string mechName = mech.Description?.Name != null ? mech.Description.Name : "Unknown";

                return new WorkOrderEntry_MechLab(WorkOrderType.MechLabGeneric,
                    "MechLab-BaseWorkOrder",
                    $"Modify 'Mech - {mechName}",
                    mechGUID,
                    0,
                    string.Format(simGame.Constants.Story.GeneralMechWorkOrderCompletedText, mechName));
            }
            catch (Exception ex)
            {
                Main.Log.LogException(ex);
                return null;
            }
        }

        #endregion

        #region Repair Prompt

        /// <summary>
        /// Filters out mechs with destroyed components from the temporary mech lab queue.
        /// </summary>
        public static int FilterMechsWithDestroyedComponents(SimGameState simGame)
        {
            int originalCount = tempMechLabQueue.Count;
            tempMechLabQueue.RemoveAll(order =>
            {
                var mech = simGame.GetMechByID(order.MechID);
                return mech.HasDestroyedComponents();
            });
            return originalCount - tempMechLabQueue.Count;
        }

        /// <summary>
        /// Shows the repair prompt to the player.
        /// </summary>
        public static void ShowRepairPrompt(SimGameState simGame, int mechRepairCount, int skipMechCount)
        {
            var notificationQueue = simGame.GetInterruptQueue();
            string skipMechCountDisplayed = GetMechCountDescription(skipMechCount, isForSkipped: true);

            // If all mechs were skipped, show a simple notification.
            if (mechRepairCount <= 0)
            {
                string message = $"Boss, {skipMechCountDisplayed} destroyed components. I'll leave the repairs for you to review.\n\n";
                notificationQueue.QueuePauseNotification(
                    "'Mech Repairs Needed",
                    message,
                    simGame.GetCrewPortrait(SimGameCrew.Crew_Yang),
                    string.Empty,
                    tempMechLabQueue.Clear,
                    "OK"
                );
                return;
            }

            // Calculate total repair costs
            int cbills = tempMechLabQueue.Sum(o => o.GetCBillCost());
            int techCost = tempMechLabQueue.Sum(o => o.GetCost());

            // Calculate tech cost in days
            int techDays = 1;
            if (techCost > 0 && simGame.MechTechSkill > 0)
            {
                techDays = Mathf.CeilToInt((float)techCost / simGame.MechTechSkill);
            }

            string mechRepairCountDisplayed = GetMechCountDescription(mechRepairCount, isForSkipped: false);
            string finalMessage = BuildFinalPromptMessage(mechRepairCountDisplayed, cbills, techDays, skipMechCount, skipMechCountDisplayed);

            notificationQueue.QueuePauseNotification(
                "'Mech Repairs Needed",
                finalMessage,
                simGame.GetCrewPortrait(SimGameCrew.Crew_Yang),
                string.Empty,
                () => ProcessRepairsAndClearQueue(simGame),
                "Yes",
                tempMechLabQueue.Clear,
                "No"
            );
        }

        /// <summary>
        /// Gets a description of the number of mechs.
        /// </summary>
        internal static string GetMechCountDescription(int count, bool isForSkipped)
        {
            return count <= 0
                ? string.Empty
                : isForSkipped
                ? count switch
                {
                    1 => "one of the 'Mechs is damaged but has",
                    2 => "two of the 'Mechs are damaged but have",
                    3 => "three of the 'Mechs are damaged but have",
                    4 => "a whole lance is damaged but has",
                    8 => "two lances are damaged but have",
                    12 => "all of our 'Mechs are damaged but have",
                    _ => $"{count} of the 'Mechs are damaged but have",
                }
                : count switch
                {
                    1 => "one of our 'Mechs was",
                    2 => "a couple of our 'Mechs were",
                    3 => "three of our 'Mechs were",
                    4 => "a whole lance was",
                    8 => "two lances were",
                    12 => "all of our 'Mechs were",
                    _ => $"{count} of our 'Mechs were",
                };
        }

        /// <summary>
        /// Builds the final prompt message for the repair prompt.
        /// </summary>
        internal static string BuildFinalPromptMessage(string mechRepairCountDisplayed, int cbills, int techDays, int skipMechCount, string skipMechCountDisplayed)
        {
            string costString = $"It'll cost <color=#DE6729>{'¢'}{cbills:n0}</color> and {techDays} days for";
            string question = "Want my crew to get started?";

            if (skipMechCount > 0)
            {
                string skipMessagePart = $"{skipMechCountDisplayed} destroyed components, so I'll leave those repairs to you.";
                return $"Boss, {mechRepairCountDisplayed} damaged. {costString} these repairs. {question}\n\nAlso, {skipMessagePart}\n\n";
            }
            else
            {
                return $"Boss, {mechRepairCountDisplayed} damaged on the last engagement. {costString} the repairs. {question}";
            }
        }

        /// <summary>
        /// Processes the repairs and clears the temporary mech lab queue.
        /// </summary>
        public static void ProcessRepairsAndClearQueue(SimGameState simGame)
        {
            foreach (var workOrder in tempMechLabQueue)
            {
                SubmitWorkOrder(simGame, workOrder);
            }
            tempMechLabQueue.Clear();
        }

        #endregion

        #region Cost Calculations

        private static string _lastMechGUID;
        private static float _totalStructure;
        private static float _techModifier;
        private static float _cbillModifier;
        private static StructureInfo _structure;
        private static ArmorInfo _armor;

        /// <summary>
        /// Calculates the structure repair cost for a given mech using tabletop rules.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Standard structure weight is 10% of the mech tonnage. Base cost per ton is 4,000 C-Bills for standard structure
        /// <br>e.g. 100 ton mech with Standard (Weight=1 and CBCost=1) = 40,000 C-Bills, Endo (Weight=0.5 and CBCost=8) = 160,000 C-Bills.</br></item>
        /// <item>Endo steel costs 96,000 C-Bills per ton (3x markup) before the technology is reintroduced in 3040.</item>
        /// </list>
        /// </remarks>
        public static void CalculateStructureRepairCost(SimGameState simGame, MechDef mech, WorkOrderEntry_RepairMechStructure workOrder)
        {
            if (mech.GUID != _lastMechGUID)
            {
                _lastMechGUID = mech.GUID;
                _totalStructure = mech.CalculateTotalStructure();
                _structure = mech.GetStructureInfo();
                _armor = mech.GetArmorInfo();
                GetQuirkModifiers(mech, out _techModifier, out _cbillModifier);
            }

            if (_totalStructure == 0) return;

            float techModifier = _techModifier;
            float cbillModifier = _cbillModifier;

            // Calculate total structure cost
            float tonnage = mech.Chassis.Tonnage;
            float structureWeight = tonnage * 0.10f * _structure.WeightMultiplier;
            float costPerTon = 4000f * _structure.CBCost;
            float structureCost = structureWeight * costPerTon;

            // Calculate cost per point of structure
            float costPerPoint = structureCost / _totalStructure;

            // If the location was destroyed, apply zero structure cost modifiers
            float maxLocStructure = mech.GetChassisLocationDef(workOrder.Location).InternalStructure;
            if (Mathf.Approximately(workOrder.StructureAmount, maxLocStructure))
            {
                techModifier *= simGame.Constants.MechLab.ZeroStructureTechPointModifier;
                cbillModifier *= simGame.Constants.MechLab.ZeroStructureCBillModifier;
            }

            // If the mech has prototype endo steel, apply prototype modifiers
            var currentDate = simGame.CurrentDate;
            if (_structure.Type == StructureType.EndoSteel && currentDate < new DateTime(3040, 1, 1))
            {
                cbillModifier *= Main.Settings.ArmorRepair.PrototypeEndoFerroRepairCostMultiplier;
            }

            // Scale repair time by tonnage if enabled
            if (Main.Settings.ArmorRepair.ScaleStructureRepairTimeByTonnage)
            {
                float tonnageFactor = Mathf.InverseLerp(20f, 100f, tonnage);
                techModifier *= Mathf.Lerp(1f, 4f, tonnageFactor);
            }

            workOrder.Cost = Mathf.CeilToInt(workOrder.Cost * _structure.TPCost * techModifier);
            workOrder.CBillCost = Mathf.CeilToInt(workOrder.StructureAmount * costPerPoint * cbillModifier);
        }

        /// <summary>
        /// Calculates the armor repair cost for a given mech using tabletop rules.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Standard armor cost is 10,000 C-Bills per ton (125 C-Bills per point * 80 points).</item>
        /// <item>Armor types with higher PptMultiplier (e.g. Ferro 1.12x) pack more points per ton.</item>
        /// <item>Ferro-fibrous armor costs 60,000 C-Bills per ton (3x markup) before the technology is reintroduced in 3040.</item>
        /// </list>
        /// </remarks>
        public static void CalculateArmorRepairCost(SimGameState simGame, MechDef mech, WorkOrderEntry_ModifyMechArmor workOrder)
        {
            if (mech.GUID != _lastMechGUID)
            {
                _lastMechGUID = mech.GUID;
                _totalStructure = mech.CalculateTotalStructure();
                _structure = mech.GetStructureInfo();
                _armor = mech.GetArmorInfo();
                GetQuirkModifiers(mech, out _techModifier, out _cbillModifier);
            }

            float techModifier = _techModifier;
            float cbillModifier = _cbillModifier;

            // Calculate cost per ton of armor
            float armorCostPerTon = _armor.CBCost / _armor.PptMultiplier;

            // If the mech has prototype ferro-fibrous armor, apply prototype modifiers
            var currentDate = simGame.CurrentDate;
            if (_armor.Type == ArmorType.FerroFibrous && currentDate < new DateTime(3040, 1, 1))
            {
                cbillModifier *= Main.Settings.ArmorRepair.PrototypeEndoFerroRepairCostMultiplier;
            }

            workOrder.Cost = Mathf.CeilToInt(workOrder.Cost * _armor.TPCost * techModifier);
            workOrder.CBillCost = Mathf.CeilToInt(workOrder.CBillCost * armorCostPerTon * cbillModifier);
        }

        /// <summary>
        /// Gets the quirk modifiers for a given mech based on its chassis tags.
        /// </summary>
        /// <remarks>
        /// Clan mechs get a flat 50% increase in repair costs instead of the standard 25% to make Clan-tech more expensive.
        /// </remarks>
        public static void GetQuirkModifiers(MechDef mech, out float techModifier, out float cbillModifier)
        {
            try
            {
                techModifier = 1f;
                cbillModifier = 1f;

                if (mech?.Chassis?.ChassisTags == null) return;

                var settings = MechQuirks.modSettings;
                if (settings == null) return;

                var tags = mech.Chassis.ChassisTags;

                if (tags.Contains("chassis_clan"))
                {
                    techModifier *= Main.Settings.ArmorRepair.ClanTechRepairCostMultiplier;
                    cbillModifier *= Main.Settings.ArmorRepair.ClanTechRepairCostMultiplier;
                }

                if (tags.Contains("mech_quirk_rugged1"))
                {
                    techModifier *= (settings.RuggedTechModifier + 100f) / 100f;
                    cbillModifier *= (settings.RuggedCostModifier + 100f) / 100f;
                }

                if (tags.Contains("mech_quirk_rugged2"))
                {
                    techModifier *= ((settings.RuggedTechModifier * 2f) + 100f) / 100f;
                    cbillModifier *= ((settings.RuggedCostModifier * 2f) + 100f) / 100f;
                }

                if (tags.Contains("mech_quirk_easytomaintain"))
                {
                    techModifier *= (settings.EasyToMaintTechModifier + 100f) / 100f;
                    cbillModifier *= (settings.EasyToMaintCostModifier + 100f) / 100f;
                }

                if (tags.Contains("mech_quirk_difficulttomaintain"))
                {
                    techModifier *= (settings.DifficultToMaintTechModifier + 100f) / 100f;
                    cbillModifier *= (settings.DifficultToMaintCostModifier + 100f) / 100f;
                }

                if (tags.Contains("mech_quirk_nonstandardparts"))
                {
                    techModifier *= (settings.NonStandardTechModifier + 100f) / 100f;
                    cbillModifier *= (settings.NonStandardCostModifier + 100f) / 100f;
                }

                if (tags.Contains("mech_quirk_prototype"))
                {
                    techModifier *= (settings.PrototypeTechModifier + 100f) / 100f;
                    cbillModifier *= (settings.PrototypeCostModifier + 100f) / 100f;
                }
            }
            catch (Exception ex)
            {
                Main.Log.LogException(ex);
                techModifier = 1f;
                cbillModifier = 1f;
            }
        }

        #endregion

        #region Status Evaluation

        /// <summary>
        /// Evaluates whether a given mech needs any structure repaired.
        /// </summary>
        public static bool NeedsStructureRepair(this MechDef mech)
        {
            foreach (var cLoc in repairPriorities.Values)
            {
                var loadout = mech.GetLocationLoadoutDef(cLoc);

                float currentStructure = loadout.CurrentInternalStructure;
                float maxStructure = mech.GetChassisLocationDef(cLoc).InternalStructure;

                if ((int)Mathf.Abs(currentStructure - maxStructure) > 0)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Evaluates whether a given mech needs any armor repaired.
        /// </summary>
        public static bool NeedArmorRepair(this MechDef mech)
        {
            foreach (var cLoc in repairPriorities.Values)
            {
                var loadout = mech.GetLocationLoadoutDef(cLoc);

                int armorDifference = loadout == mech.CenterTorso || loadout == mech.RightTorso || loadout == mech.LeftTorso
                    ? (int)Mathf.Abs(loadout.CurrentArmor - loadout.AssignedArmor) + (int)Mathf.Abs(loadout.CurrentRearArmor - loadout.AssignedRearArmor)
                    : (int)Mathf.Abs(loadout.CurrentArmor - loadout.AssignedArmor);

                if (armorDifference > 0)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Evaluates whether a given mech has any damaged components
        /// </summary>
        public static bool HasDamagedComponents(this MechDef mech)
        {
            foreach (var component in mech.Inventory)
            {
                if (component.DamageLevel == ComponentDamageLevel.Penalized)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Evaluates whether a given mech has any destroyed components.
        /// </summary>
        public static bool HasDestroyedComponents(this MechDef mech)
        {
            foreach (var component in mech.Inventory)
            {
                if (component.DamageLevel == ComponentDamageLevel.Destroyed)
                    return true;
            }
            return false;
        }

        #endregion
    }
}