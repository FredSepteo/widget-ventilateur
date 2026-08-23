using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using FanWidget.Models;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.PawnIo;

namespace FanWidget.Services;

public sealed class FanControlService : IDisposable
{
    private static readonly string[] CpuFanPatterns =
    [
        @"cpu\s*fan", @"^cpu\b",
    ];

    private static readonly string[] ExcludedPatterns =
    [
        @"cpu\s*fan", @"^cpu\b", @"pump", @"aio", @"water", @"opt", @"w_p", @"wp\b", @"chipset",
    ];

    private readonly Computer _computer = new()
    {
        IsMotherboardEnabled = true,
    };

    private readonly HardwareUpdateVisitor _visitor = new();
    private readonly object _lock = new();
    private readonly List<FanControlEntry> _controls = [];
    private readonly Dictionary<string, float> _manualTargets = [];

    private MotherboardInfo _motherboard = new();
    private MotherboardProfile _profile = MotherboardProfiles.GetById("generic");
    private string? _sysFan1Id;
    private string? _sysFan2Id;

    public MotherboardInfo Motherboard => _motherboard;
    public bool IsPawnIoInstalled => PawnIo.IsInstalled;
    public string? PawnIoVersion => PawnIo.Version?.ToString();

    public IReadOnlyList<FanControlEntry> AvailableControls
    {
        get
        {
            lock (_lock)
                return _controls.ToList();
        }
    }

    public bool Initialize(out string? error)
    {
        error = null;
        _motherboard = MotherboardInfo.Detect();
        _profile = MotherboardProfiles.GetById(_motherboard.ProfileId);

        if (!PawnIo.IsInstalled)
        {
            error = "PawnIO n'est pas installé. Ce pilote est requis pour accéder aux ventilateurs de la carte mère.\n\n"
                    + "Exécutez install-pawnio.bat puis relancez le widget.";
            return false;
        }

        try
        {
            _computer.Open();
            RefreshControls();
            LoadMapping();

            if (_controls.Count == 0)
            {
                error = $"Aucun ventilateur contrôlable détecté.\n\n"
                        + $"Carte mère : {_motherboard.Manufacturer} {_motherboard.Product}\n"
                        + $"Profil : {_profile.DisplayName}\n"
                        + $"PawnIO : {PawnIoVersion ?? "?"}\n\n"
                        + "Vérifiez que MSI Center / FanControl ne monopolisent pas le bus ISA.";
                return false;
            }

            if (_sysFan1Id is null || _sysFan2Id is null)
                AutoDetectMapping();

            return true;
        }
        catch (Exception ex)
        {
            error = $"Impossible d'accéder au matériel : {ex.Message}";
            return false;
        }
    }

    public void RefreshControls()
    {
        lock (_lock)
        {
            _controls.Clear();
            foreach (var hardware in _computer.Hardware)
                CollectControls(hardware);

            _computer.Accept(_visitor);
        }
    }

    public IReadOnlyList<FanControlEntry> GetOrderedControls()
    {
        lock (_lock)
            return _controls
                .OrderBy(c => ExtractIndex(c.Id))
                .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
    }

    public static bool IsCpuFan(string name, string id) =>
        CpuFanPatterns.Any(p => Regex.IsMatch(name, p, RegexOptions.IgnoreCase)
                                || Regex.IsMatch(id, p, RegexOptions.IgnoreCase));

    public bool IsReadOnly(string sensorId)
    {
        lock (_lock)
        {
            var entry = _controls.FirstOrDefault(c => c.Id == sensorId);
            return entry is not null && IsCpuFan(entry.Name, entry.Id);
        }
    }

    public void UpdateReading(FanDisplayItem item)
    {
        lock (_lock)
        {
            _computer.Accept(_visitor);

            var entry = _controls.FirstOrDefault(c => c.Id == item.SensorId);
            if (entry is null)
            {
                item.Rpm = null;
                item.CurrentPercent = null;
                item.IsManual = false;
                return;
            }

            item.HardwareName = entry.Name;
            item.Rpm = entry.FanSensor?.Value is float rpm ? (int)rpm : null;
            item.CurrentPercent = entry.ControlSensor.Value is float pwm ? (int)pwm : null;
            item.IsManual = !IsCpuFan(entry.Name, entry.Id) && _manualTargets.ContainsKey(item.SensorId);
        }
    }

    public void SetFanSpeed(string? sensorId, float percent)
    {
        if (string.IsNullOrEmpty(sensorId) || IsReadOnly(sensorId))
            return;

        percent = Math.Clamp(percent, 0f, 100f);

        lock (_lock)
            _manualTargets[sensorId] = percent;

        // Écriture hardware hors UI pour éviter le lag du curseur
        ThreadPool.QueueUserWorkItem(_ =>
        {
            lock (_lock)
            {
                var entry = _controls.FirstOrDefault(c => c.Id == sensorId);
                if (entry is null)
                    return;

                if (_manualTargets.TryGetValue(sensorId, out var latest))
                    entry.Control.SetSoftware(latest);
            }
        });
    }

    public void SetAuto(string? sensorId)
    {
        if (string.IsNullOrEmpty(sensorId) || IsReadOnly(sensorId))
            return;

        lock (_lock)
        {
            var entry = _controls.FirstOrDefault(c => c.Id == sensorId);
            entry?.Control.SetDefault();
            _manualTargets.Remove(sensorId);
        }
    }

    public void MapFans(string? sysFan1Id, string? sysFan2Id)
    {
        lock (_lock)
        {
            _sysFan1Id = ValidateId(sysFan1Id);
            _sysFan2Id = ValidateId(sysFan2Id);
            SaveMapping();
        }
    }

    public string? SysFan1Id => _sysFan1Id;
    public string? SysFan2Id => _sysFan2Id;

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var id in _manualTargets.Keys.ToList())
            {
                var entry = _controls.FirstOrDefault(c => c.Id == id);
                entry?.Control.SetDefault();
            }

            _manualTargets.Clear();
            _computer.Close();
        }
    }

    private void CollectControls(IHardware hardware)
    {
        if (hardware.HardwareType != HardwareType.Motherboard && hardware.HardwareType != HardwareType.SuperIO)
        {
            foreach (var sub in hardware.SubHardware)
                CollectControls(sub);
            return;
        }

        var fanByIndex = hardware.Sensors
            .Where(s => s.SensorType == SensorType.Fan)
            .ToDictionary(s => ExtractIndex(s.Identifier.ToString()), s => s);

        foreach (var control in hardware.Sensors.Where(s => s.SensorType == SensorType.Control && s.Control is not null))
        {
            var id = control.Identifier.ToString();
            if (!IsRelevantControl(id))
                continue;

            fanByIndex.TryGetValue(ExtractIndex(id), out var fanSensor);

            _controls.Add(new FanControlEntry
            {
                Id = id,
                Name = control.Name,
                ControlSensor = control,
                FanSensor = fanSensor,
                Control = control.Control!,
            });
        }

        foreach (var sub in hardware.SubHardware)
            CollectControls(sub);
    }

    private static bool IsRelevantControl(string id)
    {
        if (id.Contains("/lpc/", StringComparison.OrdinalIgnoreCase))
            return true;

        return id.Contains("nct6687", StringComparison.OrdinalIgnoreCase)
               || id.Contains("nct679", StringComparison.OrdinalIgnoreCase)
               || id.Contains("it86", StringComparison.OrdinalIgnoreCase)
               || id.Contains("/control/", StringComparison.OrdinalIgnoreCase);
    }

    private static int ExtractIndex(string identifier)
    {
        var lastSlash = identifier.LastIndexOf('/');
        if (lastSlash < 0 || lastSlash >= identifier.Length - 1)
            return -1;

        return int.TryParse(identifier[(lastSlash + 1)..], out var index) ? index : -1;
    }

    private void AutoDetectMapping()
    {
        var candidates = _controls.Where(c => !IsExcluded(c.Name) && !IsExcluded(c.Id)).ToList();

        if (_profile.PreferredControlIds is not null)
        {
            var preferred = _profile.PreferredControlIds
                .Select(id => candidates.FirstOrDefault(c => c.Id.Contains(id, StringComparison.OrdinalIgnoreCase)))
                .Where(c => c is not null)
                .Cast<FanControlEntry>()
                .DistinctBy(c => c.Id)
                .ToList();

            if (preferred.Count >= 2)
            {
                _sysFan1Id ??= preferred[0].Id;
                _sysFan2Id ??= preferred[1].Id;
                SaveMapping();
                return;
            }
        }

        _sysFan1Id ??= FindByPatterns(candidates, _profile.SysFan1Patterns)?.Id;
        _sysFan2Id ??= FindByPatterns(candidates, _profile.SysFan2Patterns)?.Id;

        if (_sysFan1Id is not null && _sysFan2Id is not null)
        {
            SaveMapping();
            return;
        }

        var remaining = candidates.Where(c => c.Id != _sysFan1Id).ToList();

        if (_sysFan1Id is null && remaining.Count > 0)
            _sysFan1Id = remaining[0].Id;

        if (_sysFan2Id is null)
            _sysFan2Id = candidates.FirstOrDefault(c => c.Id != _sysFan1Id)?.Id;

        SaveMapping();
    }

    private static FanControlEntry? FindByPatterns(IEnumerable<FanControlEntry> entries, IEnumerable<string> patterns)
    {
        foreach (var pattern in patterns)
        {
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            var match = entries.FirstOrDefault(e => regex.IsMatch(e.Name) || regex.IsMatch(e.Id));
            if (match is not null)
                return match;
        }

        return null;
    }

    private static bool IsExcluded(string name) =>
        ExcludedPatterns.Any(p => Regex.IsMatch(name, p, RegexOptions.IgnoreCase));

    private string? ValidateId(string? id) =>
        string.IsNullOrEmpty(id) || _controls.Any(c => c.Id == id) ? id : null;

    private string MappingPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FanWidget",
            "mapping.json");

    private sealed record FanMapping(string? ProfileId, string? SysFan1Id, string? SysFan2Id);

    private void LoadMapping()
    {
        try
        {
            if (!File.Exists(MappingPath))
                return;

            var json = File.ReadAllText(MappingPath);
            var mapping = JsonSerializer.Deserialize<FanMapping>(json);
            if (mapping is null)
                return;

            if (string.Equals(mapping.ProfileId, _profile.Id, StringComparison.OrdinalIgnoreCase))
            {
                _sysFan1Id = ValidateId(mapping.SysFan1Id);
                _sysFan2Id = ValidateId(mapping.SysFan2Id);
            }
        }
        catch
        {
            // Ignore corrupted config
        }
    }

    private void SaveMapping()
    {
        try
        {
            var dir = Path.GetDirectoryName(MappingPath)!;
            Directory.CreateDirectory(dir);

            var mapping = new FanMapping(_profile.Id, _sysFan1Id, _sysFan2Id);
            var json = JsonSerializer.Serialize(mapping, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(MappingPath, json);
        }
        catch
        {
            // Non-critical
        }
    }
}
