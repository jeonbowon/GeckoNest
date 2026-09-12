using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 씬 전환 페이드. SceneRouter만 사용한다 (UI에서 직접 쓰지 않는다).
///
/// 부드러운 크림색으로 덮었다가 걷어낸다 — 검은 화면보다 따뜻하고 게임 톤과 맞는다.
/// 전환 중에는 화면 전체가 터치를 막아 버튼 연타로 씬이 두 번 넘어가는 일을 막는다.
/// </summary>
public class SceneFader : MonoBehaviour
{
    private const float FADE_OUT = 0.18f;
    private const float FADE_IN  = 0.28f;
    private static readonly Color FADE_COLOR = new Color(1f, 0.973f, 0.933f, 1f);   // #FFF8EE

    private static SceneFader s_instance;

    private CanvasGroup _group;
    private bool        _busy;

    public static bool IsBusy => s_instance != null && s_instance._busy;

    private static SceneFader Instance
    {
        get
        {
            if (s_instance == null) s_instance = Create();
            return s_instance;
        }
    }

    /// <summary>페이드하며 씬을 바꾼다. 전환 중에 다시 불리면 무시한다.</summary>
    public static void Load(string sceneName)
    {
        var f = Instance;
        if (f._busy) return;
        f.StartCoroutine(f.Transition(sceneName));
    }

    private static SceneFader Create()
    {
        var go = new GameObject("[SceneFader]");
        DontDestroyOnLoad(go);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;
        go.AddComponent<GraphicRaycaster>();   // 전환 중 입력 차단

        var group = go.AddComponent<CanvasGroup>();
        group.alpha          = 0f;
        group.blocksRaycasts = false;
        group.interactable   = false;

        var imgGo = new GameObject("Cover", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imgGo.transform.SetParent(go.transform, false);
        var rt = (RectTransform)imgGo.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        imgGo.GetComponent<Image>().color = FADE_COLOR;

        var fader = go.AddComponent<SceneFader>();
        fader._group = group;
        return fader;
    }

    private IEnumerator Transition(string sceneName)
    {
        _busy = true;
        _group.blocksRaycasts = true;

        yield return Fade(_group.alpha, 1f, FADE_OUT);

        var op = SceneManager.LoadSceneAsync(sceneName);
        if (op != null)
            while (!op.isDone) yield return null;
        else
            Debug.LogError($"[SceneFader] 씬을 불러올 수 없습니다: {sceneName} (Build Settings 확인)");

        yield return null;   // 새 씬의 첫 레이아웃이 잡힐 때까지 한 프레임 기다린다
        yield return Fade(1f, 0f, FADE_IN);

        _group.blocksRaycasts = false;
        _busy = false;
    }

    private IEnumerator Fade(float from, float to, float time)
    {
        for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
        {
            float u = t / time;
            _group.alpha = Mathf.Lerp(from, to, u * u * (3f - 2f * u));
            yield return null;
        }
        _group.alpha = to;
    }
}
