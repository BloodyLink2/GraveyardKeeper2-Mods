using System;

namespace BiggerStorage
{
    /// <summary>
    /// "Prepare for uninstall": Solange eingeschaltet, gelten Normalgrößen (siehe <see cref="Plugin.EffectiveExtraSlots"/>
    /// und <see cref="Plugin.EffectiveStackSize"/>). Truhen, die noch mehr Gegenstände enthalten als ihre Normalgröße,
    /// behalten die nötigen Plätze – deshalb prüfen wir alle paar Sekunden nach, damit sie schrumpfen, sobald der Spieler
    /// etwas herausnimmt, und melden per Bildschirmmeldung, wie viele noch zu voll sind (nur wenn sich die Zahl ändert).
    /// </summary>
    internal static class UninstallGuard
    {
        private const float CheckInterval = 3f;

        private static float timer;
        private static int lastReported = -1;

        internal static void Update(float deltaTime)
        {
            if (!Plugin.PrepareUninstall.Value || MainGame.Instance?.GameSave == null)
            {
                lastReported = -1;
                return;
            }
            timer += deltaTime;
            if (timer < CheckInterval) return;
            timer = 0f;
            Report(ChestSlots.ApplyAll());
        }

        /// <summary>Meldet, wie viele Truhen noch zu voll sind – nur wenn sich die Zahl geändert hat.</summary>
        internal static void Report(int tooFull)
        {
            if (!Plugin.PrepareUninstall.Value || tooFull == lastReported) return;
            lastReported = tooFull;

            string message = tooFull > 0
                ? string.Format(Translations.Get("uninstall.pending",
                    "Bigger Storage: {0} chest(s) still hold more items than their normal size. Take some out."), tooFull)
                : Translations.Get("uninstall.done",
                    "Bigger Storage: all chests are back to their normal size. Sleep once so the game saves, then you can " +
                    "uninstall.");
            Plugin.Log(message);

            // Bewusst nicht LazySingleton<UINotificator>.Instance: Das legt ein neues Objekt an, wenn es (noch) keins gibt.
            // ShowSimpleTextNotification erwartet einen Textschlüssel; unbekannte Schlüssel zeigt LLBase.L unverändert an.
            try
            {
                UINotificator notificator =
                    UnityEngine.Object.FindAnyObjectByType<UINotificator>(UnityEngine.FindObjectsInactive.Include);
                if (notificator != null) notificator.ShowSimpleTextNotification(message);
            }
            catch (Exception e)
            {
                Plugin.LogWarning("Could not show the message on screen: " + e.Message);
            }
        }
    }
}
