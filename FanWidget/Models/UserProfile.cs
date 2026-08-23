namespace FanWidget.Models;

public sealed class FanProfileSetting
{
    public string SensorId { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = true;
    public bool IsAuto { get; set; } = true;
    public int ManualPercent { get; set; } = 50;
}

public sealed class UserProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public bool P100Enabled { get; set; }
    public List<FanProfileSetting> Fans { get; set; } = [];
}

public sealed class ProfileStoreData
{
    public string? StartupProfileId { get; set; }
    public string? ActiveProfileId { get; set; }
    public List<UserProfile> Profiles { get; set; } = [];
}
