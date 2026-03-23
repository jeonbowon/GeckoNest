using System;
using UnityEngine;

public class TimeManager
{
    // [TBD] 오프라인 시간 상한 — 방치 유저 이탈 데이터 보고 조정
    private const float MAX_OFFLINE_HOURS = 48f;

    public long GetNowTicks() => DateTime.UtcNow.Ticks;

    public float GetElapsedHours(long lastTicks)
    {
        var elapsed = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - lastTicks);
        return (float)elapsed.TotalHours;
    }

    public float GetElapsedDays(long sinceTicks)
    {
        var elapsed = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - sinceTicks);
        return (float)elapsed.TotalDays;
    }

    public float ClampOfflineProgress(float hours)
        => Mathf.Clamp(hours, 0f, MAX_OFFLINE_HOURS);
}
