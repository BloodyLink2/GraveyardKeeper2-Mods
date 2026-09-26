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
