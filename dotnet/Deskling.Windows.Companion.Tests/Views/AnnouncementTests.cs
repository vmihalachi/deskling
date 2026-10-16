using Deskling.Windows.Companion.Support;
using Deskling.Windows.Companion.Views;
using Microsoft.UI.Xaml;

namespace Deskling.Windows.Companion.Tests.Views;

/// <summary>When Narrator hears words that appear on their own: the rules, without the Windows App SDK runtime.</summary>
public sealed class AnnouncementTests
{
    private static readonly SupportCopy Copy = new(
        Pitch: "Pitch",
        BecomeSupporter: price => $"Become a supporter, {price}",
        SupporterNote: "Supporter note",
        SupporterThanks: "Supporter thanks",
        TipTitle: id => id,
        TipsNote: "Tips note",
        WelcomeLine: "Thank you!",
        WelcomeNote: "You're a supporter now.",
        TipThanksLine: "Thanks for the tip!",
        Error: _ => "Error",
        AskToBuy: "Ask to buy",
        PricesFailed: "Prices failed",
        TryAgain: "Try again",
        Restore: "Restore purchases",
        Done: "Done");

    [Fact]
    public void ABubbleAnnouncesANewLineOnlyWhenItOptedIn()
    {
        Assert.Equal("A line", SpeechBubble.Announcement(announces: true, shown: null, next: "A line"));
        Assert.Equal("The punchline", SpeechBubble.Announcement(announces: true, shown: "The setup", next: "The punchline"));
        Assert.Null(SpeechBubble.Announcement(announces: false, shown: null, next: "A line"));
    }

    [Fact]
    public void ABubbleNeverAnnouncesTheSameWordsOrNoLine()
    {
        Assert.Null(SpeechBubble.Announcement(announces: true, shown: "A line", next: "A line"));
        // x:Bind hands a null line over as empty: the "…" isn't something said.
        Assert.Null(SpeechBubble.Announcement(announces: true, shown: "A line", next: null));
        Assert.Null(SpeechBubble.Announcement(announces: true, shown: "A line", next: ""));
    }

    [Fact]
    public void OneLineShownInEveryViewIsAnnouncedOnce()
    {
        // Home opted in; the panel and the desktop bubble didn't.
        var views = new[] { true, false, false };

        var announced = views.Select(announces => SpeechBubble.Announcement(announces, "Before", "A line")).OfType<string>();

        Assert.Equal(["A line"], announced);
    }

    [Fact]
    public void APillAnnouncesWhenItsStatusChangesKindNeverForTheCountdown()
    {
        Assert.True(StatusPill.ShouldAnnounce(announces: true, shown: "Next line in about 1 min", next: "Shh until 15:51"));
        Assert.True(StatusPill.ShouldAnnounce(announces: true, shown: "Shh until 15:51", next: "Next line in about 1 min"));
        // The countdown keeps its kind while its minutes change.
        Assert.False(StatusPill.ShouldAnnounce(announces: true, shown: "Next line in about 1 min", next: "Next line in about 1 min"));
        Assert.False(StatusPill.ShouldAnnounce(announces: false, shown: "Next line in about 1 min", next: "Shh until 15:51"));
    }

    [Fact]
    public void APillIsQuietWithoutAKindAndOnTheStatusItOpenedOn()
    {
        Assert.False(StatusPill.ShouldAnnounce(announces: true, shown: null, next: "Asleep"));
        Assert.False(StatusPill.ShouldAnnounce(announces: true, shown: "", next: "Asleep"));
        Assert.False(StatusPill.ShouldAnnounce(announces: true, shown: "Asleep", next: ""));
    }

    [Fact]
    public void SupportThanksOnceRightAfterAPurchaseOrATip()
    {
        Assert.Equal("Thank you! You're a supporter now.", SupportWindow.Thanks(SupportStage.Pitch, SupportStage.Welcome, Copy));
        Assert.Equal("Thanks for the tip!", SupportWindow.Thanks(SupportStage.Supporter, SupportStage.Tipped, Copy));
        Assert.Equal("Thank you!", SupportWindow.Thanks(SupportStage.Pitch, SupportStage.Welcome, Copy with { WelcomeNote = null }));
        // Rebuilt on the same stage (a price arriving), or on an ask: nothing to say.
        Assert.Null(SupportWindow.Thanks(SupportStage.Welcome, SupportStage.Welcome, Copy));
        Assert.Null(SupportWindow.Thanks(null, SupportStage.Pitch, Copy));
        Assert.Null(SupportWindow.Thanks(SupportStage.Pitch, SupportStage.Supporter, Copy));
    }

    [Fact]
    public void ANoteAnnouncesWhenItAppears()
    {
        Assert.True(Announcer.Appears(Visibility.Collapsed, Visibility.Visible));
        Assert.False(Announcer.Appears(Visibility.Visible, Visibility.Visible));
        Assert.False(Announcer.Appears(Visibility.Visible, Visibility.Collapsed));
        Assert.False(Announcer.Appears(Visibility.Collapsed, Visibility.Collapsed));
    }
}
