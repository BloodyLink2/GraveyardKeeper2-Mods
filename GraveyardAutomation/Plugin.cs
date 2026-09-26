using System;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace GraveyardAutomation
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(FrameworkGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "de.gk2mods.graveyardautomation";
        public const string Name = "Graveyard Automation";
        public const string Version = "1.7.0";

        internal const string FrameworkGuid = "ru.superman4eg.gk2.framework";
        internal const string ModId = "graveyardautomation";

        internal const string Category = "Graveyard Automation";
        internal const float RadiusStep = 0.5f;
        internal const float RadiusMax = 20f;
        internal const float SpeedStep = 0.5f;
        internal const float SpeedMax = 10f;

        internal static Plugin Instance;

        // Die Framework-Mod liest/schreibt denselben Eintrag ("Framework", "Enabled").
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<bool> ZombieWorkbenches;
        internal static ConfigEntry<bool> ZombieIngredients;
        internal static ConfigEntry<bool> AutoCrafters;
        internal static ConfigEntry<bool> CollectResearchPoints;
        internal static ConfigEntry<bool> EmptyWells;
        internal static ConfigEntry<bool> HarvestBeehives;
        internal static ConfigEntry<bool> AutoRefuel;
        internal static ConfigEntry<bool> RepeatRecipes;
        internal static ConfigEntry<bool> PickRecipes;
        internal static ConfigEntry<bool> ZombieAutoRecipes;
        internal static ConfigEntry<float> StationSpeed;
        internal static ConfigEntry<float> ChestRadius;
        internal static ConfigEntry<bool> DropIfNoChest;

        internal static bool FrameworkMode;
        internal static bool FrameworkEnabled;

        /// <summary>Wirkt die Mod gerade? Mit Framework entscheidet dessen An/Aus-Zustand.</summary>
        internal static bool IsActive => FrameworkMode ? FrameworkEnabled : Enabled.Value;

        private void Awake()
        {
            Instance = this;

            Enabled = Config.Bind("Framework", "Enabled", true, Describe(
                Translations.Get("Enabled.name", "Mod enabled"),
                Translations.Get("Enabled.description", "Turns the mod on or off (takes effect immediately)."),
                13));

            ZombieWorkbenches = Config.Bind("Delivery", "ZombieWorkbenches", true, Describe(
                Translations.Get("ZombieWorkbenches.name", "Zombie workbenches"),
                Translations.Get("ZombieWorkbenches.description",
                    "Items finished by zombie workers go straight into a chest next to the workbench instead of " +
                    "waiting for you to pick them up, so the zombie keeps working right away."),
                12));

            ZombieIngredients = Config.Bind("Delivery", "ZombieIngredients", true, Describe(
                Translations.Get("ZombieIngredients.name", "Zombie ingredients from chests"),
                Translations.Get("ZombieIngredients.description",
                    "Zombie workers take the ingredients they need straight from the storages within the search " +
                    "radius, so no other zombie has to bring them. Ingredients another zombie is already bringing " +
                    "are left to it. If there is not enough nearby, the zombie waits as usual."),
                11));

            AutoCrafters = Config.Bind("Delivery", "AutoCrafters", true, Describe(
                Translations.Get("AutoCrafters.name", "Auto-crafting stations"),
                Translations.Get("AutoCrafters.description",
                    "Stations that craft on their own (e.g. ovens) put their finished items into a chest next to " +
                    "them instead of waiting for you to collect them, and keep running."),
                10));

            CollectResearchPoints = Config.Bind("Delivery", "CollectResearchPoints", true, Describe(
                Translations.Get("CollectResearchPoints.name", "Collect research points"),
                Translations.Get("CollectResearchPoints.description",
                    "Research points (the orbs) from stations the mod collects for you fly straight to you, like " +
                    "when you sleep, wherever you are. Off = they drop on the ground next to the station."),
                9));

            EmptyWells = Config.Bind("Delivery", "EmptyWells", true, Describe(
                Translations.Get("EmptyWells.name", "Empty wells into chests"),
                Translations.Get("EmptyWells.description",
                    "Upgraded wells fill up with water and stop when they are full. The water is moved into a chest " +
                    "within the search radius every few seconds, so the well keeps producing."),
                8));

            HarvestBeehives = Config.Bind("Delivery", "HarvestBeehives", true, Describe(
                Translations.Get("HarvestBeehives.name", "Harvest beehives"),
                Translations.Get("HarvestBeehives.description",
                    "Full beehives are harvested automatically, as if you did it yourself: the honey and beeswax go " +
                    "into a chest within the search radius, and the hive starts filling up again. Wild honey nests " +
                    "are left alone."),
                7));

            AutoRefuel = Config.Bind("Delivery", "AutoRefuel", true, Describe(
                Translations.Get("AutoRefuel.name", "Refuel from chests"),
                Translations.Get("AutoRefuel.description",
                    "When a station waits for fuel and a fuel storage in the same area can make that fuel from " +
                    "materials in your chests, the fuel storage is refilled automatically (only as much as the " +
                    "station needs)."),
                6));

            RepeatRecipes = Config.Bind("Queue", "RepeatRecipes", true, Describe(
                Translations.Get("RepeatRecipes.name", "Repeat recipes (999x)"),
                Translations.Get("RepeatRecipes.description",
                    "When you add a recipe with count 1 to the queue of an oven or a zombie workbench, it is set to " +
                    "999 so the station keeps producing. Pick a different count in the craft window to keep it, or " +
                    "lower it with \"-\" in the queue (hold to count fast)."),
                5));

            PickRecipes = Config.Bind("Queue", "AutoRecipes", false, Describe(
                Translations.Get("AutoRecipes.name", "Pick recipes from nearby chests"),
                Translations.Get("AutoRecipes.description",
                    "Stardew style: an idle oven (or other station that works on its own) looks into the chests " +
                    "within the search radius and crafts one of its recipes once, if all its ingredients are there " +
                    "(fuel may come from the fuel storage). Then it checks again, so it keeps running and switches " +
                    "between recipes: first recipes no other station in the area is making right now, then the one " +
                    "whose result you have the least of. It takes the ingredients from the nearby chests first."),
                4));

            ZombieAutoRecipes = Config.Bind("Queue", "ZombieAutoRecipes", false, Describe(
                Translations.Get("ZombieAutoRecipes.name", "Pick recipes for zombie workbenches"),
                Translations.Get("ZombieAutoRecipes.description",
                    "Like \"Pick recipes from nearby chests\", but for workbenches with a zombie worker: when the " +
                    "zombie has nothing to do, it picks a recipe it can make (tool and mastery) whose ingredients " +
                    "are all in the storages within the search radius, crafts it once and then picks again. Recipes " +
                    "no other station in the area is making come first, then the one whose result you have the least " +
                    "of. Works best together with \"Zombie ingredients from chests\"."),
                3));

            StationSpeed = Config.Bind("Speed", "StationSpeed", 1f, Describe(
                Translations.Get("StationSpeed.name", "Station speed"),
                Translations.Get("StationSpeed.description",
                    "How fast stations that craft on their own work, such as ovens and distillers (1.0 = normal, up " +
                    "to 10x). Garden beds, town building construction and stations with a zombie worker are not " +
                    "affected, and neither are tables you work at yourself (workbenches, autopsy and embalming " +
                    "table). For those, see Tireless Keeper."),
                2, new AcceptableValueRange<float>(1f, SpeedMax)));
            SnapToStep(StationSpeed, SpeedStep);
            StationSpeed.SettingChanged += (sender, e) => SnapToStep(StationSpeed, SpeedStep);

            ChestRadius = Config.Bind("Delivery", "ChestRadius", 3f, Describe(
                Translations.Get("ChestRadius.name", "Chest search radius"),
                Translations.Get("ChestRadius.description",
                    "How far a chest may be from the station (center to center, roughly 1 per tile). 0 = any chest " +
                    "in the same area. Chests that already hold the item are preferred."),
                1, new AcceptableValueRange<float>(0f, RadiusMax)));
            SnapToStep(ChestRadius, RadiusStep);
            ChestRadius.SettingChanged += (sender, e) => SnapToStep(ChestRadius, RadiusStep);

            DropIfNoChest = Config.Bind("Delivery", "DropIfNoChest", false, Describe(
                Translations.Get("DropIfNoChest.name", "Drop on the ground if no chest"),
                Translations.Get("DropIfNoChest.description",
                    "If there is no chest with room within the search radius, finished items drop on the ground next " +
                    "to the station instead, so ovens and zombies keep working. Off = they wait for you as usual."),
                0));

            var harmony = new Harmony(Guid);
            harmony.CreateClassProcessor(typeof(CraftSystemUpdatePatch)).Patch();
            harmony.CreateClassProcessor(typeof(MakeDropPatch)).Patch();
            harmony.CreateClassProcessor(typeof(DropTechPointsPatch)).Patch();
            harmony.CreateClassProcessor(typeof(RepeatRecipesPatch)).Patch();
            harmony.CreateClassProcessor(typeof(InputChestsFirstPatch)).Patch();
            harmony.CreateClassProcessor(typeof(StationSpeedPatch)).Patch();

            // Schalter je Stationsart: die Arten stehen erst mit geladenem Spielstand fest.
            MainGame.OnGameStarted += StationSwitches.DiscoverAll;

            if (Chainloader.PluginInfos.ContainsKey(FrameworkGuid))
            {
                try
                {
                    FrameworkMode = FrameworkBridge.TryRegister(this);
                }
                catch (Exception e)
                {
                    FrameworkMode = false;
                    Logger.LogWarning("GK2 Mod Framework found, but registration failed. Continuing without the framework. " + e);
                }
            }

            Enabled.SettingChanged += OnEnabledChanged;

            Logger.LogInfo($"{Name} {Version} loaded (framework: {(FrameworkMode ? "yes" : "no")}, " +
                           $"zombie workbenches: {(ZombieWorkbenches.Value ? "on" : "off")}, " +
                           $"zombie ingredients: {(ZombieIngredients.Value ? "on" : "off")}, " +
                           $"auto-crafting stations: {(AutoCrafters.Value ? "on" : "off")}, " +
                           $"collect research points: {(CollectResearchPoints.Value ? "on" : "off")}, " +
                           $"empty wells: {(EmptyWells.Value ? "on" : "off")}, " +
                           $"harvest beehives: {(HarvestBeehives.Value ? "on" : "off")}, " +
                           $"refuel: {(AutoRefuel.Value ? "on" : "off")}, " +
                           $"repeat recipes: {(RepeatRecipes.Value ? "on" : "off")}, " +
                           $"pick recipes: {(PickRecipes.Value ? "on" : "off")}, " +
                           $"zombie recipes: {(ZombieAutoRecipes.Value ? "on" : "off")}, " +
                           $"station speed: {StationSpeed.Value:0.0}x, " +
                           $"radius: {(ChestRadius.Value <= 0f ? "whole area" : ChestRadius.Value.ToString("0.0"))}, " +
                           $"drop if no chest: {(DropIfNoChest.Value ? "on" : "off")}).");
        }

        /// <summary>Rastet Regler auf ihre Schritte ein – egal ob per F1, Framework-Menü oder Config-Datei.</summary>
        private static void SnapToStep(ConfigEntry<float> entry, float step)
        {
            float snapped = Mathf.Round(entry.Value / step) * step;
            if (Mathf.Abs(snapped - entry.Value) > 0.0001f) entry.Value = snapped;
        }

        private void OnEnabledChanged(object sender, EventArgs e)
        {
            // Umschalten per F1 (ConfigurationManager) an das Framework weiterreichen,
            // damit dessen Menü und der tatsächliche Zustand übereinstimmen.
            if (!FrameworkMode) return;
            try
            {
                FrameworkBridge.SyncEnabled(Enabled.Value);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Could not pass the on/off state to the framework: " + ex.Message);
            }
        }

        /// <summary>Eintrag fürs F1-Menü und die Config-Datei; Name und Beschreibung kommen schon übersetzt.</summary>
        private static ConfigDescription Describe(string name, string description, int order, AcceptableValueBase range = null) =>
            new ConfigDescription(description, range,
                new ConfigurationManagerAttributes { Category = Category, DispName = name, Order = order });

        internal static void Log(string message) => Instance?.Logger.LogInfo(message);

        internal static void LogWarning(string message) => Instance?.Logger.LogWarning(message);
    }
}
