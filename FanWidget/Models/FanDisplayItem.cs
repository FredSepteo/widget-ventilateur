using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FanWidget.Models;

public sealed class FanDisplayItem : INotifyPropertyChanged
{
    private string _userLabel = string.Empty;
    private string _hardwareName = string.Empty;
    private int? _rpm;
    private int? _currentPercent;
    private float _sliderValue = 50;
    private bool _isManual;
    private bool _isDragging;

    public string SensorId { get; init; } = string.Empty;
    public int SortIndex { get; init; }
    public bool IsReadOnly { get; init; }

    public string UserLabel
    {
        get => _userLabel;
        set => SetField(ref _userLabel, value);
    }

    public string HardwareName
    {
        get => _hardwareName;
        set => SetField(ref _hardwareName, value);
    }

    public int? Rpm
    {
        get => _rpm;
        set
        {
            if (SetField(ref _rpm, value))
                OnPropertyChanged(nameof(RpmText));
        }
    }

    public int? CurrentPercent
    {
        get => _currentPercent;
        set
        {
            if (SetField(ref _currentPercent, value))
                OnPropertyChanged(nameof(PercentText));
        }
    }

    public float SliderValue
    {
        get => _sliderValue;
        set => SetField(ref _sliderValue, value);
    }

    public bool IsManual
    {
        get => _isManual;
        set
        {
            if (SetField(ref _isManual, value))
            {
                OnPropertyChanged(nameof(IsAuto));
                OnPropertyChanged(nameof(ModeLabel));
            }
        }
    }

    public bool IsDragging
    {
        get => _isDragging;
        set => SetField(ref _isDragging, value);
    }

    public bool IsAuto => !IsManual;

    public string ModeLabel => IsReadOnly ? "LECTURE SEULE" : (IsAuto ? "AUTO" : "MANUEL");
    public string RpmText => Rpm.HasValue ? $"{Rpm.Value} RPM" : "— RPM";
    public string PercentText => CurrentPercent.HasValue ? $"{CurrentPercent.Value} %" : "— %";

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
