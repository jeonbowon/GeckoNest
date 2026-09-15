using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게코 머리 위 말풍선 — "배불러요", "그만 만져~" 같은 짧은 한마디.
/// 톡 튀어나와 잠깐 머물다 쏙 들어간다. 게코가 움직이면 따라간다.
///
/// 글꼴은 홈 화면의 기존 TMP 글꼴을 받아 쓴다 (기본 TMP 글꼴에는 한글이 없다).
/// 특수문자(★ ♥ → …)는 글꼴에 없으므로 쓰지 않는다 — 한글·영문·숫자·기본 기호만.
/// </summary>
[DisallowMultipleComponent]
public class SpeechBubble : MonoBehaviour
{
    private const float PAD_X    = 34f;
    private const float PAD_Y    = 20f;
    private const float FONT     = 40f;
    private const float MAX_W    = 640f;    // 성장 조건 줄("Affection 60 - Need (now 52)")이 줄바꿈되지 않는 폭
    private const float MIN_W    = 120f;
    private const float LIFT     = 30f;     // 머리 위로 띄우는 거리
    private const float EDGE     = 16f;     // 화면 가장자리 여백
    private const float POP_IN   = 0.22f;
    private const float POP_OUT  = 0.16f;
    private const float DEFAULT_HOLD = 1.5f;

    private static readonly Color TEXT_COLOR = new Color(0.357f, 0.227f, 0.161f);   // #5B3A29 따뜻한 갈색

    private RectTransform    _rt;
    private RectTransform    _layer;
    private CanvasGroup      _group;
    private TextMeshProUGUI  _text;
    private Func<Vector3>    _anchor;
    private float _t, _hold;
    private bool  _showing;

    public static SpeechBubble Create(RectTransform layer, TMP_FontAsset font)
    {
        var go = new GameObject("SpeechBubble", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        go.transform.SetParent(layer, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0f);

        var bg = go.GetComponent<Image>();
        bg.sprite = FxSprites.Bubble;
        bg.type   = Image.Type.Sliced;
        bg.raycastTarget = false;

        // 꼬리 — 말풍선 아래 가운데
        var tailGo = new GameObject("Tail", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        tailGo.transform.SetParent(rt, false);
        var tailRt = (RectTransform)tailGo.transform;
        tailRt.anchorMin = tailRt.anchorMax = new Vector2(0.5f, 0f);
        tailRt.pivot = new Vector2(0.5f, 1f);
        tailRt.anchoredPosition = new Vector2(0f, 5f);
        tailRt.sizeDelta = new Vector2(34f, 28f);
        var tail = tailGo.GetComponent<Image>();
        tail.sprite = FxSprites.BubbleTail;
        tail.raycastTarget = false;

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(rt, false);
        var textRt = (RectTransform)textGo.transform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(PAD_X, PAD_Y);
        textRt.offsetMax = new Vector2(-PAD_X, -PAD_Y);
        var text = textGo.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize      = FONT;
        text.color         = TEXT_COLOR;
        text.alignment     = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        var bubble = go.AddComponent<SpeechBubble>();
        bubble._rt    = rt;
        bubble._layer = layer;
        bubble._group = go.GetComponent<CanvasGroup>();
        bubble._group.blocksRaycasts = false;
        bubble._group.interactable   = false;
        bubble._text  = text;
        go.SetActive(false);
        return bubble;
    }

    /// <summary>worldAnchor = 말풍선 꼬리가 가리킬 곳 (보통 게코 머리 위). 매 프레임 다시 읽는다.</summary>
    public void Show(string message, Func<Vector3> worldAnchor, float hold = DEFAULT_HOLD)
    {
        if (string.IsNullOrEmpty(message)) return;

        _text.text = message;
        Vector2 pref = _text.GetPreferredValues(message, MAX_W - PAD_X * 2f, 0f);
        float w = Mathf.Clamp(pref.x + PAD_X * 2f, MIN_W, MAX_W);
        float h = pref.y + PAD_Y * 2f;
        _rt.sizeDelta = new Vector2(w, h);

        _anchor  = worldAnchor;
        _hold    = hold;
        _t       = 0f;
        _showing = true;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        Follow();
        Apply(0f);
    }


    private void Update()
    {
        if (!_showing) return;
        _t += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        Follow();

        float end = POP_IN + _hold + POP_OUT;
        if (_t >= end)
        {
            _showing = false;
            gameObject.SetActive(false);
            return;
        }
        Apply(_t);
    }

    private void Apply(float t)
    {
        float scale, alpha;
        if (t < POP_IN)
        {
            float u = t / POP_IN;
            scale = EaseOutBack(u);
            alpha = Mathf.Clamp01(u * 2f);
        }
        else if (t < POP_IN + _hold)
        {
            scale = 1f;
            alpha = 1f;
        }
        else
        {
            float u = (t - POP_IN - _hold) / POP_OUT;
            scale = Mathf.Lerp(1f, 0.7f, u);
            alpha = 1f - u;
        }
        _rt.localScale = new Vector3(scale, scale, 1f);
        _group.alpha   = alpha;
    }

    private void Follow()
    {
        if (_anchor == null || _layer == null) return;
        Vector2 p = _layer.InverseTransformPoint(_anchor());
        p.y += LIFT;

        // 화면 밖으로 나가지 않게 (가로)
        Rect r = _layer.rect;
        float half = _rt.sizeDelta.x * 0.5f;
        float minX = r.xMin + half + EDGE, maxX = r.xMax - half - EDGE;
        if (minX < maxX) p.x = Mathf.Clamp(p.x, minX, maxX);

        // 로컬 좌표는 레이어 피벗 기준 → 앵커(가운데) 기준으로 맞춘다
        Vector2 pivotOffset = new Vector2((0.5f - _layer.pivot.x) * r.width, (0.5f - _layer.pivot.y) * r.height);
        _rt.anchoredPosition = p - pivotOffset;
    }

    private static float EaseOutBack(float x)
    {
        const float C1 = 1.9f, C3 = C1 + 1f;
        float t = x - 1f;
        return 1f + C3 * t * t * t + C1 * t * t;
    }
}
