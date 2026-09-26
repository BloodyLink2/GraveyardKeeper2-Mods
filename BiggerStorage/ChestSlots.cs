using System;

namespace BiggerStorage
{
    /// <summary>
    /// "Extra chest slots": Die Platzanzahl steckt im Inventar jeder Truhe (Item.InventorySize) und wird mitgespeichert;
    /// beim Bau übernimmt WgoData.SetDataFromDefinition sie aus WGODef.inventorySize. Wir setzen sie auf
    /// Normalgröße + Zusatzplätze – nach dem Laden, beim Bau und sobald sich die Einstellung ändert. Das Truhenfenster
    /// legt seine Zellen nach InventorySize an (so vergrößert das Spiel auch das Spielerinventar).
    /// Verkleinern nie unter die belegten Plätze (InventoryFillSize) – die Einträge liegen lückenlos vorn in der Liste.
    /// </summary>
    internal static class ChestSlots
    {
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
            if (changed > 0) Plugin.Log($"Resized {changed} chest(s) (extra slots: +{ExtraSlots}).");
            return tooFull;
        }

        /// <summary>
        /// Passt eine Truhe an. true = die Größe wurde geändert; <paramref name="tooFull"/> = sie bleibt größer als
        /// gewünscht, weil so viele Plätze belegt sind.
        /// </summary>
        internal static bool Apply(WgoData wgo, out bool tooFull)
        {
            tooFull = false;
            if (!IsChest(wgo)) return false;
            Item data = wgo.Inventory?.Data;
            if (data == null) return false;

            int wanted = wgo.Definition.inventorySize + ExtraSlots;
            int target = Math.Max(wanted, data.InventoryFillSize);
            tooFull = target > wanted;
            if (data.InventorySize == target) return false;
            data.InventorySize = target;
            return true;
        }

        private static int ExtraSlots => Plugin.EffectiveExtraSlots;

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
    }
}
