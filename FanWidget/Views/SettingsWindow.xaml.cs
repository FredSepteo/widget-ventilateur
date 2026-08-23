using System.Windows;
using FanWidget.Services;
using MessageBox = System.Windows.MessageBox;

namespace FanWidget.Views;

public partial class SettingsWindow : Window
{
    private readonly FanControlService _fanService;

    public SettingsWindow(FanControlService fanService)
    {
        InitializeComponent();
        _fanService = fanService;
        LoadCombos();
    }

    private void LoadCombos()
    {
        _fanService.RefreshControls();
        var fans = _fanService.AvailableControls;

        var items = fans.Select(f => new FanOption(f.Id, f.Name)).ToList();
        items.Insert(0, new FanOption(string.Empty, "— Non assigné —"));

        Fan1Combo.ItemsSource = items;
        Fan2Combo.ItemsSource = items.ToList();

        Fan1Combo.DisplayMemberPath = nameof(FanOption.Label);
        Fan2Combo.DisplayMemberPath = nameof(FanOption.Label);

        Fan1Combo.SelectedItem = items.FirstOrDefault(i => i.Id == _fanService.SysFan1Id) ?? items[0];
        Fan2Combo.SelectedItem = items.FirstOrDefault(i => i.Id == _fanService.SysFan2Id) ?? items[0];
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var fan1 = (Fan1Combo.SelectedItem as FanOption)?.Id;
        var fan2 = (Fan2Combo.SelectedItem as FanOption)?.Id;

        if (!string.IsNullOrEmpty(fan1) && fan1 == fan2)
        {
            MessageBox.Show(this, "Sys Fan 1 et Sys Fan 2 doivent être différents.", "Configuration invalide",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _fanService.MapFans(
            string.IsNullOrEmpty(fan1) ? null : fan1,
            string.IsNullOrEmpty(fan2) ? null : fan2);

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
