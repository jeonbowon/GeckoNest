using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DeveloperCoinPanel : MonoBehaviour
{
    private const int TapsToOpen = 7;
    private const float TapWindowSeconds = 5f;
    private static readonly Color Ink = new Color32(34, 40, 32, 255);

    private RectTransform _home;
    private TMP_FontAsset _font;
    private Func<bool> _canOpen;
    private Action _claimed;
    private GameObject _popup;
    private TMP_InputField _code;
    private TMP_Text _error;
    private float _firstTapAt;
    private int _tapCount;

    public bool IsOpen => _popup != null && _popup.activeInHierarchy;

    public static DeveloperCoinPanel Install(RectTransform home, Transform topBar, TMP_FontAsset font,
                                             Func<bool> canOpen, Action claimed)
    {
        var corner = Box(home, "DeveloperCoinCorner", Vector2.one, Vector2.one, Color.clear);
        corner.pivot = Vector2.one;
        corner.sizeDelta = new Vector2(164f, 164f);
        corner.anchoredPosition = Vector2.zero;
        if (topBar != null && topBar.parent == home) corner.SetSiblingIndex(topBar.GetSiblingIndex() + 1);

        var panel = corner.gameObject.AddComponent<DeveloperCoinPanel>();
        panel._home = home;
        panel._font = font;
        panel._canOpen = canOpen;
        panel._claimed = claimed;

        var trigger = corner.gameObject.AddComponent<Button>();
        trigger.transition = Selectable.Transition.None;
        trigger.navigation = new Navigation { mode = Navigation.Mode.None };
        trigger.onClick.AddListener(panel.CountTap);
        return panel;
    }

    private void CountTap()
    {
        if (!_canOpen() || IsOpen)
        {
            _tapCount = 0;
            return;
        }

        float now = Time.unscaledTime;
        if (_tapCount == 0 || now - _firstTapAt > TapWindowSeconds)
        {
            _firstTapAt = now;
            _tapCount = 0;
        }
        if (++_tapCount < TapsToOpen) return;
        _tapCount = 0;

        if (_popup == null) BuildPopup();
        _popup.transform.SetAsLastSibling();
        _popup.SetActive(true);
        _code.ActivateInputField();
    }

    private void BuildPopup()
    {
        var overlay = Box(_home, "DeveloperCoinPopup", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, .72f));
        _popup = overlay.gameObject;
        var card = Box(overlay, "Card", new Vector2(.10f, .45f), new Vector2(.90f, .76f), new Color32(33, 46, 35, 255));

        var title = Label(card, "Title", new Vector2(.08f, .78f), new Vector2(.92f, .95f), 44f);
        title.text = Loc.Get("developer_coins.title");
        title.fontStyle = FontStyles.Bold;
        Label(card, "Prompt", new Vector2(.09f, .64f), new Vector2(.91f, .78f), 31f).text = Loc.Get("developer_coins.prompt");

        var field = Box(card, "Code", new Vector2(.12f, .40f), new Vector2(.88f, .62f), new Color32(242, 240, 230, 255));
        var viewport = Rect(field, "Viewport", new Vector2(.05f, .08f), new Vector2(.95f, .92f));
        viewport.gameObject.AddComponent<RectMask2D>();
        var value = Label(viewport, "Value", Vector2.zero, Vector2.one, 52f);
        value.color = Ink;
        value.textWrappingMode = TextWrappingModes.NoWrap;

        _code = field.gameObject.AddComponent<TMP_InputField>();
        _code.targetGraphic = field.GetComponent<Image>();
        _code.textViewport = viewport;
        _code.textComponent = value;
        _code.contentType = TMP_InputField.ContentType.Pin;
        _code.characterLimit = 4;
        _code.asteriskChar = '*';
        _code.onSubmit.AddListener(_ => Claim());

        _error = Label(card, "Error", new Vector2(.08f, .24f), new Vector2(.92f, .39f), 27f);
        _error.color = new Color32(255, 190, 173, 255);

        MakeButton(card, "Cancel", new Vector2(.08f, .06f), new Vector2(.38f, .22f),
            new Color32(68, 80, 67, 255), Loc.Get("common.cancel"), Close);
        MakeButton(card, "Claim", new Vector2(.42f, .06f), new Vector2(.92f, .22f),
            new Color32(48, 122, 68, 255), Loc.Format("goal.claim", GameManager.DeveloperCoinAmount.ToString("N0")), Claim);
    }

    private void Claim()
    {
        if (!IsOpen || GameManager.Instance == null) return;
        if (!GameManager.Instance.TryClaimDeveloperCoins(_code.text))
        {
            _error.text = Loc.Get("developer_coins.invalid");
            _code.SetTextWithoutNotify("");
            return;
        }

        Close();
        _claimed();
    }

    public void Close()
    {
        _tapCount = 0;
        if (_popup == null) return;
        _code.DeactivateInputField();
        _code.SetTextWithoutNotify("");
        _error.text = "";
        _popup.SetActive(false);
    }

    private void MakeButton(RectTransform parent, string name, Vector2 min, Vector2 max,
                            Color color, string label, Action click)
    {
        var rt = Box(parent, name, min, max, color);
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = rt.GetComponent<Image>();
        button.onClick.AddListener(() => click());
        UIPressScale.Ensure(button);
        Label(rt, "Label", new Vector2(.04f, .08f), new Vector2(.96f, .92f), 33f).text = label;
    }

    private TMP_Text Label(RectTransform parent, string name, Vector2 min, Vector2 max, float size)
    {
        var label = Rect(parent, name, min, max).gameObject.AddComponent<TextMeshProUGUI>();
        if (_font != null) label.font = _font;
        label.enableAutoSizing = true;
        label.fontSizeMin = size * .7f;
        label.fontSizeMax = size;
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform Box(RectTransform parent, string name, Vector2 min, Vector2 max, Color color)
    {
        var rt = Rect(parent, name, min, max);
        rt.gameObject.AddComponent<Image>().color = color;
        return rt;
    }

    private static RectTransform Rect(RectTransform parent, string name, Vector2 min, Vector2 max)
    {
        var rt = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rt.SetParent(parent, false);
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }
}
