using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Neutral, empty age entry. No rewards, age thresholds or preferred answers are shown.</summary>
public sealed class AdAudiencePanel : MonoBehaviour
{
    private SettingsManager _settings;
    private TMP_InputField _input;
    private TMP_Text _error;
    private bool _finished;

    public static IEnumerator Present(RectTransform root, TMP_FontAsset font, SettingsManager settings)
    {
        if (root == null || settings == null || AdAudiencePolicy.HasChoice(settings.GetSettings())) yield break;
        var panel = Create(root, font, settings);
        while (panel != null && !panel._finished) yield return null;
    }

    public static AdAudiencePanel Create(RectTransform root, TMP_FontAsset font, SettingsManager settings)
    {
        var overlay = Box(root, "AdAudiencePanel", Vector2.zero, Vector2.one, new Color(0, 0, 0, 0.78f));
        overlay.SetAsLastSibling();
        var panel = overlay.gameObject.AddComponent<AdAudiencePanel>();
        panel._settings = settings;
        var card = Box(overlay, "Card", new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.86f),
            new Color(0.13f, 0.18f, 0.15f, 1));
        var title = Label(card, "Title", new Vector2(0.07f, 0.82f), new Vector2(0.93f, 0.96f), font, 42);
        title.text = Loc.Get("age.title");
        title.fontStyle = FontStyles.Bold;
        var description = Label(card, "Description", new Vector2(0.08f, 0.59f), new Vector2(0.92f, 0.81f), font, 29);
        description.text = Loc.Get("age.description");

        var field = Box(card, "AgeInput", new Vector2(0.10f, 0.44f), new Vector2(0.90f, 0.56f),
            new Color(0.95f, 0.96f, 0.92f));
        var viewport = Box(field, "Viewport", new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f), Color.clear);
        viewport.gameObject.AddComponent<RectMask2D>();
        var inputText = Label(viewport, "Value", Vector2.zero, Vector2.one, font, 36);
        inputText.color = new Color(0.10f, 0.15f, 0.12f);
        var placeholder = Label(viewport, "Placeholder", Vector2.zero, Vector2.one, font, 32);
        placeholder.text = Loc.Get("age.placeholder");
        placeholder.color = new Color(0.34f, 0.39f, 0.36f);
        panel._input = field.gameObject.AddComponent<TMP_InputField>();
        panel._input.targetGraphic = field.GetComponent<Image>();
        panel._input.textViewport = viewport;
        panel._input.textComponent = inputText;
        panel._input.placeholder = placeholder;
        panel._input.contentType = TMP_InputField.ContentType.IntegerNumber;
        panel._input.keyboardType = TouchScreenKeyboardType.NumberPad;
        panel._input.characterLimit = 3;
        panel._input.lineType = TMP_InputField.LineType.SingleLine;
        panel._input.text = "";
        panel._error = Label(card, "Error", new Vector2(0.07f, 0.34f), new Vector2(0.93f, 0.43f), font, 26);
        panel._error.color = new Color(1f, 0.75f, 0.65f);
        panel._error.text = "";
        Button(card, "Continue", new Vector2(0.10f, 0.19f), new Vector2(0.90f, 0.31f), font,
            Loc.Get("age.continue")).onClick.AddListener(panel.Confirm);
        Button(card, "Unknown", new Vector2(0.10f, 0.04f), new Vector2(0.90f, 0.16f), font,
            Loc.Get("age.unknown")).onClick.AddListener(panel.Decline);
        return panel;
    }

    private void Confirm()
    {
        if (_finished || _settings == null) return;
        if (!_settings.RecordAdAudience(_input.text))
        {
            _error.text = Loc.Get("age.invalid");
            return;
        }
        Finish();
    }

    private void Decline()
    {
        if (_finished || _settings == null || !_settings.RecordUnknownAdAudience()) return;
        Finish();
    }

    private void Finish()
    {
        _finished = true;
        _input.DeactivateInputField();
        _input.SetTextWithoutNotify("");
        Destroy(gameObject);
    }

    private static RectTransform Box(RectTransform parent, string name, Vector2 min, Vector2 max, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = color;
        return rt;
    }

    private static TMP_Text Label(RectTransform parent, string name, Vector2 min, Vector2 max, TMP_FontAsset font, float size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var text = go.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = size;
        text.enableAutoSizing = true;
        text.fontSizeMin = size * 0.8f;
        text.fontSizeMax = size;
        text.color = new Color(0.95f, 0.97f, 0.93f);
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private static Button Button(RectTransform parent, string name, Vector2 min, Vector2 max, TMP_FontAsset font, string label)
    {
        var rt = Box(parent, name, min, max, new Color(0.24f, 0.40f, 0.30f));
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = rt.GetComponent<Image>();
        Label(rt, "Label", new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.95f), font, 32).text = label;
        return button;
    }
}
