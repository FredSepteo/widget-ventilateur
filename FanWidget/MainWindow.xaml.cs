using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using FanWidget.Models;
using FanWidget.Services;
using FanWidget.Views;

namespace FanWidget;

public partial class MainWindow : Window
{
    private const int SingleColumnWidth = 360;
    private const int DualColumnWidth = 720;
    private const int TwoColumnTileThreshold = 4;
    private const int P100LinkedMinFanPercent = 30;

    private static readonly SolidColorBrush AutoBadgeBg = BrushFrom("#2EE6A8");
    private static readonly SolidColorBrush AutoBadgeFg = BrushFrom("#0A0E12");
    private static readonly SolidColorBrush ManualBadgeBg = BrushFrom("#2A3A48");
    private static readonly SolidColorBrush ManualBadgeFg = BrushFrom("#7B8C9C");

    private readonly FanControlService _fanService = new();
    private readonly GpuP100Service _p100Service = new();
    private readonly FanLabelStore _labelStore = new();
    private readonly FanVisibilityStore _visibilityStore = new();
    private readonly WidgetUiStore _uiStore = new();
    private readonly ProfileStore _profileStore = new();
    private readonly List<FanDisplayItem> _fans = [];
    private readonly Dictionary<string, FanRowControl> _rowsById = [];
    private readonly Dictionary<string, float> _pendingSpeeds = [];
    private readonly DispatcherTimer _refreshTimer;
    private readonly DispatcherTimer _applyTimer;
    private readonly DispatcherTimer _p100StateTimer;

    private TrayService? _tray;
    private bool _exitRequested;
    private bool _balloonShown;
    private bool _startupProfileApplied;
    private int _p100PollInFlight;

    public MainWindow()
    {
        InitializeComponent();

        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _refreshTimer.Tick += (_, _) => RefreshReadings();

        _applyTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(180) };
        _applyTimer.Tick += (_, _) => FlushPendingSpeeds();

        _p100StateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _p100StateTimer.Tick += (_, _) => PollP100HardwareState();

        Loaded += OnLoaded;
        ContentRendered += (_, _) => UpdateTilesLayout();
        Closing += OnClosing;
        StateChanged += OnStateChanged;
        Closed += OnClosed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _tray = new TrayService(ShowFromTray, ExitFromTray);

        if (StartupService.IsEnabled())
            StartupService.UpdateExecutablePath();
        else
            StartupService.SetEnabled(true);

        _labelStore.Load();
        _visibilityStore.Load();
        _uiStore.Load();
        _profileStore.Load();

        if (!_fanService.Initialize(out var error))
        {
            var board = _fanService.Motherboard;
            var boardLine = string.IsNullOrWhiteSpace(board.Product)
                ? string.Empty
                : $"\n{board.Manufacturer} {board.Product}";

            StatusText.Text = (error ?? "Erreur inconnue.") + boardLine;
            return;
        }

        BuildFanRows();
        RefreshProfileBadges();
        ApplyFanVisibility();
        ApplyP100TileVisibility();
        UpdateStatusText();
        RefreshP100Ui();
        ApplyStartupProfileIfNeeded();
        ApplyP100FanConstraints();
        RefreshReadings();
        _refreshTimer.Start();
        _p100StateTimer.Start();
        UpdateTilesLayout();
        Dispatcher.BeginInvoke(UpdateTilesLayout, DispatcherPriority.Loaded);
        Dispatcher.BeginInvoke(UpdateTilesLayout, DispatcherPriority.Render);
    }

    private void ApplyStartupProfileIfNeeded()
    {
        if (_startupProfileApplied)
            return;

        var startup = _profileStore.GetStartupProfile();
        if (startup is null)
            return;

        _startupProfileApplied = true;
        ApplyProfile(startup, notify: false);
        StatusText.Text = $"Profil « {startup.Name} » appliqué au démarrage";
    }

    private void BuildFanRows()
    {
        foreach (var row in _rowsById.Values)
        {
            if (row.Parent is System.Windows.Controls.Panel panel)
                panel.Children.Remove(row);
        }

        _fans.Clear();
        _rowsById.Clear();

        foreach (var entry in _fanService.GetOrderedControls())
        {
            var item = new FanDisplayItem
            {
                SensorId = entry.Id,
                SortIndex = ExtractIndex(entry.Id),
                HardwareName = entry.Name,
                UserLabel = _labelStore.GetLabel(entry.Id, entry.Name),
                IsReadOnly = FanControlService.IsCpuFan(entry.Name, entry.Id),
            };

            _fans.Add(item);

            var row = new FanRowControl { DataContext = item };
            row.SpeedChanged += OnFanSpeedChanged;
            row.AutoRequested += (_, _) => OnFanAuto(item);
            row.RenameRequested += (_, _) => OnFanRename(item);

            _rowsById[item.SensorId] = row;
        }

        UpdateTilesLayout();
    }

    private void ApplyFanVisibility()
    {
        foreach (var item in _fans)
        {
            if (_rowsById.TryGetValue(item.SensorId, out var row))
            {
                row.Visibility = _visibilityStore.IsVisible(item.SensorId)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        UpdateStatusText();
        UpdateTilesLayout();
    }

    private void UpdateTilesLayout()
    {
        var orderedTiles = new List<FrameworkElement> { P100Tile };
        orderedTiles.AddRange(_fans.Select(fan => _rowsById[fan.SensorId]));

        var visibleTiles = orderedTiles
            .Where(tile => tile.Visibility == Visibility.Visible)
            .ToList();

        var twoColumns = visibleTiles.Count > TwoColumnTileThreshold;

        Width = twoColumns ? DualColumnWidth : SingleColumnWidth;
        MinWidth = twoColumns ? DualColumnWidth - 20 : 340;
        TilesGapColumnDef.Width = twoColumns ? new GridLength(8) : new GridLength(0);
        TilesRightColumnDef.Width = twoColumns ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

        TilesLeftColumn.Children.Clear();
        TilesRightColumn.Children.Clear();

        for (var i = 0; i < visibleTiles.Count; i++)
        {
            var target = twoColumns && i % 2 == 1 ? TilesRightColumn : TilesLeftColumn;
            target.Children.Add(visibleTiles[i]);
        }
    }

    private void UpdateStatusText()
    {
        if (_fans.Count == 0)
            return;

        var visible = _fans.Count(f => _visibilityStore.IsVisible(f.SensorId));
        var active = _profileStore.GetById(_profileStore.ActiveProfileId);
        var profilePart = active is not null ? $" · {active.Name}" : string.Empty;
        StatusText.Text = $"{_fanService.Motherboard.DisplayName} · {visible}/{_fans.Count} ventilateur(s){profilePart}";
    }

    private void RefreshProfileBadges()
    {
        ProfileBadgesPanel.Children.Clear();

        if (_profileStore.Profiles.Count == 0)
        {
            ProfileEmptyHint.Visibility = Visibility.Visible;
            return;
        }

        ProfileEmptyHint.Visibility = Visibility.Collapsed;
        var activeId = _profileStore.ActiveProfileId;
        var startupId = _profileStore.StartupProfileId;
        var badgeStyle = (Style)FindResource("AirflowProfileBadge");

        foreach (var profile in _profileStore.Profiles)
        {
            var isActive = string.Equals(profile.Id, activeId, StringComparison.OrdinalIgnoreCase);
            var isStartup = string.Equals(profile.Id, startupId, StringComparison.OrdinalIgnoreCase);
            var label = isStartup ? $"{profile.Name} ★" : profile.Name;

            var badge = new System.Windows.Controls.Button
            {
                Content = label,
                Tag = isActive ? "Active" : null,
                Style = badgeStyle,
                Margin = new Thickness(0, 0, 6, 6),
                ToolTip = isStartup ? $"{profile.Name} — profil de démarrage" : profile.Name,
            };

            var captured = profile;
            badge.Click += (_, _) => ApplyProfile(captured, notify: false);

            ProfileBadgesPanel.Children.Add(badge);
        }
    }

    private void Profiles_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ProfileManagerWindow(
            _profileStore,
            _fans,
            CaptureCurrentProfile,
            profile => ApplyProfile(profile, notify: true),
            RefreshProfileBadges)
        { Owner = this };

        dialog.ShowDialog();
    }

    private UserProfile CaptureCurrentProfile()
    {
        FlushPendingSpeeds();
        RefreshReadings();

        _p100Service.Refresh();

        var settings = _fans.Select(fan =>
        {
            _fanService.UpdateReading(fan);
            return new FanProfileSetting
            {
                SensorId = fan.SensorId,
                IsAuto = fan.IsReadOnly || !fan.IsManual,
                ManualPercent = SnapPercent(fan.SliderValue),
            };
        }).ToList();

        return new UserProfile
        {
            Name = "Profil",
            P100Enabled = _p100Service.IsAvailable && _p100Service.IsEnabled,
            Fans = settings,
        };
    }

    private void ApplyProfile(UserProfile profile, bool notify)
    {
        _pendingSpeeds.Clear();
        _applyTimer.Stop();

        var settingsById = profile.Fans.ToDictionary(f => f.SensorId, StringComparer.OrdinalIgnoreCase);

        foreach (var fan in _fans)
        {
            if (!settingsById.TryGetValue(fan.SensorId, out var setting))
                continue;

            if (_fanService.IsReadOnly(fan.SensorId))
                continue;

            if (setting.IsAuto)
            {
                _fanService.SetAuto(fan.SensorId);
                fan.IsManual = false;
            }
            else
            {
                var percent = SnapPercent(setting.ManualPercent);
                _fanService.SetFanSpeed(fan.SensorId, percent);
                fan.IsManual = true;
                fan.SliderValue = percent;
            }
        }

        if (_p100Service.Refresh() && _p100Service.IsAvailable && _p100Service.IsEnabled != profile.P100Enabled)
            _p100Service.SetEnabled(profile.P100Enabled, out _);

        _profileStore.SetActiveProfile(profile.Id);
        RefreshProfileBadges();
        RefreshP100Ui();
        ApplyP100FanConstraints();
        RefreshReadings();

        if (notify)
            StatusText.Text = $"Profil « {profile.Name} » appliqué";
        else
            UpdateStatusText();
    }

    private void ApplyP100TileVisibility()
    {
        P100Tile.Visibility = _uiStore.ShowP100Tile ? Visibility.Visible : Visibility.Collapsed;
        UpdateTilesLayout();
    }

    private void Options_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new FanOptionsWindow(_fans, _visibilityStore, _uiStore, _fanService.SysFan1Id) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            ApplyFanVisibility();
            ApplyP100TileVisibility();
            RefreshP100Ui();
            ApplyP100FanConstraints();
        }
    }

    private string GetP100LinkedFanId() =>
        _uiStore.ResolveP100LinkedFanId(_fanService.SysFan1Id);

    private string GetP100LinkedFanLabel()
    {
        var linkedId = GetP100LinkedFanId();
        var fan = _fans.FirstOrDefault(f => string.Equals(f.SensorId, linkedId, StringComparison.OrdinalIgnoreCase));
        return fan?.UserLabel ?? "ventilateur lié";
    }

    private bool IsP100LinkedFan(string sensorId) =>
        !string.IsNullOrEmpty(GetP100LinkedFanId()) &&
        string.Equals(sensorId, GetP100LinkedFanId(), StringComparison.OrdinalIgnoreCase);

    private void ApplyP100FanConstraints()
    {
        var linkedId = GetP100LinkedFanId();

        foreach (var fan in _fans)
        {
            var isLinked = !string.IsNullOrEmpty(linkedId) &&
                string.Equals(fan.SensorId, linkedId, StringComparison.OrdinalIgnoreCase);

            fan.MinSliderPercent = isLinked ? P100LinkedMinFanPercent : 0;

            if (isLinked && !fan.IsReadOnly)
            {
                if (fan.IsAuto)
                {
                    fan.IsManual = true;
                    _fanService.SetFanSpeed(fan.SensorId, Math.Max(P100LinkedMinFanPercent, SnapPercent(fan.SliderValue)));
                }

                if (fan.SliderValue < P100LinkedMinFanPercent)
                {
                    fan.SliderValue = P100LinkedMinFanPercent;
                    _pendingSpeeds.Remove(fan.SensorId);
                    _fanService.SetFanSpeed(fan.SensorId, P100LinkedMinFanPercent);
                }
            }

            if (_rowsById.TryGetValue(fan.SensorId, out var row))
                row.UpdateVisuals();
        }
    }

    private void OnFanSpeedChanged(object? sender, FanSpeedEventArgs e)
    {
        if (_fanService.IsReadOnly(e.SensorId))
            return;

        var snapped = SnapPercent(e.Percent);
        if (IsP100LinkedFan(e.SensorId))
            snapped = Math.Max(snapped, P100LinkedMinFanPercent);

        if (_rowsById.TryGetValue(e.SensorId, out var row) && row.Item is not null)
        {
            row.Item.IsManual = true;
            if (row.Item.SliderValue != snapped)
            {
                row.Item.SliderValue = snapped;
                row.UpdateVisuals();
            }
            else if (e.Immediate)
            {
                row.UpdateVisuals();
            }
        }

        _pendingSpeeds[e.SensorId] = snapped;

        if (e.Immediate)
        {
            FlushPendingSpeeds();
            return;
        }

        _applyTimer.Stop();
        _applyTimer.Start();
    }

    private void OnFanAuto(FanDisplayItem item)
    {
        if (item.IsReadOnly)
            return;

        if (IsP100LinkedFan(item.SensorId))
        {
            StatusText.Text = $"Mode Auto indisponible — {GetP100LinkedFanLabel()} (plancher {P100LinkedMinFanPercent} %)";
            return;
        }

        item.IsDragging = false;
        _pendingSpeeds.Remove(item.SensorId);
        _fanService.SetAuto(item.SensorId);
        RefreshReadings();
    }

    private void OnFanRename(FanDisplayItem item)
    {
        var dialog = new RenameFanDialog(item.HardwareName, item.UserLabel) { Owner = this };
        if (dialog.ShowDialog() != true)
            return;

        item.UserLabel = dialog.ResultLabel;
        _labelStore.SetLabel(item.SensorId, dialog.ResultLabel);
        UpdateStatusText();
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_exitRequested)
            return;

        e.Cancel = true;
        HideToTray();
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
            HideToTray();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _refreshTimer.Stop();
        _applyTimer.Stop();
        _p100StateTimer.Stop();
        FlushPendingSpeeds();
        _tray?.Dispose();
        _fanService.Dispose();
    }

    private void HideToTray()
    {
        Hide();
        ShowInTaskbar = false;

        if (!_balloonShown)
        {
            _balloonShown = true;
            _tray?.ShowBalloon("FanWidget", "Réduit près de l'horloge. Double-clic pour réouvrir.");
        }
    }

    private void ShowFromTray()
    {
        ShowInTaskbar = true;
        Show();
        WindowState = WindowState.Normal;
        Activate();
        PollP100HardwareState();
    }

    private void ExitFromTray()
    {
        _exitRequested = true;
        Close();
    }

    private void RefreshReadings()
    {
        foreach (var item in _fans)
        {
            if (item.IsDragging)
                continue;

            _fanService.UpdateReading(item);

            if (!_pendingSpeeds.ContainsKey(item.SensorId)
                && !item.IsManual
                && item.CurrentPercent.HasValue)
                item.SliderValue = SnapPercent(item.CurrentPercent.Value);

            if (_rowsById.TryGetValue(item.SensorId, out var row))
                row.UpdateVisuals();
        }
    }

    private void FlushPendingSpeeds()
    {
        _applyTimer.Stop();

        foreach (var (sensorId, percent) in _pendingSpeeds.ToList())
        {
            _fanService.SetFanSpeed(sensorId, SnapPercent(percent));
            _pendingSpeeds.Remove(sensorId);
        }
    }

    private void P100Toggle_Click(object sender, RoutedEventArgs e)
    {
        P100ToggleButton.IsEnabled = false;

        try
        {
            _p100Service.Refresh();
            if (!_p100Service.IsAvailable)
            {
                StatusText.Text = "GPU P100 introuvable.";
                return;
            }

            var turnOn = !_p100Service.IsEnabled;
            if (!_p100Service.SetEnabled(turnOn, out var error))
            {
                StatusText.Text = error ?? "Échec bascule P100.";
                return;
            }

            var fanPercent = turnOn ? 100 : P100LinkedMinFanPercent;
            var linkedFanId = GetP100LinkedFanId();
            var linkedFanLabel = GetP100LinkedFanLabel();
            if (!string.IsNullOrEmpty(linkedFanId))
            {
                _pendingSpeeds.Remove(linkedFanId);
                _fanService.SetFanSpeed(linkedFanId, fanPercent);

                if (_rowsById.TryGetValue(linkedFanId, out var row) && row.Item is not null)
                {
                    row.Item.SliderValue = fanPercent;
                    row.Item.IsManual = true;
                    row.UpdateVisuals();
                }
            }

            StatusText.Text = turnOn
                ? $"P100 activé · {linkedFanLabel} → 100 %"
                : $"P100 désactivé · {linkedFanLabel} → {P100LinkedMinFanPercent} %";

            RefreshP100Ui();
            ApplyP100FanConstraints();
        }
        finally
        {
            P100ToggleButton.IsEnabled = true;
        }
    }

    private void PollP100HardwareState()
    {
        if (!_uiStore.ShowP100Tile)
            return;

        if (Interlocked.CompareExchange(ref _p100PollInFlight, 1, 0) != 0)
            return;

        Task.Run(() =>
        {
            try
            {
                var wasEnabled = _p100Service.IsEnabled;
                var wasAvailable = _p100Service.IsAvailable;
                _p100Service.RefreshLightweight();

                if (_p100Service.IsEnabled == wasEnabled && _p100Service.IsAvailable == wasAvailable)
                    return;

                Dispatcher.BeginInvoke(() =>
                {
                    RefreshP100Ui(refreshHardware: false);
                    ApplyP100FanConstraints();
                }, DispatcherPriority.Background);
            }
            finally
            {
                Interlocked.Exchange(ref _p100PollInFlight, 0);
            }
        });
    }

    private void RefreshP100Ui(bool refreshHardware = true)
    {
        if (refreshHardware)
            _p100Service.Refresh();

        if (!_p100Service.IsAvailable)
        {
            P100StatusText.Text = "ABSENT";
            P100StatusBadge.Background = ManualBadgeBg;
            P100StatusText.Foreground = ManualBadgeFg;
            P100ToggleButton.IsEnabled = false;
            P100ToggleButton.Content = "P100 —";
            P100ToggleButton.Tag = null;
            P100ToggleButton.Foreground = ManualBadgeFg;
            return;
        }

        P100ToggleButton.IsEnabled = true;

        var linkedFanLabel = GetP100LinkedFanLabel();

        if (_p100Service.IsEnabled)
        {
            P100StatusText.Text = "ON";
            P100StatusBadge.Background = AutoBadgeBg;
            P100StatusText.Foreground = AutoBadgeFg;
            P100ToggleButton.Content = "P100 OFF";
            P100ToggleButton.Tag = "On";
            P100ToggleButton.Foreground = AutoBadgeBg;
            P100ToggleButton.ToolTip = $"Désactiver le GPU P100 ({linkedFanLabel} → {P100LinkedMinFanPercent} %)";
        }
        else
        {
            P100StatusText.Text = "OFF";
            P100StatusBadge.Background = ManualBadgeBg;
            P100StatusText.Foreground = ManualBadgeFg;
            P100ToggleButton.Content = "P100 ON";
            P100ToggleButton.Tag = null;
            P100ToggleButton.Foreground = ManualBadgeFg;
            P100ToggleButton.ToolTip = $"Activer le GPU P100 et {linkedFanLabel} à 100 %";
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) =>
        HideToTray();

    private void Close_Click(object sender, RoutedEventArgs e) =>
        HideToTray();

    private static int ExtractIndex(string identifier)
    {
        var lastSlash = identifier.LastIndexOf('/');
        if (lastSlash < 0 || lastSlash >= identifier.Length - 1)
            return -1;

        return int.TryParse(identifier[(lastSlash + 1)..], out var index) ? index : -1;
    }

    private static int SnapPercent(double value) =>
        (int)(Math.Round(Math.Clamp(value, 0, 100) / 10.0) * 10);

    private static SolidColorBrush BrushFrom(string hex)
    {
        var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        brush.Freeze();
        return brush;
    }
}
