using System;
using System.Collections.Generic;
using LazyBearTechnology;

namespace GraveyardAutomation
{
    /// <summary>
    /// Prüft einmal pro Sekunde alle aktiven Crafts:
    /// - Zombie-Werkbänke (WaitingForWorkerPickUp): Der Zombie hält das Ergebnis im Craft-Inventar der Werkbank und hat
    ///   dafür Abhol-Aufträge (PickupOrder) aufgegeben. Wir führen sie aus wie Spieler oder Caretaker, nur mit einer
    ///   Truhe als Empfänger – danach arbeitet der Zombie sofort weiter.
    /// - Auto-Crafter ohne Arbeiter (ReadyToFinishAutoCraft): Wir schließen den Craft über das spieleigene
    ///   WgoData.ProcessReadyToFinishCraft ab und leiten dessen MakeDrop-Aufrufe in die Truhe um. Forschungspunkte
    ///   gehen direkt an den Spieler oder landen an der Station (siehe <see cref="StationTechPoints"/>), auch wenn der
    ///   Spieler woanders ist.
    /// Durch das Abfragen statt Einhaken greift das auch nach dem Laden eines Spielstands und sobald eine volle Truhe
    /// wieder Platz hat. Ohne passende Truhe bleibt alles wie im Spiel.
    /// </summary>
    internal static class Delivery
    {
        private const float CheckInterval = 1f;
        private const string AutoStartKey = "auto_start_same_craft_after_pickup";

        private static readonly List<CraftComponent> crafts = new List<CraftComponent>();
        private static readonly List<SGuid> orderIds = new List<SGuid>();
        private static readonly List<Item> outputItems = new List<Item>();
        private static readonly HashSet<SGuid> loggedStations = new HashSet<SGuid>();
        private static readonly HashSet<SGuid> loggedSupplies = new HashSet<SGuid>();
        private static readonly List<WgoData> supplyChests = new List<WgoData>();
        private static readonly HashSet<string> loggedSkips = new HashSet<string>();

        private static float timer;
        private static bool errorLogged;

        // Während ProcessReadyToFinishCraft: wessen Drops in welche Truhe gehen.
        private static WgoData collectingStation;
        private static Inventory collectTarget;

        internal static void Update(float deltaTime)
        {
            timer += deltaTime;
            if (timer < CheckInterval) return;
            timer = 0f;
            if (!Plugin.IsActive) return;

            List<CraftComponent> active = MainGame.Instance?.GameSave?.craftSystemData?.activeCrafts;
            if (active == null || active.Count == 0) return;

            // Kopie: Abschließen entfernt fertige Crafts aus der Liste.
            crafts.Clear();
            crafts.AddRange(active);
            foreach (CraftComponent craft in crafts)
            {
                try
                {
                    if (Plugin.ZombieIngredients.Value) TrySupplyZombieIngredients(craft);

                    if (craft.Status == CraftComponentStatus.WaitingForWorkerPickUp)
                    {
                        if (Plugin.ZombieWorkbenches.Value) TryDeliverZombieOutput(craft);
                    }
                    else if (craft.Status == CraftComponentStatus.ReadyToFinishAutoCraft)
                    {
                        if (Plugin.AutoCrafters.Value) TryCollectAutoCraft(craft);
                    }
                    else if (craft.IsQueueDelayed || craft.Status == CraftComponentStatus.ReadyToStartCraft)
                    {
                        if (Plugin.AutoRefuel.Value) FuelSupply.TryRefuelFor(craft);
                    }
                }
                catch (Exception e)
                {
                    if (errorLogged) continue;
                    errorLogged = true;
                    Plugin.LogWarning("Delivery failed (further errors will not be logged): " + e);
                }
            }
        }

        private static void TryDeliverZombieOutput(CraftComponent craft)
        {
            if (!(craft.CraftableObject.CraftableAttachedWorker is ZombieWgoData zombie) || zombie.ZombieType != ZombieType.Crafter) return;
            WgoData station = zombie.AttachedWgoData;
            WorldZoneData zone = zombie.WorldZoneData;
            if (!StationSwitches.Allows(station)) return;
            if (station == null || zone == null || zombie.CrafterOrders.Count == 0) return;

            // Kopie: Ausgeführte Aufträge verschwinden aus CrafterOrders.
            orderIds.Clear();
            orderIds.AddRange(zombie.CrafterOrders);
            for (int i = orderIds.Count - 1; i >= 0; i--)
            {
                if (!(zone.FindOrder(orderIds[i]) is PickupOrder order)) continue;
                // Leere Aufträge sind nur Platzhalter; reservierte holt schon ein Caretaker-Zombie ab.
                if (order.Item == null || order.Item.IsEmpty) continue;
                if (order.ExecutorUniqueId != null && !order.ExecutorUniqueId.IsEmpty) continue;

                WgoData chest = ChestFinder.Find(station, order.Item);
                IOrderExecutor executor;
                if (chest != null)
                {
                    executor = new ChestOrderExecutor(station, chest.Inventory);
                }
                else if (Plugin.DropIfNoChest.Value)
                {
                    executor = new GroundOrderExecutor(station);
                }
                else
                {
                    LogNoChestOnce(station);
                    continue;
                }

                // Wie beim Caretaker: Manche Stationen starten nach dem Abholen denselben Craft neu.
                CraftElementBase current = station.CraftComponent.CurrentCraftElement;
                GameRes restartRes = station.CraftComponent.Status == CraftComponentStatus.WaitingForWorkerPickUp &&
                                     current != null && current.ParamsData.customRes.GetInt(AutoStartKey) == 1
                    ? current.ParamsData.customRes.Clone()
                    : null;

                if (!order.TryExecuteOrder(executor, out _)) continue;
                zone.RemoveOrder(order.UniqueId);
                zombie.CrafterOnOrderExecuted(order);
                if (restartRes != null) RestartSameCraft(station, zombie, restartRes);
                LogFirstDelivery(station, chest);
                AutoRecipes.PickNext(station);
            }
        }

        /// <summary>
        /// "Zombie ingredients from chests": Fehlt einem Zombie-Arbeiter eine Zutat, gibt er je Zutat einen Liefer-Auftrag
        /// (DeliveryOrder) auf und wartet, bis ein Caretaker-Zombie sie bringt. Wir führen die Aufträge aus wie
        /// ZombieWgoData.CaretakerTryExecuteDeliveryOrder, nur mit den Lagern im Suchradius als Quelle. Aufträge, die ein
        /// Caretaker schon übernommen hat, bleiben ihm. Reicht der Vorrat nicht, wartet der Zombie wie gewohnt.
        /// </summary>
        private static void TrySupplyZombieIngredients(CraftComponent craft)
        {
            if (!(craft.CraftableObject.CraftableAttachedWorker is ZombieWgoData zombie) || zombie.ZombieType != ZombieType.Crafter) return;
            WgoData station = zombie.AttachedWgoData;
            WorldZoneData zone = zombie.WorldZoneData;
            if (!StationSwitches.Allows(station)) return;
            if (station == null || zone == null || zombie.CrafterOrders.Count == 0) return;

            // Kopie: Ausgeführte Aufträge verschwinden aus CrafterOrders.
            orderIds.Clear();
            orderIds.AddRange(zombie.CrafterOrders);
            ChestSupplyExecutor executor = null;
            foreach (SGuid orderId in orderIds)
            {
                if (!(zone.FindOrder(orderId) is DeliveryOrder order)) continue;
                if (order.Item == null || order.Item.IsEmpty) continue;
                if (order.ExecutorUniqueId != null && !order.ExecutorUniqueId.IsEmpty) continue;

                if (executor == null)
                {
                    ChestFinder.CollectNearby(station, supplyChests);
                    executor = new ChestSupplyExecutor(supplyChests);
                }
                if (!order.TryExecuteOrder(executor, out _))
                {
                    LogSkipOnce(station, $"waits for {order.Item.Count}x {order.Item.id}: not enough in the storages within the search radius");
                    continue;
                }
                zone.RemoveOrder(order.UniqueId);
                zombie.CrafterOnOrderExecuted(order);
                if (loggedSupplies.Add(station.UniqueId)) Plugin.Log($"{station.id} gets its ingredients from nearby storages.");
            }
        }

        /// <summary>Nachbau von ZombieWgoData.CaretakerTryExecutePickUpOrder für "auto_start_same_craft_after_pickup".</summary>
        private static void RestartSameCraft(WgoData station, ZombieWgoData zombie, GameRes customRes)
        {
            CraftComponent component = station.CraftComponent;
            if (component.AvailableCrafts.Count == 0) return;

            string craftId = component.AvailableCrafts[0].id;
            var paramsData = new CraftParamsData(craftId, station) { customRes = customRes };
            paramsData.customRes.Set(AutoStartKey, 1f);
            paramsData.customRes.Set("do_not_check_multiinventory_space", 1f);
            paramsData.customRes.Set("do_not_check_worker_dependent_values", 1f);
            component.TryStartCraft(new CraftElement(craftId, 1, paramsData));
            zombie.CrafterStopCraftActivity();
            zombie.CrafterStartCraftActivity();
        }

        private static void TryCollectAutoCraft(CraftComponent craft)
        {
            if (!(craft.CraftableObject is WgoData station) || !StationSwitches.Allows(station)) return;
            // "Fertig, wartet aufs Abholen" ohne Arbeiter heißt immer dasselbe – egal ob die Station als isAutoCrafter
            // markiert ist (z. B. Öfen) oder einen eigenen Handler hat. Fließband-Stationen regelt das Fließband.
            WGODef def = station.Definition;
            if (def == null || def.conveyorType != ConveyorElementType.None || station.Worker != null) return;

            CraftElementBase current = craft.CurrentCraftElement;
            if (current?.PreOutputItems == null) return;

            outputItems.Clear();
            foreach (ItemCount output in current.PreOutputItems)
            {
                if (output != null && !output.IsEmpty && !IsTechPoint(output.itemId))
                    outputItems.Add(new Item(output.itemId, output.count));
            }
            Item craftInventory = station.CraftableObjectCraftInventory?.Data;
            if (craftInventory?.Inventory != null)
            {
                foreach (Item item in craftInventory.Inventory)
                {
                    if (item != null && !item.IsEmpty && !IsTechPoint(item.id)) outputItems.Add(item);
                }
            }
            if (outputItems.Count == 0)
            {
                // Brennstoff-Crafts legen ihr Ergebnis ins eigene Lager (addItemsToWgoOnFinish) – einfach abschließen.
                // Andere Maschinen ohne Gegenstände (die nur einen Effekt haben) bleiben wie im Spiel.
                if (current.Def is CraftDefBase { isFuelCraft: true } && Plugin.AutoRefuel.Value)
                    RunForStation(station, null, station.ProcessReadyToFinishCraft);
                return;
            }

            WgoData chest = ChestFinder.Find(station, inventory => inventory.CanAddItemsToInventory(outputItems), outputItems[0].id);
            if (chest == null && !Plugin.DropIfNoChest.Value)
            {
                LogNoChestOnce(station);
                return;
            }

            // Ohne Truhe (Option "Drop on the ground") wirft MakeDrop wie gewohnt auf den Boden – in der Szene der Station.
            RunForStation(station, chest?.Inventory, station.ProcessReadyToFinishCraft);
            LogFirstDelivery(station, chest);
            AutoRecipes.PickNext(station);
        }

        /// <summary>
        /// Führt eine Aktion an einer Station aus, während deren MakeDrop-Aufrufe in <paramref name="chest"/> umgeleitet
        /// werden (null = normal auf den Boden) und Forschungspunkte an der Station landen.
        /// </summary>
        internal static void RunForStation(WgoData station, Inventory chest, Action action)
        {
            collectingStation = station;
            collectTarget = chest;
            try
            {
                action();
            }
            finally
            {
                collectingStation = null;
                collectTarget = null;
            }
        }

        /// <summary>
        /// Aus dem MakeDrop-Patch: Legt Drops der gerade abgeschlossenen Station in die Truhe. true = alles eingelagert,
        /// false = das Spiel lässt den Rest (item enthält nur noch ihn) wie gewohnt fallen.
        /// </summary>
        internal static bool TryRedirectDrop(WgoData station, Item item)
        {
            if (collectTarget == null || station != collectingStation || item == null || item.IsEmpty) return false;
            collectTarget.AddItemToInventory(item);
            return item.IsEmpty || StoreRest(station, item);
        }

        /// <summary>
        /// Inventory.AddItemToInventory meldet auch dann true, wenn nur ein Teil gepasst hat – item enthält danach den
        /// Rest (KeepUnaddedCountOnSource). Den Rest nicht verlieren: erst jede andere Truhe im Suchradius, dann jede
        /// Truhe im Gebiet. Passiert, wenn beim Abschließen mehr kommt als vorab geprüft (Nebenprodukte über
        /// OnCraftEnd/dropFromWgoItemsEnd) oder ein Stapel beim echten Einlagern nicht zu einem vorhandenen passt.
        /// false = nirgends Platz, der Rest muss fallen (der Craft ist zu diesem Zeitpunkt schon abgeschlossen).
        /// </summary>
        internal static bool StoreRest(WgoData station, Item item)
        {
            foreach (bool wholeArea in new[] { false, true })
            {
                for (int attempt = 0; attempt < 8 && !item.IsEmpty; attempt++)
                {
                    WgoData chest = ChestFinder.Find(station,
                        inventory => inventory.Data.CanAddItemCountToInventory(item.Definition, item.Count) > 0, item.id, wholeArea);
                    if (chest == null) break;
                    chest.Inventory.AddItemToInventory(item);
                }
                if (item.IsEmpty) return true;
            }
            LogSkipOnce(station, $"could not store {item.Count}x {item.id} in any chest with room, so it drops next to the station");
            return false;
        }

        /// <summary>
        /// Aus dem DropTechPoints-Patch, nur für Stationen, die die Mod gerade abschließt: Mit "Collect research points"
        /// gehen die Forschungspunkte direkt an den Spieler. Sonst landen sie an der Station – ist der Spieler in einer
        /// anderen Szene, als Boden-Drops dort statt in seiner Szene. true = erledigt, Original überspringen.
        /// </summary>
        internal static bool TryHandleStationTechPoints(UnityEngine.Vector3 pos, int r, int g, int b)
        {
            WgoData station = collectingStation;
            if (station == null) return false;
            if (Plugin.CollectResearchPoints.Value)
            {
                StationTechPoints.CollectToPlayer(r, g, b);
                return true;
            }
            if (station.WorldId == MainGame.PlayerData.currentGameSceneId) return false;
            return StationTechPoints.TryDrop(station, pos, r, g, b);
        }

        private static bool IsTechPoint(string itemId) =>
            itemId != null && (itemId.EndsWith("tech_red") || itemId.EndsWith("tech_green") || itemId.EndsWith("tech_blue"));

        private static void LogFirstDelivery(WgoData station, WgoData chest)
        {
            if (chest == null)
            {
                LogSkipOnce(station, "has no chest with room within the search radius, so its output drops on the ground");
                return;
            }
            if (!loggedStations.Add(station.UniqueId)) return;
            Plugin.Log($"{station.id} delivers into {chest.id} ({ChestFinder.Distance(station, chest):0.0} away).");
        }

        // Diagnose: je Station und Grund nur einmal pro Sitzung, damit das Log bei wartenden Stationen nicht vollläuft.
        internal static void LogSkipOnce(WgoData station, string reason)
        {
            if (loggedSkips.Add(station.UniqueId + reason)) Plugin.Log($"{station.id} {reason}.");
        }

        internal static void LogNoChestOnce(WgoData station)
        {
            float nearest = ChestFinder.NearestDistance(station);
            string where = nearest < 0f ? "there is no chest in this area" : $"nearest chest is {nearest:0.0} away";
            LogSkipOnce(station, $"is ready, but no chest with room within the search radius ({where})");
        }
    }
}
