import DesklingCompanion

extension PhraseBank {
    /// A small bank with one of everything the picker tells apart, and placeholder texts: three general lines of
    /// flavor a (weights 1, 3 and 5) and one each of b and c, a line per daypart and weekday tag, a line per event
    /// tag (two for `welcomeBack`), a tune, a line only the raster skin says, a line that exists only in Italian, a
    /// two-part joke for the button and an Italian-only joke. Mechanism tests and `conformance/companion/` run
    /// over it; an app's own bank is pinned by its own vectors.
    public static let fixture = PhraseBank([
        // Any moment.
        Phrase("general-a1", .line(source: "Fixture line, general, flavor a, weight 1"), tags: [.general], weight: 1, flavor: a),
        Phrase("general-a2", .line(source: "Fixture line, general, flavor a, weight 3"), tags: [.general], flavor: a),
        Phrase("general-a3", .line(source: "Fixture line, general, flavor a, weight 5"), tags: [.general], weight: 5, flavor: a),
        Phrase("general-b1", .line(source: "Fixture line, general, flavor b"), tags: [.general], flavor: b),
        Phrase("general-c1", .line(source: "Fixture line, general, flavor c"), tags: [.general], flavor: c),
        // Dayparts and weekdays.
        Phrase("morning-1", .line(source: "Fixture line, morning"), tags: [.morning], flavor: a),
        Phrase("afternoon-1", .line(source: "Fixture line, afternoon"), tags: [.afternoon], flavor: b),
        Phrase("evening-1", .line(source: "Fixture line, evening"), tags: [.evening], flavor: c),
        Phrase("night-1", .line(source: "Fixture line, night"), tags: [.night], flavor: a),
        Phrase("monday-1", .line(source: "Fixture line, Monday"), tags: [.monday], flavor: b),
        Phrase("friday-1", .line(source: "Fixture line, Friday"), tags: [.friday], flavor: c),
        Phrase("weekend-1", .line(source: "Fixture line, weekend"), tags: [.weekend], flavor: a),
        // Events.
        Phrase("welcome-back-1", .line(source: "Fixture line, welcome back, flavor a"), tags: [.welcomeBack], flavor: a),
        Phrase("welcome-back-2", .line(source: "Fixture line, welcome back, flavor b"), tags: [.welcomeBack], weight: 4, flavor: b),
        Phrase("after-call-1", .line(source: "Fixture line, after a call"), tags: [.afterCall], flavor: b),
        Phrase("shush-end-1", .line(source: "Fixture line, Shh ended"), tags: [.shushEnd], flavor: c),
        Phrase("button-press-1", .line(source: "Fixture line, button press"), tags: [.buttonPress], flavor: a),
        Phrase("battery-low-1", .line(source: "Fixture line, battery low"), tags: [.batteryLow], flavor: b),
        Phrase("charging-1", .line(source: "Fixture line, charging"), tags: [.charging], flavor: c),
        Phrase("unplugged-1", .line(source: "Fixture line, unplugged"), tags: [.unplugged], flavor: a),
        Phrase("went-dark-1", .line(source: "Fixture line, went dark"), tags: [.wentDark], flavor: b),
        Phrase("went-light-1", .line(source: "Fixture line, went light"), tags: [.wentLight], flavor: c),
        // A tune, a skin's own line, a language's own line.
        Phrase("tune-1", .tune(sound: "tune-1"), tags: [.general]),
        Phrase(
            "skin-raster-only", .line(source: "Fixture line, raster skin only"), tags: [.general], flavor: b,
            skins: [FixtureIDs.rasterSkin]),
        Phrase("it-only", .line(source: "Battuta di prova, solo in italiano"), tags: [.general], flavor: c, languages: ["it"]),
        // Jokes in two beats.
        Phrase(
            "joke-1", .sequence(source: ["Fixture joke, setup", "Fixture joke, punchline"]), tags: [.buttonPress, .general],
            flavor: a),
        Phrase(
            "joke-it", .sequence(source: ["Barzelletta di prova, preparazione", "Barzelletta di prova, battuta finale"]),
            tags: [.general], flavor: c, languages: ["it"]),
    ])

    private static let a = FixtureIDs.flavorA
    private static let b = FixtureIDs.flavorB
    private static let c = FixtureIDs.flavorC
}
