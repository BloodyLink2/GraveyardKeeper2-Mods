using System;
using System.Collections.Generic;

namespace GraveyardAutomation
{
    /// <summary>
    /// "Empty wells into chests": Der ausgebaute Brunnen (InteractionType.WellUpgrade) füllt per Dauer-Craft
    /// (z. B. well_garden_water_respawn, addItemsToWgoOnFinish) sein eigenes Inventar mit Wasser. Ist es voll, meldet der
    /// Craft NotEnoughSpaceInWgo und die Produktion steht, bis jemand Wasser entnimmt. Wir räumen den Brunnen alle paar
    /// Sekunden in Truhen im Suchradius aus – über dieselben Inventar-Methoden wie der Spieler, also mit Ereignissen.
    /// Alles andere im Brunnen (Reste des alten Kompost-Bugs) wandert gleich mit.
    /// </summary>
    internal static class Wells
    {
        private const float ScanInterval = 2f;
        // Schutz vor Endlosschleifen, falls eine Truhe "passt" meldet, das Einlagern aber scheitert.
        private const int MaxChestsPerItem = 8;

        private static readonly List<WgoData> wells = new List<WgoData>();
        private static readonly List<string> itemIds = new List<string>();
        private static readonly HashSet<SGuid> loggedWells = new HashSet<SGuid>();

        private static float timer;
        private static bool errorLogged;

        internal static void Update(float deltaTime)
        {
            timer += deltaTime;
            if (timer < ScanInterval) return;
            timer = 0f;
            if (!Plugin.IsActive || !Plugin.EmptyWells.Value) return;

            WorldData world = MainGame.Instance?.GameSave?.worldData;
            if (world?.gameSceneDataList == null) return;

            // Brunnen stehen im Gebietslager (OpenInMultiInventory) – das ist viel kürzer als wgoDataList.
            wells.Clear();
            foreach (GameSceneData scene in world.gameSceneDataList)
            {
                if (scene?.worldZones == null) continue;
                foreach (WorldZoneData zone in scene.worldZones)
                {
                    if (zone?.MultiInventoryWgoDatas == null) continue;
                    foreach (WgoData wgo in zone.MultiInventoryWgoDatas)
                    {
                        if (wgo?.Definition != null && wgo.Definition.interactionType == WGODef.InteractionType.WellUpgrade &&
                            wgo.Inventory?.Data?.Inventory != null)
                        {
                            wells.Add(wgo);
                        }
                    }
                }
            }

            foreach (WgoData well in wells)
            {
                if (!StationSwitches.Allows(well)) continue;
                try
                {
                    EmptyWell(well);
                }
                catch (Exception e)
                {
                    if (errorLogged) continue;
                    errorLogged = true;
                    Plugin.LogWarning("Emptying a well failed (further errors will not be logged): " + e);
                }
            }
        }

        private static void EmptyWell(WgoData well)
        {
            Inventory source = well.Inventory;
            itemIds.Clear();
            foreach (Item item in source.Data.Inventory)
            {
                if (item != null && !item.IsEmpty && !itemIds.Contains(item.id)) itemIds.Add(item.id);
            }

            foreach (string id in itemIds)
            {
                ItemDef def = GameBalance.Me.GetData<ItemDef>(id);
                if (def == null) continue;

                int left = source.Data.GetTotalCountInInventory(id);
                for (int i = 0; i < MaxChestsPerItem && left > 0; i++)
                {
                    WgoData chest = ChestFinder.Find(well, inventory => inventory.Data.CanAddItemCountToInventory(def, left) > 0, id);
                    if (chest == null)
                    {
                        Delivery.LogNoChestOnce(well);
                        break;
                    }

                    int count = Math.Min(left, chest.Inventory.Data.CanAddItemCountToInventory(def, left));
                    foreach (Item moved in source.RemoveItemById(id, count))
                    {
                        // Sollte passen (vorher geprüft). Was doch übrig bleibt – AddItemToInventory meldet auch bei
                        // einem Teil Erfolg und lässt den Rest im Item –, geht zurück in den Brunnen statt verloren.
                        chest.Inventory.AddItemToInventory(moved);
                        if (!moved.IsEmpty) source.AddItemToInventory(moved);
                    }
                    left = source.Data.GetTotalCountInInventory(id);
                    LogFirstEmpty(well, chest);
                }
            }
        }

        private static void LogFirstEmpty(WgoData well, WgoData chest)
        {
            if (!loggedWells.Add(well.UniqueId)) return;
            Plugin.Log($"{well.id} empties into {chest.id} ({ChestFinder.Distance(well, chest):0.0} away).");
        }
    }
}
