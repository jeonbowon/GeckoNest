using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 로컬 알림 예약 — "하코가 배고파해요", "오늘의 보상이 기다려요".
///
/// Mobile Notifications 패키지(`com.unity.mobile.notifications`)를 **리플렉션으로** 부른다.
/// 패키지가 없어도 컴파일이 깨지지 않고 조용히 아무 것도 하지 않는다.
/// (설치: Window → Package Manager → Unity Registry → Mobile Notifications)
///
/// 예약 시점: 앱이 백그라운드로 갈 때. 앱을 열면 모두 취소한다 (이미 돌본 뒤에 울리면 안 되므로).
/// </summary>
public static class NotificationScheduler
{
    private const string CHANNEL_ID   = "hako_care";
    private const string CHANNEL_NAME = "하코 돌봄 알림";
    private const string CHANNEL_DESC = "게코가 배고프거나 일일 보상이 준비되면 알려드립니다";

    private const float CARE_THRESHOLD  = 25f;   // [TBD] 이 수치까지 떨어지면 알린다
    private const float MIN_DELAY_HOURS = 1f;    // 너무 빨리 울리지 않게
    private const int   REWARD_HOUR     = 10;    // 다음 날 오전 10시에 보상 알림

    private static bool s_channelReady;
    private static bool s_unavailable;   // 패키지 없음 — 한 번만 확인한다

    // ── 공개 API ──────────────────────────────────────────────

    /// <summary>앱이 백그라운드로 갈 때 — 다음에 돌봐야 할 시각으로 알림을 예약한다.</summary>
    public static void ScheduleAll()
    {
        CancelAll();

        var gm = GameManager.Instance;
        if (gm == null || !gm.Settings.GetSettings().notificationOn) return;
        if (!EnsureChannel()) return;

        var gecko = gm.GetSelectedGecko();
        if (gecko != null)
        {
            float hours = Mathf.Max(MIN_DELAY_HOURS, GeckoManager.HoursUntilCareNeeded(gecko, CARE_THRESHOLD));
            bool thirstFirst = gecko.thirst <= gecko.hunger;
            string who = KoreanText.WithSubject(gecko.name);
            Send(thirstFirst ? $"{who} 목말라해요" : $"{who} 배고파해요",
                 "잠깐 들여다봐 주세요",
                 DateTime.Now.AddHours(hours));
        }

        // 일일 보상 — 다음 날 오전 10시
        var next = DateTime.Now.Date.AddDays(1).AddHours(REWARD_HOUR);
        if (next <= DateTime.Now) next = next.AddDays(1);
        Send("오늘의 보상이 기다려요", "하코가 기다리고 있어요", next);
    }

    /// <summary>앱을 열 때 — 예약해 둔 알림을 모두 지운다.</summary>
    public static void CancelAll()
    {
        var center = Center;
        if (center == null) return;
        try
        {
            center.GetMethod("CancelAllScheduledNotifications", BindingFlags.Public | BindingFlags.Static)
                 ?.Invoke(null, null);
        }
        catch (Exception e)
        {
            Fail(e);
        }
    }

    /// <summary>알림 권한 요청 (Android 13+). 설정에서 알림을 켤 때 부른다.</summary>
    public static void RequestPermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        const string POST_NOTIFICATIONS = "android.permission.POST_NOTIFICATIONS";
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(POST_NOTIFICATIONS))
            UnityEngine.Android.Permission.RequestUserPermission(POST_NOTIFICATIONS);
#endif
    }

    // ── 내부 (리플렉션) ───────────────────────────────────────

    private static Type Center
    {
        get
        {
            if (s_unavailable) return null;
            var t = Type.GetType("Unity.Notifications.Android.AndroidNotificationCenter, Unity.Notifications.Android");
            if (t == null)
            {
                s_unavailable = true;
                Debug.Log("[NotificationScheduler] Mobile Notifications 패키지가 없어 알림을 건너뜁니다.");
            }
            return t;
        }
    }

    private static bool EnsureChannel()
    {
        if (s_channelReady) return true;
        var center = Center;
        if (center == null) return false;

        try
        {
            var channelType = Type.GetType("Unity.Notifications.Android.AndroidNotificationChannel, Unity.Notifications.Android");
            var importance  = Type.GetType("Unity.Notifications.Android.Importance, Unity.Notifications.Android");
            if (channelType == null || importance == null) return false;

            object channel = Activator.CreateInstance(channelType);
            SetMember(channelType, ref channel, "Id", CHANNEL_ID);
            SetMember(channelType, ref channel, "Name", CHANNEL_NAME);
            SetMember(channelType, ref channel, "Description", CHANNEL_DESC);
            SetMember(channelType, ref channel, "Importance", Enum.Parse(importance, "Default"));

            center.GetMethod("RegisterNotificationChannel", BindingFlags.Public | BindingFlags.Static)
                 ?.Invoke(null, new[] { channel });
            s_channelReady = true;
            return true;
        }
        catch (Exception e)
        {
            Fail(e);
            return false;
        }
    }

    private static void Send(string title, string text, DateTime fireTime)
    {
        var center = Center;
        if (center == null) return;

        try
        {
            var notificationType = Type.GetType("Unity.Notifications.Android.AndroidNotification, Unity.Notifications.Android");
            if (notificationType == null) return;

            object n = Activator.CreateInstance(notificationType);
            SetMember(notificationType, ref n, "Title", title);
            SetMember(notificationType, ref n, "Text", text);
            SetMember(notificationType, ref n, "FireTime", fireTime);
            SetMember(notificationType, ref n, "ShouldAutoCancel", true);

            var send = center.GetMethod("SendNotification", BindingFlags.Public | BindingFlags.Static,
                                        null, new[] { notificationType, typeof(string) }, null);
            send?.Invoke(null, new[] { n, CHANNEL_ID });
            Debug.Log($"[NotificationScheduler] 알림 예약 — {fireTime:MM-dd HH:mm} \"{title}\"");
        }
        catch (Exception e)
        {
            Fail(e);
        }
    }

    // 구조체라 박싱된 객체에 값을 넣고 다시 받아야 한다
    private static void SetMember(Type type, ref object target, string name, object value)
    {
        var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(target, value);
            return;
        }
        type.GetField(name, BindingFlags.Public | BindingFlags.Instance)?.SetValue(target, value);
    }

    private static void Fail(Exception e)
    {
        s_unavailable = true;   // 한 번 실패하면 다시 시도하지 않는다
        Debug.LogWarning($"[NotificationScheduler] 알림을 예약할 수 없습니다: {e.Message}");
    }
}
