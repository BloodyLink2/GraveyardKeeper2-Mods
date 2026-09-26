using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using GK2.Framework;

namespace ZombieProductivity
{
    /// <summary>
    /// Alles, was Typen aus GK2.Framework.dll benutzt, steckt hier bzw. in <see cref="ZombieProductivityMod"/>.
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

            var mod = new ZombieProductivityMod();
            ZombieProductivityMod.Registered = FrameworkApi.RegisterMod(mod, plugin.Config);
            return ZombieProductivityMod.Registered != null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SyncEnabled(bool enabled)
        {
            RegisteredMod registered = ZombieProductivityMod.Registered;
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

    internal sealed class ZombieProductivityMod : Gk2ModBase
    {
        internal static RegisteredMod Registered;

        private static readonly Gk2ModMetadata metadata = new Gk2ModMetadata(
            Plugin.ModId,
            Plugin.Name,
            "BloodyLink2",
            Plugin.Version,
            Translations.Get("mod.description",
                "Makes zombies work faster at workbenches and in the garden (up to 10x) and optionally gives them " +
                "extra mastery."),
            supportsRuntimeToggle: true,
            requiresKnownBuild: false);

        public override Gk2ModMetadata Metadata => metadata;

        public override void OnRegister(Gk2ModContext context)
        {
            // Bind() liefert die bereits im Plugin angelegten Einträge zurück –
            // F1-Menü und Framework-Menü teilen sich also exakt dieselben Werte.
            context.Settings.AddFloatSlider("Zombies", "SpeedMultiplier", 1.0f,
                Plugin.SpeedMin, Plugin.SpeedMax,
                Translations.Get("SpeedMultiplier.name", "Work speed"),
                Translations.Get("SpeedMultiplier.hint",
                    "How fast zombies work at workbenches and in the garden (1.0 = normal, max. 10.0)."),
                step: Plugin.SpeedStep, order: 0);

            context.Settings.AddIntSlider("Zombies", "MasteryBonus", 0,
                Plugin.MasteryMin, Plugin.MasteryMax,
                Translations.Get("MasteryBonus.name", "Mastery bonus (levels)"),
                Translations.Get("MasteryBonus.hint",
                    "Extra mastery levels for working zombies (0 = normal). Max. 3 cells per hit."),
                step: 1, order: 1);
        }

        public override void OnEnable()
        {
            Plugin.FrameworkEnabled = true;
            Plugin.Log(Plugin.Name + " enabled.");
        }

        public override void OnDisable()
        {
            Plugin.FrameworkEnabled = false;
            Plugin.Log(Plugin.Name + " disabled. Zombies work at normal speed again.");
        }
    }
}
