TIRELESS KEEPER 1.2.0 for Graveyard Keeper 2 (BepInEx mod)
==========================================================

What the mod does
-----------------
- Health regenerates automatically (default 0.5 per second, 0 to 10). Also works during combat.
- Infinite health (toggle, default off): no more damage, health stays full.
  If a scene waits for the player to take damage or die, turn it off briefly.
- Energy regenerates automatically (default 0.2 per second, 0 to 5). The bar goes up to 100.
- Infinite energy (toggle, default off): work costs no energy, the bar stays full and no task
  fails for lack of energy, not even with high insanity. Working while sleep-deprived also
  no longer adds insanity.
- Insanity (the second bar next to energy) fades automatically (default 0.1 per second, 0 to 5).
  Insanity lowers your maximum energy (maximum = 100 - insanity), so less insanity means more
  room in the energy bar again. Some recipes require a minimum insanity. If you want to use
  them, set the reduction to 0.
- Sleep deprivation (the debuff after 2 days without sleep that makes insanity rise with every
  task) builds up slower. 1.0 = vanilla (after 2 days), 0.5 = after 4 days (default), 0 = never.
  A debuff that is already active only goes away with the next sleep.
- Sleep with full energy (toggle, default on): the game normally refuses to let you sleep with
  a full energy bar ("no need to sleep"), and it only saves when you sleep. With this option
  the bed always works; with full energy the sleep is just short, but the game still saves.
- Your work speed at tables (1.0 to 10.0, default 1.0): how much progress each of your hits makes
  when you work at a table yourself, such as workbenches, the autopsy and embalming table or the
  study table. Every hit still costs energy as usual, so a craft costs less energy in total.
  Recipes above your mastery stay locked.
- Regeneration runs in real time and only while you are playing: not in the main menu, not
  while paused and not while sleeping (the game refills your energy by itself there).
- All changes take effect immediately, no restart needed. Nothing is stored in your save game.

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
The mod will then be located at:  BepInEx/plugins/TirelessKeeper/TirelessKeeper.dll
Its language files go to:          BepInEx/plugins/GK2.Framework/Localization/playerregeneration/

Settings
--------
- In game: F1 (ConfigurationManager) or Mods in the main menu or pause menu (framework).
- File: BepInEx/config/de.gk2mods.playerregeneration.cfg

Check
-----
BepInEx/LogOutput.log should contain:
  "Tireless Keeper 1.2.0 loaded (framework: yes/no, ...)"

Translations
------------
With GK2 Mod Framework 0.1.8 or newer, the settings (F1 and the "Mods" menu) and the mod
description appear in your game language. Included: English and German.
To add a language, copy
  BepInEx/plugins/GK2.Framework/Localization/playerregeneration/en.json
to a file named after the game's language code (e.g. ko.json, ru.json, fr.json) in the same
folder and translate the texts on the right side. Section headings in the "Mods" menu stay in
English. Restart the game to see changes. Feel free to send me your translation and I will
gladly include it in the next version.

Uninstall
---------
Delete the folder BepInEx/plugins/TirelessKeeper and the folder
BepInEx/plugins/GK2.Framework/Localization/playerregeneration.
Your save games are not affected.
