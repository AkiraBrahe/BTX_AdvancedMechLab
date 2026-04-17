using System;
using System.Collections.Generic;

namespace BTX_AdvancedMechLab.Core
{
    public class ArmorData
    {
        public enum ArmorType
        {
            Standard,
            Primitive,
            Industrial,
            HeavyIndustrial,
            FerroFibrous,
            ClanFerro,
            Hardened,
            Stealth,
            LightFerro,
            HeavyFerro,
            Reflective,
            Reactive
        }

        public struct ArmorInfo
        {
            public string Name;
            public string Description;
            public string Tag;
            public string ScrapItemDefID;
            public int CriticalSlots;
            public DateTime IntroDate;
            public DateTime ProductionDate;
            public float PptMultiplier;
            public float TPCost;
            public float CBCost;
        }

        public static Dictionary<ArmorType, ArmorInfo> ArmorTypes = new()
        {
            { ArmorType.Standard, new ArmorInfo {
                Name = "Standard",
                Description = "Standard armor provides reliable protection and is the baseline for all other armor types.",
                Tag = string.Empty,
                ScrapItemDefID = string.Empty,
                CriticalSlots = 0,
                IntroDate = DateTime.MinValue,
                ProductionDate = DateTime.MinValue,
                PptMultiplier = 1f,
                TPCost = 1f,
                CBCost = 1f // 10,000 C-Bills per ton
            } },
            { ArmorType.Primitive, new ArmorInfo {
                Name = "Primitive",
                Description = "Primitive armor provides two-thirds the protection of standard armor at half the cost.",
                Tag = "chassis_primitive",
                ScrapItemDefID = string.Empty,
                CriticalSlots = 0,
                IntroDate = DateTime.MinValue,
                ProductionDate = DateTime.MinValue,
                PptMultiplier = 0.67f,
                TPCost = 1f,
                CBCost = 0.5f // 5,000 C-Bills per ton
            } },
            { ArmorType.Industrial, new ArmorInfo {
                Name = "Industrial",
                Description = "Industrial armor provides two-thirds the protection of standard armor at half the cost.",
                Tag = "chassis_industrial",
                ScrapItemDefID = string.Empty,
                CriticalSlots = 0,
                IntroDate = DateTime.MinValue,
                ProductionDate = DateTime.MinValue,
                PptMultiplier = 0.67f,
                TPCost = 1f,
                CBCost = 0.5f // 5,000 C-Bills per ton
            } },
            { ArmorType.HeavyIndustrial, new ArmorInfo {
                Name = "Heavy Industrial",
                Description = "Heavy Industrial armor provides the same protection as standard armor.",
                Tag = "chassis_heavy_industrial",
                ScrapItemDefID = string.Empty,
                CriticalSlots = 0,
                IntroDate = DateTime.MinValue,
                ProductionDate = DateTime.MinValue,
                PptMultiplier = 1f,
                TPCost = 1f,
                CBCost = 1f // 10,000 C-Bills per ton
            } },
            { ArmorType.FerroFibrous, new ArmorInfo {
                Name = "Ferro-Fibrous",
                Description = "Ferro-Fibrous armor provides 12% more protection than standard armor and requires 12 critical slots.",
                Tag = "chassis_ferro",
                ScrapItemDefID = string.Empty,
                CriticalSlots = 12,
                IntroDate = new DateTime(3034, 1, 1),
                ProductionDate = new DateTime(3040, 1, 1),
                PptMultiplier = 1.12f,
                TPCost = 1.5f,
                CBCost = 2f // 20,000 C-Bills per ton
            } },
            { ArmorType.ClanFerro, new ArmorInfo {
                Name = "Clan Ferro-Fibrous",
                Description = "Clan Ferro-Fibrous armor provides 20% more protection than standard armor and only requires 6 critical slots.",
                Tag = "chassis_ferro",
                ScrapItemDefID = string.Empty,
                CriticalSlots = 6,
                IntroDate = DateTime.MaxValue,
                ProductionDate = DateTime.MaxValue,
                PptMultiplier = 1.2f,
                TPCost = 1.5f, // 2.25x with default setting
                CBCost = 2f // 30,000 C-Bills per ton with default setting
            } },
            { ArmorType.Hardened, new ArmorInfo {
                Name = "Hardened",
                Description = "Hardened armor provides the same protection as standard armor and prevents through-armor criticals to the location it is applied to. Running speed is reduced when applied to the legs.",
                Tag = "chassis_hardened",
                ScrapItemDefID = string.Empty,
                CriticalSlots = 0,
                IntroDate = new DateTime(3047, 1, 1),
                ProductionDate = new DateTime(3081, 1, 1),
                PptMultiplier = 1f, // Simplified logic
                TPCost = 1.5f,
                CBCost = 1.5f // 15,000 C-Bills per ton
            } },
            { ArmorType.Stealth, new ArmorInfo {
                Name = "Stealth",
                Description = "Stealth armor provides the same protection as standard armor while making the 'Mech harder to detect and target as long as its ECM Suite is active. It requires 12 critical slots.",
                Tag = "chassis_stealth",
                ScrapItemDefID = string.Empty,
                CriticalSlots = 12,
                IntroDate = new DateTime(3051, 1, 1),
                ProductionDate = new DateTime(3063, 1, 1),
                PptMultiplier = 1f,
                TPCost = 1.5f,
                CBCost = 5f // 50,000 C-Bills per ton
            } },
            { ArmorType.LightFerro, new ArmorInfo {
                Name = "Light Ferro",
                Description = "Light Ferro-Fibrous armor provides 6% more protection than standard armor and requires 6 critical slots.",
                Tag = "chassis_light_ferro",
                ScrapItemDefID = string.Empty,
                CriticalSlots = 6,
                IntroDate = new DateTime(3055, 1, 1),
                ProductionDate = new DateTime(3067, 1, 1),
                PptMultiplier = 1.06f,
                TPCost = 1.25f,
                CBCost = 1.5f // 15,000 C-Bills per ton
            } },
            { ArmorType.HeavyFerro, new ArmorInfo {
                Name = "Heavy Ferro",
                Description = "Heavy Ferro-Fibrous armor provides 24% more protection than standard armor but requires 18 critical slots.",
                Tag = "chassis_heavy_ferro",
                ScrapItemDefID = string.Empty,
                CriticalSlots = 18,
                IntroDate = new DateTime(3056, 1, 1),
                ProductionDate = new DateTime(3069, 1, 1),
                PptMultiplier = 1.24f,
                TPCost = 1.25f,
                CBCost = 2.5f // 25,000 C-Bills per ton
            } },
            { ArmorType.Reflective, new ArmorInfo {
                Name = "Reflective",
                Description = "Reflective armor provides the same protection as standard armor and reflects 50% of incoming energy damage. It requires 10 critical slots.",
                Tag = "chassis_reflective",
                ScrapItemDefID = string.Empty,
                CriticalSlots = 10,
                IntroDate = new DateTime(3058, 1, 1),
                ProductionDate = new DateTime(3080, 1, 1),
                PptMultiplier = 1f,
                TPCost = 1.5f,
                CBCost = 3f // 30,000 C-Bills per ton
            } },
            { ArmorType.Reactive, new ArmorInfo {
                Name = "Reactive",
                Description = "Reactive armor provides the same protection as standard armor and reduces incoming missile and AoE damage by 50%. It requires 14 critical slots.",
                Tag = "chassis_reactive",
                ScrapItemDefID = string.Empty,
                CriticalSlots = 14,
                IntroDate = new DateTime(3063, 1, 1),
                ProductionDate = new DateTime(3081, 1, 1),
                PptMultiplier = 1f,
                TPCost = 1.5f,
                CBCost = 3f // 30,000 C-Bills per ton
            } },
        };
    }
}