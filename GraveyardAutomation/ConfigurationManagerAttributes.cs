// Vereinfachte Version der offiziellen Vorlage aus BepInEx.ConfigurationManager.
// Der ConfigurationManager erkennt die Klasse am Namen und liest die Felder per Reflection –
// eine Referenz auf dessen DLL ist nicht nötig.
#pragma warning disable 0169, 0414, 0649
internal sealed class ConfigurationManagerAttributes
{
    public bool? Browsable;
    public string Category;
    public object DefaultValue;
    public bool? HideDefaultButton;
    public bool? HideSettingName;
    public string Description;
    public string DispName;
    public int? Order;
    public bool? ReadOnly;
    public bool? IsAdvanced;
}
