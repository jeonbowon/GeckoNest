using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 홈 화면 분위기 연출 (2026-09-18) — 그림 없이 코드로만 만든다. `HomeUIController.EnsureAtmosphere`가 켠다.
///
///   비네트   — 화면 가장자리를 은은하게 어둡게 (테마 그림 위, 장식·게코 아래)
///   먼지     — 공기 중에 천천히 떠다니는 작은 빛 입자 (게코 앞, 누르기 안 받음)
///
/// 앞 잎사귀(아래 양쪽 모서리의 어두운 잎 실루엣)는 2026-09-21에 뺐다 — 대부분 돌봄 버튼·하단 탭에 가려
/// 끝만 삐져나왔고, 테마 흙 바닥이 밝아지자 잎이 아니라 검은 얼룩으로 보였다.
///
/// 모두 raycastTarget을 끄므로 입력에는 영향이 없다. 그림으로 바꾸려면 `Resources/Fx/vignette`.
/// </summary>
[DisallowMultipleComponent]
public class TerrariumAtmosphere : MonoBehaviour
{
    // ── 수치 [TBD] ───────────────────────────────────────────
    private const int   DUST_COUNT   = 14;
    private const float DUST_MIN     = 5f,   DUST_MAX   = 13f;    // 입자 크기 (UI)
    private const float DUST_RISE    = 9f,   DUST_RISE2 = 26f;    // 올라가는 속도 (UI/초)
    private const float DUST_SWAY    = 26f;                        // 좌우 흔들림 폭
    private const float DUST_ALPHA   = 0.30f;

    private static readonly Color VIGNETTE_COLOR = new Color(0.05f, 0.04f, 0.02f, 0.38f);
    private static readonly Color DUST_COLOR     = new Color(1f, 0.97f, 0.85f);

    private struct Dust
    {
        public RectTransform rt;
        public float rise, phase, sway, size;
    }

    private RectTransform _area;
    private Dust[]        _dust;
    private Image         _vignette;

    /// <summary>area = GeckoArea. vignetteSibling = 비네트를 끼울 순서 (장식·게코 무리 바로 앞)</summary>
    public static TerrariumAtmosphere Create(RectTransform area, int vignetteSibling)
    {
        if (area == null) return null;

        var go = new GameObject("Atmosphere", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(area, false);
        Stretch(rt);
        rt.SetAsLastSibling();   // 먼지는 게코보다 앞

        var fx = go.AddComponent<TerrariumAtmosphere>();
        fx._area = area;
        fx.BuildVignette(area, vignetteSibling);
        fx.BuildDust(rt);
        return fx;
    }

    public void SetVisible(bool on)
    {
        gameObject.SetActive(on);
        if (_vignette != null) _vignette.gameObject.SetActive(on);
    }

    // ── 만들기 ────────────────────────────────────────────────

    private void BuildVignette(RectTransform area, int sibling)
    {
        var go = new GameObject("Vignette", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(area, false);
        Stretch(rt);
        rt.SetSiblingIndex(Mathf.Clamp(sibling, 0, area.childCount - 1));

        _vignette = go.GetComponent<Image>();
        _vignette.sprite        = FxSprites.Vignette;
        _vignette.color         = VIGNETTE_COLOR;
        _vignette.raycastTarget = false;
    }

    private void BuildDust(RectTransform root)
    {
        _dust = new Dust[DUST_COUNT];
        for (int i = 0; i < DUST_COUNT; i++)
        {
            var go = new GameObject("Dust", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(root, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot     = new Vector2(0.5f, 0.5f);

            var img = go.GetComponent<Image>();
            img.sprite        = FxSprites.Dot;
            img.raycastTarget = false;

            float size = Random.Range(DUST_MIN, DUST_MAX);
            rt.sizeDelta = new Vector2(size, size);
            _dust[i] = new Dust
            {
                rt    = rt,
                rise  = Random.Range(DUST_RISE, DUST_RISE2),
                phase = Random.Range(0f, 10f),
                sway  = Random.Range(0.5f, 1f) * DUST_SWAY,
                size  = size,
            };
            PlaceDust(ref _dust[i], Random.Range(0f, 1f));
        }
    }

    // ── 움직임 ────────────────────────────────────────────────

    private void Update()
    {
        if (_area == null) return;
        float dt = Time.deltaTime;
        float t  = Time.time;
        float h  = _area.rect.height;

        for (int i = 0; i < _dust.Length; i++)
        {
            ref var d = ref _dust[i];
            if (d.rt == null) continue;

            var p = d.rt.anchoredPosition;
            p.y += d.rise * dt;
            p.x += Mathf.Sin(t * 0.7f + d.phase) * d.sway * dt;
            if (p.y > h) { PlaceDust(ref d, 0f); continue; }         // 위로 나가면 아래에서 다시
            d.rt.anchoredPosition = p;

            // 위아래 끝에서 옅어진다
            float fade = Mathf.Clamp01(p.y / 120f) * Mathf.Clamp01((h - p.y) / 200f);
            var img = d.rt.GetComponent<Image>();
            if (img != null) img.color = new Color(DUST_COLOR.r, DUST_COLOR.g, DUST_COLOR.b, DUST_ALPHA * fade);
        }
    }

    private void PlaceDust(ref Dust d, float startHeight01)
    {
        float w = _area != null ? _area.rect.width  : 1080f;
        float h = _area != null ? _area.rect.height : 1600f;
        d.rt.anchoredPosition = new Vector2(Random.Range(-w * 0.5f, w * 0.5f), h * startHeight01);
        d.rt.sizeDelta        = new Vector2(d.size, d.size);
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
