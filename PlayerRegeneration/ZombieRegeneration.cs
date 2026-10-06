using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerRegeneration
{
    /// <summary>
    /// "Zombie health in fights": Im laufenden Kampf heilen wir alle eigenen Zombies (Team Player, Typ Zombie; Wgo leitet
    /// beides aus der Id ab, "zmb" + "allie") um einen Anteil ihrer maximalen Lebenspunkte. Die Kämpfer führt
    /// FightingTargetsDatabase; HPComponent.AddHp begrenzt selbst aufs Maximum und wirkt nicht mehr, sobald ein Zombie
    /// fällt (isDeathDelayed). Angewendet einmal pro Sekunde, Nachkommaanteile je Zombie gesammelt.
    /// </summary>
    internal static class ZombieRegeneration
    {
        private const float TickInterval = 1f;
        // Nach Ladepausen o. Ä. nicht auf einen Schlag nachholen.
        private const float MaxTickSeconds = 2f;

        private static readonly Dictionary<SGuid, float> remainders = new Dictionary<SGuid, float>();
        private static readonly List<ICombatEntity> fighters = new List<ICombatEntity>();

        private static float elapsed;
        private static bool errorLogged;

        internal static void Update(FightingGameController fight)
        {
            if (!Plugin.IsActive || MainGame.IsGamePaused) return;

            float percent = Plugin.ZombieHealthPercent.Value;
            if (percent <= 0f || fight == null || fight.CurrentFightState != FightState.ActiveFight)
            {
                elapsed = 0f;
                remainders.Clear();
                return;
            }

            elapsed += Time.deltaTime;
            if (elapsed < TickInterval) return;
            float seconds = Mathf.Min(elapsed, MaxTickSeconds);
            elapsed = 0f;

            try
            {
                // Kopie: Die Datenbank ändert sich, wenn Kämpfer fallen oder dazukommen.
                fighters.Clear();
                fighters.AddRange(fight.TargetsDatabase.GetTargetsByTeam(LazyConsts.Fighting.TeamType.Player));
                foreach (ICombatEntity fighter in fighters)
                {
                    if (fighter == null || fighter.EntityType != LazyConsts.Fighting.EntityType.Zombie) continue;
                    Heal(fighter, percent / 100f * seconds);
                }
            }
            catch (Exception e)
            {
                if (errorLogged) return;
                errorLogged = true;
                Plugin.LogWarning("Zombie regeneration failed (further errors will not be logged): " + e);
            }
        }

        private static void Heal(ICombatEntity zombie, float shareOfMax)
        {
            HPComponent hp = zombie.CombatEntityHpComponent;
            if (hp == null || hp.Hp <= 0 || hp.MaxHpValue <= 0 || hp.HasFullHp)
            {
                remainders.Remove(zombie.CombatEntityUID);
                return;
            }

            remainders.TryGetValue(zombie.CombatEntityUID, out float rest);
            rest += hp.MaxHpValue * shareOfMax;
            int whole = (int)rest;
            if (whole > 0) hp.AddHp(whole);
            remainders[zombie.CombatEntityUID] = rest - whole;
        }
    }
}
