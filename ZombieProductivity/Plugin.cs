using System;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace ZombieProductivity
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(FrameworkGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "de.gk2mods.zombieproductivity";
        public const string Name = "Zombie Productivity";
        public const string Version = "1.1.0";

        internal const string FrameworkGuid = "ru.superman4eg.gk2.framework";
        internal const string ModId = "zombieproductivity";

        internal const string Category = "Zombie Productivity";
        internal const float SpeedMin = 0.1f;
        internal const float SpeedMax = 10.0f;
        internal const float SpeedStep = 0.1f;
        internal const int MasteryMin = 0;
        internal const int MasteryMax = 10;

        internal static Plugin Instance;

        // Die Framework-Mod liest/schreibt denselben Eintrag ("Framework", "Enabled").
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<float> SpeedMultiplier;
        internal static ConfigEntry<int> MasteryBonus;

        internal static bool FrameworkMode;
        internal static bool FrameworkEnabled;

        /// <summary>Wirkt die Mod gerade? Mit Framework entscheidet dessen An/Aus-Zustand.</summary>
        internal static bool IsActive => FrameworkMode ? FrameworkEnabled : Enabled.Value;

        private bool isRounding;

        private void Awake()
        {
            Instance = this;

            Enabled = Config.Bind("Framework", "Enabled", true, Describe(
                Translations.Get("Enabled.name", "Mod enabled"),
                Translations.Get("Enabled.description", "Turns the mod on or off (takes effect immediately)."),
                3));

            SpeedMultiplier = Config.Bind("Zombies", "SpeedMultiplier", 1.0f, Describe(
                Translations.Get("SpeedMultiplier.name", "Work speed"),
                Translations.Get("SpeedMultiplier.description",
                    "How fast zombies work at workbenches and in the garden (1.0 = normal, up to 10x)."),
                2, new AcceptableValueRange<float>(SpeedMin, SpeedMax)));

            MasteryBonus = Config.Bind("Zombies", "MasteryBonus", 0, Describe(
                Translations.Get("MasteryBonus.name", "Mastery bonus (levels)"),
                Translations.Get("MasteryBonus.description",
                    "Extra mastery levels for working zombies (0 = normal). This is not a multiplier. Higher mastery " +
                    "lets them make recipes above their level and gives more progress per hit (mastery ÷ recipe " +
                    "level, the game allows at most 3 cells per hit)."),
                1, new AcceptableValueRange<int>(MasteryMin, MasteryMax)));

            // Auf 0.1-Schritte einrasten – egal ob per F1, Framework-Menü oder Config-Datei.
            SpeedMultiplier.Value = RoundToStep(SpeedMultiplier.Value);
            SpeedMultiplier.SettingChanged += OnSpeedChanged;

            var harmony = new Harmony(Guid);
            harmony.CreateClassProcessor(typeof(ZombieCraftSpeedPatch)).Patch();
            harmony.CreateClassProcessor(typeof(ZombieHarvestSpeedPatch)).Patch();
            harmony.CreateClassProcessor(typeof(ZombieCraftExtraHitsPatch)).Patch();
            harmony.CreateClassProcessor(typeof(ZombieHarvestExtraHitsPatch)).Patch();
            harmony.CreateClassProcessor(typeof(ZombieMasteryPatch)).Patch();

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
                           $"work speed {SpeedMultiplier.Value:0.0}x, mastery bonus +{MasteryBonus.Value}).");
        }

        internal static float RoundToStep(float value)
        {
            float rounded = Mathf.Round(value / SpeedStep) * SpeedStep;
            return Mathf.Clamp((float)Math.Round(rounded, 1), SpeedMin, SpeedMax);
        }

        private void OnSpeedChanged(object sender, EventArgs e)
        {
            if (isRounding) return;
            float rounded = RoundToStep(SpeedMultiplier.Value);
            if (Mathf.Abs(rounded - SpeedMultiplier.Value) > 0.0001f)
            {
                isRounding = true;
                try { SpeedMultiplier.Value = rounded; }
                finally { isRounding = false; }
            }
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
