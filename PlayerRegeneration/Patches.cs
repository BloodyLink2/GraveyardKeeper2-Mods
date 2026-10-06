using HarmonyLib;

namespace PlayerRegeneration
{
    /// <summary>
    /// PlayerController.Update läuft jeden Frame, solange ein Spieler in der Welt existiert –
    /// im Hauptmenü also nicht. Ein guter Takt für die Regeneration.
    /// </summary>
    [HarmonyPatch(typeof(PlayerController), "Update")]
    internal static class PlayerUpdatePatch
    {
        private static void Postfix() => Regeneration.Update();
    }

    /// <summary>
    /// EnergySystem.TrackTimeWithoutSleep zählt die Spielzeit (in Tagen) seit dem letzten Schlaf;
    /// ab 2 Tagen gibt es den Debuff "lack_of_sleep_debuff". Wir lassen die Zeit einfach langsamer zählen.
    /// Schlafen setzt den Zähler wie gewohnt zurück.
    /// </summary>
    [HarmonyPatch(typeof(EnergySystem), "TrackTimeWithoutSleep")]
    internal static class SleepDeprivationPatch
    {
        private static void Prefix(ref float dayDeltaTime)
        {
            if (!Plugin.IsActive) return;
            dayDeltaTime *= Plugin.SleepDeprivationSpeed.Value;
        }
    }

    /// <summary>
    /// Schlafen trotz voller Energie: StartSleeping bricht bei vollem Balken ab (Dialog "no_need_to_sleep"),
    /// außer der Aufrufer setzt sleepWithMaxEnergy. Da das Spiel nur beim Schlafen speichert, setzen wir es immer.
    /// Bei voller Energie beendet RestoreEnergyWhileSleeping den Schlaf nach der Mindestdauer und speichert.
    /// </summary>
    [HarmonyPatch(typeof(EnergySystem), nameof(EnergySystem.StartSleeping))]
    internal static class SleepWithFullEnergyPatch
    {
        private static void Prefix(ref bool sleepWithMaxEnergy)
        {
            if (Plugin.SleepWithFullEnergyActive) sleepWithMaxEnergy = true;
        }
    }

    /// <summary>
    /// Unendlich Leben: Jeder Schaden am Spieler läuft über HPComponent.ApplyDamage – Nahkampf,
    /// Flächenschaden, Geschütze und Skript-Effekte (AddPlayerHP mit negativem Wert).
    /// Die Methode gilt für alle Wesen, deshalb nur die HP-Komponente des Spielers abfangen.
    /// </summary>
    [HarmonyPatch(typeof(HPComponent), nameof(HPComponent.ApplyDamage))]
    internal static class InfiniteHealthPatch
    {
        private static bool Prefix(HPComponent __instance)
        {
            return !(Plugin.InfiniteHealthActive && __instance == MainGame.PlayerData?.hpComponent);
        }
    }

    /// <summary>
    /// Unendlich Energie: Aller Energieverbrauch (Arbeit, Werkzeuge, Rezepte) ist ein Add mit negativem Wert.
    /// Übersprungen wird dabei auch der Wahnsinn, den das Spiel bei Schlafmangel für verbrauchte Energie aufschlägt.
    /// </summary>
    [HarmonyPatch(typeof(PlayerEnergyGameResSystem), nameof(PlayerEnergyGameResSystem.Add))]
    internal static class InfiniteEnergyConsumePatch
    {
        private static bool Prefix(float value) => !(value < 0f && Plugin.InfiniteEnergyActive);
    }

    /// <summary>
    /// Unendlich Energie: Alle "Genug Energie?"-Prüfungen fragen IsEnoughValue. Ohne diesen Patch würden Aufgaben
    /// bei viel Wahnsinn trotzdem scheitern, weil das Energie-Maximum (100 - Wahnsinn) unter den Kosten liegt.
    /// </summary>
    [HarmonyPatch(typeof(GK2GameResSystem), nameof(GK2GameResSystem.IsEnoughValue))]
    internal static class InfiniteEnergyCheckPatch
    {
        private static bool Prefix(GK2GameResSystem __instance, ref bool __result)
        {
            if (!(__instance is PlayerEnergyGameResSystem) || !Plugin.InfiniteEnergyActive) return true;
            __result = true;
            return false;
        }
    }

    /// <summary>
    /// "Your work speed at tables": Arbeitet der Spieler selbst an einer Werkbank, am Autopsie-, Einbalsamierungs- oder
    /// Forschungstisch, gibt PlayerCraftActivity.UseTool das Ergebnis von GetActionDamage (Felder Fortschritt pro
    /// Schlag, höchstens max_cells_per_one_hit) an CraftComponent.UpdateManual weiter. Wir vervielfachen es; der
    /// Nachkommaanteil wird gesammelt, damit auch 1.5x aufgeht. 0 (Meisterschaft zu niedrig) bleibt 0, und UpdateManual
    /// kappt selbst am Rezeptende. Energie kostet weiter jeder Schlag.
    /// </summary>
    [HarmonyPatch(typeof(PlayerCraftActivity), nameof(PlayerCraftActivity.GetActionDamage))]
    internal static class WorkSpeedPatch
    {
        private static float remainder;

        private static void Postfix(ref int __result)
        {
            if (!Plugin.IsActive || __result <= 0) return;
            float speed = Plugin.WorkSpeed.Value;
            if (speed <= 1f) return;

            float total = __result * speed + remainder;
            __result = (int)total;
            remainder = total - __result;
        }
    }

    /// <summary>
    /// "Stamina regeneration speed": StaminaSystem.UpdateStaminaLogic zählt mit deltaTime erst die Pause nach einem Angriff
    /// (stamina_regeneration_delay) ab und füllt dann stamina_regeneration pro Sekunde auf. Aufgerufen wird es nur im
    /// Kampf (FightingGameController.Update). Wir lassen für die Ausdauer die Zeit schneller laufen. Mit "Infinite
    /// stamina" bleibt der Balken einfach voll.
    /// </summary>
    [HarmonyPatch(typeof(StaminaSystem), nameof(StaminaSystem.UpdateStaminaLogic))]
    internal static class StaminaRegenerationPatch
    {
        private static bool Prefix(ref float deltaTime)
        {
            if (!Plugin.IsActive) return true;
            if (Plugin.InfiniteStamina.Value)
            {
                PlayerStaminaGameResSystem stamina = PlayerStaminaGameResSystem.GetSystem();
                if (stamina != null && !stamina.HasMax()) stamina.Set(stamina.Max);
                return false;
            }
            deltaTime *= Plugin.StaminaRegenerationSpeed.Value;
            return true;
        }
    }

    /// <summary>
    /// "Infinite stamina": Jeder Angriff (Schwert, Speer, Bogen) zieht über StaminaSystem.ConsumeStamina die Kosten der
    /// Waffe ab. Ohne Abzug bleibt der Balken voll, und CanPerformAttack lässt jeden Angriff zu.
    /// </summary>
    [HarmonyPatch(typeof(StaminaSystem), nameof(StaminaSystem.ConsumeStamina))]
    internal static class InfiniteStaminaPatch
    {
        private static bool Prefix() => !Plugin.InfiniteStaminaActive;
    }

    /// <summary>FightingGameController.Update läuft jeden Frame, solange eine Kampfebene geladen ist.</summary>
    [HarmonyPatch(typeof(FightingGameController), "Update")]
    internal static class FightUpdatePatch
    {
        private static void Postfix(FightingGameController __instance) => ZombieRegeneration.Update(__instance);
    }
}
