"""
Pflegt die Sprachdateien der Mods (GK2 Mod Framework, ab 0.1.8).

Aufruf im Projektordner:  py -3.12 tools/localization.py

- Erzeugt <Mod>/Localization/en.json aus allen Translations.Get("schlüssel", "englischer Text")-Aufrufen im Code.
  en.json ist die Vorlage für Übersetzer. Sie muss zum Code passen, denn das Framework nimmt en.json vor dem Text im
  Code – deshalb nach jeder Textänderung dieses Skript laufen lassen, nicht en.json von Hand bearbeiten.
- Prüft alle anderen Sprachdateien (de.json, ko.json, ...) auf fehlende und veraltete Schlüssel.
- Beendet sich mit Fehlercode 1, wenn ein Schlüssel im Code mit zwei verschiedenen Texten vorkommt.
"""
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
LIT = r'"(?:[^"\\]|\\.)*"'
CALL = re.compile(r'Translations\.Get\(\s*(' + LIT + r')\s*,\s*(' + LIT + r'(?:\s*\+\s*' + LIT + r')*)\s*\)')
ESCAPES = {'"': '"', "\\": "\\", "'": "'", "n": "\n", "t": "\t", "r": "\r", "0": "\0"}


def unescape(literal):
    body = literal[1:-1]
    return re.sub(r'\\(u[0-9a-fA-F]{4}|.)',
                  lambda m: chr(int(m[1][1:], 16)) if m[1][0] == "u" and len(m[1]) == 5 else ESCAPES.get(m[1], m[1]),
                  body)


def extract(project):
    texts, errors = {}, []
    # Plugin.cs zuerst: Dort stehen die Einstellungen in der Reihenfolge des F1-Menüs.
    for source in sorted(project.glob("*.cs"), key=lambda p: (p.name != "Plugin.cs", p.name)):
        for m in CALL.finditer(source.read_text(encoding="utf-8")):
            key = unescape(m[1])
            text = "".join(unescape(l) for l in re.findall(LIT, m[2]))
            if key in texts and texts[key] != text:
                errors.append(f"{source.name}: key '{key}' has two different English texts")
            texts.setdefault(key, text)

    # Für Übersetzer: Mod-Beschreibung zuerst, dann je Einstellung Name, Beschreibung, Kurzhinweis beieinander.
    groups = list(dict.fromkeys(k.rsplit(".", 1)[0] for k in texts))
    suffixes = ["name", "description", "hint"]
    def order(key):
        group, _, suffix = key.rpartition(".")
        rank = suffixes.index(suffix) if suffix in suffixes else len(suffixes)
        return (key != "mod.description", groups.index(group), rank, key)
    return {k: texts[k] for k in sorted(texts, key=order)}, errors


def main():
    failed = False
    for project in sorted(p.parent for p in ROOT.glob("*/Translations.cs")):
        texts, errors = extract(project)
        folder = project / "Localization"
        folder.mkdir(exist_ok=True)
        en_file = folder / "en.json"
        previous = json.loads(en_file.read_text(encoding="utf-8")) if en_file.exists() else {}
        with open(en_file, "w", encoding="utf-8", newline="\n") as f:
            json.dump(texts, f, ensure_ascii=False, indent=2)
            f.write("\n")
        print(f"{project.name}: en.json with {len(texts)} keys")
        for error in errors:
            print("  ERROR " + error)
            failed = True
        # Gleicher Schlüssel, neuer englischer Text: Die Übersetzungen passen dann womöglich nicht mehr.
        for key in texts:
            if key in previous and previous[key] != texts[key]:
                print(f"  changed English text: {key} – check the translations")

        for language in sorted(folder.glob("*.json")):
            if language.name == "en.json":
                continue
            translated = json.loads(language.read_text(encoding="utf-8"))
            missing = [k for k in texts if not translated.get(k)]
            obsolete = [k for k in translated if k not in texts]
            state = "complete" if not missing and not obsolete else "needs work"
            print(f"  {language.name}: {len(texts) - len(missing)}/{len(texts)} translated, {state}")
            for k in missing:
                print(f"    missing:  {k}")
            for k in obsolete:
                print(f"    obsolete: {k}")
    sys.exit(1 if failed else 0)


if __name__ == "__main__":
    main()
