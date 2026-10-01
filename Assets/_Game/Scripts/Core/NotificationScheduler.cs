using System;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using Unity.Notifications.Android;
#endif

/// <summary>
/// 로컬 알림 예약 — "하코가 배고파해요", "오늘의 보상이 기다려요" (문구는 번역표 Loc의 notify.*).
///
/// Android 플레이어에서는 Mobile Notifications 패키지를 직접 참조한다.
/// IL2CPP가 알림 타입을 제거하지 않도록 리플렉션 호출을 사용하지 않는다.
///
/// 예약 시점: 앱이 백그라운드로 갈 때. 앱을 열면 모두 취소한다 (이미 돌본 뒤에 울리면 안 되므로).
/// </summary>
public static class NotificationScheduler
{
    private const string CHANNEL_ID = "hako_care";

    private const float CARE_THRESHOLD  = 25f;   // [TBD] 이 수치까지 떨어지면 알린다
    private const float MIN_DELAY_HOURS = 1f;    // 너무 빨리 울리지 않게
    private const int   REWARD_HOUR     = 10;    // 다음 날 오전 10시에 보상 알림

    private static bool s_channelReady;

    // ── 공개 API ──────────────────────────────────────────────

    /// <summary>앱이 백그라운드로 갈 때 — 다음에 돌봐야 할 시각으로 알림을 예약한다.</summary>
    public static void ScheduleAll()
    {
        CancelAll();

        var gm = GameManager.Instance;
        if (gm == null || !gm.Settings.GetSettings().notificationOn) return;
        if (!EnsureChannel()) return;

        var selected = gm.GetSelectedGecko();
        string who = Loc.Subject(selected != null ? selected.name : Loc.Get("gecko.default_name"));

        // 돌봄 알림 — 홈에 없는 게코도 배고파지므로 모든 게코 중 가장 먼저 돌봐야 할 게코로
        var urgent = GeckoManager.MostUrgent(gm.GetPlayerData().geckos, CARE_THRESHOLD, out float untilCare);
        if (urgent != null)
        {
            float hours = Mathf.Max(MIN_DELAY_HOURS, untilCare);
            bool thirstFirst = GeckoManager.ThirstFirst(urgent, CARE_THRESHOLD);
            Send(Loc.Format(thirstFirst ? "notify.thirsty" : "notify.hungry", Loc.Subject(urgent.name)),
                 Loc.Get("notify.check"),
                 DateTime.Now.AddHours(hours));
        }

        // 일일 보상 — 다음 날 오전 10시
        var next = DateTime.Now.Date.AddDays(1).AddHours(REWARD_HOUR);
        if (next <= DateTime.Now) next = next.AddDays(1);
        Send(Loc.Get("notify.reward_title"), Loc.Format("notify.reward_text", who), next);
    }

    /// <summary>앱을 열 때 — 예약해 둔 알림을 모두 지운다.</summary>
    public static void CancelAll()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            AndroidNotificationCenter.CancelAllScheduledNotifications();
        }
        catch (Exception e)
        {
            Fail(e);
        }
#endif
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

    private static bool EnsureChannel()
    {
        if (s_channelReady) return true;
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel
            {
                Id = CHANNEL_ID,
                Name = Loc.Get("notify.channel_name"),
                Description = Loc.Get("notify.channel_desc"),
                Importance = Importance.Default,
            });
            s_channelReady = true;
            return true;
        }
        catch (Exception e)
        {
            Fail(e);
            return false;
        }
#else
        return false;
#endif
    }

    private static void Send(string title, string text, DateTime fireTime)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            var notification = new AndroidNotification
            {
                Title = title,
                Text = text,
                FireTime = fireTime,
                ShouldAutoCancel = true,
            };
            AndroidNotificationCenter.SendNotification(notification, CHANNEL_ID);
            Debug.Log($"[NotificationScheduler] 알림 예약 — {fireTime:MM-dd HH:mm} \"{title}\"");
        }
        catch (Exception e)
        {
            Fail(e);
        }
#endif
    }

    private static void Fail(Exception e)
    {
        Debug.LogWarning($"[NotificationScheduler] 알림을 예약할 수 없습니다: {e.Message}");
    }
}
