using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FanWidget.Models;

namespace FanWidget.Views;

public partial class ProfileEditWindow : Window
{
    private readonly UserProfile _profile;
    private readonly IReadOnlyList<FanDisplayItem> _fans;
    private readonly List<FanEditRow> _rows = [];

    public UserProfile? ResultProfile { get; private set; }

    public ProfileEditWindow(UserProfile profile, IReadOnlyList<FanDisplayItem> fans)
    {
        InitializeComponent();
        _profile = profile;
        _fans = fans;

        NameBox.Text = profile.Name;
        P100CheckBox.IsChecked = profile.P100Enabled;

        BuildFanRows();
    }

    private void BuildFanRows()
    {
        FanSettingsPanel.Children.Clear();
        _rows.Clear();

        var settingsById = _profile.Fans.ToDictionary(f => f.SensorId, StringComparer.OrdinalIgnoreCase);

        foreach (var fan in _fans)
        {
            if (!settingsById.TryGetValue(fan.SensorId, out var setting))
            {
                setting = new FanProfileSetting
                {
                    SensorId = fan.SensorId,
                    IsAuto = true,
                    ManualPercent = 50,
                };
            }

            var row = CreateFanRow(fan, setting);
            _rows.Add(row);
            FanSettingsPanel.Children.Add(row.Panel);
        }
    }

    private FanEditRow CreateFanRow(FanDisplayItem fan, FanProfileSetting setting)
    {
        var panel = new Border
        {
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x0F, 0x14, 0x19)),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 0, 8),
            BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2A, 0x3A, 0x48)),
            BorderThickness = new Thickness(1),
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };

        var title = new TextBlock
        {
            Text = fan.UserLabel,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brush("#F0F4F8"),
            VerticalAlignment = VerticalAlignment.Center,
        };

        var subtitle = new TextBlock
        {
            Text = fan.IsReadOnly ? $"  ·  {fan.HardwareName} (lecture seule)" : $"  ·  {fan.HardwareName}",
            Foreground = Brush("#7B8C9C"),
            VerticalAlignment = VerticalAlignment.Center,
        };

        header.Children.Add(title);
        header.Children.Add(subtitle);
        Grid.SetRow(header, 0);
        grid.Children.Add(header);

        var controls = new Grid();
        controls.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        controls.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        controls.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var modeCombo = new System.Windows.Controls.ComboBox
        {
            Width = 100,
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center,
            IsEnabled = !fan.IsReadOnly,
            Style = (Style)System.Windows.Application.Current.FindResource("AirflowComboBox"),
        };
        modeCombo.Items.Add("Auto");
        modeCombo.Items.Add("Manuel");
        modeCombo.SelectedIndex = fan.IsReadOnly || setting.IsAuto ? 0 : 1;

        var slider = new System.Windows.Controls.Slider
        {
            Minimum = 0,
            Maximum = 100,
            TickFrequency = 10,
            IsSnapToTickEnabled = true,
            Value = setting.ManualPercent,
            VerticalAlignment = VerticalAlignment.Center,
            IsEnabled = !fan.IsReadOnly && modeCombo.SelectedIndex == 1,
            Style = (Style)System.Windows.Application.Current.FindResource("FanSlider"),
        };

        var percentLabel = new TextBlock
        {
            Text = $"{SnapPercent(slider.Value)} %",
            Foreground = Brush("#2EE6A8"),
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 40,
            TextAlignment = TextAlignment.Right,
        };

        modeCombo.SelectionChanged += (_, _) =>
        {
            var manual = modeCombo.SelectedIndex == 1;
            slider.IsEnabled = !fan.IsReadOnly && manual;
            percentLabel.Opacity = manual ? 1 : 0.4;
        };

        slider.ValueChanged += (_, _) =>
            percentLabel.Text = $"{SnapPercent(slider.Value)} %";

        if (fan.IsReadOnly)
        {
            modeCombo.SelectedIndex = 0;
            slider.IsEnabled = false;
            percentLabel.Opacity = 0.4;
        }

        Grid.SetColumn(modeCombo, 0);
        Grid.SetColumn(slider, 1);
        Grid.SetColumn(percentLabel, 2);
        controls.Children.Add(modeCombo);
        controls.Children.Add(slider);
        controls.Children.Add(percentLabel);

        Grid.SetRow(controls, 1);
        grid.Children.Add(controls);

        panel.Child = grid;

        return new FanEditRow(fan.SensorId, fan.IsReadOnly, panel, modeCombo, slider);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            System.Windows.MessageBox.Show(this, "Le nom du profil ne peut pas être vide.", "Profil",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _profile.Name = name;
        _profile.P100Enabled = P100CheckBox.IsChecked == true;
        _profile.Fans = _rows.Select(r => new FanProfileSetting
        {
            SensorId = r.SensorId,
            IsAuto = r.IsReadOnly || r.ModeCombo.SelectedIndex == 0,
            ManualPercent = SnapPercent(r.Slider.Value),
        }).ToList();

        ResultProfile = _profile;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private static int SnapPercent(double value) =>
        (int)(Math.Round(Math.Clamp(value, 0, 100) / 10.0) * 10);

    private static SolidColorBrush Brush(string hex)
    {
        var b = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        b.Freeze();
        return b;
    }

    private sealed record FanEditRow(
        string SensorId,
        bool IsReadOnly,
        Border Panel,
        System.Windows.Controls.ComboBox ModeCombo,
        System.Windows.Controls.Slider Slider);
}
