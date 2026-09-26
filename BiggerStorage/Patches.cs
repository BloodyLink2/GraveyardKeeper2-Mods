using HarmonyLib;

namespace BiggerStorage
{
    /// <summary>
    /// WgoData.SetDataFromDefinition legt beim Bau einer Truhe ihr Inventar in der Größe aus der Definition an –
    /// direkt danach bekommt sie die Zusatzplätze.
    /// </summary>
    [HarmonyPatch(typeof(WgoData), nameof(WgoData.SetDataFromDefinition))]
    internal static class NewChestPatch
    {
        private static void Postfix(WgoData __instance) => ChestSlots.Apply(__instance, out _);
    }

    /// <summary>
    /// Item.CanAddItemCount rechnet stackCount - Count. Wurde die Stapelgröße verkleinert, kann ein vorhandener Stapel
    /// größer sein, dann käme eine negative Zahl heraus. Das Einlagern überspringt das zwar, TryAddItemPartial würde aber
    /// Stück zurückschieben – wir melden stattdessen einfach "passt nichts mehr drauf".
    /// </summary>
    [HarmonyPatch(typeof(Item), nameof(Item.CanAddItemCount), typeof(Item), typeof(int))]
    internal static class OversizedStackPatch
    {
        private static void Postfix(ref int __result)
        {
            if (__result < 0) __result = 0;
        }
    }
}
