using System.Collections;
using UnityEngine;

/// <summary>
/// 게코가 테라리움 바닥을 돌아다니는 AI (UI 좌표).
/// 기다림 → 목표 지점 선택 → 돌아서기 → 걷기(가속·감속) → 가끔 중간에 멈춰 둘러보기 를 반복한다.
///
/// - 부모(GeckoArea)의 아래쪽 가장자리 기준으로 발 높이를 잡는다
/// - 발 높이가 높을수록(멀리 있을수록) 조금 작게 그려 원근감을 준다
/// - 먹이·쓰다듬기 등 동작 중이거나 졸린 기분이면 걷지 않는다
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(GeckoMotor))]
public class GeckoMovementAI : MonoBehaviour
{
    [Header("걷기")]
    [Tooltip("다 자란 게코 기준 최고 속도 (UI 단위/초)")]
    [SerializeField] private float moveSpeed   = 90f;    // [TBD]
    [SerializeField] private float waitTimeMin = 2.5f;   // [TBD]
    [SerializeField] private float waitTimeMax = 6f;     // [TBD]
    [SerializeField, Range(0f, 1f)] private float pauseMidwayChance = 0.2f;

    [Header("다니는 범위")]
    [Tooltip("발이 닿는 높이 범위 (부모 영역 아래 끝 기준, UI 단위). x=가까운 쪽, y=먼 쪽")]
    [SerializeField] private Vector2 groundBand = new Vector2(400f, 520f);   // [TBD]
    [Tooltip("화면 좌우 끝에서 띄울 여백")]
    [SerializeField] private float sideMargin = 30f;

    [Header("원근")]
    [Tooltip("가장 먼 쪽(groundBand.y)에서의 크기")]
    [SerializeField, Range(0.5f, 1f)] private float farScale = 0.86f;        // [TBD]

    private const float ACCEL = 260f;   // UI 단위/초²
    private const float DECEL = 220f;

    private RectTransform _rt;
    private GeckoMotor    _motor;
    private GeckoRig      _rig;
    private Coroutine     _loop;

    private void Awake()
    {
        _rt    = (RectTransform)transform;
        _motor = GetComponent<GeckoMotor>();
        _rig   = GetComponent<GeckoRig>();
    }

    private void OnEnable()
    {
        ClampIntoBand();
        _loop = StartCoroutine(Loop());
    }

    private void OnDisable()
    {
        if (_loop != null) StopCoroutine(_loop);
        _loop = null;
        if (_motor != null) _motor.SetWalking(false);
    }

    private void Update()
    {
        if (_rig == null) return;
        float t = Mathf.InverseLerp(groundBand.x, groundBand.y, _rt.anchoredPosition.y);
        _rig.DepthScale = Mathf.Lerp(1f, farScale, t);
    }

    // ── 행동 루프 ────────────────────────────────────────────

    private IEnumerator Loop()
    {
        yield return null;   // 첫 프레임에는 레이아웃 크기가 확정되지 않았을 수 있다

        while (true)
        {
            float wait = Random.Range(waitTimeMin, waitTimeMax);
            while (wait > 0f)
            {
                if (!_motor.IsBusy) wait -= Time.deltaTime;
                yield return null;
            }

            if (_motor.Mood == GeckoMood.Sleepy) continue;   // 졸리면 제자리

            yield return WalkTo(PickTarget());

            if (Random.value < 0.3f) _motor.TryPlayIdle(GeckoAction.Tongue_Lick);
        }
    }

    private IEnumerator WalkTo(Vector2 target)
    {
        Vector2 start = _rt.anchoredPosition;
        float total = Vector2.Distance(start, target);
        if (total < 4f) yield break;

        // 돌아서기
        bool right = target.x >= start.x;
        if (right != _rig.FacingRight)
        {
            _rig.SetFacing(right);
            float limit = 0.5f;   // 그림이 없어 회전이 진행되지 않는 경우 대비
            while (_rig.IsTurning && limit > 0f)
            {
                limit -= Time.deltaTime;
                yield return null;
            }
        }

        float pauseAt = Random.value < pauseMidwayChance ? Random.Range(0.35f, 0.65f) : 2f;
        bool  paused  = false;
        float speed   = 0f;

        while (true)
        {
            // 먹이·쓰다듬기 등 동작 중이면 멈춰서 기다린다
            if (_motor.IsBusy)
            {
                _motor.SetWalking(false);
                speed = 0f;
                yield return null;
                continue;
            }

            Vector2 pos = _rt.anchoredPosition;
            Vector2 to  = target - pos;
            float dist  = to.magnitude;
            if (dist < 1f) break;

            float maxSpeed = moveSpeed * Mathf.Lerp(0.7f, 1f, _rig.StageScale) * _rig.DepthScale;
            float desired  = Mathf.Min(maxSpeed, Mathf.Sqrt(2f * DECEL * dist));
            speed = Mathf.MoveTowards(speed, desired, ACCEL * Time.deltaTime);

            float step = Mathf.Min(dist, speed * Time.deltaTime);
            _rt.anchoredPosition = pos + to / dist * step;
            _motor.SetWalking(speed > 5f);
            _motor.AddTravel(step);

            // 가끔 중간에 멈춰서 둘러본다
            if (!paused && 1f - dist / total >= pauseAt)
            {
                paused = true;
                _motor.SetWalking(false);
                float hold = Random.Range(0.8f, 1.6f);
                while (hold > 0f)
                {
                    hold -= Time.deltaTime;
                    yield return null;
                }
                speed = 0f;
            }

            yield return null;
        }

        _motor.SetWalking(false);
    }

    private Vector2 PickTarget()
    {
        float halfW = ParentWidth() * 0.5f;
        _rig.GetExtents(out float left, out float right);

        // 목표 방향으로 돌아선 뒤의 폭까지 고려해 양쪽 모두 들어가는 범위만 쓴다
        float reach = Mathf.Max(left, right);
        float minX = -halfW + sideMargin + reach;
        float maxX =  halfW - sideMargin - reach;

        float x = minX < maxX ? Random.Range(minX, maxX) : 0f;
        float y = Random.Range(groundBand.x, groundBand.y);
        return new Vector2(x, y);
    }

    private float ParentWidth()
    {
        var parent = _rt.parent as RectTransform;
        return parent != null ? parent.rect.width : 1080f;
    }

    private void ClampIntoBand()
    {
        if (_rt == null) return;
        var p = _rt.anchoredPosition;
        p.y = Mathf.Clamp(p.y, groundBand.x, groundBand.y);
        _rt.anchoredPosition = p;
    }
}
