using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게코 파츠 14개를 관절 구조로 조립해 화면에 그린다 (UI 방식).
///
/// - 파츠는 Visual 아래에 그리는 순서대로 나란히 놓인다 (uGUI는 계층 순서 = 그리는 순서).
/// - 관절 부모-자식 관계는 GeckoParts.ParentOf로 코드에서 계산한다.
///   그래서 "꼬리는 몸통 뒤에 그리지만 몸통을 따라 움직인다"가 가능하다.
/// - Visual 내부 좌표 = 스킨 픽셀. 화면 크기는 Visual 배율 하나로 맞춘다.
///
/// 그림 교체: _skin(또는 단계별 스킨)만 바꾸면 된다. 레이어 이름이 같으면 제자리에 붙는다.
/// </summary>
[DisallowMultipleComponent]
public class GeckoRig : MonoBehaviour
{
    private const string VISUAL_NAME = "Visual";
    private const float  TURN_TIME   = 0.18f;   // 좌우 돌아서기 시간
    private const float  GROW_SPEED  = 1.6f;    // 성장 시 크기 변화 속도 (/초)

    [Header("그림")]
    [SerializeField] private GeckoSkin _skin;
    [Tooltip("성장 단계별 전용 그림 (선택). 비어 있는 칸은 위 기본 그림을 쓴다. 0=Hatchling … 4=Adult")]
    [SerializeField] private GeckoSkin[] _stageSkins = new GeckoSkin[5];

    [Header("화면 크기")]
    [Tooltip("다 자란(Adult) 게코의 화면 폭. 꼬리 끝~주둥이 끝, UI 단위(가로 1080 기준)")]
    [SerializeField] private float _adultWidth = 640f;   // [TBD]
    [Tooltip("성장 단계별 크기 배율 — 아트 발주서 기준")]
    [SerializeField] private float[] _stageScales = { 0.55f, 0.68f, 0.80f, 0.90f, 1.00f };

    [Header("파츠 (자동 생성 — 직접 수정하지 마십시오)")]
    [SerializeField] private RectTransform _visual;
    [SerializeField] private Graphic[] _graphics = new Graphic[GeckoParts.Count];

    // ── 스킨에서 읽은 기본값 ─────────────────────────────────
    private GeckoSkin _activeSkin;
    private readonly Vector2[] _restPos   = new Vector2[GeckoParts.Count];
    private readonly Vector2[] _restScale = new Vector2[GeckoParts.Count];
    private readonly Vector2[] _restSize  = new Vector2[GeckoParts.Count];
    private readonly Vector2[] _pivot     = new Vector2[GeckoParts.Count];
    private readonly Color[]   _tint      = new Color[GeckoParts.Count];
    private RectTransform[] _rects;
    private float _minX, _maxX;   // 그림 좌우 범위 (스킨 픽셀)

    // ── 계산 버퍼 ────────────────────────────────────────────
    private readonly Vector2[] _worldPos   = new Vector2[GeckoParts.Count];
    private readonly float[]   _worldAngle = new float[GeckoParts.Count];
    private Sprite _faceEyeL, _faceEyeR, _faceMouth;

    // ── 상태 ─────────────────────────────────────────────────
    private int   _stage        = 4;
    private float _stageScale   = 1f;
    private float _stageTarget  = 1f;
    private bool  _facingRight  = true;
    private float _facing       = 1f;    // -1 ~ 1, 돌아서는 중에는 그 사이 값
    private float _depthScale   = 1f;

    public GeckoSkin     Skin         => _activeSkin != null ? _activeSkin : _skin;
    public RectTransform Visual       => _visual;
    public bool          FacingRight  => _facingRight;
    public bool          IsTurning    => Mathf.Abs(_facing) < 0.999f;
    public float         StageScale   => _stageScale;
    public int           GrowthStage  => _stage;

    public float DepthScale
    {
        get => _depthScale;
        set => _depthScale = Mathf.Max(0.1f, value);
    }

    /// <summary>스킨 1픽셀이 화면 UI 몇 단위인가 (지금 크기 기준)</summary>
    public float UIPerSkinPixel
    {
        get
        {
            var s = Skin;
            float refW = s != null ? Mathf.Max(1f, s.referenceWidth) : 1000f;
            return _adultWidth / refW * _stageScale * _depthScale;
        }
    }

    public int TailSegments
    {
        get
        {
            var g = _graphics != null && _graphics.Length > (int)GeckoPartId.Tail
                ? _graphics[(int)GeckoPartId.Tail] as GeckoBendGraphic : null;
            return g != null ? g.Segments : 12;
        }
    }

    // ── 생명주기 ──────────────────────────────────────────────

    private void Awake()
    {
        EnsureParts();
        ApplySkin();
        SolveRest();
    }

    // ── 공개 API ──────────────────────────────────────────────

    public void SetSkin(GeckoSkin skin)
    {
        _skin = skin;
        ApplySkin();
    }

    public void SetFacing(bool right) => _facingRight = right;

    public void SetGrowthStage(int stage, bool immediate)
    {
        stage = Mathf.Clamp(stage, 0, 4);
        bool skinChanged = ResolveSkin() != ResolveSkin(stage);
        _stage = stage;
        _stageTarget = StageScaleOf(stage);
        if (immediate) _stageScale = _stageTarget;
        if (skinChanged) ApplySkin();
    }

    public Vector2 RestPosition(GeckoPartId id) => _restPos[(int)id];
    public Vector2 RestSize(GeckoPartId id)     => _restSize[(int)id];
    public Vector2 RestPivot(GeckoPartId id)    => _pivot[(int)id];
    public Vector2 RestScale(GeckoPartId id)    => _restScale[(int)id];

    /// <summary>관절에서 스프라이트 오른쪽 끝까지 길이 (혀처럼 +x로 뻗는 파츠용)</summary>
    public float RestLengthForward(GeckoPartId id)
    {
        int i = (int)id;
        return _restSize[i].x * (1f - _pivot[i].x) * Mathf.Abs(_restScale[i].x);
    }

    /// <summary>관절에서 스프라이트 아래 끝까지 길이 (다리용)</summary>
    public float RestLengthDown(GeckoPartId id)
    {
        int i = (int)id;
        return _restSize[i].y * _pivot[i].y * Mathf.Abs(_restScale[i].y);
    }

    /// <summary>발밑 기준 좌우로 그림이 차지하는 폭 (UI 단위, 지금 방향·크기 기준)</summary>
    public void GetExtents(out float left, out float right)
    {
        float k = UIPerSkinPixel;
        float l = -_minX * k, r = _maxX * k;
        if (_facingRight) { left = l; right = r; }
        else              { left = r; right = l; }
    }

    // ── 파츠 생성 ────────────────────────────────────────────

    /// <summary>Visual과 파츠 14개가 없으면 만든다. 이미 있으면 순서만 정리한다.</summary>
    public void EnsureParts()
    {
        if (_visual == null)
        {
            var found = transform.Find(VISUAL_NAME) as RectTransform;
            if (found == null)
            {
                var go = new GameObject(VISUAL_NAME, typeof(RectTransform));
                found = (RectTransform)go.transform;
                found.SetParent(transform, false);
            }
            _visual = found;
        }

        _visual.anchorMin = _visual.anchorMax = new Vector2(0.5f, 0f);
        _visual.pivot = new Vector2(0.5f, 0.5f);
        _visual.anchoredPosition = Vector2.zero;
        _visual.sizeDelta = Vector2.zero;
        _visual.localRotation = Quaternion.identity;

        if (_graphics == null || _graphics.Length != GeckoParts.Count)
        {
            var old = _graphics;
            _graphics = new Graphic[GeckoParts.Count];
            if (old != null)
                for (int i = 0; i < Mathf.Min(old.Length, _graphics.Length); i++) _graphics[i] = old[i];
        }

        for (int i = 0; i < GeckoParts.Count; i++)
        {
            var id = (GeckoPartId)i;
            string partName = GeckoParts.LayerName(id);
            bool wantBend = id == GeckoPartId.Tail;

            Graphic g = _graphics[i];
            if (g == null)
            {
                var child = _visual.Find(partName);
                if (child != null) g = child.GetComponent<Graphic>();
            }

            // 종류가 틀리면 (꼬리는 GeckoBendGraphic, 나머지는 Image) 바꿔 끼운다
            if (g != null && wantBend != (g is GeckoBendGraphic))
            {
                var go = g.gameObject;
                DestroyComponent(g);
                g = wantBend ? (Graphic)go.AddComponent<GeckoBendGraphic>() : go.AddComponent<Image>();
            }

            if (g == null)
            {
                var go = new GameObject(partName, typeof(RectTransform), typeof(CanvasRenderer));
                go.transform.SetParent(_visual, false);
                g = wantBend ? (Graphic)go.AddComponent<GeckoBendGraphic>() : go.AddComponent<Image>();
            }

            g.raycastTarget = false;
            var rt = g.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            g.transform.SetSiblingIndex(i);
            _graphics[i] = g;
        }

        CacheRects();
    }

    private void CacheRects()
    {
        if (_rects == null || _rects.Length != GeckoParts.Count) _rects = new RectTransform[GeckoParts.Count];
        for (int i = 0; i < GeckoParts.Count; i++)
            _rects[i] = _graphics[i] != null ? _graphics[i].rectTransform : null;
    }

    private static void DestroyComponent(Object c)
    {
        if (Application.isPlaying) Destroy(c);
        else DestroyImmediate(c);
    }

    // ── 스킨 적용 ────────────────────────────────────────────

    public void ApplySkin()
    {
        if (_rects == null || _visual == null) EnsureParts();

        _activeSkin = ResolveSkin();
        _faceEyeL = _faceEyeR = _faceMouth = null;
        _minX = -500f;
        _maxX = 500f;

        if (_activeSkin == null)
        {
            for (int i = 0; i < GeckoParts.Count; i++)
                if (_graphics[i] != null) _graphics[i].enabled = false;
            return;
        }

        bool anyBounds = false;
        float minX = float.MaxValue, maxX = float.MinValue;

        for (int i = 0; i < GeckoParts.Count; i++)
        {
            var id  = (GeckoPartId)i;
            var art = _activeSkin.GetPart(id);

            Sprite sp = art != null ? art.sprite : null;
            if (id == GeckoPartId.EyeL)  sp = _activeSkin.GetEye(GeckoEye.Open, false);
            if (id == GeckoPartId.EyeR)  sp = _activeSkin.GetEye(GeckoEye.Open, true);
            if (id == GeckoPartId.Mouth) sp = _activeSkin.GetMouth(GeckoMouth.Closed);

            _restPos[i]   = art != null ? art.jointPosition : Vector2.zero;
            _restScale[i] = art != null ? art.scale : Vector2.one;
            _pivot[i]     = art != null ? art.jointPivot : new Vector2(0.5f, 0.5f);
            _tint[i]      = art != null ? art.tint : Color.white;
            _restSize[i]  = sp != null ? sp.rect.size : Vector2.zero;

            var g = _graphics[i];
            if (g == null) continue;
            AssignSprite(g, sp);
            var rt = _rects[i];
            rt.pivot     = _pivot[i];
            rt.sizeDelta = _restSize[i];

            if (sp != null && id != GeckoPartId.Shadow)
            {
                float w  = _restSize[i].x * Mathf.Abs(_restScale[i].x);
                float lx = _restPos[i].x - _pivot[i].x * w;
                minX = Mathf.Min(minX, lx);
                maxX = Mathf.Max(maxX, lx + w);
                anyBounds = true;
            }
        }

        if (anyBounds)
        {
            _minX = minX;
            _maxX = maxX;
        }

        _faceEyeL  = _activeSkin.GetEye(GeckoEye.Open, false);
        _faceEyeR  = _activeSkin.GetEye(GeckoEye.Open, true);
        _faceMouth = _activeSkin.GetMouth(GeckoMouth.Closed);
    }

    private GeckoSkin ResolveSkin() => ResolveSkin(_stage);

    private GeckoSkin ResolveSkin(int stage)
    {
        if (_stageSkins != null && stage >= 0 && stage < _stageSkins.Length && _stageSkins[stage] != null)
            return _stageSkins[stage];
        return _skin;
    }

    private float StageScaleOf(int stage)
    {
        if (_stageScales == null || _stageScales.Length == 0) return 1f;
        return _stageScales[Mathf.Clamp(stage, 0, _stageScales.Length - 1)];
    }

    private static void AssignSprite(Graphic g, Sprite sp)
    {
        if (g is Image img)
        {
            if (img.sprite != sp) img.sprite = sp;
        }
        else if (g is GeckoBendGraphic bend)
        {
            bend.sprite = sp;
        }
    }

    // ── 자세 반영 ────────────────────────────────────────────

    /// <summary>쉬는 자세로 즉시 배치 (에디터 미리보기·시작 시)</summary>
    public void SolveRest()
    {
        var pose = new GeckoPose(TailSegments);
        pose[GeckoPartId.Tongue1].scale = new Vector2(0f, 1f);
        pose[GeckoPartId.Tongue2].scale = new Vector2(0f, 1f);
        pose[GeckoPartId.ShedPatch].alpha = 0f;

        _stageScale = _stageTarget = StageScaleOf(_stage);
        _facing = _facingRight ? 1f : -1f;
        Solve(pose, 0f);
    }

    /// <summary>GeckoPose를 화면에 반영한다. 관절 부모 → 자식 순서로 계산 (정기구학).</summary>
    public void Solve(GeckoPose pose, float dt)
    {
        if (_activeSkin == null || _rects == null || pose == null) return;

        // 1) 관절 계산
        for (int n = 0; n < GeckoParts.SolveOrder.Length; n++)
        {
            var id = GeckoParts.SolveOrder[n];
            int i  = (int)id;
            ref var pp = ref pose.parts[i];
            int parent = GeckoParts.ParentOf(id);

            if (parent < 0)
            {
                _worldPos[i]   = _restPos[i] + pp.offset;
                _worldAngle[i] = pp.angle;
            }
            else
            {
                // 부모의 순간 크기(호흡 등)만 자식 위치에 반영한다. 기본 배율은 이미 기본 위치에 들어 있다.
                Vector2 d  = _restPos[i] - _restPos[parent] + pp.offset;
                Vector2 ps = pose.parts[parent].scale;
                d = new Vector2(d.x * ps.x, d.y * ps.y);
                _worldPos[i]   = _worldPos[parent] + Rotate(d, _worldAngle[parent]);
                _worldAngle[i] = _worldAngle[parent] + pp.angle;
            }

            var rt = _rects[i];
            var g  = _graphics[i];
            if (rt == null || g == null) continue;

            Vector2 sc = Vector2.Scale(_restScale[i], pp.scale);
            rt.anchoredPosition = _worldPos[i];
            rt.localRotation    = Quaternion.Euler(0f, 0f, _worldAngle[i]);
            rt.localScale       = new Vector3(sc.x, sc.y, 1f);

            bool visible = pp.alpha > 0.002f && Mathf.Abs(sc.x) > 0.002f && Mathf.Abs(sc.y) > 0.002f
                           && _restSize[i].x > 0f;
            if (g.enabled != visible) g.enabled = visible;
            if (visible)
            {
                Color c = _tint[i];
                c.a *= Mathf.Clamp01(pp.alpha);
                if (g.color != c) g.color = c;
            }
        }

        // 2) 표정
        SetFace(GeckoPartId.EyeL,  ref _faceEyeL,  _activeSkin.GetEye(pose.eyeL, false));
        SetFace(GeckoPartId.EyeR,  ref _faceEyeR,  _activeSkin.GetEye(pose.eyeR, true));
        SetFace(GeckoPartId.Mouth, ref _faceMouth, _activeSkin.GetMouth(pose.mouth));

        // 3) 꼬리 굽힘
        if (_graphics[(int)GeckoPartId.Tail] is GeckoBendGraphic bend) bend.SetBend(pose.tailBend);

        // 4) 전체 크기 · 방향
        if (dt > 0f)
        {
            _stageScale = Mathf.MoveTowards(_stageScale, _stageTarget, GROW_SPEED * dt * Mathf.Max(0.2f, _stageTarget));
            _facing     = Mathf.MoveTowards(_facing, _facingRight ? 1f : -1f, dt * 2f / TURN_TIME);
        }

        var skin = _activeSkin;
        float px = _adultWidth / Mathf.Max(1f, skin.referenceWidth);
        float s  = px * _stageScale * _depthScale * pose.rootScale;
        _visual.localScale = new Vector3(s * _facing, s, 1f);
    }

    private void SetFace(GeckoPartId id, ref Sprite current, Sprite next)
    {
        if (current == next) return;
        current = next;

        int i = (int)id;
        var g = _graphics[i];
        if (g == null) return;
        AssignSprite(g, next);
        _rects[i].sizeDelta = next != null ? next.rect.size : Vector2.zero;
    }

    private static Vector2 Rotate(Vector2 v, float deg)
    {
        float r = deg * Mathf.Deg2Rad;
        float c = Mathf.Cos(r), s = Mathf.Sin(r);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }

#if UNITY_EDITOR
    // ── 에디터 전용 ──────────────────────────────────────────

    /// <summary>
    /// Scene 뷰에서 Rect 도구(피벗 모드)로 옮긴 관절 위치를 스킨 에셋에 저장한다.
    /// 쉬는 자세일 때만 정확하다 (에디터 편집 모드).
    /// </summary>
    public int SaveJointsToSkin()
    {
        var skin = Skin;
        if (skin == null || _rects == null) return 0;

        UnityEditor.Undo.RecordObject(skin, "Save Gecko Joints");
        int saved = 0;
        for (int i = 0; i < GeckoParts.Count; i++)
        {
            var rt = _rects[i];
            var art = skin.GetPart((GeckoPartId)i);
            if (rt == null || art == null) continue;
            art.jointPivot    = rt.pivot;
            art.jointPosition = rt.anchoredPosition;
            saved++;
        }
        UnityEditor.EditorUtility.SetDirty(skin);
        ApplySkin();
        SolveRest();
        return saved;
    }
#endif
}
