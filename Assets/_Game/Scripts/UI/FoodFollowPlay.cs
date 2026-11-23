using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Transient food play UI. No inventory or player data is changed here.</summary>
public sealed class FoodFollowPlay : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private GeckoMovementAI _move;
    private RectTransform _area, _food;
    private Vector2 _target, _stillPoint;
    private Func<bool> _valid;
    private Action _eat;
    private float _still, _elapsed;
    private bool _touched, _ended;
    private int _pointer = int.MinValue;
    private bool _liveFood, _autoMoving;
    private float _chaseTime;
    private const float ChaseGrace = 6f; // [TBD] repeated crawls before catching
    private const float PlacementWait = .35f; // [TBD] brief pause after placing food
    private float _autoWait = PlacementWait, _autoTime, _autoDuration;
    private Vector2 _autoStart, _autoEnd;

    private static bool IsLiveFood(ItemSO item)
        => item != null && (item.itemId == "cricket_small" || item.itemId == "gutloaded_cricket"
            || item.itemId == "mealworm" || item.itemId == "superworm" || item.itemId == "dubia_roach");

    public static FoodFollowPlay Create(RectTransform root, RectTransform area, GeckoMovementAI move,
        ItemSO item, TMP_FontAsset font, Vector2 target, bool sick, Func<bool> valid, Action eat)
    {
        var go = new GameObject("FoodFollowPlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(FoodFollowPlay));
        var rt = (RectTransform)go.transform;
        rt.SetParent(root, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = Color.clear;
        var play = go.GetComponent<FoodFollowPlay>();
        play._move = move; play._area = area; play._target = play._stillPoint = target;
        play._valid = valid; play._eat = eat;
        play._liveFood = IsLiveFood(item);

        var food = new GameObject("Food", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        play._food = (RectTransform)food.transform;
        play._food.SetParent(rt, false); play._food.sizeDelta = new Vector2(90f, 90f);
        var icon = food.GetComponent<Image>(); icon.sprite = item.icon; icon.preserveAspect = true; icon.raycastTarget = false;

        var panel = new GameObject("Instructions", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var pr = (RectTransform)panel.transform; pr.SetParent(rt, false);
        pr.anchorMin = new Vector2(.06f, 0f); pr.anchorMax = new Vector2(.94f, 0f);
        pr.pivot = new Vector2(.5f, 0f); pr.anchoredPosition = new Vector2(0f, 120f); pr.sizeDelta = new Vector2(0f, 200f);
        panel.GetComponent<Image>().color = new Color(.06f, .1f, .06f, .96f);
        Text(pr, font, Loc.Get(sick ? "foodplay.sick" : (play._liveFood ? "foodplay.live_hint" : "foodplay.hint")), new Vector2(.03f, .45f), new Vector2(.97f, 1f));
        var cancel = new GameObject("Cancel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        var cr = (RectTransform)cancel.transform; cr.SetParent(pr, false);
        cr.anchorMin = new Vector2(.25f, .03f); cr.anchorMax = new Vector2(.75f, .43f); cr.offsetMin = cr.offsetMax = Vector2.zero;
        cancel.GetComponent<Image>().color = new Color(.25f, .35f, .24f, 1f);
        Text(cr, font, Loc.Get("foodplay.cancel"), Vector2.zero, Vector2.one);
        cancel.GetComponent<Button>().onClick.AddListener(play.Cancel);
        play.PlaceFood();
        FoodTray.MarkPlayUsed();
        return play;
    }

    private static void Text(RectTransform parent, TMP_FontAsset font, string value, Vector2 min, Vector2 max)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
        rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var text = go.GetComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.text = value; text.fontSize = 30f; text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white; text.raycastTarget = false;
    }

    private void PlaceFood()
    {
        _food.position = _area.TransformPoint(new Vector3(_target.x,
            _target.y + _area.rect.yMin, 0f));
        ApplyFoodScale();
    }

    private void ApplyFoodScale(float pulse = 1f)
    {
        // Use the same fixed depth band as the gecko, including while dragging.
        _food.localScale = Vector3.one * (_move.DepthScaleFor(_target.y) * pulse);
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (_pointer != int.MinValue || _ended) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_food, e.position, e.pressEventCamera, out var hit)) return;
        // Keep the original touch size even when perspective shrinks the artwork.
        Vector2 touchHalf = _food.rect.size * (.5f / Mathf.Min(1f, Mathf.Max(.01f, _food.localScale.x)));
        if (Mathf.Abs(hit.x) > touchHalf.x || Mathf.Abs(hit.y) > touchHalf.y) return;
        _pointer = e.pointerId; _touched = true; _still = 0f;
        _autoMoving = false; _autoWait = PlacementWait;
        ApplyFoodScale();
    }

    public void OnDrag(PointerEventData e)
    {
        if (_ended || e.pointerId != _pointer || _move == null) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, e.position, e.pressEventCamera, out var p)) return;
        p.y = p.y - _area.rect.yMin + 50f; // food sits above the finger
        if (Vector2.Distance(_target, p) < 10f) return;
        _still = 0f; // a rejected drag must not accidentally feed
        if (!_move.MoveFoodTarget(p)) return;
        // Taps and blocked drags do not restart the chase grace period.
        _chaseTime = 0f;
        _autoMoving = false;
        _autoTime = 0f;
        _autoWait = PlacementWait;
        _target = p; _stillPoint = p; PlaceFood();
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (e.pointerId != _pointer) return;
        _pointer = int.MinValue; _autoWait = PlacementWait;
    }

    private void MoveOnItsOwn()
    {
        if (_pointer != int.MinValue) return;
        if (!_liveFood)
        {
            // A quiet visual cue for prepared food; its target never changes.
            ApplyFoodScale(1f + .025f * Mathf.Sin(_elapsed * 2f));
            return;
        }
        if (_move.FoodPlayNeedsRest) { _autoMoving = false; return; }
        if (_autoMoving)
        {
            _autoTime += Time.deltaTime;
            float t = Mathf.Clamp01(_autoTime / _autoDuration);
            Vector2 next = Vector2.Lerp(_autoStart, _autoEnd, Mathf.SmoothStep(0f, 1f, t));
            if (!_move.MoveFoodTarget(next)) { _autoMoving = false; _autoWait = .5f; return; }
            _target = _stillPoint = next; _still = 0f; PlaceFood();
            if (t >= 1f) { _autoMoving = false; _autoWait = 2f; }
            return;
        }
        _autoWait -= Time.deltaTime;
        if (_autoWait > 0f) return;
        // A few nearby candidates only. Never cross a decoration or leave the clear ground.
        Vector2 away = (_target - _move.FoodPlayPosition).normalized;
        if (away.sqrMagnitude < .01f) away = Vector2.right;
        for (int attempt = 0; attempt < 16; attempt++)
        {
            // Prefer away, then try every direction and a shorter fallback step.
            float angle = (attempt % 8) * 45f * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(away.x * Mathf.Cos(angle) - away.y * Mathf.Sin(angle),
                away.x * Mathf.Sin(angle) + away.y * Mathf.Cos(angle));
            Vector2 end = _target + direction * (attempt < 8 ? UnityEngine.Random.Range(45f, 80f) : 25f);
            bool clear = true;
            for (int sample = 1; sample <= 4; sample++)
                clear &= _move.FoodPlayClear(Vector2.Lerp(_target, end, sample / 4f));
            if (!clear) continue;
            _autoStart = _target; _autoEnd = end; _autoTime = 0f;
            _autoDuration = Vector2.Distance(_autoStart, end) / 45f;
            _autoMoving = true; return;
        }
        _autoWait = .5f; // Retry blocked routes; never enter a permanent stopped state.
    }

    private void Update()
    {
        if (_ended) return;
        if (_move == null || !_move.isActiveAndEnabled || !_move.FoodPlayActive || _valid == null || !_valid()) { Cancel(); return; }
        _elapsed += Time.deltaTime;
        if (_elapsed > 90f) { Cancel(); return; } // no penalty, no item charge
        if (_pointer != int.MinValue) { _still = 0f; return; }
        bool chasing = _liveFood && !_move.FoodPlayNeedsRest;
        if (chasing) _chaseTime += Time.deltaTime;
        // Live food can be caught without a tap, but only at a pause after play time.
        // Prepared food and resting geckos keep the deliberate tap-to-feed behavior.
        bool canCatch = chasing ? _chaseTime >= ChaseGrace : _touched;
        if (_autoMoving || !canCatch || !_move.FoodPlayReady)
        {
            _still = 0f;
            MoveOnItsOwn();
            return;
        }
        if (Vector2.Distance(_target, _stillPoint) > 10f) { _stillPoint = _target; _still = 0f; }
        _still += Time.deltaTime;
        if (_still < 1f) return;
        var eat = _eat;
        Cancel(); // end first, so a callback cannot consume twice
        eat?.Invoke();
    }

    public void Cancel()
    {
        if (_ended) return;
        _ended = true;
        if (_move != null) _move.EndFoodPlay();
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void OnApplicationPause(bool paused) { if (paused) Cancel(); }
    private void OnDisable() { if (!_ended) Cancel(); }
}
