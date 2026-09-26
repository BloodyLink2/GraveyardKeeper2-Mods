using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace FlexibleCeremonies
{
    /// <summary>
    /// PrayerStandInteractionHandler.Interact bricht mit "Zeremonie am Tag des Zorns verfügbar" ab, wenn
    /// PlayerData.GetRes("sermon_ready") &lt; 1 ist. Genau dieser eine Aufruf wird auf
    /// <see cref="CeremonyGate.GetSermonReady"/> umgeleitet (gleiche Parameter, Instanz wird erstes Argument).
    /// </summary>
    [HarmonyPatch(typeof(PrayerStandInteractionHandler), nameof(PrayerStandInteractionHandler.Interact))]
    internal static class PrayerStandPatch
    {
        private static readonly MethodInfo GetRes =
            AccessTools.Method(typeof(PlayerData), nameof(PlayerData.GetRes), new[] { typeof(string), typeof(float) });

        private static readonly MethodInfo Gate =
            AccessTools.Method(typeof(CeremonyGate), nameof(CeremonyGate.GetSermonReady));

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
            CallRedirect.Apply(instructions, "sermon_ready", GetRes, Gate, "Prayer stand check");
    }

    /// <summary>
    /// "Gratitude ignorieren": Der Konstruktor von UIPrayWindowData rechnet Besucher = min(Gratitude, Kirchenqualität).
    /// Davon hängen Mindestbesucher-Prüfung und Erfolgschance (Besucher * 100 / Schwierigkeit) ab – wir setzen
    /// die Besucher auf die Kirchenqualität.
    /// </summary>
    [HarmonyPatch(typeof(UIPrayWindowData), MethodType.Constructor,
        typeof(PlayerData), typeof(WgoData), typeof(int), typeof(int), typeof(int), typeof(string))]
    internal static class PrayWindowVisitorsPatch
    {
        private static readonly Action<UIPrayWindowData, int> SetResultVisitors =
            AccessTools.MethodDelegate<Action<UIPrayWindowData, int>>(
                AccessTools.PropertySetter(typeof(UIPrayWindowData), nameof(UIPrayWindowData.ResultVisitors)));

        private static void Postfix(UIPrayWindowData __instance)
        {
            if (Plugin.IgnoreGratitudeActive) SetResultVisitors(__instance, __instance.ChurchQuality);
        }
    }

    /// <summary>
    /// "Gratitude ignorieren": UIPrayWindowData.StartCraft zieht die Besucher per PlayerData.SubRes("happiness", …)
    /// von der Gratitude ab. Genau dieser eine Aufruf wird auf <see cref="CeremonyGate.SubGratitude"/> umgeleitet.
    /// </summary>
    [HarmonyPatch(typeof(UIPrayWindowData), nameof(UIPrayWindowData.StartCraft))]
    internal static class SermonGratitudeCostPatch
    {
        private static readonly MethodInfo SubRes =
            AccessTools.Method(typeof(PlayerData), nameof(PlayerData.SubRes), new[] { typeof(string), typeof(float) });

        private static readonly MethodInfo Gate =
            AccessTools.Method(typeof(CeremonyGate), nameof(CeremonyGate.SubGratitude));

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
            CallRedirect.Apply(instructions, "happiness", SubRes, Gate, "Gratitude cost");
    }

    /// <summary>
    /// UIPrayWindowData.StartCraft wird aufgerufen, wenn der Spieler im Gebetsfenster eine Zeremonie startet.
    /// </summary>
    [HarmonyPatch(typeof(UIPrayWindowData), nameof(UIPrayWindowData.StartCraft))]
    internal static class CeremonyStartPatch
    {
        private static void Postfix() => CeremonyCounter.Record();
    }

    internal static class CallRedirect
    {
        /// <summary>
        /// Ersetzt den ersten Aufruf von <paramref name="original"/> nach dem String <paramref name="key"/> durch den
        /// statischen <paramref name="replacement"/> (gleiche Parameter, Instanz wird erstes Argument).
        /// Loggt, ob genau eine Stelle gefunden wurde.
        /// </summary>
        internal static IEnumerable<CodeInstruction> Apply(IEnumerable<CodeInstruction> instructions, string key,
            MethodInfo original, MethodInfo replacement, string what)
        {
            var codes = new List<CodeInstruction>(instructions);
            bool afterKey = false;
            int replaced = 0;

            foreach (CodeInstruction code in codes)
            {
                if (code.opcode == OpCodes.Ldstr && code.operand as string == key)
                {
                    afterKey = true;
                }
                else if (afterKey && code.Calls(original))
                {
                    code.opcode = OpCodes.Call;
                    code.operand = replacement;
                    afterKey = false;
                    replaced++;
                }
            }

            if (replaced == 1)
                Plugin.Log($"{what} redirected.");
            else
                Plugin.LogWarning($"{what} not found ({replaced} matches). This part of the mod has no effect. " +
                                  "A game update probably changed the code.");
            return codes;
        }
    }
}
