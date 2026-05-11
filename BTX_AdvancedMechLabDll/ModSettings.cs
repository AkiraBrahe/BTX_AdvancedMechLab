namespace BTX_AdvancedMechLab
{
    public class ModSettings
    {
        public bool Debug { get; set; } = false;
        public ArmorRepairSettings ArmorRepair { get; set; } = new ArmorRepairSettings();
        public ArmorSalvageSettings ArmorSalvage { get; set; } = new ArmorSalvageSettings();
    }

    public class ArmorRepairSettings
    {
        public bool EnableAutoRepairPrompt { get; set; } = true;
        public bool AutoRepairMechsWithDestroyedComponents { get; set; } = true;
        public bool AutoRepairStructure { get; set; } = true;
        public bool ScaleStructureRepairTimeByTonnage { get; set; } = true;
        public float ClanTechRepairCostMultiplier { get; set; } = 1.5f;
    }

    public class ArmorSalvageSettings
    {
        public bool EnableArmorStackLimit { get; set; } = true;
        public int MaxTonsPerStack { get; set; } = 10;
        //public int MinPercentArmorSalvaged { get; set; } = 20;
        //public int MaxPercentArmorSalvaged { get; set; } = 50;
    }
}