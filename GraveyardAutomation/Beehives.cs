using System;
using System.Collections.Generic;

namespace GraveyardAutomation
{
    /// <summary>
    /// "Harvest beehives": Der Bienenstock hat kein Lager wie der Brunnen. Der leere Stock (honey_home_empty) wird per
    /// Craft honey_home_respawn zum vollen Stock (honey_home) und wartet dann darauf, abgeerntet zu werden. Beim Ernten
    /// "stirbt" er (WgoData.RunLogicsAfterDeath): Honig und Bienenwachs fallen per MakeDrop (drop_res_on_die), dazu
    /// Forschungspunkte und Inspiration, dann ersetzt ihn wieder ein leerer Stock, der per StartCraft neu volläuft.
    /// Wir lösen genau das über WgoData.ForceDeath aus und leiten die Drops dabei in eine Truhe im Suchradius um
    /// (<see cref="Delivery.RunForStation"/>), wie beim Abschluss eines Ofens. Wilde Nester (honey_wild) bleiben unberührt.
    /// </summary>
    internal static class Beehives
    {
        internal const string FullId = "honey_home";
        internal const string EmptyId = "honey_home_empty";

        private const float ScanInterval = 3f;

        private static readonly List<WgoData> hives = new List<WgoData>();
        private static readonly List<Item> preview = new List<Item>();
        private static readonly HashSet<string> loggedHives = new HashSet<string>();

        private static float timer;
        private static bool errorLogged;

        internal static bool IsHive(string id) => id == FullId || id == EmptyId;

        internal static void Update(float deltaTime)
        {
            timer += deltaTime;
            if (timer < ScanInterval) return;
            timer = 0f;
            if (!Plugin.IsActive || !Plugin.HarvestBeehives.Value) return;

            WorldData world = MainGame.Instance?.GameSave?.worldData;
            if (world?.gameSceneDataList == null) return;

            // Erst sammeln: Das Ernten ersetzt den Stock und ändert dabei die Objektlisten.
            hives.Clear();
            foreach (GameSceneData scene in world.gameSceneDataList)
            {
                if (scene?.wgoDataList == null) continue;
                foreach (WgoData wgo in scene.wgoDataList)
                {
                    if (wgo?.id != FullId) continue;
                    // Truhen sucht ChestFinder nur im Gebiet des Stocks.
                    if (wgo.WorldZoneData != null) hives.Add(wgo);
                    else LogOnce(wgo, $"{wgo.id} stands outside of any area, so there are no chests it could use.");
                }
            }

            foreach (WgoData hive in hives)
            {
                if (!StationSwitches.Allows(hive)) continue;
                try
                {
                    Harvest(hive);
                }
                catch (Exception e)
                {
                    if (errorLogged) continue;
                    errorLogged = true;
                    Plugin.LogWarning("Harvesting a beehive failed (further errors will not be logged): " + e);
                }
            }
        }

        private static void Harvest(WgoData hive)
        {
            WGODef def = hive.Definition;
            if (def?.deathChanceItems == null || !def.deathChanceItems.HasOutput()) return;

            // Probewurf der Ausbeute, um eine Truhe mit Platz zu finden. Beim Ernten würfelt das Spiel neu; was dann
            // nicht mehr passt, verteilt Delivery.StoreRest auf andere Truhen.
            preview.Clear();
            foreach (Item item in OutputItems.MakeOutput(def.deathChanceItems.MakePreOutput(hive)))
            {
                if (item != null && !item.IsEmpty) preview.Add(item);
            }

            WgoData chest = ChestFinder.Find(hive, inventory => inventory.CanAddItemsToInventory(preview),
                preview.Count > 0 ? preview[0].id : null);
            if (chest == null && !Plugin.DropIfNoChest.Value)
            {
                Delivery.LogNoChestOnce(hive);
                return;
            }

            LogFirstHarvest(hive, chest);
            // Ohne Truhe (Option "Drop on the ground") fällt die Ernte wie beim Spieler neben den Stock.
            Delivery.RunForStation(hive, chest?.Inventory, hive.ForceDeath);
        }

        private static void LogFirstHarvest(WgoData hive, WgoData chest)
        {
            LogOnce(hive, chest == null
                ? $"{hive.id} has no chest with room within the search radius, so its honey drops on the ground."
                : $"{hive.id} is harvested into {chest.id} ({ChestFinder.Distance(hive, chest):0.0} away).");
        }

        /// <summary>Einmal pro Stock und Sitzung. Nach Ort gemerkt: Beim Ernten entsteht ein neues Objekt (ReplaceWgoData).</summary>
        private static void LogOnce(WgoData hive, string message)
        {
            if (loggedHives.Add(hive.WorldId + hive.Position)) Plugin.Log(message);
        }
    }
}
