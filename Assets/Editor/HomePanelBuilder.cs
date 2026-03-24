using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// MainHome.unity 에 NavBar / RewardPanel / SettingsPanel 을 자동 생성한다.
///
/// 메뉴: Hako/Home/⓪ NavBar 생성 (Store·GeckoList·Terrarium·Reward·Settings 버튼)
///       Hako/Home/① RewardPanel 생성
///       Hako/Home/② SettingsPanel 생성
///       Hako/Home/③ 패널 전체 생성 (①+②)
///       Hako/Home/④ 전체 생성 (NavBar + 패널)
///
/// 전제조건: MainHome.unity 씬이 열려 있고, 씬 안에 "Canvas" 가 존재해야 한다.
/// HomeUIController 의 모든 버튼·패널 슬롯까지 자동 연결한다.
/// </summary>
public static class HomePanelBuilder
{
    private const string FONT_ASSET_PATH = "Assets/_Game/Fonts/NanumGothic-Regular SDF.asset";

    // ── 색상 ──────────────────────────────────────────────────
    private static readonly Color C_OVERLAY  = new Color(0f,    0f,    0f,    0.65f);
    private static readonly Color C_POPUP    = new Color(0.14f, 0.18f, 0.14f, 0.97f);
    private static readonly Color C_HEADER   = new Color(0.10f, 0.14f, 0.10f, 1.00f);
    private static readonly Color C_BTN_OK   = new Color(0.22f, 0.55f, 0.28f, 1.00f);
    private static readonly Color C_BTN_GRAY = new Color(0.30f, 0.30f, 0.30f, 1.00f);
    private static readonly Color C_BTN_CLOSE= new Color(0.50f, 0.15f, 0.15f, 1.00f);
    private static readonly Color C_GOLD     = new Color(1.00f, 0.80f, 0.20f, 1.00f);
    private static readonly Color C_ROW_EVEN = new Color(0.18f, 0.22f, 0.18f, 1.00f);
    private static readonly Color C_ROW_ODD  = new Color(0.15f, 0.19f, 0.15f, 1.00f);
    private static readonly Color C_NAVBAR   = new Color(0.10f, 0.13f, 0.10f, 0.97f);
    private static readonly Color C_NAV_BTN  = new Color(0.18f, 0.24f, 0.18f, 1.00f);

    // NavBar 버튼 정의 (label, fieldName)
    private static readonly (string label, string emoji, string field)[] NAV_BUTTONS =
    {
        ("상점",   "🛒", "_storeButton"),
        ("게코",   "🦎", "_geckoListButton"),
        ("꾸미기", "🏠", "_terrariumButton"),
        ("보상",   "🎁", "_rewardButton"),
        ("설정",   "⚙",  "_settingsButton"),
    };

    // ── 메뉴 진입점 ───────────────────────────────────────────

    [MenuItem("Hako/Home/④ 전체 생성 (NavBar + 패널)")]
    public static void BuildEverything()
    {
        var canvas = FindCanvas(); if (canvas == null) return;
        var ctrl   = FindHomeController(); if (ctrl == null) return;

        Undo.SetCurrentGroupName("Build Home UI");
        int group = Undo.GetCurrentGroup();

        var navButtons = BuildNavBar(canvas);
        var reward     = BuildRewardPanel(canvas);
        var settings   = BuildSettingsPanel(canvas);

        ConnectNavButtons(ctrl, navButtons);
        ConnectToHomeController(ctrl, reward, settings);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[HomePanelBuilder] NavBar + RewardPanel + SettingsPanel 생성 완료.");
    }

    [MenuItem("Hako/Home/⓪ NavBar 생성")]
    public static void BuildNavBarOnly()
    {
        var canvas = FindCanvas(); if (canvas == null) return;
        var ctrl   = FindHomeController(); if (ctrl == null) return;

        Undo.SetCurrentGroupName("Build NavBar");
        int group = Undo.GetCurrentGroup();

        var navButtons = BuildNavBar(canvas);
        ConnectNavButtons(ctrl, navButtons);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[HomePanelBuilder] NavBar 생성 완료.");
    }

    [MenuItem("Hako/Home/③ 패널 전체 생성 (Reward + Settings)")]
    public static void BuildAll()
    {
        var canvas = FindCanvas(); if (canvas == null) return;
        var ctrl   = FindHomeController(); if (ctrl == null) return;

        Undo.SetCurrentGroupName("Build Home Panels");
        int group = Undo.GetCurrentGroup();

        var reward   = BuildRewardPanel(canvas);
        var settings = BuildSettingsPanel(canvas);

        ConnectToHomeController(ctrl, reward, settings);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[HomePanelBuilder] RewardPanel + SettingsPanel 생성 완료.");
    }

    [MenuItem("Hako/Home/① RewardPanel 생성")]
    public static void BuildRewardOnly()
    {
        var canvas = FindCanvas(); if (canvas == null) return;
        var ctrl   = FindHomeController(); if (ctrl == null) return;

        Undo.SetCurrentGroupName("Build RewardPanel");
        int group = Undo.GetCurrentGroup();

        var reward = BuildRewardPanel(canvas);
        ConnectToHomeController(ctrl, reward, null);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[HomePanelBuilder] RewardPanel 생성 완료.");
    }

    [MenuItem("Hako/Home/② SettingsPanel 생성")]
    public static void BuildSettingsOnly()
    {
        var canvas = FindCanvas(); if (canvas == null) return;
        var ctrl   = FindHomeController(); if (ctrl == null) return;

        Undo.SetCurrentGroupName("Build SettingsPanel");
        int group = Undo.GetCurrentGroup();

        var settings = BuildSettingsPanel(canvas);
        ConnectToHomeController(ctrl, null, settings);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[HomePanelBuilder] SettingsPanel 생성 완료.");
    }

    // ── NavBar ────────────────────────────────────────────────

    /// <summary>
    /// 화면 하단 고정 NavBar 생성. 버튼 5개를 균등 배치.
    /// 반환값: 생성된 Button 배열 (NAV_BUTTONS 순서와 동일).
    /// </summary>
    private static Button[] BuildNavBar(GameObject canvas)
    {
        DestroyExisting(canvas, "NavBar");

        var bar     = CreateImage("NavBar", canvas, C_NAVBAR);
        var barRect = bar.GetComponent<RectTransform>();
        barRect.anchorMin        = new Vector2(0, 0);
        barRect.anchorMax        = new Vector2(1, 0);
        barRect.pivot            = new Vector2(0.5f, 0);
        barRect.anchoredPosition = Vector2.zero;
        barRect.sizeDelta        = new Vector2(0, 120);

        var hlg = bar.AddComponent<HorizontalLayoutGroup>();
        hlg.padding               = new RectOffset(8, 8, 6, 6);
        hlg.spacing               = 4;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment         = TextAnchor.MiddleCenter;

        var buttons = new Button[NAV_BUTTONS.Length];

        for (int i = 0; i < NAV_BUTTONS.Length; i++)
        {
            var (label, emoji, _) = NAV_BUTTONS[i];

            var btnGO = CreateEmpty(label + "Button", bar);
            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = C_NAV_BTN;
            var btn = btnGO.AddComponent<Button>();

            // 버튼 색상 변화
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.30f, 0.55f, 0.35f, 1f);
            colors.pressedColor     = new Color(0.15f, 0.40f, 0.20f, 1f);
            btn.colors = colors;

            // 내부 레이아웃 (이모지 + 라벨 세로 배치)
            var vlg = btnGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding               = new RectOffset(4, 4, 6, 6);
            vlg.spacing               = 2;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = true;
            vlg.childAlignment         = TextAnchor.MiddleCenter;

            // 이모지
            var emojiGO  = CreateEmpty("Emoji", btnGO);
            var emojiTmp = emojiGO.AddComponent<TextMeshProUGUI>();
            emojiTmp.text      = emoji;
            emojiTmp.fontSize  = 32;
            emojiTmp.alignment = TextAlignmentOptions.Center;
            emojiTmp.color     = Color.white;
            var emojiLayoutElem = emojiGO.AddComponent<LayoutElement>();
            emojiLayoutElem.preferredHeight = 38;

            // 라벨
            var labelGO  = CreateEmpty("Label", btnGO);
            var labelTmp = labelGO.AddComponent<TextMeshProUGUI>();
            labelTmp.text      = label;
            labelTmp.fontSize  = 22;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.color     = new Color(0.85f, 0.95f, 0.85f, 1f);
            ApplyKoreanFont(labelTmp);
            var labelLayoutElem = labelGO.AddComponent<LayoutElement>();
            labelLayoutElem.preferredHeight = 28;

            buttons[i] = btn;
        }

        return buttons;
    }

    // ── RewardPanel ───────────────────────────────────────────

    private static GameObject BuildRewardPanel(GameObject canvas)
    {
        // 기존 패널 제거
        DestroyExisting(canvas, "RewardPanel");

        // 루트 (전체 화면 Overlay)
        var root     = CreateEmpty("RewardPanel", canvas);
        var rootRect = root.GetComponent<RectTransform>();
        StretchFull(rootRect);

        // 반투명 배경 (클릭 차단)
        var overlay     = root.AddComponent<Image>();
        overlay.color   = C_OVERLAY;
        var overlayBtn  = root.AddComponent<Button>();   // 빈 영역 클릭 시 닫기
        overlayBtn.onClick.AddListener(() => { });       // 런타임에 RewardPanelUI가 구독

        // PopupBox
        var box     = CreateImage("PopupBox", root, C_POPUP);
        var boxRect = box.GetComponent<RectTransform>();
        boxRect.anchorMin        = new Vector2(0.08f, 0.28f);
        boxRect.anchorMax        = new Vector2(0.92f, 0.72f);
        boxRect.offsetMin        = Vector2.zero;
        boxRect.offsetMax        = Vector2.zero;

        // 헤더
        var header     = CreateImage("Header", box, C_HEADER);
        var headerRect = header.GetComponent<RectTransform>();
        SetTopStretch(headerRect, 0, 80);
        CreateTMP(header, "TitleText", "일일 보상", 40, TextAlignmentOptions.Center, Color.white, true);

        // 닫기 버튼 (헤더 우측)
        var closeBtn = CreateButton(header, "CloseButton", C_BTN_CLOSE, "✕", 36);
        var closeRect = closeBtn.GetComponent<RectTransform>();
        closeRect.anchorMin        = new Vector2(1, 0);
        closeRect.anchorMax        = new Vector2(1, 1);
        closeRect.pivot            = new Vector2(1, 0.5f);
        closeRect.anchoredPosition = new Vector2(-8, 0);
        closeRect.sizeDelta        = new Vector2(80, -10);

        // StreakText
        var streakText = CreateTMP(box, "StreakText", "연속 1일", 34, TextAlignmentOptions.Center, C_GOLD);
        var streakRect = streakText.GetComponent<RectTransform>();
        SetTopStretch(streakRect, 90, 50);
        streakRect.offsetMin = new Vector2(20, streakRect.offsetMin.y);
        streakRect.offsetMax = new Vector2(-20, streakRect.offsetMax.y);

        // RewardText
        var rewardText = CreateTMP(box, "RewardText", "코인 +50", 40, TextAlignmentOptions.Center, Color.white, true);
        var rewardRect = rewardText.GetComponent<RectTransform>();
        rewardRect.anchorMin        = new Vector2(0.05f, 0.35f);
        rewardRect.anchorMax        = new Vector2(0.95f, 0.65f);
        rewardRect.offsetMin        = Vector2.zero;
        rewardRect.offsetMax        = Vector2.zero;

        // ResultText (수령 후 표시)
        var resultText = CreateTMP(box, "ResultText", "수령 완료!", 30, TextAlignmentOptions.Center, C_GOLD);
        var resultRect = resultText.GetComponent<RectTransform>();
        SetBottomStretch(resultRect, 75, 36);
        resultRect.offsetMin = new Vector2(20, resultRect.offsetMin.y);
        resultRect.offsetMax = new Vector2(-20, resultRect.offsetMax.y);
        resultText.gameObject.SetActive(false);

        // ClaimButton
        var claimBtn      = CreateButton(box, "ClaimButton", C_BTN_OK, "받기", 38);
        var claimBtnRect  = claimBtn.GetComponent<RectTransform>();
        SetBottomStretch(claimBtnRect, 14, 60);
        claimBtnRect.offsetMin = new Vector2(30, claimBtnRect.offsetMin.y);
        claimBtnRect.offsetMax = new Vector2(-30, claimBtnRect.offsetMax.y);

        // RewardPanelUI 컴포넌트 연결
        var panelUI = root.AddComponent<RewardPanelUI>();
        var so      = new SerializedObject(panelUI);
        so.FindProperty("_streakText").objectReferenceValue      = streakText.GetComponent<TextMeshProUGUI>();
        so.FindProperty("_rewardText").objectReferenceValue      = rewardText.GetComponent<TextMeshProUGUI>();
        so.FindProperty("_claimButton").objectReferenceValue     = claimBtn.GetComponent<Button>();
        so.FindProperty("_claimButtonText").objectReferenceValue = claimBtn.GetComponentInChildren<TextMeshProUGUI>();
        so.FindProperty("_closeButton").objectReferenceValue     = closeBtn.GetComponent<Button>();
        so.FindProperty("_resultText").objectReferenceValue      = resultText.GetComponent<TextMeshProUGUI>();
        so.ApplyModifiedProperties();

        root.SetActive(false);
        return root;
    }

    // ── SettingsPanel ─────────────────────────────────────────

    private static GameObject BuildSettingsPanel(GameObject canvas)
    {
        DestroyExisting(canvas, "SettingsPanel");

        var root     = CreateEmpty("SettingsPanel", canvas);
        var rootRect = root.GetComponent<RectTransform>();
        StretchFull(rootRect);

        root.AddComponent<Image>().color = C_OVERLAY;

        // PopupBox
        var box     = CreateImage("PopupBox", root, C_POPUP);
        var boxRect = box.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.06f, 0.20f);
        boxRect.anchorMax = new Vector2(0.94f, 0.80f);
        boxRect.offsetMin = Vector2.zero;
        boxRect.offsetMax = Vector2.zero;

        // 헤더
        var header = CreateImage("Header", box, C_HEADER);
        SetTopStretch(header.GetComponent<RectTransform>(), 0, 80);
        CreateTMP(header, "TitleText", "설정", 40, TextAlignmentOptions.Center, Color.white, true);

        var closeBtn  = CreateButton(header, "CloseButton", C_BTN_CLOSE, "✕", 36);
        var closeRect = closeBtn.GetComponent<RectTransform>();
        closeRect.anchorMin        = new Vector2(1, 0);
        closeRect.anchorMax        = new Vector2(1, 1);
        closeRect.pivot            = new Vector2(1, 0.5f);
        closeRect.anchoredPosition = new Vector2(-8, 0);
        closeRect.sizeDelta        = new Vector2(80, -10);

        // 설정 행들
        float rowY  = -90f;
        const float ROW_H = 70f;
        const float ROW_GAP = 4f;

        var bgmToggle    = BuildSettingsRow(box, "BgmRow",          "BGM",  rowY, ROW_H, C_ROW_EVEN);
        rowY -= ROW_H + ROW_GAP;
        var sfxToggle    = BuildSettingsRow(box, "SfxRow",          "효과음", rowY, ROW_H, C_ROW_ODD);
        rowY -= ROW_H + ROW_GAP;
        var vibToggle    = BuildSettingsRow(box, "VibrationRow",    "진동",  rowY, ROW_H, C_ROW_EVEN);
        rowY -= ROW_H + ROW_GAP;
        var notifToggle  = BuildSettingsRow(box, "NotificationRow", "알림",  rowY, ROW_H, C_ROW_ODD);

        // 개인정보 처리방침 버튼
        var privacyBtn     = CreateButton(box, "PrivacyButton",
                                          new Color(0.18f, 0.26f, 0.35f, 1f),
                                          "개인정보 처리방침 ↗", 26);
        var privacyRect    = privacyBtn.GetComponent<RectTransform>();
        privacyRect.anchorMin        = new Vector2(0, 0);
        privacyRect.anchorMax        = new Vector2(1, 0);
        privacyRect.pivot            = new Vector2(0.5f, 0);
        privacyRect.anchoredPosition = new Vector2(0, 46);
        privacyRect.sizeDelta        = new Vector2(-40, 40);
        privacyRect.offsetMin        = new Vector2(20, privacyRect.offsetMin.y);
        privacyRect.offsetMax        = new Vector2(-20, privacyRect.offsetMax.y);

        // 버전 텍스트
        var verText = CreateTMP(box, "VersionText", "v1.0.0", 24,
                                TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
        SetBottomStretch(verText.GetComponent<RectTransform>(), 10, 30);

        // SettingsPanelUI 컴포넌트 연결
        var panelUI = root.AddComponent<SettingsPanelUI>();
        var so      = new SerializedObject(panelUI);
        so.FindProperty("_bgmToggle").objectReferenceValue          = bgmToggle;
        so.FindProperty("_sfxToggle").objectReferenceValue          = sfxToggle;
        so.FindProperty("_vibrationToggle").objectReferenceValue    = vibToggle;
        so.FindProperty("_notificationToggle").objectReferenceValue = notifToggle;
        so.FindProperty("_privacyButton").objectReferenceValue      = privacyBtn.GetComponent<Button>();
        so.FindProperty("_closeButton").objectReferenceValue        = closeBtn.GetComponent<Button>();
        so.FindProperty("_versionText").objectReferenceValue        = verText.GetComponent<TextMeshProUGUI>();
        so.ApplyModifiedProperties();

        root.SetActive(false);
        return root;
    }

    /// <summary>라벨 + Toggle 한 행 생성. Toggle 컴포넌트를 반환.</summary>
    private static Toggle BuildSettingsRow(
        GameObject parent, string name, string label,
        float anchoredY, float height, Color bgColor)
    {
        var row     = CreateImage(name, parent, bgColor);
        var rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin        = new Vector2(0, 1);
        rowRect.anchorMax        = new Vector2(1, 1);
        rowRect.pivot            = new Vector2(0.5f, 1);
        rowRect.anchoredPosition = new Vector2(0, anchoredY);
        rowRect.sizeDelta        = new Vector2(0, height);
        rowRect.offsetMin        = new Vector2(0, rowRect.offsetMin.y);
        rowRect.offsetMax        = new Vector2(0, rowRect.offsetMax.y);

        // 라벨
        var labelGO   = CreateEmpty("Label", row);
        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin        = new Vector2(0, 0);
        labelRect.anchorMax        = new Vector2(0.7f, 1);
        labelRect.offsetMin        = new Vector2(25, 0);
        labelRect.offsetMax        = Vector2.zero;
        var labelTmp               = labelGO.AddComponent<TextMeshProUGUI>();
        labelTmp.text              = label;
        labelTmp.fontSize          = 32;
        labelTmp.alignment         = TextAlignmentOptions.MidlineLeft;
        labelTmp.color             = Color.white;
        ApplyKoreanFont(labelTmp);

        // Toggle
        var toggleGO   = CreateEmpty("Toggle", row);
        var toggleRect = toggleGO.GetComponent<RectTransform>();
        toggleRect.anchorMin        = new Vector2(0.75f, 0.15f);
        toggleRect.anchorMax        = new Vector2(0.95f, 0.85f);
        toggleRect.offsetMin        = Vector2.zero;
        toggleRect.offsetMax        = Vector2.zero;

        // Toggle 배경
        var bgGO   = CreateEmpty("Background", toggleGO);
        var bgRect = bgGO.GetComponent<RectTransform>();
        StretchFull(bgRect);
        var bgImg  = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        // Checkmark (켜진 상태 표시)
        var checkGO   = CreateEmpty("Checkmark", bgGO);
        var checkRect = checkGO.GetComponent<RectTransform>();
        checkRect.anchorMin        = new Vector2(0.1f, 0.1f);
        checkRect.anchorMax        = new Vector2(0.9f, 0.9f);
        checkRect.offsetMin        = Vector2.zero;
        checkRect.offsetMax        = Vector2.zero;
        var checkImg               = checkGO.AddComponent<Image>();
        checkImg.color             = new Color(0.22f, 0.75f, 0.35f, 1f);

        var toggle            = toggleGO.AddComponent<Toggle>();
        toggle.targetGraphic  = bgImg;
        toggle.graphic        = checkImg;
        toggle.isOn           = true;

        return toggle;
    }

    // ── HomeUIController 자동 연결 ────────────────────────────

    private static void ConnectNavButtons(HomeUIController ctrl, Button[] buttons)
    {
        var so = new SerializedObject(ctrl);
        for (int i = 0; i < NAV_BUTTONS.Length && i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            so.FindProperty(NAV_BUTTONS[i].field).objectReferenceValue = buttons[i];
        }
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(ctrl);
    }

    private static void ConnectToHomeController(
        HomeUIController ctrl,
        GameObject rewardPanel,
        GameObject settingsPanel)
    {
        var so = new SerializedObject(ctrl);

        if (rewardPanel != null)
            so.FindProperty("_rewardPanel").objectReferenceValue = rewardPanel;
        if (settingsPanel != null)
            so.FindProperty("_settingsPanel").objectReferenceValue = settingsPanel;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(ctrl);
    }

    // ── 유틸 ─────────────────────────────────────────────────

    private static GameObject FindCanvas()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Canvas 없음",
                "씬에 'Canvas' 오브젝트가 없습니다.\nMainHome.unity 가 열려 있는지 확인하세요.", "확인");
        }
        return canvas;
    }

    private static HomeUIController FindHomeController()
    {
#if UNITY_2023_1_OR_NEWER
        var ctrl = Object.FindFirstObjectByType<HomeUIController>();
#else
        var ctrl = Object.FindObjectOfType<HomeUIController>();
#endif
        if (ctrl == null)
        {
            EditorUtility.DisplayDialog("HomeUIController 없음",
                "씬에 HomeUIController 컴포넌트를 찾을 수 없습니다.", "확인");
        }
        return ctrl;
    }

    private static void DestroyExisting(GameObject canvas, string name)
    {
        var existing = canvas.transform.Find(name);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);
    }

    // ── UI 생성 헬퍼 ─────────────────────────────────────────

    private static GameObject CreateEmpty(string name, GameObject parent)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        go.AddComponent<RectTransform>();
        GameObjectUtility.SetParentAndAlign(go, parent);
        return go;
    }

    private static GameObject CreateImage(string name, GameObject parent, Color color)
    {
        var go  = CreateEmpty(name, parent);
        go.AddComponent<Image>().color = color;
        return go;
    }

    private static GameObject CreateTMP(
        GameObject parent, string name, string text,
        int size, TextAlignmentOptions align, Color color,
        bool bold = false)
    {
        var go  = CreateEmpty(name, parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text       = text;
        tmp.fontSize   = size;
        tmp.alignment  = align;
        tmp.color      = color;
        tmp.fontStyle  = bold ? FontStyles.Bold : FontStyles.Normal;
        ApplyKoreanFont(tmp);
        return go;
    }

    private static GameObject CreateButton(
        GameObject parent, string name, Color color, string label, int fontSize)
    {
        var go = CreateEmpty(name, parent);
        go.AddComponent<Image>().color = color;
        go.AddComponent<Button>();

        var labelGO = CreateEmpty("Text", go);
        StretchFull(labelGO.GetComponent<RectTransform>());
        var tmp         = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text        = label;
        tmp.fontSize    = fontSize;
        tmp.alignment   = TextAlignmentOptions.Center;
        tmp.color       = Color.white;
        ApplyKoreanFont(tmp);

        return go;
    }

    // ── RectTransform 헬퍼 ────────────────────────────────────

    private static void StretchFull(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    private static void SetTopStretch(RectTransform r, float fromTop, float height)
    {
        r.anchorMin        = new Vector2(0, 1);
        r.anchorMax        = new Vector2(1, 1);
        r.pivot            = new Vector2(0.5f, 1);
        r.anchoredPosition = new Vector2(0, -fromTop);
        r.sizeDelta        = new Vector2(0, height);
    }

    private static void SetBottomStretch(RectTransform r, float fromBottom, float height)
    {
        r.anchorMin        = new Vector2(0, 0);
        r.anchorMax        = new Vector2(1, 0);
        r.pivot            = new Vector2(0.5f, 0);
        r.anchoredPosition = new Vector2(0, fromBottom);
        r.sizeDelta        = new Vector2(0, height);
    }

    // ── 폰트 적용 ─────────────────────────────────────────────

    private static void ApplyKoreanFont(TextMeshProUGUI tmp)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ASSET_PATH);
        if (font != null)
            tmp.font = font;
    }
}
