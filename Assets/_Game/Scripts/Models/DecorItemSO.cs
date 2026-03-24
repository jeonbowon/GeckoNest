using UnityEngine;

public enum DecorCategory { Background, Floor, Decoration }

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
}
