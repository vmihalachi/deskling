using Laching.Companion.Phrases;

namespace Laching.Companion.Tests;

public sealed class PhrasePickerTests
{
    private static readonly PhraseBank Bank = Fixture.Bank;
    private static readonly SkinId Skin = FixtureIds.VectorSkin;

    /// <summary>Monday 10:00 in English with the chime on: general and morning lines, tunes included.</summary>
    private static PhraseContext MondayMorning => new(10, 2, true, Skin, "en");

    private static PhraseContext Context(int hour, int weekday, Tag? @event = null, bool chime = false) =>
        new(hour, weekday, chime, Skin, "en") { Event = @event };

    /// <summary><paramref name="count"/> general lines of weight 3, "l0", "l1", …</summary>
    private static PhraseBank GeneralBank(int count) =>
        new(Enumerable.Range(0, count).Select(i => new Phrase($"l{i}", Phrase.Line($"l{i}"), [Tag.General])));

    private static string[] Ids(IEnumerable<Phrase> phrases) => phrases.Select(p => p.Id).ToArray();

    [Fact]
    public void AmbientCandidatesAreGeneralDaypartAndWeekdayLinesOnly()
    {
        var candidates = PhrasePicker.Candidates(Bank, MondayMorning);
        Assert.Equal(["general-a1", "general-a2", "general-a3", "general-b1", "general-c1", "morning-1", "monday-1", "tune-1"], Ids(candidates));
        foreach (var phrase in candidates)
        {
            Assert.False(phrase.HasEventTag, phrase.Id);
            Assert.True(phrase.Tags.Overlaps([Tag.General, Tag.Morning, Tag.Monday]), phrase.Id);
        }
    }

    [Fact]
    public void EventLinesWinWhenThereIsAnEvent()
    {
        var context = MondayMorning with { Event = Tag.WelcomeBack };
        Assert.Equal(Ids(Bank.Tagged(Tag.WelcomeBack)), Ids(PhrasePicker.Candidates(Bank, context)));
        Assert.Equal([Tag.WelcomeBack], PhrasePicker.Pick(Bank, context, 0.5)!.Tags);
    }

    [Fact]
    public void ASequenceIsOneCandidateLikeALine()
    {
        var context = MondayMorning with { Event = Tag.ButtonPress };
        Assert.Equal(["button-press-1", "joke-1"], Ids(PhrasePicker.Candidates(Bank, context)));
        Assert.Equal("joke-1", PhrasePicker.Pick(Bank, context, 0.9)?.Id);
        // Its event tag keeps it out of ambient chatter, though it is also tagged general.
        Assert.DoesNotContain(PhrasePicker.Candidates(Bank, MondayMorning), p => p.Id == "joke-1");
        // Recents are keyed on the phrase id: a told joke is skipped whole.
        context = context with { RecentIDs = ["joke-1"] };
        Assert.Equal(["button-press-1"], Ids(PhrasePicker.Candidates(Bank, context)));
    }

    [Fact]
    public void PhrasesOutsideTheLanguageAreDroppedFirst()
    {
        Assert.DoesNotContain(PhrasePicker.Candidates(Bank, MondayMorning), p => p.Languages.Count > 0);
        var italian = Ids(PhrasePicker.Candidates(Bank, MondayMorning with { Language = "it-IT" }));
        Assert.Contains("it-only", italian);
        Assert.Contains("joke-it", italian);
        // An event whose only lines are in another language falls back to ambient chatter.
        var tiny = new PhraseBank(
            [
                new Phrase("any", Phrase.Line("any"), [Tag.General]),
                new Phrase("ciao", Phrase.Line("ciao"), [Tag.WelcomeBack], languages: ["it"]),
            ]
        );
        var context = Context(14, 4, Tag.WelcomeBack);
        Assert.Equal(["any"], Ids(PhrasePicker.Candidates(tiny, context)));
        Assert.Equal(["ciao"], Ids(PhrasePicker.Candidates(tiny, context with { Language = "it" })));
    }

    [Fact]
    public void ChimeOffLeavesTunesOut()
    {
        Assert.Contains(PhrasePicker.Candidates(Bank, MondayMorning), p => p.IsTune);
        Assert.DoesNotContain(PhrasePicker.Candidates(Bank, MondayMorning with { ChimeEnabled = false }), p => p.IsTune);
    }

    [Fact]
    public void UnitZeroPicksTheFirstCandidateAndUnitNearOnePicksTheLast()
    {
        var candidates = PhrasePicker.Candidates(Bank, MondayMorning);
        Assert.Equal(candidates[0].Id, PhrasePicker.Pick(Bank, MondayMorning, 0)?.Id);
        Assert.Equal(candidates[^1].Id, PhrasePicker.Pick(Bank, MondayMorning, 0.999_999)?.Id);
    }

    [Fact]
    public void ASkinsOwnLinesPlayOnlyForIt()
    {
        Assert.DoesNotContain(PhrasePicker.Candidates(Bank, MondayMorning), p => p.Id == "skin-raster-only");
        Assert.Contains(PhrasePicker.Candidates(Bank, MondayMorning with { Skin = FixtureIds.RasterSkin }), p => p.Id == "skin-raster-only");
    }

    /// <summary>A character never says another's lines, and an event whose lines all belong to someone else falls back to ambient chatter.</summary>
    [Fact]
    public void ACharacterNeverSaysAnothersLines()
    {
        foreach (var skin in FixtureIds.Skins)
        {
            foreach (var other in FixtureIds.Skins.Where(s => s != skin))
            {
                var tiny = new PhraseBank(
                    [
                        new Phrase("shared", Phrase.Line("shared"), [Tag.General]),
                        new Phrase("mine", Phrase.Line("mine"), [Tag.General], skins: [skin]),
                        new Phrase("theirs", Phrase.Line("theirs"), [Tag.General], skins: [other]),
                        new Phrase("their-hello", Phrase.Line("their hello"), [Tag.WelcomeBack], skins: [other]),
                    ]
                );
                var context = new PhraseContext(14, 4, false, skin, "en") { Event = Tag.WelcomeBack };
                Assert.Equal(["shared", "mine"], Ids(PhrasePicker.Candidates(tiny, context)));
            }
        }
    }

    [Fact]
    public void DaypartLinesCountDouble()
    {
        // A bank where the general line and the morning line both weigh 1: the morning line takes 2 of 3 draws.
        var tiny = new PhraseBank(
            [new Phrase("g", Phrase.Line("g"), [Tag.General], weight: 1), new Phrase("m", Phrase.Line("m"), [Tag.Morning], weight: 1)]
        );
        var context = Context(9, 3);
        Assert.Equal("g", PhrasePicker.Pick(tiny, context, 0.1)?.Id);
        Assert.Equal("m", PhrasePicker.Pick(tiny, context, 0.4)?.Id);
        Assert.Equal("m", PhrasePicker.Pick(tiny, context, 0.9)?.Id);
    }

    [Fact]
    public void RecentLinesAreSkipped()
    {
        var first = Ids(PhrasePicker.Candidates(Bank, MondayMorning).Take(3));
        var candidates = PhrasePicker.Candidates(Bank, MondayMorning with { RecentIDs = first });
        Assert.Empty(Ids(candidates).Intersect(first));
        Assert.NotEmpty(candidates);
    }

    [Fact]
    public void OnlyTheLastRecentWindowCounts()
    {
        var tiny = GeneralBank(PhrasePicker.RecentWindow + 5);
        var all = Ids(tiny.Phrases);
        var context = Context(14, 4) with { RecentIDs = all.Take(PhrasePicker.RecentWindow + 3).ToArray() };
        // The three oldest are eligible again, and the two never said.
        Assert.Equal(all.Take(3).Concat(all.TakeLast(2)), Ids(PhrasePicker.Candidates(tiny, context)));
    }

    [Fact]
    public void ExhaustedPoolFallsBackToEverythingButTheLastLine()
    {
        var tiny = GeneralBank(2);
        Assert.Equal(["l0"], Ids(PhrasePicker.Candidates(tiny, Context(10, 3) with { RecentIDs = ["l0", "l1"] })));
        Assert.Equal(["l1"], Ids(PhrasePicker.Candidates(tiny, Context(10, 3) with { RecentIDs = ["l1", "l0"] })));
    }

    [Fact]
    public void FlavorsAndExclusionsActAsIfTheLinesWerentThere()
    {
        var tiny = new PhraseBank(
            [
                new Phrase("calm", Phrase.Line("a"), [Tag.General], flavor: FixtureIds.FlavorA),
                new Phrase("silly", Phrase.Line("b"), [Tag.General], flavor: FixtureIds.FlavorB),
                new Phrase("tune", Phrase.Tune("tune-1"), [Tag.General]),
                new Phrase("back", Phrase.Line("c"), [Tag.WelcomeBack], flavor: FixtureIds.FlavorB),
            ]
        );
        var context = Context(10, 3, chime: true) with { Flavors = new HashSet<FlavorId> { FixtureIds.FlavorA } };
        Assert.Equal(["calm", "tune"], Ids(PhrasePicker.Candidates(tiny, context)));
        context = context with { Flavors = new HashSet<FlavorId>(), ExcludedIDs = new HashSet<string> { "silly", "tune" } };
        Assert.Equal(["calm"], Ids(PhrasePicker.Candidates(tiny, context)));
        // An event whose lines are all filtered out falls back to ambient chatter, as if it had none.
        context = context with { Event = Tag.WelcomeBack };
        Assert.Equal(["back"], Ids(PhrasePicker.Candidates(tiny, context)));
        context = context with { Flavors = new HashSet<FlavorId> { FixtureIds.FlavorA } };
        Assert.Equal(["calm"], Ids(PhrasePicker.Candidates(tiny, context)));
    }

    [Fact]
    public void EmptyBankPicksNothing() => Assert.Null(PhrasePicker.Pick(new PhraseBank([]), MondayMorning, 0.5));

    [Fact]
    public void PickIsDeterministic() =>
        Assert.Equal(PhrasePicker.Pick(Bank, MondayMorning, 0.37), PhrasePicker.Pick(Bank, MondayMorning, 0.37));
}
