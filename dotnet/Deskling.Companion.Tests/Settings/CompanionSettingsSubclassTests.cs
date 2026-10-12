using Deskling.Companion.Cadence;
using Deskling.Companion.Coordination;
using Deskling.Companion.Settings;
using Deskling.Core.Settings;

namespace Deskling.Companion.Tests;

/// <summary>The subclass from <see cref="CompanionSettings"/>' doc comment, as an app writes it in its own assembly.</summary>
public sealed class AppSettings : CompanionSettings
{
    private string? voice;

    public AppSettings(ISettingsStore store, CompanionDefaults defaults)
        : base(store, defaults) => voice = store.GetString("voice");

    public string? Voice
    {
        get => voice;
        set => SetProperty(ref voice, value, () => SetOrRemove("voice", value));
    }

    public override void ResetToDefaults()
    {
        base.ResetToDefaults();
        Voice = null;
    }
}

public sealed class CompanionSettingsSubclassTests
{
    [Fact]
    public void AnAppsOwnKeyWritesThroughAndResets()
    {
        var store = new InMemorySettingsStore();
        var settings = new AppSettings(store, Fixture.Defaults);
        Assert.Null(settings.Voice);
        settings.Voice = "female";
        settings.Frequency = Frequency.Chatty;
        Assert.Equal("female", store.GetString("voice"));
        Assert.Equal("female", new AppSettings(store, Fixture.Defaults).Voice);
        settings.ResetToDefaults();
        Assert.Null(settings.Voice);
        Assert.Equal(Frequency.Rarely, settings.Frequency);
        Assert.Null(store.GetString("voice"));
    }

    [Fact]
    public void BothTheBasesAndTheSubclasssPropertiesNotify()
    {
        var settings = new AppSettings(new InMemorySettingsStore(), Fixture.Defaults);
        var changed = new List<string?>();
        settings.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        settings.CommentOnPower = false;
        settings.Voice = "male";
        Assert.Equal([nameof(CompanionSettings.CommentOnPower), nameof(AppSettings.Voice)], changed);
    }

    [Fact]
    public void TheCoordinatorTakesTheSubclass()
    {
        var settings = new AppSettings(new InMemorySettingsStore(), Fixture.Defaults);
        var coordinator = new CompanionCoordinator(
            settings,
            Fixture.Bank,
            "en",
            new MockClock(DateTimeOffset.UnixEpoch),
            new MockIdle(),
            new MockBusy(),
            TimeZoneInfo.Utc,
            1
        );
        Assert.Same(settings, coordinator.Settings);
    }
}
