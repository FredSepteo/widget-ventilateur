using System.IO;
using System.Text.Json;

namespace FanWidget.Services;

public sealed class FanLabelStore
{
    private readonly Dictionary<string, string> _labels = new(StringComparer.OrdinalIgnoreCase);

    private string StorePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FanWidget",
            "fan-labels.json");

    public void Load()
    {
        _labels.Clear();
        try
        {
            if (!File.Exists(StorePath))
                return;

            var json = File.ReadAllText(StorePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (data is null)
                return;

            foreach (var (id, label) in data)
            {
                if (!string.IsNullOrWhiteSpace(label))
                    _labels[id] = label.Trim();
            }
        }
        catch
        {
            // ignore
        }
    }

    public string GetLabel(string sensorId, string defaultName) =>
        _labels.TryGetValue(sensorId, out var label) && !string.IsNullOrWhiteSpace(label)
            ? label
            : defaultName;

    public void SetLabel(string sensorId, string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            _labels.Remove(sensorId);
        else
            _labels[sensorId] = label.Trim();

        Save();
    }

    private void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(StorePath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(_labels, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(StorePath, json);
        }
        catch
        {
            // non-critical
        }
    }
}
