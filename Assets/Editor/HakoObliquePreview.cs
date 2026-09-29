using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>GPU capture of the real home scene with isolated data. No generated screenshot is used as game UI.</summary>
[InitializeOnLoad]
public static class HakoObliquePreview
{
    private static double _started;
    private static int _frame;
    private static int _phase;
    private static double _perchedAt;
    static HakoObliquePreview()
    {
        if (SessionState.GetBool(HakoObliquePreviewFixture.SessionKey,false))
        {
            _started = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }
    }

    public static void RunBatch()
    {
        SessionState.SetBool(HakoObliquePreviewFixture.SessionKey,true);
        SessionState.SetString("HAKO.PreviewStartScene", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        EditorSceneManager.playModeStartScene = null;
        EditorSceneManager.OpenScene("Assets/_Game/Scenes/MainHome.unity");
        var gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        if (gameViewType != null) EditorWindow.GetWindow(gameViewType);
        HakoGameViewFit.Fit(false);
        _started = EditorApplication.timeSinceStartup;
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
    }

    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup - _started > 150) { Debug.LogError("[ObliquePreview] Timed out before route completed."); Finish(1); return; }
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        if (++_frame < 360) return;
        try
        {
            var rig = UnityEngine.Object.FindFirstObjectByType<GeckoRig>();
            if (rig == null || GameManager.Instance == null) throw new InvalidOperationException("Preview failed to initialize.");
            var movement = rig.GetComponent<GeckoMovementAI>();
            if (_phase == 1)
            {
                if (movement.Activity != GeckoActivity.Perching) return;
                _perchedAt = EditorApplication.timeSinceStartup;
                _phase = 2;
                return;
            }
            if (_phase == 2)
            {
                if (EditorApplication.timeSinceStartup - _perchedAt < 0.7) return;
                Capture("Logs/oblique-branch-20260929.png",1080,2400);
                Debug.Log("[ObliquePreview] Live AI reached the branch perch using the measured artwork route.");
                _phase = 3;
                return;
            }
            if (_phase == 3)
            {
                if (movement.IsClimbing) return;
                Debug.Log("[ObliquePreview] Live AI descended to ground and cleared the climbing state.");
                Finish(0); return;
            }
            if (movement != null) movement.enabled = false;
            var motor = rig.GetComponent<GeckoMotor>();
            if (motor != null) motor.enabled = false;
            var gecko = (RectTransform)rig.transform;
            gecko.anchoredPosition = new Vector2(-15,830);
            rig.SetGrowthStage(4,true); rig.SetFacing(true);
            rig.DepthScale = movement != null ? movement.DepthScaleFor(830) : 0.85f;
            rig.SolveRest();
            if (GeckoTouch.ZoneAt(rig,rig.PartWorldPoint(GeckoPartId.EyeL,new Vector2(0.5f,0.5f))) != GeckoTouchZone.Eye)
                throw new InvalidOperationException("Elevated eye touch did not match rendered eye.");
            Capture("Logs/oblique-home-20260929.png",1080,2400);
            var pose = new GeckoPose(12);
            pose[GeckoPartId.Tongue1].scale = pose[GeckoPartId.Tongue2].scale = new Vector2(0f,1f);
            pose[GeckoPartId.ShedPatch].alpha = 0f;
            pose.eyeL = GeckoEye.Happy; pose.mouth = GeckoMouth.OpenSmall;
            pose[GeckoPartId.LegFrontNear].angle = 10;
            pose[GeckoPartId.LegBackFar].angle = 10;
            pose[GeckoPartId.LegFrontFar].angle = -10;
            pose[GeckoPartId.LegBackNear].angle = -10;
            pose[GeckoPartId.Tail].angle = 12;
            rig.Solve(pose,1f);
            Capture("Logs/oblique-pose-20260929.png",1080,2400);
            Debug.Log("[ObliquePreview] Real Unity GPU captures complete.");
            motor.enabled = true;
            movement.enabled = true;
            if (!movement.VisitDecor(2)) throw new InvalidOperationException("Could not start live branch visit.");
            _phase = 1;
        }
        catch (Exception e) { Debug.LogException(e); Finish(1); }
    }

    private static void Capture(string path, int width, int height)
    {
        var camGo = new GameObject("PreviewCamera",typeof(Camera));
        var camera = camGo.GetComponent<Camera>();
        camera.orthographic = true; camera.orthographicSize = height*0.5f;
        camera.transform.position = new Vector3(0,0,-100);
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
        camera.nearClipPlane = 0.1f; camera.farClipPlane = 1000;
        var rt = new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);
        camera.targetTexture = rt;
        foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!canvas.isRootCanvas) continue;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera; canvas.planeDistance = 100;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null) { scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize; scaler.scaleFactor = 1; }
        }
        Canvas.ForceUpdateCanvases();
        camera.Render();
        var prev = RenderTexture.active; RenderTexture.active=rt;
        var image = new Texture2D(width,height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,width,height),0,0); image.Apply();
        Directory.CreateDirectory("Logs"); File.WriteAllBytes(path,image.EncodeToPNG());
        RenderTexture.active=prev;
        foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!canvas.isRootCanvas || canvas.worldCamera != camera) continue;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null) { scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1080,2400); }
        }
        UnityEngine.Object.DestroyImmediate(image);
        camera.targetTexture=null; rt.Release();
        UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(camGo);
    }

    private static void Finish(int code)
    {
        EditorApplication.update -= Tick;
        SessionState.SetBool(HakoObliquePreviewFixture.SessionKey,false);
        string previous = SessionState.GetString("HAKO.PreviewStartScene", "");
        EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
        EditorApplication.Exit(code);
    }
}
