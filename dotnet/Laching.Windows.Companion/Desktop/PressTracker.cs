namespace Laching.Windows.Companion.Desktop;

/// <summary>
/// How the pointers down on a desktop window add up: a click, a drag, a hold, or a pinch, which is none of them. It goes
/// by each pointer's own positions (screen pixels), since a finger doesn't move the mouse cursor. The first pointer down
/// starts the press and only it drags; a second one down, or a pinch's scale, makes the press a pinch, which ends a drag
/// where it is and never clicks or holds. A press the window has timed as held (<see cref="Hold"/>) comes up as a hold
/// instead of a click, unless it drags or pinches first. The press is over once every pointer is up. UI thread only.
/// </summary>
internal sealed class PressTracker
{
    private readonly HashSet<uint> contacts = [];
    private uint pointer;
    private double startX;
    private double startY;
    private bool dragging;
    private bool pinch;
    private bool held;

    /// <summary>What a pointer going down did.</summary>
    public enum Start
    {
        /// <summary>It started a press: the window's frame now is where a drag moves from.</summary>
        Press,

        /// <summary>It joined a press, which is now a pinch.</summary>
        Joined,

        /// <summary>It joined a press that was dragging: the pinch ends the drag where it is.</summary>
        EndsDrag,
    }

    /// <summary>What a press came to when one of its pointers came up or was lost.</summary>
    public enum End
    {
        None,
        Click,
        Drag,

        /// <summary>A press-and-hold: the menu, as a right click.</summary>
        Hold,
    }

    /// <summary>Whether a pointer is down.</summary>
    public bool IsPressed => contacts.Count > 0;

    public Start Down(uint id, double x, double y)
    {
        if (contacts.Count > 0)
        {
            contacts.Add(id);
            return Pinch() == End.Drag ? Start.EndsDrag : Start.Joined;
        }
        contacts.Add(id);
        (pointer, startX, startY, dragging, pinch, held) = (id, x, y, false, false, false);
        return Start.Press;
    }

    /// <summary>
    /// The press has been down long enough to be a press-and-hold: one that hasn't dragged or pinched comes up as
    /// <see cref="End.Hold"/> instead of a click. Dragging or pinching later still wins.
    /// </summary>
    public void Hold()
    {
        if (IsPressed && !dragging && !pinch)
            held = true;
    }

    /// <summary>
    /// A pointer moved to (x, y): how far the window moves from its frame at the press, once the press's own pointer has
    /// gone <paramref name="threshold"/> pixels; null while it hasn't, for another pointer, and in a pinch.
    /// </summary>
    public (int Dx, int Dy)? Move(uint id, double x, double y, double threshold)
    {
        if (pinch || id != pointer || !contacts.Contains(id))
            return null;
        var (dx, dy) = (x - startX, y - startY);
        if (!dragging && Math.Sqrt(dx * dx + dy * dy) < threshold)
            return null;
        dragging = true;
        return ((int)Math.Round(dx), (int)Math.Round(dy));
    }

    /// <summary>
    /// A pointer came up: a click when the press's only pointer comes up without having dragged or pinched, a hold when
    /// it was held too.
    /// </summary>
    public End Up(uint id)
    {
        if (!contacts.Contains(id))
            return End.None;
        var end =
            id != pointer ? End.None
            : dragging ? End.Drag
            : pinch || contacts.Count != 1 ? End.None
            : held ? End.Hold
            : End.Click;
        Lift(id);
        return end;
    }

    /// <summary>A pointer's capture went elsewhere: a drag ends where it is, and the press won't click or hold.</summary>
    public End Lost(uint id)
    {
        if (!contacts.Contains(id))
            return End.None;
        var end = id == pointer && dragging ? End.Drag : End.None;
        pinch = true;
        Lift(id);
        return end;
    }

    /// <summary>The press is a pinch from now on: no click, and a drag under way ends (<see cref="End.Drag"/>).</summary>
    public End Pinch()
    {
        if (!IsPressed)
            return End.None;
        pinch = true;
        if (!dragging)
            return End.None;
        dragging = false;
        return End.Drag;
    }

    private void Lift(uint id)
    {
        contacts.Remove(id);
        if (id == pointer)
            dragging = false;
    }
}
