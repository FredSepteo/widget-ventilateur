namespace FanWidget.Models;

public sealed class MotherboardProfile
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required string[] SysFan1Patterns { get; init; }
    public required string[] SysFan2Patterns { get; init; }
    public string[]? PreferredControlIds { get; init; }
}

public static class MotherboardProfiles
{
    private static readonly MotherboardProfile Generic = new()
    {
        Id = "generic",
        DisplayName = "Générique",
        SysFan1Patterns =
        [
            @"sys\s*fan\s*1", @"system\s*fan\s*1", @"sys_fan\s*1", @"cha\s*fan\s*1",
            @"fan\s*#\s*3", @"fan\s*3\b", @"control/10\b", @"control/2\b",
        ],
        SysFan2Patterns =
        [
            @"sys\s*fan\s*2", @"system\s*fan\s*2", @"sys_fan\s*2", @"cha\s*fan\s*2",
            @"fan\s*#\s*4", @"fan\s*4\b", @"control/11\b", @"control/3\b",
        ],
    };

    private static readonly MotherboardProfile MsiB760Nct6687D = new()
    {
        Id = "msi-b760-nct6687d",
        DisplayName = "MSI B760 (NCT6687D)",
        SysFan1Patterns =
        [
            @"system\s*fan\s*1", @"sys\s*fan\s*1", @"sysfan\s*1",
            @"nct6687d/control/2\b", @"nct6687dr/control/10\b", @"control/10\b", @"control/2\b",
        ],
        SysFan2Patterns =
        [
            @"system\s*fan\s*2", @"sys\s*fan\s*2", @"sysfan\s*2",
            @"nct6687d/control/3\b", @"nct6687dr/control/11\b", @"control/11\b", @"control/3\b",
        ],
        PreferredControlIds =
        [
            "/lpc/nct6687d/0/control/2",
            "/lpc/nct6687d/0/control/3",
            "/lpc/nct6687d/control/2",
            "/lpc/nct6687d/control/3",
            "/lpc/nct6687dr/control/10",
            "/lpc/nct6687dr/control/11",
        ],
    };

    private static readonly Dictionary<string, MotherboardProfile> ByProductKey = new(StringComparer.OrdinalIgnoreCase)
    {
        ["7D98"] = MsiB760Nct6687D,
        ["PRO B760-P WIFI DDR4"] = MsiB760Nct6687D,
        ["PRO B760-P WIFI"] = MsiB760Nct6687D,
    };

    public static MotherboardProfile Resolve(string manufacturer, string product)
    {
        if (IsMsi(manufacturer))
        {
            foreach (var (key, profile) in ByProductKey)
            {
                if (product.Contains(key, StringComparison.OrdinalIgnoreCase))
                    return profile;
            }

            if (product.Contains("B760", StringComparison.OrdinalIgnoreCase))
                return MsiB760Nct6687D;
        }

        return Generic;
    }

    public static MotherboardProfile GetById(string id) =>
        id switch
        {
            "msi-b760-nct6687d" => MsiB760Nct6687D,
            _ => Generic,
        };

    private static bool IsMsi(string manufacturer) =>
        manufacturer.Contains("micro-star", StringComparison.OrdinalIgnoreCase)
        || manufacturer.Contains("msi", StringComparison.OrdinalIgnoreCase);
}
