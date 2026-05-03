using BattleTech;

namespace BTX_AdvancedMechLab
{
    internal class StatusEffects
    {
        internal static int RandomEffectID() => UnityEngine.Random.Range(1, int.MaxValue);

        internal static EffectData DHSHeatPenaltyEffect => new()
        {
            nature = EffectNature.Debuff,
            effectType = EffectType.StatisticEffect,
            targetingData = HideCreator,
            Description = new DescriptionDef("DHSHeatPenalty", "DHS HEAT PENALTY", "Heat penalty for DHS engines.", "uixSvgIcon_equipment_Heatsink", 0, 0f, false, null, null, null),
            durationData = InfiniteDuration,
            statisticData = new StatisticEffectData
            {
                statName = "HeatSinkCapacity",
                operation = StatCollection.StatOperation.Float_Subtract,
                modValue = "15",
                modType = "System.Int32"
            }
        };

        internal static EffectTargetingData HideCreator => new()
        {
            effectTriggerType = EffectTriggerType.Passive,
            triggerLimit = 0,
            extendDurationOnTrigger = 0,
            specialRules = AbilityDef.SpecialRules.NotSet,
            auraEffectType = AuraEffectType.NotSet,
            effectTargetType = EffectTargetType.Creator,
            alsoAffectCreator = false,
            range = 0f,
            forcePathRebuild = false,
            forceVisRebuild = false,
            showInTargetPreview = false,
            showInStatusPanel = false
        };

        internal static EffectDurationData InfiniteDuration => new()
        {
            duration = -1,
            stackLimit = 1
        };
    }
}