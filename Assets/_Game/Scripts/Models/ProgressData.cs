using System;
using System.Collections.Generic;

[Serializable]
public class ProgressData
{
    public bool         hatchIntroSeen;   // v4: 첫 실행 부화 연출을 봤는지 (GeckoManager.NeedsHatchIntro)
    public int          adultCount;       // 어덜트까지 키운 마릿수 (GeckoManager.EvaluateGrowth, 도감·업적용)
    public List<string> adultSpeciesIds    = new List<string>();   // v5: 어덜트 큰 보상을 이미 받은 종 = 도감 "어덜트" 도장
    public int          totalLoginDays;
    public int          totalMoltCount;                            // (쓰지 않음 — 업적은 게코별 moltCount 합)
    public List<string> unlockedSpeciesIds = new List<string>();   // v7: 도감 "만남" 도장 — 키워 본 종 (RewardManager.RecordMet)
    public List<string> achievements       = new List<string>();   // v7: 보상을 받은 업적 id (RewardManager.ClaimAchievement)

    // v7: 업적 진행 — 이때부터 센다
    public int          petCount;          // 쓰다듬기 (실제로 한 것만)
    public int          feedCount;         // 먹이 주기
    public int          goalDays;          // 오늘의 돌봄 보상을 받은 날 수
    public bool         bookRewardClaimed; // 도감 완성 보상 (모든 종 어덜트)
    public List<string> morphIds           = new List<string>();   // v9: 얻은 모프 id (GeckoMorph.Record)
}
