using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using GK2.Framework;

namespace GraveyardAutomation
{
    /// <summary>
    /// Alles, was Typen aus GK2.Framework.dll benutzt, steckt hier bzw. in <see cref="GraveyardAutomationMod"/>.
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

            var mod = new GraveyardAutomationMod();
            GraveyardAutomationMod.Registered = FrameworkApi.RegisterMod(mod, plugin.Config);
            return GraveyardAutomationMod.Registered != null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SyncEnabled(bool enabled)
        {
            RegisteredMod registered = GraveyardAutomationMod.Registered;
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

        /// <summary>
        /// Schalter einer Stationsart nachträglich ins Framework-Menü eintragen (<see cref="StationSwitches"/>). Die Seite
        /// baut sich beim Öffnen aus Gk2Settings.Items neu auf; der Config-Eintrag besteht schon, Bind liefert ihn zurück.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AddStationToggle(string id, string name, string hint)
        {
            GraveyardAutomationMod.Settings?.AddToggle(StationSwitches.Section, id, true, name, hint, order: 0);
        }
    }

    internal sealed class GraveyardAutomationMod : Gk2ModBase
    {
        internal static RegisteredMod Registered;
        internal static Gk2Settings Settings;

        private static readonly Gk2ModMetadata metadata = new Gk2ModMetadata(
            Plugin.ModId,
            Plugin.Name,
            "BloodyLink2",
            Plugin.Version,
            Translations.Get("mod.description",
                "Finished items go straight into a chest next to the workbench or station."),
            supportsRuntimeToggle: true,
            requiresKnownBuild: false);

        public override Gk2ModMetadata Metadata => metadata;

        public override void OnRegister(Gk2ModContext context)
        {
            Settings = context.Settings;

            // Bind() liefert die bereits im Plugin angelegten Einträge zurück –
            // F1-Menü und Framework-Menü teilen sich also exakt dieselben Werte.
            context.Settings.AddToggle("Delivery", "ZombieWorkbenches", true,
                Translations.Get("ZombieWorkbenches.name", "Zombie workbenches"),
                Translations.Get("ZombieWorkbenches.hint",
                    "Zombie output goes into a nearby chest, the zombie keeps working."),
                order: 0);

            context.Settings.AddToggle("Delivery", "ZombieIngredients", true,
                Translations.Get("ZombieIngredients.name", "Zombie ingredients from chests"),
                Translations.Get("ZombieIngredients.hint",
                    "Zombie workers take their ingredients from nearby storages, no other zombie has to bring them."),
                order: 1);

            context.Settings.AddToggle("Delivery", "ZombieIngredientsAllAreas", false,
                Translations.Get("ZombieIngredientsAllAreas.name", "Zombie ingredients from all areas"),
                Translations.Get("ZombieIngredientsAllAreas.hint",
                    "Not enough nearby: zombies also take ingredients from the chests in all other areas."),
                order: 2);

            context.Settings.AddToggle("Delivery", "AutoCrafters", true,
                Translations.Get("AutoCrafters.name", "Auto-crafting stations"),
                Translations.Get("AutoCrafters.hint",
                    "E.g. ovens: finished items go into a nearby chest, the station keeps running."),
                order: 3);

            context.Settings.AddToggle("Delivery", "CollectResearchPoints", true,
                Translations.Get("CollectResearchPoints.name", "Collect research points"),
                Translations.Get("CollectResearchPoints.hint",
                    "Research points from these stations fly straight to you instead of dropping."),
                order: 4);

            context.Settings.AddToggle("Delivery", "EmptyWells", true,
                Translations.Get("EmptyWells.name", "Empty wells into chests"),
                Translations.Get("EmptyWells.hint",
                    "Water from upgraded wells goes into a nearby chest, so the well never stops."),
                order: 5);

            context.Settings.AddToggle("Delivery", "HarvestBeehives", true,
                Translations.Get("HarvestBeehives.name", "Harvest beehives"),
                Translations.Get("HarvestBeehives.hint",
                    "Full beehives are harvested into a nearby chest and start filling up again."),
                order: 6);

            context.Settings.AddToggle("Delivery", "AutoRefuel", true,
                Translations.Get("AutoRefuel.name", "Refuel from chests"),
                Translations.Get("AutoRefuel.hint",
                    "Fuel storages make the fuel a waiting station needs from materials in your chests."),
                order: 7);

            context.Settings.AddToggle("Queue", "RepeatRecipes", true,
                Translations.Get("RepeatRecipes.name", "Repeat recipes (999x)"),
                Translations.Get("RepeatRecipes.hint",
                    "Recipes queued with count 1 at ovens and zombie workbenches become 999."),
                order: 8);

            context.Settings.AddToggle("Queue", "AutoRecipes", false,
                Translations.Get("AutoRecipes.name", "Pick recipes from nearby chests"),
                Translations.Get("AutoRecipes.hint",
                    "Idle ovens craft from the ingredients in a nearby chest, one at a time, what you lack most."),
                order: 9);

            context.Settings.AddToggle("Queue", "ZombieAutoRecipes", false,
                Translations.Get("ZombieAutoRecipes.name", "Pick recipes for zombie workbenches"),
                Translations.Get("ZombieAutoRecipes.hint",
                    "Idle zombie workbenches pick a recipe from the ingredients in nearby chests, one at a time."),
                order: 10);

            context.Settings.AddFloatSlider("Speed", "StationSpeed", 1f, 1f, Plugin.SpeedMax,
                Translations.Get("StationSpeed.name", "Station speed"),
                Translations.Get("StationSpeed.hint",
                    "How fast ovens, distillers and the like work (1.0 = normal). Not for tables you work at yourself."),
                step: Plugin.SpeedStep, order: 11);

            context.Settings.AddFloatSlider("Delivery", "ChestRadius", 3f, 0f, Plugin.RadiusMax,
                Translations.Get("ChestRadius.name", "Chest search radius"),
                Translations.Get("ChestRadius.hint", "Max. distance to the chest (about 1 per tile). 0 = whole area."),
                step: Plugin.RadiusStep, order: 12);

            context.Settings.AddToggle("Delivery", "DropIfNoChest", false,
                Translations.Get("DropIfNoChest.name", "Drop on the ground if no chest"),
                Translations.Get("DropIfNoChest.hint",
                    "No chest with room nearby: items drop next to the station, it keeps working."),
                order: 13);
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
