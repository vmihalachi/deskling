import Foundation

/// Picks the phrase that fits the moment. Pure: the same bank, context and random unit always give the same
/// phrase, and `conformance/companion/picker` pins it.
///
/// Rules: phrases that don't exist in the context's language are dropped first, then excluded ids, lines the
/// current skin can't say and lines whose flavor is turned off, as if they weren't in the bank;
/// an event's own lines win when there is an event (falling back to ambient lines when it has none);
/// ambient chatter draws from `general` plus the current daypart and weekday tags, never from event lines;
/// tunes only with the chime on; the last `recentWindow` ids are skipped unless that would leave nothing (then
/// only the very last one is); a line whose daypart tag matches the hour counts double; the draw is weighted,
/// in bank order, from one uniform unit in `[0, 1)`. A sequence is one candidate like a line.
public enum PhrasePicker {
    /// How many recent lines are kept out of the draw.
    public static let recentWindow = 12

    public static func pick(from bank: PhraseBank, context: PhraseContext, unit: Double) -> Phrase? {
        let candidates = candidates(in: bank, context: context)
        guard !candidates.isEmpty else { return nil }
        let daypart = Tag.daypart(hour: context.hour)
        let weights = candidates.map { $0.weight * ($0.tags.contains(daypart) ? 2 : 1) }
        let total = weights.reduce(0, +)
        var remaining = Int((unit * Double(total)).rounded(.down))
        remaining = min(max(remaining, 0), total - 1)
        for (phrase, weight) in zip(candidates, weights) {
            if remaining < weight { return phrase }
            remaining -= weight
        }
        return candidates.last
    }

    /// The phrases the draw chooses from, in bank order.
    public static func candidates(in bank: PhraseBank, context: PhraseContext) -> [Phrase] {
        let available = bank.phrases(in: context.language).filter { phrase in
            guard !context.excludedIDs.contains(phrase.id), phrase.fits(context.skin) else { return false }
            guard let flavor = phrase.flavor, !context.flavors.isEmpty else { return true }
            return context.flavors.contains(flavor)
        }
        var pool: [Phrase]
        if let event = context.event, available.contains(where: { $0.tags.contains(event) }) {
            pool = available.filter { $0.tags.contains(event) }
        } else {
            var tags: Set<Tag> = [.general, Tag.daypart(hour: context.hour)]
            if let weekdayTag = Tag.weekdayTag(weekday: context.weekday) { tags.insert(weekdayTag) }
            pool = available.filter { !$0.hasEventTag && !$0.tags.isDisjoint(with: tags) }
        }
        if !context.chimeEnabled {
            pool.removeAll { $0.isTune }
        }
        let recent = Set(context.recentIDs.suffix(recentWindow))
        let fresh = pool.filter { !recent.contains($0.id) }
        if !fresh.isEmpty { return fresh }
        if let last = context.recentIDs.last {
            let notLast = pool.filter { $0.id != last }
            if !notLast.isEmpty { return notLast }
        }
        return pool
    }
}
