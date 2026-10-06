using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using GK2.FlowCanvasNodes;
using HarmonyLib;

namespace ResurrectEveryDay
{
    /// <summary>
    /// Flow_GetDayNumber meldet über den Ausgang "isSelectedDayToday" (ein vom Compiler erzeugtes Lambda,
    /// Instanzmethode des Knotens), ob heute der gewählte Tag ist. Im Stromschalter-Skript ist das Envy –
    /// dort melden wir immer "ja". Andere Skripte mit demselben Knoten bleiben unberührt.
    /// </summary>
    [HarmonyPatch]
    internal static class ResurrectionDayPatch
    {
        private static MethodInfo target;

        private static bool Prepare()
        {
            target = AccessTools.GetDeclaredMethods(typeof(Flow_GetDayNumber)).FirstOrDefault(m =>
                !m.IsStatic && m.ReturnType == typeof(bool) && m.GetParameters().Length == 0 &&
                m.Name.Contains("RegisterPorts"));

            if (target == null)
                Plugin.LogWarning("Day check not found, so \"Resurrect on any day\" has no effect. " +
                                  "A game update probably changed the code.");
            else
                Plugin.Log("Day check hooked.");
            return target != null;
        }

        private static MethodBase TargetMethod() => target;

        private static void Postfix(Flow_GetDayNumber __instance, ref bool __result)
        {
            if (__result || !Plugin.AnyDayActive) return;
            if (ResurrectionGate.IsResurrectionGraph(__instance.graph)) __result = true;
        }
    }

    /// <summary>
    /// Flow_GetGameRes.ChangeGameRes liest Spieler-Ressourcen per PlayerData.GetRes(name, default). Dieser eine
    /// Aufruf wird auf <see cref="ResurrectionGate.GetPlayerRes"/> umgeleitet, das nur für
    /// "resurrection_has_power" etwas anderes liefert.
    /// </summary>
    [HarmonyPatch(typeof(Flow_GetGameRes), "ChangeGameRes")]
    internal static class ResurrectionPowerCheckPatch
    {
        private static readonly MethodInfo GetRes =
            AccessTools.Method(typeof(PlayerData), nameof(PlayerData.GetRes), new[] { typeof(string), typeof(float) });

        private static readonly MethodInfo Gate =
            AccessTools.Method(typeof(ResurrectionGate), nameof(ResurrectionGate.GetPlayerRes));

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            int replaced = 0;

            foreach (CodeInstruction code in codes)
            {
                if (!code.Calls(GetRes)) continue;
                code.opcode = OpCodes.Call;
                code.operand = Gate;
                replaced++;
            }

            if (replaced == 1)
                Plugin.Log("Power check redirected.");
            else
                Plugin.LogWarning($"Power check not found ({replaced} matches), so \"Unlimited storm power\" has no effect. " +
                                  "A game update probably changed the code.");
            return codes;
        }
    }

    /// <summary>
    /// Flow_StartResurrection setzt "resurrection_has_power" nach dem Wiederbeleben auf 0. Mit unbegrenzter Energie
    /// stellen wir den vorherigen Wert wieder her – so bleibt der Spielstand wie ohne Mod, falls man sie später abschaltet.
    /// </summary>
    [HarmonyPatch(typeof(Flow_StartResurrection), "StartResurrection")]
    internal static class ResurrectionPowerKeepPatch
    {
        private static void Prefix(out float __state)
        {
            __state = MainGame.PlayerData.GetRes(ResurrectionGate.PowerKey);
        }

        private static void Postfix(float __state)
        {
            if (Plugin.UnlimitedPowerActive) MainGame.PlayerData.SetRes(ResurrectionGate.PowerKey, __state);
        }
    }

    /// <summary>
    /// PanicReductionMachineInteractionHandler.Interact bricht mit "PR Machine available on (Gluttony)" ab, wenn
    /// EnvironmentData.CurrentDayNumber != ConstDef.Get("day_gluttony").IntValue ist. Genau dieser eine
    /// IntValue-Aufruf wird auf <see cref="PrMachineGate.GetAllowedDay"/> umgeleitet (Instanz wird erstes Argument).
    /// </summary>
    [HarmonyPatch(typeof(PanicReductionMachineInteractionHandler), nameof(PanicReductionMachineInteractionHandler.Interact))]
    internal static class PrMachineDayPatch
    {
        private static readonly MethodInfo IntValue =
            AccessTools.PropertyGetter(typeof(ConstDef), nameof(ConstDef.IntValue));

        private static readonly MethodInfo Gate =
            AccessTools.Method(typeof(PrMachineGate), nameof(PrMachineGate.GetAllowedDay));

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            bool afterKey = false;
            int replaced = 0;

            foreach (CodeInstruction code in codes)
            {
                if (code.opcode == OpCodes.Ldstr && code.operand as string == PrMachineGate.DayKey)
                {
                    afterKey = true;
                }
                else if (afterKey && code.Calls(IntValue))
                {
                    code.opcode = OpCodes.Call;
                    code.operand = Gate;
                    afterKey = false;
                    replaced++;
                }
            }

            if (replaced == 1)
                Plugin.Log("PR Machine day check redirected.");
            else
                Plugin.LogWarning($"PR Machine day check not found ({replaced} matches), so \"PR Machine on any day\" has no effect. " +
                                  "A game update probably changed the code.");
            return codes;
        }
    }
}
