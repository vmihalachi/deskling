---
name: voice
description: Record, re-record or audition a Laching companion's voices with ElevenLabs (laching/tools/voice.py), see which lines and joke parts are pending a voice, add a voiced language, pick a voice, fix a clip that sounds wrong, or promote a language from testing to shipping. Use after adding or changing a phrase or its translation, or when asked about the character's voice, speech quality, ElevenLabs, voice design or the Voice Library.
---

# The character's voices

The character never says free text: every line is a catalog string, so each one is recorded once at dev time with
ElevenLabs and shipped in the bundle (`<voice.dir>/Voice/<lang>/<voice>/<clip id>.m4a` plus `manifest.json`; the
Windows app links the same files). Clip ids: `<id>` for a line, `<id>.1`, `<id>.2`, … for a joke's parts (one clip per
part), plus `voice.extraClips` (UI strings the character says). The voices folder is a submodule (`voice.repo`):
commit the clips there, push, then commit the new pointer in the app (`voice.py publish` does all three). The app makes
no network requests and never touches the microphone; only `voice.py` talks to ElevenLabs, sending nothing but text.
The voices' names, the rubric for choosing one and the voices chosen so far are in the app's `DESIGN.md` and
`.agents/notes/voice.md`.

## Rules

- **Never read the voice manifest or edit anything under `<voice.dir>/Voice/`** (the hook refuses). Use
  `python3 laching/tools/voice.py status`.
- **Pending lines are fine; users never see them.** A clip is *voiced* when it exists and its `textHash` matches the
  current catalog text; otherwise it's *pending* (*missing* or *stale*) and the app never picks the phrase in that
  voice (a joke is pending while any part is). Debug builds can fall back to the system voice instead
  (`VoiceLibrary(fallbackToSystemVoice: true)`). `check` fails only on a broken manifest, orphans or a pending
  *required* clip (`voice.required`) in a `shipping` language.
- **Language-scoped phrases** are recorded only in their languages; `status` doesn't list them elsewhere.
- **Credits are real money.** Always `render … --dry-run` first and state the character count (= credits) before
  `--yes`. Render only final text.
- **The API key never passes through you.** It lives in the login Keychain (service `voice.keychainService`) or in
  `ELEVENLABS_API_KEY`. If it's missing, give the owner
  `security add-generic-password -a "$USER" -s <voice.keychainService> -w`. Never ask for the key in chat, print it or
  commit it. It needs Text to Speech, Voices (write) and Voice Generation, nothing else.
- **Original.** Voice Design prompts end with "not imitating any existing character". Never clone a real person's
  voice. A Voice Library voice is a licensed voice actor whose owner can withdraw it after its notice period (clips
  already rendered stay licensed), so render everything while it's listed.
- Audio made during a paid ElevenLabs month stays commercially licensed after cancelling; alpha/beta models' output
  doesn't. The config's `model` must be a GA model. Before an important change (a new language, a design round, a full
  render), check ElevenLabs for newer GA models (`GET /v1/models`) and tell the owner; never switch on your own (a new
  model means re-rendering every clip).

## After a phrase or translation changes

1. `python3 laching/tools/voice.py status` lists the clip as *missing* or *stale* per voiced language and voice. If
   the text isn't final yet, stop: the phrase simply waits, unheard.
2. Delivery tags where they help (`[whispers]`, `[softly]`…) and `gain_db` go in `voice.direction`.
3. Per voiced language and voice: `voice.py render --lang <lang> --voice <voice> --dry-run`, state the credits, then
   `--yes` (with the owner's go-ahead for anything beyond a handful of clips).
4. `voice.py review [--lang <lang>]` writes `<mac.dir>/build/voice/review/<lang>.html`: every clip in bank order (a
   joke's parts together), the voices side by side, a listen-through mode with keyboard flags (R retake, T text, N
   note) and a "Copy notes for Claude" summary the owner pastes back. The friends' copy lives in the voices repo
   (`review/<lang>.html`, refreshed by `render`, `retake` and `site`).
5. From the summary: `Retake <lang>/<voice> (…): id, id` → `voice.py retake id,id --lang <lang> --voice <voice>` (a
   new seed; shows the credits, add `--yes`). A mispronounced word: a `replace` entry for the language in the config,
   then render. A text change: the `localization` skill.
6. `voice.py publish -m "<what changed>"`: checks, refreshes the site, commits and pushes the voices repo, stages its
   pointer in the app; commit that with the change that needed it. Never commit a pointer to an unpushed voices commit.

## Finding a voice for a language

Listening pages land in `<mac.dir>/build/voice/design/<lang>/<voice>/index.html`; each candidate reads the config's
`design_lines` in that language, so the language needs its translations first.

- **Design your own**: `voice.py design --lang <lang> --voice <voice> --description "…"` (three candidates, a few
  hundred credits).
- **Voice Library**: `voice.py library --lang <lang> --voice <voice> --sample 4` (`--search`, `--use-case` narrow it);
  prefer a long notice period and fewer users.
- **Any voice id**: `voice.py sample <voice_id> --lang <lang> --voice <voice> --name <slug>`.
- Then `voice.py adopt <generated_voice_id> …` keeps a design candidate, or `voice.py use <voice_id> … --name <Name>`.

## Adding a voiced language

1. Its text is translated (`localization` skill; `l10n.py check` ok) and fits every voice speaking it.
2. Add it to the voice config's `languages` with `"status": "testing"` and a `default` voice; find its voices.
3. Render with `--dry-run` first. The owner listens to the Dev build in that language; when happy, set
   `"status": "shipping"`, render again (the manifest picks up the status) and run `check`.
