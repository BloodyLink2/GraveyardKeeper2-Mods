using System;

namespace FlexibleCeremonies
{
    /// <summary>
    /// Entscheidet anstelle des Spiels, ob am Gebetsstand gerade eine Zeremonie möglich ist.
    /// Das Spiel selbst setzt die Spieler-Ressource "sermon_ready" am Tag des Zorns um 0.01 auf 1 und um 0.99
    /// wieder auf 0; das Skript "System_Pray" setzt sie außerdem beim Start jeder Zeremonie auf 0
    /// (daher im Original nur eine pro Woche). Wir schreiben diesen Wert nie, sondern beantworten nur die eine
    /// Abfrage im Gebetsstand (siehe <see cref="PrayerStandPatch"/>) – der Spielstand bleibt unberührt.
    /// </summary>
    internal static class CeremonyGate
    {
        private const string CeremonyDayId = "day_wrath";

        private static bool errorLogged;

        /// <summary>Ersatz für PlayerData.GetRes("sermon_ready") in PrayerStandInteractionHandler.Interact.</summary>
        internal static float GetSermonReady(PlayerData player, string type, float defaultValue)
        {
            float vanilla = player.GetRes(type, defaultValue);
            if (!Plugin.IsActive) return vanilla;

            try
            {
                // Auch ohne "jeder Tag" gilt der Tag des Zorns als Zeremonientag – sonst würde ein Limit > 1
                // dort nichts bringen, weil "sermon_ready" nach der ersten Zeremonie auf 0 steht.
                // vanilla >= 1 deckt Story-Stellen ab, die eine Zeremonie außer der Reihe freischalten.
                bool dayAllowed = Plugin.AnyDay.Value || vanilla >= 1f || IsCeremonyDay();
                int limit = Plugin.CeremoniesPerDay.Value;
                bool countAllowed = limit <= 0 || CeremonyCounter.Today < limit;
                return dayAllowed && countAllowed ? 1f : 0f;
            }
            catch (Exception e)
            {
                if (!errorLogged)
                {
                    errorLogged = true;
                    Plugin.LogWarning("Sermon check failed, using the game's value (further errors will not be logged): " + e);
                }
                return vanilla;
            }
        }

        /// <summary>Ersatz für PlayerData.SubRes("happiness", Besucher) in UIPrayWindowData.StartCraft.</summary>
        internal static void SubGratitude(PlayerData player, string type, float value)
        {
            if (Plugin.IgnoreGratitudeActive) return;
            player.SubRes(type, value);
        }

        private static bool IsCeremonyDay()
        {
            return MainGame.Instance.GameSave.environmentData.CurrentDayNumber == ConstDef.Get(CeremonyDayId).IntValue;
        }
    }

    /// <summary>
    /// Zählt die heute begonnenen Zeremonien. Nur im Speicher: Nach dem Laden eines Spielstands beginnt die
    /// Zählung für den laufenden Tag von vorn.
    /// </summary>
    internal static class CeremonyCounter
    {
        private static GameSave save;
        private static int day = -1;
        private static int count;

        internal static int Today
        {
            get
            {
                Refresh();
                return count;
            }
        }

        internal static void Record()
        {
            Refresh();
            count++;
        }

        private static void Refresh()
        {
            GameSave currentSave = MainGame.Instance.GameSave;
            int currentDay = currentSave.environmentData.Day;
            if (currentSave == save && currentDay == day) return;
            save = currentSave;
            day = currentDay;
            count = 0;
        }
    }
}
