using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>Read-only release readiness report. Never creates keys, changes settings or starts a build.</summary>
public static class HakoReleaseAudit
{
    [MenuItem("Hako/검사/출시 준비 검사", priority = 101)]
    public static void Run()
    {
        string report = Inspect(out int errors);
        Debug.Log(report);
        EditorUtility.DisplayDialog("출시 준비 검사", report, "확인");
    }

    public static void RunBatch()
    {
        Debug.Log(Inspect(out int errors));
        EditorApplication.Exit(errors == 0 ? 0 : 1);
    }

    public static string Inspect(out int errors)
    {
        var lines = new List<string>();
        int failed = 0;
        void Need(bool ok, string message)
        {
            lines.Add((ok ? "OK: " : "ERROR: ") + message);
            if (!ok) failed++;
        }
        var scenes = Array.FindAll(EditorBuildSettings.scenes, s => s.enabled);
        Need(scenes.Length > 0 && scenes[0].path == "Assets/_Game/Scenes/Boot.unity", "Boot is the first enabled scene");
        foreach (string name in new[] { "Boot", "MainHome", "GeckoList", "Store", "Terrarium" })
            Need(Array.Exists(scenes, s => s.path == $"Assets/_Game/Scenes/{name}.unity" && File.Exists(s.path)), "Scene: " + name);
        Need(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) == "com.tnbsoft.hako", "Android application ID");
        Need(PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) == ScriptingImplementation.IL2CPP, "Android IL2CPP");
        Need((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) != 0, "Android ARM64");
        Need(PlayerSettings.defaultInterfaceOrientation == UIOrientation.Portrait, "Portrait orientation");
        var shader = Resources.Load<Shader>(GeckoWholeSurface.ShaderPath);
        Need(shader != null && !ShaderUtil.ShaderHasError(shader), "Whole-body surface shader");
        foreach (var species in Resources.LoadAll<GeckoSpeciesSO>("Species"))
        {
            if (species.skin == null)
            {
                lines.Add("WARNING: " + species.speciesId + " uses fallback artwork, not species-specific final art");
                continue;
            }
            var skin = species.skin;
            if (!skin.wholeBody) continue;
            Need(skin.GetPart(GeckoPartId.Body) != null && skin.GetPart(GeckoPartId.Body).sprite != null, species.speciesId + " body sprite");
            Need(skin.wholeEyeAtlas != null && skin.wholeMouthAtlas != null, species.speciesId + " expression atlases");
            Need(ValidRegion(skin.wholeEyeLeftRect) && ValidRegion(skin.wholeEyeRightRect) && ValidRegion(skin.wholeMouthRect),
                 species.speciesId + " expression regions inside body UVs");
        }
        foreach (Sfx id in Enum.GetValues(typeof(Sfx)))
            if (Resources.Load<AudioClip>("Audio/Sfx/" + SfxLibrary.FileName(id)) == null)
                lines.Add("WARNING: synthesized fallback SFX: " + id);
        if (Resources.Load<AudioClip>("Audio/Bgm/home") == null)
            lines.Add("WARNING: synthesized fallback ambience; no final home music file");
        bool signed = PlayerSettings.Android.useCustomKeystore && !string.IsNullOrWhiteSpace(PlayerSettings.Android.keystoreName)
                      && File.Exists(PlayerSettings.Android.keystoreName);
        lines.Add(signed ? "OK: custom signing key configured (passwords not inspected)" : "WARNING: release signing key must be configured by the owner");
        lines.Add("MANUAL: installed Target SDK versus current store requirements, privacy-policy content/URL, device notifications, save recovery, visual alignment, performance");
        errors = failed;
        return "[HakoReleaseAudit] Configuration errors: " + errors + "\n" + string.Join("\n", lines)
               + "\nThis report is not a release certification; warnings and device tests remain actionable.";
    }

    private static bool ValidRegion(Rect r) => r.width > 0f && r.height > 0f && r.xMin >= 0f && r.yMin >= 0f
                                               && r.xMax <= 1f && r.yMax <= 1f;
}
