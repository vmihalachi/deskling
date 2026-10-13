using Deskling.Companion.Rules;
using Deskling.Windows.Companion.Support;
using Deskling.Windows.Companion.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FixtureApp.Views;

/// <summary>The Support window over the mock store: every word plain English, the fixture's character and a bubble.</summary>
public static class FixtureSupport
{
    public const string Tip = "fixture.tip";

    public static SupportWindow Make(FixtureModel model) =>
        new(
            model.Purchases,
            new SupportProducts(FixtureModel.Supporter, [Tip]),
            new SupportCopy(
                Pitch: "The fixture is made by one person. Supporting it keeps it going, and says thanks.",
                BecomeSupporter: price => price is null ? "Become a supporter" : $"Become a supporter, {price}",
                SupporterNote: "Pay once. Shared with your family.",
                SupporterThanks: "Thank you for supporting the fixture. A tip now and then is welcome too.",
                TipTitle: _ => "A small tip",
                TipsNote: "Tips unlock nothing: they're thanks.",
                WelcomeLine: "Thank you!",
                WelcomeNote: "You're a supporter now.",
                TipThanksLine: "Thanks for the tip!",
                Error: error => $"That didn't go through ({error}).",
                AskToBuy: "Waiting for approval.",
                PricesFailed: "The prices didn't load.",
                TryAgain: "Try again",
                Restore: "Restore purchases",
                Done: "Done"
            ),
            new SupportLook(
                Title: () => new TextBlock { Text = "Support", Style = Style("TitleTextBlockStyle") },
                TipsHeader: () => new TextBlock { Text = "Leave a tip", Style = Style("BodyStrongTextBlockStyle") },
                Character: celebrating => new CharacterCanvas
                {
                    Character = model.GlyphCharacter,
                    Mood = celebrating ? Mood.Talking : Mood.Awake,
                    Width = celebrating ? 150 : 96,
                    Height = celebrating ? 150 : 96,
                    AccessibilityLabel = "The fixture",
                },
                Bubble: line => new SpeechBubble { Text = line, Tail = BubbleTail.Bottom, FontSize = 18, FillsWidth = false },
                Primary: Style("AccentButtonStyle"),
                LargePrimary: Style("AccentButtonStyle")
            ),
            () => model.Windows.Close(FixtureWindow.Support)
        );

    private static Style Style(string key) => (Style)Application.Current.Resources[key];
}
