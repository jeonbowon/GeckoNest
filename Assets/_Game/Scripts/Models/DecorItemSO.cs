using UnityEngine;

public enum DecorCategory { Background, Floor, Decoration }

/// <summary>장식을 놓는 곳 — 바닥 칸(0·1) / 뒷벽 칸(2·3). 칸 자리는 TerrariumLayout</summary>
public enum DecorPlacement { Floor, Wall }

/// <summary>게코가 장식을 어떻게 쓰는가 (GeckoMovementAI)</summary>
public enum DecorUse
{
    None,         // 보기만 하는 장식 (바위·화분)
    Hide,         // 은신처 — 들어가 쉰다
    ClimbPanel,   // 코르크 뒤판 — 판 위를 오르내린다
    Branch,       // 나뭇가지 — 올라가 위쪽 가로 부분에서 엎드려 쉰다
    Vine,         // 덩굴 — 세로로 오르내린다
}

/// <summary>놓아 두면 생기는 작은 효과 — 수치와 뜻은 DecorPerks (2026-09-21)</summary>
public enum DecorPerk
{
    None,
    MoltRub,    // 이끼 바위 — 허물 성공률
    Droplets,   // 화분 — 물 줄 때 목마름 더
    Basking,    // 바위 — 건강 회복 더
    Shelter,    // 은신처 — 기분이 덜 떨어짐
    Play,       // 구조물 — 쓰다듬기 애정도 더
}

[CreateAssetMenu(fileName = "DecorItem", menuName = "Hako/DecorItem")]
public class DecorItemSO : ScriptableObject
{
    public string       itemId;
    public string       displayName;
    public Sprite       icon;           // UI 썸네일
    public Sprite       previewSprite;  // 홈 화면에 실제 표시될 이미지
    public DecorCategory category;
    public int          coinPrice;
    public int          gemPrice;

    [Header("장식 (category = Decoration)")]
    public DecorPlacement placement;    // 바닥 칸 / 뒷벽 칸
    public DecorUse       use;          // 게코가 쓰는 법
    [Tooltip("그림 아래 투명 여백 비율 (0~1) — 이 높이가 바닥에 닿게 놓는다. 나뭇가지는 TerrariumLayout 선 위치를 따른다")]
    [Range(0f, 1f)] public float baseline;
    [Tooltip("놓아 두면 생기는 효과 (DecorPerks) — 꾸미기 카드에 한 줄로 보인다")]
    public DecorPerk      perk;

    [Header("잠금")]
    [Tooltip("어덜트를 이만큼 키워야 열린다 (ProgressData.adultCount). 0 = 처음부터")]
    [Min(0)] public int requiredAdults;
}
