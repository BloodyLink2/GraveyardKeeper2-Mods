using System.Runtime.CompilerServices;
using GK2.FlowCanvasNodes;
using NodeCanvas.Framework;

namespace ResurrectEveryDay
{
    /// <summary>
    /// Die Wiederbelebung löst das FlowCanvas-Skript "power_switch_table" (Stromschalter im Labor) aus:
    /// Ohne Buff "buff_storm" (Prayer for Storms) muss heute Envy sein (Flow_GetDayNumber, Tag 3) und die
    /// Spieler-Ressource "resurrection_has_power" = 1. Die setzt ein Zeit-Event am Ende von Lust auf 1,
    /// Flow_StartResurrection verbraucht sie wieder. Wir beantworten nur diese beiden Abfragen anders –
    /// Tische vorbereiten, Organe, Halsbänder usw. bleiben unverändert.
    /// </summary>
    internal static class ResurrectionGate
    {
        internal const string PowerKey = "resurrection_has_power";

        private static readonly object Yes = new object();
        private static readonly object No = new object();

        // Pro Graph-Instanz gemerkt, ob er einen Flow_StartResurrection-Knoten enthält – nur dort greift "jeder Tag".
        private static readonly ConditionalWeakTable<Graph, object> resurrectionGraphs = new ConditionalWeakTable<Graph, object>();

        internal static bool IsResurrectionGraph(Graph graph)
        {
            if (graph == null) return false;
            return resurrectionGraphs.GetValue(graph,
                g => g.allNodes != null && g.allNodes.Exists(n => n is Flow_StartResurrection) ? Yes : No) == Yes;
        }

        /// <summary>Ersatz für PlayerData.GetRes(name, default) in Flow_GetGameRes.ChangeGameRes.</summary>
        internal static float GetPlayerRes(PlayerData player, string type, float defaultValue)
        {
            if (type == PowerKey && Plugin.UnlimitedPowerActive) return 1f;
            return player.GetRes(type, defaultValue);
        }
    }
}
