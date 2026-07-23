using System;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace BetterTroopHUD;

public class BetterTroopHudSettings : AttributeGlobalSettings<BetterTroopHudSettings>
{
    public override string Id => "BetterTroopHUD";
    public override string DisplayName => $"Better Troop HUD {typeof(BetterTroopHudSettings).Assembly.GetName().Version.ToString(3)}";
    public override string FolderName => "BetterTroopHUD";
    public override string FormatType => "json";

    private float _updateRate = 5f;
    
    [SettingPropertyBool("Show Widget", Order = 1, RequireRestart = false, HintText = "Used to hide the widget without exiting the game.")]
    [SettingPropertyGroup("General")]
    public bool ShowTroopStatsWidget { get; set; } = true;

    [SettingPropertyBool("Show additional Markers", Order = 2, RequireRestart = false, HintText = "Enables additional markers that provides more information about your troops.")]
    [SettingPropertyGroup("General")]
    public bool ShowWidgetMarkers { get; set; } = false;
    
    [SettingPropertyFloatingInteger("Update Rate", 1.5f, 10f, "0.0 seconds", Order = 3, RequireRestart = false, HintText = "Specifies how often the widget should be updated. Will affect performance if set too low.")]
    [SettingPropertyGroup("General")]
    public float UpdateRate
    {
        get => _updateRate;
        set => _updateRate = (float) Math.Round(value, 1); // Round to 1 decimal
    }
}