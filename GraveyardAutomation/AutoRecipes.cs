using System;
using System.Collections.Generic;

namespace GraveyardAutomation
{
    /// <summary>
    /// "Pick recipes from nearby chests" (Stardew-Stil): Eine leere Station, die von selbst arbeitet (Auto-Rezepte, kein
    /// Arbeiter, z. B. ein Ofen), schaut alle paar Sekunden in die Truhen im Suchradius. Liegen dort alle Zutaten eines
    /// ihrer Rezepte – Brennstoff ausgenommen, der kommt aus dem Lager –, reiht sie genau einen Durchgang ein und wählt
    /// nach dem Abholen neu (Reihenfolge siehe <see cref="TryQueueFromChests"/>). So läuft sie dauerhaft, mehrere
    /// Stationen verteilen sich auf verschiedene Rezepte und die Vorräte bleiben ausgeglichen.
    /// Damit wirklich diese Zutaten verbraucht werden (und nicht gleiche Items aus anderen Truhen, sonst würde die Station
    /// endlos nachlegen), sortiert <see cref="PrioritizeInputChests"/> die Truhen daneben beim Entnehmen nach vorn.
    /// </summary>
    internal static class AutoRecipes
    {
        private const float ScanInterval = 3f;

        private static readonly Dictionary<string, bool> autoStationTypes = new Dictionary<string, bool>();
        private static readonly Dictionary<SGuid, string> lastRecipes = new Dictionary<SGuid, string>();
        private static readonly List<WgoData> inputChests = new List<WgoData>();
        private static readonly List<WgoData> priorityChests = new List<WgoData>();
        private static readonly List<Candidate> candidates = new List<Candidate>();
        private static readonly Dictionary<string, int> recipesInUse = new Dictionary<string, int>();
        private static readonly HashSet<string> recipesSeen = new HashSet<string>();

        private static float timer;
        private static bool errorLogged;

        internal static void Update(float deltaTime)
        {
            timer += deltaTime;
            if (timer < ScanInterval) return;
            timer = 0f;
            if (!Plugin.IsActive || (!Plugin.PickRecipes.Value && !Plugin.ZombieAutoRecipes.Value)) return;

            WorldData world = MainGame.Instance?.GameSave?.worldData;
            if (world?.gameSceneDataList == null) return;

            foreach (GameSceneData scene in world.gameSceneDataList)
            {
                if (scene?.worldZones == null) continue;
                foreach (WorldZoneData zone in scene.worldZones)
                {
                    if (zone?.MultiInventoryWgoDatas == null || zone.MultiInventoryWgoDatas.Count == 0) continue;
                    foreach (SGuid id in zone.wgoDataList)
                    {
                        WgoData station = world.GetWgoData(id);
                        if (!ShouldPick(station)) continue;
                        try
                        {
                            TryQueueFromChests(station);
                        }
                        catch (Exception e)
                        {
                            if (errorLogged) continue;
                            errorLogged = true;
                            Plugin.LogWarning("Picking a recipe failed (further errors will not be logged): " + e);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Merkt sich, welches Rezept der Spieler zuletzt selbst an dieser Station eingereiht hat – bei Gleichstand
        /// bevorzugt die Auswahl dieses Rezept.
        /// </summary>
        internal static void RememberRecipe(WgoData station, string craftId)
        {
            if (station != null && !string.IsNullOrEmpty(craftId)) lastRecipes[station.UniqueId] = craftId;
        }

        /// <summary>
        /// Aus dem GetCraftableMultiInventory-Patch: Die Truhen im Suchradius kommen an den Anfang der Liste, damit
        /// MultiInventory.RemoveItems (geht die Liste der Reihe nach durch) zuerst dort entnimmt.
        /// </summary>
        internal static void PrioritizeInputChests(WgoData station, MultiInventory inventory)
        {
            if (!Plugin.IsActive || !Plugin.PickRecipes.Value || inventory?.inventoryList == null) return;
            if (station.Worker != null || !IsAutoStationType(station) || !StationSwitches.Allows(station)) return;

            ChestFinder.CollectNearby(station, priorityChests);
            int insertAt = 0;
            foreach (WgoData chest in priorityChests)
            {
                int index = inventory.inventoryList.IndexOf(chest.Inventory);
                if (index < 0) continue;
                if (index > insertAt)
                {
                    inventory.inventoryList.RemoveAt(index);
                    inventory.inventoryList.Insert(insertAt, chest.Inventory);
                }
                insertAt++;
            }
        }

        /// <summary>Soll diese Station jetzt selbst ein Rezept wählen? Je nach Option Öfen und/oder Zombie-Werkbänke.</summary>
        private static bool ShouldPick(WgoData station) =>
            ((Plugin.PickRecipes.Value && IsIdleAutoStation(station)) ||
             (Plugin.ZombieAutoRecipes.Value && IsIdleZombieStation(station))) &&
            StationSwitches.Allows(station);

        /// <summary>
        /// "Pick recipes for zombie workbenches": Werkbank mit Zombie-Arbeiter, leere Warteschlange und keine offenen
        /// Aufträge des Zombies (sonst wartet er noch auf Zutaten oder aufs Abholen).
        /// </summary>
        private static bool IsIdleZombieStation(WgoData station)
        {
            CraftComponent craft = station?.CraftComponent;
            if (craft == null || craft.IsDestroyingCraftActive || craft.CraftElementsQueue.Count > 0) return false;
            if (station.Definition == null || station.Definition.conveyorType != ConveyorElementType.None) return false;
            return station.Worker is ZombieWgoData zombie && zombie.ZombieType == ZombieType.Crafter &&
                   zombie.AttachedWgoData == station && zombie.CrafterOrders.Count == 0;
        }

        private static bool IsIdleAutoStation(WgoData station)
        {
            CraftComponent craft = station?.CraftComponent;
            if (craft == null || station.Worker != null || craft.IsDestroyingCraftActive) return false;
            if (craft.CraftElementsQueue.Count > 0) return false;
            if (station.Definition == null || station.Definition.conveyorType != ConveyorElementType.None) return false;
            return IsAutoStationType(station);
        }

        /// <summary>Hat dieser Stationstyp überhaupt Rezepte, die von selbst laufen? Je WGO-Typ gemerkt.</summary>
        private static bool IsAutoStationType(WgoData station)
        {
            if (station.CraftComponent == null) return false;
            if (autoStationTypes.TryGetValue(station.id, out bool isAuto)) return isAuto;

            isAuto = station.CraftComponent.CraftsIn.Exists(d => d is CraftDef def && IsPickable(def));
            autoStationTypes[station.id] = isAuto;
            return isAuto;
        }

        // Nur Auto-Rezepte, die man normal einreiht: keine versteckten, Sofort-, Brennstoff- oder Fließband-Rezepte und
        // keine, für die man etwas von Hand in die Station legt (z. B. Leichen).
        /// <summary>
        /// Rezepte, die ein Zombie an seiner Werkbank machen kann: normale (nicht selbstlaufende) Rezepte, für die er das
        /// Werkzeug hat und deren Meisterschaft er erreicht – genau das prüft ZombieWgoData vor dem Arbeiten
        /// (CrafterCanUseTool, CrafterIsEnoughMastery). Sonst bliebe das Rezept eingereiht und der Zombie stünde still.
        /// </summary>
        private static bool CanZombieMake(ZombieWgoData zombie, WgoData station, CraftDef def)
        {
            if (def.isAuto || def.isHidden || def.skipQueue || def.isFuelCraft || def.isConveyorCraft ||
                def.needItemsFromWgo.Count > 0)
            {
                return false;
            }
            if (!zombie.CrafterCanUseTool(station, def)) return false;
            if (def.isStarCraft || def.isAutopsyCraft || def.isPocketExtractCraft) return true;
            return zombie.GetMasteryLevelForTalentBranch(station.Definition.talent, def) >= def.talentLock;
        }

        private static bool IsPickable(CraftDef def) =>
            def.isAuto && !def.isHidden && !def.skipQueue && !def.isFuelCraft && !def.isConveyorCraft &&
            def.needItemsFromWgo.Count == 0;

        /// <summary>
        /// Direkt nachdem die Mod ein fertiges Ergebnis abgeholt hat: gleich das nächste Rezept wählen, statt bis zum
        /// nächsten Durchlauf zu warten – sonst stünde die Station bei jedem Stück bis zu <see cref="ScanInterval"/> still.
        /// </summary>
        internal static void PickNext(WgoData station)
        {
            if (!Plugin.IsActive || !ShouldPick(station)) return;
            try
            {
                TryQueueFromChests(station);
            }
            catch (Exception e)
            {
                if (errorLogged) return;
                errorLogged = true;
                Plugin.LogWarning("Picking a recipe failed (further errors will not be logged): " + e);
            }
        }

        /// <summary>
        /// Wählt unter allen machbaren Rezepten eins und reiht genau einen Durchgang ein – nach dem Abholen wird neu
        /// gewählt. So verteilen sich mehrere Stationen und die Vorräte bleiben ausgeglichen. Reihenfolge: Rezepte, die
        /// gerade keine andere Station im Gebiet macht; dann das Ergebnis, von dem am wenigsten in den Lagern des Gebiets
        /// liegt; dann das Rezept mit mehr verschiedenen Zutaten aus den Truhen (sonst nähme ein einfaches Rezept dem
        /// aufwendigeren die Zutaten weg); dann das Rezept, das der Spieler zuletzt selbst eingereiht hat; dann die
        /// Rezeptliste.
        /// </summary>
        private static void TryQueueFromChests(WgoData station)
        {
            ChestFinder.CollectNearby(station, inputChests);
            if (inputChests.Count == 0) return;

            WorldZoneData zone = station.WorldZoneData;
            CountRecipesInUse(station, zone);
            lastRecipes.TryGetValue(station.UniqueId, out string lastId);

            candidates.Clear();
            List<CraftDefBase> available = station.CraftComponent.AvailableCrafts;
            for (int i = 0; i < available.Count; i++)
            {
                if (!(available[i] is CraftDef def)) continue;
                var needList = new List<NeedItemData>();
                if (!TryResolveNeeds(station, def, needList, out int ingredients)) continue;
                recipesInUse.TryGetValue(def.id, out int busy);
                candidates.Add(new Candidate
                {
                    Def = def,
                    Needs = needList,
                    Busy = busy,
                    Stock = CountInZone(zone, MainOutputId(def)),
                    Ingredients = ingredients,
                    IsLast = def.id == lastId,
                    Order = i,
                });
            }

            candidates.Sort((a, b) =>
                a.Busy != b.Busy ? a.Busy.CompareTo(b.Busy) :
                a.Stock != b.Stock ? a.Stock.CompareTo(b.Stock) :
                a.Ingredients != b.Ingredients ? b.Ingredients.CompareTo(a.Ingredients) :
                a.IsLast != b.IsLast ? b.IsLast.CompareTo(a.IsLast) :
                a.Order.CompareTo(b.Order));

            foreach (Candidate candidate in candidates)
            {
                if (TryQueueOne(station, candidate.Def, candidate.Needs)) return;
            }
        }

        /// <summary>
        /// Prüft, ob alle Zutaten eines Durchgangs in den Truhen daneben liegen, und löst Gruppen-Zutaten auf.
        /// <paramref name="ingredients"/> = Anzahl verschiedener Zutaten aus den Truhen (Brennstoff zählt nicht).
        /// </summary>
        private static bool TryResolveNeeds(WgoData station, CraftDef def, List<NeedItemData> needList, out int ingredients)
        {
            ingredients = 0;
            bool pickable = station.Worker is ZombieWgoData zombie ? CanZombieMake(zombie, station, def) : IsPickable(def);
            if (!pickable || !station.CraftComponent.IsCraftAllowedByAttachedExtensions(def)) return false;

            MultiInventory everywhere = null;
            foreach (NeedItemData need in def.needItems)
            {
                int perCraft = need.GetCount(station);
                if (perCraft <= 0)
                {
                    needList.Add(need);
                    continue;
                }

                if (!need.IsGroup)
                {
                    // Brennstoff darf von überall kommen (Brennstofflager, notfalls per "Refuel from chests").
                    if (need.ItemDef != null && need.ItemDef.isFuel)
                    {
                        needList.Add(need);
                        continue;
                    }
                    if (CountInInputChests(need.id) < perCraft) return false;
                    ingredients++;
                    needList.Add(need);
                    continue;
                }

                // Gruppen-Zutat ("beliebiges Holz") wie das Craft-Fenster auflösen: erste Variante, die reicht.
                if (!need.TryGetGroupItemDefs(out List<ItemDef> variants) || variants == null || variants.Count == 0) return false;
                if (variants.TrueForAll(v => v.isFuel))
                {
                    everywhere ??= station.GetCraftableMultiInventory();
                    ItemDef fuel = variants.Find(v => everywhere.GetTotalCount(v.id) >= perCraft) ?? variants[0];
                    needList.Add(new NeedItemData(fuel.id, need.count));
                    continue;
                }
                ItemDef pick = variants.Find(v => CountInInputChests(v.id) >= perCraft);
                if (pick == null) return false;
                ingredients++;
                needList.Add(new NeedItemData(pick.id, need.count));
            }
            // Ohne Zutat aus den Truhen gibt es keinen Anlass, etwas einzureihen.
            return ingredients > 0;
        }

        private static bool TryQueueOne(WgoData station, CraftDef def, List<NeedItemData> needList)
        {
            var paramsData = new CraftParamsData(def.id, station);
            // Wie das Craft-Fenster: mit dem Arbeiter der Station rechnen (Zombie), ohne Arbeiter mit dem Spieler.
            paramsData.RecalculateParams(needList, station.Worker ?? MainGame.PlayerController);
            var element = new CraftElement(def.id, 1, needList, paramsData);

            // Fehlt nur Brennstoff, trotzdem einreihen – die Station wartet dann und "Refuel from chests" springt ein.
            CraftComponent component = station.CraftComponent;
            CraftStatus status = component.GetStartCraftStatus(element);
            if (status != CraftStatus.OK && status != CraftStatus.NotEnoughResources) return false;

            component.AddToQueue(element, addToQueueTop: true);
            component.TryContinueFromQueue();
            Delivery.LogSkipOnce(station, $"picks {def.id} from the chest next to it");
            return true;
        }

        /// <summary>Welche Rezepte gerade an anderen Stationen im selben Gebiet laufen oder warten (je Station einmal).</summary>
        private static void CountRecipesInUse(WgoData station, WorldZoneData zone)
        {
            recipesInUse.Clear();
            List<CraftComponent> active = MainGame.Instance?.GameSave?.craftSystemData?.activeCrafts;
            if (active == null || zone == null) return;

            foreach (CraftComponent craft in active)
            {
                if (!(craft?.CraftableObject is WgoData other) || other == station || other.WorldZoneData != zone) continue;
                recipesSeen.Clear();
                foreach (CraftElementBase element in craft.CraftElementsQueue)
                {
                    string id = element?.Def?.id;
                    if (id != null && recipesSeen.Add(id)) recipesInUse[id] = recipesInUse.TryGetValue(id, out int n) ? n + 1 : 1;
                }
            }
        }

        /// <summary>Das Hauptergebnis eines Rezepts: der erste feste Eintrag der Ausgabe (null, wenn es keinen gibt).</summary>
        private static string MainOutputId(CraftDef def)
        {
            List<ChanceOutputItem> outputs = def.outputItems?.chanceOutputItems;
            if (outputs == null) return null;
            foreach (ChanceOutputItem output in outputs)
            {
                if (!string.IsNullOrEmpty(output?.id)) return output.id;
            }
            return null;
        }

        /// <summary>Vorrat eines Items in allen Lagern des Gebiets.</summary>
        private static int CountInZone(WorldZoneData zone, string itemId)
        {
            if (itemId == null || zone?.MultiInventoryWgoDatas == null) return 0;
            int total = 0;
            foreach (WgoData storage in zone.MultiInventoryWgoDatas)
            {
                if (storage?.Inventory?.Data != null) total += storage.Inventory.Data.GetTotalCountInInventory(itemId);
            }
            return total;
        }

        private sealed class Candidate
        {
            internal CraftDef Def;
            internal List<NeedItemData> Needs;
            internal int Busy;
            internal int Stock;
            internal int Ingredients;
            internal bool IsLast;
            internal int Order;
        }

        private static int CountInInputChests(string itemId)
        {
            int total = 0;
            foreach (WgoData chest in inputChests) total += chest.Inventory.Data.GetTotalCountInInventory(itemId);
            return total;
        }
    }
}
