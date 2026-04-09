using System.Collections;
using UnityEngine;

/// <summary>
/// 게코 오브젝트의 랜덤 이동 AI.
/// 테라리움 내에서 목표 지점을 선택해 이동하고 대기를 반복.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class GeckoMovementAI : MonoBehaviour
{
    [SerializeField] private float moveSpeed    = 80f;          // px/s (World unit/s) [TBD]
    [SerializeField] private float waitTimeMin  = 1.5f;         // [TBD]
    [SerializeField] private float waitTimeMax  = 4f;           // [TBD]
    [SerializeField] private Rect  moveBounds;                  // 이동 가능 영역 (Inspector 설정)
    [SerializeField] private Transform[] climbTargets;          // 나무 등 오를 수 있는 오브젝트

    private const float CLIMB_DETECT_RADIUS  = 1.5f;   // [TBD]
    private const float CLIMB_CHANCE         = 0.3f;   // [TBD]

    private SpriteRenderer _sr;
    private Coroutine      _moveRoutine;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        _moveRoutine = StartCoroutine(MoveLoop());
    }

    private void OnDisable()
    {
        if (_moveRoutine != null)
            StopCoroutine(_moveRoutine);
    }

    private IEnumerator MoveLoop()
    {
        while (true)
        {
            // 대기
            yield return new WaitForSeconds(Random.Range(waitTimeMin, waitTimeMax));

            // 목표 지점 결정
            Vector3 target = PickTarget();

            // 이동
            yield return MoveToTarget(target);
        }
    }

    private Vector3 PickTarget()
    {
        // 근처 climbTarget 있으면 30% 확률로 해당 위치 위로
        if (climbTargets != null && climbTargets.Length > 0)
        {
            foreach (var ct in climbTargets)
            {
                if (ct == null) continue;
                float dist = Vector2.Distance(transform.position, ct.position);
                if (dist < CLIMB_DETECT_RADIUS && Random.value < CLIMB_CHANCE)
                    return ct.position + Vector3.up * 0.5f;
            }
        }

        // 이동 가능 영역 내 랜덤 지점
        return new Vector3(
            Random.Range(moveBounds.xMin, moveBounds.xMax),
            Random.Range(moveBounds.yMin, moveBounds.yMax),
            transform.position.z
        );
    }

    private IEnumerator MoveToTarget(Vector3 target)
    {
        // 방향에 따라 스프라이트 좌우 반전
        if (target.x < transform.position.x)
            _sr.flipX = true;
        else if (target.x > transform.position.x)
            _sr.flipX = false;

        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                moveSpeed * Time.deltaTime
            );
            yield return null;
        }

        transform.position = target;
    }
}
