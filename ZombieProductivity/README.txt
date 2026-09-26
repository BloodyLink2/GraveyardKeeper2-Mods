ZOMBIE PRODUCTIVITY 1.1.0 for Graveyard Keeper 2 (BepInEx mod)
==============================================================

What the mod does
-----------------
- Work speed (0.1 to 10.0 in 0.1 steps, default 1.0 = normal): zombies at workbenches (also
  conveyor zombies and gardeners when sowing and harvesting) work faster or slower. The game
  lets a zombie hit at most once per work check, so just speeding up time stops at about 5x.
  This mod also carries out the extra hits, so up to 10x really works.
- Mastery bonus (0 to 10 levels, default 0): extra mastery levels for working zombies. This is
  not a multiplier: +7 means seven mastery levels on top of the zombie's own. Higher mastery
  lets zombies make recipes above their level and gives more progress per hit (mastery divided
  by the recipe level, and the game allows at most 3 cells per hit).
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
The mod will then be located at:  BepInEx/plugins/ZombieProductivity/ZombieProductivity.dll
Its language files go to:          BepInEx/plugins/GK2.Framework/Localization/zombieproductivity/

Settings
--------
- In game: F1 (ConfigurationManager) or Mods in the main menu or pause menu (framework).
- File: BepInEx/config/de.gk2mods.zombieproductivity.cfg

Check
-----
BepInEx/LogOutput.log should contain:
  "Zombie Productivity 1.1.0 loaded (framework: yes/no, ...)"

Translations
------------
With GK2 Mod Framework 0.1.8 or newer, the settings (F1 and the "Mods" menu) and the mod
description appear in your game language. Included: English and German.
To add a language, copy
  BepInEx/plugins/GK2.Framework/Localization/zombieproductivity/en.json
to a file named after the game's language code (e.g. ko.json, ru.json, fr.json) in the same
folder and translate the texts on the right side. Section headings in the "Mods" menu stay in
English. Restart the game to see changes. Feel free to send me your translation and I will
gladly include it in the next version.

Uninstall
---------
Delete the folder BepInEx/plugins/ZombieProductivity and the folder
BepInEx/plugins/GK2.Framework/Localization/zombieproductivity.
Your save games are not affected.
