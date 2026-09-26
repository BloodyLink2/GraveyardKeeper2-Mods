using System;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;

namespace FlexibleCeremonies
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(FrameworkGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "de.gk2mods.flexibleceremonies";
        public const string Name = "Sermon Every Day";
        public const string Version = "1.2.0";

        internal const string FrameworkGuid = "ru.superman4eg.gk2.framework";
        internal const string ModId = "flexibleceremonies";

        internal const string Category = "Sermon Every Day";
        internal const int CeremoniesPerDayMax = 10;

        internal static Plugin Instance;

        // Die Framework-Mod liest/schreibt denselben Eintrag ("Framework", "Enabled").
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<bool> AnyDay;
        internal static ConfigEntry<int> CeremoniesPerDay;
        internal static ConfigEntry<bool> IgnoreGratitude;

        internal static bool FrameworkMode;
        internal static bool FrameworkEnabled;

        /// <summary>Wirkt die Mod gerade? Mit Framework entscheidet dessen An/Aus-Zustand.</summary>
        internal static bool IsActive => FrameworkMode ? FrameworkEnabled : Enabled.Value;

        internal static bool IgnoreGratitudeActive => IsActive && IgnoreGratitude.Value;

        private void Awake()
        {
            Instance = this;

            Enabled = Config.Bind("Framework", "Enabled", true, Describe(
                Translations.Get("Enabled.name", "Mod enabled"),
                Translations.Get("Enabled.description", "Turns the mod on or off (takes effect immediately)."),
                3));

            AnyDay = Config.Bind("Ceremonies", "AnyDay", true, Describe(
                Translations.Get("AnyDay.name", "Sermons on any day"),
                Translations.Get("AnyDay.description",
                    "Allow sermons on every day of the week, not just on the Day of Wrath."),
                2));

            CeremoniesPerDay = Config.Bind("Ceremonies", "CeremoniesPerDay", 0, Describe(
                Translations.Get("CeremoniesPerDay.name", "Sermons per day"),
                Translations.Get("CeremoniesPerDay.description",
                    "How many sermons are allowed per day (0 = unlimited). Unless \"Ignore Gratitude\" is on, every " +
                    "sermon still consumes Gratitude equal to the number of parishioners. That remains the natural " +
                    "limit."),
                1, new AcceptableValueRange<int>(0, CeremoniesPerDayMax)));

            IgnoreGratitude = Config.Bind("Ceremonies", "IgnoreGratitude", false, Describe(
                Translations.Get("IgnoreGratitude.name", "Ignore Gratitude"),
                Translations.Get("IgnoreGratitude.description",
                    "Sermons no longer depend on Gratitude: the number of parishioners is set by church quality " +
                    "alone and no Gratitude is consumed. Takes effect the next time you open the prayer window."),
                0));

            var harmony = new Harmony(Guid);
            harmony.CreateClassProcessor(typeof(PrayerStandPatch)).Patch();
            harmony.CreateClassProcessor(typeof(CeremonyStartPatch)).Patch();
            harmony.CreateClassProcessor(typeof(PrayWindowVisitorsPatch)).Patch();
            harmony.CreateClassProcessor(typeof(SermonGratitudeCostPatch)).Patch();

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
                           $"any day: {(AnyDay.Value ? "on" : "off")}, " +
                           $"per day: {(CeremoniesPerDay.Value <= 0 ? "unlimited" : CeremoniesPerDay.Value.ToString())}, " +
                           $"ignore gratitude: {(IgnoreGratitude.Value ? "on" : "off")}).");
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
