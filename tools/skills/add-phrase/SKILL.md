---
name: add-phrase
description: Add, change or retire one of the character's lines, two-part jokes or tunes in a Deskling companion app (the Swift PhraseBank, tags, weights, flavors, skins and languages, the catalog keys, translations, recordings and the conformance vectors the Windows port replays). Use when asked for new phrases, lines, jokes, greetings, reactions or tunes, or to change when a phrase can play.
---

# Add a phrase

The bank lives in the app's Swift core (`mac.core` in companion.json) as `PhraseBank` data, one `Phrase` per entry: a
stable `id`, a `kind`, `tags` that say when it may play, a `weight` (1–5), a `flavor`, optional `skins` and
`languages`. The Windows port loads the same bank from the generated `<conformance.dir>/phrases.json`, so the Swift
list is the only place to edit. What the character sounds like (length, register, what it never says, examples) is in
the app's `DESIGN.md` and `.agents/notes/add-phrase.md`: read them before writing a line.

## Kinds

- `.line(source: "…")`: one catalog key, `phrase.<id>`. `source` is the catalog's source text.
- `.sequence(source: ["Setup…", "Punchline…"])`: a joke told in beats, at least two parts, keys `phrase.<id>.1`,
  `.2`, …, each recorded as its own clip (`<id>.1`, `<id>.2`). The app speaks part 1, waits a beat
  (`PhraseTiming.partBeat`), then part 2; the bubble follows the parts, the notification shows both lines. Recents,
  muting and the quiet rules treat the joke as one phrase. Keep each part speakable on its own and the punchline last.
- `.tune(sound: "…")`: a sound file from `deskling/tools/synth.py` (the app's `sounds.recipes`); chime channel only,
  no flavor, no text.

## Steps

1. **Add the entry**: a new `id` (lowercase, `-` separated, never reused or renamed once shipped), its `tags` (event
   tags `welcomeBack`, `afterCall`, `shushEnd`, `buttonPress`, `batteryLow`, `charging`, `unplugged`, `wentDark`,
   `wentLight` win over ambient ones; daypart tags double the weight when they match), a `weight` (3 normal, 1 rare,
   5 a favorite) and a `flavor` from the app's list.
   **Who can say it:** leave `skins` empty when every skin could say it; list the skins whose body or nature it
   names. **Where it exists:** leave `languages` empty for a phrase that works in every language; list base codes
   (`["it"]`) for one that only works in some (a pun): write its source text in that language.
2. **Vectors**: `deskling/tools/conformance.sh` (or the app's Regenerate vectors workflow) regenerates
   `phrases.json` and the picker and quiet vectors that drew the changed lines. Commit them with the change, then run
   the Windows Core tests (the port reads the new bank automatically).
3. **Catalog keys**: `python3 deskling/tools/phrases.py sync` adds the missing keys (a joke's parts as separate keys
   with "Part n of m" comments; a scoped phrase's comment names its languages), updates changed source text (dropping
   that key's translations) and removes retired keys. `phrases.py check` runs in `verify.sh` and CI.
4. **Tests**: a new tag or rule gets a `PhrasePicker` test in the app; otherwise the vectors cover it. If the app runs
   `BankAudit.floors`, keep every (language, skin, tag) above its floor.
5. **Translate**: steps 3–7 of the `localization` skill (scoped keys are only asked for in their languages).
6. **Voice**: the new phrase stays unheard (pending) in every voiced language until each clip is recorded; a joke
   waits for all its parts. Once text and translations are final: the `voice` skill (`voice.py status`, then
   `render --dry-run`).
7. **Retiring**: remove the entry, regenerate the vectors, `phrases.py sync` drops its keys, and
   `voice.py render --lang <lang> --voice <voice> --prune` its clips. Keep the `id` out of circulation.
