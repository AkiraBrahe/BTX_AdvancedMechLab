using BattleTech;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace BTX_AdvancedMechLab.Patches.Overrides
{
    /// <summary>
    /// Overrides the heat sinking calculation to use the new internal heat sinks stat,
    /// which allows for mechs to have a variable number of engine heat sinks.
    /// </summary>
    [HarmonyPatch]
    public static class HeatSinkingCalculation_Patches
    {
        [HarmonyTargetMethods]
        public static IEnumerable<System.Reflection.MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Mech), "GetHeatSinkDissipation");
            yield return AccessTools.Method(typeof(MechStatisticsRules), "CalculateHeatEfficiencyStat");
            yield return AccessTools.Method(typeof(StatTooltipData), "SetHeatData");
        }

        public static float GetBaseHeatSinking(MechDef mech)
        {
            float capacity = mech.CoolingType is not null and ("Double" or "ClanDouble") ? 6.0f : 3.0f;
            return mech.InternalHeatSinks * capacity;
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var matcher = new CodeMatcher(instructions, il)
                .MatchForward(false,
                    new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(MechDef), nameof(MechDef.Chassis))),
                    new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ChassisDef), nameof(ChassisDef.Heatsinks)))
                );

            if (matcher.IsInvalid)
            {
                Main.Log.LogError("Could not find MechDef.Chassis getter to replace heat calculations.");
                return instructions;
            }
            int startIndex = matcher.Pos;
            matcher.MatchForward(true, new CodeMatch(OpCodes.Mul), new CodeMatch(OpCodes.Add));
            if (matcher.IsInvalid) return instructions;

            int endIndex = matcher.Pos;
            int count = endIndex - startIndex + 1;

            matcher.Advance(startIndex - matcher.Pos); // Move back to get_Chassis
            matcher.RemoveInstructions(count); // Remove the old calculation

            matcher.Insert(
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HeatSinkingCalculation_Patches), nameof(GetBaseHeatSinking)))
            );

            return matcher.InstructionEnumeration();
        }
    }
}