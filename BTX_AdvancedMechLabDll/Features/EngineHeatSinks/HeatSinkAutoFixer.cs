using BattleTech;
using CustomComponents;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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

        internal static void NormalizeHeatSinkCount(MechDef mech)
        {
            if (!mech.DataManager.ChassisDefs.TryGet(mech.Chassis.Description.Id, out var chassis))
                chassis = mech.Chassis;

            var cache = chassis.GetComponent<AdvancedChassisData>();
            if (cache == null) return;

            for (int i = 0; i < mech.Inventory.Length; i++)
            {
                if (mech.Inventory[i].ComponentDefType == ComponentType.HeatSink &&
                    mech.Inventory[i].IsCategory("Internal"))
                {
                    return;
                }
            }

            // Step A: Determine how many heat sinks the engine can support.
            var hsType = mech.MechTags.GetCoolingType();
            var specs = HeatSinkManager.GetEngineSpecs(chassis, hsType);

            if (cache.ExtraHSCount == 0 && specs.AdditionalSlots == 0) return;

            var inventory = mech.Inventory.ToList();
            string externalId = specs.ExternalDefID;
            string internalId = specs.InternalDefID;

            int heatSinksToAdd = cache.ExtraHSCount;
            int slotsAvailable = specs.AdditionalSlots;

            // Step B: Internalize heat sinks if more can fit in the engine
            if (slotsAvailable > 0)
            {
                int internalToAdd = Math.Max(0, heatSinksToAdd - slotsAvailable);
                if (internalToAdd > 0)
                {
                    heatSinksToAdd -= internalToAdd;
                    for (int i = 0; i < internalToAdd; i++)
                    {
                        inventory.Add(new MechComponentRef(internalId, "", ComponentType.HeatSink, ChassisLocations.CenterTorso) { DataManager = mech.DataManager });
                        slotsAvailable--;
                    }
                }

                if (slotsAvailable > 0)
                {
                    var toMove = inventory.Where(c => c.ComponentDefID == externalId).Take(slotsAvailable).ToList();
                    if (toMove.Count > 0)
                    {
                        inventory = [.. inventory.Except(toMove)];
                        for (int i = 0; i < toMove.Count; i++)
                        {
                            inventory.Add(new MechComponentRef(internalId, "", ComponentType.HeatSink, ChassisLocations.CenterTorso) { DataManager = mech.DataManager });
                        }
                    }
                }
            }

            // Step C: Externalize heat sinks that don't fit in the engine
            if (heatSinksToAdd > 0)
            {
                int hsSize = HeatSinkTypes[specs.HSType].Slots;

                var distribution = allLocations.ToDictionary(l => l, l => 0);
                var freeSlots = distribution.Keys.ToDictionary(l => l, l => mech.GetFreeSlotsInLoc([.. inventory], l, hsSize));

                AddHeatSinks(mech, inventory, externalId, heatSinksToAdd, distribution, freeSlots, out int leftover);

                if (leftover > 0)
                {
                    for (int i = 0; i < leftover; i++)
                    {
                        inventory.Add(new MechComponentRef(internalId, "", ComponentType.HeatSink, ChassisLocations.CenterTorso) { DataManager = mech.DataManager });
                    }
                }
            }

            mech.SetInventory([.. inventory]);
            mech.RefreshInventory();
        }

        /// <summary>
        /// Adds heat sinks in paired locations to prioritize symmetry.
        /// </summary>
        private static void AddHeatSinks(MechDef mech, List<MechComponentRef> inventory, string externalId, int externalToAdd, Dictionary<ChassisLocations, int> distribution, Dictionary<ChassisLocations, int> freeSlots, out int leftover)
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
                foreach (var loc in allLocations)
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

            leftover = externalToAdd;
        }
    }
}