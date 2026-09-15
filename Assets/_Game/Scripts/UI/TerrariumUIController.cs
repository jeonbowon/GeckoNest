using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Terrarium.unity 에 부착. TerrariumManager를 통해 배경/바닥/장식을 변경한다.
///
/// 씬 구성 (Unity Editor에서 직접 배치):
///   Canvas
///     TopBar
///       CoinText / GemText
///     TabBar
///       BgTabButton      — 배경 탭
///       FloorTabButton   — 바닥 탭
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
        _floorTabButton?.onClick.AddListener(OnFloorTabClicked);
        _decorTabButton?.onClick.AddListener(OnDecorTabClicked);
        _backButton?.onClick.AddListener(OnBackClicked);

        if (_errorPanel != null) _errorPanel.SetActive(false);

        RefreshCurrency();
        ShowTab(DecorCategory.Background);
    }

    private void OnDisable()
    {
        _bgTabButton?.onClick.RemoveListener(OnBgTabClicked);
        _floorTabButton?.onClick.RemoveListener(OnFloorTabClicked);
        _decorTabButton?.onClick.RemoveListener(OnDecorTabClicked);
        _backButton?.onClick.RemoveListener(OnBackClicked);
    }

    // ── 탭 전환 ───────────────────────────────────────────────

    private void OnBgTabClicked()    => ShowTab(DecorCategory.Background);
    private void OnFloorTabClicked() => ShowTab(DecorCategory.Floor);
    private void OnDecorTabClicked() => ShowTab(DecorCategory.Decoration);

    private void ShowTab(DecorCategory category)
    {
        _currentTab = category;
        BuildItemList(category);
    }

    // ── 아이템 목록 생성 ──────────────────────────────────────

    private void BuildItemList(DecorCategory category)
    {
        if (_itemListContent == null || _decorSlotPrefab == null || _allDecorItems == null) return;

        foreach (Transform child in _itemListContent)
            Destroy(child.gameObject);

        var data = _terrarium.GetData();

        foreach (var item in _allDecorItems)
        {
            if (item == null || item.category != category) continue;

            // 지금 적용 중인가 — 장식은 슬롯 어딘가에 놓여 있으면 적용 중
            bool applied = category == DecorCategory.Background ? item.itemId == data.backgroundId
                         : category == DecorCategory.Floor      ? item.itemId == data.floorId
                         : FindDecorSlot(item.itemId) >= 0;

            var go   = Instantiate(_decorSlotPrefab, _itemListContent);
            var slot = go.GetComponent<DecorSlotUI>();
            if (slot != null)
                slot.Setup(item, OnDecorItemSelected, applied, _terrarium.IsOwned(item),
                           canRemove: applied && category == DecorCategory.Decoration);
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

            // 빈 슬롯부터 확인 — 예전에는 재화를 먼저 차감한 뒤 "가득 찼습니다"를 띄워 코인만 사라졌다
            decorSlot = FindEmptyDecorSlot();
            if (decorSlot < 0)
            {
                ShowError(Loc.Get("terrarium.slots_full"));
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
            case DecorCategory.Floor:
                _terrarium.MarkOwned(item.itemId);
                _terrarium.SetFloor(item.itemId);
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

    private int FindEmptyDecorSlot()
    {
        var slots = _terrarium.GetData().decorSlots;
        for (int i = 0; i < slots.Length; i++)
            if (string.IsNullOrEmpty(slots[i])) return i;
        return -1;
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
