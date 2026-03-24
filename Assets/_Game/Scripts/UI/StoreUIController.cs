using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Store.unity 에 부착. StoreManager를 통해 아이템/게코 구매를 처리한다.
///
/// 씬 구성 (Unity Editor에서 직접 배치):
///   Canvas
///     TopBar           — 코인/젬 표시
///     ItemListPanel    — 아이템 목록 (ScrollView > Content 에 ItemSlot 프리팹 배치)
///     BackButton       — 홈으로 돌아가기
///     ErrorPanel       — 구매 실패 알림 (2.5초 자동 숨김)
///       ErrorText
/// </summary>
public class StoreUIController : MonoBehaviour
{
    [Header("상단 바")]
    [SerializeField] private TMP_Text _coinText;
    [SerializeField] private TMP_Text _gemText;

    [Header("아이템 슬롯 루트 (ScrollView > Viewport > Content)")]
    [SerializeField] private Transform _itemListContent;
    [SerializeField] private GameObject _itemSlotPrefab; // ItemSlot 프리팹

    [Header("판매 아이템 목록 (Inspector에서 드래그)")]
    [SerializeField] private ItemSO[] _itemsForSale;

    [Header("오류 알림")]
    [SerializeField] private GameObject _errorPanel;
    [SerializeField] private TMP_Text   _errorText;
    private const float ERROR_DISPLAY_SECONDS = 2.5f;
    private Coroutine   _errorCoroutine;

    [Header("뒤로가기")]
    [SerializeField] private Button _backButton;

    private StoreManager _store;

    // ── 생명주기 ──────────────────────────────────────────────

    private void OnEnable()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[StoreUIController] GameManager.Instance가 null — Boot 씬부터 실행하세요.");
            return;
        }

        _store = GameManager.Instance.Store;
        _store.OnItemPurchased  += OnItemPurchasedHandler;
        _store.OnPurchaseFailed += OnPurchaseFailedHandler;

        _backButton.onClick.AddListener(OnBackClicked);

        if (_errorPanel != null)
            _errorPanel.SetActive(false);

        RefreshCurrency();
        BuildItemList();
    }

    private void OnDisable()
    {
        if (_store != null)
        {
            _store.OnItemPurchased  -= OnItemPurchasedHandler;
            _store.OnPurchaseFailed -= OnPurchaseFailedHandler;
        }
        _backButton.onClick.RemoveListener(OnBackClicked);
    }

    // ── 아이템 목록 생성 ──────────────────────────────────────

    private void BuildItemList()
    {
        if (_itemListContent == null || _itemSlotPrefab == null || _itemsForSale == null) return;

        // 기존 슬롯 제거 후 재생성
        foreach (Transform child in _itemListContent)
            Destroy(child.gameObject);

        foreach (var item in _itemsForSale)
        {
            if (item == null) continue;
            var go = Instantiate(_itemSlotPrefab, _itemListContent);
            var slot = go.GetComponent<ItemSlotUI>();
            if (slot != null)
                slot.Setup(item, OnBuyItemClicked);
        }
    }

    // ── 버튼 핸들러 ───────────────────────────────────────────

    private void OnBuyItemClicked(ItemSO item)
    {
        _store.BuyItem(item, 1);
    }

    private void OnBackClicked()
    {
        SceneRouter.GoToHome();
    }

    // ── 이벤트 핸들러 ─────────────────────────────────────────

    private void OnItemPurchasedHandler(string itemId, int newCount)
    {
        RefreshCurrency();
        Debug.Log($"[StoreUIController] 구매 완료 — {itemId} (보유: {newCount})");
    }

    private void OnPurchaseFailedHandler(string reason)
    {
        ShowError(reason);
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
}
