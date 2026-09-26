using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using LazyBearTechnology;

namespace GraveyardAutomation
{
    /// <summary>
    /// "Automated stations": ein Schalter je Stationsart (Config-Abschnitt "Stations", Schlüssel = WGO-Id, Standard an).
    /// Aus = die Mod lässt diese Art komplett in Ruhe. Welche Arten es gibt, steht erst mit geladenem Spielstand fest –
    /// deshalb gehen wir nach MainGame.OnGameStarted alle Objekte durch und legen für jede Art, die die Mod automatisiert,
    /// einen Eintrag an; später gebaute Arten kommen beim ersten Kontakt dazu. BepInEx übernimmt beim Bind den Wert aus
    /// der Config-Datei, auch wenn der Eintrag erst jetzt entsteht. Im Framework-Menü erscheinen neue Einträge beim
    /// nächsten Öffnen der Einstellungen (Gk2Settings.Items ist die lebende Liste).
    /// </summary>
    internal static class StationSwitches
    {
        internal const string Section = "Stations";

        private static readonly Dictionary<string, ConfigEntry<bool>> entries = new Dictionary<string, ConfigEntry<bool>>();
        private static readonly Dictionary<string, bool> hasAutoCrafts = new Dictionary<string, bool>();
        private static readonly Dictionary<string, string> idByName = new Dictionary<string, string>();

        /// <summary>
        /// Darf die Mod diese Station automatisieren? Unbekannte Arten werden dabei angemeldet (nur wenn die Mod sie
        /// überhaupt automatisiert); ohne Eintrag gilt "an".
        /// </summary>
        internal static bool Allows(WgoData station)
        {
            string id = KeyOf(station?.id);
            if (string.IsNullOrEmpty(id)) return true;
            if (!entries.TryGetValue(id, out ConfigEntry<bool> entry))
            {
                if (!IsAutomatable(station)) return true;
                entry = Register(id, DisplayName(id));
            }
            return entry == null || entry.Value;
        }

        /// <summary>Nach dem Laden: alle Arten im Spielstand anmelden, gleichnamige Ausbaustufen mit Id im Namen.</summary>
        internal static void DiscoverAll()
        {
            WorldData world = MainGame.Instance?.GameSave?.worldData;
            if (world?.gameSceneDataList == null || Plugin.Instance == null) return;

            var found = new SortedSet<string>(StringComparer.Ordinal);
            foreach (GameSceneData scene in world.gameSceneDataList)
            {
                if (scene?.wgoDataList == null) continue;
                foreach (WgoData wgo in scene.wgoDataList)
                {
                    try
                    {
                        string id = KeyOf(wgo?.id);
                        if (id != null && !entries.ContainsKey(id) && IsAutomatable(wgo)) found.Add(id);
                    }
                    catch (Exception e)
                    {
                        Plugin.LogWarning($"Could not check {wgo?.id}: {e.Message}");
                    }
                }
            }
            if (found.Count == 0) return;

            // Gleiche Namen (z. B. drei Ausbaustufen "Ofen") vorab erkennen, damit schon der erste Eintrag die Id trägt.
            var nameCount = new Dictionary<string, int>();
            foreach (string id in found)
            {
                string name = DisplayName(id);
                nameCount[name] = (nameCount.TryGetValue(name, out int n) ? n : 0) + 1;
            }
            foreach (string id in found)
            {
                string name = DisplayName(id);
                Register(id, nameCount[name] > 1 || idByName.ContainsKey(name) ? $"{name} ({id})" : name);
            }
            Plugin.Log($"{entries.Count} kind(s) of automated stations can be switched on and off under \"{Section}\".");
        }

        private static ConfigEntry<bool> Register(string id, string name)
        {
            if (entries.TryGetValue(id, out ConfigEntry<bool> existing)) return existing;
            if (Plugin.Instance == null) return null;

            if (idByName.TryGetValue(name, out string other) && other != id) name = $"{name} ({id})";
            idByName[name] = id;

            string description = string.Format(Translations.Get("station.description",
                "Automate \"{0}\". Off = Graveyard Automation leaves this kind of station alone: no delivery into " +
                "chests, no ingredients from chests, no refuelling, no picked or repeated recipes and no extra speed."),
                name);
            ConfigEntry<bool> entry = Plugin.Instance.Config.Bind(Section, id, true, new ConfigDescription(description, null,
                new ConfigurationManagerAttributes
                {
                    Category = Translations.Get("stations.category", "Automated stations"),
                    DispName = name,
                    Order = 0,
                }));
            entries[id] = entry;

            if (Plugin.FrameworkMode)
            {
                try
                {
                    FrameworkBridge.AddStationToggle(id, name,
                        Translations.Get("station.hint", "Off = the mod leaves this kind of station alone."));
                }
                catch (Exception e)
                {
                    Plugin.LogWarning($"Could not add {id} to the framework menu: {e.Message}");
                }
            }
            return entry;
        }

        /// <summary>Der Bienenstock wechselt zwischen voll und leer (zwei Ids): ein Schalter für beide.</summary>
        private static string KeyOf(string id) => id == Beehives.EmptyId ? Beehives.FullId : id;

        /// <summary>Name wie im Spiel (Info-Fenster: LLBase.L(id)); ohne Übersetzung die Id.</summary>
        private static string DisplayName(string id)
        {
            string name = LLBase.L(id);
            return string.IsNullOrWhiteSpace(name) ? id : name.Trim();
        }

        /// <summary>
        /// Arten, die die Mod automatisiert: selbstlaufende Stationen (sichtbare Auto-Rezepte, keine Stadtgebäude und
        /// kein Brennstoff), Werkbänke mit Zombie-Arbeiter, ausgebaute Brunnen und Bienenstöcke. Fließband und
        /// Brennstofflager nicht.
        /// </summary>
        private static bool IsAutomatable(WgoData wgo)
        {
            WGODef def = wgo?.Definition;
            if (def == null || def.conveyorType != ConveyorElementType.None || def.isFuelContainer) return false;
            if (def.interactionType == WGODef.InteractionType.WellUpgrade || Beehives.IsHive(wgo.id)) return true;
            if (wgo.Worker is ZombieWgoData zombie && zombie.ZombieType == ZombieType.Crafter) return true;

            if (!hasAutoCrafts.TryGetValue(wgo.id, out bool auto))
            {
                auto = wgo.CraftComponent != null && wgo.CraftComponent.CraftsIn.Exists(d =>
                    d.isAuto && !d.isHidden && !d.autoFinishAutoCraft && !d.isFuelCraft);
                hasAutoCrafts[wgo.id] = auto;
            }
            return auto;
        }
    }
}
