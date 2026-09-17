using System;
using System.Collections.Generic;

[Serializable]
public class GeckoData
{
    // ── 식별 ───────────────────────────────────────────────────
    public string id;               // System.Guid.NewGuid().ToString()
    public string name;             // 사용자 지정 이름
    public string speciesId;        // "crested" | "leopard" | "gargoyle"
                                    // → GeckoSpeciesSO의 speciesId 필드와 매칭

    // ── 성장 ───────────────────────────────────────────────────
    public int   growthStage;       // 0=Hatchling 1=Baby 2=Juvenile 3=Sub-Adult 4=Adult
    public float growthExp;         // 누적 성장치. 단계 전환 후 0 리셋
    public float moltProgress;      // 0 ~ 100. 100 이상 → TryMolt() 호출
    public int   moltCount;         // 누적 허물 횟수 (도감, 성장 조건에 사용)
    public float moltBonus;         // 먹이로 쌓인 다음 허물 성공률 가산 (0.1 = +10%, 최대 0.15). 허물 판정 뒤 0

    // ── 상태값 (모두 0 ~ 100) ──────────────────────────────────
    public float hunger;            // 0 → Health 감소 시작
    public float thirst;            // 0 → Health 감소 (hunger보다 빠름)
    public float mood;
    public float health;            // 마지막 방어선. 감소 매우 느림
    public float cleanliness;       // 20 이하 → Mood에 패널티

    // ── 관계 ───────────────────────────────────────────────────
    public float affection;         // 0 ~ 100. 감소 없음. 특별 반응 해금 조건

    // ── 시간 ───────────────────────────────────────────────────
    public long  createdAtTicks;    // DateTime.UtcNow.Ticks  (생성 시점)
    public long  lastUpdatedTicks;  // ← 핵심! 경과 시간 계산 기준. 진행 보정 때마다 갱신

    // ── 기타 ───────────────────────────────────────────────────
    public bool  isFavorite;        // 목록 상단 고정
    public int   giftDay;           // 어덜트의 선물을 마지막으로 받은 날 (UTC 날짜 번호, RewardManager.ClaimGift)

    // ── 생성 ───────────────────────────────────────────────────
    public const float START_STAT = 80f;   // [TBD] 새 게코의 배고픔·목마름·기분·건강·청결 시작값

    /// <summary>새 게코 한 마리. 기본 게코(하코)와 분양 게코 모두 이것으로 만든다.</summary>
    public static GeckoData CreateNew(string name, string speciesId)
    {
        long now = DateTime.UtcNow.Ticks;
        return new GeckoData
        {
            id               = Guid.NewGuid().ToString(),
            name             = name,
            speciesId        = speciesId,
            growthStage      = 0,
            hunger           = START_STAT,
            thirst           = START_STAT,
            mood             = START_STAT,
            health           = START_STAT,
            cleanliness      = START_STAT,
            affection        = 0f,
            createdAtTicks   = now,
            lastUpdatedTicks = now,
        };
    }
}
