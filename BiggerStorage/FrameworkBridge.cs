using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using GK2.Framework;

namespace BiggerStorage
{
    /// <summary>
    /// Alles, was Typen aus GK2.Framework.dll benutzt, steckt hier bzw. in <see cref="BiggerStorageMod"/>.
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

            var mod = new BiggerStorageMod();
            BiggerStorageMod.Registered = FrameworkApi.RegisterMod(mod, plugin.Config);
            return BiggerStorageMod.Registered != null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SyncEnabled(bool enabled)
        {
            RegisteredMod registered = BiggerStorageMod.Registered;
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

    internal sealed class BiggerStorageMod : Gk2ModBase
    {
        internal static RegisteredMod Registered;

        private static readonly Gk2ModMetadata metadata = new Gk2ModMetadata(
            Plugin.ModId,
            Plugin.Name,
            "BloodyLink2",
            Plugin.Version,
            Translations.Get("mod.description",
                "More slots for every chest and, if you like, bigger stacks in all inventories."),
            supportsRuntimeToggle: true,
            requiresKnownBuild: false);

        public override Gk2ModMetadata Metadata => metadata;

        public override void OnRegister(Gk2ModContext context)
        {
            // Bind() liefert die bereits im Plugin angelegten Einträge zurück –
            // F1-Menü und Framework-Menü teilen sich also exakt dieselben Werte.
            context.Settings.AddIntSlider("Chests", "ExtraSlots", 10, 0, Plugin.ExtraSlotsMax,
                Translations.Get("ExtraSlots.name", "Extra chest slots"),
                Translations.Get("ExtraSlots.hint", "Slots every chest gets on top of its normal size (0 = normal)."),
                step: 1, order: 0);

            context.Settings.AddIntSlider("Stacks", "StackSize", 0, 0, Plugin.StackSizeMax,
                Translations.Get("StackSize.name", "Stack size"),
                Translations.Get("StackSize.hint", "Items per slot in all inventories (0 = normal, up to 300)."),
                step: Plugin.StackSizeStep, order: 1);

            context.Settings.AddToggle("Uninstall", "PrepareUninstall", false,
                Translations.Get("PrepareUninstall.name", "Prepare for uninstall"),
                Translations.Get("PrepareUninstall.hint",
                    "Resets all chests and stacks to normal so you can remove the mod safely."),
                order: 2);
        }

        public override void OnEnable()
        {
            Plugin.FrameworkEnabled = true;
            Plugin.ApplyEverything();
            Plugin.Log(Plugin.Name + " enabled.");
        }

        public override void OnDisable()
        {
            Plugin.FrameworkEnabled = false;
            Plugin.ApplyEverything();
            Plugin.Log(Plugin.Name + " disabled. Chests and stacks are back to normal.");
        }
    }
}
