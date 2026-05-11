using BattleTech;
using CustomComponents;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BTX_AdvancedMechLab.Features.Armor
{
    internal class ArmorAutoFixer
    {
        public static void Register() => AutoFixer.Shared.RegisterMechFixer(AutoFixBlockers);

        /// <summary>
        /// Fixes the number of slots taken up by blockers to match the structure and armor type.
        /// </summary>
        private static void AutoFixBlockers(List<MechDef> mechs)
        {
            var sw = Stopwatch.StartNew();
            foreach (var mech in mechs)
            {
                try
                {
                    NormalizeBlockers(mech);
                }
                catch (Exception e)
                {
                    Main.Log.LogError($"Error auto-fixing blockers for {mech.Description.Id}: {e}");
                }
            }
            sw.Stop();
            Main.Log.LogDebug($"Auto-fixed blockers for {mechs.Count} mechs in {sw.Elapsed.TotalSeconds:F2} seconds.");
        }

        internal static void NormalizeBlockers(MechDef mech)
        {
            if (!string.IsNullOrEmpty(mech.ArmorType)) return;

            var structure = mech.GetStructureInfo();
            var armor = mech.GetArmorInfo();
            mech.ArmorType = armor.Type.ToString();

            if (!mech.DataManager.ChassisDefs.TryGet(mech.Chassis.Description.Id, out var chassis))
                chassis = mech.Chassis;

            var fixedInv = chassis.fixedEquipment?.ToList() ?? [];
            var mechInv = mech.Inventory.ToList();

            // Step A: Collect existing blockers
            var fixedBlockers = fixedInv.Where(c => c.ComponentDefID.StartsWith("Gear_Armor_")).ToList();
            var inventoryBlockers = mechInv.Where(c => c.ComponentDefID.StartsWith("Gear_Armor_")).ToList();
            var allBlockers = fixedBlockers.Concat(inventoryBlockers).ToList();

            int currentSlots = GetTotalBlockerSlots(allBlockers);
            int totalRequired = structure.CriticalSlots + armor.CriticalSlots;

            if (currentSlots == totalRequired && fixedBlockers.Count == 0)
            {
                return;
            }

            if (totalRequired == 0)
            {
                if (allBlockers.Count > 0)
                {
                    chassis.fixedEquipment = [.. fixedInv.Except(fixedBlockers)];
                    mech.SetInventory([.. mechInv.Except(inventoryBlockers)]);
                }
                return;
            }

            // Step B: Migrate blockers to inventory
            chassis.fixedEquipment = [.. fixedInv.Except(fixedBlockers)];
            var inventory = mechInv.Except(inventoryBlockers).ToList();

            string baseID = ArmorManager.GetBlockerBaseID(structure, armor);
            if (string.IsNullOrEmpty(baseID))
            {
                Main.Log.LogWarning($"{mech.Description.Id} has invalid armor configuration. Cannot auto-fix blockers.");
                return;
            }

            bool isClan = chassis.ChassisTags.Contains("chassis_clan");
            if (!isClan)
            {
                // IS Mech: Adjust blockers to match required slots
                if (allBlockers.Count == 0)
                {
                    AddBlockers(mech, ref allBlockers, baseID, totalRequired);
                }
                else
                {
                    if (currentSlots > totalRequired)
                    {
                        ReduceBlockers(allBlockers, currentSlots - totalRequired);
                    }
                    else if (currentSlots < totalRequired)
                    {
                        AddBlockers(mech, ref allBlockers, baseID, totalRequired - currentSlots);
                    }
                }
            }
            else
            {
                // Clan Mech: Add blockers from scratch
                if (currentSlots != totalRequired || allBlockers.Any(b => !b.ComponentDefID.StartsWith(baseID)))
                {
                    allBlockers.Clear();
                    AddBlockers(mech, ref allBlockers, baseID, totalRequired);
                }
            }

            // Reassign chassis if prefabOverride is set
            if (!ReferenceEquals(mech.Chassis, chassis))
            {
                mech.Chassis = chassis;
            }

            // Save changes
            inventory.AddRange(allBlockers);
            mech.SetInventory([.. inventory]);

            mech.RefreshInventory();
            chassis.RefreshInventory();
        }

        /// <summary>
        /// Gets the total number of inventory slots taken up by a list of blockers.
        /// </summary>
        private static int GetTotalBlockerSlots(List<MechComponentRef> allBlockers)
        {
            return allBlockers == null || allBlockers.Count == 0
                ? 0 : allBlockers.SelectMany(b => b.Def != null ? [b.Def.InventorySize] : new int[0]).Sum();
        }

        /// <summary>
        /// Reduces the number of inventory slots taken up by blockers.
        /// </summary>
        private static void ReduceBlockers(List<MechComponentRef> allBlockers, int slotsToRemove)
        {
            if (slotsToRemove <= 0) return;

            while (slotsToRemove > 0)
            {
                bool changed = false;
                foreach (var location in repairPriorities.Values)
                {
                    var blocker = allBlockers.FirstOrDefault(b => b.MountedLocation == location);
                    if (blocker != null)
                    {
                        int currentSize = blocker.Def.InventorySize;
                        if (currentSize <= 1)
                        {
                            allBlockers.Remove(blocker);
                        }
                        else
                        {
                            string currentSuffix = currentSize + "_Slot";
                            string newSuffix = currentSize - 1 + "_Slot";
                            blocker.ComponentDefID = blocker.ComponentDefID.Replace(currentSuffix, newSuffix);
                            blocker.RefreshComponentDef();
                        }
                        slotsToRemove--;
                        changed = true;
                        if (slotsToRemove <= 0) break;
                    }
                }
                if (!changed) break;
            }
        }

        /// <summary>
        /// Adds blockers by distributing them evenly across all locations.
        /// </summary>
        private static void AddBlockers(MechDef mech, ref List<MechComponentRef> allBlockers, string baseID, int slotsToAdd)
        {
            if (slotsToAdd <= 0) return;

            var distribution = allLocations.ToDictionary(l => l, l => 0);
            var freeSlots = distribution.Keys.ToDictionary(l => l, l => mech.GetFreeSlotsInLoc([.. mech.Inventory], l));

            void Fill(List<ChassisLocations> locations)
            {
                if (slotsToAdd <= 0) return;
                bool added;
                do
                {
                    added = false;
                    foreach (var loc in locations.OrderBy(l => distribution[l]))
                    {
                        if (slotsToAdd > 0 && freeSlots[loc] > 0)
                        {
                            distribution[loc]++;
                            freeSlots[loc]--;
                            slotsToAdd--;
                            added = true;
                            if (slotsToAdd <= 0) break;
                        }
                    }
                } while (added && slotsToAdd > 0);
            }

            Fill([.. sideLocations]);
            Fill([.. coreLocations]);

            if (slotsToAdd > 0)
            {
                Main.Log.LogWarning($"{mech.Description.Id} doesn't have enough free slots for blockers.");
            }

            foreach (var kvp in distribution)
            {
                int needed = kvp.Value;
                while (needed > 0)
                {
                    int size = Math.Min(needed, 8);
                    string itemID = $"{baseID}_{size}_Slot";
                    allBlockers.Add(new MechComponentRef(itemID, "", ComponentType.Upgrade, kvp.Key) { DataManager = mech.DataManager });
                    needed -= size;
                }
            }
        }
    }
}