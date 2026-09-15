using UnityEditor;
using UnityEngine.SceneManagement;

/// <summary>
/// Hako > 검사 > 언어 — 플레이 중에 한국어·영어 화면을 바로 확인한다 (에디터 전용, 빌드에 들어가지 않는다).
///
/// 에디터는 PC 언어를 따르므로 한국어 Windows에서는 영어 화면을 볼 방법이 없어서 만들었다.
/// 고른 언어는 저장 파일에 남는다 — 확인이 끝나면 "기기 언어"로 되돌린다.
/// </summary>
public static class HakoLanguageMenu
{
    private const string MENU = "Hako/검사/언어/";

    [MenuItem(MENU + "한국어",    priority = 140)] private static void Korean()  => Apply("ko");
    [MenuItem(MENU + "영어",      priority = 141)] private static void English() => Apply("en");
    [MenuItem(MENU + "기기 언어", priority = 142)] private static void Device()  => Apply(SettingsData.LANGUAGE_AUTO);

    [MenuItem(MENU + "한국어",    true)]
    [MenuItem(MENU + "영어",      true)]
    [MenuItem(MENU + "기기 언어", true)]
    private static bool CanApply() => EditorApplication.isPlaying && GameManager.Instance != null && !SceneRouter.IsTransitioning;

    private static void Apply(string language)
    {
        GameManager.Instance.Settings.SetLanguage(language);
        SceneRouter.GoToScene(SceneManager.GetActiveScene().name);   // 떠 있는 글자를 새 언어로 다시 그린다
    }
}
