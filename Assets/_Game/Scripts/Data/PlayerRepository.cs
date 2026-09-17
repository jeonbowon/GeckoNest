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

    // ── 새 플레이어 ───────────────────────────────────────────

    private const string STARTER_SPECIES_ID = "crested";
    private const string STARTER_FOOD_ID    = "cricket_small";
    private const int    STARTER_FOOD_COUNT = 3;

    /// <summary>
    /// 게코가 한 마리도 없으면 기본 게코(하코 / Hako)를 선택 상태로 넣고 첫 먹이를 준다.
    /// 새 플레이어 · 저장 파일 손상 복구 공통. 넣었으면 true. 저장은 호출한 쪽에서.
    /// 이름은 생성 시점의 언어로 정해져 저장된다 (나중에 언어가 바뀌어도 사용자 데이터라 그대로).
    /// </summary>
    public bool EnsureStarterGecko()
    {
        var data = GetPlayerData();
        if (data.geckos.Count > 0) return false;

        var gecko = GeckoData.CreateNew(Loc.Get("gecko.default_name"), STARTER_SPECIES_ID);
        data.geckos.Add(gecko);
        data.selectedGeckoId = gecko.id;
        RewardManager.RecordMet(data, STARTER_SPECIES_ID, reward: false);   // 도감 "만남" — 기본 게코는 보상 없이
        AddItem(STARTER_FOOD_ID, STARTER_FOOD_COUNT);
        return true;
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
