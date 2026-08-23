using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using FanWidget.Models;

namespace FanWidget.Views;

public partial class FanRowControl : System.Windows.Controls.UserControl
{
    public event EventHandler<FanSpeedEventArgs>? SpeedChanged;
    public event EventHandler? AutoRequested;
    public event EventHandler? RenameRequested;
    public event EventHandler? DragStarted;
    public event EventHandler? DragEnded;

    private bool _updatingUi;
    private FanDisplayItem? _item;

    public FanRowControl()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => BindItem();
        WireSlider();
    }

    public FanDisplayItem? Item => _item;

    private void BindItem()
    {
        _item = DataContext as FanDisplayItem;
        UpdateVisuals();
    }

    private void WireSlider()
    {
        FanSlider.AddHandler(Thumb.DragStartedEvent, new DragStartedEventHandler((_, _) =>
        {
            if (_item is null || _item.IsReadOnly)
                return;

            _item.IsDragging = true;
            DragStarted?.Invoke(this, EventArgs.Empty);
        }), handledEventsToo: true);

        FanSlider.AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler((_, _) =>
        {
            if (_item is null || _item.IsReadOnly)
                return;

            _item.IsDragging = false;
            DragEnded?.Invoke(this, EventArgs.Empty);
            EmitSpeed(immediate: true);
        }), handledEventsToo: true);

        FanSlider.PreviewMouseLeftButtonUp += (_, _) =>
        {
            if (_item is null || _item.IsReadOnly)
                return;

            _item.IsDragging = false;
            DragEnded?.Invoke(this, EventArgs.Empty);
            EmitSpeed(immediate: true);
        };
    }

    public void UpdateVisuals()
    {
        if (_item is null)
            return;

        if (_item.IsReadOnly)
        {
            PercentText.Text = _item.CurrentPercent.HasValue
                ? $"{SnapPercent(_item.CurrentPercent.Value)} %"
                : "— %";

            if (!_item.IsDragging)
            {
                _updatingUi = true;
                FanSlider.Value = _item.CurrentPercent ?? _item.SliderValue;
                _updatingUi = false;
            }

            FanSlider.IsEnabled = false;
            AutoButton.Visibility = Visibility.Collapsed;
            RenameButton.IsEnabled = true;

            ModeBadge.Background = Brush("#1A2834");
            ModeText.Foreground = Brush("#9BB0C2");
            ModeText.Text = "LECTURE SEULE";
            PanelBorder.BorderBrush = Brush("#2A3A48");
            return;
        }

        FanSlider.IsEnabled = true;
        AutoButton.Visibility = Visibility.Visible;

        PercentText.Text = _item.IsAuto
            ? (_item.CurrentPercent.HasValue ? $"~{SnapPercent(_item.CurrentPercent.Value)} %" : "AUTO")
            : (_item.CurrentPercent.HasValue ? $"{SnapPercent(_item.CurrentPercent.Value)} %" : "— %");

        if (!_item.IsDragging)
        {
            _updatingUi = true;
            FanSlider.Value = _item.SliderValue;
            _updatingUi = false;
        }

        if (_item.IsAuto)
        {
            ModeBadge.Background = Brush("#2EE6A8");
            ModeText.Foreground = Brush("#0A0E12");
            ModeText.Text = "AUTO";
            PanelBorder.BorderBrush = Brush("#2EE6A8");
            AutoButton.Tag = "Active";
            AutoButton.Content = "Auto ●";
        }
        else
        {
            ModeBadge.Background = Brush("#2A3A48");
            ModeText.Foreground = Brush("#7B8C9C");
            ModeText.Text = "MANUEL";
            PanelBorder.BorderBrush = Brush("#1E2A36");
            AutoButton.Tag = null;
            AutoButton.Content = "Auto";
        }
    }

    private void FanSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updatingUi || _item is null || _item.IsReadOnly)
            return;

        var snapped = SnapPercent(e.NewValue);
        _item.SliderValue = snapped;
        PercentText.Text = $"{snapped} %";
        EmitSpeed(immediate: false);
    }

    private void EmitSpeed(bool immediate)
    {
        if (_item is null || _item.IsReadOnly)
            return;

        SpeedChanged?.Invoke(this, new FanSpeedEventArgs(_item.SensorId, SnapPercent(_item.SliderValue), immediate));
    }

    private void Auto_Click(object sender, RoutedEventArgs e)
    {
        if (_item?.IsReadOnly == true)
            return;

        AutoRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Rename_Click(object sender, RoutedEventArgs e) =>
        RenameRequested?.Invoke(this, EventArgs.Empty);

    private static int SnapPercent(double value) =>
        (int)(Math.Round(Math.Clamp(value, 0, 100) / 10.0) * 10);

    private static SolidColorBrush Brush(string hex)
    {
        var b = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        b.Freeze();
        return b;
    }
}

public sealed class FanSpeedEventArgs : EventArgs
{
    public FanSpeedEventArgs(string sensorId, float percent, bool immediate)
    {
        SensorId = sensorId;
        Percent = percent;
        Immediate = immediate;
    }

    public string SensorId { get; }
    public float Percent { get; }
    public bool Immediate { get; }
}
