RESURRECT EVERY DAY 1.2.0 for Graveyard Keeper 2 (BepInEx mod)
==============================================================

What the mod does
-----------------
Normally you can only resurrect zombies on the Day of Envy: only then does the power switch in
the resurrection lab work, and its storm power recharges just once a week (at the end of the Day
of Lust) and is used up by a resurrection. The only exception is the "God of the storms" buff
from the Prayer for Storms.

The PR Machine, which raises your zombie limit, has a similar lock: it can only be used on the
Day of Gluttony.

- Resurrect on any day (toggle, default on): the power switch works on all six days of the week.
- Unlimited storm power (toggle, default on): the lab always has power, so you can resurrect as
  often as you like. Off = vanilla: the power recharges once a week and is used up by a
  resurrection.
- PR Machine on any day (toggle, default on): the PR Machine can be used on all six days of the
  week, so you can raise the zombie limit again as soon as the previous run has ended. A run still
  ends at the end of a Day of Wrath, like in vanilla. Started on the Day of Wrath, its first week
  is over the same evening.
- Everything else stays as usual: you still prepare the resurrection tables with a body, all main
  organs, the resurrection liquid and a collar, and then pull the power switch.
- With unlimited power on, the mod does not use up your weekly vanilla power, so your save is left
  exactly as it would be without the mod.
- All changes take effect immediately, no restart needed.

Requirements
------------
1. BepInEx 5.4.23.5 (x64) installed in the game folder, game started once.

Optional (for in-game settings):
2. GK2 Mod Framework 0.1.12 (Nexus Mods) -> toggles in the "Mods" menu (main menu and pause menu).
3. BepInEx.ConfigurationManager v19.0 (file "..._BepInEx5_v19.0.zip") -> settings via F1.
   Both can be installed at the same time; the values always stay in sync.

Installation
------------
Copy the "BepInEx" folder from this archive into the Graveyard Keeper 2 game folder.
The mod will then be located at:  BepInEx/plugins/ResurrectEveryDay/ResurrectEveryDay.dll
Its language files go to:          BepInEx/plugins/GK2.Framework/Localization/resurrecteveryday/

Settings
--------
- In game: F1 (ConfigurationManager) or Mods in the main menu or pause menu (framework).
- File: BepInEx/config/de.gk2mods.resurrecteveryday.cfg

Check
-----
BepInEx/LogOutput.log should contain:
  "Day check hooked."
  "Power check redirected."
  "PR Machine day check redirected."
  "Resurrect Every Day 1.2.0 loaded (framework: yes/no, ...)"
If one of them says "... not found" instead, a game update has changed the code and that part
of the mod has no effect.

Translations
------------
With GK2 Mod Framework 0.1.8 or newer, the settings (F1 and the "Mods" menu) and the mod
description appear in your game language. Included: English and German.
To add a language, copy
  BepInEx/plugins/GK2.Framework/Localization/resurrecteveryday/en.json
to a file named after the game's language code (e.g. ko.json, ru.json, fr.json) in the same
folder and translate the texts on the right side. Section headings in the "Mods" menu stay in
English. Restart the game to see changes. Feel free to send me your translation and I will
gladly include it in the next version.

Uninstall
---------
Delete the folder BepInEx/plugins/ResurrectEveryDay and the folder
BepInEx/plugins/GK2.Framework/Localization/resurrecteveryday.
Your save games are not affected.
