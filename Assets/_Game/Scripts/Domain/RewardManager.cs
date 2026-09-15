using System;
using UnityEngine;

public class RewardManager
{
    private readonly PlayerRepository _repo;

    // 7일 순환 보상 테이블 (coin, gem)
    private static readonly (int coin, int gem)[] REWARD_TABLE =
    {
        (50,  0),  // day 1
        (75,  0),  // day 2
        (100, 0),  // day 3
        (125, 0),  // day 4
        (150, 0),  // day 5
        (200, 0),  // day 6
        (250, 3),  // day 7 — 젬 보너스
    };

    public RewardManager(PlayerRepository repo)
    {
        _repo = repo;
    }

    // ── 공개 API ──────────────────────────────────────────────

    /// <summary>오늘 아직 보상을 받지 않았으면 true.</summary>
    public bool CanClaim()
    {
        var data = _repo.GetPlayerData().dailyReward;
        if (data.lastClaimedTicks == 0) return true;

        var lastClaimed = new DateTime(data.lastClaimedTicks, DateTimeKind.Utc);
        return DateTime.UtcNow.Date > lastClaimed.Date;
    }

    /// <summary>
    /// 화면에 보여줄 연속 일수. 오늘 아직 안 받았으면 받으면 될 일수, 이미 받았으면 오늘 받은 일수.
    /// (예전에는 받기 전에 저장된 어제 일수를 보여줘서 "연속 1일"인데 2일째 보상이 뜨는 식으로 어긋났다)
    /// </summary>
    public int GetStreak()
    {
        var data = _repo.GetPlayerData().dailyReward;
        return CanClaim() ? CalcNextStreak(data) : Mathf.Max(1, data.streakDays);
    }

    /// <summary>GetStreak()일째 보상 — 받기 전이면 받을 보상, 받은 뒤면 오늘 받은 보상.</summary>
    public (int coin, int gem) PeekReward() => RewardOf(GetStreak());

    /// <summary>
    /// 일일 보상 지급. CanClaim()이 false이면 아무 일도 하지 않고 (0,0) 반환.
    /// </summary>
    public (int coin, int gem) ClaimReward()
    {
        if (!CanClaim())
        {
            Debug.LogWarning("[RewardManager] 오늘 이미 보상을 받았습니다.");
            return (0, 0);
        }

        var playerData = _repo.GetPlayerData();
        var reward     = playerData.dailyReward;

        int nextStreak         = CalcNextStreak(reward);
        reward.streakDays      = nextStreak;
        reward.lastClaimedTicks = DateTime.UtcNow.Ticks;

        var (coin, gem) = RewardOf(nextStreak);
        playerData.coin += coin;
        playerData.gem  += gem;

        _repo.Save();

        Debug.Log($"[RewardManager] 일일 보상 지급 — 연속 {nextStreak}일 / 코인 +{coin} 젬 +{gem}");
        return (coin, gem);
    }

    // ── 내부 헬퍼 ─────────────────────────────────────────────

    /// <summary>streak일째 보상 (7일마다 순환)</summary>
    private static (int coin, int gem) RewardOf(int streak)
        => REWARD_TABLE[(Mathf.Max(1, streak) - 1) % REWARD_TABLE.Length];

    /// <summary>
    /// 다음 클레임 시 적용될 streak 계산.
    /// 어제 받았으면 +1, 더 오래됐으면 1로 리셋.
    /// </summary>
    private static int CalcNextStreak(DailyRewardData data)
    {
        if (data.lastClaimedTicks == 0) return 1;

        var lastClaimed = new DateTime(data.lastClaimedTicks, DateTimeKind.Utc).Date;
        var yesterday   = DateTime.UtcNow.Date.AddDays(-1);

        return lastClaimed == yesterday ? data.streakDays + 1 : 1;
    }
}
