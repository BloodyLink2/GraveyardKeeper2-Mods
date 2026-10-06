using System;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace BiggerStorage
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(FrameworkGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "de.gk2mods.biggerstorage";
        public const string Name = "Bigger Storage";
        public const string Version = "1.1.0";

        internal const string FrameworkGuid = "ru.superman4eg.gk2.framework";
        internal const string ModId = "biggerstorage";

        internal const string Category = "Bigger Storage";
        internal const int ExtraSlotsMax = 100;
        internal const int StackSizeMax = 300;
        internal const int StackSizeStep = 10;

        internal static Plugin Instance;

        // Die Framework-Mod liest/schreibt denselben Eintrag ("Framework", "Enabled").
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<int> ExtraChestSlots;
        internal static ConfigEntry<int> ExtraStorageSlots;
        internal static ConfigEntry<bool> ShowFillLevel;
        internal static ConfigEntry<int> StackSize;
        internal static ConfigEntry<bool> PrepareUninstall;

        internal static bool FrameworkMode;
        internal static bool FrameworkEnabled;

        /// <summary>Wirkt die Mod gerade? Mit Framework entscheidet dessen An/Aus-Zustand.</summary>
        internal static bool IsActive => FrameworkMode ? FrameworkEnabled : Enabled.Value;

        /// <summary>Zusatzplätze, die gerade gelten: 0, wenn die Mod aus ist oder die Deinstallation vorbereitet wird.</summary>
        internal static int EffectiveExtraSlots =>
            IsActive && !PrepareUninstall.Value ? Math.Max(0, ExtraChestSlots.Value) : 0;

        /// <summary>Dasselbe für die Lager von Stämmen, Stein- und Marmorblöcken.</summary>
        internal static int EffectiveExtraStorageSlots =>
            IsActive && !PrepareUninstall.Value ? Math.Max(0, ExtraStorageSlots.Value) : 0;

        /// <summary>Stapelgröße, die gerade gilt: 0 = normal, wenn die Mod aus ist oder die Deinstallation vorbereitet wird.</summary>
        internal static int EffectiveStackSize => IsActive && !PrepareUninstall.Value ? StackSize.Value : 0;

        private void Awake()
        {
            Instance = this;

            Enabled = Config.Bind("Framework", "Enabled", true, Describe(
                Translations.Get("Enabled.name", "Mod enabled"),
                Translations.Get("Enabled.description",
                    "Turns the mod on or off (takes effect immediately). Off = chests and storages shrink back to " +
                    "their normal size as far as their contents allow, and stack sizes are back to normal."),
                5));

            ExtraChestSlots = Config.Bind("Chests", "ExtraSlots", 10, Describe(
                Translations.Get("ExtraSlots.name", "Extra chest slots"),
                Translations.Get("ExtraSlots.description",
                    "How many slots every chest gets on top of its normal size (0 = normal, up to 100). Applies to " +
                    "all chests you can open, also the ones you already built. Lowering it shrinks chests again, but " +
                    "never below the slots in use, so nothing gets lost."),
                4, new AcceptableValueRange<int>(0, ExtraSlotsMax)));

            ExtraStorageSlots = Config.Bind("Chests", "ExtraStorageSlots", 10, Describe(
                Translations.Get("ExtraStorageSlots.name", "Extra slots for wood, stone and marble"),
                Translations.Get("ExtraStorageSlots.description",
                    "How many slots the storages for logs, stone blocks and marble blocks (e.g. the wood storage at " +
                    "the sawmill) get on top of their normal size (0 = normal, up to 100), also the ones you already " +
                    "built. Lowering it shrinks them again, but never below the slots in use."),
                3, new AcceptableValueRange<int>(0, ExtraSlotsMax)));

            ShowFillLevel = Config.Bind("Chests", "ShowFillLevel", true, Describe(
                Translations.Get("ShowFillLevel.name", "Show fill level"),
                Translations.Get("ShowFillLevel.description",
                    "Shows how full the storages for logs, stone blocks and marble blocks are, e.g. 12/20, in a " +
                    "small bubble above them, like the one above fuel storages."),
                2));

            StackSize = Config.Bind("Stacks", "StackSize", 0, Describe(
                Translations.Get("StackSize.name", "Stack size"),
                Translations.Get("StackSize.description",
                    "How many items fit on one slot in every inventory: yours, chests and stations (0 = normal, up " +
                    "to 300, in steps of 10). Items that already stack higher keep their size, items that do not " +
                    "stack (like tools) stay single. Lowering it keeps bigger stacks you already have; they just do " +
                    "not grow anymore."),
                1, new AcceptableValueRange<int>(0, StackSizeMax)));

            PrepareUninstall = Config.Bind("Uninstall", "PrepareUninstall", false, Describe(
                Translations.Get("PrepareUninstall.name", "Prepare for uninstall"),
                Translations.Get("PrepareUninstall.description",
                    "Turn this on before you remove the mod: chests and storages go back to their normal size and " +
                    "stack sizes to normal, whatever else is set. One that still holds more items than its normal " +
                    "size keeps the extra slots it needs and shrinks as soon as you take items out. A message tells " +
                    "you how many are still too full. When all are back to normal, sleep once so the game " +
                    "saves, then uninstall. Bigger stacks you already have stay as they are; the game handles them " +
                    "fine without the mod."),
                0));

            SnapStackSize();
            ExtraChestSlots.SettingChanged += (sender, e) => ChestSlots.ApplyAll();
            ExtraStorageSlots.SettingChanged += (sender, e) => ChestSlots.ApplyAll();
            ShowFillLevel.SettingChanged += (sender, e) => StorageBubble.RedrawAll();
            PrepareUninstall.SettingChanged += (sender, e) =>
            {
                StackSizes.Apply();
                UninstallGuard.Report(ChestSlots.ApplyAll());
            };
            StackSize.SettingChanged += (sender, e) =>
            {
                if (SnapStackSize()) return; // Das Einrasten löst SettingChanged erneut aus – dann wird dort angewendet.
                StackSizes.Apply();
            };

            var harmony = new Harmony(Guid);
            harmony.CreateClassProcessor(typeof(NewChestPatch)).Patch();
            harmony.CreateClassProcessor(typeof(OversizedStackPatch)).Patch();
            harmony.CreateClassProcessor(typeof(StorageViewInitPatch)).Patch();
            harmony.CreateClassProcessor(typeof(StorageViewDeInitPatch)).Patch();
            harmony.CreateClassProcessor(typeof(StorageBubblePatch)).Patch();
            harmony.CreateClassProcessor(typeof(StorageBubbleIconPatch)).Patch();

            // Nach dem Laden eines Spielstands (und bei einem neuen Spiel) alle Truhen und Stapel anpassen.
            MainGame.OnGameStarted += ApplyEverything;

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
                           $"extra chest slots +{ExtraChestSlots.Value}, " +
                           $"extra wood/stone/marble slots +{ExtraStorageSlots.Value}, " +
                           $"fill level {(ShowFillLevel.Value ? "on" : "off")}, " +
                           $"stack size {(StackSize.Value <= 0 ? "normal" : StackSize.Value.ToString())}" +
                           $"{(PrepareUninstall.Value ? ", preparing for uninstall" : "")}).");
        }

        private void Update() => UninstallGuard.Update(Time.unscaledDeltaTime);

        /// <summary>Wendet Truhengröße und Stapelgröße auf den laufenden Spielstand an (ohne Spielstand: nichts).</summary>
        internal static void ApplyEverything()
        {
            StackSizes.Apply();
            UninstallGuard.Report(ChestSlots.ApplyAll());
        }

        /// <summary>Rastet die Stapelgröße auf 10er-Schritte ein. true = der Wert wurde geändert.</summary>
        private static bool SnapStackSize()
        {
            int snapped = Mathf.Clamp(Mathf.RoundToInt(StackSize.Value / (float)StackSizeStep) * StackSizeStep, 0, StackSizeMax);
            if (snapped == StackSize.Value) return false;
            StackSize.Value = snapped;
            return true;
        }

        private void OnEnabledChanged(object sender, EventArgs e)
        {
            ApplyEverything();

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
