using System;
using HarmonyLib;

namespace ZombieProductivity
{
    /// <summary>
    /// ZombieCraftActivity.Update sammelt Zeit; pro voller Sekunde entsteht ein Sub-Tick,
    /// nach "zombie_craft_sub_ticks_count" Sub-Ticks gibt es echten Craft-Fortschritt.
    /// Wir skalieren einfach die vergangene Zeit. Gilt für Werkbank-Zombies, Fließband-Zombies und Gärtner beim Säen.
    /// Fällig gewordene Schläge holt <see cref="ZombieCraftExtraHitsPatch"/> nach.
    /// </summary>
    [HarmonyPatch(typeof(ZombieCraftActivity), nameof(ZombieCraftActivity.Update))]
    internal static class ZombieCraftSpeedPatch
    {
        private static void Prefix(ref float deltaTime)
        {
            if (!Plugin.IsActive) return;
            deltaTime *= Plugin.SpeedMultiplier.Value;
        }
    }

    /// <summary>
    /// Gärtner beim Ernten laufen nicht über das Crafting, sondern über ZombieHPActivity.Update:
    /// gleiches Prinzip (pro voller Sekunde ein Schlag auf die Pflanze).
    /// </summary>
    [HarmonyPatch(typeof(ZombieHPActivity), nameof(ZombieHPActivity.Update))]
    internal static class ZombieHarvestSpeedPatch
    {
        private static void Prefix(ref float deltaTime)
        {
            if (!Plugin.IsActive) return;
            deltaTime *= Plugin.SpeedMultiplier.Value;
        }
    }

    /// <summary>
    /// Update übergibt UseTool, wie viele Schläge fällig sind (deltaTick), UseTool führt aber immer nur einen aus.
    /// Da das CraftSystem Update nur alle paar Zehntelsekunden aufruft, war bisher bei etwa 5x Schluss.
    /// Wir holen die restlichen Schläge nach – aber nur, solange der Craft noch läuft.
    /// </summary>
    [HarmonyPatch(typeof(ZombieCraftActivity), nameof(ZombieCraftActivity.UseTool))]
    internal static class ZombieCraftExtraHitsPatch
    {
        private static readonly Func<ZombieCraftActivity, CraftComponent> GetCraftComponent =
            AccessTools.MethodDelegate<Func<ZombieCraftActivity, CraftComponent>>(
                AccessTools.PropertyGetter(typeof(ZombieCraftActivity), "CraftComponent"));

        private static bool repeating;

        private static void Postfix(ZombieCraftActivity __instance, Item tool, int deltaTick)
        {
            if (repeating || deltaTick <= 1 || !Plugin.IsActive) return;
            repeating = true;
            try
            {
                for (int i = 1; i < deltaTick && IsStillCrafting(GetCraftComponent(__instance)); i++)
                    __instance.UseTool(tool, 1);
            }
            finally
            {
                repeating = false;
            }
        }

        private static bool IsStillCrafting(CraftComponent craft) =>
            craft != null && craft.Status == CraftComponentStatus.Started && !craft.HasPreFinishUpdate &&
            craft.CurrentCraftElement != null && craft.CurrentCraftElement.IsStarted;
    }

    /// <summary>Wie <see cref="ZombieCraftExtraHitsPatch"/>, nur für Gärtner beim Ernten: Schläge bis die Pflanze ab ist.</summary>
    [HarmonyPatch(typeof(ZombieHPActivity), nameof(ZombieHPActivity.UseTool))]
    internal static class ZombieHarvestExtraHitsPatch
    {
        private static readonly Func<ZombieHPActivity, WgoData> GetTarget =
            AccessTools.MethodDelegate<Func<ZombieHPActivity, WgoData>>(
                AccessTools.PropertyGetter(typeof(ZombieHPActivity), "WgoData"));

        private static bool repeating;

        private static void Postfix(ZombieHPActivity __instance, Item tool, int deltaTick)
        {
            if (repeating || deltaTick <= 1 || !Plugin.IsActive) return;
            repeating = true;
            try
            {
                for (int i = 1; i < deltaTick && GetTarget(__instance)?.HpComponent?.Hp > 0; i++)
                    __instance.UseTool(tool, 1);
            }
            finally
            {
                repeating = false;
            }
        }
    }

    /// <summary>
    /// Meisterschaft eines Zombies = Talentwert + Werkzeug-Bonus + Perks. Wir addieren den Bonus obendrauf.
    /// Wirkt auf Fortschritt pro Schlag (gedeckelt durch "max_cells_per_one_hit" = 3) und auf Mastery-Sperren.
    /// Betrifft nur Zombies – der Spieler nutzt eine eigene Methode.
    /// </summary>
    [HarmonyPatch(typeof(ZombieWgoData), nameof(ZombieWgoData.GetMasteryLevelForTalentBranch))]
    internal static class ZombieMasteryPatch
    {
        private static void Postfix(ref int __result)
        {
            if (!Plugin.IsActive) return;
            int bonus = Plugin.MasteryBonus.Value;
            if (bonus > 0) __result += bonus;
        }
    }
}
