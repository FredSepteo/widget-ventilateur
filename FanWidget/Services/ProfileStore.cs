using System.IO;
using System.Text.Json;
using FanWidget.Models;

namespace FanWidget.Services;

public sealed class ProfileStore
{
    private ProfileStoreData _data = new();

    private string StorePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FanWidget",
            "profiles.json");

    public IReadOnlyList<UserProfile> Profiles => _data.Profiles;
    public string? StartupProfileId => _data.StartupProfileId;
    public string? ActiveProfileId => _data.ActiveProfileId;

    public void Load()
    {
        _data = new ProfileStoreData();
        try
        {
            if (!File.Exists(StorePath))
                return;

            var json = File.ReadAllText(StorePath);
            var loaded = JsonSerializer.Deserialize<ProfileStoreData>(json);
            if (loaded is not null)
                _data = loaded;
        }
        catch
        {
            _data = new ProfileStoreData();
        }
    }

    public UserProfile? GetById(string? id) =>
        string.IsNullOrWhiteSpace(id)
            ? null
            : _data.Profiles.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));

    public UserProfile? GetStartupProfile() => GetById(_data.StartupProfileId);

    public void Add(UserProfile profile)
    {
        _data.Profiles.Add(profile);
        Save();
    }

    public void Update(UserProfile profile)
    {
        var index = _data.Profiles.FindIndex(p => string.Equals(p.Id, profile.Id, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            Add(profile);
            return;
        }

        _data.Profiles[index] = profile;
        Save();
    }

    public void Delete(string profileId)
    {
        _data.Profiles.RemoveAll(p => string.Equals(p.Id, profileId, StringComparison.OrdinalIgnoreCase));

        if (string.Equals(_data.StartupProfileId, profileId, StringComparison.OrdinalIgnoreCase))
            _data.StartupProfileId = null;

        if (string.Equals(_data.ActiveProfileId, profileId, StringComparison.OrdinalIgnoreCase))
            _data.ActiveProfileId = null;

        Save();
    }

    public void SetStartupProfile(string? profileId)
    {
        _data.StartupProfileId = string.IsNullOrWhiteSpace(profileId) ? null : profileId;
        Save();
    }

    public void SetActiveProfile(string? profileId)
    {
        _data.ActiveProfileId = string.IsNullOrWhiteSpace(profileId) ? null : profileId;
        Save();
    }

    private void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(StorePath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(StorePath, json);
        }
        catch
        {
            // non-critical
        }
    }
}
