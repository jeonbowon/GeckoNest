using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Hako > Gecko 메뉴
///   ① 프록시 게코 만들기 (현재 씬)       — MainHome의 GeckoObject를 움직이는 게코로 조립
///   ② 선택한 PSD·폴더로 스킨 만들기     — 최종 아트 → GeckoSkin 에셋
///   ③ 선택한 스킨을 씬 게코에 적용       — 그림 교체
/// </summary>
public static class GeckoRigBuilder
{
    private const string MENU    = "Hako/Gecko/";
    private const float  START_Y = 440f;   // 발 높이 (GeckoArea 아래 끝 기준, UI 단위)

    // ── ① ─────────────────────────────────────────────────────

    [MenuItem(MENU + "① 프록시 게코 만들기 (현재 씬)", priority = 1)]
    public static void BuildProxyGecko()
    {
        var area = FindRect("GeckoArea");
        if (area == null)
        {
            EditorUtility.DisplayDialog("프록시 게코 만들기",
                "현재 씬에서 'GeckoArea'를 찾지 못했습니다.\nMainHome 씬을 연 뒤 다시 실행해 주십시오.", "확인");
            return;
        }

        GeckoSkin skin;
        try
        {
            EditorUtility.DisplayProgressBar("프록시 게코", "임시 그림 26장 그리는 중…", 0.4f);
            skin = GeckoProxyArt.Generate();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
        if (skin == null) return;

        Undo.SetCurrentGroupName("Build Proxy Gecko");
        int group = Undo.GetCurrentGroup();
        Undo.RegisterFullObjectHierarchyUndo(area.gameObject, "Build Proxy Gecko");

        var geckoT = area.Find("GeckoObject") as RectTransform;
        if (geckoT == null)
        {
            var go = new GameObject("GeckoObject", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create GeckoObject");
            geckoT = (RectTransform)go.transform;
            geckoT.SetParent(area, false);
        }
        var gecko = geckoT.gameObject;

        // 클립 없이 비어 있던 Animator는 더 이상 쓰지 않는다
        var animator = gecko.GetComponent<Animator>();
        if (animator != null) Undo.DestroyObjectImmediate(animator);

        geckoT.anchorMin = geckoT.anchorMax = new Vector2(0.5f, 0f);
        geckoT.pivot            = new Vector2(0.5f, 0f);
        geckoT.anchoredPosition = new Vector2(0f, START_Y);
        geckoT.sizeDelta        = new Vector2(640f, 320f);
        geckoT.localRotation    = Quaternion.identity;
        geckoT.localScale       = Vector3.one;

        // 하위 캔버스 — 게코가 매 프레임 움직여도 홈 화면 전체 UI를 다시 계산하지 않게 한다
        if (gecko.GetComponent<Canvas>() == null) Undo.AddComponent<Canvas>(gecko);

        var rig        = GetOrAdd<GeckoRig>(gecko);
        var motor      = GetOrAdd<GeckoMotor>(gecko);
        var controller = GetOrAdd<GeckoAnimatorController>(gecko);
        var ai         = GetOrAdd<GeckoMovementAI>(gecko);

        bool hadVisual = geckoT.Find("Visual") != null;
        SetField(rig, "_skin", skin);
        rig.EnsureParts();
        if (!hadVisual && rig.Visual != null) Undo.RegisterCreatedObjectUndo(rig.Visual.gameObject, "Create Gecko Visual");
        rig.ApplySkin();
        rig.SolveRest();

        SetField(controller, "_motor", motor);

        // 그리는 순서: 장식보다 앞, 허물 배지보다 뒤
        geckoT.SetAsLastSibling();
        var badge = area.Find("MoltBadge");
        if (badge != null) badge.SetAsLastSibling();

        // 홈 화면과 연결
        var home = Object.FindFirstObjectByType<HomeUIController>(FindObjectsInactive.Include);
        if (home != null)
        {
            SetField(home, "_geckoAnimator", controller);
            SetField(home, "_geckoMovement", ai);
        }

        EditorUtility.SetDirty(rig);
        EditorSceneManager.MarkSceneDirty(gecko.scene);
        Undo.CollapseUndoOperations(group);
        Selection.activeGameObject = gecko;
        EditorGUIUtility.PingObject(gecko);

        var msg = new StringBuilder();
        msg.AppendLine("회색 프록시 게코를 조립했습니다.");
        msg.AppendLine();
        msg.AppendLine("• 그림 26장: " + GeckoProxyArt.TEX_DIR);
        msg.AppendLine("• 스킨: " + GeckoProxyArt.SKIN_PATH);
        msg.AppendLine("• 씬: GeckoArea / GeckoObject");
        msg.AppendLine(home != null ? "• HomeUIController 연결 완료" : "▲ HomeUIController를 찾지 못해 연결하지 못했습니다");
        msg.AppendLine();
        msg.AppendLine("다음 순서:");
        msg.AppendLine("1) Ctrl+S로 씬 저장");
        msg.AppendLine("2) Boot 씬을 열고 실행");
        msg.AppendLine("3) 실행 중 Hierarchy에서 GeckoObject 선택 →");
        msg.AppendLine("   Inspector의 GeckoMotor 아래 버튼으로 동작을 하나씩 확인");
        EditorUtility.DisplayDialog("프록시 게코 만들기", msg.ToString(), "확인");
    }

    // ── ② ─────────────────────────────────────────────────────

    [MenuItem(MENU + "② 선택한 PSD·폴더로 스킨 만들기", priority = 2)]
    public static void BuildSkinFromSelection()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        var report = new StringBuilder();
        var skin = GeckoSkinImporter.Build(path, report);
        if (skin != null)
        {
            Selection.activeObject = skin;
            EditorGUIUtility.PingObject(skin);
        }
        EditorUtility.DisplayDialog("스킨 만들기", report.ToString(), "확인");
    }

    [MenuItem(MENU + "② 선택한 PSD·폴더로 스킨 만들기", true)]
    private static bool ValidateBuildSkin()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(path)) return false;
        string lower = path.ToLowerInvariant();
        return AssetDatabase.IsValidFolder(path) || lower.EndsWith(".psd") || lower.EndsWith(".psb");
    }

    // ── ③ ─────────────────────────────────────────────────────

    [MenuItem(MENU + "③ 선택한 스킨을 씬 게코에 적용", priority = 3)]
    public static void ApplySelectedSkin()
    {
        var skin = Selection.activeObject as GeckoSkin;
        var rig = Object.FindFirstObjectByType<GeckoRig>(FindObjectsInactive.Include);
        if (skin == null || rig == null)
        {
            EditorUtility.DisplayDialog("스킨 적용",
                rig == null ? "씬에 게코가 없습니다. 먼저 ① 프록시 게코 만들기를 실행해 주십시오."
                            : "Project 창에서 GeckoSkin 에셋을 선택한 뒤 실행해 주십시오.", "확인");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(rig.gameObject, "Apply Gecko Skin");
        SetField(rig, "_skin", skin);
        rig.EnsureParts();
        rig.ApplySkin();
        rig.SolveRest();
        EditorSceneManager.MarkSceneDirty(rig.gameObject.scene);
        Selection.activeGameObject = rig.gameObject;
    }

    [MenuItem(MENU + "③ 선택한 스킨을 씬 게코에 적용", true)]
    private static bool ValidateApplySkin() => Selection.activeObject is GeckoSkin;

    // ── 내부 ──────────────────────────────────────────────────

    private static RectTransform FindRect(string name)
    {
        var scene = SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
            foreach (var rt in root.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == name) return rt;
        return null;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : Undo.AddComponent<T>(go);
    }

    private static void SetField(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop == null)
        {
            Debug.LogWarning($"[GeckoRigBuilder] {target.GetType().Name}.{field} 필드를 찾지 못했습니다.");
            return;
        }
        prop.objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }
}
