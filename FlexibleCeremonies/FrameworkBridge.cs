using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using GK2.Framework;

namespace FlexibleCeremonies
{
    /// <summary>
    /// Alles, was Typen aus GK2.Framework.dll benutzt, steckt hier bzw. in <see cref="FlexibleCeremoniesMod"/>.
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

            var mod = new FlexibleCeremoniesMod();
            FlexibleCeremoniesMod.Registered = FrameworkApi.RegisterMod(mod, plugin.Config);
            return FlexibleCeremoniesMod.Registered != null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SyncEnabled(bool enabled)
        {
            RegisteredMod registered = FlexibleCeremoniesMod.Registered;
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

    internal sealed class FlexibleCeremoniesMod : Gk2ModBase
    {
        internal static RegisteredMod Registered;

        private static readonly Gk2ModMetadata metadata = new Gk2ModMetadata(
            Plugin.ModId,
            Plugin.Name,
            "BloodyLink2",
            Plugin.Version,
            Translations.Get("mod.description",
                "Hold sermons on any day and as often as you like (adjustable), optionally without Gratitude."),
            supportsRuntimeToggle: true,
            requiresKnownBuild: false);

        public override Gk2ModMetadata Metadata => metadata;

        public override void OnRegister(Gk2ModContext context)
        {
            // Bind() liefert die bereits im Plugin angelegten Einträge zurück –
            // F1-Menü und Framework-Menü teilen sich also exakt dieselben Werte.
            context.Settings.AddToggle("Ceremonies", "AnyDay", true,
                Translations.Get("AnyDay.name", "Sermons on any day"),
                Translations.Get("AnyDay.hint", "Not just on the Day of Wrath."),
                order: 0);

            context.Settings.AddIntSlider("Ceremonies", "CeremoniesPerDay", 0, 0, Plugin.CeremoniesPerDayMax,
                Translations.Get("CeremoniesPerDay.name", "Sermons per day"),
                Translations.Get("CeremoniesPerDay.hint",
                    "0 = unlimited. Gratitude is still consumed unless ignored."),
                step: 1, order: 1);

            context.Settings.AddToggle("Ceremonies", "IgnoreGratitude", false,
                Translations.Get("IgnoreGratitude.name", "Ignore Gratitude"),
                Translations.Get("IgnoreGratitude.hint", "Parishioners = church quality, no Gratitude is consumed."),
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
