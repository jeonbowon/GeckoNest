using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Hako > 검사 > 로직 자가 검사 — 게임 규칙이 의도대로 도는지 Unity 안에서 확인한다.
/// 플레이 모드 없이 즉시 돌고, 진짜 저장 파일 대신 임시 파일(player_data_selftest)을 쓴 뒤 지운다.
///
/// 규칙을 바꿨는데 여기서 빨간 줄이 나오면, 바꾼 쪽이 맞는지 이 표를 먼저 확인할 것.
/// </summary>
public static class HakoSelfTest
{
    private const string SAVE_STEM = "player_data_selftest";

    private static List<string> s_lines;
    private static int s_fail;

    [MenuItem("Hako/검사/로직 자가 검사", priority = 100)]
    public static void Run()
    {
        s_lines = new List<string>();
        s_fail  = 0;

        try
        {
            TestFeed();
            TestWaterAndClean();
            TestPetFatigue();
            TestMoltPacing();
            TestGrowthAndQueue();
            TestElapsedTime();
            TestTerrariumOwnership();
            TestSaveRecovery();
            TestDailyRewardDisplay();
            TestLocalization();
            TestFoodEffects();
            TestHealthAndGrowthCheck();
            TestHatchIntro();
            TestKoreanParticles();
            TestMotorActions();
        }
        catch (Exception e)
        {
            s_fail++;
            s_lines.Add("실패 (예외): " + e);
        }
        finally
        {
            new SaveManager(SAVE_STEM).DeleteFiles();
        }

        var report = new StringBuilder();
        foreach (var l in s_lines) report.AppendLine(l);
        report.AppendLine();
        report.AppendLine(s_fail == 0 ? $"모두 통과 ({s_lines.Count}개)" : $"실패 {s_fail}개 / 전체 {s_lines.Count}개");

        if (s_fail == 0) Debug.Log("[HakoSelfTest]\n" + report);
        else             Debug.LogError("[HakoSelfTest]\n" + report);

        EditorUtility.DisplayDialog("로직 자가 검사",
            (s_fail == 0 ? "모두 통과했습니다.\n\n" : $"실패 {s_fail}개 — Console을 확인해 주세요.\n\n") + report,
            "확인");
    }

    // ── 검사 ──────────────────────────────────────────────────

    private static void TestFeed()
    {
        var (repo, gecko, _, g) = Fresh();
        var cricket = Food(20f);

        g.hunger = 80f;
        Check(gecko.FeedGecko(g.id, cricket) == CareResult.Done, "배고프면 먹는다");
        Check(repo.GetItemCount("cricket_small") == 2, "먹으면 재고가 1 줄어든다");

        g.hunger = 96f;
        Check(gecko.FeedGecko(g.id, cricket) == CareResult.Refused, "배부르면 거절한다");
        Check(repo.GetItemCount("cricket_small") == 2, "거절하면 재고가 그대로다");
        Check(gecko.FeedGecko(g.id, Supplement()) == CareResult.Done, "배불러도 영양제는 먹는다");
    }

    private static void TestWaterAndClean()
    {
        var (_, gecko, _, g) = Fresh();

        g.thirst = 96f;
        Check(gecko.GiveWater(g.id) == CareResult.Refused, "목이 안 마르면 물을 거절한다");
        g.thirst = 50f;
        Check(gecko.GiveWater(g.id) == CareResult.Done && Mathf.Approximately(g.thirst, 90f), "물을 주면 40 오른다");

        g.cleanliness = 99f;
        Check(gecko.Clean(g.id) == CareResult.Refused, "이미 깨끗하면 청소가 의미 없다");
    }

    private static void TestPetFatigue()
    {
        var (_, gecko, _, g) = Fresh();
        g.mood = 50f;
        g.affection = 0f;

        int done = 0;
        for (int i = 0; i < 4; i++) if (gecko.Pet(g.id) == CareResult.Done) done++;
        Check(done == 4, "연달아 4번까지는 쓰다듬기를 좋아한다");

        float affection = g.affection, mood = g.mood;
        Check(gecko.Pet(g.id) == CareResult.Annoyed, "5번째는 귀찮아한다");
        Check(Mathf.Approximately(g.affection, affection), "귀찮을 때는 애정도가 오르지 않는다");
        Check(Mathf.Approximately(g.mood, mood - 2f), "귀찮을 때는 기분이 2 내려간다");
    }

    private static void TestMoltPacing()
    {
        var (_, gecko, queue, g) = Fresh();
        g.moltCount = 0;
        g.moltProgress = 0f;

        gecko.ApplyOfflineProgress(g.id, 30f);
        Check(Mathf.Abs(g.moltProgress - 50.1f) < 0.5f, $"첫 허물은 빠르다 — 30시간에 {g.moltProgress:F1}% (약 50%)");

        gecko.ApplyOfflineProgress(g.id, 30f);   // 100% 도달 → 성공이든 실패든 판정이 일어난다
        Check(queue.Count >= 1, "허물 판정이 일어나면 사건이 대기열에 쌓인다 (홈 화면이 없어도)");
        Check(queue.TryDequeue(out var e) &&
              (e.type == GeckoEventType.MoltSuccess || e.type == GeckoEventType.MoltFail), "쌓인 사건은 허물 성공/실패다");
        Check(g.moltProgress < 100f, "판정 뒤에는 진행도가 내려간다 (성공 0 / 실패 30)");

        g.moltCount = 1;
        g.moltProgress = 0f;
        gecko.ApplyOfflineProgress(g.id, 48f);
        Check(Mathf.Abs(g.moltProgress - 9.6f) < 0.2f, $"두 번째부터는 느리다 — 48시간에 {g.moltProgress:F1}% (9.6%)");

        g.moltProgress = 50f;
        Check(!gecko.TryMolt(g.id), "진행도가 100 미만이면 허물을 벗지 않는다");
    }

    private static void TestGrowthAndQueue()
    {
        var (_, gecko, queue, g) = Fresh();
        g.createdAtTicks = DateTime.UtcNow.AddDays(-16).Ticks;
        g.growthStage = 0;

        gecko.EvaluateGrowth(g.id);
        Check(g.growthStage == 1, "15일이 지나면 해츨링에서 베이비가 된다");
        Check(queue.TryGetPendingGrowthFrom(g.id, out int from) && from == 0, "홈 진입 때 성장 전 단계를 알 수 있다");
        Check(!queue.TryGetPendingGrowthFrom("다른게코", out _), "다른 게코의 사건과 섞이지 않는다");

        g.createdAtTicks = DateTime.UtcNow.AddDays(-31).Ticks;   // 날짜 조건은 채웠지만
        g.growthStage = 1;
        g.moltCount = 0;                                          // 허물 1회 조건은 못 채운 상태
        gecko.EvaluateGrowth(g.id);
        Check(g.growthStage == 1, "허물 조건을 못 채우면 다음 단계로 가지 않는다");
    }

    private static void TestElapsedTime()
    {
        var (_, gecko, _, g) = Fresh();
        g.hunger = 80f;
        g.lastUpdatedTicks = DateTime.UtcNow.AddHours(-2).Ticks;

        gecko.ApplyElapsedProgressAll();
        Check(Mathf.Abs(g.hunger - 72f) < 0.1f, $"2시간이 지나면 배고픔이 8 줄어든다 (80 → {g.hunger:F1})");

        float before = g.hunger;
        gecko.ApplyElapsedProgressAll();
        Check(Mathf.Abs(g.hunger - before) < 0.05f, "방금 보정했으면 다시 불러도 이중으로 줄지 않는다");

        g.lastUpdatedTicks = DateTime.UtcNow.AddHours(1).Ticks;
        gecko.ApplyElapsedProgressAll();
        Check(Mathf.Abs(g.hunger - before) < 0.05f, "기기 시계를 과거로 돌려도 진행하지 않는다");

        g.hunger = 60f;
        g.thirst = 100f;
        Check(Mathf.Abs(GeckoManager.HoursUntilCareNeeded(g, 25f) - 8.75f) < 0.1f, "다음 돌봄 시각을 계산한다 (알림 예약용)");
    }

    private static void TestTerrariumOwnership()
    {
        var (repo, _, _, _) = Fresh();
        var terrarium = new TerrariumManager(repo);

        var desert = Decor("bg_desert", DecorCategory.Background, 100);
        var jungle = Decor("bg_jungle", DecorCategory.Background, 0);
        var rock   = Decor("decor_rock", DecorCategory.Decoration, 30);

        Check(!terrarium.IsOwned(desert), "사지 않은 유료 배경은 보유가 아니다");
        Check(terrarium.IsOwned(jungle), "무료 배경은 보유다");

        terrarium.MarkOwned("bg_desert");
        terrarium.SetBackground("bg_desert");
        terrarium.SetBackground("bg_jungle");
        Check(terrarium.IsOwned(desert), "한 번 산 배경은 바꿔도 계속 보유다 (다시 결제 안 함)");
        Check(!terrarium.IsOwned(rock), "장식은 놓을 때마다 값을 낸다");

        terrarium.SetDecor(0, "decor_rock");
        Check(terrarium.GetData().decorSlots[0] == "decor_rock", "장식이 슬롯에 놓인다");
        terrarium.ClearDecor(0);
        Check(string.IsNullOrEmpty(terrarium.GetData().decorSlots[0]), "장식을 다시 빼낼 수 있다 (슬롯이 잠기지 않음)");
    }

    private static void TestSaveRecovery()
    {
        var save = new SaveManager(SAVE_STEM);
        save.DeleteFiles();

        var data = save.Load();
        Check(data.geckos.Count == 0 && data.coin > 0, "새 저장 데이터에는 코인만 있다 (게코는 EnsureStarterGecko가 준다)");
        Check(data.terrarium.backgroundId == TerrariumData.DEFAULT_BACKGROUND_ID
              && data.terrarium.floorId == TerrariumData.DEFAULT_FLOOR_ID, "새 데이터에는 기본 배경·바닥이 깔려 있다");

        var repo = new PlayerRepository(save);
        Check(repo.EnsureStarterGecko() && repo.GetPlayerData().geckos.Count == 1
              && repo.GetItemCount("cricket_small") == 3, "게코가 없으면 기본 게코와 첫 먹이 3개를 준다");
        Check(!repo.EnsureStarterGecko() && repo.GetPlayerData().geckos.Count == 1, "게코가 있으면 다시 주지 않는다");

        data.coin = 111;
        save.Save(data);
        data.coin = 222;
        save.Save(data);   // 메인 = 222, 백업 = 111

        string main = SavePath(".json"), tmp = SavePath(".tmp");

        File.Move(main, tmp);   // 저장 도중 멈춤: 메인을 지운 직후, 다 쓴 임시 파일만 남은 상태
        Check(save.Load().coin == 222, "저장 도중 멈춰 메인이 없으면 임시 파일로 복원한다");
        File.Move(tmp, main);

        File.WriteAllText(main, "{broken");
        Check(save.Load().coin == 111, "메인 파일이 깨지면 백업으로 복원한다");

        var old = new PlayerData();
        old.terrarium.backgroundId = "";
        old.terrarium.floorId      = null;
        save.Save(old);
        var migrated = save.Load();
        Check(migrated.terrarium.backgroundId == TerrariumData.DEFAULT_BACKGROUND_ID
              && migrated.terrarium.floorId == TerrariumData.DEFAULT_FLOOR_ID, "배경·바닥이 빈 예전 저장 파일은 기본값으로 채운다");

        save.DeleteFiles();
    }

    private static void TestDailyRewardDisplay()
    {
        var (repo, _, _, _) = Fresh();
        var reward = new RewardManager(repo);
        var daily  = repo.GetPlayerData().dailyReward;

        daily.streakDays       = 3;
        daily.lastClaimedTicks = DateTime.UtcNow.AddDays(-1).Ticks;
        var shown = reward.PeekReward();
        Check(reward.GetStreak() == 4, "어제 받았으면 받기 전에 '연속 4일'로 보인다");

        var got = reward.ClaimReward();
        Check(got == shown, "받기 전에 보여준 보상과 실제로 받은 보상이 같다");
        Check(reward.GetStreak() == 4 && reward.PeekReward() == got, "받은 뒤에도 같은 연속 일수·보상이 보인다");

        daily.streakDays       = 5;
        daily.lastClaimedTicks = DateTime.UtcNow.AddDays(-3).Ticks;
        Check(reward.GetStreak() == 1, "며칠 빠지면 '연속 1일'부터 다시 보인다");
    }

    private static void TestLocalization()
    {
        var saved = Loc.Current;
        try
        {
            int empty = 0, placeholders = 0;
            var bad = new StringBuilder();
            foreach (var key in Loc.Keys)
            {
                Loc.TryGetPair(key, out var ko, out var en);
                if (string.IsNullOrWhiteSpace(ko) || string.IsNullOrWhiteSpace(en)) { empty++; bad.Append(key).Append(' '); }
                if (CountPlaceholders(ko) != CountPlaceholders(en))                 { placeholders++; bad.Append(key).Append(' '); }
            }
            Check(empty == 0 && placeholders == 0, empty == 0 && placeholders == 0
                ? "번역표의 모든 문구에 한국어·영어가 있고 {0} 자리 수가 같다"
                : $"비었거나 {{0}} 자리 수가 다른 문구: {bad}");

            var conflicts = Loc.SourceConflicts();
            Check(conflicts.Count == 0, conflicts.Count == 0
                ? "씬 원문이 뜻이 다른 문구와 겹치지 않는다"
                : "겹치는 원문: " + string.Join(", ", conflicts));

            Loc.Set(GameLanguage.English);
            Check(Loc.Get("home.feed") == "Feed", "영어: 먹이 버튼은 Feed");
            Check(Loc.TryTranslateSource("먹이", out var toEnglish) && toEnglish == "Feed", "영어: 씬에 한글로 적힌 원문도 영어로 바뀐다");
            Check(Loc.Subject("하코") == "하코", "영어: 이름 뒤에 조사를 붙이지 않는다");

            // 게코 이름 글자는 번역표 원문("하코")과 같아도 바뀌지 않아야 한다
            var root      = new GameObject("LocTestRoot");
            var nameText  = new GameObject("Name").AddComponent<TextMeshProUGUI>();
            var labelText = new GameObject("Label").AddComponent<TextMeshProUGUI>();
            try
            {
                nameText.transform.SetParent(root.transform, false);
                labelText.transform.SetParent(root.transform, false);
                SceneTextLocalizer.Ignore(nameText);
                nameText.text  = "하코";
                labelText.text = "먹이";
                SceneTextLocalizer.LocalizeUnder(root);
                Check(nameText.text == "하코" && labelText.text == "Feed", "영어: 게코 이름 글자는 번역하지 않고, 고정 글자만 번역한다");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            Loc.Set(GameLanguage.Korean);
            Check(Loc.TryTranslateSource("Feed", out var toKorean) && toKorean == "먹이", "한국어: 씬에 영어로 적힌 원문도 한글로 바뀐다");
            Check(Loc.Format("event.growth", Loc.Subject("하코"), "A", "B").StartsWith("하코가 "), "한국어: 결과 알림 이름 뒤에 조사가 붙는다");
            Check(!Loc.TryTranslateSource("1,250", out _) && !Loc.TryTranslateSource("별님", out _), "표에 없는 글자(숫자·이름)는 건드리지 않는다");
            Check(Loc.Resolve("ko") == GameLanguage.Korean && Loc.Resolve("en") == GameLanguage.English, "설정값 ko·en이 기기 언어보다 우선한다");

            // 예전 저장 파일(v2)의 "ko"는 고른 값이 아니라 기본값이었다 → 기기 언어를 따르게 비운다
            var save = new SaveManager(SAVE_STEM);
            save.DeleteFiles();
            var old = new PlayerData { saveVersion = 2 };
            old.settings.language = "ko";
            save.Save(old);
            var loaded = save.Load();
            Check(loaded.settings.language == SettingsData.LANGUAGE_AUTO && loaded.saveVersion == PlayerData.CURRENT_SAVE_VERSION,
                  "예전 저장 파일의 언어값은 기기 언어를 따르게 바뀐다");
            save.DeleteFiles();

            // 글꼴은 정적 아틀라스 — 번역표의 글자가 하나라도 없으면 화면에 □로 나온다
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Game/Fonts/NanumGothic-Regular SDF.asset");
            if (font == null)
            {
                Check(false, "글꼴 에셋을 찾지 못함 — Assets/_Game/Fonts/NanumGothic-Regular SDF.asset");
            }
            else
            {
                var absent = new HashSet<char>();
                foreach (var key in Loc.Keys)
                {
                    Loc.TryGetPair(key, out var ko, out var en);
                    foreach (char c in ko + en)
                        if (c != '|' && !char.IsWhiteSpace(c) && !font.HasCharacter(c)) absent.Add(c);
                }
                Check(absent.Count == 0, absent.Count == 0
                    ? "번역표의 모든 글자가 글꼴에 있다 (□ 없음)"
                    : "글꼴에 없는 글자: " + string.Join(" ", absent));
            }
        }
        finally
        {
            Loc.Set(saved);
        }
    }

    private static int CountPlaceholders(string s)
    {
        if (s == null) return 0;
        int n = 0;
        for (int i = 0; i + 2 < s.Length; i++)
            if (s[i] == '{' && char.IsDigit(s[i + 1]) && s[i + 2] == '}') n++;
        return n;
    }

    private static void TestFoodEffects()
    {
        var (repo, gecko, _, g) = Fresh();

        // 성장 가속 — 성장치 1 = 3시간, 필요한 실제 날짜의 최대 30%까지
        g.createdAtTicks = DateTime.UtcNow.AddDays(-13).Ticks;
        g.growthStage    = 0;
        g.growthExp      = 16f;   // 48시간 = 2일
        gecko.EvaluateGrowth(g.id);
        Check(g.growthStage == 1, "성장치 1 = 3시간 — 실제 13일 + 성장치 16(2일)이면 15일 조건을 채워 자란다");

        g.createdAtTicks = DateTime.UtcNow.AddDays(-5).Ticks;
        g.growthStage    = 0;
        g.growthExp      = 1000f;
        gecko.EvaluateGrowth(g.id);
        Check(g.growthStage == 0, "먹이로 앞당기는 건 최대 30% — 실제 5일이면 성장치를 아무리 쌓아도 15일이 되지 않는다");
        Check(Mathf.Abs(GeckoManager.EffectiveAgeDays(10.5f, 1000f) - 15f) < 0.01f, "실제 10.5일(15일의 70%)이 최대로 앞당긴 한계다");

        // 좋아하는 먹이 — 크레스티드 + 밀웜
        var mealworm = FoodItem("mealworm", hunger: 22f, mood: 3f, exp: 3f, prefer: "crested");
        g.speciesId = "crested";
        g.hunger    = 50f;
        g.mood      = 50f;
        g.affection = 0f;
        repo.AddItem("mealworm", 2);
        Check(gecko.FeedGecko(g.id, mealworm, out var favorite) == CareResult.Done && favorite.favorite, "크레스티드에게 밀웜은 좋아하는 먹이다");
        Check(Mathf.Approximately(g.affection, 4f) && Mathf.Approximately(g.mood, 56f), "좋아하는 먹이: 애정도 2배(+4), 기분은 먹이 +3에 +3 더 (50 → 56)");
        Check(repo.GetPlayerData().lastFoodItemId == "mealworm", "마지막으로 준 먹이를 기억한다 (선반 맨 앞)");

        g.speciesId = "leopard";
        g.hunger    = 50f;
        float affectionBefore = g.affection;
        Check(gecko.FeedGecko(g.id, mealworm, out var plain) == CareResult.Done && !plain.favorite
              && Mathf.Approximately(g.affection, affectionBefore + 2f), "다른 종에게 밀웜은 보통 먹이 (애정도 +2)");

        // 허물 보너스 — 칼슘 +10%, 여러 번 먹어도 최대 +15%, 판정 뒤 사라짐
        var calcium = FoodItem("calcium_dusting", hunger: 5f, health: 10f, molt: 0.10f);
        g.moltBonus = 0f;
        g.hunger    = 50f;
        repo.AddItem("calcium_dusting", 3);
        gecko.FeedGecko(g.id, calcium, out var calciumEffect);
        Check(Mathf.Approximately(g.moltBonus, 0.10f) && Mathf.Approximately(calciumEffect.moltBonus, 0.10f), "칼슘 영양제는 다음 허물 성공률 +10%를 쌓는다");
        gecko.FeedGecko(g.id, calcium, out _);
        gecko.FeedGecko(g.id, calcium, out _);
        Check(Mathf.Approximately(g.moltBonus, 0.15f), "여러 번 먹어도 허물 보너스는 최대 +15%");
        g.moltProgress = 100f;
        gecko.TryMolt(g.id);
        Check(g.moltBonus == 0f, "허물 판정이 끝나면 먹이 보너스는 사라진다 (1회용)");

        // 성장촉진제 — 배를 채우지 않으므로 배불러도 먹는다
        var booster = FoodItem("growth_booster", exp: 8f);
        g.hunger = 99f;
        repo.AddItem("growth_booster", 1);
        float expBefore = g.growthExp;
        Check(gecko.FeedGecko(g.id, booster, out var boost) == CareResult.Done
              && Mathf.Approximately(g.growthExp, expBefore + 8f) && Mathf.Approximately(boost.growthExp, 8f), "성장촉진제는 배불러도 먹고 성장치 +8");

        // 실제 먹이 에셋이 제안 표와 같은지
        var dubia     = Resources.Load<ItemSO>("Items/dubia_roach");
        var superworm = Resources.Load<ItemSO>("Items/superworm");
        var realMeal  = Resources.Load<ItemSO>("Items/mealworm");
        var realCal   = Resources.Load<ItemSO>("Items/calcium_dusting");
        var realBoost = Resources.Load<ItemSO>("Items/growth_booster");
        Check(dubia != null && dubia.kind == FoodKind.Big && Mathf.Approximately(dubia.moltBonus, 0.05f) && Mathf.Approximately(dubia.moodBonus, 4f),
              "에셋: 두비아 = 큰 먹이 · 기분 +4 · 허물 +5%");
        Check(superworm != null && superworm.kind == FoodKind.Big && Mathf.Approximately(superworm.moodBonus, 6f),
              "에셋: 슈퍼밀웜 = 큰 먹이 · 기분 +6");
        Check(realMeal != null && Array.IndexOf(realMeal.preferredSpeciesIds ?? new string[0], "crested") >= 0,
              "에셋: 밀웜은 크레스티드가 좋아하는 먹이");
        Check(realCal != null && realCal.kind == FoodKind.Supplement && Mathf.Approximately(realCal.moltBonus, 0.1f)
              && realBoost != null && realBoost.kind == FoodKind.Supplement,
              "에셋: 칼슘·성장촉진제 = 영양제 반응, 칼슘 허물 +10%");
    }

    private static void TestHealthAndGrowthCheck()
    {
        var (_, gecko, _, g) = Fresh();

        // 건강 회복 — 배고픔·목마름이 둘 다 50을 넘는 동안 1시간에 +0.5
        g.health = 10f; g.hunger = 100f; g.thirst = 100f;
        g.lastUpdatedTicks = DateTime.UtcNow.AddHours(-8).Ticks;
        gecko.ApplyElapsedProgressAll();
        Check(Mathf.Abs(g.health - 14f) < 0.1f, $"배고픔·목마름이 넉넉하면 건강이 1시간에 0.5씩 회복된다 (8시간: 10 → {g.health:F1})");

        g.health = 10f; g.hunger = 40f; g.thirst = 100f;
        g.lastUpdatedTicks = DateTime.UtcNow.AddHours(-8).Ticks;
        gecko.ApplyElapsedProgressAll();
        Check(Mathf.Abs(g.health - 10f) < 0.1f, $"배고픔이 50 이하면 건강이 회복되지 않는다 (10 → {g.health:F1})");

        g.health = 10f; g.hunger = 70f; g.thirst = 100f;   // 배고픔이 5시간 뒤 50
        g.lastUpdatedTicks = DateTime.UtcNow.AddHours(-8).Ticks;
        gecko.ApplyElapsedProgressAll();
        Check(Mathf.Abs(g.health - 12.5f) < 0.1f, $"넉넉한 시간만큼만 회복된다 (5시간 → +2.5, 10 → {g.health:F1})");

        // 다음 성장 조건 — 판정과 화면 표시가 같은 계산
        g.growthStage    = 2;
        g.createdAtTicks = DateTime.UtcNow.AddDays(-254).Ticks;
        g.growthExp      = 0f;
        g.moltCount      = 6;
        g.health         = 10f;
        g.affection      = 100f;
        var check = gecko.GetGrowthCheck(g.id);
        Check(check.nextStage == 3 && check.DaysMet && check.MoltsMet && !check.HealthMet && !check.AllMet,
              "주버나일: 나이·허물을 채워도 건강 50 미만이면 조건 미충족");

        var savedLanguage = Loc.Current;
        try
        {
            Loc.Set(GameLanguage.Korean);
            string text = HomeUIController.DescribeGrowth(check);
            Check(text.Contains("서브어덜트") && text.Contains("나이 60일 - 충족") && text.Contains("건강 50 - 부족 (지금 10)"),
                  "성장 조건 말풍선에 다음 단계와 부족한 조건·지금 값이 나온다");
        }
        finally
        {
            Loc.Set(savedLanguage);
        }

        gecko.EvaluateGrowth(g.id);
        Check(g.growthStage == 2, "건강이 부족하면 서브어덜트로 자라지 않는다");
        g.health = 50f;
        gecko.EvaluateGrowth(g.id);
        Check(g.growthStage == 3, "건강 50이 되면 서브어덜트로 자란다");
        Check(gecko.GetGrowthCheck(g.id).needAffection > 0f, "서브어덜트의 다음 조건에는 애정도가 들어간다");

        g.growthStage = 4;
        Check(gecko.GetGrowthCheck(g.id).IsAdult, "어덜트는 다음 성장 조건이 없다");
    }

    private static void TestHatchIntro()
    {
        var (_, gecko, _, _) = Fresh();
        Check(gecko.NeedsHatchIntro(), "새 게임은 첫 홈에서 부화 연출을 보여준다");

        gecko.CompleteHatchIntro();
        Check(!gecko.NeedsHatchIntro(), "부화 연출이 끝나면 다시 보여주지 않는다");

        var save = new SaveManager(SAVE_STEM);
        Check(save.Load().progress.hatchIntroSeen, "부화 연출을 봤다는 기록이 바로 저장된다 (다음 실행에도 안 나옴)");

        var old = new PlayerData { saveVersion = 3 };
        old.geckos.Add(GeckoData.CreateNew("하코", "crested"));
        save.Save(old);
        var migrated = save.Load();
        Check(migrated.progress.hatchIntroSeen && migrated.saveVersion == PlayerData.CURRENT_SAVE_VERSION,
              "게코와 함께 플레이하던 예전 저장(v3)은 부화 연출을 건너뛴다");
        save.DeleteFiles();
    }

    private static ItemSO FoodItem(string id, float hunger = 0f, float mood = 0f, float health = 0f, float exp = 0f, float molt = 0f, string prefer = null)
    {
        var item = ScriptableObject.CreateInstance<ItemSO>();
        item.itemId              = id;
        item.hungerRestore       = hunger;
        item.moodBonus           = mood;
        item.healthRestore       = health;
        item.growthExpGain       = exp;
        item.moltBonus           = molt;
        item.preferredSpeciesIds = prefer != null ? new[] { prefer } : new string[0];
        return item;
    }

    private static void TestKoreanParticles()
    {
        Check(KoreanText.WithSubject("하코") == "하코가", "받침 없는 이름에는 '가'");
        Check(KoreanText.WithSubject("별님") == "별님이", "받침 있는 이름에는 '이'");
        Check(KoreanText.WithSubject("Leo") == "Leo(이)가", "한글이 아닌 이름은 (이)가");
        Check(KoreanText.WithSubject("") == "게코가", "이름이 비어도 문장이 깨지지 않는다");
    }

    private static void TestMotorActions()
    {
        int missing = 0;
        var names = new StringBuilder();
        foreach (GeckoAction a in Enum.GetValues(typeof(GeckoAction)))
        {
            if (a == GeckoAction.None) continue;
            if (GeckoMotor.DurationOf(a) > 0f) continue;
            missing++;
            names.Append(a).Append(' ');
        }
        Check(missing == 0, missing == 0
            ? "모든 동작에 길이가 정해져 있다"
            : $"길이가 없는 동작: {names}(GeckoMotor.DurationOf에 추가할 것)");

        Check(GeckoParts.Count == Enum.GetValues(typeof(GeckoPartId)).Length, "파츠 개수와 enum이 일치한다");
        Check(GeckoParts.LayerName(GeckoPartId.Head) == "head", "파츠 레이어 이름표가 정상이다");
    }

    // ── 도구 ──────────────────────────────────────────────────

    private static (PlayerRepository repo, GeckoManager gecko, GeckoEventQueue queue, GeckoData g) Fresh()
    {
        var save = new SaveManager(SAVE_STEM);
        save.DeleteFiles();                       // 앞 검사의 흔적 제거
        var repo  = new PlayerRepository(save);
        repo.EnsureStarterGecko();                // 실제 게임과 같은 경로로 기본 게코·첫 먹이
        var gecko = new GeckoManager(repo, new TimeManager());
        var queue = new GeckoEventQueue(gecko);
        return (repo, gecko, queue, repo.GetPlayerData().geckos[0]);
    }

    private static string SavePath(string extension)
        => Path.Combine(Application.persistentDataPath, SAVE_STEM + extension);

    private static ItemSO Food(float hunger)
    {
        var item = ScriptableObject.CreateInstance<ItemSO>();
        item.itemId = "cricket_small";
        item.hungerRestore = hunger;
        return item;
    }

    private static ItemSO Supplement()
    {
        var item = ScriptableObject.CreateInstance<ItemSO>();
        item.itemId = "cricket_small";
        item.healthRestore = 10f;
        return item;
    }

    private static DecorItemSO Decor(string id, DecorCategory category, int coin)
    {
        var item = ScriptableObject.CreateInstance<DecorItemSO>();
        item.itemId = id;
        item.category = category;
        item.coinPrice = coin;
        return item;
    }

    private static void Check(bool ok, string what)
    {
        s_lines.Add((ok ? "통과  " : "실패  ") + what);
        if (!ok) s_fail++;
    }
}
