using System;
using UnityEngine;

public class TerrariumManager
{
    private readonly PlayerRepository _repo;

    public event Action OnTerrariumChanged;

    public TerrariumManager(PlayerRepository repo)
    {
        _repo = repo;
    }

    public TerrariumData GetData() => _repo.GetPlayerData().terrarium;

    // ── 보유 ──────────────────────────────────────────────────

    /// <summary>
    /// 배경·바닥을 이미 가지고 있는지 (무료 · 산 적 있음 · 지금 적용 중).
    /// 장식은 놓을 때마다 값을 내므로 항상 false.
    /// </summary>
    public bool IsOwned(DecorItemSO item)
    {
        if (item == null || item.category == DecorCategory.Decoration) return false;
        if (item.coinPrice <= 0 && item.gemPrice <= 0) return true;

        var t = GetData();
        return item.itemId == t.backgroundId
            || item.itemId == t.floorId
            || (t.ownedDecorIds != null && t.ownedDecorIds.Contains(item.itemId));
    }

    /// <summary>배경·바닥 구매 기록. 저장은 이어서 부르는 Set*에서.</summary>
    public void MarkOwned(string itemId)
    {
        var t = GetData();
        t.ownedDecorIds ??= new System.Collections.Generic.List<string>();
        if (!string.IsNullOrEmpty(itemId) && !t.ownedDecorIds.Contains(itemId))
            t.ownedDecorIds.Add(itemId);
    }

    // ── 배경 / 바닥 ───────────────────────────────────────────

    public void SetBackground(string itemId)
    {
        _repo.GetPlayerData().terrarium.backgroundId = itemId;
        _repo.Save();
        Debug.Log($"[TerrariumManager] 배경 변경 — {itemId}");
        OnTerrariumChanged?.Invoke();
    }

    public void SetFloor(string itemId)
    {
        _repo.GetPlayerData().terrarium.floorId = itemId;
        _repo.Save();
        Debug.Log($"[TerrariumManager] 바닥 변경 — {itemId}");
        OnTerrariumChanged?.Invoke();
    }

    // ── 장식 슬롯 ─────────────────────────────────────────────

    /// <summary>slot: 0~3. itemId가 null이면 해당 슬롯 비움.</summary>
    public void SetDecor(int slot, string itemId)
    {
        var terrarium = _repo.GetPlayerData().terrarium;
        if (slot < 0 || slot >= terrarium.decorSlots.Length)
        {
            Debug.LogWarning($"[TerrariumManager] 유효하지 않은 슬롯 인덱스: {slot}");
            return;
        }

        terrarium.decorSlots[slot] = itemId;
        _repo.Save();
        Debug.Log($"[TerrariumManager] 장식 슬롯[{slot}] 변경 — {itemId ?? "비움"}");
        OnTerrariumChanged?.Invoke();
    }

    public void ClearDecor(int slot) => SetDecor(slot, null);
}
