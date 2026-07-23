using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using static BetterTroopHUD.Utils;


namespace BetterTroopHUD
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            DisplayDebugMessage("[DEBUG] SubModule 'BetterTroopHUD' loaded");
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            base.OnMissionBehaviorInitialize(mission);
            DisplayDebugMessage("[DEBUG] SubModule.OnMissionBehaviorInitialize called");

            // Skip if mission does not include formation gameplay
            if (!IncludesFormationGameplay(mission))
            {
                DisplayDebugMessage("[DEBUG] SubModule.OnMissionBehaviorInitialize: mission does not include formation gameplay, will not attach behavior");
                return;
            }
            
            DisplayDebugMessage("[DEBUG] SubModule.OnMissionBehaviorInitialize: Attaching 'BetterTroopHudMissionBehavior'");
            mission.AddMissionBehavior(new BetterTroopHudMissionBehavior());
        }

        private static bool IncludesFormationGameplay(Mission mission)
        {
            // Non-combat missions will not include formation gameplay
            if (mission.CombatType != Mission.MissionCombatType.Combat)
            {
                DisplayDebugMessage("[DEBUG] SubModule.MissionIncludeFormationGameplay: mission is not combat, will thus not include formation gameplay");
                return false;
            }
            
            // Assume that if the mission contains a OrderTroopPlacer, it will include formation gameplay
            if (!ContainsMissionBehavior(mission, typeof(OrderTroopPlacer)))
            {
                DisplayDebugMessage("[DEBUG] SubModule.MissionIncludeFormationGameplay: mission does not contain OrderTroopPlacer, will thus not include formation gameplay");
                return false;
            }

            return true;
        }
        
        private static bool ContainsMissionBehavior(Mission mission, Type behaviorType)
        {
            List<MissionBehavior>? missionBehaviors = mission.MissionBehaviors;
            return missionBehaviors.Any(missionBehavior => missionBehavior.GetType() == behaviorType);
        }
    }
}