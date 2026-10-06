using System.Collections.Generic;
using HarmonyLib;
using LazyBearTechnology;
using TMPro;

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

    /// <summary>Füllstand-Blase: Wgo.InitDataBindings verbindet eine Objekt-Ansicht mit ihren Daten.</summary>
    [HarmonyPatch(typeof(Wgo), "InitDataBindings")]
    internal static class StorageViewInitPatch
    {
        private static void Postfix(Wgo __instance) => StorageBubble.Attach(__instance);
    }

    /// <summary>Füllstand-Blase: Wgo.DeInit löst die Ansicht wieder von ihren Daten.</summary>
    [HarmonyPatch(typeof(Wgo), "DeInit")]
    internal static class StorageViewDeInitPatch
    {
        private static void Prefix(Wgo __instance) => StorageBubble.Detach(__instance);
    }

    /// <summary>Füllstand-Blase: Wgo.GetWidgetData liefert alles, was über dem Objekt angezeigt wird.</summary>
    [HarmonyPatch(typeof(Wgo), nameof(Wgo.GetWidgetData))]
    internal static class StorageBubblePatch
    {
        private static void Postfix(Wgo __instance, List<LazyWidgetDataBase> __result) =>
            StorageBubble.AddWidget(__instance, __result);
    }

    /// <summary>Füllstand-Blase: UIQualityTooltipWidget.Draw zeichnet Icon und Zahl, bei uns kommt das Item-Icon dazu.</summary>
    [HarmonyPatch(typeof(UIQualityTooltipWidget), nameof(UIQualityTooltipWidget.Draw))]
    internal static class StorageBubbleIconPatch
    {
        private static void Postfix(UIQualityTooltipWidgetData data, TextMeshProUGUI ___iconLabel) =>
            StorageBubble.DrawIcon(data, ___iconLabel);
    }
}
