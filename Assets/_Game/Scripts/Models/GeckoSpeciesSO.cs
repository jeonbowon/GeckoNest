using UnityEngine;

[CreateAssetMenu(menuName = "Hako/GeckoSpecies")]
public class GeckoSpeciesSO : ScriptableObject
{
    public string speciesId;   // "crested" | "leopard" | "gargoyle"
    public string displayName;
    public Sprite thumbnailSprite;
    [Tooltip("사용하지 않음 — 게코 움직임은 GeckoMotor가 코드로 계산한다")]
    public RuntimeAnimatorController animController;
    public int coinPrice;
    public bool isUnlockedByDefault;

    [Header("홈 화면 게코")]
    [Tooltip("이 종의 그림. 비워 두면 씬 게코(GeckoRig)에 들어 있는 기본 그림을 쓴다")]
    public GeckoSkin skin;
    [Tooltip("눈꺼풀이 있는 종만 켠다 (레오파드). 크레스티드·가고일은 눈꺼풀이 없어 혀로 눈을 닦는다")]
    public bool canBlink;
    [Tooltip("전용 스킨에 종의 기본색과 노멀 무늬가 그려져 있으면 켠다")]
    public bool hasPaintedBaseColor;
    [Tooltip("바닥과 은신처를 이용하고 유리벽과 높은 장식에는 오르지 않는다")]
    public bool groundDweller;
    [Tooltip("천천히 이동하고 네 발을 조금씩 다른 박자로 옮긴다")]
    public bool useSlowGait;
}
