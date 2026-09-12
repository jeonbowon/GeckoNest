using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI(Canvas) 위에 뿌리는 가벼운 파티클 — 하트, 물방울, 반짝이, 허물 조각 등.
/// 이 컴포넌트가 붙은 RectTransform의 로컬 좌표(가운데 0,0)로 뿌린다.
///
/// 파티클마다 코루틴을 돌리지 않고 Update 하나에서 전부 움직인다. Image는 재사용한다.
/// 모양: 튀어나오듯 커졌다가(오버슈트) 수명 끝에 줄어들며 사라진다.
/// </summary>
[DisallowMultipleComponent]
public class UIParticles : MonoBehaviour
{
    private const int MAX_PARTICLES = 160;

    /// <summary>한 번에 뿌리는 설정</summary>
    public struct Burst
    {
        public Sprite  sprite;
        public Color   color;
        public Color   colorB;       // color~colorB 사이 무작위 (같으면 한 색)
        public int     count;
        public Vector2 speed;        // 최소·최대 (UI 단위/초)
        public float   angle;        // 뿌리는 방향 (도, 0=오른쪽, 90=위)
        public float   spread;       // 방향 흩어짐 (±도)
        public Vector2 size;         // 최소·최대 (UI 단위)
        public Vector2 life;         // 최소·최대 (초)
        public float   gravity;      // 아래로 당기는 힘 (음수면 떠오름)
        public float   drag;         // 공기 저항 (초당 감속 비율)
        public float   spin;         // 회전 속도 최대 (도/초)
        public float   growTo;       // 수명 끝 크기 배율 (0이면 1 — 크기 유지)
        public Vector2 area;         // 뿌리는 위치 흩어짐 (가로·세로 반폭)
        public float   delay;        // 파티클마다 0~delay초 늦게 등장
        public float   sway;         // 좌우 흔들림 크기 (UI 단위) — 하트가 둥실둥실
    }

    private class P
    {
        public RectTransform rt;
        public Image  img;
        public bool   active;
        public Vector2 pos, vel;
        public float  age, life, size, rot, spin, gravity, drag, growTo, delay, sway, swayPhase;
        public Color  color;
    }

    private readonly List<P> _pool = new List<P>();
    private RectTransform _rt;

    private void Awake()
    {
        _rt = (RectTransform)transform;
    }

    /// <summary>월드 좌표 → 이 레이어의 로컬 좌표</summary>
    public Vector2 ToLocal(Vector3 world)
    {
        if (_rt == null) _rt = (RectTransform)transform;
        return _rt.InverseTransformPoint(world);
    }

    public void Emit(Vector2 localPos, Burst b)
    {
        if (b.sprite == null || b.count <= 0) return;

        for (int i = 0; i < b.count; i++)
        {
            var p = Take();
            if (p == null) return;

            float ang = (b.angle + Random.Range(-b.spread, b.spread)) * Mathf.Deg2Rad;
            float spd = Random.Range(b.speed.x, b.speed.y);

            p.pos       = localPos + new Vector2(Random.Range(-b.area.x, b.area.x), Random.Range(-b.area.y, b.area.y));
            p.vel       = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd;
            p.age       = 0f;
            p.life      = Mathf.Max(0.05f, Random.Range(b.life.x, b.life.y));
            p.size      = Random.Range(b.size.x, b.size.y);
            p.rot       = Random.Range(-15f, 15f);
            p.spin      = Random.Range(-b.spin, b.spin);
            p.gravity   = b.gravity;
            p.drag      = b.drag;
            p.growTo    = b.growTo > 0f ? b.growTo : 1f;
            p.delay     = b.delay > 0f ? Random.Range(0f, b.delay) : 0f;
            p.sway      = b.sway;
            p.swayPhase = Random.value * Mathf.PI * 2f;
            p.color     = Color.Lerp(b.color, b.colorB, Random.value);

            p.img.sprite = b.sprite;
            p.img.color  = new Color(p.color.r, p.color.g, p.color.b, 0f);
            p.rt.sizeDelta = new Vector2(p.size, p.size);
            p.rt.localScale = Vector3.zero;
            p.rt.SetAsLastSibling();
            p.img.enabled = true;
            p.active = true;
        }
    }

    private P Take()
    {
        for (int i = 0; i < _pool.Count; i++)
            if (!_pool[i].active) return _pool[i];
        if (_pool.Count >= MAX_PARTICLES) return null;

        var go = new GameObject("p", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(transform, false);
        var p = new P
        {
            rt  = (RectTransform)go.transform,
            img = go.GetComponent<Image>(),
        };
        p.img.raycastTarget = false;
        p.rt.anchorMin = p.rt.anchorMax = new Vector2(0.5f, 0.5f);
        p.rt.pivot = new Vector2(0.5f, 0.5f);
        _pool.Add(p);
        return p;
    }

    private void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        for (int i = 0; i < _pool.Count; i++)
        {
            var p = _pool[i];
            if (!p.active) continue;

            if (p.delay > 0f)
            {
                p.delay -= dt;
                continue;
            }

            p.age += dt;
            if (p.age >= p.life)
            {
                p.active = false;
                p.img.enabled = false;
                continue;
            }

            p.vel.y -= p.gravity * dt;
            p.vel   *= Mathf.Max(0f, 1f - p.drag * dt);
            p.pos   += p.vel * dt;
            p.rot   += p.spin * dt;

            float u = p.age / p.life;

            // 등장: 0.15초 동안 1.25배까지 튀어나왔다가 1로 / 퇴장: 마지막 30% 동안 줄어듦
            float pop  = Mathf.Clamp01(p.age / 0.15f);
            float scale = pop < 1f ? EaseOutBack(pop) : 1f;
            scale *= Mathf.Lerp(1f, p.growTo, u);
            if (u > 0.7f) scale *= 1f - (u - 0.7f) / 0.3f * 0.6f;

            float alpha = p.color.a * Mathf.Clamp01(p.age / 0.06f) * (u > 0.65f ? 1f - (u - 0.65f) / 0.35f : 1f);

            float swayX = p.sway > 0f ? Mathf.Sin(p.age * 5f + p.swayPhase) * p.sway : 0f;
            p.rt.localPosition = new Vector3(p.pos.x + swayX, p.pos.y, 0f);
            p.rt.localRotation = Quaternion.Euler(0f, 0f, p.rot);
            p.rt.localScale    = new Vector3(scale, scale, 1f);
            p.img.color        = new Color(p.color.r, p.color.g, p.color.b, alpha);
        }
    }

    private static float EaseOutBack(float x)
    {
        const float C1 = 1.9f, C3 = C1 + 1f;
        float t = x - 1f;
        return 1f + C3 * t * t * t + C1 * t * t;
    }
}
