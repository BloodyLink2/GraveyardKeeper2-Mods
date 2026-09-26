using System.Collections.Generic;
using UnityEngine;

namespace GraveyardAutomation
{
    /// <summary>
    /// Brennstoff ist für Rezepte eine ganz normale Zutat (ItemDef.isFuel), die Stationen wie jede andere aus dem
    /// Gebiets-Inventar nehmen – bevorzugt aus Brennstofflagern (WGODef.isFuelContainer). Ins Lager kommt er nur über
    /// einen Brennstoff-Craft (CraftDef.isFuelCraft), den der Spieler von Hand mit einer Menge startet.
    /// Wartet eine Station (QueueDelayed) genau auf Brennstoff, starten wir diesen Craft wie das Spiel
    /// (CraftInteractionHandler.FormCraftElementAndStartCraft / OnCraftPressed) – nur so oft wie nötig.
    /// </summary>
    internal static class FuelSupply
    {
        private static readonly List<NeedItemData> needs = new List<NeedItemData>();

        internal static void TryRefuelFor(CraftComponent craft)
        {
            if (!(craft.CraftableObject is WgoData station) || station.WorldZoneData == null) return;
            if (!StationSwitches.Allows(station)) return;
            CraftElementBase next = craft.CurrentCraftElement;
            if (next == null || next.IsStarted || next.Requirements == null) return;

            MultiInventory available = null;
            foreach (NeedItemData need in next.Requirements)
            {
                ItemDef fuel = need.IsGroup ? null : need.ItemDef;
                if (fuel == null || !fuel.isFuel) continue;

                available ??= station.GetCraftableMultiInventory();
                int missing = need.GetCount(station) - available.GetTotalCount(need.id);
                if (missing <= 0) continue;

                if (!TryMakeFuel(station, fuel, missing))
                    Delivery.LogSkipOnce(station, $"needs {fuel.id}, but no fuel storage in this area can make it right now " +
                                                  "(no idle fuel storage for it, or the materials are missing)");
            }
        }

        private static bool TryMakeFuel(WgoData station, ItemDef fuel, int missing)
        {
            foreach (WgoData storage in station.WorldZoneData.MultiInventoryWgoDatas)
            {
                if (storage?.Definition == null || !storage.Definition.isFuelContainer || storage.CraftComponent == null) continue;
                // Läuft dort schon etwas (auch vom Spieler), nicht dazwischenfunken.
                if (storage.CraftComponent.CraftElementsQueue.Count > 0) continue;

                foreach (CraftDefBase craftDef in storage.CraftComponent.AvailableCrafts)
                {
                    if (!(craftDef is CraftDef fuelCraft) || !fuelCraft.isFuelCraft || fuelCraft.FuelItemDef?.id != fuel.id) continue;
                    if (TryStart(storage, fuelCraft, fuel, missing))
                    {
                        Delivery.LogSkipOnce(storage, $"makes {fuel.id} for {station.id} from materials in your chests");
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool TryStart(WgoData storage, CraftDef fuelCraft, ItemDef fuel, int missing)
        {
            // Gruppen-Zutaten ("beliebiges Holz") wie das Craft-Fenster auflösen: erste vorhandene Variante.
            MultiInventory inventory = storage.GetCraftableMultiInventory();
            needs.Clear();
            foreach (NeedItemData need in fuelCraft.needItems)
            {
                if (!need.IsGroup)
                {
                    needs.Add(need);
                    continue;
                }
                if (!need.TryGetGroupItemDefs(out List<ItemDef> variants) || variants == null) return false;
                ItemDef pick = variants.Find(v => inventory.GetTotalCount(v.id) >= need.GetCount(storage));
                if (pick == null) return false;
                needs.Add(new NeedItemData(pick.id, need.count));
            }

            var paramsData = new CraftParamsData(fuelCraft.id, storage);
            paramsData.RecalculateParams(needs, storage.Worker ?? MainGame.PlayerController);
            var element = new CraftElement(fuelCraft.id, 1, new List<NeedItemData>(needs), paramsData);
            element.DoBeforeStartCalculations(storage);

            // Wie FormCraftElementAndStartCraft: Menge nach Platz im Lager begrenzen – hier zusätzlich nach Bedarf.
            int perCraft = element.PreToWgoOnFinishItems.Count > 0 ? element.PreToWgoOnFinishItems[0].count : 0;
            if (perCraft <= 0) return false;
            int bySpace = storage.Inventory.Data.CanAddItemCountToInventory(fuel, 99999) / perCraft;
            int count = Mathf.Min(Mathf.CeilToInt((float)missing / perCraft), bySpace);
            if (count <= 0) return false;
            element.Count = count;

            CraftComponent component = storage.CraftComponent;
            if (component.GetStartCraftStatus(element) != CraftStatus.OK) return false;

            // Forschungspunkte landen über den DropTechPoints-Patch am Lager, auch wenn der Spieler woanders ist.
            Delivery.RunForStation(storage, null, () =>
            {
                if (fuelCraft.skipQueue)
                {
                    if (element.CanFinishCraft(storage) == CraftStatus.OK) component.ProcessInstantCraft(storage, element);
                }
                else
                {
                    component.AddToQueue(element, addToQueueTop: true);
                    component.TryContinueFromQueue();
                }
            });
            return true;
        }
    }
}
