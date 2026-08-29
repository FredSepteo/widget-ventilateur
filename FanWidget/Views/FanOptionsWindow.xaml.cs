using System.Windows;
using System.Windows.Controls;
using FanWidget.Models;
using FanWidget.Services;

namespace FanWidget.Views;

public partial class FanOptionsWindow : Window
{
    private readonly FanVisibilityStore _store;
    private readonly WidgetUiStore _uiStore;
    private readonly List<System.Windows.Controls.CheckBox> _checkboxes = [];

    public FanOptionsWindow(
        IReadOnlyList<FanDisplayItem> fans,
        FanVisibilityStore store,
        WidgetUiStore uiStore,
        string? sysFan1FallbackId)
    {
        InitializeComponent();
        _store = store;
        _uiStore = uiStore;

        StartupToggle.IsChecked = StartupService.IsEnabled();
        P100TileToggle.IsChecked = uiStore.ShowP100Tile;
        P100StartupToggle.IsChecked = uiStore.P100EnabledAtStartup;
        LoadP100LinkedFanCombo(fans, sysFan1FallbackId);

        foreach (var fan in fans)
        {
            var cb = new System.Windows.Controls.CheckBox
            {
                Tag = fan.SensorId,
                IsChecked = store.IsVisible(fan.SensorId),
                Margin = new Thickness(4, 6, 4, 6),
                Style = (Style)System.Windows.Application.Current.FindResource("AirflowCheckBox"),
            };

            var label = new TextBlock { TextWrapping = TextWrapping.Wrap };
            label.Inlines.Add(new System.Windows.Documents.Run(fan.UserLabel) { FontWeight = FontWeights.SemiBold });
            label.Inlines.Add(new System.Windows.Documents.Run($"  ·  {fan.HardwareName}") { Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#7B8C9C")! });
            cb.Content = label;

            _checkboxes.Add(cb);
            FanListPanel.Children.Add(cb);
        }
    }

    private void LoadP100LinkedFanCombo(IReadOnlyList<FanDisplayItem> fans, string? sysFan1FallbackId)
    {
        var controllableFans = fans.Where(f => !f.IsReadOnly).ToList();
        var items = controllableFans
            .Select(f => new FanOption(f.SensorId, $"{f.UserLabel}  ·  {f.HardwareName}"))
            .ToList();

        P100LinkedFanCombo.ItemsSource = items;
        P100LinkedFanCombo.DisplayMemberPath = nameof(FanOption.Label);

        var currentId = _uiStore.P100LinkedFanId ?? sysFan1FallbackId;
        P100LinkedFanCombo.SelectedItem = items.FirstOrDefault(i => i.Id == currentId)
            ?? items.FirstOrDefault();
    }

    private void ShowAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var cb in _checkboxes)
            cb.IsChecked = true;
    }

    private void HideAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var cb in _checkboxes)
            cb.IsChecked = false;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var visibility = new Dictionary<string, bool>();
        foreach (var cb in _checkboxes)
        {
            if (cb.Tag is not string sensorId)
                continue;

            visibility[sensorId] = cb.IsChecked == true;
        }

        _store.Apply(visibility);
        var startupEnabled = StartupToggle.IsChecked == true;
        if (startupEnabled)
            StartupService.SetEnabled(true);
        else
            StartupService.SetEnabled(false);

        _uiStore.SetShowP100Tile(P100TileToggle.IsChecked == true);
        _uiStore.SetP100EnabledAtStartup(P100StartupToggle.IsChecked == true);
        _uiStore.SetP100LinkedFanId((P100LinkedFanCombo.SelectedItem as FanOption)?.Id);

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private sealed record FanOption(string Id, string Label);
}
