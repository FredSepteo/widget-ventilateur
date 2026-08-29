using System.Diagnostics;
using System.Globalization;
using LibreHardwareMonitor.Hardware;

namespace FanWidget.Services;

/// <summary>
/// Lecture température P100 — LibreHardwareMonitor puis repli nvidia-smi (dual-GPU).
/// </summary>
public sealed class GpuTemperatureService : IDisposable
{
    private readonly HardwareUpdateVisitor _visitor = new();
    private readonly object _lock = new();
    private Computer? _computer;
    private int _lhmFailureCount;

    public float? TryReadP100CoreTemperature()
    {
        var lhm = TryReadViaLibreHardwareMonitor();
        if (lhm.HasValue)
            return lhm;

        return TryReadViaNvidiaSmi();
    }

    public void Dispose()
    {
        lock (_lock)
            CloseComputer();
    }

    private float? TryReadViaLibreHardwareMonitor()
    {
        if (_lhmFailureCount >= 3)
            return null;

        try
        {
            lock (_lock)
            {
                var computer = EnsureComputer();
                if (computer is null)
                {
                    _lhmFailureCount++;
                    return null;
                }

                computer.Accept(_visitor);
                var temp = FindP100CoreTemperature(computer.Hardware);
                if (temp.HasValue)
                    _lhmFailureCount = 0;

                return temp;
            }
        }
        catch
        {
            _lhmFailureCount++;
            CloseComputer();
            return null;
        }
    }

    private float? TryReadViaNvidiaSmi()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "nvidia-smi",
                Arguments = "--query-gpu=index,name,temperature.gpu --format=csv,noheader,nounits",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using var process = Process.Start(psi);
            if (process is null)
                return null;

            var output = process.StandardOutput.ReadToEnd();
            if (!process.WaitForExit(5000) || process.ExitCode != 0)
                return null;

            foreach (var line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split(',');
                if (parts.Length < 3)
                    continue;

                var name = parts[1].Trim();
                if (!IsP100Name(name))
                    continue;

                var tempText = parts[2].Trim();
                if (float.TryParse(tempText, NumberStyles.Float, CultureInfo.InvariantCulture, out var temp))
                    return temp;
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }

    private Computer? EnsureComputer()
    {
        if (_computer is not null)
            return _computer;

        try
        {
            _computer = new Computer { IsGpuEnabled = true };
            _computer.Open();
            return _computer;
        }
        catch
        {
            CloseComputer();
            return null;
        }
    }

    private void CloseComputer()
    {
        try
        {
            _computer?.Close();
        }
        catch
        {
            // ignore
        }

        _computer = null;
    }

    private static float? FindP100CoreTemperature(IEnumerable<IHardware> hardwareList)
    {
        foreach (var hardware in hardwareList)
        {
            if (IsP100Hardware(hardware))
            {
                var temp = ReadCoreTemperature(hardware);
                if (temp.HasValue)
                    return temp;
            }

            var subTemp = FindP100CoreTemperature(hardware.SubHardware);
            if (subTemp.HasValue)
                return subTemp;
        }

        return null;
    }

    private static bool IsP100Hardware(IHardware hardware) =>
        hardware.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd
        && IsP100Name(hardware.Name);

    private static bool IsP100Name(string name) =>
        name.Contains("P100", StringComparison.OrdinalIgnoreCase)
        || name.Contains("Tesla", StringComparison.OrdinalIgnoreCase);

    private static float? ReadCoreTemperature(IHardware hardware)
    {
        try
        {
            hardware.Update();
        }
        catch
        {
            return null;
        }

        foreach (var sensor in hardware.Sensors)
        {
            if (sensor.SensorType != SensorType.Temperature || sensor.Value is not float value)
                continue;

            if (sensor.Name.Equals("GPU Core", StringComparison.OrdinalIgnoreCase))
                return value;

            if (sensor.Name.Contains("Core", StringComparison.OrdinalIgnoreCase))
                return value;
        }

        return hardware.Sensors.FirstOrDefault(s =>
            s.SensorType == SensorType.Temperature && s.Value is float)?.Value;
    }
}
