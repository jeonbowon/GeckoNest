using System;

[Serializable]
public class TerrariumData
{
    public string   backgroundId;
    public string   floorId;
    public string[] decorSlots = new string[4];  // null = ºó ½½·Ô
    public string[] toolSlots  = new string[2];
}
