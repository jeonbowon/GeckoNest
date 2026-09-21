/// <summary>
/// 장식 효과 (2026-09-21) — 놓아 두기만 하면 생기는 작은 효과. 실제 게코 습성에서 가져왔다.
///
///   이끼 바위 MoltRub  — 거친 곳에 몸을 비벼 허물을 벗는다 → 허물 성공률 +10%
///   화분     Droplets — 크레스티드는 잎에 맺힌 물방울을 핥아 마신다 → 물 줄 때 목마름 +10 더
///   바위     Basking  — 따뜻한 돌에 기대 몸을 데운다 → 건강 회복 +50% (0.5 → 0.75/h)
///   은신처   Shelter  — 숨을 곳이 있으면 안심한다 → 기분이 20% 덜 떨어진다
///   구조물   Play     — 타고 놀 거리가 있다 → 쓰다듬기 애정도 +1
///
/// 모두 **"놓여 있는가"만** 본다 — 앱을 꺼 둔 동안(시간 보정)에도 그대로 계산되고, 같은 효과는 겹치지 않는다.
/// 게코가 장식에 가서 하는 행동(비비기·핥기·쉬기)은 보여 주기일 뿐 효과와는 따로다 (GeckoMovementAI.VisitDecor).
/// 수치는 모두 [TBD].
/// </summary>
public static class DecorPerks
{
    public const float MOLT_RUB_BONUS      = 0.10f;   // [TBD] 허물 성공률
    public const float DROPLET_WATER_BONUS = 10f;     // [TBD] 물 줄 때 목마름 추가
    public const float BASK_HEALTH_REGEN   = 0.25f;   // [TBD] /h — 건강 회복에 더한다 (회복 조건은 그대로)
    public const float SHELTER_MOOD_MUL    = 0.80f;   // [TBD] 시간당 기분 감소에 곱한다
    public const float PLAY_PET_AFFECTION  = 1f;      // [TBD] 쓰다듬기 애정도 추가

    /// <summary>이 효과를 주는 장식이 맞는 칸에 놓여 있는가</summary>
    public static bool Has(TerrariumData t, DecorPerk perk)
    {
        if (perk == DecorPerk.None || t == null || t.decorSlots == null) return false;
        for (int i = 0; i < t.decorSlots.Length; i++)
        {
            var item = DecorCatalog.Find(t.decorSlots[i]);
            if (item != null && item.perk == perk && TerrariumManager.Fits(item, i)) return true;
        }
        return false;
    }
}
