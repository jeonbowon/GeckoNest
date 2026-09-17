using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 홈 화면 장식 그림 하나에 붙는 누르기·끌기 입력 — 길게 누르기(편집 모드 시작)와 끌기를 HomeUIController에 넘긴다.
/// HomeUIController가 실행 중에 붙인다. 끌기는 CanDrag가 true일 때(편집 모드)만 시작하며,
/// 길게 눌러 편집 모드가 켜진 뒤 손을 떼지 않고 끌어도 이어서 옮겨진다.
/// </summary>
[DisallowMultipleComponent]
public class DecorDragHandle : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public const float LONG_PRESS      = 0.5f;   // [TBD] 초
    public const float LONG_PRESS_SLOP = 24f;    // 길게 누르는 동안 이만큼(화면 픽셀) 넘게 움직이면 취소

    public int Slot;
    public Action<int>                   LongPressed;
    public Func<int, bool>               CanDrag;
    public Action<int, PointerEventData> DragBegan, Dragged, DragEnded;

    private bool    _pressing;
    private bool    _fired;
    private bool    _dragging;
    private float   _downTime;
    private Vector2 _downPos;

    public void OnPointerDown(PointerEventData e)
    {
        _pressing = true;
        _fired    = false;
        _downTime = Time.unscaledTime;
        _downPos  = e.position;
    }

    public void OnPointerUp(PointerEventData e) => _pressing = false;

    private void Update()
    {
        if (!_pressing || _fired || Time.unscaledTime - _downTime < LONG_PRESS) return;
        _fired = true;
        LongPressed?.Invoke(Slot);
    }

    public void OnBeginDrag(PointerEventData e) => TryBegin(e);

    public void OnDrag(PointerEventData e)
    {
        if (!_dragging)
        {
            if (!_fired && (e.position - _downPos).magnitude > LONG_PRESS_SLOP) _pressing = false;   // 길게 누르기 취소
            TryBegin(e);   // 길게 눌러 편집 모드가 켜진 뒤 이어서 끄는 경우
            if (!_dragging) return;
        }
        Dragged?.Invoke(Slot, e);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (!_dragging) return;
        _dragging = false;
        DragEnded?.Invoke(Slot, e);
    }

    private void TryBegin(PointerEventData e)
    {
        if (_dragging || CanDrag == null || !CanDrag(Slot)) return;
        _dragging = true;
        DragBegan?.Invoke(Slot, e);
    }

    private void OnDisable()
    {
        _pressing = false;
        if (_dragging)
        {
            _dragging = false;
            DragEnded?.Invoke(Slot, null);
        }
    }
}
