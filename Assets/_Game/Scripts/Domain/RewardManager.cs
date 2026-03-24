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

    /// <summary>현재 연속 접속 일수. 아직 오늘 안 받았으면 예상값.</summary>
    public int GetStreak()
    {
        var data = _repo.GetPlayerData().dailyReward;
        return Mathf.Max(1, data.streakDays);
    }

    /// <summary>다음 보상 미리보기 (클레임 전 UI 표시용).</summary>
    public (int coin, int gem) PeekReward()
    {
        var data   = _repo.GetPlayerData().dailyReward;
        int streak = CalcNextStreak(data);
        return REWARD_TABLE[(streak - 1) % REWARD_TABLE.Length];
    }

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

        var (coin, gem) = REWARD_TABLE[(nextStreak - 1) % REWARD_TABLE.Length];
        playerData.coin += coin;
        playerData.gem  += gem;

        _repo.Save();

        Debug.Log($"[RewardManager] 일일 보상 지급 — 연속 {nextStreak}일 / 코인 +{coin} 젬 +{gem}");
        return (coin, gem);
    }

    // ── 내부 헬퍼 ─────────────────────────────────────────────

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
