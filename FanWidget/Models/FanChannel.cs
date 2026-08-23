using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FanWidget.Models;

public sealed class FanChannel : INotifyPropertyChanged
{
    private string _displayName = string.Empty;
    private string _sensorId = string.Empty;
    private float _targetPercent;
    private int? _rpm;
    private int? _currentPercent;
    private bool _isManual;
    private bool _isAvailable;

    public string DisplayName
    {
        get => _displayName;
        set => SetField(ref _displayName, value);
    }

    public string SensorId
    {
        get => _sensorId;
        set => SetField(ref _sensorId, value);
    }

    public float TargetPercent
    {
        get => _targetPercent;
        set => SetField(ref _targetPercent, value);
    }

    public int? Rpm
    {
        get => _rpm;
        set => SetField(ref _rpm, value);
    }

    public int? CurrentPercent
    {
        get => _currentPercent;
        set => SetField(ref _currentPercent, value);
    }

    public bool IsManual
    {
        get => _isManual;
        set => SetField(ref _isManual, value);
    }

    public bool IsAvailable
    {
        get => _isAvailable;
        set => SetField(ref _isAvailable, value);
    }

    public string RpmText => Rpm.HasValue ? $"{Rpm.Value} RPM" : "— RPM";
    public string PercentText => CurrentPercent.HasValue ? $"{CurrentPercent.Value} %" : "— %";

    public event PropertyChangedEventHandler? PropertyChanged;

    public void NotifyComputed()
    {
        OnPropertyChanged(nameof(RpmText));
        OnPropertyChanged(nameof(PercentText));
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        OnPropertyChanged(name);

        if (name is nameof(Rpm) or nameof(CurrentPercent))
            NotifyComputed();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
