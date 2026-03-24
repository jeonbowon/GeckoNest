using System;

/// <summary>
/// 인벤토리 슬롯 — itemId + 보유 수량.
/// PlayerData.inventory 에서 사용. JsonUtility 호환을 위해 Dictionary 대신 List로 관리.
/// </summary>
[Serializable]
public class ItemStack
{
    public string itemId;
    public int    count;

    public ItemStack() { }
    public ItemStack(string id, int cnt) { itemId = id; count = cnt; }
}
