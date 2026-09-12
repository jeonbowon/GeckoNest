using UnityEngine;

/// <summary>
/// 짧은 진동. 설정의 vibrationOn을 따른다.
///
/// Android 8(API 26)+는 세기까지 조절한 아주 짧은 진동(VibrationEffect)을 쓴다.
/// Handheld.Vibrate()는 길고 투박해서 쓰지 않는다 — 다만 이 호출이 코드에 있어야
/// Unity가 VIBRATE 권한을 매니페스트에 넣어 주므로, 구형 기기용 대체 경로로 남겨 둔다.
/// </summary>
public static class Haptics
{
    public static void Light()   => Vibrate(12,  60);
    public static void Medium()  => Vibrate(22, 120);
    public static void Success() => Vibrate(35, 160);

    private static bool Enabled
    {
        get
        {
            var gm = GameManager.Instance;
            return gm == null || gm.Settings.GetSettings().vibrationOn;
        }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject s_vibrator;
    private static int  s_sdk = -1;
    private static bool s_failed;

    private static void Vibrate(long ms, int amplitude)
    {
        if (!Enabled || s_failed) return;
        try
        {
            if (s_vibrator == null)
            {
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                    s_sdk = version.GetStatic<int>("SDK_INT");
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    s_vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }
            if (s_vibrator == null) return;

            if (s_sdk >= 26)
            {
                using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                using (var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", ms, Mathf.Clamp(amplitude, 1, 255)))
                    s_vibrator.Call("vibrate", effect);
            }
            else
            {
                s_vibrator.Call("vibrate", ms);
            }
        }
        catch (System.Exception e)
        {
            s_failed = true;   // 한 번 실패하면 다시 시도하지 않는다 (매 터치마다 예외 방지)
            Debug.LogWarning($"[Haptics] 진동을 사용할 수 없습니다: {e.Message}");
        }
    }

    // VIBRATE 권한 자동 추가용 — 실제로는 호출되지 않는다
    [UnityEngine.Scripting.Preserve]
    private static void PermissionAnchor() => Handheld.Vibrate();
#else
    private static void Vibrate(long ms, int amplitude)
    {
        // 에디터·기타 플랫폼에서는 진동 없음
    }
#endif
}
