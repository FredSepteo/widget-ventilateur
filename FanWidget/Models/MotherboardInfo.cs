using System.Management;

namespace FanWidget.Models;

public sealed class MotherboardInfo
{
    public string Manufacturer { get; init; } = string.Empty;
    public string Product { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string ProfileId { get; init; } = "generic";
    public string DisplayName { get; init; } = string.Empty;

    public static MotherboardInfo Detect()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Manufacturer, Product, Version FROM Win32_BaseBoard");
            foreach (ManagementObject obj in searcher.Get())
            {
                var manufacturer = obj["Manufacturer"]?.ToString()?.Trim() ?? string.Empty;
                var product = obj["Product"]?.ToString()?.Trim() ?? string.Empty;
                var version = obj["Version"]?.ToString()?.Trim() ?? string.Empty;
                var profile = MotherboardProfiles.Resolve(manufacturer, product);

                return new MotherboardInfo
                {
                    Manufacturer = manufacturer,
                    Product = product,
                    Version = version,
                    ProfileId = profile.Id,
                    DisplayName = profile.DisplayName,
                };
            }
        }
        catch
        {
            // WMI indisponible
        }

        return new MotherboardInfo();
    }
}
