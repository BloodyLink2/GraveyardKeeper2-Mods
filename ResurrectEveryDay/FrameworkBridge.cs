using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using GK2.Framework;

namespace ResurrectEveryDay
{
    /// <summary>
    /// Alles, was Typen aus GK2.Framework.dll benutzt, steckt hier bzw. in <see cref="ResurrectEveryDayMod"/>.
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

            var mod = new ResurrectEveryDayMod();
            ResurrectEveryDayMod.Registered = FrameworkApi.RegisterMod(mod, plugin.Config);
            return ResurrectEveryDayMod.Registered != null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SyncEnabled(bool enabled)
        {
            RegisteredMod registered = ResurrectEveryDayMod.Registered;
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

    internal sealed class ResurrectEveryDayMod : Gk2ModBase
    {
        internal static RegisteredMod Registered;

        private static readonly Gk2ModMetadata metadata = new Gk2ModMetadata(
            Plugin.ModId,
            Plugin.Name,
            "BloodyLink2",
            Plugin.Version,
            Translations.Get("mod.description",
                "Resurrect zombies on any day and as often as you like, not just once on the Day of Envy. " +
                "The PR Machine works on any day too."),
            supportsRuntimeToggle: true,
            requiresKnownBuild: false);

        public override Gk2ModMetadata Metadata => metadata;

        public override void OnRegister(Gk2ModContext context)
        {
            // Bind() liefert die bereits im Plugin angelegten Einträge zurück –
            // F1-Menü und Framework-Menü teilen sich also exakt dieselben Werte.
            context.Settings.AddToggle("Resurrection", "AnyDay", true,
                Translations.Get("AnyDay.name", "Resurrect on any day"),
                Translations.Get("AnyDay.hint", "Not just on the Day of Envy."),
                order: 0);

            context.Settings.AddToggle("Resurrection", "UnlimitedPower", true,
                Translations.Get("UnlimitedPower.name", "Unlimited storm power"),
                Translations.Get("UnlimitedPower.hint", "The lab always has power. Off = recharges once a week."),
                order: 1);

            context.Settings.AddToggle("PR Machine", "PrMachineAnyDay", true,
                Translations.Get("PrMachineAnyDay.name", "PR Machine on any day"),
                Translations.Get("PrMachineAnyDay.hint", "Not just on the Day of Gluttony. A run still ends on a Day of Wrath."),
                order: 2);
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
