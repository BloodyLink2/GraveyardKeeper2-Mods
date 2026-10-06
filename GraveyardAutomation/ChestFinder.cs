using System;
using System.Collections.Generic;
using UnityEngine;

namespace GraveyardAutomation
{
    /// <summary>
    /// Sucht die Truhe, in die eine Station ihre Ausgabe legt. Kandidaten sind dieselben Lager, die das Spiel für
    /// Werkbank-Zutaten und den Caretaker-Zombie nutzt (WorldZoneData.MultiInventoryWgoDatas), davon aber nur echte
    /// Truhen (siehe <see cref="IsStorageChest"/>). Truhen, die das Item schon enthalten, gewinnen vor näheren – wie beim
    /// Caretaker.
    /// </summary>
    internal static class ChestFinder
    {
        private static readonly HashSet<WgoData> seen = new HashSet<WgoData>();

        internal static WgoData Find(WgoData station, Item item) =>
            Find(station, inventory => inventory.CanAddItemToInventory(item), item.id);

        /// <param name="wholeArea">true = Suchradius ignorieren, jede Truhe im Gebiet (Ausweichweg beim Einlagern).</param>
        internal static WgoData Find(WgoData station, Func<Inventory, bool> canTake, string preferItemId, bool wholeArea = false)
        {
            List<WgoData> chests = station.WorldZoneData?.MultiInventoryWgoDatas;
            if (chests == null) return null;

            float radius = wholeArea ? 0f : Plugin.ChestRadius.Value;
            WgoData best = null;
            bool bestHasItem = false;
            float bestDistance = float.MaxValue;

            foreach (WgoData chest in chests)
            {
                if (chest == null || chest == station || chest.Inventory == null || !IsStorageChest(chest)) continue;

                float distance = Distance(station, chest);
                if (radius > 0f && distance > radius) continue;
                if (!canTake(chest.Inventory)) continue;

                bool hasItem = preferItemId != null && chest.Inventory.Data.HasItemByItemId(preferItemId, 1);
                if (best != null && ((bestHasItem && !hasItem) || (bestHasItem == hasItem && distance >= bestDistance))) continue;

                best = chest;
                bestHasItem = hasItem;
                bestDistance = distance;
            }
            return best;
        }

        /// <summary>
        /// Nur echte Truhen: Im Gebietslager stehen auch andere Objekte mit Inventar, deren Vorrat Rezepte nutzen dürfen –
        /// z. B. der ausgebaute Brunnen (Wasser, WellUpgradeInteractionHandler). Die haben keinen Filter und nehmen alles
        /// an, solange sie leer sind. Ablegen nur dort, wo auch der Spieler per Hand einlagern kann: Ein Truhenfenster öffnet
        /// das Spiel nur für InteractionType.Chest (ChestInteractionHandler). Fließband-Truhen gehören dem Fließband.
        /// </summary>
        private static bool IsStorageChest(WgoData chest) =>
            chest.Definition != null &&
            chest.Definition.interactionType == WGODef.InteractionType.Chest &&
            chest.Definition.conveyorType == ConveyorElementType.None;

        /// <summary>
        /// Alle Lager im Suchradius der Station (ohne Fließband-Truhen), nächste zuerst. Nur für Zutaten – hier bleiben
        /// Brunnen und Co. bewusst drin, das Spiel nimmt Zutaten ebenfalls aus ihnen.
        /// </summary>
        internal static void CollectNearby(WgoData station, List<WgoData> result)
        {
            result.Clear();
            List<WgoData> chests = station.WorldZoneData?.MultiInventoryWgoDatas;
            if (chests == null) return;

            float radius = Plugin.ChestRadius.Value;
            foreach (WgoData chest in chests)
            {
                if (chest == null || chest == station || chest.Inventory == null || chest.Definition == null ||
                    chest.Definition.conveyorType != ConveyorElementType.None) continue;
                if (radius > 0f && Distance(station, chest) > radius) continue;
                result.Add(chest);
            }
            result.Sort((a, b) => Distance(station, a).CompareTo(Distance(station, b)));
        }

        /// <summary>
        /// "Zombie ingredients from all areas": erst die Lager im Suchradius (<paramref name="nearby"/>, schon sortiert),
        /// dann die übrigen Lager im Gebiet der Station (nächste zuerst), dann die Truhen aller anderen Gebiete. Im eigenen
        /// Gebiet zählt wie beim Caretaker jedes Lager. Aus fremden Gebieten nehmen wir nur aus echten Truhen, nicht aus
        /// Brunnen, Material- oder Sonderlagern, deren Vorrat dort gebraucht wird.
        /// </summary>
        internal static void CollectEverywhere(WgoData station, List<WgoData> nearby, List<WgoData> result)
        {
            result.Clear();
            result.AddRange(nearby);
            seen.Clear();
            foreach (WgoData chest in nearby) seen.Add(chest);

            WorldZoneData ownZone = station.WorldZoneData;
            int ownStart = result.Count;
            if (ownZone?.MultiInventoryWgoDatas != null)
            {
                foreach (WgoData chest in ownZone.MultiInventoryWgoDatas)
                {
                    if (chest == null || chest == station || chest.Inventory == null || chest.Definition == null ||
                        chest.Definition.conveyorType != ConveyorElementType.None) continue;
                    if (seen.Add(chest)) result.Add(chest);
                }
                result.Sort(ownStart, result.Count - ownStart,
                    Comparer<WgoData>.Create((a, b) => Distance(station, a).CompareTo(Distance(station, b))));
            }

            WorldData world = MainGame.Instance?.GameSave?.worldData;
            if (world?.gameSceneDataList == null) return;
            foreach (GameSceneData scene in world.gameSceneDataList)
            {
                if (scene?.worldZones == null) continue;
                foreach (WorldZoneData zone in scene.worldZones)
                {
                    if (zone == null || zone == ownZone || zone.MultiInventoryWgoDatas == null) continue;
                    foreach (WgoData chest in zone.MultiInventoryWgoDatas)
                    {
                        if (chest == null || chest == station || chest.Inventory?.Data == null || !IsStorageChest(chest)) continue;
                        if (seen.Add(chest)) result.Add(chest);
                    }
                }
            }
        }

        /// <summary>Für die Diagnose: Abstand zur nächsten Truhe im Gebiet, egal ob mit Platz; -1 = keine Truhe.</summary>
        internal static float NearestDistance(WgoData station)
        {
            float nearest = -1f;
            List<WgoData> chests = station.WorldZoneData?.MultiInventoryWgoDatas;
            if (chests == null) return nearest;

            foreach (WgoData chest in chests)
            {
                if (chest == null || chest == station || !IsStorageChest(chest)) continue;
                float distance = Distance(station, chest);
                if (nearest < 0f || distance < nearest) nearest = distance;
            }
            return nearest;
        }

        /// <summary>Abstand auf dem Boden (x/z), die Höhe spielt keine Rolle.</summary>
        internal static float Distance(WgoData a, WgoData b)
        {
            Vector3 d = a.Position - b.Position;
            return new Vector2(d.x, d.z).magnitude;
        }
    }
}
