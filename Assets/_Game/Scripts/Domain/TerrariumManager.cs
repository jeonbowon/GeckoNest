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
    /// 테마를 이미 가지고 있는지 (무료 · 산 적 있음 · 지금 적용 중).
    /// 장식은 놓을 때마다 값을 내므로 항상 false. 바닥은 테마에 합쳐져 더 팔지 않는다 (2026-09-21)
    /// </summary>
    public bool IsOwned(DecorItemSO item)
    {
        if (item == null || item.category != DecorCategory.Background) return false;
        if (item.coinPrice <= 0 && item.gemPrice <= 0) return true;

        var t = GetData();
        return item.itemId == t.backgroundId
            || (t.ownedDecorIds != null && t.ownedDecorIds.Contains(item.itemId));
    }

    // ── 잠금 (어덜트 전용 장식) ────────────────────────────────

    /// <summary>키운 어덜트 수 (같은 종도 센다)</summary>
    public int AdultsRaised
    {
        get
        {
            var p = _repo.GetPlayerData().progress;
            return p != null ? p.adultCount : 0;
        }
    }

    public bool IsUnlocked(DecorItemSO item) => IsUnlocked(item, AdultsRaised);

    public static bool IsUnlocked(DecorItemSO item, int adultsRaised)
        => item != null && adultsRaised >= item.requiredAdults;

    /// <summary>
    /// 어덜트 수가 before → after로 늘면서 새로 열린 장식 (홈 알림용). 없으면 빈 목록
    /// </summary>
    public static System.Collections.Generic.List<DecorItemSO> NewlyUnlocked(
        System.Collections.Generic.IEnumerable<DecorItemSO> items, int before, int after)
    {
        var list = new System.Collections.Generic.List<DecorItemSO>();
        if (items == null) return list;
        foreach (var item in items)
            if (item != null && item.requiredAdults > before && item.requiredAdults <= after) list.Add(item);
        return list;
    }

    /// <summary>테마 구매 기록. 저장은 이어서 부르는 SetBackground에서.</summary>
    public void MarkOwned(string itemId)
    {
        var t = GetData();
        t.ownedDecorIds ??= new System.Collections.Generic.List<string>();
        if (!string.IsNullOrEmpty(itemId) && !t.ownedDecorIds.Contains(itemId))
            t.ownedDecorIds.Add(itemId);
    }

    // ── 테마 ──────────────────────────────────────────────────
    // 테마 = 뒷벽과 바닥이 한 장에 그려진 배경 (2026-09-21 배경·바닥을 합쳤다 — 바닥 띠는 하단 탭에 가려 보이지 않았고,
    // 게코는 배경 그림 위를 걸었으며, 섞으면 사막 배경 + 정글 흙처럼 어긋났다)

    public void SetBackground(string itemId)
    {
        _repo.GetPlayerData().terrarium.backgroundId = itemId;
        _repo.Save();
        Debug.Log($"[TerrariumManager] 테마 변경 — {itemId}");
        OnTerrariumChanged?.Invoke();
    }

    // ── 장식 슬롯 ─────────────────────────────────────────────

    /// <summary>slot: 0~3. itemId가 null이면 해당 슬롯 비움. 장식이 바뀌면 그 칸은 기본 자리로</summary>
    public void SetDecor(int slot, string itemId)
    {
        var slots = EnsureSlots();
        if (slot < 0 || slot >= slots.Length)
        {
            Debug.LogWarning($"[TerrariumManager] 유효하지 않은 슬롯 인덱스: {slot}");
            return;
        }

        slots[slot] = itemId;
        GetData().decorPositions[slot] = Vector2.zero;
        _repo.Save();
        Debug.Log($"[TerrariumManager] 장식 슬롯[{slot}] 변경 — {itemId ?? "비움"}");
        OnTerrariumChanged?.Invoke();
    }

    public void ClearDecor(int slot) => SetDecor(slot, null);

    /// <summary>
    /// 홈 편집 모드에서 옮긴 위치 저장 (TerrariumLayout 좌표). 범위 자르기는 화면 폭을 아는 UI가 먼저 한다 —
    /// 여기서는 높이 범위와 벽 높이만 다시 맞춘다. 빈 칸이면 무시
    /// </summary>
    public void SetDecorPosition(int slot, Vector2 anchor)
    {
        var slots = EnsureSlots();
        if (slot < 0 || slot >= slots.Length || string.IsNullOrEmpty(slots[slot])) return;

        anchor = TerrariumLayout.PlacementOf(slot) == DecorPlacement.Wall
            ? new Vector2(anchor.x, TerrariumLayout.WALL_Y)
            : new Vector2(anchor.x, Mathf.Clamp(anchor.y, TerrariumLayout.FLOOR_MIN_Y, TerrariumLayout.FLOOR_MAX_Y));
        if (anchor == Vector2.zero) anchor.x = 0.01f;   // (0,0)은 "기본 자리" 표시라 피한다

        GetData().decorPositions[slot] = anchor;
        _repo.Save();
        Debug.Log($"[TerrariumManager] 장식 슬롯[{slot}] 위치 — {anchor}");
        OnTerrariumChanged?.Invoke();
    }

    // ── 칸 종류 (바닥 0·1 / 뒷벽 2·3 — TerrariumLayout) ───────

    /// <summary>이 장식을 이 칸에 놓을 수 있는가</summary>
    public static bool Fits(DecorItemSO item, int slot)
        => item != null && slot >= 0 && slot < TerrariumLayout.SlotCount
           && item.placement == TerrariumLayout.PlacementOf(slot);

    /// <summary>이 장식을 놓을 수 있는 빈 칸. 없으면 -1</summary>
    public int FindEmptySlot(DecorItemSO item)
    {
        var slots = EnsureSlots();
        for (int i = 0; i < slots.Length; i++)
            if (string.IsNullOrEmpty(slots[i]) && Fits(item, i)) return i;
        return -1;
    }

    /// <summary>
    /// 예전 저장 정리 (앱 시작) — 칸 종류가 생기기 전에는 아무 칸에나 놓였다.
    /// 맞지 않는 장식은 맞는 빈 칸(기본 자리)으로 옮기고, 자리가 없으면 빼고 값을 돌려준다. 돌려준 코인을 반환.
    /// find = 장식 id → 에셋 (DecorCatalog.Find). 모르는 id는 그대로 둔다.
    /// </summary>
    public int NormalizeSlots(Func<string, DecorItemSO> find)
    {
        var data      = _repo.GetPlayerData();
        var slots     = EnsureSlots();
        var positions = GetData().decorPositions;
        bool changed = false;
        int  refund  = 0;

        for (int i = 0; i < slots.Length; i++)
        {
            var item = find != null && !string.IsNullOrEmpty(slots[i]) ? find(slots[i]) : null;
            if (item == null || Fits(item, i)) continue;

            string id = slots[i];
            slots[i]     = null;
            positions[i] = Vector2.zero;
            int j = FindEmptySlot(item);
            if (j >= 0)
            {
                slots[j]     = id;
                positions[j] = Vector2.zero;
                Debug.Log($"[TerrariumManager] 장식 칸 정리 — {id}: 칸 {i} → {j}");
            }
            else
            {
                data.coin += item.coinPrice;
                data.gem  += item.gemPrice;
                refund    += item.coinPrice;
                Debug.Log($"[TerrariumManager] 장식 칸 정리 — {id}: 맞는 빈 칸이 없어 빼고 코인 {item.coinPrice} · 젬 {item.gemPrice} 환불");
            }
            changed = true;
        }

        if (changed)
        {
            _repo.Save();
            OnTerrariumChanged?.Invoke();
        }
        return refund;
    }

    // 칸·위치 배열 길이를 칸 수에 맞춘다 (예전 저장 · 손상 대비)
    private string[] EnsureSlots()
    {
        var t = GetData();
        int n = TerrariumLayout.SlotCount;
        if (t.decorSlots == null || t.decorSlots.Length != n)
        {
            var fixedSlots = new string[n];
            if (t.decorSlots != null) Array.Copy(t.decorSlots, fixedSlots, Mathf.Min(t.decorSlots.Length, n));
            t.decorSlots = fixedSlots;
        }
        if (t.decorPositions == null || t.decorPositions.Length != n)
        {
            var fixedPositions = new Vector2[n];
            if (t.decorPositions != null) Array.Copy(t.decorPositions, fixedPositions, Mathf.Min(t.decorPositions.Length, n));
            t.decorPositions = fixedPositions;
        }
        return t.decorSlots;
    }
}
