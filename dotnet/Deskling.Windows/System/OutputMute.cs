using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Deskling.Windows.Shell;

namespace Deskling.Windows.System;

/// <summary>
/// Whether the default output device is muted, read from Core Audio's endpoint volume (<c>IMMDeviceEnumerator</c> →
/// <c>IMMDevice</c> → <c>IAudioEndpointVolume</c>). Reads one flag; never opens a stream, never prompts. Call it
/// from a thread with COM initialized (the UI thread). The source-generated COM interfaces keep it Native AOT friendly.
/// </summary>
public sealed class OutputMute
{
    private static readonly Guid DeviceEnumeratorClass = new("BCDE0395-E52F-467C-8E3D-C4579291692E");
    private static readonly Guid DeviceEnumeratorInterface = new("A95664D2-9614-4F35-A746-DE8DB63617E6");
    private static readonly Guid EndpointVolumeInterface = new("5CDF2C82-841E-4546-9722-0CF74078229A");
    private static readonly StrategyBasedComWrappers Wrappers = new();

    /// <summary>EDataFlow::eRender.</summary>
    private const int Render = 0;

    /// <summary>ERole::eMultimedia: the device sounds and media play on.</summary>
    private const int Multimedia = 1;

    /// <summary>True only when there is a default output device and it is muted; unknown reads as not muted.</summary>
    public bool IsMuted => TryRead(out var muted) && muted;

    /// <summary>
    /// Reads the mute flag; false when there is no output device or Core Audio can't be reached, with
    /// <paramref name="muted"/> left false.
    /// </summary>
    public bool TryRead(out bool muted)
    {
        muted = false;
        var hr = NativeMethods.CoCreateInstance(
            in DeviceEnumeratorClass,
            0,
            NativeMethods.CLSCTX_INPROC_SERVER,
            in DeviceEnumeratorInterface,
            out var raw
        );
        if (hr < 0 || raw == 0)
            return false;
        var enumerator = Wrap<IMMDeviceEnumerator>(raw);
        try
        {
            if (enumerator.GetDefaultAudioEndpoint(Render, Multimedia, out var devicePointer) < 0 || devicePointer == 0)
                return false;
            var device = Wrap<IMMDevice>(devicePointer);
            try
            {
                var activated = device.Activate(
                    in EndpointVolumeInterface,
                    NativeMethods.CLSCTX_INPROC_SERVER,
                    0,
                    out var volumePointer
                );
                if (activated < 0 || volumePointer == 0)
                    return false;
                var volume = Wrap<IAudioEndpointVolume>(volumePointer);
                try
                {
                    if (volume.GetMute(out var flag) < 0)
                        return false;
                    muted = flag != 0;
                    return true;
                }
                finally
                {
                    Release(volume);
                }
            }
            finally
            {
                Release(device);
            }
        }
        finally
        {
            Release(enumerator);
        }
    }

    /// <summary>
    /// Wraps a pointer we own in a unique wrapper (released by <see cref="Release"/>, not the GC) and drops our reference.
    /// </summary>
    private static T Wrap<T>(nint pointer)
        where T : class
    {
        try
        {
            return (T)Wrappers.GetOrCreateObjectForComInstance(pointer, CreateObjectFlags.UniqueInstance);
        }
        finally
        {
            Marshal.Release(pointer);
        }
    }

    private static void Release(object wrapper) => (wrapper as ComObject)?.FinalRelease();
}

[GeneratedComInterface]
[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
internal partial interface IMMDeviceEnumerator
{
    [PreserveSig]
    int EnumAudioEndpoints(int dataFlow, uint stateMask, out nint devices);

    [PreserveSig]
    int GetDefaultAudioEndpoint(int dataFlow, int role, out nint endpoint);
}

[GeneratedComInterface]
[Guid("D666063F-1587-4E43-81F1-B948E807363F")]
internal partial interface IMMDevice
{
    [PreserveSig]
    int Activate(in Guid iid, uint clsCtx, nint activationParams, out nint instance);
}

[GeneratedComInterface]
[Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
internal partial interface IAudioEndpointVolume
{
    [PreserveSig]
    int RegisterControlChangeNotify(nint callback);

    [PreserveSig]
    int UnregisterControlChangeNotify(nint callback);

    [PreserveSig]
    int GetChannelCount(out uint count);

    [PreserveSig]
    int SetMasterVolumeLevel(float levelDecibels, nint eventContext);

    [PreserveSig]
    int SetMasterVolumeLevelScalar(float level, nint eventContext);

    [PreserveSig]
    int GetMasterVolumeLevel(out float levelDecibels);

    [PreserveSig]
    int GetMasterVolumeLevelScalar(out float level);

    [PreserveSig]
    int SetChannelVolumeLevel(uint channel, float levelDecibels, nint eventContext);

    [PreserveSig]
    int SetChannelVolumeLevelScalar(uint channel, float level, nint eventContext);

    [PreserveSig]
    int GetChannelVolumeLevel(uint channel, out float levelDecibels);

    [PreserveSig]
    int GetChannelVolumeLevelScalar(uint channel, out float level);

    [PreserveSig]
    int SetMute(int mute, nint eventContext);

    [PreserveSig]
    int GetMute(out int mute);
}
