GRAVEYARD AUTOMATION 1.8.1 for Graveyard Keeper 2 (BepInEx mod)
===============================================================

What the mod does
-----------------
Finished items go straight into a chest next to the workbench or station instead of waiting for
you to pick them up.

- Zombie workbenches (toggle, default on): normally a zombie worker holds the finished item and
  waits until you (or a Caretaker zombie) pick it up. With this mod the item goes into a nearby
  chest within a second and the zombie keeps working right away.
- Zombie ingredients from chests (toggle, default on): a zombie worker that is missing an
  ingredient normally waits until another zombie brings it. With this option it takes the
  ingredients straight from the storages within the search radius, so zombie workbenches run
  without a second zombie. Ingredients another zombie is already bringing are left to it; if there
  is not enough nearby, the zombie waits as usual.
- Zombie ingredients from all areas (toggle, default off): an addition to the option above. If the
  storages within the search radius do not hold enough of an ingredient, the zombie also takes it
  from the other storages in its area and from the chests in all other areas, so nothing has to be
  carried between areas. Storages nearby are used first. From other areas only real chests are
  used, not wells or the storages for logs, stone and marble.
- Auto-crafting stations (toggle, default on): stations that craft on their own (e.g. ovens) keep
  their finished items inside until you collect them, and stop until then. With this mod they are
  collected automatically, the items go into a nearby chest and the station keeps running, even
  while you are somewhere else.
- Collect research points (toggle, default on): the research points (the orbs) of stations the
  mod collects for you fly straight to you, just like when you sleep, wherever you are. Off =
  they drop on the ground next to the station, just like when you collect by hand; pick them up
  whenever you pass by. Zombie workers keep their research points as usual (they spend them on
  their own skills).
- Empty wells into chests (toggle, default on): an upgraded well fills up with water on its own
  and stops once it is full. Every few seconds the water is moved into a chest within the search
  radius, so the well keeps producing. Stations in the area can use the water from the chest just
  like from the well.
- Harvest beehives (toggle, default on): a full beehive waits for you to harvest it. The mod
  harvests it for you, as if you did it yourself: the honey and beeswax go into a chest within the
  search radius, and the hive starts filling up again. Its research points follow "Collect
  research points". Wild honey nests are left alone.
- Refuel from chests (toggle, default on): when a station waits because it is out of fuel, and a
  fuel storage in the same area can make that fuel from materials in your chests (e.g. wood), the
  mod starts that fuel craft for you, but only as much as the station needs and only when the fuel
  storage is idle.
- Repeat recipes (toggle, default on): when you add a recipe with count 1 to the queue of an oven
  or a zombie workbench, it is set to 999, so the station keeps producing as long as there are
  ingredients and fuel materials in your chests. Pick a different count in the craft window to
  keep it, or lower it with "-" in the queue (hold to count fast). Instant recipes, fuel, recipes
  that only allow one at a time and recipes that need something placed into the station by hand
  (e.g. a body) are left alone.
- Pick recipes from nearby chests (toggle, default off): Stardew style. An idle oven (or any other
  station that works on its own) looks into the chests within the search radius every few
  seconds. If all ingredients of one of its recipes are there (fuel may come from the fuel
  storage), it crafts that recipe once, then checks again. So it keeps running and switches
  between all its unlocked recipes: first recipes that no other station in the area is making right
  now (several ovens share the work instead of all making the same), then the one whose result you
  have the least of in your chests, then the one that uses more different ingredients. The station
  takes these ingredients from the nearby chests first. Finished items may go into the same chest;
  if they are ingredients of another recipe of that station, it will process them further.
- Pick recipes for zombie workbenches (toggle, default off): the same for workbenches with a
  zombie worker. When the zombie has nothing to do, it picks a recipe it can make (right tool and
  enough mastery) whose ingredients are all in the storages within the search radius, crafts it
  once and then picks again. Recipes no other station in the area is making come first,
  then the one whose result you have the least of. Together with "Zombie ingredients from chests" and "Zombie
  workbenches", a zombie workbench runs completely on its own.
- Station speed (1.0 to 10.0, default 1.0): how fast stations that craft on their own work,
  such as ovens and distillers. Only the actual production is sped up. Garden beds, town
  building construction, special machines with their own timing and stations with a zombie
  worker are not affected (for zombies, see Zombie Productivity). Neither are tables you work at
  yourself (workbenches, the autopsy and embalming table): there you make the progress with your
  own hits (to speed those up, see Tireless Keeper).
- Chest search radius (0 to 20, default 3): how far the chest may be from the station, measured
  center to center (roughly 1 per tile). 0 = any chest in the same area. Chests that already hold
  the item are preferred, otherwise the nearest one is used.
- Automated stations (one switch per kind of station, default on): after loading a save, the
  settings list every kind of station the mod automates in that save (e.g. ovens, compost heaps,
  wells, beehives or a workbench with a zombie worker) under "Automated stations" (F1) or "Stations"
  (Mods menu). Turn one off and the mod leaves that kind of station alone: no delivery into
  chests, no ingredients from chests, no refuelling, no picked or repeated recipes and no extra
  speed. Upgrades with the same name show their id, e.g. "Oven (furnace_2)". Kinds you build later
  are added as soon as the mod first works with them.
- If there is no chest in range or the chest is full, everything works as usual. Once the chest
  has room again, waiting items are moved in automatically.
- Drop on the ground if no chest (toggle, default off): if there is no chest with room within the
  search radius, finished items drop on the ground next to the station instead, so ovens and
  zombies keep working without waiting for you.
- Only real chests are used. Other objects with storage, like the upgraded well, never receive
  items. Big items (crates and the like) still drop on the ground as usual.
  Conveyor chests are never used.
- The mod stores no data of its own in your save game. All changes take effect immediately.

Requirements
------------
1. BepInEx 5.4.23.5 (x64) installed in the game folder, game started once.

Optional (for in-game settings):
2. GK2 Mod Framework 0.1.12 (Nexus Mods) -> settings in the "Mods" menu (main menu and pause menu).
3. BepInEx.ConfigurationManager v19.0 (file "..._BepInEx5_v19.0.zip") -> settings via F1.
   Both can be installed at the same time; the values always stay in sync.

Installation
------------
Copy the "BepInEx" folder from this archive into the Graveyard Keeper 2 game folder.
The mod will then be located at:  BepInEx/plugins/GraveyardAutomation/GraveyardAutomation.dll
Its language files go to:          BepInEx/plugins/GK2.Framework/Localization/graveyardautomation/

Settings
--------
- In game: F1 (ConfigurationManager) or Mods in the main menu or pause menu (framework).
- File: BepInEx/config/de.gk2mods.graveyardautomation.cfg

Check
-----
BepInEx/LogOutput.log should contain:
  "Graveyard Automation 1.8.1 loaded (framework: yes/no, ...)"
The first time a station delivers into a chest, the log shows which chest it uses and how far
away it is, e.g. "workbench_1 delivers into chest_1 (2.1 away)".
If a finished station cannot deliver, the log says why (once per station), e.g.
"oven_1 is ready, but no chest with room within the search radius (nearest chest is 3.4 away)".
Then move the chest closer or raise the search radius.

Translations
------------
With GK2 Mod Framework 0.1.8 or newer, the settings (F1 and the "Mods" menu) and the mod
description appear in your game language. Included: English, German and Russian (Russian
translation by Alex5Not).
To add a language, copy
  BepInEx/plugins/GK2.Framework/Localization/graveyardautomation/en.json
to a file named after the game's language code (e.g. ko.json, fr.json, es.json) in the same
folder and translate the texts on the right side. Section headings in the "Mods" menu stay in
English. Restart the game to see changes. Feel free to send me your translation and I will
gladly include it in the next version.

Uninstall
---------
Delete the folder BepInEx/plugins/GraveyardAutomation and the folder
BepInEx/plugins/GK2.Framework/Localization/graveyardautomation.
Your save games are not affected.
