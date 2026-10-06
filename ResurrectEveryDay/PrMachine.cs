namespace ResurrectEveryDay
{
    /// <summary>
    /// Die PR-Maschine (WGO "panic_reduction_machine_1" bis "_5") hebt das Zombie-Limit an: Ihre Rezepte
    /// "reduce_panic_..." erhöhen beim Start die Qualität der Zone "resurrection" und nehmen sie am Ende wieder weg.
    /// Starten lässt sie sich nur an Gluttony: PanicReductionMachineInteractionHandler.Interact vergleicht den
    /// Wochentag mit ConstDef "day_gluttony". Das Ende hängt nicht am Starttag. Das Zeit-Event
    /// "finish_panic_reduction_machine_craft" zählt am Ende jedes Wrath-Tages "panic_reduction_duration" herunter
    /// und schließt den Craft bei 0 ab. Wir beantworten nur die Tagesabfrage anders.
    /// </summary>
    internal static class PrMachineGate
    {
        internal const string DayKey = "day_gluttony";

        /// <summary>Ersatz für ConstDef.IntValue beim Vergleich mit dem heutigen Wochentag.</summary>
        internal static int GetAllowedDay(ConstDef day)
        {
            if (Plugin.PrMachineAnyDayActive) return MainGame.Instance.GameSave.environmentData.CurrentDayNumber;
            return day.IntValue;
        }
    }
}
