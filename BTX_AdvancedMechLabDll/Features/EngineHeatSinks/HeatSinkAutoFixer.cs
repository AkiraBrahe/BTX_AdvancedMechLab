using BattleTech;
using CustomComponents;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using static BTX_AdvancedMechLab.Features.EngineHeatSinks.HeatSinkManager;

namespace BTX_AdvancedMechLab.Features.EngineHeatSinks
{
    internal class HeatSinkAutoFixer
    {
        public static void Register() => AutoFixer.Shared.RegisterMechFixer(AutoFixHeatSinkCount);

        /// <summary>
        /// Fixes heat sink counts on all mechs to match their engine rating.
        /// </summary>
        public static void AutoFixHeatSinkCount(List<MechDef> mechDefs)
        {
            var sw = Stopwatch.StartNew();
            foreach (var mech in mechDefs)
            {
                try
                {
                    NormalizeHeatSinkCount(mech);
                }
                catch (Exception e)
                {
                    Main.Log.LogError($"Error auto-fixing heat sink count for {mech.Description.Id}: {e}");
                }
            }
            sw.Stop();
            Main.Log.LogDebug($"Auto-fixed heat sink counts for {mechDefs.Count} mechs in {sw.Elapsed.TotalSeconds:F2} seconds.");
        }

        private static HashSet<string> processedChassis = [];

        internal static void NormalizeHeatSinkCount(MechDef mech)
        {
            if (!string.IsNullOrEmpty(mech.CoolingType)) return;

            if (!mech.DataManager.ChassisDefs.TryGet(mech.Chassis.Description.Id, out var chassis))
                chassis = mech.Chassis;

            var specs = GetEngineSpecs(chassis);
            var inventory = mech.Inventory.ToList();

            string internalId = specs.InternalDefID;
            string externalId = specs.ExternalDefID;
            int heatSinkingPerHS = specs.HSType == EngineHSType.Single ? 3 : 6;
            mech.CoolingType = specs.HSType.ToString();

            int standardHSCount = (chassis.Heatsinks + 30) / heatSinkingPerHS;
            int normalizedHSCount = Mathf.Min(10, specs.MinInternal);
            mech.InternalHeatSinks = normalizedHSCount;

            int internalToAdd = Mathf.Max(0, standardHSCount - 10);
            int externalToAdd = Mathf.Max(0, 10 - normalizedHSCount);

            // Step A: Convert excess heat dissipation into internal heat sinks
            if (internalToAdd > 0)
            {
                for (int i = 0; i < internalToAdd; i++)
                {
                    inventory.Add(new MechComponentRef(internalId, "", ComponentType.HeatSink, ChassisLocations.CenterTorso) { DataManager = mech.DataManager });
                }
            }

            // Step B: Internalize heat sinks that can fit in the engine
            if (specs.AdditionalSlots > 0)
            {
                var toMove = inventory.Where(c => c.ComponentDefID == externalId).Take(specs.AdditionalSlots).ToList();
                if (toMove.Count > 0)
                {
                    inventory = [.. inventory.Except(toMove)];
                    for (int i = 0; i < toMove.Count; i++)
                    {
                        inventory.Add(new MechComponentRef(internalId, "", ComponentType.HeatSink, ChassisLocations.CenterTorso) { DataManager = mech.DataManager });
                    }
                }
            }

            // Step C: Externalize heat sinks that don't fit in the engine
            if (externalToAdd > 0)
            {
                int hsSize = specs.HSType == EngineHSType.Single ? 1 : (specs.HSType == EngineHSType.Double ? 3 : 2);

                var distribution = Globals.allLocations.ToDictionary(l => l, l => 0);
                var freeSlots = distribution.Keys.ToDictionary(l => l, l => mech.GetFreeSlotsInLoc([.. inventory], l, hsSize));

                AddHeatSinks(mech, inventory, externalId, externalToAdd, distribution, freeSlots);

                // Fallback to internal if necessary
                if (externalToAdd > 0)
                {
                    Main.Log.LogDebug($"{mech.Description.Id} lacked space for {externalToAdd} external heat sinks. Adding internal instead.");
                    for (int i = 0; i < externalToAdd; i++)
                    {
                        inventory.Add(new MechComponentRef(internalId, "", ComponentType.HeatSink, ChassisLocations.CenterTorso) { DataManager = mech.DataManager });
                    }
                }
            }

            // Step D: Adjust chassis tonnage once based on added heat sinks
            if (!processedChassis.Contains(chassis.Description.Id))
            {
                int addedHeatSinks = internalToAdd + externalToAdd;
                chassis.InitialTonnage -= addedHeatSinks;
                processedChassis.Add(chassis.Description.Id);
            }

            // Reassign chassis if prefabOverride is set
            if (!ReferenceEquals(mech.Chassis, chassis))
            {
                mech.Chassis = chassis;
            }

            // Save changes
            mech.SetInventory([.. inventory]);
            mech.RefreshInventory();
        }

        /// <summary>
        /// Adds heat sinks in paired locations to prioritize symmetry.
        /// </summary>
        private static void AddHeatSinks(MechDef mech, List<MechComponentRef> inventory, string externalId, int externalToAdd, Dictionary<ChassisLocations, int> distribution, Dictionary<ChassisLocations, int> freeSlots)
        {
            void AddSingle(ChassisLocations loc)
            {
                if (externalToAdd > 0 && freeSlots[loc] > 0)
                {
                    distribution[loc]++;
                    freeSlots[loc]--;
                    externalToAdd--;
                }
            }

            void AddInPairs(ChassisLocations loc1, ChassisLocations loc2)
            {
                while (externalToAdd >= 2 && freeSlots[loc1] > 0 && freeSlots[loc2] > 0)
                {
                    AddSingle(loc1);
                    AddSingle(loc2);
                }
            }

            void AddOdd(ChassisLocations loc1, ChassisLocations loc2)
            {
                if (externalToAdd % 2 != 0)
                {
                    if (freeSlots[loc1] > 0) AddSingle(loc1);
                    else if (freeSlots[loc2] > 0) AddSingle(loc2);
                }
            }

            AddInPairs(ChassisLocations.LeftTorso, ChassisLocations.RightTorso);
            AddOdd(ChassisLocations.Head, ChassisLocations.CenterTorso);
            AddInPairs(ChassisLocations.LeftArm, ChassisLocations.RightArm);
            AddInPairs(ChassisLocations.LeftLeg, ChassisLocations.RightLeg);

            if (externalToAdd > 0)
            {
                foreach (var loc in Globals.allLocations)
                {
                    while (externalToAdd > 0 && freeSlots[loc] > 0) AddSingle(loc);
                }
            }

            foreach (var kvp in distribution)
            {
                for (int i = 0; i < kvp.Value; i++)
                {
                    inventory.Add(new MechComponentRef(externalId, "", ComponentType.HeatSink, kvp.Key) { DataManager = mech.DataManager });
                }
            }
        }
    }
}