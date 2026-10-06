using System;
using System.Collections.Generic;

namespace BiggerStorage
{
    /// <summary>
    /// "Extra chest slots" und "Extra slots for wood, stone and marble": Die Platzanzahl steckt im Inventar jeder Truhe (Item.InventorySize) und wird mitgespeichert;
    /// beim Bau übernimmt WgoData.SetDataFromDefinition sie aus WGODef.inventorySize. Wir setzen sie auf
    /// Normalgröße + Zusatzplätze – nach dem Laden, beim Bau und sobald sich die Einstellung ändert. Das Truhenfenster
    /// legt seine Zellen nach InventorySize an (so vergrößert das Spiel auch das Spielerinventar).
    /// Verkleinern nie unter die belegten Plätze (InventoryFillSize) – die Einträge liegen lückenlos vorn in der Liste.
    /// Die Lager für getragene Stämme und Blöcke (z. B. sawmill_wood_container) funktionieren genauso: Einlagern
    /// (InsertOvrhdItem) und die Zombies prüfen Inventory.CanAddItemToInventory, also die Platzanzahl.
    /// </summary>
    internal static class ChestSlots
    {
        // Lager für getragene Materialien nehmen laut Inventar-Filter nur diese Items an.
        private static readonly HashSet<string> Materials = new HashSet<string> { "wood", "stone", "marble" };

        // Rückfall, falls ein Lager keinen Filter hat (Stand Patch 1.006).
        private static readonly HashSet<string> KnownStorages = new HashSet<string>
        {
            "wood_container", "stone_container", "marble_container", "sawmill_wood_container", "wood_container_town_workshop",
        };

        /// <summary>
        /// Passt alle Truhen im Spielstand an. Rückgabe: wie viele Truhen ihre Zielgröße nicht erreichen, weil mehr
        /// Plätze belegt sind (nur beim Verkleinern möglich).
        /// </summary>
        internal static int ApplyAll()
        {
            WorldData world = MainGame.Instance?.GameSave?.worldData;
            if (world?.gameSceneDataList == null) return 0;

            int changed = 0;
            int tooFull = 0;
            foreach (GameSceneData scene in world.gameSceneDataList)
            {
                if (scene?.wgoDataList == null) continue;
                foreach (WgoData wgo in scene.wgoDataList)
                {
                    try
                    {
                        if (Apply(wgo, out bool full)) changed++;
                        if (full) tooFull++;
                    }
                    catch (Exception e)
                    {
                        Plugin.LogWarning($"Could not resize {wgo?.id}: {e.Message}");
                    }
                }
            }
            if (changed > 0)
                Plugin.Log($"Resized {changed} chest(s) and storage(s) (extra slots: chests +{Plugin.EffectiveExtraSlots}, " +
                           $"wood/stone/marble +{Plugin.EffectiveExtraStorageSlots}).");
            // Die Füllstand-Blasen zeigen die Platzanzahl mit an.
            StorageBubble.RedrawAll();
            return tooFull;
        }

        /// <summary>
        /// Passt eine Truhe an. true = die Größe wurde geändert; <paramref name="tooFull"/> = sie bleibt größer als
        /// gewünscht, weil so viele Plätze belegt sind.
        /// </summary>
        internal static bool Apply(WgoData wgo, out bool tooFull)
        {
            tooFull = false;
            int extra;
            if (IsChest(wgo)) extra = Plugin.EffectiveExtraSlots;
            else if (IsMaterialStorage(wgo)) extra = Plugin.EffectiveExtraStorageSlots;
            else return false;
            Item data = wgo.Inventory?.Data;
            if (data == null) return false;

            int wanted = wgo.Definition.inventorySize + extra;
            int target = Math.Max(wanted, data.InventoryFillSize);
            tooFull = target > wanted;
            if (data.InventorySize == target) return false;
            data.InventorySize = target;
            return true;
        }

        /// <summary>
        /// Nur Truhen, die der Spieler öffnen kann (nur ChestInteractionHandler öffnet ein Truhenfenster). Fließband-Truhen,
        /// Brennstofflager, Brunnen und andere Objekte mit Inventar bleiben, wie sie sind.
        /// </summary>
        private static bool IsChest(WgoData wgo) =>
            wgo?.Definition != null &&
            wgo.Definition.interactionType == WGODef.InteractionType.Chest &&
            wgo.Definition.conveyorType == ConveyorElementType.None &&
            !wgo.Definition.isFuelContainer &&
            wgo.Definition.inventorySize > 0;

        /// <summary>
        /// Lager, in die der Spieler getragene Stämme, Stein- oder Marmorblöcke legt: Inventar-Filter nur mit diesen Items
        /// (oder bekannte Id). Fließband-Lager bleiben, wie sie sind.
        /// </summary>
        internal static bool IsMaterialStorage(WgoData wgo) => TryGetMaterial(wgo, out _);

        /// <summary>Wie <see cref="IsMaterialStorage"/>, liefert dazu das gelagerte Material (wood, stone oder marble).</summary>
        internal static bool TryGetMaterial(WgoData wgo, out string material)
        {
            material = null;
            WGODef def = wgo?.Definition;
            if (def == null || def.interactionType == WGODef.InteractionType.Chest || def.inventorySize <= 0) return false;
            if (def.conveyorType != ConveyorElementType.None || def.isFuelContainer || wgo.id.Contains("conveyor")) return false;

            WhiteListItemFilter filter = def.inventoryWhiteList;
            bool onlyMaterials = filter?.itemsIds != null && filter.itemsIds.Count > 0 &&
                                 (filter.groupsIds == null || filter.groupsIds.Count == 0) &&
                                 filter.itemsIds.TrueForAll(Materials.Contains);
            if (onlyMaterials)
            {
                material = filter.itemsIds[0];
                return true;
            }
            if (!KnownStorages.Contains(wgo.id)) return false;
            material = wgo.id.Contains("stone") ? "stone" : wgo.id.Contains("marble") ? "marble" : "wood";
            return true;
        }
    }
}
