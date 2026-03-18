using System;
using System.Collections.Generic;

[Serializable]
public class ProgressData
{
    public int          totalLoginDays;
    public int          totalMoltCount;
    public List<string> unlockedSpeciesIds = new List<string>();
    public List<string> achievements       = new List<string>();
}
