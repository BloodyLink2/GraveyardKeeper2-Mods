BIGGER STORAGE 1.0.0 for Graveyard Keeper 2 (BepInEx mod)
=========================================================

What the mod does
-----------------
- Extra chest slots (0 to 100, default 10): every chest gets this many slots on top of its normal
  size. This applies to all chests you can open (also the ones you already built), but not to
  conveyor chests, fuel storages, wells and other objects with storage.
- Stack size (0 to 300 in steps of 10, default 0 = normal): how many items fit on one slot, in
  every inventory: yours, chests and stations. Items that already stack higher keep their size,
  items that do not stack (like tools) stay single. A stack size only for chests is not possible:
  the game uses the same stack size everywhere, so a chest-only rule would risk losing or
  duplicating items when you move them around.
- Prepare for uninstall (toggle, default off): turn this on before you remove the mod. Chests go
  back to their normal size and stack sizes to normal, whatever else is set. A chest that still
  holds more items than its normal size keeps the slots it needs and shrinks as soon as you take
  items out; a message on screen tells you how many chests are still too full, and when all of
  them are back to normal.
- All changes take effect immediately, no restart needed.

Good to know
------------
- The chest size is stored in your save game, like everything else about the chest. Lowering
  "Extra chest slots" (or turning the mod off) shrinks chests again, but never below the slots in
  use, so nothing gets lost.
- Lowering the stack size (or turning the mod off) keeps bigger stacks you already have; they just
  do not grow anymore.
- To remove the mod: turn on "Prepare for uninstall", take items out of the chests the message
  tells you about, sleep once so the game saves, then uninstall. Bigger stacks you already have
  stay as they are; the game handles them fine without the mod.

Requirements
------------
1. BepInEx 5.4.23.5 (x64) installed in the game folder, game started once.

Optional (for in-game settings):
2. GK2 Mod Framework 0.1.12 (Nexus Mods) -> sliders in the "Mods" menu (main menu and pause menu).
3. BepInEx.ConfigurationManager v19.0 (file "..._BepInEx5_v19.0.zip") -> settings via F1.
   Both can be installed at the same time; the values always stay in sync.

Installation
------------
Copy the "BepInEx" folder from this archive into the Graveyard Keeper 2 game folder.
The mod will then be located at:  BepInEx/plugins/BiggerStorage/BiggerStorage.dll
Its language files go to:          BepInEx/plugins/GK2.Framework/Localization/biggerstorage/

Settings
--------
- In game: F1 (ConfigurationManager) or Mods in the main menu or pause menu (framework).
- File: BepInEx/config/de.gk2mods.biggerstorage.cfg

Check
-----
BepInEx/LogOutput.log should contain:
  "Bigger Storage 1.0.0 loaded (framework: yes/no, ...)"
After loading a save it shows how many chests were resized, e.g. "Resized 12 chest(s) (extra slots: +10)."

Translations
------------
With GK2 Mod Framework 0.1.8 or newer, the settings (F1 and the "Mods" menu) and the mod
description appear in your game language. Included: English and German.
To add a language, copy
  BepInEx/plugins/GK2.Framework/Localization/biggerstorage/en.json
to a file named after the game's language code (e.g. ko.json, ru.json, fr.json) in the same
folder and translate the texts on the right side. Section headings in the "Mods" menu stay in
English. Restart the game to see changes. Feel free to send me your translation and I will
gladly include it in the next version.

Uninstall
---------
See "Good to know" above, then delete the folder BepInEx/plugins/BiggerStorage and the folder
BepInEx/plugins/GK2.Framework/Localization/biggerstorage.
