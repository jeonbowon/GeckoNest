using System;
using System.Collections.Generic;

[Serializable]
public class PlayerData
{
    public int              coin;
    public int              gem;
    public List<GeckoData>  geckos          = new List<GeckoData>();
    public List<string>     ownedItemIds    = new List<string>();
    public string           selectedGeckoId;
    public TerrariumData    terrarium       = new TerrariumData();
    public DailyRewardData  dailyReward     = new DailyRewardData();
    public ProgressData     progress        = new ProgressData();
    public SettingsData     settings        = new SettingsData();
    public int              saveVersion     = 1;
}
