using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 홈 바닥의 빈 곳 두 번 톡톡 — 유대 Lv.3 부르기 (HomeUIController.EnsureFloorCatcher).
/// 게코·장식보다 뒤 순서의 투명한 판이라 그 위를 누르면 이쪽까지 오지 않는다.
/// 두 번째 누르기가 DOUBLE_TAP_TIME 안에, 첫 자리에서 DOUBLE_TAP_SLOP 안이면 DoubleTapped(화면 좌표).
/// </summary>
[RequireComponent(typeof(Image))]
public class FloorTapCatcher : MonoBehaviour, IPointerClickHandler
{
    public const float DOUBLE_TAP_TIME = 0.4f;   // [TBD] 초
    public const float DOUBLE_TAP_SLOP = 80f;    // 화면 픽셀

    public Action<PointerEventData> DoubleTapped;

    private float   _lastTime = -10f;
    private Vector2 _lastPos;

    public void OnPointerClick(PointerEventData e)
    {
        float now = Time.unscaledTime;
        if (now - _lastTime <= DOUBLE_TAP_TIME && (e.position - _lastPos).magnitude <= DOUBLE_TAP_SLOP)
        {
            _lastTime = -10f;   // 세 번째 누르기가 또 두 번 톡톡이 되지 않게
            DoubleTapped?.Invoke(e);
            return;
        }
        _lastTime = now;
        _lastPos  = e.position;
    }

    private void OnDisable() => _lastTime = -10f;
}
