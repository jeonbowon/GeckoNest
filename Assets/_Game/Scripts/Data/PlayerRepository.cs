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
}
