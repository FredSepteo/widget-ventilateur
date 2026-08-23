using System.IO;
using System.Text;
using LibreHardwareMonitor.Hardware;

namespace FanWidget.Tools;

internal static class HardwareScanner
{
    public static int Run(string outputPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Scan matériel — {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Admin: {IsAdmin()}");
        sb.AppendLine();

        AppendWmiInfo(sb);

        var computer = new Computer
        {
            IsMotherboardEnabled = true,
            IsCpuEnabled = true,
        };

        try
        {
            computer.Open();
            computer.Accept(new UpdateVisitor());

            foreach (var hardware in computer.Hardware)
            {
                DumpHardware(sb, hardware, 0);
                try
                {
                    var report = hardware.GetReport();
                    if (!string.IsNullOrWhiteSpace(report))
                    {
                        sb.AppendLine();
                        sb.AppendLine("=== Rapport matériel ===");
                        sb.AppendLine(report);
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"Rapport indisponible: {ex.Message}");
                }
            }

            computer.Close();
        }
        catch (Exception ex)
        {
            sb.AppendLine($"ERREUR: {ex}");
        }

        File.WriteAllText(outputPath, sb.ToString());
        Console.WriteLine($"Rapport écrit: {outputPath}");
        return 0;
    }

    private static bool IsAdmin()
    {
        try
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private static void AppendWmiInfo(StringBuilder sb)
    {
        try
        {
            using var board = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");
            foreach (System.Management.ManagementObject obj in board.Get())
            {
                sb.AppendLine($"Fabricant: {obj["Manufacturer"]}");
                sb.AppendLine($"Modèle:    {obj["Product"]}");
                sb.AppendLine($"Version:   {obj["Version"]}");
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine($"WMI erreur: {ex.Message}");
        }

        sb.AppendLine();
    }

    private static void DumpHardware(StringBuilder sb, IHardware hardware, int depth)
    {
        var indent = new string(' ', depth * 2);
        hardware.Update();

        sb.AppendLine($"{indent}[{hardware.HardwareType}] {hardware.Name}");

        foreach (var sensor in hardware.Sensors)
        {
            var controlInfo = sensor.Control is not null ? " [CONTROL OK]" : string.Empty;
            var val = sensor.Value.HasValue ? sensor.Value.Value.ToString("F1") : "null";
            sb.AppendLine($"{indent}  {sensor.SensorType,-8} {sensor.Name,-30} {val,8}  {sensor.Identifier}{controlInfo}");
        }

        foreach (var sub in hardware.SubHardware)
            DumpHardware(sb, sub, depth + 1);
    }

    private sealed class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer) => computer.Traverse(this);
        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (var sub in hardware.SubHardware)
                sub.Accept(this);
        }
        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
    }
}
