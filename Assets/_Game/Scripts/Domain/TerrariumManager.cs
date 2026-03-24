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
