using Laching.Companion.Rules;
using Laching.Companion.Shell.Speech;

namespace Laching.Windows.Companion.Shell;

/// <summary>
/// Which glyph frame the tray shows (Swift's <c>StatusItemController</c> frames): the mood's frame 0 at rest; while
/// talking or waving, the talking frames from 1 up at 8 fps, or frame 1 held when motion is reduced. Draws through
/// <c>draw</c> on every change. Apart from the icon, so its timing is tested with hand-run timers.
/// </summary>
internal sealed class GlyphAnimation(ITimers timers, Func<bool> reduceMotion, Action<Mood, int> draw)
{
    /// <summary>The glyph's frame rate while it animates.</summary>
    public const double FrameInterval = 1.0 / 8;

    /// <summary>How long a wave runs.</summary>
    public const double WaveSeconds = 1.2;

    private Mood mood = Mood.Awake;
    private bool talking;
    private int frame;
    private IDisposable? frameTimer;
    private IDisposable? waveEnd;

    public bool IsAnimating => talking || waveEnd is not null;

    /// <summary>Shows <paramref name="mood"/>, stepping the talking frames while <paramref name="talking"/> (or a wave runs).</summary>
    public void Show(Mood mood, bool talking)
    {
        this.mood = mood;
        if (talking != this.talking)
        {
            this.talking = talking;
            if (talking)
                StartFrames();
            else if (waveEnd is null)
                StopFrames();
        }
        Redraw();
    }

    /// <summary>The talking frames for a moment, though nothing is said (Home appeared).</summary>
    public void Wave()
    {
        waveEnd?.Dispose();
        StartFrames();
        waveEnd = timers.After(
            WaveSeconds,
            () =>
            {
                waveEnd = null;
                if (!talking)
                    StopFrames();
                Redraw();
            }
        );
        Redraw();
    }

    public void Stop()
    {
        waveEnd?.Dispose();
        waveEnd = null;
        StopFrames();
    }

    private void StartFrames()
    {
        frameTimer?.Dispose();
        frameTimer = null;
        frame = 1;
        if (reduceMotion())
            return;
        frameTimer = timers.Every(
            FrameInterval,
            () =>
            {
                frame++;
                Redraw();
            }
        );
    }

    private void StopFrames()
    {
        frameTimer?.Dispose();
        frameTimer = null;
        frame = 0;
    }

    private void Redraw() => draw(IsAnimating ? Mood.Talking : mood, frame);
}
