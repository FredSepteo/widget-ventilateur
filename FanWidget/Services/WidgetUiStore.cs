using System.IO;
using System.Text.Json;

namespace FanWidget.Services;

public sealed class WidgetUiStore
{
    private bool _showP100Tile = true;

    private string StorePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FanWidget",
            "ui-settings.json");

    public bool ShowP100Tile => _showP100Tile;

    public void Load()
    {
        _showP100Tile = true;
        try
        {
            if (!File.Exists(StorePath))
                return;

            var json = File.ReadAllText(StorePath);
            var data = JsonSerializer.Deserialize<UiSettings>(json);
            if (data is not null)
                _showP100Tile = data.ShowP100Tile;
        }
        catch
        {
            _showP100Tile = true;
        }
    }

    public void SetShowP100Tile(bool visible)
    {
        _showP100Tile = visible;
        Save();
    }

    private void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(StorePath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(new UiSettings { ShowP100Tile = _showP100Tile },
                new JsonSerializerOptions { WriteIndented = true });
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
    }
}
