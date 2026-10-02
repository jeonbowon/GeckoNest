using System;

[Serializable]
public class RewardedAdData
{
    public int day;          // UTC 날짜 번호 (RewardManager.TodayNumber)
    public int dailyCount;   // 보상 탭에서 오늘 완료한 광고 수
}
