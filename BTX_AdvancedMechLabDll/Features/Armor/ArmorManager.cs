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
    }
}