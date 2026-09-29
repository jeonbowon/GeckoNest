using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Quiet terrarium HUD. Reuses the existing controls and callbacks.</summary>
public static class HomePresentation
{
    private static readonly Color Panel = new Color(0.09f, 0.12f, 0.065f, 0.88f);
    private static readonly Color Ivory = new Color(0.94f, 0.9f, 0.73f);

    public static void Apply(RectTransform root, Button[] care, Button[] nav)
    {
        if (root == null) return;
        var actions = root.Find("ActionButtons") as RectTransform;
        if (actions != null)
        {
            Bar(actions, 160f, 170f, -64f);
            PanelImage(actions);
            for (int i = 0; i < care.Length; i++)
            {
                if (care[i] == null) continue;
                var rt = (RectTransform)care[i].transform;
                rt.anchorMin = new Vector2(i / 4f, 0f);
                rt.anchorMax = new Vector2((i + 1) / 4f, 1f);
                rt.offsetMin = new Vector2(8f, 8f); rt.offsetMax = new Vector2(-8f, -8f);
                StyleButton(care[i], 27f);
                var icon = rt.Find("Icon") as RectTransform;
                if (icon != null)
                {
                    icon.sizeDelta = new Vector2(48f, 48f);
                    icon.anchoredPosition = Vector2.zero;
                    var img = icon.GetComponent<Image>();
                    if (i == 2 && img != null) { img.sprite = FxSprites.Hand; img.color = Ivory; }
                    if (i == 3 && img != null) { img.sprite = FxSprites.Broom; img.color = Ivory; }
                }
            }
        }
        var navigation = root.Find("NavBar") as RectTransform;
        if (navigation != null)
        {
            Bar(navigation, 16f, 128f, -64f);
            PanelImage(navigation);
            var layout = navigation.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.childControlWidth = true; layout.childControlHeight = true;
                layout.childForceExpandWidth = true; layout.childForceExpandHeight = true;
                layout.spacing = 8f; layout.padding = new RectOffset(12, 12, 8, 8);
            }
            foreach (var button in nav) if (button != null) StyleButton(button, 24f);
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

    private static void StyleButton(Button button, float fontSize)
    {
        var image = button.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = FxSprites.Bubble; image.type = Image.Type.Sliced;
            image.color = new Color(0.17f, 0.21f, 0.12f, 0.32f);
        }
        foreach (var text in button.GetComponentsInChildren<TMP_Text>(true))
        {
            text.color = Ivory; text.fontSize = fontSize;
            text.fontSizeMin = fontSize - 5f; text.fontSizeMax = fontSize;
            text.enableAutoSizing = true;
        }
        foreach (var outline in button.GetComponents<Outline>()) outline.enabled = false;
        foreach (var shadow in button.GetComponents<Shadow>()) shadow.enabled = false;
        var icon = button.transform.Find("Emoji/Icon");
        if (icon != null)
        {
            var graphic = icon.GetComponent<Image>();
            if (graphic != null) graphic.color = Ivory;
        }
    }
}
