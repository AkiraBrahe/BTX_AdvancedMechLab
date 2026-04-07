using BattleTech;

namespace BTX_AdvancedMechLab
{
    /// <summary>
    /// Compatibility stub for CAC. CAC looks for this class to hook into the contract resolution process.
    /// </summary>
    public static class SimGameState_ResolveCompleteContract_Patch
    {
        public static void Prefix(ref bool flag, SimGameState sim) => Patches.SimGameState_ResolveCompleteContract.Prefix(ref flag, sim);

        public static void Postfix(SimGameState sim) => Patches.SimGameState_ResolveCompleteContract.Postfix(sim);
    }
}