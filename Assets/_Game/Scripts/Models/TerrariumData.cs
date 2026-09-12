using System;
using System.Collections.Generic;

[Serializable]
public class TerrariumData
{
    public string   backgroundId;
    public string   floorId;
    public string[] decorSlots = new string[4];  // null = 빈 슬롯
    public string[] toolSlots  = new string[2];

    // 한 번 산 배경·바닥 — 다시 골라도 값을 받지 않는다 (장식은 놓을 때마다 값을 낸다)
    public List<string> ownedDecorIds = new List<string>();
}
