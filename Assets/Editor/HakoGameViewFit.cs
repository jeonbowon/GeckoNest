using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Game 창을 실제 기기(1080x2400 세로)와 같게 맞추고 확대를 1배로 되돌린다.
///
/// 왜 필요한가 (2026-09-21): Game 창이 Free Aspect + 1.22배 확대 + 옆으로 밀린 상태로 저장돼,
/// 게코 이름·코인·젬·돌봄 버튼·하단 탭이 **화면 밖으로 잘려** 안 보였다. 씬·코드는 멀쩡했다.
/// Game 창 위에서 Ctrl/Alt + 마우스 휠을 굴리면 이렇게 확대되고, 이 상태는 Unity를 다시 켜도 남는다.
///
/// - 메뉴 `Hako > 화면 > 게임 화면 맞추기 (1080x2400)` — 언제든 다시 맞춘다
/// - 이 스크립트가 처음 컴파일되면 한 번 저절로 실행된다 (FIT_VERSION을 올리면 다시 한 번)
/// - Game 창은 Unity 내부 기능이라 리플렉션으로 부른다. 어느 단계가 안 되면 콘솔에 남기고 넘어간다
/// </summary>
[InitializeOnLoad]
public static class HakoGameViewFit
{
    private const int    WIDTH       = 1080;
    private const int    HEIGHT      = 2400;
    private const string LABEL       = "Hako 1080x2400 Portrait";
    private const int    FIT_VERSION = 1;
    private const string PREF_KEY    = "Hako.GameViewFit.Version";

    private const BindingFlags ANY = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    static HakoGameViewFit()
    {
        if (Application.isBatchMode) return;
        if (EditorPrefs.GetInt(PREF_KEY, 0) >= FIT_VERSION) return;

        // 창이 다 뜬 뒤에 (컴파일 직후에는 Game 창이 아직 준비되지 않았을 수 있다)
        EditorApplication.delayCall += () =>
        {
            if (Fit(logResult: true)) EditorPrefs.SetInt(PREF_KEY, FIT_VERSION);
        };
    }

    [MenuItem("Hako/화면/게임 화면 맞추기 (1080x2400)")]
    private static void FitMenu() => Fit(logResult: true);

    /// <summary>세로 1080x2400을 고르고 확대를 화면에 딱 맞게. 해냈으면 true</summary>
    public static bool Fit(bool logResult)
    {
        var editorAsm    = typeof(Editor).Assembly;
        var gameViewType = editorAsm.GetType("UnityEditor.GameView");
        var sizesType    = editorAsm.GetType("UnityEditor.GameViewSizes");
        var sizeType     = editorAsm.GetType("UnityEditor.GameViewSize");
        var sizeKindType = editorAsm.GetType("UnityEditor.GameViewSizeType");
        if (gameViewType == null || sizesType == null || sizeType == null || sizeKindType == null)
            return Fail("Unity 내부 Game 창 형식을 찾지 못함");

        var view = Resources.FindObjectsOfTypeAll(gameViewType).FirstOrDefault() as EditorWindow;
        if (view == null) return Fail("열린 Game 창이 없음 — Window > General > Game을 연 뒤 메뉴를 다시 누르세요");

        // ① 해상도 목록에 1080x2400 세로를 넣는다 (지금 플랫폼 묶음, 이미 있으면 그대로)
        var instance = typeof(ScriptableSingleton<>).MakeGenericType(sizesType)
                                                   .GetProperty("instance", ANY)?.GetValue(null);
        if (instance == null) return Fail("해상도 목록을 열지 못함");

        var groupTypeProp = sizesType.GetProperty("currentGroupType", ANY);
        object groupType  = groupTypeProp?.GetValue(groupTypeProp.GetGetMethod(true).IsStatic ? null : instance);
        if (groupType == null) return Fail("지금 플랫폼의 해상도 묶음을 알 수 없음");

        var group = sizesType.GetMethod("GetGroup", ANY)?.Invoke(instance, new[] { groupType });
        if (group == null) return Fail("해상도 묶음을 열지 못함");

        int index = FindSize(group, WIDTH, HEIGHT);
        if (index < 0)
        {
            var fixedKind = Enum.ToObject(sizeKindType, 1);   // FixedResolution
            var size      = Activator.CreateInstance(sizeType, ANY, null,
                                                     new object[] { fixedKind, WIDTH, HEIGHT, LABEL }, null);
            group.GetType().GetMethod("AddCustomSize", ANY)?.Invoke(group, new[] { size });
            sizesType.GetMethod("SaveToHDD", ANY)?.Invoke(instance, null);
            index = FindSize(group, WIDTH, HEIGHT);
        }
        if (index < 0) return Fail("1080x2400 해상도를 목록에 넣지 못함");

        // ② 그 해상도를 고른다 — 드롭다운에서 고른 것과 같은 경로
        var select = gameViewType.GetMethod("SizeSelectionCallback", ANY);
        if (select != null) select.Invoke(view, new object[] { index, null });
        else gameViewType.GetProperty("selectedSizeIndex", ANY)?.SetValue(view, index);

        // ③ 확대를 "화면에 딱 맞게"로 — 고른 해상도 기준으로 다시 계산한 뒤 그 값으로
        gameViewType.GetMethod("UpdateZoomAreaAndParent", ANY)?.Invoke(view, null);
        var defaultScale = gameViewType.GetField("m_defaultScale", ANY)?.GetValue(view);
        var snap         = gameViewType.GetMethod("SnapZoom", ANY);
        if (snap == null || defaultScale == null) return Fail("확대 배율을 되돌리지 못함 (해상도는 골랐음)");
        snap.Invoke(view, new[] { defaultScale });

        view.Repaint();
        if (logResult)
            Debug.Log($"[HakoGameViewFit] Game 창 = {WIDTH}x{HEIGHT} 세로 · 확대 {(float)defaultScale:0.##}배 (화면에 딱 맞게)");
        return true;
    }

    // 묶음 안에서 가로·세로가 같은 고정 해상도의 번호 (없으면 -1)
    private static int FindSize(object group, int width, int height)
    {
        var t     = group.GetType();
        int count = (int)t.GetMethod("GetTotalCount", ANY).Invoke(group, null);
        var get   = t.GetMethod("GetGameViewSize", ANY);
        for (int i = 0; i < count; i++)
        {
            var s  = get.Invoke(group, new object[] { i });
            var st = s.GetType();
            int w  = (int)st.GetProperty("width",  ANY).GetValue(s);
            int h  = (int)st.GetProperty("height", ANY).GetValue(s);
            int k  = Convert.ToInt32(st.GetProperty("sizeType", ANY).GetValue(s));
            if (w == width && h == height && k == 1) return i;
        }
        return -1;
    }

    private static bool Fail(string why)
    {
        Debug.LogWarning("[HakoGameViewFit] " + why);
        return false;
    }
}
