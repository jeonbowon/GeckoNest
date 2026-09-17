using System;

/// <summary>오늘의 돌봄 목표에 세는 돌봄 종류 (GeckoManager.OnCareDone)</summary>
public enum CareKind { Feed, Water, Pet, Clean }

/// <summary>
/// 오늘의 돌봄 목표 진행 — 하루(UTC 날짜)가 바뀌면 RewardManager가 새로 만든다.
/// 목표 수·보상은 RewardManager에 있다.
/// </summary>
[Serializable]
public class DailyGoalData
{
    public int  day;       // UTC 날짜 번호 (DateTime.UtcNow.Date.Ticks / TicksPerDay)
    public int  fed;
    public int  watered;
    public int  petted;
    public int  cleaned;
    public bool claimed;   // 오늘 보상을 받았는지
}
