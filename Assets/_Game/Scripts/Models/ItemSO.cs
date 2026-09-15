using UnityEngine;

/// <summary>먹이 종류 — 먹을 때 게코 반응이 달라진다 (HomeUIController.PlayFeedReaction)</summary>
public enum FoodKind
{
    Normal     = 0,   // 혀로 받아먹기
    Big        = 1,   // 받아먹고 오래 오물오물 (두비아·슈퍼밀웜)
    Supplement = 2,   // 가루를 뿌려 할짝 (영양제·성장촉진제)
}

[CreateAssetMenu(fileName = "Item", menuName = "Hako/Item")]
public class ItemSO : ScriptableObject
{
    public string itemId;
    public string displayName;
    public Sprite icon;
    public int    coinPrice;
    public int    gemPrice;

    [Header("먹었을 때 효과")]
    public float hungerRestore; // [TBD]
    public float thirstRestore; // 물 주기 전용 (GiveWater에서 직접 처리)
    public float moodBonus;     // [TBD]
    public float healthRestore; // 칼슘+비타민 더스팅 등 영양 보충제에 사용
    [Tooltip("성장치 1 = 성장 일수 3시간 앞당김, 필요한 실제 날짜의 최대 30%까지 (GeckoManager.EffectiveAgeDays)")]
    public float growthExpGain;
    [Tooltip("다음 허물 1회 성공률 가산 (0.05 = +5%). 여러 번 먹어도 최대 +15%, 허물 판정 뒤 사라진다")]
    public float moltBonus;

    [Header("반응")]
    public FoodKind kind;
    [Tooltip("이 먹이를 좋아하는 종 ID — 애정도 2배 · 기분 +3 · 기뻐하는 반응")]
    public string[] preferredSpeciesIds;
}
