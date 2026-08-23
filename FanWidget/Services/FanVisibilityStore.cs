using System.IO;
using System.Text.Json;

namespace FanWidget.Services;

public sealed class FanVisibilityStore
{
    private readonly HashSet<string> _hiddenIds = new(StringComparer.OrdinalIgnoreCase);

    private string StorePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FanWidget",
            "fan-visibility.json");

    public void Load()
    {
        _hiddenIds.Clear();
        try
        {
            if (!File.Exists(StorePath))
                return;

            var json = File.ReadAllText(StorePath);
            var ids = JsonSerializer.Deserialize<List<string>>(json);
            if (ids is null)
                return;

            foreach (var id in ids)
            {
                if (!string.IsNullOrWhiteSpace(id))
                    _hiddenIds.Add(id.Trim());
            }
        }
        catch
        {
            // ignore
        }
    }

    public bool IsVisible(string sensorId) => !_hiddenIds.Contains(sensorId);

    public void SetVisible(string sensorId, bool visible)
    {
        if (visible)
            _hiddenIds.Remove(sensorId);
        else
            _hiddenIds.Add(sensorId);

        Save();
    }

    public void Apply(IReadOnlyDictionary<string, bool> visibility)
    {
        _hiddenIds.Clear();
        foreach (var (sensorId, visible) in visibility)
        {
            if (!visible)
                _hiddenIds.Add(sensorId);
        }

        Save();
    }

    private void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(StorePath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(_hiddenIds.ToList(), new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(StorePath, json);
        }
        catch
        {
            // non-critical
        }
    }
}
