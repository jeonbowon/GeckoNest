using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Hako/Item")]
public class ItemSO : ScriptableObject
{
    public string itemId;
    public string displayName;
    public Sprite icon;
    public int    coinPrice;
    public int    gemPrice;

    // 상태값 회복량
    public float hungerRestore; // [TBD]
    public float thirstRestore; // 물 주기 전용 (GiveWater에서 직접 처리)
    public float moodBonus;     // [TBD]
    public float healthRestore; // 칼슘+비타민 더스팅 등 영양 보충제에 사용
    public float growthExpGain; // 귀뚜라미+1 / 것로딩+2 / 밀웜+3 / 두비아+4 / 슈퍼밀웜+5 / 성장촉진제+8

    public string[] preferredSpeciesIds; // 선호 종 ID 목록
}
