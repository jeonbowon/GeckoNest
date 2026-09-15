using UnityEditor;

/// <summary>
/// Hako > 검사 > 시간 건너뛰기 — 플레이 중에 시간이 흐른 것처럼 만든다 (에디터 전용, 빌드에 들어가지 않는다).
///
/// +6시간 · +24시간 = 내버려 둔 경우. 게이지 감소·경고 깜빡임 확인용 (6시간을 두세 번 누르면 30 이하 경고가 보인다).
///   오프라인 상한(48시간)이 적용된다.
/// +7일 · +2주 · +30일 = 잘 돌봤다고 가정. 8시간씩 나눠 진행하며 배고픔·목마름·청결·기분을 채운다.
///   나이·허물·성장·일일 보상이 건너뛴 기간만큼 실제 순서대로 일어난다 (애정도는 그대로).
/// </summary>
public static class HakoTimeSkip
{
    private const string MENU = "Hako/검사/시간 건너뛰기/";

    [MenuItem(MENU + "+6시간 (내버려 둠)",  priority = 120)] private static void Skip6h()  => Skip(6f,  false);
    [MenuItem(MENU + "+24시간 (내버려 둠)", priority = 121)] private static void Skip24h() => Skip(24f, false);
    [MenuItem(MENU + "+7일 (잘 돌봄)",      priority = 132)] private static void Skip7d()  => Skip(24f * 7f,  true);
    [MenuItem(MENU + "+2주 (잘 돌봄)",      priority = 133)] private static void Skip14d() => Skip(24f * 14f, true);
    [MenuItem(MENU + "+30일 (잘 돌봄)",     priority = 134)] private static void Skip30d() => Skip(24f * 30f, true);

    [MenuItem(MENU + "+6시간 (내버려 둠)",  true)]
    [MenuItem(MENU + "+24시간 (내버려 둠)", true)]
    [MenuItem(MENU + "+7일 (잘 돌봄)",      true)]
    [MenuItem(MENU + "+2주 (잘 돌봄)",      true)]
    [MenuItem(MENU + "+30일 (잘 돌봄)",     true)]
    private static bool CanSkip() => EditorApplication.isPlaying && GameManager.Instance != null;

    private static void Skip(float hours, bool caredFor) => GameManager.Instance.DebugSkipTime(hours, caredFor);
}
