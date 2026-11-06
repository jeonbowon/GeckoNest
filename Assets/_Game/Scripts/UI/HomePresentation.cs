using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Terrarium HUD with equal-width controls and colorful illustrated buttons.</summary>
public static class HomePresentation
{
    private static readonly Color Panel = new Color(0.09f, 0.12f, 0.065f, 0.88f);
    private static readonly Color Ivory = new Color(0.94f, 0.9f, 0.73f);
    private static readonly Color BottomBacking = new Color(0.10f, 0.15f, 0.08f, 0.12f);
    private static readonly Color ButtonLeaf = new Color(0.23f, 0.32f, 0.18f, 0.62f);
    private static TMP_FontAsset s_buttonFont;

    public static void Apply(RectTransform root, Button[] care, Button[] nav)
    {
        if (root == null) return;
        var actions = root.Find("ActionButtons") as RectTransform;
        if (actions != null)
        {
            Bar(actions, 160f, 170f, -64f);
            PanelImage(actions);
            actions.GetComponent<Image>().color = BottomBacking;
            ArrangeButtons(actions, care, 0, 44f, 88f);
        }
        var navigation = root.Find("NavBar") as RectTransform;
        if (navigation != null)
        {
            Bar(navigation, 16f, 128f, -64f);
            PanelImage(navigation);
            navigation.GetComponent<Image>().color = BottomBacking;
            ArrangeButtons(navigation, nav, 4, 40f, 64f);
        }
        var habitat = root.Find("GeckoArea") as RectTransform;
        var topBar = root.Find("TopBar");
        if (habitat != null && topBar != null && topBar.GetSiblingIndex() < habitat.GetSiblingIndex())
            topBar.SetSiblingIndex(habitat.GetSiblingIndex());
        var backingParent = habitat != null ? habitat : root;
        if (backingParent.Find("HabitatHudBacking") == null)
        {
            var go = new GameObject("HabitatHudBacking", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(backingParent, false);
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -28f); rt.sizeDelta = new Vector2(-64f, 312f);
            PanelImage(rt);
            var background = backingParent.Find("Background");
            rt.SetSiblingIndex(background != null ? background.GetSiblingIndex() + 1 : 0);
        }
    }

    public static void PanelImage(RectTransform rt)
    {
        var image = rt.GetComponent<Image>();
        if (image == null) image = rt.gameObject.AddComponent<Image>();
        image.sprite = FxSprites.Bubble; image.type = Image.Type.Sliced;
        image.color = Panel; image.raycastTarget = false;
    }

    private static void Bar(RectTransform rt, float y, float height, float widthDelta)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.right;
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, y); rt.sizeDelta = new Vector2(widthDelta, height);
    }

    private static void ArrangeButtons(RectTransform bar, Button[] buttons, int firstIcon, float fontSize, float iconSize)
    {
        // TMP preferred/minimum widths must not push the last button beyond the bar.
        // Fractional anchors give every control the same share, regardless of label language.
        foreach (var layout in bar.GetComponents<LayoutGroup>()) layout.enabled = false;
        for (int i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            if (button == null) continue;
            var rt = (RectTransform)button.transform;
            foreach (var layout in button.GetComponents<LayoutGroup>()) layout.enabled = false;
            var fitter = button.GetComponent<ContentSizeFitter>();
            if (fitter != null) fitter.enabled = false;
            rt.anchorMin = new Vector2((float)i / buttons.Length, 0f);
            rt.anchorMax = new Vector2((float)(i + 1) / buttons.Length, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(8f, 8f);
            rt.offsetMax = new Vector2(-8f, -8f);
            StyleButton(button, fontSize);
            var oldIcon = rt.Find("Icon");
            if (oldIcon != null) oldIcon.gameObject.SetActive(false);
            var emoji = rt.Find("Emoji");
            if (emoji != null) emoji.gameObject.SetActive(false);
            var art = rt.Find("FriendlyIcon") as RectTransform;
            if (art == null)
            {
                var go = new GameObject("FriendlyIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                art = (RectTransform)go.transform;
                art.SetParent(rt, false);
            }
            art.anchorMin = art.anchorMax = new Vector2(0.5f, 0.67f);
            art.anchoredPosition = Vector2.zero;
            art.sizeDelta = new Vector2(iconSize, iconSize);
            var image = art.GetComponent<Image>();
            image.sprite = HomeButtonArt.Get(firstIcon + i);
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }
    }

    private static void StyleButton(Button button, float fontSize)
    {
        var image = button.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = FxSprites.Bubble; image.type = Image.Type.Sliced;
            image.color = ButtonLeaf;
        }
        button.transition = Selectable.Transition.ColorTint;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.10f, 1.03f, 1f);
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.80f, 0.87f, 0.75f, 1f);
        colors.disabledColor = new Color(0.65f, 0.65f, 0.65f, 0.6f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.12f;
        button.colors = colors;
        foreach (var text in button.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name == "Emoji") continue;
            var font = ButtonFont(text.font);
            if (font != null)
            {
                text.font = font;
                text.fontSharedMaterial = font.material;
            }
            text.fontStyle = FontStyles.Normal;
            text.fontWeight = FontWeight.Regular;
            text.color = Ivory; text.fontSize = fontSize;
            text.fontSizeMin = fontSize - 5f; text.fontSizeMax = fontSize;
            text.enableAutoSizing = true;
            text.alignment = TextAlignmentOptions.Center;
            var label = text.rectTransform;
            label.anchorMin = Vector2.zero;
            label.anchorMax = new Vector2(1f, 0.35f);
            label.offsetMin = new Vector2(4f, 0f);
            label.offsetMax = new Vector2(-4f, 0f);
        }
        foreach (var outline in button.GetComponents<Outline>()) outline.enabled = false;
        foreach (var shadow in button.GetComponents<Shadow>()) shadow.enabled = false;
    }

    private static TMP_FontAsset ButtonFont(TMP_FontAsset fallback)
    {
        if (s_buttonFont != null) return s_buttonFont;
        var source = Resources.Load<Font>("Fonts/Jua-Regular");
        if (source == null) return null;
        // Only short home labels use this atlas; keep the existing static body font.
        s_buttonFont = TMP_FontAsset.CreateFontAsset(source, 64, 8,
            UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024,
            AtlasPopulationMode.Dynamic, true);
        if (s_buttonFont == null) return null;
        s_buttonFont.name = "HAKO Rounded Button Font";
        s_buttonFont.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
        if (fallback != null) s_buttonFont.fallbackFontAssetTable.Add(fallback);
        var material = s_buttonFont.material;
        ShaderUtilities.GetShaderPropertyIDs();
        material.EnableKeyword("UNDERLAY_ON");
        material.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0.04f, 0.07f, 0.03f, 0.65f));
        material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.3f);
        material.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.08f);
        material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.25f);
        return s_buttonFont;
    }
}
