using System;
using UnityEngine;

namespace PlayerRegeneration
{
    /// <summary>
    /// Füllt Leben und Energie des Spielers in Echtzeit auf und baut Wahnsinn ab. Gesammelt wird pro Frame,
    /// angewendet einmal pro Sekunde – so feuern die UI-Events des Spiels nicht jeden Frame.
    /// </summary>
    internal static class Regeneration
    {
        private const float TickInterval = 1f;
        // Nach Ladepausen o. Ä. nicht auf einen Schlag nachholen.
        private const float MaxTickSeconds = 2f;

        private static int lastFrame = -1;
        private static float elapsed;
        private static float healthRemainder;
        private static bool errorLogged;

        internal static void Update()
        {
            // Nur einmal pro Frame, falls es doch mehrere PlayerController geben sollte.
            if (Time.frameCount == lastFrame) return;
            lastFrame = Time.frameCount;

            if (!Plugin.IsActive || MainGame.IsGamePaused) return;

            PlayerData player = MainGame.PlayerData;
            if (player == null) return;

            // Im Schlaf regeneriert das Spiel selbst.
            EnergySystem sleep = player.energySystem;
            if (sleep != null && (sleep.IsSleeping || sleep.IsInTransitionBetweenSleep)) return;

            elapsed += Time.deltaTime;
            if (elapsed < TickInterval) return;
            float seconds = Mathf.Min(elapsed, MaxTickSeconds);
            elapsed = 0f;

            try
            {
                HPComponent hp = player.hpComponent;
                PlayerEnergyGameResSystem energy = PlayerEnergyGameResSystem.GetSystem();
                // Unendlich: bis zum Maximum auffüllen, AddHp/Add begrenzen selbst. Danach verhindern die Patches jeden Verlust.
                float healthAmount = Plugin.InfiniteHealth.Value && hp != null ? hp.MaxHpValue : Plugin.HealthPerSecond.Value * seconds;
                float energyAmount = Plugin.InfiniteEnergy.Value && energy != null ? energy.Max : Plugin.EnergyPerSecond.Value * seconds;

                RegenerateHealth(hp, healthAmount);
                RegenerateRes(energy, energyAmount);
                ReduceInsanity(player, PlayerInsanityGameResSystem.GetSystem(), Plugin.InsanityReductionPerSecond.Value * seconds);
            }
            catch (Exception e)
            {
                if (errorLogged) return;
                errorLogged = true;
                Plugin.LogWarning("Regeneration failed (further errors will not be logged): " + e);
            }
        }

        private static void RegenerateHealth(HPComponent hp, float amount)
        {
            // Hp 0 = tot bzw. Todesanimation läuft – dort nicht eingreifen.
            if (amount <= 0f || hp == null || hp.Hp <= 0 || hp.HasFullHp)
            {
                healthRemainder = 0f;
                return;
            }

            // Leben sind ganze Zahlen, Nachkommaanteile sammeln wir für den nächsten Tick.
            healthRemainder += amount;
            int whole = (int)healthRemainder;
            if (whole <= 0) return;
            healthRemainder -= whole;
            hp.AddHp(whole);
        }

        private static void RegenerateRes(GK2GameResSystem system, float amount)
        {
            // HasMax() heißt "steht bereits auf dem Maximum"; Add() begrenzt selbst auf Min/Max.
            if (amount <= 0f || system == null || system.HasMax()) return;
            system.Add(amount);
        }

        private static void ReduceInsanity(PlayerData player, PlayerInsanityGameResSystem system, float amount)
        {
            // Wahnsinn ist schlecht: er senkt die maximale Energie (Maximum = 100 - Wahnsinn).
            if (amount <= 0f || system == null) return;
            float current = player.GetRes("insanity");
            if (current <= 0f) return;
            // Set statt Add(-amount): Add würde bei aktivem Schlafmangel die Hälfte gleich wieder draufschlagen.
            // Set begrenzt selbst auf 0 bis Max.
            system.Set(current - amount);
        }
    }
}
