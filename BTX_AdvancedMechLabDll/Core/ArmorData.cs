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
                Name = "Standard Armor",
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
                Tag = "chassis_ferro",
                ScrapItemDefID = string.Empty,
                CriticalSlots = 12, // ~25% of available slots
                IntroDate = new DateTime(3034, 1, 1),
                ProductionDate = new DateTime(3040, 1, 1),
                PptMultiplier = 1.12f,
                TPCost = 1.5f,
                CBCost = 2f // 20,000 C-Bills per ton (60,000 C-Bills per ton before 3040)
            } },
            { ArmorType.ClanFerro, new ArmorInfo {
                Name = "Clan Ferro-Fibrous",
                Tag = "chassis_ferro",
                ScrapItemDefID = string.Empty,
                CriticalSlots = 6,
                IntroDate = DateTime.MaxValue,
                ProductionDate = DateTime.MaxValue,
                PptMultiplier = 1.2f,
                TPCost = 1.75f,
                CBCost = 6f // 60,000 C-Bills per ton (3x markup)
            } },
            { ArmorType.Hardened, new ArmorInfo {
                Name = "Hardened",
                Tag = "chassis_hardened",
                ScrapItemDefID = string.Empty,
                CriticalSlots = 0,
                IntroDate = new DateTime(3047, 1, 1),
                ProductionDate = new DateTime(3081, 1, 1),
                PptMultiplier = 0.5f,
                TPCost = 1.5f,
                CBCost = 1.5f // 15,000 C-Bills per ton
            } },
            { ArmorType.Stealth, new ArmorInfo {
                Name = "Stealth",
                Tag = "chassis_stealth",
                ScrapItemDefID = string.Empty,
                CriticalSlots = 12, // 2 per location, plus 2 for ECM
                IntroDate = new DateTime(3051, 1, 1),
                ProductionDate = new DateTime(3063, 1, 1),
                PptMultiplier = 1f,
                TPCost = 1.5f,
                CBCost = 5f // 50,000 C-Bills per ton
            } },
            { ArmorType.LightFerro, new ArmorInfo {
                Name = "Light Ferro",
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
