using System;
using System.Collections.Generic;

[Serializable]
public class TerrariumData
{
    // 새로 시작할 때 깔려 있는 무료 배경·바닥 (Resources/Decor). 예전 저장 파일의 빈 값도 SaveManager.TryMigrate가 이것으로 채운다
    public const string DEFAULT_BACKGROUND_ID = "bg_jungle";
    public const string DEFAULT_FLOOR_ID      = "floor_soil";

    public string   backgroundId = DEFAULT_BACKGROUND_ID;
    public string   floorId      = DEFAULT_FLOOR_ID;
    public string[] decorSlots = new string[TerrariumLayout.SlotCount];  // null = 빈 슬롯
    // 칸별로 옮긴 위치 (홈 편집 모드, TerrariumLayout.AnchorOf). (0,0) = 기본 자리. 장식을 바꾸면 기본 자리로
    public UnityEngine.Vector2[] decorPositions = new UnityEngine.Vector2[TerrariumLayout.SlotCount];
    public string[] toolSlots  = new string[2];

    // 한 번 산 배경·바닥 — 다시 골라도 값을 받지 않는다 (장식은 놓을 때마다 값을 낸다)
    public List<string> ownedDecorIds = new List<string>();
}
