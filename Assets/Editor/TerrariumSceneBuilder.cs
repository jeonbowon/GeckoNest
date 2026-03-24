using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using TMPro.EditorUtilities;

/// <summary>
/// Hako 메뉴에서 실행:
///   ① "Hako/Terrarium/① 씬 UI 생성"   — Terrarium.unity 전체 Canvas 자동 생성 + 레퍼런스 자동 연결
///   ② "Hako/Terrarium/② DecorSlot 프리팹 생성" — Assets/_Game/Prefabs/UI/DecorSlot.prefab 생성
/// </summary>
public static class TerrariumSceneBuilder
{
    // ── 한글 폰트 에셋 경로 ───────────────────────────────────
    // Font Asset Creator로 생성한 SDF 에셋 경로. 폰트 교체 시 이 값만 수정.
    private const string FONT_ASSET_PATH = "Assets/_Game/Fonts/NanumGothic-Regular SDF.asset";

    // ── 색상 팔레트 ───────────────────────────────────────────
    private static readonly Color C_BG        = new Color(0.10f, 0.13f, 0.10f, 1.00f);
    private static readonly Color C_PANEL     = new Color(0.15f, 0.18f, 0.15f, 0.95f);
    private static readonly Color C_TOPBAR    = new Color(0.12f, 0.15f, 0.12f, 1.00f);
    private static readonly Color C_TAB       = new Color(0.20f, 0.40f, 0.25f, 1.00f);
    private static readonly Color C_BTN_BACK  = new Color(0.25f, 0.50f, 0.30f, 1.00f);
    private static readonly Color C_ERROR     = new Color(0.70f, 0.15f, 0.15f, 0.95f);
    private static readonly Color C_SCROLL_BG = new Color(0.08f, 0.10f, 0.08f, 0.80f);

    // ── ① 씬 UI 생성 ─────────────────────────────────────────

    [MenuItem("Hako/Terrarium/① 씬 UI 생성")]
    public static void BuildScene()
    {
        if (!EditorUtility.DisplayDialog(
            "Terrarium 씬 UI 자동 생성",
            "현재 씬에 Terrarium UI 전체를 생성합니다.\n기존 'Canvas' 오브젝트는 삭제 후 재생성됩니다.\n\n계속하시겠습니까?",
            "생성", "취소"))
            return;

        Undo.SetCurrentGroupName("Build Terrarium Scene UI");
        int group = Undo.GetCurrentGroup();

        EnsureEventSystem();

        // 기존 Canvas 제거
        var existing = GameObject.Find("Canvas");
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);

        // ── Canvas ────────────────────────────────────────────
        var canvas = BuildCanvas();

        // ── 배경 이미지 ───────────────────────────────────────
        var bg = CreateImage("Background", canvas, C_BG);
        StretchFull(bg.GetComponent<RectTransform>());

        // ── UI 레이어 오브젝트들 ──────────────────────────────
        var topBar     = BuildTopBar(canvas);
        var tabBar     = BuildTabBar(canvas);
        var scrollView = BuildScrollView(canvas);
        var backBtn    = BuildBackButton(canvas);
        var errorPanel = BuildErrorPanel(canvas);

        // ── TerrariumController (컴포넌트 부착 오브젝트) ──────
        var ctrlGO = CreateEmpty("TerrariumController", canvas);
        StretchFull(ctrlGO.GetComponent<RectTransform>());
        var controller = ctrlGO.AddComponent<TerrariumUIController>();

        // ── SerializeField 자동 연결 ──────────────────────────
        ConnectReferences(controller, topBar, tabBar, scrollView, backBtn, errorPanel);

        // ── 씬 Dirty 마킹 ─────────────────────────────────────
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Selection.activeGameObject = ctrlGO;
        EditorGUIUtility.PingObject(ctrlGO);

        Debug.Log("[TerrariumSceneBuilder] 씬 UI 생성 완료.\n" +
                  "남은 수동 작업 (Inspector):\n" +
                  "  - Decor Slot Prefab : Assets/_Game/Prefabs/UI/DecorSlot.prefab\n" +
                  "  - All Decor Items   : DecorItemSO 에셋 전부 드래그");
    }

    // ── ② DecorSlot 프리팹 생성 ───────────────────────────────

    [MenuItem("Hako/Terrarium/② DecorSlot 프리팹 생성")]
    public static void BuildDecorSlotPrefab()
    {
        const string PATH = "Assets/_Game/Prefabs/UI/DecorSlot.prefab";

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PATH) != null)
        {
            if (!EditorUtility.DisplayDialog("DecorSlot 프리팹",
                $"이미 존재합니다:\n{PATH}\n\n덮어쓰시겠습니까?",
                "덮어쓰기", "취소"))
                return;
        }

        // 임시 씬 오브젝트 생성 후 프리팹으로 저장
        var root = BuildDecorSlotObject();

        bool saved;
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PATH, out saved);
        Object.DestroyImmediate(root);

        if (saved)
        {
            AssetDatabase.Refresh();
            Debug.Log($"[TerrariumSceneBuilder] DecorSlot 프리팹 저장 완료: {PATH}");
            EditorGUIUtility.PingObject(prefab);
        }
        else
        {
            Debug.LogError("[TerrariumSceneBuilder] 프리팹 저장 실패");
        }
    }

    // ── Canvas ────────────────────────────────────────────────

    private static GameObject BuildCanvas()
    {
        var go = new GameObject("Canvas");
        Undo.RegisterCreatedObjectUndo(go, "Create Canvas");

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode            = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution    = new Vector2(1080, 1920);
        scaler.screenMatchMode        = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight     = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    // ── TopBar ────────────────────────────────────────────────

    private static GameObject BuildTopBar(GameObject parent)
    {
        var bar = CreateImage("TopBar", parent, C_TOPBAR);
        SetTopStretch(bar.GetComponent<RectTransform>(), offsetFromTop: 0, height: 110);

        var hlg = bar.AddComponent<HorizontalLayoutGroup>();
        hlg.padding               = new RectOffset(40, 40, 15, 15);
        hlg.spacing               = 20;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment         = TextAnchor.MiddleCenter;

        CreateTMP(bar, "CoinText",  "💰 0",   38, TextAlignmentOptions.Left);
        CreateTMP(bar, "GemText",   "💎 0",   38, TextAlignmentOptions.Right);

        return bar;
    }

    // ── TabBar ────────────────────────────────────────────────

    private static GameObject BuildTabBar(GameObject parent)
    {
        var bar = CreateImage("TabBar", parent, C_PANEL);
        SetTopStretch(bar.GetComponent<RectTransform>(), offsetFromTop: 110, height: 90);

        var hlg = bar.AddComponent<HorizontalLayoutGroup>();
        hlg.padding               = new RectOffset(10, 10, 10, 10);
        hlg.spacing               = 8;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;

        CreateButton(bar, "BgTabButton",    "배경",  C_TAB, 34);
        CreateButton(bar, "FloorTabButton", "바닥",  C_TAB, 34);
        CreateButton(bar, "DecorTabButton", "장식",  C_TAB, 34);

        return bar;
    }

    // ── ScrollView ────────────────────────────────────────────

    private static GameObject BuildScrollView(GameObject parent)
    {
        // 전체 영역에서 TopBar(110) + TabBar(90) = 200 위에서, BackButton(110) 아래에서
        var svGO = CreateEmpty("ScrollView", parent);
        var svRect = svGO.GetComponent<RectTransform>();
        svRect.anchorMin = new Vector2(0, 0);
        svRect.anchorMax = new Vector2(1, 1);
        svRect.offsetMin = new Vector2(0,  110);   // 아래: BackButton 높이
        svRect.offsetMax = new Vector2(0, -200);   // 위:   TopBar + TabBar

        var bgImg   = svGO.AddComponent<Image>();
        bgImg.color = C_SCROLL_BG;

        var scroll  = svGO.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical   = true;
        scroll.scrollSensitivity = 30f;

        // Viewport
        var viewportGO   = CreateEmpty("Viewport", svGO);
        var viewportRect = viewportGO.GetComponent<RectTransform>();
        StretchFull(viewportRect);
        viewportGO.AddComponent<RectMask2D>();
        scroll.viewport = viewportRect;

        // Content
        var contentGO   = CreateEmpty("Content", viewportGO);
        var contentRect = contentGO.GetComponent<RectTransform>();
        contentRect.anchorMin        = new Vector2(0, 1);
        contentRect.anchorMax        = new Vector2(1, 1);
        contentRect.pivot            = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta        = new Vector2(0, 0);

        var grid = contentGO.AddComponent<GridLayoutGroup>();
        grid.cellSize        = new Vector2(320, 380);
        grid.spacing         = new Vector2(12, 12);
        grid.padding         = new RectOffset(20, 20, 20, 20);
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.startCorner     = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis       = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment  = TextAnchor.UpperCenter;

        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = contentRect;

        return svGO;
    }

    // ── BackButton ────────────────────────────────────────────

    private static GameObject BuildBackButton(GameObject parent)
    {
        var btn = CreateButton(parent, "BackButton", "← 홈으로", C_BTN_BACK, 38);

        var rect       = btn.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot     = new Vector2(0.5f, 0);
        rect.anchoredPosition = new Vector2(0, 0);
        rect.sizeDelta        = new Vector2(0, 110);
        rect.offsetMin        = new Vector2(30, rect.offsetMin.y);
        rect.offsetMax        = new Vector2(-30, rect.offsetMax.y);

        return btn;
    }

    // ── ErrorPanel ────────────────────────────────────────────

    private static GameObject BuildErrorPanel(GameObject parent)
    {
        var panel = CreateImage("ErrorPanel", parent, C_ERROR);
        var rect  = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.05f, 0.42f);
        rect.anchorMax = new Vector2(0.95f, 0.58f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // 둥근 모서리 효과용 패딩 레이아웃
        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding               = new RectOffset(25, 25, 15, 15);
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = true;
        vlg.childAlignment         = TextAnchor.MiddleCenter;

        var text = CreateTMP(panel, "ErrorText", "오류 메시지", 34, TextAlignmentOptions.Center);
        StretchFull(text.GetComponent<RectTransform>());

        panel.SetActive(false);
        return panel;
    }

    // ── DecorSlot 오브젝트 빌드 (프리팹용) ───────────────────

    private static GameObject BuildDecorSlotObject()
    {
        // 루트
        var root = new GameObject("DecorSlot");
        root.AddComponent<RectTransform>().sizeDelta = new Vector2(320, 380);

        // 배경 이미지 (루트)
        var rootImg   = root.AddComponent<Image>();
        rootImg.color = new Color(0.18f, 0.22f, 0.18f, 1f);

        // SelectButton — 전체 덮는 투명 버튼
        var btnGO   = new GameObject("SelectButton");
        btnGO.transform.SetParent(root.transform, false);
        var btnRect = btnGO.AddComponent<RectTransform>();
        StretchFull(btnRect);

        var btnImg   = btnGO.AddComponent<Image>();
        btnImg.color = new Color(1, 1, 1, 0.01f); // 거의 투명, Raycast는 받음

        var btn           = btnGO.AddComponent<Button>();
        var btnColors     = btn.colors;
        btnColors.highlightedColor = new Color(0.6f, 0.9f, 0.6f, 0.5f);
        btn.colors        = btnColors;

        // IconImage — 상단 60%
        var iconGO   = new GameObject("IconImage");
        iconGO.transform.SetParent(root.transform, false);
        var iconRect = iconGO.AddComponent<RectTransform>();
        iconRect.anchorMin        = new Vector2(0.1f, 0.35f);
        iconRect.anchorMax        = new Vector2(0.9f, 0.95f);
        iconRect.offsetMin        = Vector2.zero;
        iconRect.offsetMax        = Vector2.zero;
        var iconImg               = iconGO.AddComponent<Image>();
        iconImg.color             = Color.white;
        iconImg.preserveAspect   = true;

        // NameText — 중단
        var nameGO   = new GameObject("NameText");
        nameGO.transform.SetParent(root.transform, false);
        var nameRect = nameGO.AddComponent<RectTransform>();
        nameRect.anchorMin        = new Vector2(0, 0.20f);
        nameRect.anchorMax        = new Vector2(1, 0.38f);
        nameRect.offsetMin        = new Vector2(8, 0);
        nameRect.offsetMax        = new Vector2(-8, 0);
        var nameTmp               = nameGO.AddComponent<TextMeshProUGUI>();
        nameTmp.text              = "아이템명";
        nameTmp.fontSize          = 28;
        nameTmp.alignment         = TextAlignmentOptions.Center;
        nameTmp.color             = Color.white;
        ApplyKoreanFont(nameTmp);

        // PriceText — 하단
        var priceGO   = new GameObject("PriceText");
        priceGO.transform.SetParent(root.transform, false);
        var priceRect = priceGO.AddComponent<RectTransform>();
        priceRect.anchorMin        = new Vector2(0, 0.02f);
        priceRect.anchorMax        = new Vector2(1, 0.20f);
        priceRect.offsetMin        = new Vector2(8, 0);
        priceRect.offsetMax        = new Vector2(-8, 0);
        var priceTmp               = priceGO.AddComponent<TextMeshProUGUI>();
        priceTmp.text              = "Free";
        priceTmp.fontSize          = 26;
        priceTmp.alignment         = TextAlignmentOptions.Center;
        priceTmp.color             = new Color(1f, 0.85f, 0.3f);
        ApplyKoreanFont(priceTmp);

        // DecorSlotUI 컴포넌트 연결
        var slotUI            = root.AddComponent<DecorSlotUI>();
        var so                = new SerializedObject(slotUI);
        so.FindProperty("_iconImage").objectReferenceValue    = iconImg;
        so.FindProperty("_nameText").objectReferenceValue     = nameTmp;
        so.FindProperty("_priceText").objectReferenceValue    = priceTmp;
        so.FindProperty("_selectButton").objectReferenceValue = btn;
        so.ApplyModifiedProperties();

        return root;
    }

    // ── SerializeField 자동 연결 ──────────────────────────────

    private static void ConnectReferences(
        TerrariumUIController controller,
        GameObject topBar, GameObject tabBar,
        GameObject scrollView, GameObject backBtn,
        GameObject errorPanel)
    {
        var so = new SerializedObject(controller);

        // 상단 바
        var tmps = topBar.GetComponentsInChildren<TextMeshProUGUI>();
        if (tmps.Length >= 2)
        {
            so.FindProperty("_coinText").objectReferenceValue = tmps[0];
            so.FindProperty("_gemText").objectReferenceValue  = tmps[1];
        }

        // 탭 버튼
        so.FindProperty("_bgTabButton").objectReferenceValue =
            tabBar.transform.Find("BgTabButton")?.GetComponent<Button>();
        so.FindProperty("_floorTabButton").objectReferenceValue =
            tabBar.transform.Find("FloorTabButton")?.GetComponent<Button>();
        so.FindProperty("_decorTabButton").objectReferenceValue =
            tabBar.transform.Find("DecorTabButton")?.GetComponent<Button>();

        // 스크롤 Content
        var content = scrollView.transform.Find("Viewport/Content");
        so.FindProperty("_itemListContent").objectReferenceValue = content;

        // 뒤로가기 버튼
        so.FindProperty("_backButton").objectReferenceValue =
            backBtn.GetComponent<Button>();

        // 에러 패널
        so.FindProperty("_errorPanel").objectReferenceValue = errorPanel;
        so.FindProperty("_errorText").objectReferenceValue  =
            errorPanel.GetComponentInChildren<TextMeshProUGUI>();

        // DecorSlot 프리팹 — 에셋 경로에서 자동 로드 시도
        const string PREFAB_PATH = "Assets/_Game/Prefabs/UI/DecorSlot.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        if (prefab != null)
            so.FindProperty("_decorSlotPrefab").objectReferenceValue = prefab;
        else
            Debug.LogWarning($"[TerrariumSceneBuilder] DecorSlot.prefab 없음 — 메뉴 ②를 먼저 실행하세요.\n경로: {PREFAB_PATH}");

        so.ApplyModifiedProperties();
    }

    // ── 공통 UI 생성 헬퍼 ────────────────────────────────────

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
        var img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    private static TextMeshProUGUI CreateTMP(GameObject parent, string name, string text, int size, TextAlignmentOptions align)
    {
        var go  = CreateEmpty(name, parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.alignment = align;
        tmp.color     = Color.white;
        ApplyKoreanFont(tmp);
        return tmp;
    }

    /// <summary>
    /// FONT_ASSET_PATH 의 TMP_FontAsset 을 적용. 에셋이 없으면 경고만 출력하고 기본 폰트 유지.
    /// </summary>
    private static void ApplyKoreanFont(TextMeshProUGUI tmp)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ASSET_PATH);
        if (font != null)
        {
            tmp.font = font;
        }
        else
        {
            Debug.LogWarning($"[TerrariumSceneBuilder] 한글 폰트 에셋 없음 — 기본 폰트 사용.\n" +
                             $"경로: {FONT_ASSET_PATH}\n" +
                             $"Window → TextMeshPro → Font Asset Creator 로 생성 후 재실행하세요.");
        }
    }

    private static GameObject CreateButton(GameObject parent, string name, string label, Color color, int fontSize)
    {
        var go  = CreateEmpty(name, parent);
        var img = go.AddComponent<Image>();
        img.color = color;
        go.AddComponent<Button>();

        var labelGO  = CreateEmpty("Text", go);
        var tmp      = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        ApplyKoreanFont(tmp);
        StretchFull(labelGO.GetComponent<RectTransform>());

        return go;
    }

    // ── RectTransform 헬퍼 ────────────────────────────────────

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetTopStretch(RectTransform rect, float offsetFromTop, float height)
    {
        rect.anchorMin        = new Vector2(0, 1);
        rect.anchorMax        = new Vector2(1, 1);
        rect.pivot            = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0, -offsetFromTop);
        rect.sizeDelta        = new Vector2(0, height);
    }

    // ── EventSystem ───────────────────────────────────────────

    private static void EnsureEventSystem()
    {
#if UNITY_2023_1_OR_NEWER
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
#else
        if (Object.FindObjectOfType<EventSystem>() != null) return;
#endif
        var es = new GameObject("EventSystem");
        Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }
}
