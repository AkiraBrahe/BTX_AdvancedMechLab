using BattleTech;
using System;
using System.Collections.Generic;

namespace BTX_AdvancedMechLab.Features.Armor
{
    internal class ArmorManager
    {
        /// <summary>
        /// Determines the appropriate blockers for a mech based on its structure and armor types.
        /// </summary>
        public static string GetBlockerBaseID(StructureInfo structure, ArmorInfo armor)
        {
            if (structure.CriticalSlots == 0 && armor.CriticalSlots == 0) return null;
            if (structure.CriticalSlots == 0) return $"Gear_Armor_{armor.Type}";
            if (armor.CriticalSlots == 0) return $"Gear_Armor_{structure.Type}";

            if (structure.Type == StructureType.EndoSteel)
            {
                switch (armor.Type)
                {
                    case ArmorType.FerroFibrous:
                        return "Gear_Armor_EndoFerroCombo";
                    case ArmorType.LightFerro:
                        return "Gear_Armor_EndoLightFerroCombo";
                    case ArmorType.HeavyFerro:
                        return "Gear_Armor_EndoHeavyFerroCombo";
                }
            }
            else if (structure.Type == StructureType.ClanEndoSteel
                && armor.Type == ArmorType.ClanFerroFibrous)
            {
                return "Gear_Armor_ClanEndoFerroCombo";
            }

            return null;
        }

        /// <summary>
        /// Returns a list of available armor types at the current date.
        /// </summary>
        public static List<ArmorInfo> GetAvailableArmorTypes(SimGameState simGame)
        {
            var currentDate = simGame.CurrentDate;
            var armorTypes = new List<ArmorInfo>();

            foreach (var armorType in ArmorTypes.Values)
            {
                if (currentDate >= armorType.IntroDate && armorType.ScrapItemDefID != string.Empty)
                {
                    armorTypes.Add(armorType);
                }
            }

            return armorTypes;
        }

        private static readonly Random _rng = new();

        /// <summary>
        /// Returns a list of random chassis locations. Guarantees at least three locations, with a chance for more on "bad rolls".
        /// </summary>
        public static List<ChassisLocations> GetRandomPatchworkLocations()
        {
            var result = new HashSet<ChassisLocations>();

            while (result.Count < 3)
            {
                result.Add(allLocations[_rng.Next(allLocations.Length)]);
            }

            while (_rng.NextDouble() < 0.20 && result.Count < allLocations.Length)
            {
                result.Add(allLocations[_rng.Next(allLocations.Length)]);
            }

            return [.. result];
        }
    }
}