using System.Collections.Concurrent;
using System.Diagnostics;
using IOPath = System.IO.Path;

namespace Deskling.Windows.Companion.Tests;

/// <summary>
/// A UI thread for a test: work posted to it waits in a queue until the test pumps it on its own thread, as a
/// dispatcher would between frames.
/// </summary>
internal sealed class TestUiContext : SynchronizationContext
{
    private readonly BlockingCollection<(SendOrPostCallback Work, object? State)> queue = [];

    public override void Post(SendOrPostCallback d, object? state) => queue.Add((d, state));

    public override void Send(SendOrPostCallback d, object? state) => throw new NotSupportedException();

    /// <summary>Runs posted work until <paramref name="done"/> holds; false if it still doesn't after <paramref name="seconds"/>.</summary>
    public bool PumpUntil(Func<bool> done, double seconds = 15)
    {
        var clock = Stopwatch.StartNew();
        while (!done())
        {
            var left = TimeSpan.FromSeconds(seconds) - clock.Elapsed;
            if (left <= TimeSpan.Zero)
                return false;
            if (queue.TryTake(out var item, left))
                item.Work(item.State);
        }
        return true;
    }

    /// <summary>Runs whatever arrives for <paramref name="seconds"/>.</summary>
    public void PumpFor(double seconds) => PumpUntil(() => false, seconds);
}

/// <summary>Sound files for the tests, in a folder of their own that is deleted afterwards.</summary>
internal sealed class SoundFolder : IDisposable
{
    public SoundFolder()
    {
        Path = IOPath.Combine(IOPath.GetTempPath(), "deskling-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    /// <summary>A mono 16-bit PCM WAV of silence, <paramref name="seconds"/> long.</summary>
    public string Silence(string name, double seconds)
    {
        const int Rate = 16000;
        var samples = (int)(Rate * seconds);
        var path = IOPath.Combine(Path, name);
        using var writer = new BinaryWriter(File.Create(path));
        writer.Write("RIFF"u8);
        writer.Write(36 + samples * 2);
        writer.Write("WAVEfmt "u8);
        writer.Write(16);
        writer.Write((short)1); // PCM
        writer.Write((short)1); // mono
        writer.Write(Rate);
        writer.Write(Rate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(samples * 2);
        writer.Write(new byte[samples * 2]);
        return path;
    }

    /// <summary>A file named like a clip that holds no audio.</summary>
    public string Garbage(string name)
    {
        var path = IOPath.Combine(Path, name);
        File.WriteAllBytes(path, [.. Enumerable.Range(0, 4096).Select(i => (byte)(i * 31))]);
        return path;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // A player may still hold a file for a moment; the temp folder is cleaned up eventually.
        }
        catch (UnauthorizedAccessException) { }
    }
}
