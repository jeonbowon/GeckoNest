using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 테라리움 장식 칸 배치 — 칸 7개의 종류·기본 자리·옮길 수 있는 범위, 장식 그림 크기, 게코가 타는 경로.
/// 좌표는 GeckoArea 아래 가운데 기준 (GeckoObject.anchoredPosition과 같은 좌표, 1080×2400 기준).
///
///   칸 0 바닥 왼쪽 · 1 바닥 오른쪽 · 4 바닥 가운데 앞 · 5 바닥 가운데 뒤 — 은신처·바위·화분. 기준점 = 장식이 바닥에 닿는 곳 (은신처 안 게코 발 위치)
///   칸 2 뒷벽 왼쪽 · 3 뒷벽 오른쪽 · 6 뒷벽 가운데 — 코르크 뒤판·덩굴·나뭇가지. 기준점 = 게코가 올라서는 밑동 (높이는 WALL_Y 고정)
///
/// 칸 수는 고정이고, 홈 편집 모드에서 범위 안으로만 끌어 옮길 수 있다 (ClampAnchor · TooClose).
/// 옮긴 위치는 TerrariumData.decorPositions — (0,0)이면 기본 자리. 홈 화면·이동 AI·임시 그림 생성기가 같은 값을 쓴다.
/// </summary>
public static class TerrariumLayout
{
    public const int SlotCount = 7;   // 바닥 4 · 뒷벽 3 (2026-09-17 사용자 결정, 예전 바닥 2 · 뒷벽 2)

    // 기본 자리
    public const float FLOOR_X = 300f;     // [TBD] 바닥 칸 가로 (가운데에서)
    public const float FLOOR_Y = 600f;     // [TBD] 바닥 칸 — 장식이 바닥에 닿는 높이
    public const float WALL_X  = 290f;     // [TBD] 뒷벽 칸 가로
    public const float WALL_Y  = 760f;     // [TBD] 뒷벽 구조물 밑동 높이 (옮겨도 고정)

    public const float WALL_FOOT    = 30f;  // 뒤판·덩굴에 발을 올리는 높이 (밑동 위)
    public const float WALL_TOP_GAP = 160f; // 뒤판·덩굴 위 끝에서 이만큼 아래까지만 오른다

    // 옮길 수 있는 범위 (홈 편집 모드)
    public const float FLOOR_MIN_Y   = 420f;  // [TBD] 게코가 다니는 바닥(380~) 안
    public const float FLOOR_MAX_Y   = 740f;  // [TBD] 뒷벽 구조물 밑동(760)보다 앞
    public const float EDGE_MARGIN   = 10f;   // 그림이 화면 밖으로 나가지 않게
    public const float BRANCH_MAX_X  = 430f;  // [TBD] 나뭇가지 밑동 가로 한계 (게코가 올라서는 곳)
    public const float MIN_GAP_FLOOR = 220f;  // [TBD] 바닥 장식끼리 최소 거리
    public const float MIN_GAP_WALL  = 200f;  // [TBD] 벽 구조물끼리 최소 가로 거리

    // 나뭇가지 그림 — 왼쪽 아래 원점, 오른쪽 위로 뻗는다. 게코 발이 지나는 선(가운데 선)을 그림과 경로가 함께 쓴다
    public static readonly Vector2   BRANCH_SIZE = new Vector2(480f, 600f);
    public static readonly Vector2[] BRANCH_LINE = { new Vector2(60f, 40f), new Vector2(300f, 440f), new Vector2(440f, 440f) };
    public const float BRANCH_THICK = 44f;   // 가지 굵기 (밑동 쪽) — 발은 선보다 굵기 절반 위

    // 칸 종류 — 예전 저장의 0~3번은 그대로 두고 뒤에 추가했다 (바닥 0·1·4·5 / 뒷벽 2·3·6)
    private static readonly DecorPlacement[] PLACEMENTS =
    {
        DecorPlacement.Floor, DecorPlacement.Floor, DecorPlacement.Wall, DecorPlacement.Wall,
        DecorPlacement.Floor, DecorPlacement.Floor, DecorPlacement.Wall,
    };

    // 새 칸의 기본 자리 — 바닥은 가운데 앞(왼편)·가운데 뒤(오른편), 뒷벽은 가운데. 서로 MIN_GAP_* 이상 떨어진다
    public const float FLOOR_FRONT_X = 90f;    // [TBD]
    public const float FLOOR_FRONT_Y = 450f;   // [TBD]
    public const float FLOOR_BACK_Y  = 730f;   // [TBD]

    public static DecorPlacement PlacementOf(int slot)
        => slot >= 0 && slot < PLACEMENTS.Length ? PLACEMENTS[slot] : DecorPlacement.Floor;

    /// <summary>이 종류의 칸 수 (바닥 4 · 뒷벽 3)</summary>
    public static int CountOf(DecorPlacement placement)
    {
        int n = 0;
        for (int i = 0; i < SlotCount; i++) if (PlacementOf(i) == placement) n++;
        return n;
    }

    // ── 기준점 ────────────────────────────────────────────────

    /// <summary>칸의 기본 자리</summary>
    public static Vector2 DefaultAnchor(int slot)
    {
        switch (slot)
        {
            case 0:  return new Vector2(-FLOOR_X, FLOOR_Y);
            case 1:  return new Vector2( FLOOR_X, FLOOR_Y);
            case 2:  return new Vector2(-WALL_X,  WALL_Y);
            case 3:  return new Vector2( WALL_X,  WALL_Y);
            case 4:  return new Vector2(-FLOOR_FRONT_X, FLOOR_FRONT_Y);
            case 5:  return new Vector2( FLOOR_FRONT_X, FLOOR_BACK_Y);
            default: return new Vector2(0f, WALL_Y);
        }
    }

    /// <summary>칸의 지금 기준점 — 옮겼으면 저장된 위치, 아니면 기본 자리</summary>
    public static Vector2 AnchorOf(TerrariumData data, int slot)
    {
        var positions = data != null ? data.decorPositions : null;
        Vector2 p = positions != null && slot >= 0 && slot < positions.Length ? positions[slot] : Vector2.zero;
        return p == Vector2.zero ? DefaultAnchor(slot) : p;
    }

    /// <summary>
    /// 편집 모드에서 옮길 수 있는 곳으로 자른다. areaWidth = 게코 영역 폭.
    /// depthAt = 발 높이 → 원근 크기 (바닥 장식은 뒤로 갈수록 작아져 화면 끝에 더 붙을 수 있다). 없으면 1
    /// </summary>
    public static Vector2 ClampAnchor(DecorItemSO item, Vector2 p, float areaWidth, Func<float, float> depthAt = null)
    {
        if (item == null) return p;
        float half = areaWidth * 0.5f;
        Vector2 size = ImageSize(item.use);

        if (item.placement == DecorPlacement.Wall)
        {
            float limit = item.use == DecorUse.Branch
                ? Mathf.Min(BRANCH_MAX_X, half - BRANCH_LINE[0].x - EDGE_MARGIN)   // 가지는 가운데 쪽으로 뻗으므로 밑동 쪽 여백만
                : half - size.x * 0.5f - EDGE_MARGIN;
            return new Vector2(Mathf.Clamp(p.x, -limit, limit), WALL_Y);
        }

        float y     = Mathf.Clamp(p.y, FLOOR_MIN_Y, FLOOR_MAX_Y);
        float scale = depthAt != null ? depthAt(y) : 1f;
        float xMax  = Mathf.Max(0f, half - size.x * 0.5f * scale - EDGE_MARGIN);
        return new Vector2(Mathf.Clamp(p.x, -xMax, xMax), y);
    }

    /// <summary>같은 종류 장식끼리 너무 가까운가 (바닥은 거리, 벽은 가로 거리)</summary>
    public static bool TooClose(DecorPlacement placement, Vector2 a, Vector2 b)
        => placement == DecorPlacement.Wall
            ? Mathf.Abs(a.x - b.x) < MIN_GAP_WALL
            : (a - b).magnitude < MIN_GAP_FLOOR;

    // ── 그림 ─────────────────────────────────────────────────

    /// <summary>장식 그림 크기 (UI 단위, 원근 적용 전)</summary>
    public static Vector2 ImageSize(DecorUse use)
    {
        switch (use)
        {
            case DecorUse.Hide:       return new Vector2(460f, 460f);    // [TBD] 다 자란 게코보다 작아 꼬리가 삐죽 나온다
            case DecorUse.ClimbPanel: return new Vector2(380f, 1000f);
            case DecorUse.Vine:       return new Vector2(220f, 1000f);
            case DecorUse.Branch:     return BRANCH_SIZE;
            default:                  return new Vector2(300f, 300f);    // 바위·화분
        }
    }

    /// <summary>나뭇가지는 화면 가운데 쪽으로 뻗는다 — 왼쪽(가운데 포함)에 있으면 오른쪽 위로</summary>
    public static bool BranchRisesRight(Vector2 anchor) => anchor.x <= 0f;

    /// <summary>장식 그림을 놓는 법 — 위치(피벗 자리)·피벗·좌우 반전</summary>
    public static void ImagePlacement(DecorItemSO item, Vector2 anchor, out Vector2 position, out Vector2 pivot, out bool flipX)
    {
        flipX = false;
        if (item != null && item.use == DecorUse.Branch)
        {
            // 그림 왼쪽 아래(피벗)를 밑동 발 위치(BRANCH_LINE[0])가 anchor.x에 오게 놓는다. 왼쪽으로 뻗으면 좌우 반전
            bool right = BranchRisesRight(anchor);
            pivot    = Vector2.zero;
            position = new Vector2(anchor.x + (right ? -BRANCH_LINE[0].x : BRANCH_LINE[0].x), WALL_Y);
            flipX    = !right;
            return;
        }

        pivot = new Vector2(0.5f, item != null ? Mathf.Clamp01(item.baseline) : 0f);
        position = item != null && item.placement == DecorPlacement.Wall ? new Vector2(anchor.x, WALL_Y) : anchor;
    }

    // ── 게코 경로 ─────────────────────────────────────────────

    /// <summary>
    /// 벽 구조물을 타는 경로 — 첫 점은 바닥에서 올라서는 곳. maxTop = 발이 올라갈 수 있는 가장 높은 곳.
    /// rise01 = 뒤판·덩굴을 얼마나 높이 오를지 (0~1). 탈 수 없으면 null.
    /// </summary>
    public static Vector2[] ClimbPath(DecorUse use, Vector2 anchor, float maxTop, float rise01)
    {
        switch (use)
        {
            case DecorUse.ClimbPanel:
            case DecorUse.Vine:
            {
                float bottom = WALL_Y + WALL_FOOT;
                float top    = Mathf.Min(WALL_Y + ImageSize(use).y - WALL_TOP_GAP, maxTop);
                if (top < bottom + 100f) return null;
                return new[] { new Vector2(anchor.x, bottom), new Vector2(anchor.x, Mathf.Lerp(bottom + 100f, top, Mathf.Clamp01(rise01))) };
            }
            case DecorUse.Branch:
            {
                float sx = BranchRisesRight(anchor) ? 1f : -1f;
                float ox = anchor.x - sx * BRANCH_LINE[0].x;
                var path = new Vector2[BRANCH_LINE.Length];
                for (int i = 0; i < path.Length; i++)
                {
                    Vector2 p = BRANCH_LINE[i];
                    path[i] = new Vector2(ox + sx * p.x, Mathf.Min(WALL_Y + p.y + BRANCH_THICK * 0.5f, maxTop));
                }
                return path;
            }
            default:
                return null;
        }
    }

    /// <summary>은신처 문 앞 — 화면 가운데 쪽에서 들어간다</summary>
    public static Vector2 HideDoor(Vector2 anchor, float offset)
        => anchor + new Vector2(anchor.x <= 0f ? offset : -offset, 0f);
}

/// <summary>장식 목록 — Resources/Decor의 모든 DecorItemSO (한 번 읽고 재사용). 씬 목록에 없는 새 장식도 찾는다</summary>
public static class DecorCatalog
{
    private static DecorItemSO[] s_all;
    private static Dictionary<string, DecorItemSO> s_byId;   // 실행 중 조회용 (저장하지 않음)

    public static IReadOnlyList<DecorItemSO> All
    {
        get
        {
            Load();
            return s_all;
        }
    }

    public static DecorItemSO Find(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;
        Load();
        return s_byId.TryGetValue(itemId, out var item) ? item : null;
    }

    private static void Load()
    {
        if (s_all != null) return;
        s_all  = Resources.LoadAll<DecorItemSO>("Decor");
        s_byId = new Dictionary<string, DecorItemSO>();
        foreach (var item in s_all)
            if (item != null && !string.IsNullOrEmpty(item.itemId)) s_byId[item.itemId] = item;
    }
}
