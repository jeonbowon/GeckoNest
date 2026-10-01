using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Repeatable Unity Editor builds for physical-device testing.</summary>
public static class HakoAndroidBuild
{
    private const string TestApkPath = "Builds/Android/Hako-device-test.apk";

    [MenuItem("Hako/빌드/Android 기기 테스트 APK")]
    public static void BuildDeviceTestApk()
    {
        if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            throw new InvalidOperationException("Could not switch the active build target to Android.");

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
        if (scenes.Length == 0)
            throw new InvalidOperationException("No enabled scenes are configured.");

        string absolutePath = Path.GetFullPath(TestApkPath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
        EditorUserBuildSettings.buildAppBundle = false;

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = absolutePath,
            target = BuildTarget.Android,
            options = BuildOptions.None,
        });

        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException($"Android APK build failed: {report.summary.result} ({report.summary.totalErrors} errors)");

        Debug.Log($"[HakoAndroidBuild] APK ready: {absolutePath} ({report.summary.totalSize} bytes)");
    }

    public static void BuildDeviceTestApkBatch()
    {
        try
        {
            BuildDeviceTestApk();
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorApplication.Exit(1);
        }
    }
}
