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
    public int   growthStage;       // 0=Baby  1=Juvenile  2=Sub-Adult  3=Adult
    public float growthExp;         // 누적 성장치. 단계 전환 후 0 리셋
    public float moltProgress;      // 0 ~ 100. 100 이상 → TryMolt() 호출
    public int   moltCount;         // 누적 허물 횟수 (도감, 성장 조건에 사용)

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
    public long  lastUpdatedTicks;  // ← 핵심! 앱 닫기 직전마다 갱신 필수

    // ── 기타 ───────────────────────────────────────────────────
    public bool  isFavorite;        // 목록 상단 고정
}
