using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static BetterTroopHUD.Utils;

namespace BetterTroopHUD;

public class BetterTroopHudVM : ViewModel
{
    private readonly Mission? _mission;
    private readonly BetterTroopHudSettings? _betterTroopHudSettings;
    private MissionPeer? _mpMissionPeer;
    private MissionMultiplayerGameModeBaseClient? _mpGameMode; // Note: Unused for now, can be used for MP-specific features later TODO remove?
    
    private MissionPeer? MpMissionPeer
    {
        get
        {
            // Return cached value if possible
            if (_mpMissionPeer != null) return _mpMissionPeer;

            // Otherwise, try to get the peer from the network
            if (GameNetwork.MyPeer != null)
            {
                _mpMissionPeer = GameNetwork.MyPeer.GetComponent<MissionPeer>();
            }

            return _mpMissionPeer;
        }
    }

    public bool IsAgentStatusAvailable { get; set; } // Note: Unused for now TODO remove? Specified but not used by TW code
    private bool _hasAttachedListeners;

    private const int ExpectedNumTroops = 500; // Arbitrary, but may as well be generous when allocating memory
    private readonly List<float> _troopHealthList = new List<float>(ExpectedNumTroops);
    private readonly List<float> _troopMountHealthList = new List<float>(ExpectedNumTroops);
    private readonly List<float> _troopShieldHealthList = new List<float>(ExpectedNumTroops);
    private readonly List<float> _troopAmmoCountList = new List<float>(ExpectedNumTroops);

    private float _updateInterval = 5f;
    private float _timeSinceLastUpdate = 5f;

    private bool _showTroopHealthBar;
    private bool _showTroopShieldHealthBar;
    private bool _showTroopMountHealthBar;
    private bool _showTopMostTroopArrowCountBar;
    private bool _showShieldPosTroopArrowCountBar;
    private bool _showWidgetMarkers;

    private int _healthMedianPrc;
    private int _healthMedianLowHalfPrc;
    private int _healthMedianHighHalfPrc;
    private int _healthMaxPrc;

    private int _shieldHealthMedianPrc;
    private int _shieldHealthMedianLowHalfPrc;
    private int _shieldHealthMedianHighHalfPrc;
    private int _shieldHealthMaxPrc;

    private int _mountHealthMedianPrc;
    private int _mountHealthMedianLowHalfPrc;
    private int _mountHealthMedianHighHalfPrc;
    private int _mountHealthMaxPrc;

    private int _arrowCountMedianPrc;
    private int _arrowCountMedianLowHalfPrc;
    private int _arrowCountMedianHighHalfPrc;
    private int _arrowCountMaxPrc;

    public BetterTroopHudVM(Mission? mission)
    {
        _mission = mission;
        _betterTroopHudSettings = BetterTroopHudSettings.Instance;
    }

    public void Initialize()
    {
        _mpGameMode = Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
    }

    public override void OnFinalize()
    {
        base.OnFinalize();

        if (_mission == null)
        {
            DisplayDebugMessage("BetterTroopsHUD - OnFinalize: Mission is null, cannot detach from formation selection events");
            return;
        }

        // Clean up listeners
        if (!TryGetPlayerOrderController(_mission, out OrderController? orderController))
        {
            DisplayMessage("BetterTroopsHUD - OnFinalize: Failed to get player order controller, cannot detach from formation selection events");
            return;
        }

        DisplayDebugMessage("OnFinalize: Detached from formation selection events");
        orderController.OnSelectedFormationsChanged -= OnSelectedFormationsChanged;

        _hasAttachedListeners = false;
    }

    public void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
    {
        DisplayDebugMessage($"[DEBUG] OnMissionModeChange: {oldMissionMode} -> {_mission?.Mode}, atStart: {atStart}");

        // Do not attach multiple times
        if (_hasAttachedListeners) return;

        // Do not attach if mission is null, would fail
        if (_mission == null) return;

        // Setup listeners
        if (!TryGetPlayerOrderController(_mission, out OrderController? orderController))
        {
            DisplayDebugMessage("Initialize: Failed to get player order controller, cannot attach to formation selection events");
            return;
        }

        DisplayDebugMessage("Initialize: Attached to formation selection events");
        orderController.OnSelectedFormationsChanged += OnSelectedFormationsChanged;

        _hasAttachedListeners = true;
    }

    public void Tick(float dt, bool force = false)
    {
        // Check if UI update is needed
        _timeSinceLastUpdate += dt;
        bool hasPressedKey = Input.IsKeyPressed(InputKey.O) && Input.IsKeyDown(InputKey.LeftControl);
        if (_timeSinceLastUpdate < _updateInterval && !hasPressedKey && !force) return;

        if (hasPressedKey) DisplayMessage("[BetterTroopHUD] Refreshed UI");

        // Reset counter upon ui update
        _timeSinceLastUpdate = 0f;
        
        // Update update rate, in case config changed
        if (_betterTroopHudSettings != null) _updateInterval = _betterTroopHudSettings.UpdateRate;

        if (_mission == null) return;
        if (_mission.MainAgent != null) // TODO: include check for IsInDeployement? (&& !IsInDeployment)
        {
            BattleTick(dt);
        }
        else
        {
            NonBattleTick(dt);
        }
    }

    private void BattleTick(float dt)
    {
        // Ensure we are in a battle, and not in a friendly mission
        if ((_mission?.Mode != MissionMode.Battle && _mission?.Mode != MissionMode.Stealth) || _mission.IsFriendlyMission || _mission.MainAgent == null) return;

        // If multiplayer TODO test if needed separate checks for MP support
        if (MpMissionPeer != null)
        {
            // Ensure we are controlling troops
            bool isTroopsActive = MpMissionPeer?.ControlledFormation != null;
            if (!isTroopsActive) return;

            // Ensure we have troops
            int troopCount = MpMissionPeer!.ControlledFormation.CountOfUnits;
            if (troopCount == 0) return;
        }

        // Ensure we have a valid order controller
        if (!TryGetPlayerOrderController(_mission, out OrderController? orderController)) return;

        // Get controlled formations
        MBReadOnlyList<Formation> selectedFormations = orderController.SelectedFormations;
        if (selectedFormations.Count == 0 || _betterTroopHudSettings?.ShowTroopStatsWidget == false)
        {
            // If no selected formations or disabled in config, hide HUD
            DisplayDebugMessage("[Debug] BattleTick: No selected formations or widget disabled in config, hiding HUD");
            ShowTroopHealthBar = false;
            ShowTroopShieldHealthBar = false;
            ShowTroopMountHealthBar = false;
            ShowShieldPosTroopArrowCountBar = false;
            ShowTopMostTroopArrowCountBar = false;
            return;
        }

        // Collect troop stats
        RefreshStoredUnitStats(selectedFormations);

        // Show UI bars
        ShowTroopHealthBar = _troopHealthList.Count > 0;
        ShowTroopShieldHealthBar = _troopShieldHealthList.Count > 0;
        ShowTroopMountHealthBar = _troopMountHealthList.Count > 0;
        // Show arrow count bar at shield bar position if appropriate
        ShowTopMostTroopArrowCountBar = _troopAmmoCountList.Count > 0 && ShowTroopShieldHealthBar;
        ShowShieldPosTroopArrowCountBar = _troopAmmoCountList.Count > 0 && !ShowTroopShieldHealthBar;

        // Update visibility of bar markers
        ShowWidgetMarkers = _betterTroopHudSettings?.ShowWidgetMarkers == true;
        
        // Update UI values
        UpdateUIBarValues();
    }

    private void NonBattleTick(float dt)
    {
        // Reset all values
        ShowTroopHealthBar = false;
        ShowTroopShieldHealthBar = false;
        ShowTroopMountHealthBar = false;
        ShowShieldPosTroopArrowCountBar = false;
        ShowTopMostTroopArrowCountBar = false;
        HealthMedianPrc = 0;
        HealthMedianLowHalfPrc = 0;
        HealthMedianHighHalfPrc = 0;
        HealthMaxPrc = 0;
        ShieldHealthMedianPrc = 0;
        ShieldHealthMedianLowHalfPrc = 0;
        ShieldHealthMedianHighHalfPrc = 0;
        ShieldHealthMaxPrc = 0;
        MountHealthMedianPrc = 0;
        MountHealthMedianLowHalfPrc = 0;
        MountHealthMedianHighHalfPrc = 0;
        MountHealthMaxPrc = 0;
        ArrowCountMedianPrc = 0;
        ArrowCountMedianLowHalfPrc = 0;
        ArrowCountMedianHighHalfPrc = 0;
        ArrowCountMaxPrc = 0;
    }

    private void OnSelectedFormationsChanged()
    {
        DisplayDebugMessage("OnSelectedFormationsChanged: called");

        Tick(0, true);
    }

    private void UpdateUIBarValues()
    {
        // Sort lists for later median calculation
        _troopHealthList.Sort();
        _troopShieldHealthList.Sort();
        _troopMountHealthList.Sort();
        _troopAmmoCountList.Sort();

        // Compute bar values
        GetMediansAsInts(_troopHealthList, out int median, out int lowHalfMedian, out int highHalfMedian, out int max);
        HealthMedianPrc = median;
        HealthMedianLowHalfPrc = lowHalfMedian;
        HealthMedianHighHalfPrc = highHalfMedian;
        HealthMaxPrc = max;

        GetMediansAsInts(_troopShieldHealthList, out median, out lowHalfMedian, out highHalfMedian, out max);
        ShieldHealthMedianPrc = median;
        ShieldHealthMedianLowHalfPrc = lowHalfMedian;
        ShieldHealthMedianHighHalfPrc = highHalfMedian;
        ShieldHealthMaxPrc = max;

        GetMediansAsInts(_troopMountHealthList, out median, out lowHalfMedian, out highHalfMedian, out max);
        MountHealthMedianPrc = median;
        MountHealthMedianLowHalfPrc = lowHalfMedian;
        MountHealthMedianHighHalfPrc = highHalfMedian;
        MountHealthMaxPrc = max;

        GetMediansAsInts(_troopAmmoCountList, out median, out lowHalfMedian, out highHalfMedian, out max);
        ArrowCountMedianPrc = median;
        ArrowCountMedianLowHalfPrc = lowHalfMedian;
        ArrowCountMedianHighHalfPrc = highHalfMedian;
        ArrowCountMaxPrc = max;
        return;

        // Inner helper function to get median values
        void GetMediansAsInts(IReadOnlyList<float> prcList, out int median, out int lowHalfMedian, out int highHalfMedian, out int max)
        {
            // Handle edge case, empty list
            if (prcList.Count == 0)
            {
                median = 0;
                lowHalfMedian = 0;
                highHalfMedian = 0;
                max = 0;
                return;
            }

            // Get median and max
            int medianIndex = prcList.Count / 2;
            median = (int)(prcList[prcList.Count / 2] * 100);
            max = (int)(prcList[prcList.Count - 1] * 100);

            // Handle edge cases, where there is no low or high half
            if (prcList.Count < 3)
            {
                lowHalfMedian = median;
                highHalfMedian = median;
                return;
            }

            // Get low and high half medians
            lowHalfMedian = (int)(prcList[medianIndex / 2] * 100);
            highHalfMedian = (int)(prcList[medianIndex + medianIndex / 2] * 100);
        }
    }

    private static bool TryGetPlayerOrderController(Mission mission, [NotNullWhen(returnValue: true)] out OrderController? orderController)
    {
        // Set default return value
        orderController = null;

        if (mission.Mode != MissionMode.Battle && mission.Mode != MissionMode.Stealth)
        {
            DisplayDebugMessage("[DEBUG] TryGetPlayerOrderController: mission mode is not battle, so no selected formations. Returning");
            return false;
        }

        if (mission.MainAgent == null)
        {
            DisplayDebugMessage("[DEBUG] TryGetPlayerOrderController: main agent is null, so no selected formations. Returning");
            return false;
        }

        if (mission.MainAgent.Health.ApproximatelyEqualsTo(0.0f))
        {
            DisplayDebugMessage("[DEBUG] TryGetPlayerOrderController: main agent is dead, so no selected formations. Returning");
            return false;
        }

        Team? playerTeam = mission.PlayerTeam;
        if (playerTeam == null)
        {
            DisplayDebugMessage("[DEBUG] TryGetPlayerOrderController: mission.PlayerTeam is null, so no selected formations. Returning");
            return false;
        }

        orderController = playerTeam.PlayerOrderController;
        if (orderController == null)
        {
            DisplayDebugMessage("[DEBUG] TryGetPlayerOrderController: orderController is null, so no selected formations. Returning");
            return false;
        }

        return true;
    }

    private void RefreshStoredUnitStats(MBReadOnlyList<Formation> selectedFormations)
    {
        // Reset lists
        _troopHealthList.Clear();
        _troopMountHealthList.Clear();
        _troopShieldHealthList.Clear();
        _troopAmmoCountList.Clear();

        // Compute stats for units in each selected formation
        foreach (Formation formation in selectedFormations)
        {
            // Get both attached (in formation) and detached units (using ladders, siege weapons, etc.) 
            List<IFormationUnit> formationUnits = formation.Arrangement.GetAllUnits().Concat(formation.DetachedUnits).ToList();
            foreach (IFormationUnit formationUnit in formationUnits)
            {
                // Skip non-agents
                if (formationUnit is not Agent agent)
                {
                    // DisplayDebugMessage("[DEBUG] UpdateUi: formationUnit is not Agent");
                    continue;
                }

                // Skip non-human agents
                if (!agent.IsHuman)
                {
                    DisplayDebugMessage("[DEBUG] UpdateUi: agent is not human");
                    continue;
                }

                // Collect human agent health
                float prcHealth = agent.Health / agent.HealthLimit;
                // DisplayDebugMessage($"[DEBUG] UpdateUi: agent is human. Health: {prcHealth:1f}");
                _troopHealthList.Add(prcHealth);

                // Collect mount agent health, if applicable
                if (agent.HasMount)
                {
                    Agent mountAgent = agent.MountAgent;
                    float prcMountHealth = mountAgent.Health / mountAgent.HealthLimit;
                    // DisplayDebugMessage($"[DEBUG] UpdateUi: agent has mount. Health: {prcMountHealth:1f}");
                    _troopMountHealthList.Add(prcMountHealth);
                }

                // Collect shield health, if applicable
                if (agent.HasShieldCached)
                {
                    int totHitPoints = 0;
                    int totMaxHitPoints = 0;

                    // Iterate over all equipment slots, and if the slot is a shield, add its health to the list
                    for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.ExtraWeaponSlot; equipmentIndex++)
                    {
                        // Skip non-shield equipment
                        MissionWeapon missionWeapon = agent.Equipment[equipmentIndex];
                        if (missionWeapon.IsEmpty || !missionWeapon.CurrentUsageItem.IsShield) continue;

                        // Get shield health
                        totHitPoints += missionWeapon.HitPoints;
                        totMaxHitPoints += missionWeapon.ModifiedMaxHitPoints;
                    }

                    float prcShieldHealth = (float)totHitPoints / totMaxHitPoints;
                    // DisplayDebugMessage($"[DEBUG] UpdateUi: agent has shield(s). Health: {prcShieldHealth:1f}");
                    _troopShieldHealthList.Add(prcShieldHealth);
                }

                // Collect (ranged) ammunition count, if applicable
                if (agent.IsRangedCached || agent.HasThrownCached)
                {
                    int totAmmo = 0;
                    int totMaxAmmo = 0;

                    // Iterate over all equipment slots, and if the slot is ammo, add its count to the list
                    for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.ExtraWeaponSlot; equipmentIndex++)
                    {
                        // Skip non-ammo and non-throwing equipment (i.e. not arrow, bolt, javelin, etc.)
                        MissionWeapon missionWeapon = agent.Equipment[equipmentIndex];
                        missionWeapon.GatherInformationFromWeapon(
                            out bool _,
                            out bool _,
                            out bool _,
                            out bool _,
                            out bool weaponHasThrown, // Defined as: is ranged weapon and is consumable
                            out WeaponClass _);
                        if (missionWeapon.IsEmpty || !missionWeapon.IsAnyAmmo() && !weaponHasThrown) continue;

                        // Get ammo count
                        totAmmo += missionWeapon.Amount;
                        totMaxAmmo += missionWeapon.ModifiedMaxAmount;
                    }

                    float prcAmmoCount = (float)totAmmo / totMaxAmmo;
                    // DisplayDebugMessage($"[DEBUG] UpdateUi: agent has ranged weapon. Ammo: {prcAmmoCount:1f}");
                    _troopAmmoCountList.Add(prcAmmoCount);
                }
            }
        }
    }

    // -----------------------------------------------------------------------
    // Data source properties
    // Used by TroopHealthWidget and attached/referenced in BetterTroopHUD.xml
    // -----------------------------------------------------------------------

    [DataSourceProperty] public int MaxBarPrc => 100;

    [DataSourceProperty]
    public bool ShowTroopHealthBar
    {
        get => _showTroopHealthBar;
        set
        {
            if (value == _showTroopHealthBar) return;
            _showTroopHealthBar = value;
            OnPropertyChangedWithValue(value);
        }
    }

    [DataSourceProperty]
    public bool ShowTroopShieldHealthBar
    {
        get => _showTroopShieldHealthBar;
        set
        {
            if (value == _showTroopShieldHealthBar) return;
            _showTroopShieldHealthBar = value;
            OnPropertyChangedWithValue(value);
        }
    }

    [DataSourceProperty]
    public bool ShowTroopMountHealthBar
    {
        get => _showTroopMountHealthBar;
        set
        {
            if (value == _showTroopMountHealthBar) return;
            _showTroopMountHealthBar = value;
            OnPropertyChangedWithValue(value);
        }
    }

    [DataSourceProperty]
    public bool ShowTopMostTroopArrowCountBar
    {
        get => _showTopMostTroopArrowCountBar;
        set
        {
            if (value == _showTopMostTroopArrowCountBar) return;
            _showTopMostTroopArrowCountBar = value;
            OnPropertyChangedWithValue(value);
        }
    }

    [DataSourceProperty]
    public bool ShowShieldPosTroopArrowCountBar
    {
        get => _showShieldPosTroopArrowCountBar;
        set
        {
            if (value == _showShieldPosTroopArrowCountBar) return;
            _showShieldPosTroopArrowCountBar = value;
            OnPropertyChangedWithValue(value);
        }
    }

    [DataSourceProperty]
    public bool ShowWidgetMarkers
    {
        get => _showWidgetMarkers;
        set
        {
            if (value == _showWidgetMarkers) return;
            _showWidgetMarkers = value;
            OnPropertyChangedWithValue(value);
        }
    }

    [DataSourceProperty]
    public int HealthMedianPrc
    {
        get => _healthMedianPrc;
        set
        {
            if (value == _healthMedianPrc) return;
            _healthMedianPrc = value;
            OnPropertyChangedWithValue(value);
        }
    }

    [DataSourceProperty]
    public int HealthMedianLowHalfPrc
    {
        get => _healthMedianLowHalfPrc;
        set
        {
            if (value == _healthMedianLowHalfPrc) return;
            _healthMedianLowHalfPrc = value;
            OnPropertyChangedWithValue(value);
        }
    }

    [DataSourceProperty]
    public int HealthMedianHighHalfPrc
    {
        get => _healthMedianHighHalfPrc;
        set
        {
            if (value == _healthMedianHighHalfPrc) return;
            _healthMedianHighHalfPrc = value;
            OnPropertyChangedWithValue(value);
        }
    }

    [DataSourceProperty]
    public int HealthMaxPrc
    {
        get => _healthMaxPrc;
        set
        {
            if (value == _healthMaxPrc) return;
            _healthMaxPrc = value;
            OnPropertyChangedWithValue(value);
        }
    }

    [DataSourceProperty]
    public int ShieldHealthMedianPrc
    {
        get => _shieldHealthMedianPrc;
        set
        {
            if (value == _shieldHealthMedianPrc) return;
            _shieldHealthMedianPrc = value;
            OnPropertyChangedWithValue(value);
        }
    }

    [DataSourceProperty]
    public int ShieldHealthMedianLowHalfPrc
    {
        get => _shieldHealthMedianLowHalfPrc;
        set
        {
            if (value == _shieldHealthMedianLowHalfPrc) return;
            _shieldHealthMedianLowHalfPrc = value;
            OnPropertyChangedWithValue(value);
        }
    }

    [DataSourceProperty]
    public int ShieldHealthMedianHighHalfPrc
    {
        get => _shieldHealthMedianHighHalfPrc;
        set
        {
            if (value == _shieldHealthMedianHighHalfPrc) return;
            _shieldHealthMedianHighHalfPrc = value;
            OnPropertyChangedWithValue(value);
        }
    }

    [DataSourceProperty]
    public int ShieldHealthMaxPrc
    {
        get => _shieldHealthMaxPrc;
        set
        {
            if (value == _shieldHealthMaxPrc) return;
            _shieldHealthMaxPrc = value;
            OnPropertyChangedWithValue(value);
        }
    }

    [DataSourceProperty]
    public int MountHealthMedianPrc
    {
        get => _mountHealthMedianPrc;
        set
        {
            if (value == _mountHealthMedianPrc) return;
            _mountHealthMedianPrc = value;
            OnPropertyChangedWithValue(value);
        }
    }

    [DataSourceProperty]
    public int MountHealthMedianLowHalfPrc
    {
        get => _mountHealthMedianLowHalfPrc;
        set
        {
            if (value == _mountHealthMedianLowHalfPrc) return;
            _mountHealthMedianLowHalfPrc = value;
            OnPropertyChangedWithValue(value);
        }
    }

    [DataSourceProperty]
    public int MountHealthMedianHighHalfPrc
    {
        get => _mountHealthMedianHighHalfPrc;
        set
        {
            if (value == _mountHealthMedianHighHalfPrc) return;
            _mountHealthMedianHighHalfPrc = value;
            OnPropertyChangedWithValue(value);
        }
    }

    [DataSourceProperty]
    public int MountHealthMaxPrc
    {
        get => _mountHealthMaxPrc;
        set
        {
            if (value == _mountHealthMaxPrc) return;
            _mountHealthMaxPrc = value;
            OnPropertyChangedWithValue(value);
        }
    }

    [DataSourceProperty]
    public int ArrowCountMedianPrc
    {
        get => _arrowCountMedianPrc;
        set
        {
            if (value == _arrowCountMedianPrc) return;
            _arrowCountMedianPrc = value;
            OnPropertyChangedWithValue(value);
        }
    }

    [DataSourceProperty]
    public int ArrowCountMedianLowHalfPrc
    {
        get => _arrowCountMedianLowHalfPrc;
        set
        {
            if (value == _arrowCountMedianLowHalfPrc) return;
            _arrowCountMedianLowHalfPrc = value;
            OnPropertyChangedWithValue(value);
        }
    }

    [DataSourceProperty]
    public int ArrowCountMedianHighHalfPrc
    {
        get => _arrowCountMedianHighHalfPrc;
        set
        {
            if (value == _arrowCountMedianHighHalfPrc) return;
            _arrowCountMedianHighHalfPrc = value;
            OnPropertyChangedWithValue(value);
        }
    }

    [DataSourceProperty]
    public int ArrowCountMaxPrc
    {
        get => _arrowCountMaxPrc;
        set
        {
            if (value == _arrowCountMaxPrc) return;
            _arrowCountMaxPrc = value;
            OnPropertyChangedWithValue(value);
        }
    }
}