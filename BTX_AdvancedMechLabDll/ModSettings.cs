namespace BTX_AdvancedMechLab
{
    public class ModSettings
    {
        public ArmorRepairSettings ArmorRepair { get; set; } = new ArmorRepairSettings();
    }

    public class ArmorRepairSettings
    {
        public bool EnableStructureRepair { get; set; } = true;
        public bool EnableTonnageRepairScaling { get; set; } = true;
        public bool EnableAutoRepairPrompt { get; set; } = true;
        public bool AutoRepairMechsWithDestroyedComponents { get; set; } = true;
        public float ClanTechRepairCostMultiplier { get; set; } = 1.5f;
    }
}