using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission;
using TaleWorlds.TwoDimension;

namespace BetterTroopHUD;

public class TroopHealthWidget : AgentHealthWidget
{
    private const float MarkerWidth = 2f;
    private const float MarkerHeightFactor = 1f; // Todo remove

    private bool _showMarkers;
    private Brush? _highestHealthFillBrush;
    private Brush? _healthMarkerBrush;
    private Widget? _troopWidgetContainer;

    private BrushWidget? _highestHealthWidget;
    private BrushWidget? _topMarkerWidget;
    private BrushWidget? _bottomMarkerWidget;

    private bool _hasSetupWidgets;
    private bool _hasValuesChanged;
    private float _prevHealth;
    private float _prevMaxHealth;

    private int _highestHealthValue;
    private int _bottomMarkerValue;
    private int _topMarkerValue;

    public TroopHealthWidget(UIContext context) : base(context)
    {
    }

    private (float highestHealthBarWidth, float highestHealthBarStartOffsetX, float bottomMarkerStartOffsetX, float topMarkerStartOffsetX) ComputeWidgetsWidthAndOffsets()
    {
        // Get the full max width of the health bar
        // Using .Right instead of .Size.X due to compilation error related to system.numerics.vector2
        float healthBarFullWidth = FindChild(new BindingPath("Canvas\\FillBar\\FillVisualParent")).Right / _scaleToUse;

        float highestHealthBarWidth = /*Mathf.Ceil*/(healthBarFullWidth * ((float)(HighestHealthValue - Health) / MaxHealth));
        float highestHealthBarStartOffsetX = /*Mathf.Floor*/(healthBarFullWidth * ((float)Health / MaxHealth));
        float bottomMarkerStartOffsetX = /*Mathf.Floor*/(healthBarFullWidth * ((float)BottomMarkerValue / MaxHealth));
        float topMarkerStartOffsetX = /*Mathf.Floor*/(healthBarFullWidth * ((float)TopMarkerValue / MaxHealth));

        // Fine-tune the positions for a better look
        const float halfMarkerWidth = MarkerWidth / 2f;
        bottomMarkerStartOffsetX -= halfMarkerWidth;
        topMarkerStartOffsetX -= halfMarkerWidth;

        // Make sure the widgets are not outside the health bar
        highestHealthBarWidth = Mathf.Min(highestHealthBarWidth, healthBarFullWidth);
        highestHealthBarStartOffsetX = Mathf.Min(highestHealthBarStartOffsetX, healthBarFullWidth - highestHealthBarWidth);
        bottomMarkerStartOffsetX = Mathf.Min(bottomMarkerStartOffsetX, healthBarFullWidth - halfMarkerWidth);
        topMarkerStartOffsetX = Mathf.Min(topMarkerStartOffsetX, healthBarFullWidth - halfMarkerWidth);

        return (highestHealthBarWidth, highestHealthBarStartOffsetX, bottomMarkerStartOffsetX, topMarkerStartOffsetX);
    }

    protected override void OnUpdate(float dt)
    {
        base.OnUpdate(dt);
        
        // Override base class' visibility logic
        // This is needed as the base class' visibility is set
        // to false when MedianPrc is 0, which is not what we want.
        // We want to show the widget as long as there are relevant selected troops left.
        if (!IsVisible && ShowHealthBar) // If set to invisible by base class, but we want to show it
        {
            IsVisible = true;
        }

        // Only update if the health bar is visible
        if (HealthBar == null || !HealthBar.IsVisible) return;

        // Only update if the values have changed
        if (!_hasValuesChanged && _prevHealth.ApproximatelyEqualsTo(base.Health) && _prevMaxHealth.ApproximatelyEqualsTo(base.MaxHealth)) return;
        _hasValuesChanged = false;
        // Keep track of previous values from superclass as well
        _prevHealth = base.Health;
        _prevMaxHealth = base.MaxHealth;

        // Setup widgets if not already done
        if (!_hasSetupWidgets)
        {
            SetupWidgets();
            return;
        }

        // Update contained widgets
        (float highestHealthBarWidth, float highestHealthBarStartOffsetX, float bottomMarkerStartOffsetX, float topMarkerStartOffsetX) = ComputeWidgetsWidthAndOffsets();
        _highestHealthWidget!.SuggestedWidth = highestHealthBarWidth;
        _highestHealthWidget.PositionXOffset = highestHealthBarStartOffsetX;
        _bottomMarkerWidget!.PositionXOffset = bottomMarkerStartOffsetX;
        _topMarkerWidget!.PositionXOffset = topMarkerStartOffsetX;
        _bottomMarkerWidget.IsVisible = _showMarkers;
        _topMarkerWidget.IsVisible = _showMarkers;
    }

    private void SetupWidgets()
    {
        _hasSetupWidgets = true;

        // Compute width and start offset for contained widgets
        (float highestHealthBarWidth, float highestHealthBarStartOffsetX, float bottomMarkerStartOffsetX, float topMarkerStartOffsetX) = ComputeWidgetsWidthAndOffsets();

        // Create the highest health bar widget
        _highestHealthWidget = new BrushWidget(Context);
        _highestHealthWidget.Id = "HighestHealth";
        _highestHealthWidget.WidthSizePolicy = SizePolicy.Fixed;
        _highestHealthWidget.HeightSizePolicy = SizePolicy.Fixed;
        _highestHealthWidget.Brush = HighestHealthFillBrush;
        _highestHealthWidget.SuggestedWidth = highestHealthBarWidth;
        _highestHealthWidget.SuggestedHeight = _highestHealthWidget.ReadOnlyBrush.Sprite.Height;
        _highestHealthWidget.HorizontalAlignment = HorizontalAlignment.Left;
        _highestHealthWidget.VerticalAlignment = VerticalAlignment.Center;
        _highestHealthWidget.PositionXOffset = highestHealthBarStartOffsetX;
        _highestHealthWidget.ParentWidget = TroopWidgetContainer; // Use container above bar widget

        // Create the bottom marker widget
        _bottomMarkerWidget = new BrushWidget(Context);
        _bottomMarkerWidget.Id = "BottomMarker";
        _bottomMarkerWidget.WidthSizePolicy = SizePolicy.Fixed;
        _bottomMarkerWidget.HeightSizePolicy = SizePolicy.Fixed;
        _bottomMarkerWidget.Brush = HealthMarkerBrush;
        _bottomMarkerWidget.SuggestedWidth = MarkerWidth;
        _bottomMarkerWidget.SuggestedHeight = Mathf.Ceil(_highestHealthWidget.ReadOnlyBrush.Sprite.Height * MarkerHeightFactor);
        _bottomMarkerWidget.HorizontalAlignment = HorizontalAlignment.Left;
        _bottomMarkerWidget.VerticalAlignment = VerticalAlignment.Center;
        _bottomMarkerWidget.PositionXOffset = bottomMarkerStartOffsetX;
        _bottomMarkerWidget.ParentWidget = TroopWidgetContainer; // Use container placed above bar widget

        // Create the top marker widget
        _topMarkerWidget = new BrushWidget(Context);
        _topMarkerWidget.Id = "TopMarker";
        _topMarkerWidget.WidthSizePolicy = SizePolicy.Fixed;
        _topMarkerWidget.HeightSizePolicy = SizePolicy.Fixed;
        _topMarkerWidget.Brush = HealthMarkerBrush;
        _topMarkerWidget.SuggestedWidth = MarkerWidth;
        _topMarkerWidget.SuggestedHeight = Mathf.Ceil(_highestHealthWidget.ReadOnlyBrush.Sprite.Height * MarkerHeightFactor);
        _topMarkerWidget.HorizontalAlignment = HorizontalAlignment.Left;
        _topMarkerWidget.VerticalAlignment = VerticalAlignment.Center;
        _topMarkerWidget.PositionXOffset = topMarkerStartOffsetX;
        _topMarkerWidget.ParentWidget = TroopWidgetContainer; // Use container placed above bar widget
    }

    // ---------------------------------------------
    // Editor properties
    // Attached/references in the BetterTroopHUD.xml
    // ---------------------------------------------

    [Editor]
    public bool ShowMarkers
    {
        get => _showMarkers;
        set
        {
            if (_showMarkers == value) return;
            _showMarkers = value;
            _hasValuesChanged = true;
            OnPropertyChanged(value);
        }
    }
    
    [Editor]
    public int HighestHealthValue
    {
        get => _highestHealthValue;
        set
        {
            if (_highestHealthValue == value) return;
            _highestHealthValue = value;
            _hasValuesChanged = true;
            OnPropertyChanged(value);
        }
    }

    [Editor]
    public int BottomMarkerValue
    {
        get => _bottomMarkerValue;
        set
        {
            if (_bottomMarkerValue == value) return;
            _bottomMarkerValue = value;
            _hasValuesChanged = true;
            OnPropertyChanged(value);
        }
    }

    [Editor]
    public int TopMarkerValue
    {
        get => _topMarkerValue;
        set
        {
            if (_topMarkerValue == value) return;
            _topMarkerValue = value;
            _hasValuesChanged = true;
            OnPropertyChanged(value);
        }
    }

    [Editor]
    public Brush? HighestHealthFillBrush
    {
        get => _highestHealthFillBrush;
        set
        {
            if (_highestHealthFillBrush == value)
                return;
            _highestHealthFillBrush = value;
            OnPropertyChanged(value);
        }
    }

    [Editor]
    public Brush? HealthMarkerBrush
    {
        get => _healthMarkerBrush;
        set
        {
            if (_healthMarkerBrush == value)
                return;
            _healthMarkerBrush = value;
            OnPropertyChanged(value);
        }
    }

    [Editor]
    public Widget? TroopWidgetContainer
    {
        get => _troopWidgetContainer;
        set
        {
            if (_troopWidgetContainer == value)
                return;
            _troopWidgetContainer = value;
            OnPropertyChanged(value);
        }
    }
}