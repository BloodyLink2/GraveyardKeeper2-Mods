using System;
using BepInEx.Bootstrap;

namespace PlayerRegeneration
{
    /// <summary>
    /// Texte fürs F1-Menü, das Framework-Menü und die Mod-Beschreibung. Übersetzungen liefert das GK2 Mod Framework
    /// (ab 0.1.8) aus BepInEx/plugins/GK2.Framework/Localization/&lt;ModId&gt;/&lt;Sprache&gt;.json, passend zur
    /// Spielsprache (z. B. de, ko). Ohne Framework, mit älterem Framework oder ohne Eintrag bleibt der englische Text aus
    /// dem Code. Localization/en.json (Vorlage für Übersetzer) erzeugt tools/localization.py aus diesen Aufrufen.
    /// </summary>
    internal static class Translations
    {
        private static Func<string, string, string, string> lookup;
        private static bool resolved;

        internal static string Get(string key, string english)
        {
            if (!resolved)
            {
                resolved = true;
                if (Chainloader.PluginInfos.ContainsKey(Plugin.FrameworkGuid))
                {
                    try
                    {
                        lookup = FrameworkBridge.GetTranslationLookup();
                    }
                    catch (Exception e)
                    {
                        Plugin.LogWarning("Translations are not available: " + e.Message);
                    }
                }
            }
            if (lookup == null) return english;
            try
            {
                return lookup(Plugin.ModId, key, english);
            }
            catch (Exception)
            {
                return english;
            }
        }
    }
}
