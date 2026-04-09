using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 테라리움 씬의 모든 DepthObject를 관리.
/// Update마다 sortingOrder로 정렬 (앞 오브젝트가 위에 렌더링).
/// </summary>
public class TerrariumDepthManager : MonoBehaviour
{
    [SerializeField] private DepthScaleConfig _config;

    private readonly List<DepthObject> _depthObjects = new List<DepthObject>();

    private void Update()
    {
        // 씬 내 DepthObject 목록 갱신 (동적 생성 대응)
        _depthObjects.Clear();
        _depthObjects.AddRange(FindObjectsByType<DepthObject>(FindObjectsSortMode.None));

        // sortingOrder 내림차순 = 앞에 있는 오브젝트를 위에 렌더링
        // (DepthObject.Update가 먼저 실행되어 sortingOrder를 설정한 뒤 이 정렬은 참고용)
    }

    /// <summary>
    /// 장식품 배치 미리보기 시 해당 Y 위치의 스케일을 반환.
    /// </summary>
    public float GetScaleAtY(float y)
    {
        if (_config == null) return 1f;

        float range = _config.bottomY - _config.topY;
        if (Mathf.Approximately(range, 0f)) return 1f;

        float t = Mathf.Clamp01((y - _config.topY) / range);
        return Mathf.Lerp(_config.minScale, _config.maxScale, t);
    }

    /// <summary>
    /// 외부에서 DepthObject를 등록할 때 사용 (동적 생성 시 호출).
    /// </summary>
    public void Register(DepthObject obj)
    {
        if (!_depthObjects.Contains(obj))
            _depthObjects.Add(obj);
    }
}
