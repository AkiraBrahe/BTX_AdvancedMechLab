using BattleTech;
using HBS.Logging;
using Newtonsoft.Json;
using System;
using System.Reflection;

namespace BTX_AdvancedMechLab
{
    public class Main
    {
        internal static Harmony harmony;
        internal static string modDir;
        internal static ILog Log { get; private set; }
        internal static ModSettings Settings { get; private set; }

        public static void Init(string directory, string settingsJSON)
        {
            modDir = directory;
            Settings = JsonConvert.DeserializeObject<ModSettings>(settingsJSON) ?? new ModSettings();
            Log = Logger.GetLogger("BTX_AdvancedMechLab", Settings.Debug ? LogLevel.Debug : LogLevel.Log);

            try
            {
                harmony = new Harmony("com.github.AkiraBrahe.BTX_AdvancedMechLab");
                ApplyHarmonyPatches();
                RegisterAutoFixers();
                SyncQuirkSettings();
                Log.Log("Mod initialized!");
            }
            catch (Exception ex)
            {
                Log.LogException(ex);
            }
        }

        internal static void ApplyHarmonyPatches()
        {
            // --- BEX Quirks ---
            /* Repair Cost Modifiers */
            harmony.Unpatch(AccessTools.DeclaredMethod(typeof(SimGameState), "CreateMechRepairWorkOrder"), HarmonyPatchType.Prefix, "BEX.BattleTech.MechQuirks");
            harmony.Unpatch(AccessTools.Constructor(typeof(WorkOrderEntry_RepairMechStructure)), HarmonyPatchType.Prefix, "BEX.BattleTech.MechQuirks");

            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        internal static void RegisterAutoFixers()
        {
            Features.Armor.ArmorAutoFixer.Register();
            Features.EngineHeatSinks.HeatSinkAutoFixer.Register();
        }

        internal static void SyncQuirkSettings()
        {
            try
            {
                var settings = Quirks.MechQuirks.modSettings;
                if (!settings.ClansDifficultToMaint && !settings.ClansNonStandard)
                    Settings.ArmorRepair.ClanTechRepairCostMultiplier = 1.0f;
                if (!settings.ExtraTonnageRepairScaling)
                    Settings.ArmorRepair.EnableTonnageRepairScaling = false;
            }
            catch (Exception ex)
            {
                Log.LogException(ex);
            }
        }
    }
}