using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Repeatable Unity Editor Android builds.</summary>
public static class HakoAndroidBuild
{
    private const string TestApkPath = "Builds/Android/Hako-device-test.apk";

    [MenuItem("Hako/빌드/가독성 확인 APK (별도 앱)")]
    public static void BuildReadabilityApk()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Stop Play Mode and wait for compilation before building.");
        string appId = PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android);
        string product = PlayerSettings.productName;
        bool customKey = PlayerSettings.Android.useCustomKeystore;
        bool bundle = EditorUserBuildSettings.buildAppBundle;
        bool export = EditorUserBuildSettings.exportAsGoogleAndroidProject;
        try
        {
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.tnbsoft.hako.readabilitytest");
            PlayerSettings.productName = "HAKO UI Test";
            PlayerSettings.Android.useCustomKeystore = false;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            string path = Path.GetFullPath($"Builds/Android/Hako-readability-{DateTime.UtcNow:yyyyMMdd-HHmmss}.apk");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = path,
                target = BuildTarget.Android,
                options = BuildOptions.None,
                extraScriptingDefines = new[] { "HAKO_DEVICE_TEST" },
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"Readability APK failed: {report.summary.result}");
            Debug.Log($"[HakoAndroidBuild] READABILITY APK ready: {path} ({report.summary.totalSize} bytes)");
        }
        finally
        {
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, appId);
            PlayerSettings.productName = product;
            PlayerSettings.Android.useCustomKeystore = customKey;
            EditorUserBuildSettings.buildAppBundle = bundle;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = export;
            AssetDatabase.SaveAssets();
        }
    }

    [MenuItem("Hako/빌드/Android 출시 AAB")]
    public static void BuildReleaseAab()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Stop Play Mode and wait for compilation before building.");
        if (AdMobService.USE_TEST_ADS)
            throw new InvalidOperationException("Release AAB requires production ad IDs (USE_TEST_ADS = false).");
        if (!PlayerSettings.Android.useCustomKeystore
            || !File.Exists(PlayerSettings.Android.keystoreName)
            || string.IsNullOrWhiteSpace(PlayerSettings.Android.keyaliasName))
            throw new InvalidOperationException("Configure the release keystore and key alias in Publishing Settings.");
        if (string.IsNullOrEmpty(PlayerSettings.Android.keystorePass)
            || string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass))
            throw new InvalidOperationException("Enter both signing passwords in Publishing Settings for this Editor session.");
        if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            throw new InvalidOperationException("Could not switch the active build target to Android.");

        // Google Mobile Ads Unity 11.5.0's Android library requires API 24.
        if ((int)PlayerSettings.Android.minSdkVersion < 24)
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;

        string audit = HakoReleaseAudit.Inspect(out int errors);
        Debug.Log(audit);
        if (errors != 0)
            throw new InvalidOperationException("Release configuration audit failed. See Console.");

        string outputDirectory = Path.GetFullPath("Builds/Android/Release");
        Directory.CreateDirectory(outputDirectory);
        string outputPath = Path.Combine(outputDirectory,
            $"hako-{PlayerSettings.bundleVersion}-{PlayerSettings.Android.bundleVersionCode}.aab");
        // Preserve previous artifacts; a repeat build receives a timestamp.
        if (File.Exists(outputPath))
            outputPath = Path.Combine(outputDirectory,
                $"hako-{PlayerSettings.bundleVersion}-{PlayerSettings.Android.bundleVersionCode}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.aab");
        EditorUserBuildSettings.buildAppBundle = true;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.allowDebugging = false;
        EditorUserBuildSettings.connectProfiler = false;

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None,
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException($"Release AAB build failed: {report.summary.result} ({report.summary.totalErrors} errors)");
        Debug.Log($"[HakoAndroidBuild] RELEASE AAB ready: {outputPath} ({report.summary.totalSize} bytes, {report.summary.totalErrors} errors)");
    }

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
