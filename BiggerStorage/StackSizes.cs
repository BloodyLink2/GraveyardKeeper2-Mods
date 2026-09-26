using System.Collections.Generic;

namespace BiggerStorage
{
    /// <summary>
    /// "Stack size": Wie viele Stück auf einen Platz passen, liest das Spiel überall direkt aus ItemDef.stackCount
    /// (Einlagern, Zusammenführen, Aufteilen, Drops, Craft-Ausgabe, Oberfläche). Eine Sonderregel nur für Truhen hätte
    /// all diese Stellen abfangen müssen; deshalb setzen wir den Wert selbst – für alle Inventare gleich.
    /// Nur stapelbare Gegenstände (normal mehr als 1), und nie unter den Normalwert. Die Normalwerte merken wir uns beim
    /// ersten Anwenden, so lässt sich die Einstellung jederzeit ändern oder zurücknehmen.
    /// </summary>
    internal static class StackSizes
    {
        private static readonly Dictionary<ItemDef, int> vanilla = new Dictionary<ItemDef, int>();

        internal static void Apply()
        {
            // Erst mit laufendem Spielstand – vorher nicht versehentlich das Laden der Spieldaten anstoßen.
            if (MainGame.Instance?.GameSave == null) return;
            List<ItemDef> defs = GameBalance.Me?.itemDefs;
            if (defs == null) return;

            int size = Plugin.EffectiveStackSize;
            int changed = 0;
            foreach (ItemDef def in defs)
            {
                if (def == null) continue;
                if (!vanilla.TryGetValue(def, out int original))
                {
                    original = def.stackCount;
                    vanilla[def] = original;
                }

                int target = original > 1 && size > original ? size : original;
                if (def.stackCount == target) continue;
                def.stackCount = target;
                changed++;
            }
            if (changed > 0) Plugin.Log($"Stack size {(size > 0 ? size.ToString() : "normal")} applied to {changed} item type(s).");
        }
    }
}
