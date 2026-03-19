using UnityEngine.SceneManagement;

public static class SceneRouter
{
    private const string SCENE_HOME       = "MainHome";
    private const string SCENE_STORE      = "Store";
    private const string SCENE_GECKO_LIST = "GeckoList";
    private const string SCENE_TERRARIUM  = "Terrarium";
    private const string SCENE_POPUP      = "Popup";

    public static void GoToHome()       => Load(SCENE_HOME);
    public static void GoToStore()      => Load(SCENE_STORE);
    public static void GoToGeckoList()  => Load(SCENE_GECKO_LIST);
    public static void GoToTerrarium()  => Load(SCENE_TERRARIUM);

    public static void OpenPopup()
        => SceneManager.LoadScene(SCENE_POPUP, LoadSceneMode.Additive);

    public static void ClosePopup()
        => SceneManager.UnloadSceneAsync(SCENE_POPUP);

    private static void Load(string sceneName)
        => SceneManager.LoadScene(sceneName);
}
