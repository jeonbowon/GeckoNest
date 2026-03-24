using System;
using UnityEngine;

public class StoreManager
{
    private readonly PlayerRepository _repo;

    // UI 통지용 이벤트
    public event Action<string, int> OnItemPurchased;  // (itemId, newCount)
    public event Action<GeckoData>   OnGeckoPurchased; // 새 게코 분양 완료
    public event Action<string>      OnPurchaseFailed; // 실패 사유 메시지

    public StoreManager(PlayerRepository repo)
    {
        _repo = repo;
    }

    // ── 아이템 구매 ───────────────────────────────────────────

    /// <summary>
    /// ItemSO를 coinPrice / gemPrice 기준으로 구매. count 개만큼 인벤토리에 추가.
    /// 재화 부족 또는 item=null 이면 OnPurchaseFailed 발생.
    /// </summary>
    public void BuyItem(ItemSO item, int count = 1)
    {
        if (item == null)
        {
            Debug.LogWarning("[StoreManager] BuyItem: item이 null");
            OnPurchaseFailed?.Invoke("Item not found.");
            return;
        }

        var data = _repo.GetPlayerData();

        if (item.gemPrice > 0)
        {
            int total = item.gemPrice * count;
            if (data.gem < total)
            {
                OnPurchaseFailed?.Invoke($"젬이 부족합니다. (필요: {total}, 보유: {data.gem})");
                return;
            }
            data.gem -= total;
        }
        else
        {
            int total = item.coinPrice * count;
            if (data.coin < total)
            {
                OnPurchaseFailed?.Invoke($"코인이 부족합니다. (필요: {total}, 보유: {data.coin})");
                return;
            }
            data.coin -= total;
        }

        _repo.AddItem(item.itemId, count);
        _repo.Save();

        int newCount = _repo.GetItemCount(item.itemId);
        Debug.Log($"[StoreManager] 구매 완료 — {item.displayName} x{count} (보유: {newCount})");
        OnItemPurchased?.Invoke(item.itemId, newCount);
    }

    // ── 게코 분양 ─────────────────────────────────────────────

    /// <summary>
    /// GeckoSpeciesSO 기준으로 새 게코를 분양. 코인 차감 후 geckos 목록에 추가.
    /// isUnlockedByDefault == true면 무료.
    /// </summary>
    public void BuyGecko(GeckoSpeciesSO species, string geckoName)
    {
        if (species == null)
        {
            Debug.LogWarning("[StoreManager] BuyGecko: species가 null");
            OnPurchaseFailed?.Invoke("Species not found.");
            return;
        }

        var data = _repo.GetPlayerData();

        if (!species.isUnlockedByDefault)
        {
            if (data.coin < species.coinPrice)
            {
                OnPurchaseFailed?.Invoke($"코인이 부족합니다. (필요: {species.coinPrice}, 보유: {data.coin})");
                return;
            }
            data.coin -= species.coinPrice;
        }

        var gecko = new GeckoData
        {
            id               = System.Guid.NewGuid().ToString(),
            name             = string.IsNullOrWhiteSpace(geckoName) ? species.displayName : geckoName,
            speciesId        = species.speciesId,
            growthStage      = 0,
            hunger           = 80f,
            thirst           = 80f,
            mood             = 80f,
            health           = 80f,
            cleanliness      = 80f,
            affection        = 0f,
            createdAtTicks   = DateTime.UtcNow.Ticks,
            lastUpdatedTicks = DateTime.UtcNow.Ticks,
        };

        data.geckos.Add(gecko);
        _repo.Save();

        Debug.Log($"[StoreManager] 게코 분양 완료 — {gecko.name} ({species.speciesId})");
        OnGeckoPurchased?.Invoke(gecko);
    }
}
