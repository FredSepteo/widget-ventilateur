using System.IO;
using System.Text.Json;

namespace FanWidget.Services;

public sealed class WidgetUiStore
{
    private bool _showP100Tile = true;
    private bool _p100EnabledAtStartup = true;
    private string? _p100LinkedFanId;

    private string StorePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FanWidget",
            "ui-settings.json");

    public bool ShowP100Tile => _showP100Tile;

    public bool P100EnabledAtStartup => _p100EnabledAtStartup;

    public string? P100LinkedFanId =>
        string.IsNullOrWhiteSpace(_p100LinkedFanId) ? null : _p100LinkedFanId;

    public string ResolveP100LinkedFanId(string? sysFan1Fallback)
    {
        if (!string.IsNullOrWhiteSpace(_p100LinkedFanId))
            return _p100LinkedFanId;

        return sysFan1Fallback ?? string.Empty;
    }

    public void Load()
    {
        _showP100Tile = true;
        _p100EnabledAtStartup = true;
        _p100LinkedFanId = null;
        try
        {
            if (!File.Exists(StorePath))
                return;

            var json = File.ReadAllText(StorePath);
            var data = JsonSerializer.Deserialize<UiSettings>(json);
            if (data is null)
                return;

            _showP100Tile = data.ShowP100Tile;
            _p100EnabledAtStartup = data.P100EnabledAtStartup ?? true;
            _p100LinkedFanId = string.IsNullOrWhiteSpace(data.P100LinkedFanId)
                ? null
                : data.P100LinkedFanId.Trim();
        }
        catch
        {
            _showP100Tile = true;
            _p100EnabledAtStartup = true;
            _p100LinkedFanId = null;
        }
    }

    public void SetShowP100Tile(bool visible)
    {
        _showP100Tile = visible;
        Save();
    }

    public void SetP100EnabledAtStartup(bool enabled)
    {
        _p100EnabledAtStartup = enabled;
        Save();
    }

    public void SetP100LinkedFanId(string? sensorId)
    {
        _p100LinkedFanId = string.IsNullOrWhiteSpace(sensorId) ? null : sensorId.Trim();
        Save();
    }

    private void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(StorePath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(new UiSettings
            {
                ShowP100Tile = _showP100Tile,
                P100EnabledAtStartup = _p100EnabledAtStartup,
                P100LinkedFanId = _p100LinkedFanId,
            }, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(StorePath, json);
        }
        catch
        {
            // non-critical
        }
    }

    private sealed class UiSettings
    {
        public bool ShowP100Tile { get; set; } = true;
        public bool? P100EnabledAtStartup { get; set; }
        public string? P100LinkedFanId { get; set; }
    }
}
