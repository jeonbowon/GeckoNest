using System;

[Serializable]
public class DailyRewardData
{
    public long lastClaimedTicks;
    public int  streakDays;
    public int  adWatchedToday;
    public long adResetTicks;
}
