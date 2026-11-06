using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Opt-in isolated UI inspection in the running Editor.</summary>
[InitializeOnLoad]
public static class HakoReadabilityPreview
{
    static HakoReadabilityPreview()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool("HAKO.ReadabilityPreview", false))
            {
                SessionState.SetBool(HakoReadabilityFixture.SessionKey, false);
                SessionState.SetBool("HAKO.ReadabilityPreview", false);
                EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString("HAKO.ReadabilityStart", ""));
            }
        };
    }
    [MenuItem("Hako/검사/UI 가독성 미리보기 (별도 저장)")]
    public static void Start()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool("HAKO.ReadabilityPreview", true);
        SessionState.SetString("HAKO.ReadabilityStart", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        SessionState.SetBool(HakoReadabilityFixture.SessionKey, true);
        SessionState.SetString(HakoReadabilityFixture.SaveKey, "ui_readability_" + Guid.NewGuid().ToString("N"));
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/_Game/Scenes/Store.unity");
        EditorApplication.isPlaying = true;
    }
}
