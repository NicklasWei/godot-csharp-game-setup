# 0005: Key-based gettext PO localization

**Status:** Accepted (__DATE__)

## Context

The game ships in __LOCALES__. Some languages need three or more plural forms, which Godot's CSV translations
don't support. English text used as keys breaks every translation when the English wording changes.

## Decision

- gettext PO files in `localization/`, one per locale, registered in `project.godot`.
- **Keys:** the `msgid` is a **stable key** (`ui.title.build`). `en.po` holds the English text and defines which keys
  exist. Code uses `L10nKeys` constants; content keys come from `L10nKeyBuilder`; scene labels hold the key and
  translate automatically.
- **Untranslated entries** keep an empty `msgstr` and `#, fuzzy`, and Godot falls back to English. Never
  machine-translate silently.
- **Tests enforce:** every code key exists in `en.po`; every content definition's keys exist; no orphan keys; every
  translatable scene text is a key; all locales have the same key set; untranslated entries are fuzzy; placeholders
  match across locales; every catalog is registered.
- **Pseudolocalization** at runtime with `-- --pseudoloc`. Exports include the TextServer ICU data for CJK line
  breaking (`internationalization/locale/include_text_server_data=true`).

## Consequences

- Adding a string means touching `L10nKeys` (or a scene) and every PO file; the tests catch omissions.
- Adding a locale is adding its PO file (every key, fuzzy) and registering it; the tests find it.
