using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GeckoList.unity 에 부착. 보유 게코 목록 표시 + 신규 분양 처리.
///
/// 씬 구성 (Unity Editor에서 직접 배치):
///   Canvas
///     TopBar               — 코인/젬 표시
///     GeckoListContent     — ScrollView > Viewport > Content (GeckoSlot 프리팹 배치)
///     AdoptPanel           — 분양 패널 (종 선택 + 이름 입력 + 확인 버튼)
///       SpeciesDropdown    — TMP_Dropdown
///       NameInputField     — TMP_InputField
///       ConfirmButton      — Button
///       CancelButton       — Button
///     AdoptButton          — 분양 패널 열기 버튼
///     BackButton           — 홈으로
///     ErrorPanel / ErrorText
/// </summary>
public class GeckoListUIController : MonoBehaviour
{
    [Header("상단 바")]
    [SerializeField] private TMP_Text _coinText;
    [SerializeField] private TMP_Text _gemText;

    [Header("게코 목록 (ScrollView > Content)")]
    [SerializeField] private Transform  _geckoListContent;
    [SerializeField] private GameObject _geckoSlotPrefab;

    [Header("분양 패널")]
    [SerializeField] private GameObject       _adoptPanel;
    [SerializeField] private TMP_Dropdown     _speciesDropdown;
    [SerializeField] private TMP_InputField   _nameInputField;
    [SerializeField] private Button           _confirmAdoptButton;
    [SerializeField] private Button           _cancelAdoptButton;
    [SerializeField] private Button           _adoptButton;

    [Header("판매 종 목록 (Inspector에서 드래그)")]
    [SerializeField] private GeckoSpeciesSO[] _speciesForSale;

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
            Debug.LogError("[GeckoListUIController] GameManager.Instance가 null — Boot 씬부터 실행하세요.");
            return;
        }

        _store = GameManager.Instance.Store;
        _store.OnGeckoPurchased += OnGeckoPurchasedHandler;
        _store.OnPurchaseFailed += OnPurchaseFailedHandler;

        _adoptButton.onClick.AddListener(OpenAdoptPanel);
        _confirmAdoptButton.onClick.AddListener(OnConfirmAdopt);
        _cancelAdoptButton.onClick.AddListener(CloseAdoptPanel);
        _backButton.onClick.AddListener(OnBackClicked);

        if (_adoptPanel != null)   _adoptPanel.SetActive(false);
        if (_errorPanel != null)   _errorPanel.SetActive(false);

        BuildSpeciesDropdown();
        RefreshCurrency();
        RefreshGeckoList();
    }

    private void OnDisable()
    {
        if (_store != null)
        {
            _store.OnGeckoPurchased -= OnGeckoPurchasedHandler;
            _store.OnPurchaseFailed -= OnPurchaseFailedHandler;
        }
        _adoptButton.onClick.RemoveListener(OpenAdoptPanel);
        _confirmAdoptButton.onClick.RemoveListener(OnConfirmAdopt);
        _cancelAdoptButton.onClick.RemoveListener(CloseAdoptPanel);
        _backButton.onClick.RemoveListener(OnBackClicked);
    }

    // ── 게코 목록 ─────────────────────────────────────────────

    private void RefreshGeckoList()
    {
        if (_geckoListContent == null || _geckoSlotPrefab == null) return;

        foreach (Transform child in _geckoListContent)
            Destroy(child.gameObject);

        var data = GameManager.Instance.GetPlayerData();
        foreach (var gecko in data.geckos)
        {
            var go   = Instantiate(_geckoSlotPrefab, _geckoListContent);
            var slot = go.GetComponent<GeckoSlotUI>();
            if (slot != null)
                slot.Setup(gecko, OnGeckoSlotClicked);
        }
    }

    private void OnGeckoSlotClicked(GeckoData gecko)
    {
        GameManager.Instance.SetSelectedGecko(gecko.id);
        SceneRouter.GoToHome();
    }

    // ── 분양 패널 ─────────────────────────────────────────────

    private void BuildSpeciesDropdown()
    {
        if (_speciesDropdown == null || _speciesForSale == null) return;

        _speciesDropdown.ClearOptions();
        var options = new System.Collections.Generic.List<string>();
        foreach (var s in _speciesForSale)
        {
            if (s == null) continue;
            string label = s.isUnlockedByDefault
                ? $"{s.displayName}  (Free)"
                : $"{s.displayName}  {s.coinPrice} C";
            options.Add(label);
        }
        _speciesDropdown.AddOptions(options);
    }

    private void OpenAdoptPanel()
    {
        if (_adoptPanel != null) _adoptPanel.SetActive(true);
        if (_nameInputField != null) _nameInputField.text = "";
    }

    private void CloseAdoptPanel()
    {
        if (_adoptPanel != null) _adoptPanel.SetActive(false);
    }

    private void OnConfirmAdopt()
    {
        if (_speciesForSale == null || _speciesForSale.Length == 0) return;

        int idx     = _speciesDropdown != null ? _speciesDropdown.value : 0;
        var species = _speciesForSale[Mathf.Clamp(idx, 0, _speciesForSale.Length - 1)];
        string name = _nameInputField != null ? _nameInputField.text.Trim() : "";

        _store.BuyGecko(species, name);
        CloseAdoptPanel();
    }

    // ── 이벤트 핸들러 ─────────────────────────────────────────

    private void OnGeckoPurchasedHandler(GeckoData gecko)
    {
        RefreshCurrency();
        RefreshGeckoList();
        Debug.Log($"[GeckoListUIController] 분양 완료 — {gecko.name}");
    }

    private void OnPurchaseFailedHandler(string reason)
    {
        ShowError(reason);
    }

    private void OnBackClicked()
    {
        SceneRouter.GoToHome();
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
