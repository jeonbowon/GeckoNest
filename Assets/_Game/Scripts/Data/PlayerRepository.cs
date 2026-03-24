using UnityEngine;

public class PlayerRepository
{
    private readonly SaveManager _save;
    private PlayerData _cache;

    public PlayerRepository(SaveManager save)
    {
        _save = save;
    }

    public PlayerData GetPlayerData()
    {
        if (_cache == null)
            _cache = _save.Load();
        return _cache;
    }

    public GeckoData GetGecko(string id)
    {
        var data = GetPlayerData();
        return data.geckos.Find(g => g.id == id);
    }

    public void UpdateGecko(GeckoData gecko)
    {
        var data = GetPlayerData();
        int index = data.geckos.FindIndex(g => g.id == gecko.id);
        if (index < 0)
        {
            Debug.LogWarning($"[PlayerRepository] 게코를 찾을 수 없음: {gecko.id}");
            return;
        }
        data.geckos[index] = gecko;
    }

    public void Save()
    {
        if (_cache == null) return;
        _save.Save(_cache);
    }

    // ── 인벤토리 ──────────────────────────────────────────────

    /// <summary>itemId 아이템을 count만큼 추가. 슬롯이 없으면 새로 생성.</summary>
    public void AddItem(string itemId, int count = 1)
    {
        var data = GetPlayerData();
        var stack = data.inventory.Find(s => s.itemId == itemId);
        if (stack != null)
            stack.count += count;
        else
            data.inventory.Add(new ItemStack(itemId, count));
    }

    /// <summary>
    /// itemId 아이템을 count만큼 차감. 성공하면 true, 수량 부족이면 false (차감 안 함).
    /// </summary>
    public bool RemoveItem(string itemId, int count = 1)
    {
        var data = GetPlayerData();
        var stack = data.inventory.Find(s => s.itemId == itemId);
        if (stack == null || stack.count < count) return false;
        stack.count -= count;
        return true;
    }

    /// <summary>현재 보유 수량 반환. 없으면 0.</summary>
    public int GetItemCount(string itemId)
    {
        var data = GetPlayerData();
        var stack = data.inventory.Find(s => s.itemId == itemId);
        return stack?.count ?? 0;
    }
}
