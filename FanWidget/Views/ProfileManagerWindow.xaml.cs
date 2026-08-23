using System.Windows;
using System.Windows.Controls;
using FanWidget.Models;
using FanWidget.Services;

namespace FanWidget.Views;

public partial class ProfileManagerWindow : Window
{
    private readonly ProfileStore _store;
    private readonly IReadOnlyList<FanDisplayItem> _fans;
    private readonly Func<UserProfile> _captureCurrent;
    private readonly Action<UserProfile> _applyProfile;
    private readonly Action _refreshProfilesUi;

    public ProfileManagerWindow(
        ProfileStore store,
        IReadOnlyList<FanDisplayItem> fans,
        Func<UserProfile> captureCurrent,
        Action<UserProfile> applyProfile,
        Action refreshProfilesUi)
    {
        InitializeComponent();
        _store = store;
        _fans = fans;
        _captureCurrent = captureCurrent;
        _applyProfile = applyProfile;
        _refreshProfilesUi = refreshProfilesUi;

        Loaded += (_, _) => RefreshList();
    }

    private void RefreshList()
    {
        var selectedId = (ProfileList.SelectedItem as ProfileListItem)?.Profile.Id;
        var items = _store.Profiles
            .Select(p => new ProfileListItem(p, string.Equals(p.Id, _store.StartupProfileId, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        ProfileList.ItemsSource = items;

        if (selectedId is not null)
        {
            var match = items.FirstOrDefault(i => i.Profile.Id == selectedId);
            if (match is not null)
                ProfileList.SelectedItem = match;
        }

        UpdateButtons();
    }

    private UserProfile? SelectedProfile =>
        (ProfileList.SelectedItem as ProfileListItem)?.Profile;

    private void UpdateButtons()
    {
        var hasSelection = SelectedProfile is not null;
        EditButton.IsEnabled = hasSelection;
        ApplyButton.IsEnabled = hasSelection;
        DeleteButton.IsEnabled = hasSelection;
        StartupButton.IsEnabled = hasSelection;

        if (SelectedProfile is not null)
        {
            var isStartup = string.Equals(_store.StartupProfileId, SelectedProfile.Id, StringComparison.OrdinalIgnoreCase);
            StartupButton.Content = isStartup ? "★ Profil de démarrage" : "Définir au démarrage";
            StartupButton.Tag = isStartup ? "Active" : null;
        }
        else
        {
            StartupButton.Content = "Profil de démarrage";
            StartupButton.Tag = null;
        }
    }

    private void ProfileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        UpdateButtons();
    }

    private void New_Click(object sender, RoutedEventArgs e)
    {
        var profile = CreateDefaultProfile("Nouveau profil");
        var dialog = new ProfileEditWindow(profile, _fans) { Owner = this };
        if (dialog.ShowDialog() != true || dialog.ResultProfile is null)
            return;

        _store.Add(dialog.ResultProfile);
        RefreshList();
        SelectProfileById(dialog.ResultProfile.Id);
        _refreshProfilesUi();
    }

    private void SaveCurrent_Click(object sender, RoutedEventArgs e)
    {
        var nameDialog = new SaveProfileDialog { Owner = this };
        if (nameDialog.ShowDialog() != true)
            return;

        var profile = _captureCurrent();
        profile.Name = nameDialog.ResultName;
        _store.Add(profile);
        RefreshList();
        SelectProfileById(profile.Id);
        _refreshProfilesUi();
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedProfile is null)
            return;

        var clone = CloneProfile(SelectedProfile);
        var dialog = new ProfileEditWindow(clone, _fans) { Owner = this };
        if (dialog.ShowDialog() != true || dialog.ResultProfile is null)
            return;

        _store.Update(dialog.ResultProfile);
        RefreshList();
        SelectProfileById(dialog.ResultProfile.Id);
        _refreshProfilesUi();
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedProfile is null)
            return;

        _applyProfile(SelectedProfile);
        System.Windows.MessageBox.Show(this, $"Profil « {SelectedProfile.Name} » appliqué.", "Profils",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Startup_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedProfile is null)
            return;

        var profileId = SelectedProfile.Id;
        var isStartup = string.Equals(_store.StartupProfileId, profileId, StringComparison.OrdinalIgnoreCase);
        _store.SetStartupProfile(isStartup ? null : profileId);
        RefreshList();
        SelectProfileById(profileId);
        _refreshProfilesUi();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedProfile is null)
            return;

        var confirm = System.Windows.MessageBox.Show(this,
            $"Supprimer le profil « {SelectedProfile.Name} » ?",
            "Profils",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
            return;

        _store.Delete(SelectedProfile.Id);
        RefreshList();
        _refreshProfilesUi();
    }

    private void SelectProfileById(string profileId)
    {
        if (ProfileList.ItemsSource is not IEnumerable<ProfileListItem> items)
            return;

        ProfileList.SelectedItem = items.FirstOrDefault(i => i.Profile.Id == profileId);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private UserProfile CreateDefaultProfile(string name)
    {
        return new UserProfile
        {
            Name = name,
            P100Enabled = false,
            Fans = _fans.Select(f => new FanProfileSetting
            {
                SensorId = f.SensorId,
                IsAuto = !f.IsReadOnly,
                ManualPercent = 50,
            }).ToList(),
        };
    }

    private static UserProfile CloneProfile(UserProfile source) =>
        new()
        {
            Id = source.Id,
            Name = source.Name,
            P100Enabled = source.P100Enabled,
            Fans = source.Fans.Select(f => new FanProfileSetting
            {
                SensorId = f.SensorId,
                IsAuto = f.IsAuto,
                ManualPercent = f.ManualPercent,
            }).ToList(),
        };
}

internal sealed class ProfileListItem
{
    public ProfileListItem(UserProfile profile, bool isStartup)
    {
        Profile = profile;
        IsStartup = isStartup;
    }

    public UserProfile Profile { get; }
    public bool IsStartup { get; }
    public string DisplayName => IsStartup ? $"{Profile.Name}  ★ démarrage" : Profile.Name;
}
