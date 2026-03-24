using System;
using System.Collections.Generic;

[Serializable]
public class PlayerData
{
    public int              coin;
    public int              gem;
    public List<GeckoData>  geckos          = new List<GeckoData>();
    public List<ItemStack>  inventory       = new List<ItemStack>(); // v2+: 아이템 ID + 수량
    public string           selectedGeckoId;
    public TerrariumData    terrarium       = new TerrariumData();
    public DailyRewardData  dailyReward     = new DailyRewardData();
    public ProgressData     progress        = new ProgressData();
    public SettingsData     settings        = new SettingsData();
    public int              saveVersion     = 2;

    // v1 마이그레이션 전용 — SaveManager.TryMigrate() 에서만 읽음. 직접 사용 금지.
    public List<string>     ownedItemIds    = new List<string>();
}
