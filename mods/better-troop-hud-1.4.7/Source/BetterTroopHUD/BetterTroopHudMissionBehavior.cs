using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;
using static BetterTroopHUD.Utils;

namespace BetterTroopHUD;

public class BetterTroopHudMissionBehavior : MissionBattleUIBaseView
{
    private GauntletLayer? _gauntletLayer;
    private BetterTroopHudVM? _dataSource;

    public override void EarlyStart()
    {
        base.EarlyStart();
        DisplayDebugMessage("[DEBUG] EarlyStart: called");

        // OrderTroopPlacer is also present in some civilian settlement missions on
        // Bannerlord 1.4.7. Those missions do not have a battle HUD and attempting
        // to load this movie there can fail during mission startup.
        //
        // Keep this check in EarlyStart rather than OnMissionBehaviorInitialize:
        // battle UI initialization changes IsFriendlyMission to false before this
        // point, while it is still true when submodules add their mission behavior.
        if (Mission.IsFriendlyMission)
        {
            DisplayDebugMessage("[DEBUG] EarlyStart: friendly mission, skipping BetterTroopHUD movie");
            return;
        }
        
        _dataSource = new BetterTroopHudVM(Mission);
        _gauntletLayer = new GauntletLayer("BetterTroopHUD", 1, false);
        _gauntletLayer.LoadMovie("BetterTroopHUD", _dataSource);
        MissionScreen.AddLayer(_gauntletLayer);
    }

    public override void AfterStart()
    {
        base.AfterStart();
        
        DisplayDebugMessage("[DEBUG] AfterStart: called");
        
        _dataSource?.Initialize();
    }
    
    protected override void OnCreateView()
    {
        if (_dataSource is not null)
        {
            _dataSource.IsAgentStatusAvailable = true;
            _dataSource.Tick(0f, true);
        }

        if (_gauntletLayer is not null && !IsViewSuspended)
        {
            ScreenManager.SetSuspendLayer(_gauntletLayer, false);
        }
    }

    protected override void OnDestroyView()
    {
        if (_dataSource is not null)
        {
            _dataSource.IsAgentStatusAvailable = false;
        }

        if (_gauntletLayer is not null)
        {
            ScreenManager.SetSuspendLayer(_gauntletLayer, true);
        }
    }

    protected override void OnSuspendView()
    {
        if (_gauntletLayer is not null)
        {
            ScreenManager.SetSuspendLayer(_gauntletLayer, true);
        }
    }

    protected override void OnResumeView()
    {
        if (_gauntletLayer is not null && IsViewCreated)
        {
            ScreenManager.SetSuspendLayer(_gauntletLayer, false);
        }
    }

    // public override void OnMissionScreenInitialize()
    // {
    //     base.OnMissionScreenInitialize();
    //     
    //     DisplayDebugMessage("[DEBUG] OnMissionScreenInitialize: called");
    //     
    //     // todo Implement _isInDeployment
    //     // this._isInDeployement = base.Mission.GetMissionBehavior<BattleDeploymentHandler>() != null;
    //     // if (this._isInDeployement)
    //     // {
    //     //     this._deploymentMissionView = base.Mission.GetMissionBehavior<DeploymentMissionView>();
    //     //     if (this._deploymentMissionView != null)
    //     //     {
    //     //         DeploymentMissionView deploymentMissionView = this._deploymentMissionView;
    //     //         deploymentMissionView.OnDeploymentFinish = (OnPlayerDeploymentFinishDelegate)Delegate.Combine(deploymentMissionView.OnDeploymentFinish, new OnPlayerDeploymentFinishDelegate(this.OnDeploymentFinish));
    //     //     }
    //     // }
    // }
    
    public override void OnMissionScreenFinalize()
    {
        base.OnMissionScreenFinalize();
        
        DisplayDebugMessage("[DEBUG] OnMissionScreenFinalize: called");
        
        // Clean up
        if (_gauntletLayer is not null)
        {
            MissionScreen.RemoveLayer(_gauntletLayer);
        }
        _gauntletLayer = null;
        _dataSource?.OnFinalize();
        _dataSource = null;
    }

    // private void OnDeploymentFinish()
    // {
    //     this._isInDeployement = false;
    //     DeploymentMissionView deploymentMissionView = this._deploymentMissionView;
    //     deploymentMissionView.OnDeploymentFinish = (OnPlayerDeploymentFinishDelegate)Delegate.Remove(deploymentMissionView.OnDeploymentFinish, new OnPlayerDeploymentFinishDelegate(this.OnDeploymentFinish));
    // }
    
    public override void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
    {
        base.OnMissionModeChange(oldMissionMode, atStart);
        _dataSource?.OnMissionModeChange(oldMissionMode, atStart);
    }

    public override void OnMissionScreenTick(float dt)
    {
        base.OnMissionScreenTick(dt);
        
        // _dataSource?.IsInDeployment = _isInDeployment; // todo
        if (IsViewCreated && !IsViewSuspended)
        {
            _dataSource?.Tick(dt);
        }
    }

    public override void OnPhotoModeActivated()
    {
        base.OnPhotoModeActivated();
        
        // Hide UI
        if (_gauntletLayer is not null)
        {
            _gauntletLayer.UIContext.ContextAlpha = 0f;
        }
    }

    public override void OnPhotoModeDeactivated()
    {
        base.OnPhotoModeDeactivated();
        
        // Un-hide UI
        if (_gauntletLayer is not null)
        {
            _gauntletLayer.UIContext.ContextAlpha = 1f;
        }
    }
}
