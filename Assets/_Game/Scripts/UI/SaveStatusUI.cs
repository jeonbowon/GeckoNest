using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Persistent, non-modal notice until pending progress reaches disk.</summary>
public sealed class SaveStatusUI : MonoBehaviour
{
    private static SaveStatusUI s_instance;
    private GameObject _panel;
    private TMP_Text _message;
    private TMP_Text _retryText;
    private RectTransform _safeRoot;

    public static void Create()
    {
        if (s_instance != null) return;
        var go = new GameObject("[SaveStatusUI]", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(go);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 31000;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 2400);
        scaler.matchWidthOrHeight = 0f;
        s_instance = go.AddComponent<SaveStatusUI>();
        s_instance.Build();
    }

    private void Build()
    {
        _safeRoot = new GameObject("SafeArea", typeof(RectTransform)).GetComponent<RectTransform>();
        _safeRoot.SetParent(transform, false);
        var panel = new GameObject("SaveWarning", typeof(RectTransform), typeof(Image));
        var rt = panel.GetComponent<RectTransform>();
        rt.SetParent(_safeRoot, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, -12);
        rt.sizeDelta = new Vector2(-32, 230);
        panel.GetComponent<Image>().color = new Color(0.22f, 0.10f, 0.06f, 0.98f);
        _message = Label(rt, "Message", new Vector2(0.025f, 0.08f), new Vector2(0.74f, 0.92f), 30);
        var retry = new GameObject("Retry", typeof(RectTransform), typeof(Image), typeof(Button));
        var retryRt = retry.GetComponent<RectTransform>();
        retryRt.SetParent(rt, false);
        retryRt.anchorMin = new Vector2(0.76f, 0.15f);
        retryRt.anchorMax = new Vector2(0.98f, 0.85f);
        retryRt.offsetMin = retryRt.offsetMax = Vector2.zero;
        retry.GetComponent<Image>().color = new Color(0.35f, 0.24f, 0.12f);
        var button = retry.GetComponent<Button>();
        button.targetGraphic = retry.GetComponent<Image>();
        button.onClick.AddListener(() =>
        {
            if (GameManager.Instance != null) GameManager.Instance.RetryPendingSave();
        });
        _retryText = Label(retryRt, "Text", new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f), 32);
        _panel = panel;
        _panel.SetActive(false);
    }

    private void Update()
    {
        bool pending = GameManager.Instance != null && GameManager.Instance.HasPendingSave;
        if (_panel.activeSelf != pending) _panel.SetActive(pending);
        if (!pending) return;
        var safe = Screen.safeArea;
        _safeRoot.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        _safeRoot.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        _safeRoot.offsetMin = _safeRoot.offsetMax = Vector2.zero;
        _message.text = Loc.Get("save.pending");
        _retryText.text = Loc.Get("save.retry");
    }

    private static TMP_Text Label(RectTransform parent, string name, Vector2 min, Vector2 max, float size)
    {
        var text = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        text.rectTransform.SetParent(parent, false);
        text.rectTransform.anchorMin = min;
        text.rectTransform.anchorMax = max;
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.enableAutoSizing = true;
        text.fontSizeMin = 26;
        text.fontSizeMax = size;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private void OnDestroy()
    {
        if (s_instance == this) s_instance = null;
    }
}
