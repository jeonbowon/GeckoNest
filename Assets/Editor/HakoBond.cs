using UnityEditor;

/// <summary>
/// Hako > 검사 > 유대 — 플레이 중에 선택한 게코(홈에 있는 게코)의 유대 점수를 바꾼다 (에디터 전용).
/// 인사·부르기·재롱·손바닥처럼 높은 레벨에서 풀리는 것을 바로 확인할 때 쓴다. 바꾼 즉시 저장된다.
/// 레벨이 오르면 평소처럼 보상이 들어오고 홈 화면에서 레벨업 연출이 나온다 (인사는 홈에 다시 들어올 때).
/// </summary>
public static class HakoBond
{
    private const string MENU = "Hako/검사/유대/";

    [MenuItem(MENU + "다음 레벨까지",  priority = 160)] private static void NextLevel() => GameManager.Instance.DebugBondNextLevel();
    [MenuItem(MENU + "점수 +20",       priority = 171)] private static void Add20()     => GameManager.Instance.DebugAddBond(20f);
    [MenuItem(MENU + "점수 +100",      priority = 172)] private static void Add100()    => GameManager.Instance.DebugAddBond(100f);
    [MenuItem(MENU + "최고 레벨 (Lv.5)", priority = 173)] private static void Max()     => GameManager.Instance.DebugAddBond(GeckoBond.LEVEL_POINTS[GeckoBond.MAX_LEVEL]);
    [MenuItem(MENU + "처음으로 (0점)", priority = 184)] private static void Reset()     => GameManager.Instance.DebugResetBond();

    [MenuItem(MENU + "다음 레벨까지",  true)]
    [MenuItem(MENU + "점수 +20",       true)]
    [MenuItem(MENU + "점수 +100",      true)]
    [MenuItem(MENU + "최고 레벨 (Lv.5)", true)]
    [MenuItem(MENU + "처음으로 (0점)", true)]
    private static bool CanUse() => EditorApplication.isPlaying && GameManager.Instance != null && GameManager.Instance.GetSelectedGecko() != null;
}
