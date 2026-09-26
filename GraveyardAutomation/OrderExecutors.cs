using System;
using System.Collections.Generic;

namespace GraveyardAutomation
{
    /// <summary>
    /// Führt Abhol-Aufträge (PickupOrder) aus wie Spieler oder Caretaker – nur landet das Item in einer Truhe.
    /// </summary>
    internal sealed class ChestOrderExecutor : IOrderExecutor
    {
        private readonly WgoData station;
        private readonly Inventory chest;

        internal ChestOrderExecutor(WgoData station, Inventory chest)
        {
            this.station = station;
            this.chest = chest;
        }

        public bool CanAddItem(Item item) => chest.CanAddItemToInventory(item);

        /// <summary>
        /// PickupOrder.ExecuteOrder nimmt das Item danach in jedem Fall aus der Werkbank – ein Rest, der nicht in die
        /// Truhe passt, darf also nicht verloren gehen (siehe <see cref="Delivery.StoreRest"/>).
        /// </summary>
        public void AddItem(Item item)
        {
            var copy = new Item(item.id, item.Count);
            chest.AddItemToInventory(copy);
            if (copy.IsEmpty || Delivery.StoreRest(station, copy)) return;
            MainGame.Instance.dropSystem.DropItem(copy, station.WorldId, station.GetDropPos(copy));
        }

        // Nur für Liefer-Aufträge (Zutaten bringen) nötig – die führt die Mod nicht aus.
        public bool HasItem(Item item) => false;

        public void RemoveItem(Item item)
        {
        }
    }

    /// <summary>
    /// "Drop on the ground if no chest": Führt Abhol-Aufträge aus, indem das Item vor der Werkbank auf den Boden fällt
    /// (in deren Szene). Bewusst DropSystem.DropItem statt WgoData.MakeDrop – das würde bei Zombie-Werkbänken sofort
    /// einen neuen Abhol-Auftrag aufgeben.
    /// </summary>
    internal sealed class GroundOrderExecutor : IOrderExecutor
    {
        private readonly WgoData station;

        internal GroundOrderExecutor(WgoData station)
        {
            this.station = station;
        }

        public bool CanAddItem(Item item) => true;

        public void AddItem(Item item)
        {
            var drop = new Item(item.id, item.Count);
            MainGame.Instance.dropSystem.DropItem(drop, station.WorldId, station.GetDropPos(drop));
        }

        public bool HasItem(Item item) => false;

        public void RemoveItem(Item item)
        {
        }
    }

    /// <summary>
    /// "Zombie ingredients from chests": Führt Liefer-Aufträge (DeliveryOrder) eines Zombie-Arbeiters aus wie der
    /// Caretaker – nur kommen die Zutaten direkt aus den Lagern im Suchradius statt aus seinen Händen.
    /// DeliveryOrder.ExecuteOrder ruft RemoveItem und legt die Zutat danach selbst ins Craft-Inventar der Werkbank.
    /// </summary>
    internal sealed class ChestSupplyExecutor : IOrderExecutor
    {
        private readonly List<WgoData> sources;

        /// <param name="sources">Lager, nächstes zuerst – aus ihnen wird der Reihe nach entnommen.</param>
        internal ChestSupplyExecutor(List<WgoData> sources)
        {
            this.sources = sources;
        }

        public bool HasItem(Item item) => CountInSources(item.id) >= item.Count;

        public void RemoveItem(Item item)
        {
            int left = item.Count;
            foreach (WgoData source in sources)
            {
                if (left <= 0) break;
                int here = source.Inventory.Data.GetTotalCountInInventory(item.id);
                if (here <= 0) continue;
                int take = Math.Min(here, left);
                source.Inventory.RemoveItemById(item.id, take);
                left -= take;
            }
        }

        // Nur für Abhol-Aufträge nötig – die übernimmt ChestOrderExecutor.
        public bool CanAddItem(Item item) => false;

        public void AddItem(Item item)
        {
        }

        private int CountInSources(string itemId)
        {
            int total = 0;
            foreach (WgoData source in sources) total += source.Inventory.Data.GetTotalCountInInventory(itemId);
            return total;
        }
    }
}
