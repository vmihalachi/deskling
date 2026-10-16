using Deskling.Core.Store;
using Microsoft.UI.Xaml;

namespace Deskling.Windows.Companion.Support;

/// <summary>The products the Support window sells (Swift's <c>SupportProducts</c>): the one-time supporter purchase and the tips, smallest first.</summary>
public sealed record SupportProducts(string Supporter, IReadOnlyList<string> Tips);

/// <summary>
/// Every word the Support window shows, already localized by the app (Swift's <c>SupportCopy</c>). No defaults.
/// </summary>
/// <param name="Pitch">Beside the character before the supporter purchase: who makes the app and what supporting does.</param>
/// <param name="BecomeSupporter">The supporter button, with the store's price once loaded (null before).</param>
/// <param name="SupporterNote">Under the supporter button (pay once, shared with the family).</param>
/// <param name="SupporterThanks">Beside the character once the user is a supporter, above the tips.</param>
/// <param name="TipTitle">A tip's name, by product id.</param>
/// <param name="TipsNote">Under the tips.</param>
/// <param name="WelcomeLine">What the character says right after the supporter purchase.</param>
/// <param name="WelcomeNote">The note under it, if any.</param>
/// <param name="TipThanksLine">What the character says right after a tip.</param>
/// <param name="Error">A purchase or restore failed.</param>
/// <param name="AskToBuy">Waiting for someone's approval.</param>
/// <param name="PricesFailed">The prices couldn't be loaded; <paramref name="TryAgain"/> is the button that asks again.</param>
public sealed record SupportCopy(
    string Pitch,
    Func<string?, string> BecomeSupporter,
    string SupporterNote,
    string SupporterThanks,
    Func<string, string> TipTitle,
    string TipsNote,
    string WelcomeLine,
    string? WelcomeNote,
    string TipThanksLine,
    Func<PurchaseError, string> Error,
    string AskToBuy,
    string PricesFailed,
    string TryAgain,
    string Restore,
    string Done
);

/// <summary>
/// How the Support window looks in the app (Swift's <c>SupportLook</c>): its title and the header over the tips (the
/// app's own text styles), its character beside the words (<c>celebrating</c> false) or large after a purchase, the
/// bubble over the celebrating character, and its buttons' styles. Each is made when the window needs it. The title and
/// the tips' header are headings for Narrator only when the app makes them so (<c>AutomationProperties.HeadingLevel</c>).
/// </summary>
/// <param name="Primary">
/// Done's style, and <paramref name="LargePrimary"/> the supporter button's and the tips': accent styles, such as WinUI's
/// <c>AccentButtonStyle</c>. In a contrast theme their words keep the style's own contrast colors, with no backplate.
/// </param>
/// <param name="TipPrice">A tip's price under its name: the app tones it down its own way; 12-point text at 80 % when null.</param>
public sealed record SupportLook(
    Func<UIElement> Title,
    Func<UIElement> TipsHeader,
    Func<bool, UIElement> Character,
    Func<string, UIElement> Bubble,
    Style? Primary = null,
    Style? LargePrimary = null,
    Style? Outline = null,
    Func<string, UIElement>? TipPrice = null
);

/// <summary>What the Support window shows.</summary>
public enum SupportStage
{
    /// <summary>The pitch and the supporter button.</summary>
    Pitch,

    /// <summary>Right after the supporter purchase: the character, glad, and nothing else to buy.</summary>
    Welcome,

    /// <summary>A supporter: thanks, and the tips.</summary>
    Supporter,

    /// <summary>Right after a tip: only the thank-you.</summary>
    Tipped,
}
