using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Terrarium.unity 에 부착. TerrariumManager를 통해 테마/장식을 변경한다.
///
/// 씬 구성 (Unity Editor에서 직접 배치):
///   Canvas
///     TopBar
///       CoinText / GemText
///     TabBar
///       BgTabButton      — 테마 탭 (씬 글자 "배경"을 실행 중에 "테마"로)
///       FloorTabButton   — 쓰지 않음 (2026-09-21 바닥은 테마에 합쳤다 — 실행 중에 숨긴다)
///       DecorTabButton   — 장식 탭
///     ScrollView
///       Viewport > Content  ← _itemListContent (탭 전환 시 재생성)
///     BackButton
///     ErrorPanel / ErrorText
/// </summary>
public class TerrariumUIController : MonoBehaviour
{
    [Header("상단 바")]
    [SerializeField] private TMP_Text _coinText;
    [SerializeField] private TMP_Text _gemText;

    [Header("탭 버튼")]
    [SerializeField] private Button _bgTabButton;
    [SerializeField] private Button _floorTabButton;
    [SerializeField] private Button _decorTabButton;

    [Header("아이템 목록 (ScrollView > Content)")]
    [SerializeField] private Transform  _itemListContent;
    [SerializeField] private GameObject _decorSlotPrefab;

    [Header("판매 장식 목록 (Inspector에서 드래그)")]
    [SerializeField] private DecorItemSO[] _allDecorItems;

    [Header("뒤로가기")]
    [SerializeField] private Button _backButton;

    [Header("오류 알림")]
    [SerializeField] private GameObject _errorPanel;
    [SerializeField] private TMP_Text   _errorText;
    private const float ERROR_DISPLAY_SECONDS = 2.5f;
    private Coroutine   _errorCoroutine;

    private TerrariumManager _terrarium;
    private DecorCategory    _currentTab = DecorCategory.Background;

    // ── 생명주기 ──────────────────────────────────────────────

    private void OnEnable()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[TerrariumUIController] GameManager가 아직 없습니다 — AppBootstrap이 초기화한 뒤 이 씬을 다시 엽니다.");
            return;
        }

        _terrarium = GameManager.Instance.Terrarium;

        _bgTabButton?.onClick.AddListener(OnBgTabClicked);
        _decorTabButton?.onClick.AddListener(OnDecorTabClicked);
        _backButton?.onClick.AddListener(OnBackClicked);
        ApplyThemeTabs();

        if (_errorPanel != null) _errorPanel.SetActive(false);
        FitErrorText();

        RefreshCurrency();
        ShowTab(DecorCategory.Background);
    }

    private void OnDisable()
    {
        _bgTabButton?.onClick.RemoveListener(OnBgTabClicked);
        _decorTabButton?.onClick.RemoveListener(OnDecorTabClicked);
        _backButton?.onClick.RemoveListener(OnBackClicked);
    }

    // ── 탭 전환 ───────────────────────────────────────────────

    private void OnBgTabClicked()    => ShowTab(DecorCategory.Background);
    private void OnDecorTabClicked() => ShowTab(DecorCategory.Decoration);

    // 배경·바닥을 "테마"로 합쳤다 (2026-09-21) — 바닥 탭은 숨기고(탭 줄이 가로 배치라 나머지 둘이 넓어진다),
    // 배경 탭 글자는 "테마"로. 씬은 그대로 둔다
    private void ApplyThemeTabs()
    {
        if (_floorTabButton != null) _floorTabButton.gameObject.SetActive(false);
        if (_bgTabButton == null) return;
        var label = _bgTabButton.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = Loc.Get("terrarium.theme");
    }

    private void ShowTab(DecorCategory category)
    {
        _currentTab = category;
        BuildItemList(category);
    }

    // ── 아이템 목록 생성 ──────────────────────────────────────

    // 씬 목록(Inspector) + Resources/Decor의 새 장식 — 씬을 고치지 않아도 새 구조물이 보인다. 무료·싼 것부터
    private System.Collections.Generic.List<DecorItemSO> ItemsForSale()
    {
        var list = new System.Collections.Generic.List<DecorItemSO>();
        var seen = new System.Collections.Generic.HashSet<string>();
        void Add(DecorItemSO item)
        {
            if (item != null && !string.IsNullOrEmpty(item.itemId) && seen.Add(item.itemId)) list.Add(item);
        }
        if (_allDecorItems != null) foreach (var item in _allDecorItems) Add(item);
        foreach (var item in DecorCatalog.All) Add(item);
        list.Sort((a, b) => (a.gemPrice * 100 + a.coinPrice).CompareTo(b.gemPrice * 100 + b.coinPrice));
        return list;
    }

    private void BuildItemList(DecorCategory category)
    {
        if (_itemListContent == null || _decorSlotPrefab == null) return;

        foreach (Transform child in _itemListContent)
            Destroy(child.gameObject);

        var data = _terrarium.GetData();

        foreach (var item in ItemsForSale())
        {
            if (item == null || item.category != category) continue;

            // 지금 적용 중인가 — 장식은 슬롯 어딘가에 놓여 있으면 적용 중
            bool applied = category == DecorCategory.Background ? item.itemId == data.backgroundId
                         : FindDecorSlot(item.itemId) >= 0;

            var go   = Instantiate(_decorSlotPrefab, _itemListContent);
            var slot = go.GetComponent<DecorSlotUI>();
            if (slot != null)
                slot.Setup(item, OnDecorItemSelected, applied, _terrarium.IsOwned(item),
                           canRemove: applied && category == DecorCategory.Decoration,
                           locked: !_terrarium.IsUnlocked(item));
        }
    }

    // ── 선택 핸들러 ───────────────────────────────────────────

    private void OnDecorItemSelected(DecorItemSO item)
    {
        int decorSlot = -1;
        if (item.category == DecorCategory.Decoration)
        {
            // 이미 놓은 장식을 다시 누르면 빼낸다 (무료) — 슬롯 4개가 영영 잠기지 않게
            int placed = FindDecorSlot(item.itemId);
            if (placed >= 0)
            {
                _terrarium.ClearDecor(placed);
                AudioManager.Play(Sfx.Pop, 0.8f);
                Haptics.Light();
                BuildItemList(_currentTab);
                return;
            }

            // 어덜트 전용 장식 — 조건을 알려 준다 (값을 받기 전에)
            if (!_terrarium.IsUnlocked(item))
            {
                ShowError(Loc.Format("terrarium.locked_hint", item.requiredAdults));
                return;
            }

            // 빈 슬롯부터 확인 — 예전에는 재화를 먼저 차감한 뒤 "가득 찼습니다"를 띄워 코인만 사라졌다
            decorSlot = _terrarium.FindEmptySlot(item);   // 바닥 장식은 바닥 칸(0·1), 벽 구조물은 뒷벽 칸(2·3)
            if (decorSlot < 0)
            {
                ShowError(Loc.Get(item.placement == DecorPlacement.Wall ? "terrarium.wall_full" : "terrarium.floor_full"));
                return;
            }
        }

        // 재화 확인 및 차감 — 이미 가진 배경·바닥은 다시 값을 받지 않는다
        if (!_terrarium.IsOwned(item))
        {
            if (item.gemPrice > 0)
            {
                if (!GameManager.Instance.SpendGem(item.gemPrice))
                {
                    ShowError(Loc.Format("common.need_gem", item.gemPrice));
                    return;
                }
            }
            else if (item.coinPrice > 0)
            {
                if (!GameManager.Instance.SpendCoin(item.coinPrice))
                {
                    ShowError(Loc.Format("common.need_coin", item.coinPrice));
                    return;
                }
            }
        }

        switch (item.category)
        {
            case DecorCategory.Background:
                _terrarium.MarkOwned(item.itemId);
                _terrarium.SetBackground(item.itemId);
                break;
            case DecorCategory.Decoration:
                _terrarium.SetDecor(decorSlot, item.itemId);
                break;
        }

        AudioManager.Play(Sfx.Sparkle, 0.8f);
        Haptics.Light();
        RefreshCurrency();
        BuildItemList(_currentTab);
    }

    /// <summary>이 장식이 놓여 있는 슬롯 번호. 없으면 -1.</summary>
    private int FindDecorSlot(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return -1;
        var slots = _terrarium.GetData().decorSlots;
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == itemId) return i;
        return -1;
    }

    // ── UI 갱신 ───────────────────────────────────────────────

    private void RefreshCurrency()
    {
        var data = GameManager.Instance.GetPlayerData();
        if (_coinText != null) _coinText.text = Loc.Format("hud.coin", data.coin.ToString("N0"));
        if (_gemText  != null) _gemText.text  = Loc.Format("hud.gem",  data.gem.ToString("N0"));
    }

    // 씬의 오류 패널에 붙은 VerticalLayoutGroup(자식 크기 조절 꺼짐)이 글자 칸 폭을 0으로 만들어
    // 문구가 한 글자씩 세로로 나왔다 — 레이아웃을 끄고 글자를 패널 안에 여백만 두고 꽉 채운다
    private static readonly Vector2 ERROR_PADDING = new Vector2(25f, 15f);

    private void FitErrorText()
    {
        if (_errorPanel == null || _errorText == null) return;
        var group = _errorPanel.GetComponent<UnityEngine.UI.LayoutGroup>();
        if (group != null) group.enabled = false;

        var rt = _errorText.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.offsetMin = ERROR_PADDING;
        rt.offsetMax = -ERROR_PADDING;
        _errorText.textWrappingMode = TextWrappingModes.Normal;
        _errorText.alignment        = TextAlignmentOptions.Center;
        _errorText.enableAutoSizing = true;   // 두 줄 문구도 패널 안에 들어가게
        _errorText.fontSizeMin      = 22f;
        _errorText.fontSizeMax      = 34f;
    }

    private void ShowError(string message)
    {
        AudioManager.Play(Sfx.Error, 0.8f);
        if (_errorPanel == null || _errorText == null) return;
        if (_errorCoroutine != null) StopCoroutine(_errorCoroutine);
        _errorText.text = message;
        _errorPanel.SetActive(true);
        _errorCoroutine = StartCoroutine(HideErrorAfterDelay());
    }

    private System.Collections.IEnumerator HideErrorAfterDelay()
    {
        yield return new WaitForSeconds(ERROR_DISPLAY_SECONDS);
        if (_errorPanel != null) _errorPanel.SetActive(false);
        _errorCoroutine = null;
    }

    private void OnBackClicked() => SceneRouter.GoToHome();
}
