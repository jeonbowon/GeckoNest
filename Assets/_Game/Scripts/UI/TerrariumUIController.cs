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
            Debug.LogError("[TerrariumUIController] GameManager.Instance가 null — Boot 씬부터 실행하세요.");
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

        var data       = _terrarium.GetData();
        string selected = category == DecorCategory.Background ? data.backgroundId
                        : category == DecorCategory.Floor      ? data.floorId
                        : null; // 장식은 슬롯별 개별 관리

        foreach (var item in _allDecorItems)
        {
            if (item == null || item.category != category) continue;

            var go   = Instantiate(_decorSlotPrefab, _itemListContent);
            var slot = go.GetComponent<DecorSlotUI>();
            if (slot != null)
                slot.Setup(item, OnDecorItemSelected, item.itemId == selected);
        }
    }

    // ── 선택 핸들러 ───────────────────────────────────────────

    private void OnDecorItemSelected(DecorItemSO item)
    {
        // 재화 확인 및 차감
        if (item.gemPrice > 0)
        {
            if (!GameManager.Instance.SpendGem(item.gemPrice))
            {
                ShowError($"젬이 부족합니다. (필요: {item.gemPrice})");
                return;
            }
        }
        else if (item.coinPrice > 0)
        {
            if (!GameManager.Instance.SpendCoin(item.coinPrice))
            {
                ShowError($"코인이 부족합니다. (필요: {item.coinPrice})");
                return;
            }
        }

        switch (item.category)
        {
            case DecorCategory.Background:
                _terrarium.SetBackground(item.itemId);
                break;
            case DecorCategory.Floor:
                _terrarium.SetFloor(item.itemId);
                break;
            case DecorCategory.Decoration:
                SetDecorToFirstEmptySlot(item.itemId);
                break;
        }

        RefreshCurrency();
        BuildItemList(_currentTab);
    }

    private void SetDecorToFirstEmptySlot(string itemId)
    {
        var slots = _terrarium.GetData().decorSlots;
        for (int i = 0; i < slots.Length; i++)
        {
            if (string.IsNullOrEmpty(slots[i]))
            {
                _terrarium.SetDecor(i, itemId);
                return;
            }
        }
        ShowError("장식 슬롯이 가득 찼습니다. (최대 4개)");
    }

    // ── UI 갱신 ───────────────────────────────────────────────

    private void RefreshCurrency()
    {
        var data = GameManager.Instance.GetPlayerData();
        if (_coinText != null) _coinText.text = data.coin.ToString("N0");
        if (_gemText  != null) _gemText.text  = data.gem.ToString("N0");
    }

    private void ShowError(string message)
    {
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
