using System.Collections.Frozen;

namespace Deskling.Core.Scheduling;

/// <summary>Reports how long the user has been away from mouse and keyboard.</summary>
public interface IIdleTimeProvider
{
    double SecondsSinceLastInput();
}

/// <summary>Something that makes now a bad moment for a reminder.</summary>
public enum BusyReason
{
    /// <summary>Another app is using the camera or the microphone.</summary>
    Call,

    /// <summary>The frontmost app is full screen (video, presenting, games).</summary>
    FullScreen,
}

/// <summary>Reports what the user is busy with right now. Implementations must never prompt for a permission.</summary>
public interface IBusyStateProvider
{
    IReadOnlySet<BusyReason> CurrentBusyReasons();
}

/// <summary>Never busy. The default when no provider is injected.</summary>
public sealed class NeverBusy : IBusyStateProvider
{
    public IReadOnlySet<BusyReason> CurrentBusyReasons() => FrozenSet<BusyReason>.Empty;
}
