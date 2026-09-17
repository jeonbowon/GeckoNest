using UnityEditor;

/// <summary>
/// Hako > 검사 > 모프 — 플레이 중에 선택한 어덜트 게코(홈에 있는 게코)의 모프를 바꾼다 (에디터 전용).
/// 모프마다 색·무늬가 어떻게 보이는지 확인할 때 쓴다. 도감에 기록되지만 보상은 없다. 바로 저장된다.
/// 어덜트가 아니면 회색 — 먼저 시간 건너뛰기(잘 돌봄)로 키운다.
/// </summary>
public static class HakoMorph
{
    private const string MENU = "Hako/검사/모프/";

    [MenuItem(MENU + "다음 모프로 (차례로)", priority = 190)] private static void Next()   => GameManager.Instance.DebugChangeMorph(next: true);
    [MenuItem(MENU + "다시 뽑기 (확률대로)", priority = 191)] private static void Reroll() => GameManager.Instance.DebugChangeMorph(next: false);

    [MenuItem(MENU + "다음 모프로 (차례로)", true)]
    [MenuItem(MENU + "다시 뽑기 (확률대로)", true)]
    private static bool CanUse()
    {
        if (!EditorApplication.isPlaying || GameManager.Instance == null) return false;
        var g = GameManager.Instance.GetSelectedGecko();
        return g != null && GeckoManager.IsAdult(g);
    }
}
