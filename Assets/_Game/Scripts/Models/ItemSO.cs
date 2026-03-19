using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Hako/Item")]
public class ItemSO : ScriptableObject
{
    public string itemId;
    public string displayName;
    public Sprite icon;
    public int    coinPrice;
    public int    gemPrice;

    // 상태값 회복량 ([TBD] 수치는 09장 밸런스 참고)
    public float hungerRestore; // [TBD] 귀뚜라미 30, 밀웜 20, 과일퓌레 15
    public float thirstRestore; // 물 주기 전용 (GiveWater에서 직접 처리)
    public float moodBonus;     // [TBD] 밀웜 +5, 과일퓌레 +15
    public float growthExpGain; // [TBD] 귀뚜라미 +5, 밀웜 +10, 과일퓌레 +3

    public string[] preferredSpeciesIds; // 선호 종 ID 목록
}
