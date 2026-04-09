using UnityEngine;

/// <summary>
/// 테라리움 내 오브젝트에 붙여 Y 좌표 기반 원근감(스케일/알파/정렬)을 적용.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class DepthObject : MonoBehaviour
{
    [SerializeField] private DepthScaleConfig _config;

    private SpriteRenderer _sr;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (_config == null) return;

        float range = _config.bottomY - _config.topY;
        if (Mathf.Approximately(range, 0f)) return;

        float normalizedDepth = Mathf.Clamp01(
            (transform.position.y - _config.topY) / range
        );

        // 스케일 — topY(0)가 작고 bottomY(1)가 크다
        float scale = Mathf.Lerp(_config.minScale, _config.maxScale, normalizedDepth);
        transform.localScale = Vector3.one * scale;

        // 알파
        Color c = _sr.color;
        c.a     = Mathf.Lerp(_config.minAlpha, _config.maxAlpha, normalizedDepth);
        _sr.color = c;

        // 소팅 오더 — 아래로 갈수록 앞으로
        _sr.sortingOrder = Mathf.RoundToInt(normalizedDepth * 100f);
    }
}
