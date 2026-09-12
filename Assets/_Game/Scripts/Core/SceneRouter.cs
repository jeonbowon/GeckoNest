using UnityEngine.SceneManagement;

/// <summary>씬 전환은 반드시 여기서만. 화면 전환은 SceneFader로 부드럽게 넘어간다.</summary>
public static class SceneRouter
{
    private const string SCENE_HOME       = "MainHome";
    private const string SCENE_STORE      = "Store";
    private const string SCENE_GECKO_LIST = "GeckoList";
    private const string SCENE_TERRARIUM  = "Terrarium";

    public static void GoToHome()       => Load(SCENE_HOME);
    public static void GoToStore()      => Load(SCENE_STORE);
    public static void GoToGeckoList()  => Load(SCENE_GECKO_LIST);
    public static void GoToTerrarium()  => Load(SCENE_TERRARIUM);

    /// <summary>전환 중인지 — 전환 도중 버튼 입력을 무시할 때 쓴다</summary>
    public static bool IsTransitioning => SceneFader.IsBusy;

    /// <summary>AppBootstrap 전용 — Boot을 거치지 않고 실행한 씬으로 되돌아갈 때만 쓴다. UI에서 호출 금지.</summary>
    public static void GoToScene(string sceneName) => Load(sceneName);

    private static void Load(string sceneName)
        => SceneFader.Load(sceneName);
}
