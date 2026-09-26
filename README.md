# Graveyard Keeper 2 Mods

BepInEx 5 mods for [Graveyard Keeper 2](https://store.steampowered.com/app/4358690/) by BloodyLink2. Downloads, screenshots and full descriptions are on Nexus Mods.

| Mod | What it does | Source | Download |
|---|---|---|---|
| Tireless Keeper | Health and energy regenerate, insanity fades, sleep deprivation builds up slower. Optional infinite health and energy, faster work at tables. | [PlayerRegeneration](PlayerRegeneration) | [Nexus](https://www.nexusmods.com/graveyardkeeper2/mods/79) |
| Sermon Every Day | Hold sermons on any day of the week and as often as you like. | [FlexibleCeremonies](FlexibleCeremonies) | [Nexus](https://www.nexusmods.com/graveyardkeeper2/mods/83) |
| Resurrect Every Day | Resurrect zombies on any day of the week and as often as you like. | [ResurrectEveryDay](ResurrectEveryDay) | [Nexus](https://www.nexusmods.com/graveyardkeeper2/mods/87) |
| Graveyard Automation | Finished items go straight into a nearby chest, ovens run on their own and refuel themselves, zombies take their ingredients from chests. | [GraveyardAutomation](GraveyardAutomation) | [Nexus](https://www.nexusmods.com/graveyardkeeper2/mods/89) |
| Zombie Productivity | Zombies work up to 10x faster at workbenches and in the garden, with optional extra mastery. | [ZombieProductivity](ZombieProductivity) | [Nexus](https://www.nexusmods.com/graveyardkeeper2/mods/90) |
| Bigger Storage | Up to 100 extra slots for every chest and optional stacks of up to 300. | [BiggerStorage](BiggerStorage) | [Nexus](https://www.nexusmods.com/graveyardkeeper2/mods/113) |

Each mod folder has a `changelog.txt` with the changes of every version.

## Building

Requirements:

- A current .NET SDK (the projects target `netstandard2.1`).
- Graveyard Keeper 2 with **BepInEx 5.4.23.5 (x64)** installed and the game started once.
- The **GK2 Mod Framework** installed in the game. Its `GK2.Framework.dll` is needed to build. In game the mods also run without it.

Setup:

1. Copy `LocalPaths.props.example` to `LocalPaths.props` and set `GameDir` to your game folder. Without this file, the default Steam path `C:\Program Files (x86)\Steam\steamapps\common\Graveyard Keeper 2` is used. `LocalPaths.props` is ignored by git.
2. Build all mods:

   ```
   dotnet build GK2Mods.sln
   ```

   After the build, each project copies its DLL into `BepInEx/plugins/<ModName>` and its language files into the folder of the GK2 Mod Framework. Restart the game to load the new version.

Options, on the command line or as properties in `LocalPaths.props`:

| Option | Effect |
|---|---|
| `-p:DeployToGame=false` | Build only, copy nothing into the game. |
| `-p:GameDir="D:\Games\Graveyard Keeper 2"` | Use a different game folder. |
| `-p:FrameworkDll="C:\Path\To\GK2.Framework.dll"` | Take `GK2.Framework.dll` from somewhere else. |

Release packages:

```
dotnet build GK2Mods.sln -c Release
```

This creates `Releases/<ModName>-<Version>.zip` for every mod. The zip holds a single `BepInEx` folder with the DLL, the README and the language files, so it can be extracted straight into the game folder (and installs cleanly with Vortex). The version comes from `<Version>` in the project file.

## Translations

The settings and mod descriptions are translated through the language files of the GK2 Mod Framework (0.1.8 or newer). Each mod has `Localization/en.json` and `Localization/de.json`, which end up in `BepInEx/plugins/GK2.Framework/Localization/<modid>/`.

- Texts in the code go through `Translations.Get("key", "English text")`. The English text is the fallback when the framework is missing.
- `en.json` is generated from the code, so do not edit it by hand. After changing texts, run (Python 3):

  ```
  python tools/localization.py
  ```

  It rewrites every `en.json` and reports missing or outdated keys in the other language files.
- To add a language, copy `en.json` to a file named after the language code (for example `ko.json`) and translate the values. Pull requests are welcome.

## Project layout

Every mod folder follows the same structure:

| File | Purpose |
|---|---|
| `Plugin.cs` | BepInEx entry point and settings. |
| `Patches.cs` and the feature files | Harmony patches and the mod logic. |
| `FrameworkBridge.cs` | All access to the GK2 Mod Framework, so the mod also runs without it. |
| `Translations.cs` | Text lookup through the framework with the English fallback. |
| `ConfigurationManagerAttributes.cs` | Display options for the F1 menu, following the template of BepInEx.ConfigurationManager. |
| `README.txt` | Player readme, included in the release zip. |
| `NEXUS_DESCRIPTION.txt`, `DISCORD_DESCRIPTION.md` | Texts for the Nexus page and the Discord post. |

Code comments are in German.

## Game code and assets

This repository contains only the source code of these mods. It includes no code, DLLs or assets of Graveyard Keeper 2. The build references the DLLs of your own game installation.

## Credits

- **Lazy Bear Games** for Graveyard Keeper 2.
- **The BepInEx team** for BepInEx.
- **Andreas Pardeike** for Harmony.
- **ManlyMarco and the BepInEx team** for ConfigurationManager.
- **SuperMan4eg** for the GK2 Mod Framework.

## License

[MIT](LICENSE)
