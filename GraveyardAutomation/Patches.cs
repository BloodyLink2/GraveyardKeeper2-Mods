using HarmonyLib;
using UnityEngine;

namespace GraveyardAutomation
{
    /// <summary>
    /// CraftSystem.CustomUpdate treibt alle Crafts an. Direkt danach prüfen wir (einmal pro Sekunde), ob fertige
    /// Ergebnisse in eine Truhe wandern können – und alle paar Sekunden, ob Brunnen geleert und Bienenstöcke geerntet
    /// werden können.
    /// </summary>
    [HarmonyPatch(typeof(CraftSystem), nameof(CraftSystem.CustomUpdate))]
    internal static class CraftSystemUpdatePatch
    {
        private static void Postfix(float deltaTime)
        {
            Delivery.Update(deltaTime);
            AutoRecipes.Update(deltaTime);
            Wells.Update(deltaTime);
            Beehives.Update(deltaTime);
        }
    }

    /// <summary>
    /// "Station speed": Selbstlaufende Stationen (Öfen, Destillierwürfel, ...) zählen in CraftComponent.Update die Zeit
    /// hoch und rechnen fällige Fortschritts-Ticks per FloorToInt(Zeit / Tick-Dauer) aus – auch mehrere auf einmal.
    /// Wir beschleunigen nur, während wirklich produziert wird (Status Started); das sekündliche Nachprüfen beim Warten
    /// auf Zutaten bleibt normal. Ausgenommen: Beete, Stadtgebäude-Bau, versteckte System-Crafts und Zombie-Stationen.
    /// </summary>
    [HarmonyPatch(typeof(CraftComponent), nameof(CraftComponent.Update))]
    internal static class StationSpeedPatch
    {
        private static void Prefix(CraftComponent __instance, ref float deltaTime)
        {
            if (!Plugin.IsActive) return;
            float speed = Plugin.StationSpeed.Value;
            if (speed <= 1f || __instance.Status != CraftComponentStatus.Started) return;

            CraftElementBase craft = __instance.CurrentCraftElement;
            CraftDefBase def = craft?.Def;
            if (def == null || !craft.IsStarted || !def.isAuto || def.isHidden || def.autoFinishAutoCraft) return;
            if (craft.ParamsData.craftParamsType == CraftParamsData.CraftParamsType.GardenGrowing) return;
            if (__instance.CraftableObject.CraftableAttachedWorker is ZombieWgoData) return;
            if (__instance.CraftableObject is WgoData station && !StationSwitches.Allows(station)) return;

            deltaTime *= speed;
        }
    }

    /// <summary>
    /// "Pick recipes from nearby chests": Stationen entnehmen Zutaten der Reihe nach aus dieser Liste – die Truhen
    /// neben der Station kommen nach vorn (siehe <see cref="AutoRecipes.PrioritizeInputChests"/>).
    /// </summary>
    [HarmonyPatch(typeof(WgoData), nameof(WgoData.GetCraftableMultiInventory))]
    internal static class InputChestsFirstPatch
    {
        private static void Postfix(WgoData __instance, MultiInventory __result) =>
            AutoRecipes.PrioritizeInputChests(__instance, __result);
    }

    /// <summary>
    /// WgoData.MakeDrop legt ein Item vor der Station auf den Boden. Während die Mod einen Auto-Craft abschließt,
    /// landet es stattdessen in der Truhe (siehe <see cref="Delivery.TryRedirectDrop"/>).
    /// </summary>
    [HarmonyPatch(typeof(WgoData), nameof(WgoData.MakeDrop), typeof(Item))]
    internal static class MakeDropPatch
    {
        private static bool Prefix(WgoData __instance, Item item) => !Delivery.TryRedirectDrop(__instance, item);
    }

    /// <summary>
    /// DropSystem.DropTechPoints lässt Forschungspunkte immer in der Szene des Spielers erscheinen. Während die Mod
    /// eine Station abschließt, gehen sie direkt an den Spieler ("Collect research points") oder landen an der Station,
    /// auch wenn die in einer anderen Szene steht.
    /// </summary>
    [HarmonyPatch(typeof(DropSystem), nameof(DropSystem.DropTechPoints))]
    internal static class DropTechPointsPatch
    {
        private static bool Prefix(Vector3 pos, int r, int g, int b) => !Delivery.TryHandleStationTechPoints(pos, r, g, b);
    }

    /// <summary>
    /// "Repeat recipes": Legt der Spieler im Craft-Fenster eines Ofens (Auto-Craft) oder einer Zombie-Werkbank ein Rezept
    /// mit Anzahl 1 in die Warteschlange, machen wir 999 daraus – das Maximum, das auch das Fenster zulässt.
    /// Bewusst hier und nicht in CraftComponent.AddToQueue: Das nutzt das Spiel auch intern (gestartete Crafts,
    /// Wiederbelebung, Neustarts nach dem Abholen).
    /// </summary>
    [HarmonyPatch(typeof(CraftInteractionHandler), "OnCraftPressed")]
    internal static class RepeatRecipesPatch
    {
        internal const int RepeatCount = 999;

        private static void Prefix(CraftElement craftElement, Wgo ___assignedWgo)
        {
            if (!Plugin.IsActive || craftElement == null) return;
            CraftDef def = craftElement.Definition;
            WgoData station = ___assignedWgo != null ? ___assignedWgo.Data : null;
            if (def == null || station == null) return;

            // Für "Pick recipes from nearby chests": bei Gleichstand gewinnt das zuletzt selbst eingereihte Rezept.
            AutoRecipes.RememberRecipe(station, def.id);

            // Anzahl bewusst im Fenster gewählt – dann bleibt sie.
            if (!Plugin.RepeatRecipes.Value || craftElement.Count != 1 || craftElement.IsInfinite) return;
            // Sofort-Crafts liefen 999-mal auf einmal, Brennstoff regelt FuelSupply, manche Rezepte erlauben nur einen
            // Eintrag, und Rezepte mit Dingen, die man von Hand in die Station legt (z. B. Leichen), bleiben wie sie sind.
            if (def.skipQueue || def.isFuelCraft || def.IsMultipleCraftsDisabled || def.needItemsFromWgo.Count > 0) return;

            // Nur Stationen, die von selbst arbeiten: Auto-Crafts (Öfen) und Zombie-Werkbänke.
            if ((def.isAuto || station.Worker is ZombieWgoData) && StationSwitches.Allows(station)) craftElement.Count = RepeatCount;
        }
    }
}
