using System;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;

namespace ResurrectEveryDay
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(FrameworkGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "de.gk2mods.resurrecteveryday";
        public const string Name = "Resurrect Every Day";
        public const string Version = "1.2.0";

        internal const string FrameworkGuid = "ru.superman4eg.gk2.framework";
        internal const string ModId = "resurrecteveryday";

        internal const string Category = "Resurrect Every Day";

        internal static Plugin Instance;

        // Die Framework-Mod liest/schreibt denselben Eintrag ("Framework", "Enabled").
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<bool> AnyDay;
        internal static ConfigEntry<bool> UnlimitedPower;
        internal static ConfigEntry<bool> PrMachineAnyDay;

        internal static bool FrameworkMode;
        internal static bool FrameworkEnabled;

        /// <summary>Wirkt die Mod gerade? Mit Framework entscheidet dessen An/Aus-Zustand.</summary>
        internal static bool IsActive => FrameworkMode ? FrameworkEnabled : Enabled.Value;

        internal static bool AnyDayActive => IsActive && AnyDay.Value;

        internal static bool UnlimitedPowerActive => IsActive && UnlimitedPower.Value;

        internal static bool PrMachineAnyDayActive => IsActive && PrMachineAnyDay.Value;

        private void Awake()
        {
            Instance = this;

            Enabled = Config.Bind("Framework", "Enabled", true, Describe(
                Translations.Get("Enabled.name", "Mod enabled"),
                Translations.Get("Enabled.description", "Turns the mod on or off (takes effect immediately)."),
                3));

            AnyDay = Config.Bind("Resurrection", "AnyDay", true, Describe(
                Translations.Get("AnyDay.name", "Resurrect on any day"),
                Translations.Get("AnyDay.description",
                    "Allow resurrections on every day of the week, not just on the Day of Envy."),
                2));

            UnlimitedPower = Config.Bind("Resurrection", "UnlimitedPower", true, Describe(
                Translations.Get("UnlimitedPower.name", "Unlimited storm power"),
                Translations.Get("UnlimitedPower.description",
                    "The power switch in the resurrection lab always has power, so you can resurrect as often as you " +
                    "like. Off = vanilla: the power recharges once a week at the end of the Day of Lust and is used " +
                    "up by a resurrection."),
                1));

            PrMachineAnyDay = Config.Bind("PR Machine", "PrMachineAnyDay", true, Describe(
                Translations.Get("PrMachineAnyDay.name", "PR Machine on any day"),
                Translations.Get("PrMachineAnyDay.description",
                    "Use the PR Machine on every day of the week, not just on the Day of Gluttony, so you can raise " +
                    "the zombie limit again as soon as the previous run has ended. A run still ends at the end of a " +
                    "Day of Wrath, like in vanilla: started on the Day of Wrath, its first week is over the same " +
                    "evening."),
                0));

            var harmony = new Harmony(Guid);
            harmony.CreateClassProcessor(typeof(ResurrectionDayPatch)).Patch();
            harmony.CreateClassProcessor(typeof(ResurrectionPowerCheckPatch)).Patch();
            harmony.CreateClassProcessor(typeof(ResurrectionPowerKeepPatch)).Patch();
            harmony.CreateClassProcessor(typeof(PrMachineDayPatch)).Patch();

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
                           $"unlimited power: {(UnlimitedPower.Value ? "on" : "off")}, " +
                           $"PR Machine on any day: {(PrMachineAnyDay.Value ? "on" : "off")}).");
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
