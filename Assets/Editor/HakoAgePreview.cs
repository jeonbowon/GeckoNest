using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Real MainHome Play Mode age UI regression with isolated saves and Editor-only mock ads.</summary>
[InitializeOnLoad]
public static class HakoAgePreview
{
    private static double _started;
    private static double _next;
    private static int _phase;
    static HakoAgePreview()
    {
        if (!SessionState.GetBool(HakoAgePreviewFixture.SessionKey, false)) return;
        _started = EditorApplication.timeSinceStartup;
        EditorApplication.update += Tick;
    }

    public static void RunBatch()
    {
        SessionState.SetBool(HakoAgePreviewFixture.SessionKey, true);
        SessionState.SetString(HakoAgePreviewFixture.SaveKey, "age_preview_" + Guid.NewGuid().ToString("N"));
        SessionState.SetString("HAKO.AgePreviewStartScene", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        EditorSceneManager.playModeStartScene = null;
        EditorSceneManager.OpenScene("Assets/_Game/Scenes/MainHome.unity");
        HakoGameViewFit.Fit(false);
        _started = EditorApplication.timeSinceStartup;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
    }

    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup - _started > 100) { Finish(1, "Timed out"); return; }
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.timeSinceStartup < _next) return;
        try
        {
            var panel = UnityEngine.Object.FindFirstObjectByType<AdAudiencePanel>();
            if (_phase == 0)
            {
                if (panel == null) return;
                Require(AdMobService.Instance != null && AdMobService.Instance.State == AdMobService.AdState.WaitingForAge,
                    "SDK remains blocked before age choice");
                bool paid = false;
                Require(!AdMobService.Instance.ShowRewarded(() => paid = true) && !paid, "No reward or ad before age choice");
                var input = panel.GetComponentInChildren<TMP_InputField>();
                Require(input.text == "", "No preset age");
                Capture(panel, "Logs/age-screen-ko-20261008.png");
                panel.transform.Find("Card/Continue").GetComponent<Button>().onClick.Invoke();
                Require(!AdAudiencePolicy.HasChoice(GameManager.Instance.Settings.GetSettings()), "Blank submission stays blocked");
                input.text = "12";
                panel.transform.Find("Card/Continue").GetComponent<Button>().onClick.Invoke();
                _phase = 1; _next = EditorApplication.timeSinceStartup + 1; return;
            }
            if (_phase == 1)
            {
                Require(panel == null && AdMobService.Instance.IsReady, "Child selection closes UI and reaches Editor mock ad");
                Require(AdAudiencePolicy.RequiresProtection(GameManager.Instance.Settings.GetSettings()), "Child saved with protection");
                var loaded = new SaveManager(SessionState.GetString(HakoAgePreviewFixture.SaveKey, "")).Load();
                Require(AdAudiencePolicy.HasChoice(loaded.settings) && loaded.settings.adAudience == AdAudience.Minor,
                    "Child choice survives disk reload");
                Loc.Set(GameLanguage.English);
                var repo = new PlayerRepository(new SaveManager(SessionState.GetString(HakoAgePreviewFixture.SaveKey, "") + "_unknown"));
                var canvas = UnityEngine.Object.FindFirstObjectByType<HomeUIController>().transform as RectTransform;
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Game/Fonts/NanumGothic-Regular SDF.asset");
                if (font == null) font = TMP_Settings.defaultFontAsset;
                AdAudiencePanel.Create(canvas, font, new SettingsManager(repo));
                _phase = 2; _next = EditorApplication.timeSinceStartup + 1; return;
            }
            if (_phase == 2)
            {
                Require(panel != null, "English panel exists");
                Capture(panel, "Logs/age-screen-en-20261008.png");
                panel.transform.Find("Card/Unknown").GetComponent<Button>().onClick.Invoke();
                _phase = 3; _next = EditorApplication.timeSinceStartup + 0.5; return;
            }
            Require(panel == null, "Decline closes panel");
            var unknown = new SaveManager(SessionState.GetString(HakoAgePreviewFixture.SaveKey, "") + "_unknown").Load();
            Require(AdAudiencePolicy.HasChoice(unknown.settings) && unknown.settings.adAudience == AdAudience.Unknown
                && AdAudiencePolicy.RequiresProtection(unknown.settings), "Decline persists protected unknown choice");
            Finish(0, "PASS: age gate, empty input, child save, unknown save, Korean and English GPU captures. Network ads not exercised.");
        }
        catch (Exception e) { Finish(1, e.ToString()); }
    }

    private static void Require(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException(message);
        Debug.Log("[AgePreview] PASS " + message);
    }

    private static void Capture(AdAudiencePanel panel, string path)
    {
        var camGo = new GameObject("AgePreviewCamera", typeof(Camera));
        var camera = camGo.GetComponent<Camera>();
        camera.orthographic = true; camera.orthographicSize = 1200;
        camera.transform.position = new Vector3(0, 0, -100);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black; camera.farClipPlane = 1000;
        var target = new RenderTexture(1080, 2400, 24);
        camera.targetTexture = target;
        var canvas = panel.GetComponentInParent<Canvas>().rootCanvas;
        var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera;
        float oldDistance = canvas.planeDistance;
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 100;
        Canvas.ForceUpdateCanvases();
        foreach (var text in panel.GetComponentsInChildren<TMP_Text>())
        {
            text.ForceMeshUpdate();
            Require(!text.isTextOverflowing, "No text overflow: " + text.name);
        }
        camera.Render();
        var oldTarget = RenderTexture.active; RenderTexture.active = target;
        var texture = new Texture2D(1080, 2400, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, 1080, 2400), 0, 0); texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        RenderTexture.active = oldTarget;
        canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.planeDistance = oldDistance;
        camera.targetTexture = null; target.Release();
        UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(camGo);
    }

    private static void Finish(int exitCode, string message)
    {
        EditorApplication.update -= Tick;
        if (exitCode == 0) Debug.Log("[AgePreview] " + message); else Debug.LogError("[AgePreview] " + message);
        string stem = SessionState.GetString(HakoAgePreviewFixture.SaveKey, "");
        if (stem.StartsWith("age_preview_", StringComparison.Ordinal))
        { new SaveManager(stem).DeleteFiles(); new SaveManager(stem + "_unknown").DeleteFiles(); }
        SessionState.SetBool(HakoAgePreviewFixture.SessionKey, false);
        string previous = SessionState.GetString("HAKO.AgePreviewStartScene", "");
        EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
        EditorApplication.Exit(exitCode);
    }
}
