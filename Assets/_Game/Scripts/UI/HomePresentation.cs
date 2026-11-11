using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Terrarium HUD with equal-width controls and colorful illustrated buttons.</summary>
public static class HomePresentation
{
    private static readonly Color Panel = new Color(0.09f, 0.12f, 0.065f, 0.88f);
    private static readonly Color Ivory = new Color(0.94f, 0.9f, 0.73f);
    private static readonly Color BottomBacking = new Color(0.10f, 0.15f, 0.08f, 0.06f);
    private static readonly Color ButtonLeaf = new Color(0.36f, 0.49f, 0.29f, 0.48f);
    private static readonly Color[] CareColors =
    {
        new Color(0.78f, 0.49f, 0.32f, 0.48f), // peach: food
        new Color(0.35f, 0.62f, 0.75f, 0.48f), // sky: water
        new Color(0.74f, 0.43f, 0.52f, 0.48f), // rose: pet
        new Color(0.36f, 0.64f, 0.51f, 0.48f), // mint: clean
    };
    private static Sprite s_buttonShape, s_buttonRim;
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
            StyleButton(button, fontSize, firstIcon == 0 ? CareColors[i % CareColors.Length] : ButtonLeaf);
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

    private static void StyleButton(Button button, float fontSize, Color tint)
    {
        var image = button.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = ButtonShape(false); image.type = Image.Type.Sliced;
            image.color = tint;
            button.targetGraphic = image;
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
        // A hollow rim adds definition without another opaque fill over the habitat.
        ButtonDecoration(button, "SoftRimShadow", new Color(0.08f, 0.13f, 0.06f, 0.20f), new Vector2(0f, -3f));
        ButtonDecoration(button, "CreamRim", new Color(1f, 0.94f, 0.79f, 0.48f), Vector2.zero);
        UIPressScale.Ensure(button);
    }

    private static void ButtonDecoration(Button button, string name, Color color, Vector2 offset)
    {
        var rt = button.transform.Find(name) as RectTransform;
        if (rt == null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            rt = (RectTransform)go.transform;
            rt.SetParent(button.transform, false);
        }
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = offset; rt.offsetMax = offset;
        rt.SetAsFirstSibling();
        var image = rt.GetComponent<Image>();
        image.sprite = ButtonShape(true);
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = false;
    }

    private static Sprite ButtonShape(bool rim)
    {
        var cached = rim ? s_buttonRim : s_buttonShape;
        if (cached != null && cached.texture != null) return cached;
        // Dedicated runtime geometry: never overwrite shared Bubble or final PNG assets.
        const int size = 96;
        const float radius = 28f;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float qx = Mathf.Abs(x + 0.5f - size * 0.5f) - (size * 0.5f - radius - 1f);
            float qy = Mathf.Abs(y + 0.5f - size * 0.5f) - (size * 0.5f - radius - 1f);
            float d = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude
                + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
            float alpha = Mathf.Clamp01(0.5f - d);
            if (rim) alpha *= Mathf.Clamp01(d + 2.5f);
            pixels[x + y * size] = new Color(1f, 1f, 1f, alpha);
        }
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = rim ? "HomeButtonRim" : "HomeButtonShape",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave,
        };
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, new Vector4(32f, 32f, 32f, 32f));
        sprite.name = texture.name;
        sprite.hideFlags = HideFlags.HideAndDontSave;
        if (rim) s_buttonRim = sprite;
        else s_buttonShape = sprite;
        return sprite;
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
