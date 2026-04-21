using System;
using System.Collections.Generic;

namespace BTX_AdvancedMechLab.Core.Data
{
    public class StructureData
    {
        public enum StructureType
        {
            Standard,
            Primitive,
            Industrial,
            EndoSteel,
            Composite,
            Reinforced
        }

        public struct StructureInfo
        {
            public string Name;
            public string Tag;
            public string ScrapItemDefID;
            public int CriticalSlots;
            public DateTime IntroDate;
            public DateTime ProductionDate;
            public float WeightMultiplier;
            public float TPCost;
            public float CBCost;
        }

        public static Dictionary<StructureType, StructureInfo> StructureTypes = new()
        {
            { StructureType.Standard, new StructureInfo {
                Name = "Standard Structure",
                Tag = string.Empty,
                ScrapItemDefID = string.Empty,
                CriticalSlots = 0,
                IntroDate = DateTime.MinValue,
                ProductionDate = DateTime.MinValue,
                WeightMultiplier = 1f,
                TPCost = 1f,
                CBCost = 1f // 4,000 C-Bills per ton
            } },
            { StructureType.Primitive, new StructureInfo {
                Name = "Primitive",
                Tag = "chassis_primitive",
                ScrapItemDefID = string.Empty,
                CriticalSlots = 0,
                IntroDate = DateTime.MinValue,
                ProductionDate = DateTime.MinValue,
                WeightMultiplier = 1f,
                TPCost = 1f,
                CBCost = 0.75f // 3,000 C-Bills per ton
            } },
            { StructureType.Industrial, new StructureInfo {
                Name = "Industrial",
                Tag = "chassis_industrial",
                ScrapItemDefID = string.Empty,
                CriticalSlots = 0,
                IntroDate = DateTime.MinValue,
                ProductionDate = DateTime.MinValue,
                WeightMultiplier = 2f,
                TPCost = 1f,
                CBCost = 0.75f // 3,000 C-Bills per ton
            } },
            { StructureType.EndoSteel, new StructureInfo {
                Name = "Endo Steel",
                Tag = "chassis_endo",
                ScrapItemDefID = string.Empty,
                CriticalSlots = 12,
                IntroDate = new DateTime(3035, 1, 1),
                ProductionDate = new DateTime(3040, 1, 1),
                WeightMultiplier = 0.5f,
                TPCost = 2f,
                CBCost = 8f // 32,000 C-Bills per ton (96,000 C-Bills per ton before 3040)
            } },
            { StructureType.Composite, new StructureInfo {
                Name = "Composite",
                Tag = "chassis_composite",
                ScrapItemDefID = string.Empty,
                CriticalSlots = 0,
                IntroDate = new DateTime(3054, 1, 1),
                ProductionDate = new DateTime(3061, 1, 1),
                WeightMultiplier = 0.5f,
                TPCost = 2f,
                CBCost = 8f // 32,000 C-Bills per ton
            } },
            { StructureType.Reinforced, new StructureInfo {
                Name = "Reinforced",
                Tag = "chassis_reinforced",
                ScrapItemDefID = string.Empty,
                CriticalSlots = 0,
                IntroDate = new DateTime(3055, 1, 1),
                ProductionDate = new DateTime(3057, 1, 1),
                WeightMultiplier = 2f,
                TPCost = 2f,
                CBCost = 8f // 32,000 C-Bills per ton
            } }
        };
        // Note: Costs are scaled for a 100-ton mech.
    }
}