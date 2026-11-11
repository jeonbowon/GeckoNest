using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Home-only, non-modal update notice. Hidden during gameplay popups and consent/ads.</summary>
public sealed class InAppUpdateUI : MonoBehaviour
{
    private HomeUIController _home;
    private GameObject _canvasObject, _panel, _blocker;
    private RectTransform _safe;
    private TMP_Text _message, _actionText, _laterText;
    private Button _action, _later;

    public void Initialize(HomeUIController home, TMP_FontAsset font)
    {
        _home = home;
        if (!InAppUpdateService.Supported) return;
        _canvasObject = new GameObject("UpdateNotice", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        Stretch(_canvasObject.GetComponent<RectTransform>(), transform, Vector2.zero, Vector2.one);
        var canvas = _canvasObject.GetComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 30000; // Save failure notice remains above this layer.
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scale = _canvasObject.GetComponent<CanvasScaler>();
        scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scale.referenceResolution = new Vector2(1080, 2400);
        scale.matchWidthOrHeight = 0;
        // Only block touches during the short save/install handoff, never during downloading.
        _blocker = new GameObject("InstallingInputBlocker", typeof(RectTransform), typeof(Image));
        Stretch(_blocker.GetComponent<RectTransform>(), _canvasObject.transform, Vector2.zero, Vector2.one);
        _blocker.GetComponent<Image>().color = new Color(0, 0, 0, 0.15f);
        _blocker.SetActive(false);
        _safe = new GameObject("SafeArea", typeof(RectTransform)).GetComponent<RectTransform>();
        Stretch(_safe, _canvasObject.transform, Vector2.zero, Vector2.one);
        _panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        var rect = _panel.GetComponent<RectTransform>();
        rect.SetParent(_safe, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.right;
        rect.pivot = new Vector2(0.5f, 0);
        rect.anchoredPosition = new Vector2(0, 350);
        rect.sizeDelta = new Vector2(-64, 260);
        _panel.GetComponent<Image>().color = new Color(0.12f, 0.19f, 0.09f, 0.93f);
        _message = Label(rect, font, "Message", new Vector2(0.03f, 0.47f), new Vector2(0.97f, 0.96f), 34);
        _action = MakeButton(rect, font, "Action", 0.04f, 0.64f, out _actionText);
        _later = MakeButton(rect, font, "Later", 0.67f, 0.96f, out _laterText);
        _action.onClick.AddListener(OnAction);
        _later.onClick.AddListener(() =>
        {
            if (InAppUpdateService.Instance != null) InAppUpdateService.Instance.Dismiss();
        });
        _panel.SetActive(false);
    }

    private void Update()
    {
        var service = InAppUpdateService.Instance;
        if (_canvasObject == null || service == null) return;
        bool homeReady = _home != null && _home.CanOfferAppUpdate;
        service.HomeAvailable = homeReady;
        bool installing = service.State == InAppUpdateService.UpdateState.Installing;
        _blocker.SetActive(installing);
        bool visible = installing || (homeReady && !service.Dismissed
            && service.State != InAppUpdateService.UpdateState.Hidden);
        _panel.SetActive(visible);
        if (!visible) return;
        var safe = Screen.safeArea;
        _safe.anchorMin = new Vector2(safe.xMin / Mathf.Max(1, Screen.width), safe.yMin / Mathf.Max(1, Screen.height));
        _safe.anchorMax = new Vector2(safe.xMax / Mathf.Max(1, Screen.width), safe.yMax / Mathf.Max(1, Screen.height));
        _laterText.text = Loc.Get("update.later");
        _later.interactable = !installing;
        _action.interactable = !service.Busy && !installing;
        switch (service.State)
        {
            case InAppUpdateService.UpdateState.Available:
                _message.text = Loc.Get("update.available");
                _actionText.text = Loc.Get("update.download");
                break;
            case InAppUpdateService.UpdateState.Downloading:
                _message.text = Loc.Format("update.downloading", Mathf.RoundToInt(service.DownloadProgress * 100));
                _actionText.text = Loc.Get("update.download");
                _action.interactable = false;
                break;
            case InAppUpdateService.UpdateState.Ready:
                _message.text = Loc.Get(service.FailureKey == "update.failed" ? "update.ready" : service.FailureKey);
                _actionText.text = Loc.Get("update.restart");
                break;
            case InAppUpdateService.UpdateState.Installing:
                _message.text = Loc.Get("update.installing");
                _actionText.text = Loc.Get("update.restart");
                break;
            default:
                _message.text = Loc.Get("update.failed");
                _actionText.text = Loc.Get("update.retry");
                break;
        }
    }

    private void OnAction()
    {
        var service = InAppUpdateService.Instance;
        if (service == null || _home == null || !_home.CanOfferAppUpdate) return;
        if (service.State == InAppUpdateService.UpdateState.Ready) service.Install();
        else service.Download();
    }

    private void OnDisable()
    {
        if (InAppUpdateService.Instance != null) InAppUpdateService.Instance.HomeAvailable = false;
        if (_panel != null) _panel.SetActive(false);
        if (_blocker != null) _blocker.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_canvasObject != null) Destroy(_canvasObject);
    }

    private static void Stretch(RectTransform rect, Transform parent, Vector2 min, Vector2 max)
    {
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static TMP_Text Label(RectTransform parent, TMP_FontAsset font, string name, Vector2 min, Vector2 max, float size)
    {
        var label = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        Stretch(label.rectTransform, parent, min, max);
        label.font = font != null ? font : TMP_Settings.defaultFontAsset;
        label.fontSize = size;
        label.enableAutoSizing = true;
        label.fontSizeMin = 28;
        label.fontSizeMax = size;
        label.color = new Color(0.97f, 0.95f, 0.85f);
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private static Button MakeButton(RectTransform parent, TMP_FontAsset font, string name, float min, float max, out TMP_Text label)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        var rect = go.GetComponent<RectTransform>();
        Stretch(rect, parent, new Vector2(min, 0.08f), new Vector2(max, 0.43f));
        go.GetComponent<Image>().color = new Color(0.32f, 0.43f, 0.23f, 0.9f);
        var button = go.GetComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        label = Label(rect, font, "Label", new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.97f), 36);
        return button;
    }
}
