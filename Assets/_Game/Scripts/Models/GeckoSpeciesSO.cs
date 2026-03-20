using UnityEngine;

[CreateAssetMenu(menuName = "Hako/GeckoSpecies")]
public class GeckoSpeciesSO : ScriptableObject
{
    public string speciesId;   // "crested" | "leopard" | "gargoyle"
    public string displayName;
    public Sprite thumbnailSprite;
    public RuntimeAnimatorController animController;
    public int coinPrice;
    public bool isUnlockedByDefault;
}
