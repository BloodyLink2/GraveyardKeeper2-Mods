using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using GK2.Framework;

namespace PlayerRegeneration
{
    /// <summary>
    /// Alles, was Typen aus GK2.Framework.dll benutzt, steckt hier bzw. in <see cref="PlayerRegenerationMod"/>.
    /// Diese Klasse hat bewusst keine Felder und keine Signaturen mit Framework-Typen, damit die Mod
    /// auch ohne installiertes Framework sauber lädt (optionale Abhängigkeit).
    /// </summary>
    internal static class FrameworkBridge
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static bool TryRegister(Plugin plugin)
        {
            if (!FrameworkApi.IsReady)
            {
                Plugin.Log("GK2 Mod Framework is installed but not ready yet. Running without the framework.");
                return false;
            }

            var mod = new PlayerRegenerationMod();
            PlayerRegenerationMod.Registered = FrameworkApi.RegisterMod(mod, plugin.Config);
            return PlayerRegenerationMod.Registered != null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SyncEnabled(bool enabled)
        {
            RegisteredMod registered = PlayerRegenerationMod.Registered;
            if (registered == null || registered.IsEnabled == enabled) return;
            FrameworkApi.SetRuntimeEnabled(Plugin.ModId, enabled);
        }

        /// <summary>
        /// FrameworkLocalization.Get(modId, key, englishFallback) gibt es erst ab Framework 0.1.8 – deshalb per Reflection,
        /// damit die Mod mit älteren Versionen weiter lädt. null = keine Übersetzungen.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Func<string, string, string, string> GetTranslationLookup()
        {
            MethodInfo get = typeof(FrameworkApi).Assembly.GetType("GK2.Framework.FrameworkLocalization")?
                .GetMethod("Get", new[] { typeof(string), typeof(string), typeof(string) });
            return get == null
                ? null
                : (Func<string, string, string, string>)Delegate.CreateDelegate(typeof(Func<string, string, string, string>), get);
        }
    }

    internal sealed class PlayerRegenerationMod : Gk2ModBase
    {
        internal static RegisteredMod Registered;

        private static readonly Gk2ModMetadata metadata = new Gk2ModMetadata(
            Plugin.ModId,
            Plugin.Name,
            "BloodyLink2",
            Plugin.Version,
            Translations.Get("mod.description",
                "Health and energy regenerate (or stay full), insanity fades and sleep deprivation builds up slower."),
            supportsRuntimeToggle: true,
            requiresKnownBuild: false);

        public override Gk2ModMetadata Metadata => metadata;

        public override void OnRegister(Gk2ModContext context)
        {
            // Bind() liefert die bereits im Plugin angelegten Einträge zurück –
            // F1-Menü und Framework-Menü teilen sich also exakt dieselben Werte.
            context.Settings.AddFloatSlider("Regeneration", "HealthPerSecond", 0.5f, 0f, Plugin.HealthRateMax,
                Translations.Get("HealthPerSecond.name", "Health per second"),
                Translations.Get("HealthPerSecond.hint", "Health points regained per second (0 = off)."),
                step: Plugin.Step, order: 0);

            context.Settings.AddToggle("Infinite", "InfiniteHealth", false,
                Translations.Get("InfiniteHealth.name", "Infinite health"),
                Translations.Get("InfiniteHealth.hint", "No damage, health stays full."),
                order: 1);

            context.Settings.AddFloatSlider("Regeneration", "EnergyPerSecond", 0.2f, 0f, Plugin.EnergyRateMax,
                Translations.Get("EnergyPerSecond.name", "Energy per second"),
                Translations.Get("EnergyPerSecond.hint",
                    "Energy regained per second (0 = off, the bar goes up to 100)."),
                step: Plugin.Step, order: 2);

            context.Settings.AddToggle("Infinite", "InfiniteEnergy", false,
                Translations.Get("InfiniteEnergy.name", "Infinite energy"),
                Translations.Get("InfiniteEnergy.hint", "Work costs no energy, the bar stays full."),
                order: 3);

            context.Settings.AddFloatSlider("Regeneration", "InsanityReductionPerSecond", 0.1f, 0f, Plugin.InsanityRateMax,
                Translations.Get("InsanityReductionPerSecond.name", "Insanity reduction per second"),
                Translations.Get("InsanityReductionPerSecond.hint",
                    "Insanity fading per second (0 = off). Less insanity = more maximum energy."),
                step: Plugin.Step, order: 4);

            context.Settings.AddFloatSlider("Sleep", "SleepDeprivationSpeed", 0.5f, 0f, 1f,
                Translations.Get("SleepDeprivationSpeed.name", "Sleep deprivation speed"),
                Translations.Get("SleepDeprivationSpeed.hint",
                    "1.0 = vanilla (after 2 days), 0.5 = after 4 days, 0 = never."),
                step: Plugin.Step, order: 5);

            context.Settings.AddToggle("Sleep", "SleepWithFullEnergy", true,
                Translations.Get("SleepWithFullEnergy.name", "Sleep with full energy"),
                Translations.Get("SleepWithFullEnergy.hint",
                    "The bed no longer refuses when your energy is full. Important for saving."),
                order: 6);

            context.Settings.AddFloatSlider("Work", "WorkSpeed", 1f, 1f, Plugin.WorkSpeedMax,
                Translations.Get("WorkSpeed.name", "Your work speed at tables"),
                Translations.Get("WorkSpeed.hint", "More progress per hit when you work at a table yourself (1.0 = normal)."),
                step: Plugin.Step, order: 7);
        }

        public override void OnEnable()
        {
            Plugin.FrameworkEnabled = true;
            Plugin.Log(Plugin.Name + " enabled.");
        }

        public override void OnDisable()
        {
            Plugin.FrameworkEnabled = false;
            Plugin.Log(Plugin.Name + " disabled.");
        }
    }
}
