using UnityEditor;

/// <summary>
/// Hako > 검사 > 재화 — 플레이 중에 테스트용 코인·젬을 넣는다 (에디터 전용, 빌드에 들어가지 않는다).
/// 먹이·장식·분양·성장촉진제(젬) 구매를 확인할 때 쓴다. 넣은 즉시 저장된다.
/// </summary>
public static class HakoCurrency
{
    private const string MENU = "Hako/검사/재화/";

    [MenuItem(MENU + "코인 +1,000",  priority = 140)] private static void Coin1k()  => Add(1000, 0);
    [MenuItem(MENU + "코인 +10,000", priority = 141)] private static void Coin10k() => Add(10000, 0);
    [MenuItem(MENU + "젬 +100",      priority = 152)] private static void Gem100()  => Add(0, 100);

    [MenuItem(MENU + "코인 +1,000",  true)]
    [MenuItem(MENU + "코인 +10,000", true)]
    [MenuItem(MENU + "젬 +100",      true)]
    private static bool CanAdd() => EditorApplication.isPlaying && GameManager.Instance != null;

    private static void Add(int coin, int gem) => GameManager.Instance.DebugAddCurrency(coin, gem);
}
