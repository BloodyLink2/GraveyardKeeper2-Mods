using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BiggerStorage
{
    /// <summary>
    /// "Show fill level": Blase über den Lagern für Stämme, Stein- und Marmorblöcke, z. B. "12/20". Vorbild ist die Blase
    /// über den Brennstofflagern: Wgo.GetWidgetData hängt dort ein UIQualityTooltipWidgetData(Item-Id, Anzahl) an, und
    /// Wgo.InitDataBindings meldet die Ansicht an OnItemsAdd/OnItemsRemove an, damit Wgo.DrawWidgets die Blase bei jeder
    /// Änderung neu zeichnet (Wgo.DeInit meldet wieder ab). Genau das bauen wir für die Materiallager nach.
    /// Das Icon zeichnet UIQualityTooltipWidget als TextMeshPro-Tag &lt;sprite name="..."&gt; aus dem Font-Sprite-Atlas.
    /// Der kennt nur UI-Symbole (Zahnrad, Feuer, Runen ...), aber kein Holz, keinen Stein und keinen Marmor, das Tag
    /// stünde dann als Text da. Für unsere Blasen ersetzt DrawIcon es deshalb durch ein Bild des Item-Icons.
    /// </summary>
    internal static class StorageBubble
    {
        private const string IconObjectName = "BiggerStorageIcon";

        /// <summary>Font-Icon, dessen Größe unser Bild bekommt (zeigt die Blase über Zahnrad-Antrieben).</summary>
        private const string SizeReferenceIcon = "gear";

        private static readonly Dictionary<Wgo, Action<List<Item>>> attached = new Dictionary<Wgo, Action<List<Item>>>();
        private static readonly List<Wgo> redrawBuffer = new List<Wgo>();

        /// <summary>Unsere Blasendaten mit der Icon-Id des Materials (die Widgets teilen sie sich mit den Brennstofflagern).</summary>
        private static readonly ConditionalWeakTable<UIQualityTooltipWidgetData, string> ownWidgets =
            new ConditionalWeakTable<UIQualityTooltipWidgetData, string>();

        private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        internal static void Attach(Wgo view)
        {
            WgoData data = view == null ? null : view.Data;
            if (data?.Inventory == null || attached.ContainsKey(view) || !ChestSlots.IsMaterialStorage(data)) return;

            Action<List<Item>> redraw = items => view.DrawWidgets();
            data.Inventory.OnItemsAdd += redraw;
            data.Inventory.OnItemsRemove += redraw;
            attached[view] = redraw;
        }

        internal static void Detach(Wgo view)
        {
            if (view is null || !attached.TryGetValue(view, out Action<List<Item>> redraw)) return;
            attached.Remove(view);
            Inventory inventory = view.Data?.Inventory;
            if (inventory == null) return;
            inventory.OnItemsAdd -= redraw;
            inventory.OnItemsRemove -= redraw;
        }

        /// <summary>Nach geänderten Einstellungen (Platzanzahl, An/Aus) alle sichtbaren Lager neu zeichnen.</summary>
        internal static void RedrawAll()
        {
            redrawBuffer.Clear();
            redrawBuffer.AddRange(attached.Keys);
            foreach (Wgo view in redrawBuffer)
            {
                if (view != null) view.DrawWidgets();
            }
        }

        internal static void AddWidget(Wgo view, List<LazyWidgetDataBase> widgets)
        {
            if (!Plugin.IsActive || !Plugin.ShowFillLevel.Value || widgets == null || view == null) return;
            WgoData data = view.Data;
            if (data == null || data.IsHidden || !ChestSlots.TryGetMaterial(data, out string material)) return;

            Item inventory = data.Inventory?.Data;
            ItemDef def = GameBalance.Me?.GetData<ItemDef>(material);
            if (inventory == null || def == null) return;

            int count = inventory.GetTotalCountInInventory(material);
            int capacity = inventory.InventorySize * Math.Max(1, def.stackCount);
            var widget = new UIQualityTooltipWidgetData(material, $"{count}/{capacity}");
            ownWidgets.Add(widget, string.IsNullOrEmpty(def.iconId) ? "i_" + material : def.iconId);
            widgets.Add(widget);
        }

        /// <summary>
        /// Nach UIQualityTooltipWidget.Draw: bei unseren Blasen das Sprite-Tag durch ein Bild des Item-Icons ersetzen.
        /// Die Widgets kommen aus einem Pool, ein Widget mit unserem Bild kann also später eine Brennstoff-Blase zeigen;
        /// dort schalten wir Bild und Größenvorgabe wieder ab.
        /// </summary>
        internal static void DrawIcon(UIQualityTooltipWidgetData data, TextMeshProUGUI iconLabel)
        {
            if (data == null || iconLabel == null) return;
            Transform existing = iconLabel.transform.Find(IconObjectName);
            LayoutElement size = iconLabel.GetComponent<LayoutElement>();
            Sprite sprite = ownWidgets.TryGetValue(data, out string iconId) ? GetSprite(iconId) : null;
            if (sprite == null)
            {
                if (existing != null) existing.gameObject.SetActive(false);
                if (size != null) size.enabled = false;
                return;
            }

            // Platz wie ein Font-Icon: Die Blase ordnet Icon und Zahl per HorizontalLayoutGroup nach ihren Wunschgrößen
            // an, ein leeres Textfeld hätte keine. Die Vorgabe per LayoutElement hat Vorrang vor der des Textfelds.
            Vector2 reference = iconLabel.GetPreferredValues(SizeReferenceIcon.FontIcon());
            float height = reference.y > 0f ? reference.y : iconLabel.fontSize;
            float aspect = sprite.rect.height > 0f ? sprite.rect.width / sprite.rect.height : 1f;
            if (size == null) size = iconLabel.gameObject.AddComponent<LayoutElement>();
            size.enabled = true;
            size.preferredHeight = height;
            size.preferredWidth = height * aspect;
            iconLabel.text = string.Empty;

            Image image;
            if (existing == null)
            {
                var icon = new GameObject(IconObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                icon.layer = iconLabel.gameObject.layer;
                var rect = (RectTransform)icon.transform;
                rect.SetParent(iconLabel.transform, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                image = icon.GetComponent<Image>();
                image.raycastTarget = false;
                image.preserveAspect = true;
            }
            else
            {
                existing.gameObject.SetActive(true);
                image = existing.GetComponent<Image>();
            }
            image.sprite = sprite;
        }

        private static Sprite GetSprite(string iconId)
        {
            if (spriteCache.TryGetValue(iconId, out Sprite sprite) && sprite != null) return sprite;
            EasySpritesCollection sprites = LazySingletonSO<EasySpritesCollection>.Instance;
            sprite = sprites != null && sprites.HasSprite(iconId) ? sprites.GetSprite(iconId) : null;
            if (sprite != null) spriteCache[iconId] = sprite;
            return sprite;
        }
    }
}
