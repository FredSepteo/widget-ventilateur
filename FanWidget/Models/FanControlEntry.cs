using LibreHardwareMonitor.Hardware;

namespace FanWidget.Models;

public sealed class FanControlEntry
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public ISensor ControlSensor { get; init; } = null!;
    public ISensor? FanSensor { get; init; }
    public IControl Control { get; init; } = null!;
}
