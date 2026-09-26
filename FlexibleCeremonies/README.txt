SERMON EVERY DAY 1.2.0 for Graveyard Keeper 2 (BepInEx mod)
===========================================================

What the mod does
-----------------
Normally you can only hold one sermon (at the prayer stand in the church) per week, and only on
the Day of Wrath; on any other day the game tells you it is only available on the Day of Wrath.

- Sermons on any day (toggle, default on): sermons on all six days of the week.
- Sermons per day (0 to 10, default 0 = unlimited): limit per in-game day. Also applies on the
  Day of Wrath.
- Every sermon still consumes Gratitude equal to the number of parishioners, as usual
  (parishioners = the lower of Gratitude and church quality). If you hold several sermons in a
  row, you need enough Gratitude for enough parishioners.
- Ignore Gratitude (toggle, default off): sermons no longer depend on Gratitude. The number of
  parishioners (and with it the success chance) is set by church quality alone, and no
  Gratitude is consumed. Takes effect the next time you open the prayer window.
- Once the limit is reached, the game shows its usual "available on the Day of Wrath" message.
  The next day it works again.
- The mod does not change any save game value. The "per day" counter is kept in memory only and
  starts over for the current day after loading a save.
- All changes take effect immediately, no restart needed.

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
The mod will then be located at:  BepInEx/plugins/SermonEveryDay/SermonEveryDay.dll
Its language files go to:          BepInEx/plugins/GK2.Framework/Localization/flexibleceremonies/

Settings
--------
- In game: F1 (ConfigurationManager) or Mods in the main menu or pause menu (framework).
- File: BepInEx/config/de.gk2mods.flexibleceremonies.cfg

Check
-----
BepInEx/LogOutput.log should contain:
  "Prayer stand check redirected."
  "Gratitude cost redirected."
  "Sermon Every Day 1.2.0 loaded (framework: yes/no, ...)"
If one of them says "... not found" instead, a game update has changed the code and that part
of the mod has no effect.

Translations
------------
With GK2 Mod Framework 0.1.8 or newer, the settings (F1 and the "Mods" menu) and the mod
description appear in your game language. Included: English and German.
To add a language, copy
  BepInEx/plugins/GK2.Framework/Localization/flexibleceremonies/en.json
to a file named after the game's language code (e.g. ko.json, ru.json, fr.json) in the same
folder and translate the texts on the right side. Section headings in the "Mods" menu stay in
English. Restart the game to see changes. Feel free to send me your translation and I will
gladly include it in the next version.

Uninstall
---------
Delete the folder BepInEx/plugins/SermonEveryDay and the folder
BepInEx/plugins/GK2.Framework/Localization/flexibleceremonies.
Your save games are not affected.
