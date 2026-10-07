using Windows.System.Power;

namespace Deskling.Windows.System;

/// <summary>
/// Battery and mains state from <c>Windows.System.Power.PowerManager</c>: whether the PC runs on battery, is
/// charging, how full the battery is and whether battery saver is on. Reads the figures the OS already has: no
/// permission, no device access. <see cref="Changed"/> is raised on a thread-pool thread whenever any of them
/// changes; dispose to stop listening.
/// </summary>
public sealed class PowerSource : IDisposable
{
    private readonly EventHandler<object> onChanged;

    public PowerSource()
    {
        onChanged = (_, _) => Changed?.Invoke();
        PowerManager.BatteryStatusChanged += onChanged;
        PowerManager.PowerSupplyStatusChanged += onChanged;
        PowerManager.RemainingChargePercentChanged += onChanged;
        PowerManager.EnergySaverStatusChanged += onChanged;
    }

    /// <summary>Any of the properties below changed.</summary>
    public event Action? Changed;

    /// <summary>False on a desktop PC, or a laptop whose battery is gone.</summary>
    public bool HasBattery => PowerManager.BatteryStatus != BatteryStatus.NotPresent;

    /// <summary>No mains power: running on the battery.</summary>
    public bool IsOnBattery => PowerManager.PowerSupplyStatus == PowerSupplyStatus.NotPresent;

    public bool IsCharging => PowerManager.BatteryStatus == BatteryStatus.Charging;

    /// <summary>0…100, across all batteries.</summary>
    public int ChargePercent => PowerManager.RemainingChargePercent;

    /// <summary>Windows' battery saver is on, so the app should do less in the background too.</summary>
    public bool IsEnergySaverOn => PowerManager.EnergySaverStatus == EnergySaverStatus.On;

    public void Dispose()
    {
        PowerManager.BatteryStatusChanged -= onChanged;
        PowerManager.PowerSupplyStatusChanged -= onChanged;
        PowerManager.RemainingChargePercentChanged -= onChanged;
        PowerManager.EnergySaverStatusChanged -= onChanged;
    }
}
