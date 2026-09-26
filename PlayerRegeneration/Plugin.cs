using System;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace PlayerRegeneration
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(FrameworkGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "de.gk2mods.playerregeneration";
        public const string Name = "Tireless Keeper";
        public const string Version = "1.2.0";

        internal const string FrameworkGuid = "ru.superman4eg.gk2.framework";
        internal const string ModId = "playerregeneration";

        internal const string Category = "Tireless Keeper";
        internal const float Step = 0.1f;
        internal const float HealthRateMax = 10f;
        internal const float EnergyRateMax = 5f;
        internal const float InsanityRateMax = 5f;
        internal const float WorkSpeedMax = 10f;

        internal static Plugin Instance;

        // Die Framework-Mod liest/schreibt denselben Eintrag ("Framework", "Enabled").
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<float> HealthPerSecond;
        internal static ConfigEntry<bool> InfiniteHealth;
        internal static ConfigEntry<float> EnergyPerSecond;
        internal static ConfigEntry<bool> InfiniteEnergy;
        internal static ConfigEntry<float> InsanityReductionPerSecond;
        internal static ConfigEntry<float> SleepDeprivationSpeed;
        internal static ConfigEntry<bool> SleepWithFullEnergy;
        internal static ConfigEntry<float> WorkSpeed;

        internal static bool FrameworkMode;
        internal static bool FrameworkEnabled;

        /// <summary>Wirkt die Mod gerade? Mit Framework entscheidet dessen An/Aus-Zustand.</summary>
        internal static bool IsActive => FrameworkMode ? FrameworkEnabled : Enabled.Value;

        internal static bool InfiniteHealthActive => IsActive && InfiniteHealth.Value;

        internal static bool InfiniteEnergyActive => IsActive && InfiniteEnergy.Value;

        internal static bool SleepWithFullEnergyActive => IsActive && SleepWithFullEnergy.Value;

        private void Awake()
        {
            Instance = this;

            Enabled = Config.Bind("Framework", "Enabled", true, Describe(
                Translations.Get("Enabled.name", "Mod enabled"),
                Translations.Get("Enabled.description", "Turns the mod on or off (takes effect immediately)."),
                8));

            HealthPerSecond = BindStepped("Regeneration", "HealthPerSecond", 0.5f, 0f, HealthRateMax,
                Translations.Get("HealthPerSecond.name", "Health per second"),
                Translations.Get("HealthPerSecond.description",
                    "Health points the player regains per second (0 = off). Also works during combat."), 7);

            InfiniteHealth = BindToggle("Infinite", "InfiniteHealth", false,
                Translations.Get("InfiniteHealth.name", "Infinite health"),
                Translations.Get("InfiniteHealth.description",
                    "The player no longer takes damage and always has full health. If a scene waits for the player " +
                    "to take damage or die, it cannot continue. Turn this off briefly then."), 6);

            EnergyPerSecond = BindStepped("Regeneration", "EnergyPerSecond", 0.2f, 0f, EnergyRateMax,
                Translations.Get("EnergyPerSecond.name", "Energy per second"),
                Translations.Get("EnergyPerSecond.description",
                    "Energy the player regains per second (0 = off). The bar goes up to 100."), 5);

            InfiniteEnergy = BindToggle("Infinite", "InfiniteEnergy", false,
                Translations.Get("InfiniteEnergy.name", "Infinite energy"),
                Translations.Get("InfiniteEnergy.description",
                    "Work no longer costs energy, the bar stays full and no task fails for lack of energy. This also " +
                    "means working while sleep-deprived no longer adds insanity."), 4);

            InsanityReductionPerSecond = BindStepped("Regeneration", "InsanityReductionPerSecond", 0.1f, 0f, InsanityRateMax,
                Translations.Get("InsanityReductionPerSecond.name", "Insanity reduction per second"),
                Translations.Get("InsanityReductionPerSecond.description",
                    "How much insanity fades per second (0 = off). Insanity lowers your maximum energy (maximum = " +
                    "100 - insanity) and rises with every task while sleep-deprived. If you need insanity for " +
                    "certain recipes, set this to 0."), 3);

            SleepDeprivationSpeed = BindStepped("Sleep", "SleepDeprivationSpeed", 0.5f, 0f, 1f,
                Translations.Get("SleepDeprivationSpeed.name", "Sleep deprivation speed"),
                Translations.Get("SleepDeprivationSpeed.description",
                    "How fast sleep deprivation builds up. 1.0 = vanilla (debuff after 2 days without sleep), 0.5 = " +
                    "after 4 days, 0 = never. A debuff that is already active only goes away with the next sleep."), 2);

            SleepWithFullEnergy = BindToggle("Sleep", "SleepWithFullEnergy", true,
                Translations.Get("SleepWithFullEnergy.name", "Sleep with full energy"),
                Translations.Get("SleepWithFullEnergy.description",
                    "The bed no longer refuses with \"no need to sleep\" when your energy bar is full. Important " +
                    "because the game only saves when you sleep. With full energy the sleep is just short."), 1);

            WorkSpeed = BindStepped("Work", "WorkSpeed", 1f, 1f, WorkSpeedMax,
                Translations.Get("WorkSpeed.name", "Your work speed at tables"),
                Translations.Get("WorkSpeed.description",
                    "How much progress each of your hits makes when you work at a table yourself, such as " +
                    "workbenches, the autopsy and embalming table or the study table (1.0 = normal, up to 10x). " +
                    "Every hit still costs energy as usual, so a craft costs less energy in total. Recipes above " +
                    "your mastery stay locked."), 0);

            var harmony = new Harmony(Guid);
            harmony.CreateClassProcessor(typeof(PlayerUpdatePatch)).Patch();
            harmony.CreateClassProcessor(typeof(SleepDeprivationPatch)).Patch();
            harmony.CreateClassProcessor(typeof(InfiniteHealthPatch)).Patch();
            harmony.CreateClassProcessor(typeof(InfiniteEnergyConsumePatch)).Patch();
            harmony.CreateClassProcessor(typeof(InfiniteEnergyCheckPatch)).Patch();
            harmony.CreateClassProcessor(typeof(SleepWithFullEnergyPatch)).Patch();
            harmony.CreateClassProcessor(typeof(WorkSpeedPatch)).Patch();

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
                           $"health {HealthPerSecond.Value:0.0}/s, energy {EnergyPerSecond.Value:0.0}/s, " +
                           $"insanity -{InsanityReductionPerSecond.Value:0.0}/s, sleep deprivation speed {SleepDeprivationSpeed.Value:0.0}x, " +
                           $"infinite health {(InfiniteHealth.Value ? "on" : "off")}, infinite energy {(InfiniteEnergy.Value ? "on" : "off")}, " +
                           $"sleep with full energy {(SleepWithFullEnergy.Value ? "on" : "off")}, work speed {WorkSpeed.Value:0.0}x).");
        }

        private ConfigEntry<bool> BindToggle(string section, string key, bool defaultValue, string displayName, string description, int order)
        {
            return Config.Bind(section, key, defaultValue,
                new ConfigDescription(description, null,
                    new ConfigurationManagerAttributes { Category = Category, DispName = displayName, Order = order }));
        }

        /// <summary>
        /// Legt einen Float-Eintrag an, der auf 0.1-Schritte einrastet – egal ob per F1, Framework-Menü oder Config-Datei.
        /// </summary>
        private ConfigEntry<float> BindStepped(string section, string key, float defaultValue, float min, float max,
            string displayName, string description, int order)
        {
            ConfigEntry<float> entry = Config.Bind(section, key, defaultValue,
                new ConfigDescription(description, new AcceptableValueRange<float>(min, max),
                    new ConfigurationManagerAttributes { Category = Category, DispName = displayName, Order = order }));

            SnapToStep(entry);
            // Der erneute Aufruf durch das Setzen findet den Wert bereits eingerastet vor und tut nichts mehr.
            entry.SettingChanged += (sender, e) => SnapToStep(entry);
            return entry;
        }

        private static void SnapToStep(ConfigEntry<float> entry)
        {
            float snapped = (float)Math.Round(Mathf.Round(entry.Value / Step) * Step, 1);
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
