using System;
using System.Collections.Generic;

[Serializable]
public class ProgressData
{
    public bool         hatchIntroSeen;   // v4: 첫 실행 부화 연출을 봤는지 (GeckoManager.NeedsHatchIntro)
    public int          totalLoginDays;
    public int          totalMoltCount;
    public List<string> unlockedSpeciesIds = new List<string>();
    public List<string> achievements       = new List<string>();
}
