using BattleTech;
using System.Collections.Generic;

namespace BTX_AdvancedMechLab.Core
{
    public class Globals
    {
        #region Tags

        /* ADVANCED MECHLAB TAGS
         * Names of custom tags used by Advanced MechLab.
         */
        public const string ArmorPrefix = "AML_Armor_";
        public const string CoolingPrefix = "AML_Cooling_";
        public const string PatchworkPrefix = "AML_Patchwork_";

        #endregion

        #region Temporary Variables

        /* TEMP MECHLAB QUEUE
         * Temporary queue to hold post-battle work orders until player confirms they want them processed.
         */
        public static List<WorkOrderEntry_MechLab> tempMechLabQueue = [];

        /* TEMP REPAIR INTENTIONS
         * Temporary list to hold the intended repair actions for each mech until player confirms they want them processed.
         */
        public static List<RepairIntention> tempRepairIntentions = [];

        /* REPAIR INTENTION
         * Holds the intended repair actions for a single mech location.
         */
        public class RepairIntention
        {
            public string MechID { get; set; }
            public ChassisLocations Location { get; set; }
            public ArmorType ArmorType { get; set; }
            public int RequiredScrapKG { get; set; }
            public bool UsesPatchwork { get; set; }
        }

        /* TEMP REPAIR CONSUMPTION RESULT
         * Temporary result of consuming scraps after all mech repairs have been processed.
         */
        public static ScrapConsumptionResult TempRepairResult { get; set; }

        /* SCRAP CONSUMPTION RESULT
         * Result of consuming scraps for armor repairs.
         */
        public enum ScrapConsumptionResult
        {
            NotSet,
            Failed_InsufficientScrap,
            Success_Depleted,
            Success
        }

        #endregion

        #region Constants

        /* REPAIR PRIORITIES
         * Set priority order of chassis locations for repairs (key 0 = highest priority).
         * 
         * These are ordered so that repair work orders target the most important locations for the player first.
         * This lets them cancel a work order before it completes but still have key locations like the Head and Center Torso repaired.
         */
        public static Dictionary<int, ChassisLocations> repairPriorities = new()
        {
            { 0, ChassisLocations.CenterTorso },
            { 1, ChassisLocations.Head },
            { 2, ChassisLocations.LeftTorso },
            { 3, ChassisLocations.RightTorso },
            { 4, ChassisLocations.LeftLeg },
            { 5, ChassisLocations.RightLeg },
            { 6, ChassisLocations.LeftArm },
            { 7, ChassisLocations.RightArm }
        };

        /* ALL LOCATIONS
         * Logically ordered locations on a mech.
         */
        public static readonly ChassisLocations[] allLocations = [
            ChassisLocations.Head,
            ChassisLocations.LeftArm,
            ChassisLocations.LeftTorso,
            ChassisLocations.CenterTorso,
            ChassisLocations.RightTorso,
            ChassisLocations.RightArm,
            ChassisLocations.LeftLeg,
            ChassisLocations.RightLeg
        ];

        /* SIDE LOCATIONS
         * Locations that are considered side locations on a mech.
         */
        public static readonly ChassisLocations[] sideLocations = [
            ChassisLocations.LeftArm, ChassisLocations.RightArm,
            ChassisLocations.LeftTorso, ChassisLocations.RightTorso,
            ChassisLocations.LeftLeg, ChassisLocations.RightLeg
        ];

        /* CORE LOCATIONS
         * Locations that are considered core locations on a mech.
         */
        public static readonly ChassisLocations[] coreLocations = [
            ChassisLocations.CenterTorso,
            ChassisLocations.Head
        ];

        #endregion
    }
}