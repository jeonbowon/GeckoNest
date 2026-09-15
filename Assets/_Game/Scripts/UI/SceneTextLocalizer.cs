using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬이 열릴 때 모든 TMP 글자 중 번역표(Loc)의 원문과 같은 것을 현재 언어로 바꾼다.
/// 씬 파일에 번역 컴포넌트를 붙이지 않아도 된다 — 씬에는 한국어든 영어든 번역표에 있는 원문을 적어 두면 된다.
/// 나중에 Instantiate하는 프리팹의 고정 글자는 슬롯 스크립트가 LocalizeUnder를 부른다.
///
/// 사용자 데이터(게코 이름)를 담는 글자는 Ignore로 빼야 한다 — 이름이 번역표 원문과 우연히 같으면
/// ("하코", "먹이" …) 다른 언어로 바뀌어 보인다. 이름은 OnEnable에서 채워진 뒤 sceneLoaded에서 번역되므로 순서로 피할 수 없다.
/// </summary>
public static class SceneTextLocalizer
{
    private static readonly HashSet<TMP_Text> s_ignored = new HashSet<TMP_Text>();

    /// <summary>이 글자는 번역하지 않는다 (게코 이름처럼 사용자가 정한 글자). 글자를 채우기 전에 부른다.</summary>
    public static void Ignore(TMP_Text text)
    {
        if (text != null) s_ignored.Add(text);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;   // 도메인 리로드를 끈 에디터에서도 한 번만 구독
        SceneManager.sceneLoaded += OnSceneLoaded;
        LocalizeScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => LocalizeScene(scene);

    private static void LocalizeScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        foreach (var root in scene.GetRootGameObjects()) LocalizeUnder(root);
    }

    /// <summary>root 아래 모든 TMP 글자(꺼진 것 포함)를 번역한다.</summary>
    public static void LocalizeUnder(GameObject root)
    {
        if (root == null) return;
        s_ignored.RemoveWhere(t => t == null);   // 씬이 바뀌며 파괴된 글자 정리

        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (s_ignored.Contains(text)) continue;
            if (Loc.TryTranslateSource(text.text, out var translated) && text.text != translated)
                text.text = translated;
        }
    }
}
