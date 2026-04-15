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
            Log = Logger.GetLogger("BTX_AdvancedMechLab", LogLevel.Debug);

            try
            {
                Settings = JsonConvert.DeserializeObject<ModSettings>(settingsJSON) ?? new ModSettings();
                harmony = new Harmony("com.github.AkiraBrahe.BTX_AdvancedMechLab");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Log("Mod initialized!");
            }
            catch (Exception ex)
            {
                Log.LogException(ex);
            }
        }
    }
}