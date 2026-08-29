using System.Diagnostics;
using System.Management;
using System.Text;

namespace FanWidget.Services;

public sealed class GpuP100Service
{
    private const string NamePattern = "P100";
    private readonly object _lock = new();
    private string? _instanceId;

    public bool IsAvailable { get; private set; }
    public bool IsEnabled { get; private set; }
    public string DisplayName { get; private set; } = "Tesla P100";

    public bool Refresh() =>
        Refresh(includePowerShellFallback: true);

    /// <summary>Lecture WMI uniquement — pour le polling périodique sans bloquer l'UI.</summary>
    public bool RefreshLightweight() =>
        Refresh(includePowerShellFallback: false);

    private bool Refresh(bool includePowerShellFallback)
    {
        lock (_lock)
        {
            if (TryRefreshViaWmi())
                return IsAvailable;

            if (includePowerShellFallback)
                return RefreshViaPowerShell();

            return IsAvailable;
        }
    }

    private bool TryRefreshViaWmi()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, DeviceID, Status, ConfigManagerErrorCode FROM Win32_PnPEntity WHERE Name LIKE '%P100%' OR Name LIKE '%Tesla P100%'");

            foreach (ManagementObject obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString() ?? string.Empty;
                if (!name.Contains(NamePattern, StringComparison.OrdinalIgnoreCase)
                    && !name.Contains("Tesla", StringComparison.OrdinalIgnoreCase))
                    continue;

                _instanceId = obj["DeviceID"]?.ToString();
                DisplayName = name;
                IsAvailable = !string.IsNullOrWhiteSpace(_instanceId);

                var status = obj["Status"]?.ToString() ?? string.Empty;
                var errorCode = Convert.ToInt32(obj["ConfigManagerErrorCode"] ?? 0);
                IsEnabled = status.Equals("OK", StringComparison.OrdinalIgnoreCase) && errorCode == 0;
                return true;
            }
        }
        catch
        {
            // ignore
        }

        return false;
    }

    public bool SetEnabled(bool enable, out string? error)
    {
        error = null;

        lock (_lock)
        {
            if (!Refresh(includePowerShellFallback: true) || string.IsNullOrWhiteSpace(_instanceId))
            {
                error = "GPU P100 introuvable.";
                return false;
            }
        }

        var instanceId = _instanceId!;
        var cmd = enable
            ? $"Enable-PnpDevice -InstanceId '{Escape(instanceId)}' -Confirm:$false"
            : $"Disable-PnpDevice -InstanceId '{Escape(instanceId)}' -Confirm:$false";

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{cmd}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                error = "Impossible de lancer PowerShell.";
                return false;
            }

            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit(15000);

            if (process.ExitCode != 0)
            {
                error = string.IsNullOrWhiteSpace(stderr)
                    ? $"Échec (code {process.ExitCode})."
                    : stderr.Trim();
                return false;
            }

            // Laisser Windows appliquer l'état
            Thread.Sleep(800);
            lock (_lock)
            {
                Refresh(includePowerShellFallback: true);
            }
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private bool RefreshViaPowerShell()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -Command \"Get-PnpDevice | Where-Object { $_.FriendlyName -match 'P100|Tesla P100' } | Select-Object -First 1 Status,FriendlyName,InstanceId | ConvertTo-Csv -NoTypeInformation\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using var process = Process.Start(psi);
            if (process is null)
                return false;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(8000);

            var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2)
                return false;

            var cols = ParseCsv(lines[1]);
            if (cols.Count < 3)
                return false;

            IsEnabled = cols[0].Equals("OK", StringComparison.OrdinalIgnoreCase);
            DisplayName = cols[1];
            _instanceId = cols[2];
            IsAvailable = !string.IsNullOrWhiteSpace(_instanceId);
            return IsAvailable;
        }
        catch
        {
            return false;
        }
    }

    private static string Escape(string value) =>
        value.Replace("'", "''");

    private static List<string> ParseCsv(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;

        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (c == ',' && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
                continue;
            }

            sb.Append(c);
        }

        result.Add(sb.ToString());
        return result;
    }
}
