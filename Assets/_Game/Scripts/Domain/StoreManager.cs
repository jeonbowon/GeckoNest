using System;
using UnityEngine;

public class StoreManager
{
    private readonly PlayerRepository _repo;

    // UI 통지용 이벤트
    public event Action<string, int> OnItemPurchased;  // (itemId, newCount)
    public event Action<GeckoData>   OnGeckoPurchased; // 새 게코 분양 완료
    public event Action<string>      OnPurchaseFailed; // 실패 사유 메시지 (현재 언어)

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
            OnPurchaseFailed?.Invoke(Loc.Get("common.not_found"));
            return;
        }

        var data = _repo.GetPlayerData();

        if (item.gemPrice > 0)
        {
            int total = item.gemPrice * count;
            if (data.gem < total)
            {
                OnPurchaseFailed?.Invoke(Loc.Format("common.need_gem_have", total, data.gem));
                return;
            }
            data.gem -= total;
        }
        else
        {
            int total = item.coinPrice * count;
            if (data.coin < total)
            {
                OnPurchaseFailed?.Invoke(Loc.Format("common.need_coin_have", total, data.coin));
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

    public const int MAX_GECKOS = 5;   // [TBD] 한 번에 키울 수 있는 마릿수

    /// <summary>마지막 분양에서 도감 새 종 보상으로 받은 코인 (없으면 0) — OnGeckoPurchased 전에 채운다</summary>
    public int LastMeetCoin { get; private set; }

    /// <summary>게코를 더 들일 수 있는가 (MAX_GECKOS 미만)</summary>
    public static bool CanAdoptMore(PlayerData data)
        => data == null || data.geckos == null || data.geckos.Count < MAX_GECKOS;

    /// <summary>
    /// 무료로 분양받을 수 있는가 — isUnlockedByDefault 종이고, 그 종을 한 마리도 키우고 있지 않을 때만.
    /// 처음 받은 하코가 크레스티드이므로 크레스티드도 보통은 coinPrice를 낸다 (무료 반복 분양 → 어덜트 보상 반복 방지).
    /// </summary>
    public static bool IsFreeFor(PlayerData data, GeckoSpeciesSO species)
    {
        if (species == null || !species.isUnlockedByDefault) return false;
        if (data == null || data.geckos == null) return true;
        return !data.geckos.Exists(g => g != null && g.speciesId == species.speciesId);
    }

    /// <summary>
    /// GeckoSpeciesSO 기준으로 새 게코를 분양. 코인 차감 후 geckos 목록에 추가.
    /// 무료 조건은 IsFreeFor, 마릿수는 MAX_GECKOS까지. 이름을 비우면 종 이름(현재 언어)을 쓴다.
    /// </summary>
    public void BuyGecko(GeckoSpeciesSO species, string geckoName)
    {
        if (species == null)
        {
            Debug.LogWarning("[StoreManager] BuyGecko: species가 null");
            OnPurchaseFailed?.Invoke(Loc.Get("common.not_found"));
            return;
        }

        var data = _repo.GetPlayerData();

        if (!CanAdoptMore(data))
        {
            OnPurchaseFailed?.Invoke(Loc.Format("geckolist.full", MAX_GECKOS));
            return;
        }

        if (!IsFreeFor(data, species))
        {
            if (data.coin < species.coinPrice)
            {
                OnPurchaseFailed?.Invoke(Loc.Format("common.need_coin_have", species.coinPrice, data.coin));
                return;
            }
            data.coin -= species.coinPrice;
        }

        var gecko = GeckoData.CreateNew(
            string.IsNullOrWhiteSpace(geckoName) ? Loc.SpeciesName(species) : geckoName,
            species.speciesId);

        data.geckos.Add(gecko);
        LastMeetCoin = RewardManager.RecordMet(data, species.speciesId, reward: true) ? RewardManager.BOOK_MEET_COIN : 0;
        _repo.Save();

        Debug.Log($"[StoreManager] 게코 분양 완료 — {gecko.name} ({species.speciesId})");
        OnGeckoPurchased?.Invoke(gecko);
    }
}
