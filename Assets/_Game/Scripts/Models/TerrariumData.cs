using System;
using System.Collections.Generic;

[Serializable]
public class TerrariumData
{
    // 새로 시작할 때 깔려 있는 무료 테마 (Resources/Decor). 예전 저장 파일의 빈 값도 SaveManager.TryMigrate가 이것으로 채운다
    public const string DEFAULT_BACKGROUND_ID = "bg_jungle";
    public const string DEFAULT_FLOOR_ID      = "floor_soil";

    // 테마 = 뒷벽과 바닥이 한 장에 그려진 배경 (2026-09-21 — 배경·바닥을 합쳤다).
    // 아이디는 예전 배경 그대로(bg_*)라 저장을 옮길 필요가 없다
    public string   backgroundId = DEFAULT_BACKGROUND_ID;

    // 쓰지 않는다 (2026-09-21) — 바닥은 테마 그림에 들어갔다. 예전 저장을 그대로 읽으려고 필드만 남긴다
    public string   floorId      = DEFAULT_FLOOR_ID;

    public string[] decorSlots = new string[TerrariumLayout.SlotCount];  // null = 빈 슬롯
    // 칸별로 옮긴 위치 (홈 편집 모드, TerrariumLayout.AnchorOf). (0,0) = 기본 자리. 장식을 바꾸면 기본 자리로
    public UnityEngine.Vector2[] decorPositions = new UnityEngine.Vector2[TerrariumLayout.SlotCount];
    public string[] toolSlots  = new string[2];

    // 한 번 산 테마 — 다시 골라도 값을 받지 않는다 (장식은 놓을 때마다 값을 낸다)
    public List<string> ownedDecorIds = new List<string>();

    // 없어진 유료 바닥과 그 값 [TBD 가격은 에셋과 같게] — 저장 v10에서 산 사람에게 코인을 돌려준다
    public static readonly (string id, int coin)[] RETIRED_FLOORS = { ("floor_bark", 80) };

    /// <summary>
    /// 없어진 유료 바닥을 산 기록을 지우고 돌려줄 코인을 센다 (산 기록 또는 지금 깔려 있으면 산 것).
    /// 코인을 더하는 것은 부르는 쪽 (SaveManager v10)
    /// </summary>
    public static int RefundRetiredFloors(TerrariumData t)
    {
        if (t == null) return 0;
        int refund = 0;
        foreach (var (id, coin) in RETIRED_FLOORS)
        {
            bool owned = (t.ownedDecorIds != null && t.ownedDecorIds.Remove(id)) | t.floorId == id;
            if (owned) refund += coin;
        }
        t.floorId = DEFAULT_FLOOR_ID;
        return refund;
    }
}
