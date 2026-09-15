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
    public string           lastFoodItemId;                          // 먹이 선반에서 마지막으로 준 먹이 (맨 앞에 보여 준다)
    public TerrariumData    terrarium       = new TerrariumData();
    public DailyRewardData  dailyReward     = new DailyRewardData();
    public ProgressData     progress        = new ProgressData();
    public SettingsData     settings        = new SettingsData();
    public const int        CURRENT_SAVE_VERSION = 4;   // v3: 언어 설정이 기기 언어를 따름 · v4: 부화 연출 기록 (SaveManager.TryMigrate)
    public int              saveVersion     = CURRENT_SAVE_VERSION;

    // v1 마이그레이션 전용 — SaveManager.TryMigrate() 에서만 읽음. 직접 사용 금지.
    public List<string>     ownedItemIds    = new List<string>();
}
