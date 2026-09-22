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
        string report = RunAll();
        EditorUtility.DisplayDialog("로직 자가 검사",
            (s_fail == 0 ? "모두 통과했습니다.\n\n" : $"실패 {s_fail}개 — Console을 확인해 주세요.\n\n") + report,
            "확인");
    }

    /// <summary>
    /// 창 없이(배치 모드) 실행 — 결과는 로그에 쓰고, 실패가 있으면 종료 코드 1. Unity가 꺼져 있어야 한다.
    /// Unity.exe -batchmode -nographics -projectPath (프로젝트) -executeMethod HakoSelfTest.RunBatch -logFile (로그 파일)
    /// </summary>
    public static void RunBatch()
    {
        RunAll();
        EditorApplication.Exit(s_fail == 0 ? 0 : 1);
    }

    private static string RunAll()
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
            TestAdultStage();
            TestMultipleGeckos();
            TestAdultGiftAndUnlocks();
            TestBookAndAchievements();
            TestBond();
            TestMorph();
            TestAtmosphere();
            TestDailyGoals();
            TestTouchAndMovement();
            TestTerrariumStructures();
            TestDecorPerks();
            TestKoreanParticles();
            TestMotorActions();
            TestHeadMotion();
            TestHideDoor();
            TestWholeBodySkin();
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
        return report.ToString();
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

        gecko.ApplyOfflineProgress(g.id, 6f);
        Check(Mathf.Abs(g.moltProgress - 50f) < 0.5f, $"첫 허물은 빠르다 — 6시간에 {g.moltProgress:F1}% (12시간에 첫 허물)");

        gecko.ApplyOfflineProgress(g.id, 6f);    // 100% 도달 → 성공이든 실패든 판정이 일어난다
        Check(queue.Count >= 1, "허물 판정이 일어나면 사건이 대기열에 쌓인다 (홈 화면이 없어도)");
        Check(queue.TryDequeue(out var e) &&
              (e.type == GeckoEventType.MoltSuccess || e.type == GeckoEventType.MoltFail), "쌓인 사건은 허물 성공/실패다");
        Check(g.moltProgress < 100f, "판정 뒤에는 진행도가 내려간다 (성공 0 / 실패 30)");

        g.moltCount = 1;
        g.moltProgress = 0f;
        gecko.ApplyOfflineProgress(g.id, 48f);
        Check(Mathf.Abs(g.moltProgress - 66.7f) < 0.3f, $"두 번째부터는 3일 주기 — 48시간에 {g.moltProgress:F1}% (약 66.7%)");

        g.moltProgress = 50f;
        Check(!gecko.TryMolt(g.id), "진행도가 100 미만이면 허물을 벗지 않는다");
    }

    private static void TestGrowthAndQueue()
    {
        var (_, gecko, queue, g) = Fresh();
        g.createdAtTicks = DateTime.UtcNow.AddDays(-1.2).Ticks;
        g.growthStage = 0;

        gecko.EvaluateGrowth(g.id);
        Check(g.growthStage == 1, "1일이 지나면 해츨링에서 베이비가 된다");
        Check(queue.TryGetPendingGrowthFrom(g.id, out int from) && from == 0, "홈 진입 때 성장 전 단계를 알 수 있다");
        Check(!queue.TryGetPendingGrowthFrom("다른게코", out _), "다른 게코의 사건과 섞이지 않는다");

        g.createdAtTicks = DateTime.UtcNow.AddDays(-3.5).Ticks;  // 날짜 조건(3일)은 채웠지만
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

        // 배경·바닥 → 테마 (2026-09-21) — 바닥은 더 팔지도, 가진 것으로 치지도 않는다
        var bark = Decor("floor_bark", DecorCategory.Floor, 80);
        terrarium.MarkOwned("floor_bark");
        Check(!terrarium.IsOwned(bark), "테마: 바닥은 샀던 기록이 있어도 보유로 치지 않는다 (테마에 합쳐져 팔지 않음)");

        // 없어진 유료 바닥 돌려주기 — 산 기록 또는 지금 깔려 있으면 산 것
        var bought = new TerrariumData();
        bought.ownedDecorIds.Add("floor_bark");
        bought.ownedDecorIds.Add("bg_desert");
        Check(TerrariumData.RefundRetiredFloors(bought) == 80
              && !bought.ownedDecorIds.Contains("floor_bark") && bought.ownedDecorIds.Contains("bg_desert"),
              "테마: 산 나무판 바닥은 80코인을 돌려주고 기록을 지운다 (산 테마는 그대로)");
        var applied = new TerrariumData { floorId = "floor_bark" };
        Check(TerrariumData.RefundRetiredFloors(applied) == 80 && applied.floorId == TerrariumData.DEFAULT_FLOOR_ID,
              "테마: 깔려 있던 유료 바닥도 산 것으로 보고 돌려준다");
        Check(TerrariumData.RefundRetiredFloors(new TerrariumData()) == 0, "테마: 무료 흙 바닥만 쓰던 저장은 돌려줄 것이 없다");

        // 정글 테마 그림 = 잎사귀 벽 + 흙 바닥을 합친 임시 그림 (ThemeProxyArt)
        var jungleAsset = Resources.Load<DecorItemSO>("Decor/bg_jungle");
        string jungleSprite = jungleAsset != null && jungleAsset.previewSprite != null ? jungleAsset.previewSprite.name : "없음";
        Check(jungleSprite == "theme_jungle",
              jungleSprite == "theme_jungle"
                  ? "테마: 정글 테마는 벽과 바닥이 합쳐진 그림(theme_jungle)을 쓴다"
                  : $"테마: 정글 테마 그림이 theme_jungle이 아님 (지금 {jungleSprite}) — ThemeProxyArt.Generate 실행");
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

        // v9 → v10: 배경·바닥을 테마로 합쳤다 — 산 바닥 값은 돌려준다, 한 번만
        var v9 = new PlayerData { coin = 500, saveVersion = 9 };
        v9.terrarium.ownedDecorIds.Add("floor_bark");
        v9.terrarium.floorId = "floor_bark";
        save.Save(v9);
        var v10 = save.Load();
        Check(v10.saveVersion == PlayerData.CURRENT_SAVE_VERSION && v10.coin == 580
              && !v10.terrarium.ownedDecorIds.Contains("floor_bark"),
              $"저장 v10: 산 나무판 바닥을 한 번만 돌려준다 (500 → {v10.coin})");
        save.Save(v10);
        Check(save.Load().coin == 580, "저장 v10: 다시 읽어도 또 돌려주지 않는다");

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
        g.createdAtTicks = DateTime.UtcNow.AddHours(-19).Ticks;
        g.growthStage    = 0;
        g.growthExp      = 2f;    // 6시간
        gecko.EvaluateGrowth(g.id);
        Check(g.growthStage == 1, "성장치 1 = 3시간 — 실제 19시간 + 성장치 2(6시간)이면 1일 조건을 채워 자란다");

        g.createdAtTicks = DateTime.UtcNow.AddHours(-12).Ticks;
        g.growthStage    = 0;
        g.growthExp      = 1000f;
        gecko.EvaluateGrowth(g.id);
        Check(g.growthStage == 0, "먹이로 앞당기는 건 최대 30% — 실제 12시간이면 성장치를 아무리 쌓아도 1일이 되지 않는다");
        Check(Mathf.Abs(GeckoManager.EffectiveAgeDays(0.7f, 1000f) - 1f) < 0.01f, "실제 0.7일(1일의 70%)이 최대로 앞당긴 한계다");

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


        // 하단 탭 아이콘 5종 (2026-09-21) — 이모지가 □로 나와서 그림으로 넣는다
        var missingIcon = new StringBuilder();
        foreach (var iconName in HomeUIController.NAV_ICONS)
            if (Resources.Load<Sprite>($"Icons/{iconName}") == null) missingIcon.Append(iconName).Append(' ');
        Check(missingIcon.Length == 0,
              missingIcon.Length == 0
                  ? "에셋: 하단 탭 아이콘 5종이 Resources/Icons에 있다"
                  : "하단 탭 아이콘 없음 (NavIconArt.Generate 실행): " + missingIcon);
        Check(HomeUIController.NAV_ICONS.Length == NavIconArt.NAMES.Length
              && System.Array.IndexOf(HomeUIController.NAV_ICONS, NavIconArt.NAMES[0]) == 0
              && System.Array.IndexOf(HomeUIController.NAV_ICONS, NavIconArt.NAMES[4]) == 4,
              "에셋: 탭 아이콘 이름·순서가 그림 생성기와 같다 (상점 → 게코 → 꾸미기 → 보상 → 설정)");
        // 먹이 버튼 표시 = 선반 목록 (2026-09-20)
        // 예전에는 버튼만 배를 채우는 먹이를 세어, 영양제만 있으면 "먹이 없음"인데 선반은 열렸다
        var feedData = new PlayerData();
        feedData.inventory.Add(new ItemStack("growth_booster", 2));
        var list = HomeUIController.OwnedFoods(feedData, g);
        Check(list.Count == 1 && list[0].item != null && list[0].item.itemId == "growth_booster" && list[0].count == 2,
              "먹이 버튼: 배를 채우지 않는 영양제만 있어도 목록에 나온다");

        feedData.inventory.Add(new ItemStack("mealworm", 3));
        feedData.lastFoodItemId = "mealworm";
        list = HomeUIController.OwnedFoods(feedData, g);
        Check(list.Count == 2 && list[0].item.itemId == "mealworm",
              "먹이 버튼: 마지막으로 준 먹이가 맨 앞 (버튼 글자도 이 먹이)");

        var grown = GeckoData.CreateNew("다 큰", "crested");
        grown.growthStage = GeckoManager.ADULT_STAGE;
        var grownList = HomeUIController.OwnedFoods(feedData, grown);
        bool boosterUseless = grownList.Find(o => o.item.itemId == "growth_booster").useless;
        Check(grownList.Count == 2 && boosterUseless && grownList.TrueForAll(o => o.grown),
              "먹이 버튼: 다 자란 게코에게 성장촉진제는 \"필요 없음\"으로 나온다");

        Check(HomeUIController.OwnedFoods(null, g).Count == 0, "먹이 버튼: 데이터가 없으면 빈 목록 (오류 없이)");
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

        // 굶주림 — 배고픔·목마름이 0이 된 뒤부터만 건강이 준다 (2026-09-20)
        // 배고픔 80(20시간 뒤 0) · 목마름 80(16시간 뒤 0) → 24시간 중 마지막 8시간만 -1/h
        // (앞 6시간은 둘 다 50을 넘어 +0.5/h 회복: 50 +3 -8 = 45)
        g.health = 50f; g.hunger = 80f; g.thirst = 80f; g.cleanliness = 100f;
        g.lastUpdatedTicks = DateTime.UtcNow.AddHours(-24).Ticks;
        gecko.ApplyElapsedProgressAll();
        Check(Mathf.Abs(g.health - 45f) < 0.1f,
              $"굶주림: 0이 된 뒤 시간만큼만 건강이 준다 (24시간 중 8시간 → -8, 50 → {g.health:F1})");

        // 처음부터 0이면 경과 시간 전체가 줄어든다
        g.health = 50f; g.hunger = 0f; g.thirst = 0f; g.cleanliness = 100f;
        g.lastUpdatedTicks = DateTime.UtcNow.AddHours(-10).Ticks;
        gecko.ApplyElapsedProgressAll();
        Check(Mathf.Abs(g.health - 40f) < 0.1f, $"굶주림: 이미 0이면 경과 시간 전체가 줄어든다 (10시간 → -10, 50 → {g.health:F1})");

        // 더러움 — 청결 20 이하가 된 뒤부터만 기분에 추가 패널티
        // 청결 40(약 29.85시간 뒤 20) · 기분 100 → 40시간 중 약 10.15시간만 -0.5/h, 기본 감소는 -1/h
        g.health = 100f; g.hunger = 100f; g.thirst = 100f; g.cleanliness = 40f; g.mood = 100f;
        g.lastUpdatedTicks = DateTime.UtcNow.AddHours(-40).Ticks;
        gecko.ApplyElapsedProgressAll();
        float dirtyHours = 40f - (40f - GeckoManager.CLEAN_MOOD_THRESHOLD) / 0.67f;
        Check(Mathf.Abs(g.mood - (100f - 40f - 0.5f * dirtyHours)) < 0.2f,
              $"더러움: 청결 20 아래가 된 뒤 시간만큼만 기분이 더 준다 (기분 {g.mood:F1})");

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
            Check(text.Contains("서브어덜트") && text.Contains("나이 7일 - 충족") && text.Contains("건강 50 - 부족 (지금 10)"),
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

    private static void TestAdultStage()
    {
        var (repo, gecko, _, g) = Fresh();
        var data = repo.GetPlayerData();

        // 서브어덜트 → 어덜트: 14일 + 허물 3회 + 애정도 60
        g.growthStage    = 3;
        g.createdAtTicks = DateTime.UtcNow.AddDays(-15).Ticks;
        g.moltCount      = 3;
        g.affection      = 60f;
        int coin0 = data.coin, gem0 = data.gem;
        gecko.EvaluateGrowth(g.id);
        Check(g.growthStage == 4 && data.coin == coin0 + GeckoManager.ADULT_REWARD_COIN + gecko.LastMorph.coin
              && data.gem == gem0 + GeckoManager.ADULT_REWARD_GEM + gecko.LastMorph.gem && data.progress.adultCount == 1,
              $"어덜트가 되면 코인 +{GeckoManager.ADULT_REWARD_COIN} · 젬 +{GeckoManager.ADULT_REWARD_GEM}을 받고 기록된다 (새 모프 보상은 따로)");

        gecko.EvaluateGrowth(g.id);
        Check(data.coin == coin0 + GeckoManager.ADULT_REWARD_COIN + gecko.LastMorph.coin && data.progress.adultCount == 1, "어덜트 보상은 한 번만 받는다");
        Check(gecko.GetGrowthCheck(g.id).IsAdult, "어덜트는 다음 성장 조건이 없다 (\"다 자랐어요!\")");

        // 다 자라면 성장만 주는 먹이는 쓸모없다
        var booster = FoodItem("growth_booster", exp: 8f);
        repo.AddItem("growth_booster", 1);
        Check(GeckoManager.IsUselessFood(g, booster) && gecko.FeedGecko(g.id, booster) == CareResult.Refused
              && repo.GetItemCount("growth_booster") == 1, "어덜트는 성장촉진제를 거절하고 재고가 그대로다");

        var calcium = FoodItem("calcium_dusting", hunger: 5f, health: 10f, molt: 0.10f);
        repo.AddItem("calcium_dusting", 1);
        g.hunger = 50f;
        Check(!GeckoManager.IsUselessFood(g, calcium) && gecko.FeedGecko(g.id, calcium) == CareResult.Done,
              "어덜트도 다른 효과가 있는 먹이(칼슘)는 먹는다");

        var mealworm = FoodItem("mealworm", hunger: 22f, mood: 3f, exp: 3f);
        repo.AddItem("mealworm", 1);
        g.hunger = 50f;
        float exp0 = g.growthExp;
        Check(gecko.FeedGecko(g.id, mealworm, out var effect) == CareResult.Done
              && Mathf.Approximately(g.growthExp, exp0) && effect.growthExp == 0f,
              "어덜트는 성장치가 쌓이지 않고 먹은 뒤 말풍선에도 성장이 나오지 않는다");

        g.growthStage = 2;
        Check(!GeckoManager.IsUselessFood(g, booster), "자라는 중인 게코에게 성장촉진제는 쓸모 있다");
    }

    private static void TestAdultGiftAndUnlocks()
    {
        var (repo, gecko, _, g) = Fresh();
        var reward = new RewardManager(repo);
        var data   = repo.GetPlayerData();
        int today  = RewardManager.TodayNumber();

        // 선물 조건 — 어덜트 + 상태 모두 50 초과 + 오늘 아직
        g.hunger = g.thirst = g.cleanliness = g.mood = g.health = 80f;
        Check(!RewardManager.CanGift(g), "어덜트가 아니면 선물이 없다");
        g.growthStage = GeckoManager.ADULT_STAGE;
        Check(RewardManager.CanGift(g), "잘 지내는 어덜트는 선물을 남긴다");
        g.mood = 50f;
        Check(!RewardManager.CanGift(g), "상태 하나라도 50 이하면 선물이 없다");
        g.mood = 80f;

        int coin0 = data.coin;
        Check(reward.ClaimGift(g.id, new System.Random(1), out var gift)
              && gift.coin >= RewardManager.GIFT_COIN_MIN && gift.coin <= RewardManager.GIFT_COIN_MAX
              && data.coin == coin0 + gift.coin && g.giftDay == today,
              $"선물을 받으면 코인 {RewardManager.GIFT_COIN_MIN}~{RewardManager.GIFT_COIN_MAX}이 들어오고 오늘 날짜가 기록된다");
        coin0 = data.coin;
        Check(!RewardManager.CanGift(g) && !reward.ClaimGift(g.id, null, out _) && data.coin == coin0,
              "선물은 게코마다 하루 한 번");
        g.giftDay = today - 1;
        Check(RewardManager.CanGift(g), "다음 날에는 다시 선물이 있다");

        // 먹이가 함께 나오는 비율 (고정 난수로 여러 번)
        var rng = new System.Random(7);
        int foods = 0, tries = 300, items0 = 0, items1 = 0;
        foreach (var id in RewardManager.GIFT_FOODS) items0 += repo.GetItemCount(id);
        for (int i = 0; i < tries; i++)
        {
            g.giftDay = 0;
            if (reward.ClaimGift(g.id, rng, out var gi) && gi.foodId != null) foods++;
        }
        foreach (var id in RewardManager.GIFT_FOODS) items1 += repo.GetItemCount(id);
        float rate = foods / (float)tries;
        Check(items1 - items0 == foods && Mathf.Abs(rate - RewardManager.GIFT_FOOD_CHANCE) < 0.08f,
              $"선물 먹이는 약 {RewardManager.GIFT_FOOD_CHANCE:P0} 확률로 인벤토리에 들어간다 (실제 {rate:P0})");
        bool foodAssets = true;
        foreach (var id in RewardManager.GIFT_FOODS) foodAssets &= Resources.Load<ItemSO>($"Items/{id}") != null;
        Check(foodAssets, "선물 먹이 에셋이 모두 있다");

        // 게코 목록 — 급한 일이 없을 때 "선물이 있어요"
        Check(Loc.TryGetPair("geckolist.status_gift", out _, out _) && Loc.TryGetPair("line.gift", out _, out _)
              && Loc.TryGetPair("gift.coin_food", out _, out _), "선물 문구가 번역표에 있다");

        // 어덜트 전용 장식 잠금
        var rock = DecorItem("decor_test_lock", DecorPlacement.Floor, DecorUse.None, 0);
        rock.requiredAdults = 2;
        Check(!TerrariumManager.IsUnlocked(rock, 1) && TerrariumManager.IsUnlocked(rock, 2),
              "어덜트 수가 모자라면 장식이 잠겨 있다");
        var terrarium = new TerrariumManager(repo);
        data.progress.adultCount = 1;
        Check(!terrarium.IsUnlocked(rock), "키운 어덜트 수(저장)로 잠금을 판정한다");
        var free = DecorItem("decor_test_free", DecorPlacement.Floor, DecorUse.None, 0);
        var newly = TerrariumManager.NewlyUnlocked(new[] { rock, free }, 1, 2);
        Check(newly.Count == 1 && newly[0] == rock && TerrariumManager.NewlyUnlocked(new[] { rock }, 2, 3).Count == 0,
              "어덜트가 늘어난 순간 새로 열린 장식만 알린다");
        UnityEngine.Object.DestroyImmediate(rock);
        UnityEngine.Object.DestroyImmediate(free);

        // 어덜트가 되면 사건에 키운 수가 담긴다
        var (_, gecko2, queue2, g2) = Fresh();
        g2.growthStage = 3; g2.createdAtTicks = DateTime.UtcNow.AddDays(-15).Ticks; g2.moltCount = 3; g2.affection = 60f;
        gecko2.EvaluateGrowth(g2.id);
        GeckoEvent last = default;
        while (queue2.TryDequeue(out var ev)) if (ev.type == GeckoEventType.GrowthUp) last = ev;
        Check(last.type == GeckoEventType.GrowthUp && last.adultsRaised == 1, "어덜트 사건에 키운 어덜트 수가 담긴다");

        // 예전 저장(v5) — 어덜트 게코 수만큼 보정
        var save = new SaveManager(SAVE_STEM);
        var old  = new PlayerData { saveVersion = 5 };
        var a1 = GeckoData.CreateNew("a", "crested"); a1.growthStage = GeckoManager.ADULT_STAGE;
        var a2 = GeckoData.CreateNew("b", "leopard"); a2.growthStage = GeckoManager.ADULT_STAGE;
        old.geckos.Add(a1); old.geckos.Add(a2); old.geckos.Add(GeckoData.CreateNew("c", "crested"));
        old.progress.adultCount = 1;
        save.Save(old);
        var migrated = save.Load();
        Check(migrated.saveVersion == PlayerData.CURRENT_SAVE_VERSION && migrated.progress.adultCount == 2,
              "예전 저장(v5)은 키운 어덜트 수를 지금 어덜트 수로 올린다");
        save.DeleteFiles();

        // 에셋 (DecorProxyArt.GenerateBatch로 만든 것)
        CheckLockedDecor("decor_moss_rock", DecorPlacement.Floor, DecorUse.None,   1);
        CheckLockedDecor("decor_cave",      DecorPlacement.Floor, DecorUse.Hide,   2);
        CheckLockedDecor("decor_driftwood", DecorPlacement.Wall,  DecorUse.Branch, 3);
        Check(FxSprites.Gift != null, "선물 상자 그림을 만들 수 있다");
    }

    private static void TestBookAndAchievements()
    {
        var (repo, gecko, _, g) = Fresh();
        var reward = new RewardManager(repo);
        gecko.OnCareDone += reward.RecordCare;
        var store  = new StoreManager(repo);
        var data   = repo.GetPlayerData();
        var species = SpeciesCatalog.All;

        // 종 목록
        Check(species.Count == 3 && species[0].speciesId == "crested" && species[1].speciesId == "leopard"
              && species[2].speciesId == "gargoyle", "도감 종 순서: 크레스티드 · 레오파드 · 가고일");

        // 만남 — 기본 게코는 보상 없이, 새 종 분양은 코인 +50 한 번
        Check(reward.HasMet("crested") && !reward.HasMet("leopard"), "기본 게코의 종은 처음부터 도감에 있다");
        data.coin = 1000;
        store.BuyGecko(species[1], "레오");
        Check(reward.HasMet("leopard") && store.LastMeetCoin == RewardManager.BOOK_MEET_COIN
              && data.coin == 1000 - species[1].coinPrice + RewardManager.BOOK_MEET_COIN,
              $"새 종을 처음 분양하면 도감에 기록되고 코인 +{RewardManager.BOOK_MEET_COIN}");
        int coin1 = data.coin;
        store.BuyGecko(species[1], "레오2");
        Check(store.LastMeetCoin == 0 && data.coin == coin1 - species[1].coinPrice, "같은 종을 또 분양하면 도감 보상이 없다");

        // 도감 완성 — 모든 종 어덜트
        Check(!reward.BookComplete(species) && !reward.CanClaimBook(species) && reward.ClaimBook(species) == 0,
              "모든 종을 어덜트로 키우기 전에는 도감 보상을 받을 수 없다");
        data.progress.adultSpeciesIds.AddRange(new[] { "crested", "leopard", "gargoyle" });
        int gem0 = data.gem;
        Check(reward.BookAdults(species, out int total) == 3 && total == 3 && reward.CanClaimBook(species)
              && reward.ClaimBook(species) == RewardManager.BOOK_COMPLETE_GEM && data.gem == gem0 + RewardManager.BOOK_COMPLETE_GEM,
              $"모든 종을 어덜트로 키우면 젬 +{RewardManager.BOOK_COMPLETE_GEM}");
        Check(!reward.CanClaimBook(species) && reward.ClaimBook(species) == 0 && data.gem == gem0 + RewardManager.BOOK_COMPLETE_GEM,
              "도감 완성 보상은 한 번만");

        // 업적 — 쓰다듬기·먹이는 목표를 넘겨도 센다
        g.mood = 50f;
        int pets = 0;
        for (int i = 0; i < 6; i++) if (gecko.Pet(g.id) == CareResult.Done) pets++;
        Check(data.progress.petCount == pets && pets == 4, "업적: 실제로 한 쓰다듬기만 센다 (삐짐 제외, 오늘의 목표를 넘겨도)");
        repo.AddItem("cricket_small", 5);
        var cricket = FoodItem("cricket_small", hunger: 5f);
        int feeds = 0;
        for (int i = 0; i < 3; i++) { g.hunger = 40f; if (gecko.FeedGecko(g.id, cricket) == CareResult.Done) feeds++; }
        Check(data.progress.feedCount == 3 && feeds == 3, "업적: 먹이 준 횟수를 센다");

        RewardManager.TryGetAchievement("full_house", out var house);
        Check(reward.StatValue(AchievementStat.Geckos) == 3 && reward.IsAchieved(house) && reward.CanClaimAchievement(house),
              "업적 \"북적이는 집\": 게코 3마리");
        RewardManager.TryGetAchievement("gentle_hand", out var gentle);
        Check(!reward.IsAchieved(gentle) && reward.AchievementProgress(gentle) == 4
              && !reward.ClaimAchievement(gentle.id, out _, out _), "달성 전 업적은 받을 수 없다");

        g.moltCount = 1;
        RewardManager.TryGetAchievement("first_molt", out var firstMolt);
        Check(reward.IsAchieved(firstMolt) && reward.StatValue(AchievementStat.Molts) == 1, "업적 \"첫 허물\": 게코들의 허물 합");

        var fresh = reward.TakeNewlyAchieved();
        Check(fresh != null && fresh.Exists(a => a.id == "full_house") && fresh.Exists(a => a.id == "first_molt")
              && reward.TakeNewlyAchieved() == null, "새로 달성한 업적은 한 번만 알린다");

        int claimable = reward.ClaimableCount(species);
        int coin2 = data.coin;
        Check(reward.ClaimAchievement("full_house", out int hc, out int hg) && hc == house.coin && hg == house.gem
              && data.coin == coin2 + house.coin && reward.IsClaimed("full_house")
              && reward.ClaimableCount(species) == claimable - 1, "업적 보상을 받으면 코인이 들어오고 받은 것으로 기록된다");
        Check(!reward.ClaimAchievement("full_house", out _, out _) && data.coin == coin2 + house.coin, "업적 보상은 한 번만");
        Check(!reward.ClaimAchievement("no_such", out _, out _), "없는 업적 id는 무시한다");

        // 오늘의 돌봄 보상 → 꾸준한 돌봄
        int goalDays0 = data.progress.goalDays;
        data.dailyGoal = new DailyGoalData { day = RewardManager.TodayNumber(), fed = 2, watered = 2, petted = 3, cleaned = 1 };
        Check(reward.ClaimGoals() > 0 && data.progress.goalDays == goalDays0 + 1, "오늘의 돌봄 보상을 받으면 꾸준한 돌봄이 1 오른다");

        // 저장 파일 — 기록이 남는다
        var reloaded = new SaveManager(SAVE_STEM).Load();
        Check(reloaded.progress.achievements.Contains("full_house") && reloaded.progress.bookRewardClaimed
              && reloaded.progress.unlockedSpeciesIds.Contains("leopard"), "도감·업적 기록이 저장 파일에 남는다");

        // 예전 저장(v6) — 지금 게코의 종과 어덜트 종을 만남으로
        var save = new SaveManager(SAVE_STEM);
        var old  = new PlayerData { saveVersion = 6 };
        old.geckos.Add(GeckoData.CreateNew("a", "leopard"));
        old.progress.adultSpeciesIds.Add("gargoyle");
        old.progress.unlockedSpeciesIds = null;
        old.progress.achievements = null;
        int oldCoin = old.coin;
        save.Save(old);
        var migrated = save.Load();
        Check(migrated.saveVersion == PlayerData.CURRENT_SAVE_VERSION && migrated.coin == oldCoin
              && migrated.progress.unlockedSpeciesIds.Contains("leopard") && migrated.progress.unlockedSpeciesIds.Contains("gargoyle")
              && !migrated.progress.unlockedSpeciesIds.Contains("crested") && migrated.progress.achievements != null,
              "예전 저장(v6): 키우던 종·어덜트 종을 도감에 기록 (보상 없이)");
        save.DeleteFiles();

        // 문구
        bool locOk = true;
        foreach (var a in RewardManager.ACHIEVEMENTS)
            locOk &= Loc.TryGetPair(a.NameKey, out _, out _) && Loc.TryGetPair(a.DescKey, out _, out _);
        foreach (var k in new[] { "book.button", "book.button_count", "book.title", "book.close", "book.tab_book", "book.tab_achieve",
                                   "book.unknown", "book.stamp_met", "book.complete", "book.claimed", "book.met_reward",
                                   "achieve.claim", "achieve.reward_coin", "achieve.reward_gem", "achieve.progress", "achieve.done" })
            locOk &= Loc.TryGetPair(k, out _, out _);
        foreach (var s in species) locOk &= Loc.TryGetPair("species." + s.speciesId, out _, out _) || !string.IsNullOrEmpty(s.displayName);
        Check(locOk, "도감·업적 문구가 모두 번역표에 있다");
    }

    private static void TestBond()
    {
        var (repo, gecko, queue, g) = Fresh();
        var data  = repo.GetPlayerData();
        int today = RewardManager.TodayNumber();
        while (queue.TryDequeue(out _)) { }

        // 레벨 경계
        Check(GeckoBond.LevelOf(0f) == 0 && GeckoBond.LevelOf(19.9f) == 0 && GeckoBond.LevelOf(20f) == 1
              && GeckoBond.LevelOf(100f) == 3 && GeckoBond.LevelOf(299f) == 4 && GeckoBond.LevelOf(9999f) == GeckoBond.MAX_LEVEL,
              "유대 레벨 경계 (20 · 50 · 100 · 180 · 300)");
        Check(GeckoBond.NextPoints(0) == 20f && GeckoBond.NextPoints(GeckoBond.MAX_LEVEL) < 0f, "다음 레벨 점수 · 최고 레벨");
        Check(GeckoBond.PerkOf(3) == BondPerk.Come && GeckoBond.PerkOf(0) == BondPerk.None, "레벨마다 풀리는 것");

        // 애정도 → 넘친 몫 → 하루 한도
        var t = GeckoData.CreateNew("t", "crested");
        t.affection = 98f;
        float toAff = GeckoBond.AddAffection(t, 5f, today);
        Check(Mathf.Approximately(toAff, 2f) && Mathf.Approximately(t.affection, 100f) && Mathf.Approximately(t.bondOverflow, 3f)
              && Mathf.Approximately(GeckoBond.Points(t), 103f), "애정도 100을 넘는 몫은 유대 점수로 쌓인다");
        GeckoBond.AddAffection(t, 100f, today);
        Check(Mathf.Approximately(t.bondOverflow, GeckoBond.DAILY_OVERFLOW_CAP) && GeckoBond.TodayFull(t, today),
              $"넘친 몫은 하루 {GeckoBond.DAILY_OVERFLOW_CAP}까지");
        GeckoBond.AddAffection(t, 5f, today + 1);
        Check(Mathf.Approximately(t.bondOverflow, GeckoBond.DAILY_OVERFLOW_CAP + 5f) && !GeckoBond.TodayFull(t, today + 1),
              "다음 날에는 다시 쌓인다");

        // 쓰다듬기 → 레벨업 · 보상 · 사건 (게코 매니저 경로)
        g.affection = 18f;
        g.mood = 50f;
        int coin0 = data.coin;
        Check(gecko.Pet(g.id) == CareResult.Done && GeckoBond.Level(g) == 1 && g.bondRewardedLevel == 1
              && data.coin == coin0 + GeckoBond.LEVEL_REWARDS[1].coin, "유대 Lv.1이 되면 보상을 받는다");
        Check(queue.TryDequeue(out var e1) && e1.type == GeckoEventType.BondUp && e1.bondLevel == 1
              && e1.rewardCoin == GeckoBond.LEVEL_REWARDS[1].coin, "유대 레벨업 사건에 레벨과 보상이 담긴다");

        // 여러 레벨을 한 번에 넘기면 그 사이 보상을 모두
        g.affection = 99f;
        g.bondOverflow = 0f;
        g.bondRewardedLevel = 1;
        int gem0 = data.gem;
        coin0 = data.coin;
        gecko.CheckBondLevel(g);   // 99 → Lv.2 (50) 만 — 3은 100부터
        Check(g.bondRewardedLevel == 2 && data.coin == coin0 + GeckoBond.LEVEL_REWARDS[2].coin, "Lv.2 보상");
        g.bondOverflow = 250f;     // 349 → Lv.5
        coin0 = data.coin;
        gecko.CheckBondLevel(g);
        int wantCoin = GeckoBond.LEVEL_REWARDS[3].coin + GeckoBond.LEVEL_REWARDS[4].coin + GeckoBond.LEVEL_REWARDS[5].coin;
        int wantGem  = GeckoBond.LEVEL_REWARDS[3].gem  + GeckoBond.LEVEL_REWARDS[4].gem  + GeckoBond.LEVEL_REWARDS[5].gem;
        Check(g.bondRewardedLevel == 5 && data.coin == coin0 + wantCoin && data.gem == gem0 + wantGem,
              "여러 레벨을 한 번에 넘으면 그 사이 보상을 모두 받는다");
        coin0 = data.coin;
        gecko.CheckBondLevel(g);
        Check(data.coin == coin0, "같은 레벨 보상은 한 번만");

        // 쓰다듬기 한도 — Lv.2부터 6번
        var fresh = GeckoData.CreateNew("f", "crested");
        Check(GeckoBond.PetLimit(fresh) == GeckoBond.PET_LIMIT && GeckoBond.PetLimit(g) == GeckoBond.PET_LIMIT_LOVER,
              "쓰다듬기를 좋아하는 횟수: 기본 4 · 유대 Lv.2부터 6");
        var (_, gecko2, _, g2) = Fresh();
        g2.affection = 60f;   // Lv.2
        g2.bondRewardedLevel = 2;
        g2.mood = 50f;
        int done = 0;
        for (int i = 0; i < 8; i++) if (gecko2.Pet(g2.id) == CareResult.Done) done++;
        Check(done == GeckoBond.PET_LIMIT_LOVER, $"유대 Lv.2 이상이면 연달아 {GeckoBond.PET_LIMIT_LOVER}번까지 좋아한다 (실제 {done})");

        // 업적 "단짝"
        var reward = new RewardManager(repo);
        RewardManager.TryGetAchievement("best_friend", out var bf);
        Check(reward.StatValue(AchievementStat.BondLevel) == GeckoBond.MAX_LEVEL && reward.IsAchieved(bf), "업적 \"단짝\": 유대 Lv.5 게코");

        // 말풍선 문구
        string info = HomeUIController.DescribeBond(g, today);
        Check(info.Contains(Loc.Get("bond.info_max")) && info.Contains(Loc.Get("bond.perk.5")), "유대 말풍선: 최고 레벨 · 풀린 것");
        var low = GeckoData.CreateNew("l", "crested");
        Check(HomeUIController.DescribeBond(low, today).Contains(Loc.Get("bond.info_none")), "유대 말풍선: 아직 풀린 것 없음");

        // 저장 · 예전 저장(v7)
        var save = new SaveManager(SAVE_STEM);
        var old  = new PlayerData { saveVersion = 7 };
        var og = GeckoData.CreateNew("o", "crested");
        og.affection = 100f;  // Lv.3
        old.geckos.Add(og);
        int oldCoin = old.coin;
        save.Save(old);
        var migrated = save.Load();
        Check(migrated.saveVersion == PlayerData.CURRENT_SAVE_VERSION && migrated.geckos[0].bondRewardedLevel == 3 && migrated.coin == oldCoin,
              "예전 저장(v7): 지금 애정도의 유대 레벨은 보상 없이 받은 것으로");
        save.DeleteFiles();

        // 문구
        bool locOk = true;
        for (int lv = 1; lv <= GeckoBond.MAX_LEVEL; lv++)
            locOk &= Loc.TryGetPair("bond.perk." + lv, out _, out _) && Loc.TryGetPair("bond.perk_desc." + lv, out _, out _);
        foreach (var k in new[] { "bond.label", "bond.info_title", "bond.info_next", "bond.info_max", "bond.info_perks", "bond.info_none",
                                   "bond.info_today_full", "event.bond", "geckolist.bond", "line.bond", "line.greet", "line.come",
                                   "line.trick", "line.palm", "achieve.best_friend", "achieve.desc.bondlevel" })
            locOk &= Loc.TryGetPair(k, out _, out _);
        Check(locOk, "유대 문구가 모두 번역표에 있다");
        Check(GeckoMotor.DurationOf(GeckoAction.Spin) > 0f && FxSprites.Hand != null, "재롱 동작 길이 · 손바닥 그림");
    }

    private static void TestAtmosphere()
    {
        // 공기 원근 — 앞은 그대로, 뒤로 갈수록 차갑고 살짝 어둡게
        var go = new GameObject("AtmosphereTest", typeof(RectTransform));
        try
        {
            var move = go.AddComponent<GeckoMovementAI>();   // 기본값 groundBand (380, 950)
            Color near = move.DepthTintFor(380f), far = move.DepthTintFor(950f);
            Check(near == Color.white && far.b > far.r && far.r < 1f && move.DepthAt(380f) < 0.01f && move.DepthAt(950f) > 0.99f,
                  "공기 원근: 앞쪽은 원래 색, 뒤로 갈수록 차갑고 어둡게");
            Check(move.DepthTintFor(-9999f) == Color.white && move.DepthTintFor(99999f) == far, "공기 원근: 범위 밖은 끝 값");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }

        Check(FxSprites.Vignette != null, "비네트 그림을 만들 수 있다");
    }

    private static void TestMorph()
    {
        // 표 — 종마다 흔함 1 · 희귀 2 · 아주 희귀 1, id 겹침 없음, 이름 번역
        bool tableOk = true;
        var ids = new HashSet<string>();
        foreach (var s in new[] { "crested", "leopard", "gargoyle" })
        {
            var list = GeckoMorph.ForSpecies(s);
            tableOk &= list.Count == 4
                && list.FindAll(m => m.rarity == MorphRarity.Common).Count == 1
                && list.FindAll(m => m.rarity == MorphRarity.Rare).Count == 2
                && list.FindAll(m => m.rarity == MorphRarity.VeryRare).Count == 1;
        }
        foreach (var m in GeckoMorph.ALL)
            tableOk &= ids.Add(m.id) && Loc.TryGetPair(m.NameKey, out _, out _);
        Check(tableOk, "모프 표: 종마다 흔함 1 · 희귀 2 · 아주 희귀 1, 이름이 번역표에 있다");
        Check(!GeckoMorph.Roll("no_such", new System.Random(1)).IsValid, "모프가 없는 종은 뽑지 않는다");

        // 확률 — 고정 난수 6000번
        var rng = new System.Random(123);
        int[] counts = new int[3];
        const int N = 6000;
        for (int i = 0; i < N; i++) counts[(int)GeckoMorph.Roll("crested", rng).rarity]++;
        float c0 = counts[0] / (float)N, c1 = counts[1] / (float)N, c2 = counts[2] / (float)N;
        Check(Mathf.Abs(c0 - 0.70f) < 0.03f && Mathf.Abs(c1 - 0.25f) < 0.03f && Mathf.Abs(c2 - 0.05f) < 0.015f,
              $"모프 등급 확률 70 · 25 · 5 (실제 {c0:P0} · {c1:P0} · {c2:P1})");

        // 어덜트가 되면 모프가 정해지고 도감 · 처음 보상 · 사건
        var (repo, gecko, queue, g) = Fresh();
        var data = repo.GetPlayerData();
        g.growthStage = 3; g.createdAtTicks = DateTime.UtcNow.AddDays(-15).Ticks; g.moltCount = 3; g.affection = 60f;
        int coin0 = data.coin, gem0 = data.gem;
        gecko.EvaluateGrowth(g.id);
        var def = GeckoMorph.Find(g.morphId);
        var (rc, rg) = GeckoMorph.FIRST_REWARD[(int)def.rarity];
        Check(def.IsValid && def.speciesId == "crested" && GeckoMorph.Has(data, g.morphId) && gecko.LastMorph.first
              && data.coin == coin0 + GeckoManager.ADULT_REWARD_COIN + rc && data.gem == gem0 + GeckoManager.ADULT_REWARD_GEM + rg,
              "어덜트가 되면 모프가 정해지고 도감에 기록, 처음 얻은 모프 보상");
        GeckoEvent growth = default, morph = default;
        int order = 0, growthAt = -1, morphAt = -1;
        while (queue.TryDequeue(out var ev))
        {
            if (ev.type == GeckoEventType.GrowthUp)    { growth = ev; growthAt = order; }
            if (ev.type == GeckoEventType.MorphReveal) { morph  = ev; morphAt  = order; }
            order++;
        }
        Check(growthAt >= 0 && morphAt > growthAt && morph.morphId == g.morphId && morph.morphFirst
              && morph.rewardCoin == rc && morph.rewardGem == rg, "모프 사건은 성장 사건 다음, 모프·보상이 담긴다");
        string msg = HomeUIController.MorphMessage(morph);
        Check(msg.Contains(Loc.Get(def.NameKey)) && msg.Contains(Loc.Get(HomeUIController.RarityKey(def.rarity))),
              "모프 알림에 이름과 등급이 나온다");

        // 같은 모프는 두 번째부터 보상 없음 · 이미 정해진 모프는 그대로
        var twin = GeckoData.CreateNew("twin", "crested");
        twin.morphId = g.morphId;
        coin0 = data.coin;
        var again = GeckoMorph.Assign(data, twin, new System.Random(5), reward: true);
        Check(!again.first && again.coin == 0 && data.coin == coin0 && twin.morphId == g.morphId,
              "이미 얻은 모프는 보상이 없고, 정해진 모프는 바뀌지 않는다");

        // 모습 — 어덜트 전·연출 전에는 종별 기본색, 뒤에는 모프
        var baby = GeckoData.CreateNew("b", "leopard");
        var look = GeckoMorph.LookOf(baby, revealed: false);
        Check(!look.IsValid && look.body == GeckoMorph.BaseColor("leopard") && look.pattern == MorphPattern.None
              && GeckoMorph.BaseColor("leopard") != GeckoMorph.BaseColor("gargoyle"), "모프 전에는 종별 기본색 (종마다 다름)");
        Check(GeckoMorph.LookOf(g, revealed: true).id == g.morphId && !GeckoMorph.LookOf(g, revealed: false).IsValid,
              "모프가 드러나면 모프 모습, 연출 전에는 기본색");
        Check(GeckoMorph.SeedOf(g) == GeckoMorph.SeedOf(g) && GeckoMorph.SeedOf(g) != GeckoMorph.SeedOf(baby),
              "무늬 자리는 게코마다 고정");

        // 업적 · 예전 저장(v8)
        var reward = new RewardManager(repo);
        data.progress.morphIds.Clear();
        foreach (var m in GeckoMorph.ALL) if (data.progress.morphIds.Count < 6) data.progress.morphIds.Add(m.id);
        RewardManager.TryGetAchievement("morph_collector", out var collector);
        Check(reward.StatValue(AchievementStat.Morphs) == 6 && reward.IsAchieved(collector), "업적 \"모프 수집가\": 모프 6종");

        var save = new SaveManager(SAVE_STEM);
        var old  = new PlayerData { saveVersion = 8 };
        var oa = GeckoData.CreateNew("a", "gargoyle"); oa.growthStage = GeckoManager.ADULT_STAGE;
        var ob = GeckoData.CreateNew("b", "gargoyle");
        old.geckos.Add(oa); old.geckos.Add(ob);
        old.progress.morphIds = null;
        int oldCoin = old.coin;
        save.Save(old);
        var migrated = save.Load();
        var ma = migrated.geckos[0];
        Check(migrated.saveVersion == PlayerData.CURRENT_SAVE_VERSION && GeckoMorph.Find(ma.morphId).speciesId == "gargoyle"
              && string.IsNullOrEmpty(migrated.geckos[1].morphId) && migrated.progress.morphIds.Count == 1
              && migrated.coin == oldCoin, "예전 저장(v8): 어덜트만 모프를 정하고 도감에 기록 (보상 없음)");
        save.DeleteFiles();

        // 그림 — 색 곱하기 · 무늬 점 (진짜 프록시 게코)
        var rig = MakeProxyRig(out var rigRoot);
        if (rig != null)
        {
            try
            {
                var dal = GeckoMorph.Find("crested_dalmatian");
                rig.SetMorph(dal.body, dal.pattern, dal.patternColor, 42);
                bool colorOk = rig.MorphColorOf(GeckoPartId.Body) == dal.body && rig.MorphColorOf(GeckoPartId.EyeL) == Color.white;
                int dots = rig.PatternDotCount;
                rig.SetMorph(Color.white, MorphPattern.None, Color.clear, 42);
                Check(colorOk && dots > 0 && rig.PatternDotCount == 0,
                      $"모프 그림: 몸에만 색을 곱하고 무늬 점을 얹는다 (점 {dots}개), 무늬 없음이면 점이 사라진다");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rigRoot);
            }
        }
        else
        {
            Check(false, "모프 그림 검사용 프록시 게코를 만들 수 없다");
        }

        bool locOk = true;
        foreach (var k in new[] { "event.morph", "morph.new", "morph.rarity.0", "morph.rarity.1", "morph.rarity.2",
                                   "line.morph", "book.morphs", "geckolist.morph", "achieve.morph_collector", "achieve.desc.morphs" })
            locOk &= Loc.TryGetPair(k, out _, out _);
        Check(locOk, "모프 문구가 모두 번역표에 있다");
    }

    /// <summary>
    /// 씬 게코가 실제로 쓰는 그림 (2026-09-20) — 부위 판정·모프 검사는 화면에 나오는 그림으로 해야 한다.
    /// 최종 그림이 없으면 프록시로 물러난다.
    /// </summary>
    private static GeckoSkin SceneSkin()
    {
        var skin = AssetDatabase.LoadAssetAtPath<GeckoSkin>("Assets/_Game/GeckoSkins/GeckoSkin_Painted.asset");
        if (skin == null) skin = AssetDatabase.LoadAssetAtPath<GeckoSkin>("Assets/_Game/GeckoSkins/GeckoSkin_Proxy.asset");
        return skin;
    }

    // 진짜 그림을 입힌 게코 (그림 검사용). 없으면 null — root는 부르는 쪽에서 지운다
    private static GeckoRig MakeProxyRig(out GameObject root)
    {
        root = null;
        var skin = SceneSkin();
        if (skin == null) return null;
        root = new GameObject("MorphTestGecko", typeof(RectTransform));
        var rig = root.AddComponent<GeckoRig>();
        rig.SetSkin(skin, useStageSkins: false);
        rig.SetGrowthStage(4, immediate: true);
        rig.SolveRest();
        return rig;
    }

    private static void CheckLockedDecor(string id, DecorPlacement placement, DecorUse use, int adults)
    {
        var item = DecorCatalog.Find(id);
        Check(item != null && item.placement == placement && item.use == use && item.requiredAdults == adults
              && item.previewSprite != null && item.icon != null
              && Loc.TryGetPair("decor." + id, out _, out _),
              $"어덜트 전용 장식 {id} — 어덜트 {adults}마리, 그림·이름 있음");
    }

    private static void TestMultipleGeckos()
    {
        var (repo, gecko, queue, g) = Fresh();
        var data  = repo.GetPlayerData();
        var store = new StoreManager(repo);
        string failed = null;
        store.OnPurchaseFailed += r => failed = r;

        var crested = Species("crested", 300, free: true);
        var leopard = Species("leopard", 300, free: false);

        // 무료 분양은 그 종이 한 마리도 없을 때만 — 하코가 크레스티드이므로 크레스티드도 값을 낸다
        Check(!StoreManager.IsFreeFor(data, crested) && !StoreManager.IsFreeFor(data, leopard),
              "크레스티드를 키우고 있으면 크레스티드도 무료가 아니다");
        var empty = new PlayerData();
        Check(StoreManager.IsFreeFor(empty, crested) && !StoreManager.IsFreeFor(empty, leopard),
              "크레스티드가 없으면 크레스티드만 무료다");

        data.coin = 1000;
        store.BuyGecko(crested, "별이");
        Check(failed == null && data.geckos.Count == 2 && data.coin == 700, "두 번째 크레스티드는 300코인을 낸다");

        data.coin = 10000;
        while (data.geckos.Count < StoreManager.MAX_GECKOS) store.BuyGecko(leopard, "");
        int coinFull = data.coin;
        failed = null;
        store.BuyGecko(leopard, "");
        Check(failed != null && data.geckos.Count == StoreManager.MAX_GECKOS && data.coin == coinFull
              && !StoreManager.CanAdoptMore(data),
              $"게코는 {StoreManager.MAX_GECKOS}마리까지 — 넘으면 거절하고 코인을 받지 않는다");

        // 어덜트 보상: 종마다 처음만 크게
        var second = data.geckos[1];   // 두 번째 크레스티드
        void MakeAdult(GeckoData x)
        {
            x.growthStage    = 3;
            x.createdAtTicks = DateTime.UtcNow.AddDays(-15).Ticks;
            x.moltCount      = 3;
            x.affection      = 60f;
            gecko.EvaluateGrowth(x.id);
        }
        while (queue.TryDequeue(out _)) { }

        int coin0 = data.coin, gem0 = data.gem;
        MakeAdult(g);
        Check(data.coin == coin0 + GeckoManager.ADULT_REWARD_COIN + gecko.LastMorph.coin && data.gem == gem0 + GeckoManager.ADULT_REWARD_GEM + gecko.LastMorph.gem
              && data.progress.adultSpeciesIds.Contains("crested"), "처음 키운 크레스티드 어덜트는 큰 보상을 받는다");
        Check(queue.TryDequeue(out var e1) && e1.rewardCoin == GeckoManager.ADULT_REWARD_COIN
              && e1.rewardGem == GeckoManager.ADULT_REWARD_GEM, "성장 사건에 실제로 받은 보상이 담긴다");
        while (queue.TryDequeue(out _)) { }   // 뒤따르는 모프 사건

        coin0 = data.coin; gem0 = data.gem;
        MakeAdult(second);
        Check(data.coin == coin0 + GeckoManager.ADULT_REWARD_REPEAT_COIN + gecko.LastMorph.coin && data.gem == gem0 + gecko.LastMorph.gem
              && data.progress.adultCount == 2,
              $"같은 종 두 번째 어덜트는 코인 +{GeckoManager.ADULT_REWARD_REPEAT_COIN}만 받는다");
        Check(queue.TryDequeue(out var e2) && e2.rewardCoin == GeckoManager.ADULT_REWARD_REPEAT_COIN && e2.rewardGem == 0
              && Loc.Format("event.adult_coin", "x", e2.rewardCoin).Contains(e2.rewardCoin.ToString()),
              "두 번째 어덜트 알림은 코인만 보여준다");

        coin0 = data.coin;
        MakeAdult(data.geckos[2]);   // 레오파드
        Check(data.coin == coin0 + GeckoManager.ADULT_REWARD_COIN + gecko.LastMorph.coin, "다른 종의 첫 어덜트는 다시 큰 보상을 받는다");

        // 예전 저장(v4)에 어덜트가 있으면 그 종은 받은 것으로 친다
        var save = new SaveManager(SAVE_STEM);
        var old  = new PlayerData { saveVersion = 4 };
        var oldAdult = GeckoData.CreateNew("하코", "gargoyle");
        oldAdult.growthStage = GeckoManager.ADULT_STAGE;
        old.geckos.Add(oldAdult);
        old.geckos.Add(GeckoData.CreateNew("별이", "leopard"));
        old.progress.adultSpeciesIds = null;
        save.Save(old);
        var migrated = save.Load();
        Check(migrated.saveVersion == PlayerData.CURRENT_SAVE_VERSION
              && migrated.progress.adultSpeciesIds.Count == 1 && migrated.progress.adultSpeciesIds[0] == "gargoyle",
              "예전 저장(v4)의 어덜트 종은 큰 보상을 이미 받은 것으로 기록된다");
        save.DeleteFiles();

        // 게코 목록 상태 표시
        var s = GeckoData.CreateNew("s", "crested");
        s.hunger = s.thirst = s.cleanliness = s.health = 80f;
        Check(GeckoManager.AlertOf(s) == GeckoAlert.None, "상태가 좋으면 \"잘 지내요\"");
        s.cleanliness = 10f;
        Check(GeckoManager.AlertOf(s) == GeckoAlert.Dirty, "청결 20 이하면 \"청소 필요\"");
        s.thirst = 20f;
        Check(GeckoManager.AlertOf(s) == GeckoAlert.Thirsty, "목마름이 청소보다 급하다");
        s.hunger = 25f;
        Check(GeckoManager.AlertOf(s) == GeckoAlert.Hungry, "배고픔이 목마름보다 급하다");
        s.health = 15f;
        Check(GeckoManager.AlertOf(s) == GeckoAlert.Sick, "건강 20 이하가 가장 급하다");
        bool keysOk = true;
        foreach (GeckoAlert a in Enum.GetValues(typeof(GeckoAlert)))
            keysOk &= Loc.TryGetPair(GeckoSlotUI.AlertKey(a), out _, out _);
        Check(keysOk, "상태 표시 문구가 모두 번역표에 있다");

        // 알림 — 모든 게코 중 가장 먼저 돌봐야 할 게코
        var a1 = GeckoData.CreateNew("a", "crested"); a1.hunger = 90f; a1.thirst = 90f;
        var a2 = GeckoData.CreateNew("b", "crested"); a2.hunger = 90f; a2.thirst = 40f;
        var a3 = GeckoData.CreateNew("c", "crested"); a3.hunger = 50f; a3.thirst = 90f;
        var urgent = GeckoManager.MostUrgent(new List<GeckoData> { a1, a2, a3 }, 25f, out float hours);
        Check(urgent == a2 && Mathf.Approximately(hours, GeckoManager.HoursUntilCareNeeded(a2, 25f))
              && GeckoManager.ThirstFirst(a2, 25f),
              "알림은 선택과 상관없이 가장 먼저 목마르거나 배고파질 게코로 예약한다");
        Check(!GeckoManager.ThirstFirst(a3, 25f), "배고픔이 먼저면 \"배고파해요\" 알림");
        Check(GeckoManager.MostUrgent(new List<GeckoData>(), 25f, out _) == null, "게코가 없으면 돌봄 알림을 예약하지 않는다");

        // 홈 화면은 선택한 게코만 그린다 (2026-09-20)
        // 시간 진행은 모든 게코에 OnStateChanged를 보내므로, 걸러내지 않으면 목록 마지막 게코가 화면을 덮는다
        Check(HomeUIController.IsHomeGecko(a1, a1.id) && !HomeUIController.IsHomeGecko(a2, a1.id),
              "홈 갱신: 선택한 게코의 상태 변화만 화면에 반영한다");
        Check(!HomeUIController.IsHomeGecko(null, a1.id) && HomeUIController.IsHomeGecko(a1, null),
              "홈 갱신: 선택이 비어 있으면(저장 손상) 화면이 비지 않게 그대로 그린다");

        // 실제로 두 마리를 키우며 시간을 보내도 선택 게코 외에는 홈이 반응하지 않는다
        var (repo2, gecko2, _, first) = Fresh();
        var data2 = repo2.GetPlayerData();
        data2.geckos.Add(GeckoData.CreateNew("두번째", "crested"));
        var last = data2.geckos[data2.geckos.Count - 1];
        int shown = 0;
        gecko2.OnStateChanged += x => { if (HomeUIController.IsHomeGecko(x, data2.selectedGeckoId)) shown++; };
        foreach (var each in data2.geckos) each.lastUpdatedTicks = DateTime.UtcNow.AddHours(-1).Ticks;
        gecko2.ApplyElapsedProgressAll();
        Check(shown == 1 && data2.selectedGeckoId == first.id && last.id != first.id,
              $"시간 진행이 게코 2마리에 일어나도 홈은 선택 게코 1번만 그린다 (그린 횟수 {shown})");
    }

    private static void TestDailyGoals()
    {
        var (repo, gecko, _, g) = Fresh();
        var reward = new RewardManager(repo);
        gecko.OnCareDone += reward.RecordCare;   // AppBootstrap과 같은 연결
        var data = repo.GetPlayerData();

        Check(!reward.GoalsComplete && !reward.CanClaimGoals() && reward.GoalCount(CareKind.Feed) == 0,
              "새 하루에는 오늘의 돌봄 목표가 비어 있다");

        g.thirst = 100f;
        Check(gecko.GiveWater(g.id) == CareResult.Refused && reward.GoalCount(CareKind.Water) == 0,
              "거절당한 돌봄은 목표에 세지 않는다");

        var food = FoodItem("cricket_small", hunger: 5f);
        repo.AddItem("cricket_small", 5);
        for (int i = 0; i < 3; i++)
        {
            g.hunger = 10f;
            gecko.FeedGecko(g.id, food);
        }
        Check(reward.GoalCount(CareKind.Feed) == RewardManager.GoalTarget(CareKind.Feed),
              "목표를 넘겨도 목표 수까지만 센다 (먹이 3번 → 2/2)");

        for (int i = 0; i < RewardManager.GoalTarget(CareKind.Water); i++)
        {
            g.thirst = 10f;
            gecko.GiveWater(g.id);
        }
        for (int i = 0; i < RewardManager.GoalTarget(CareKind.Pet); i++)
            gecko.Pet(g.id);
        g.cleanliness = 10f;
        gecko.Clean(g.id);
        Check(reward.GoalsComplete && reward.CanClaimGoals(), "먹이 2 · 물 2 · 쓰다듬기 3 · 청소 1을 채우면 보상을 받을 수 있다");

        int coin0 = data.coin;
        Check(reward.ClaimGoals() == RewardManager.GOAL_REWARD_COIN && data.coin == coin0 + RewardManager.GOAL_REWARD_COIN,
              $"오늘의 돌봄 보상 코인 +{RewardManager.GOAL_REWARD_COIN}");
        Check(reward.ClaimGoals() == 0 && data.coin == coin0 + RewardManager.GOAL_REWARD_COIN && reward.GoalsClaimed,
              "오늘의 돌봄 보상은 하루에 한 번");

        data.dailyGoal.day -= 1;   // 하루가 지났다
        Check(reward.GoalCount(CareKind.Feed) == 0 && !reward.GoalsClaimed && !reward.CanClaimGoals(),
              "다음 날에는 목표가 새로 시작된다");
    }

    private static void TestTouchAndMovement()
    {
        // 원근 — 발 높이가 높을수록(멀수록) 작게
        var band = new Vector2(380f, 950f);
        Check(Mathf.Approximately(GeckoMovementAI.DepthScaleAt(380f, band, 0.62f), 1f)
              && Mathf.Approximately(GeckoMovementAI.DepthScaleAt(950f, band, 0.62f), 0.62f)
              && Mathf.Abs(GeckoMovementAI.DepthScaleAt(665f, band, 0.62f) - 0.81f) < 0.001f,
              "원근: 가까운 쪽 100% · 가운데 81% · 먼 쪽 62%");
        Check(Mathf.Approximately(GeckoMovementAI.DepthScaleAt(2000f, band, 0.62f), 0.62f)
              && Mathf.Approximately(GeckoMovementAI.DepthScaleAt(100f, band, 0.62f), 1f),
              "원근: 범위 밖은 끝 값에서 멈춘다");
        Check(Mathf.Approximately(GeckoMovementAI.ClimbTopY(2400f, 420f, 300f), 1680f),
              "벽 타기 한계 = 영역 높이 - 위 여백 - 몸 길이");

        // 부위 판정 순서 — 작은 부위부터
        Check(GeckoTouch.PriorityOf(GeckoPartId.EyeL) < GeckoTouch.PriorityOf(GeckoPartId.Head)
              && GeckoTouch.PriorityOf(GeckoPartId.Mouth) < GeckoTouch.PriorityOf(GeckoPartId.Head)
              && GeckoTouch.PriorityOf(GeckoPartId.LegFrontNear) < GeckoTouch.PriorityOf(GeckoPartId.Body)
              && GeckoTouch.PriorityOf(GeckoPartId.Head) < GeckoTouch.PriorityOf(GeckoPartId.Body)
              && GeckoTouch.PriorityOf(GeckoPartId.Tongue1) < 0,
              "터치: 눈·입·다리를 머리·몸통보다 먼저 보고, 혀는 판정하지 않는다");
        Check(GeckoTouch.TailZone(0.9f) == GeckoTouchZone.TailBase && GeckoTouch.TailZone(0.2f) == GeckoTouchZone.TailTip,
              "터치: 꼬리 그림의 관절 쪽(오른쪽)은 뿌리, 반대쪽은 꼬리 끝");

        // 판정 박스 — 눈·입은 표정 판(빈 그림)보다 작게 보고, 머리·몸통은 그림 그대로 (2026-09-20)
        Vector2 eyeBox = Vector2.zero, headBox = Vector2.zero, bodyBox = Vector2.zero;   // && 로 건너뛸 수 있어 미리 채운다
        bool boxOk = GeckoTouch.TryBoxOf(GeckoPartId.EyeL, out _, out eyeBox)
                     && GeckoTouch.TryBoxOf(GeckoPartId.Head, out _, out headBox)
                     && GeckoTouch.TryBoxOf(GeckoPartId.Body, out _, out bodyBox);
        Check(boxOk && eyeBox.x < 1f && eyeBox.y < 1f && headBox == Vector2.one && bodyBox == Vector2.one,
              "터치: 눈 판정 박스는 그림 판보다 작고, 머리·몸통은 그림 사각형 그대로");

        // 부위별 반응 문구가 번역표에 모두 있다
        var missing = new StringBuilder();
        foreach (var key in HomeUIController.TouchLineKeys)
            if (!Loc.TryGetPair(key, out _, out _)) missing.Append(key).Append(' ');
        Check(missing.Length == 0, missing.Length == 0 ? "부위별 반응 문구가 번역표에 모두 있다" : "번역표에 없는 반응 문구: " + missing);

        // 모든 동작의 곡선이 시작·끝에서 0 — 동작끼리 넘어갈 때 튀지 않는다 (길이 확인은 TestMotorActions)
        // → 곡선은 GeckoMotor 내부라 여기서는 새 동작이 목록·길이에 들어갔는지만 본다
        Check(GeckoMotor.DurationOf(GeckoAction.Yawn) > 0f && GeckoMotor.DurationOf(GeckoAction.Wave) > 0f
              && GeckoMotor.DurationOf(GeckoAction.PawShake) > 0f && GeckoMotor.DurationOf(GeckoAction.Kick) > 0f
              && GeckoMotor.DurationOf(GeckoAction.Shiver) > 0f, "만지기 반응 동작 5개(하품·앞발 인사·발 털기·뒷발 차기·부르르)에 길이가 있다");

        // 씬에 적용된 그림으로 부위 판정 — 부위 가운데를 누르면 그 부위 (오른쪽·왼쪽·벽 타는 자세 모두)
        var skin = SceneSkin();
        if (skin == null)
        {
            Check(false, "게코 스킨이 없어 부위 판정을 확인하지 못함 — Hako > Gecko > ① 프록시 게코 만들기");
            return;
        }

        var go = new GameObject("TouchTestGecko", typeof(RectTransform));
        try
        {
            var rig = go.AddComponent<GeckoRig>();
            rig.SetSkin(skin, useStageSkins: false);
            rig.SetGrowthStage(4, immediate: true);
            foreach (var (facingRight, angle) in new[] { (true, 0f), (false, 0f), (true, 90f), (false, -90f) })
            {
                go.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                rig.SetFacing(facingRight);
                rig.SolveRest();
                string pose = $"{(facingRight ? "오른쪽" : "왼쪽")}을 보고 {angle:0}도";
                Check(ZoneOfPart(rig, GeckoPartId.EyeL, 0.5f, 0.5f) == GeckoTouchZone.Eye
                      && ZoneOfPart(rig, GeckoPartId.EyeR, 0.5f, 0.5f) == GeckoTouchZone.Eye, $"터치({pose}): 눈 → 눈");
                Check(ZoneOfPart(rig, GeckoPartId.Mouth, 0.5f, 0.5f) == GeckoTouchZone.Mouth, $"터치({pose}): 입 → 입 (눈으로 잡히지 않음)");
                Check(ZoneOfPart(rig, GeckoPartId.LegFrontNear, 0.5f, 0.3f) == GeckoTouchZone.FrontLeg, $"터치({pose}): 앞다리 → 앞다리");
                Check(ZoneOfPart(rig, GeckoPartId.LegBackNear, 0.5f, 0.3f) == GeckoTouchZone.BackLeg, $"터치({pose}): 뒷다리 → 뒷다리");
                Check(ZoneOfPart(rig, GeckoPartId.Tail, 0.08f, 0.5f) == GeckoTouchZone.TailTip, $"터치({pose}): 꼬리 끝 → 꼬리 끝");
                Check(ZoneOfPart(rig, GeckoPartId.Body, 0.5f, 0.6f) == GeckoTouchZone.Body, $"터치({pose}): 몸통 → 몸통");
                Check(ZoneOfPart(rig, GeckoPartId.Head, 0.2f, 0.8f) == GeckoTouchZone.Head, $"터치({pose}): 머리 윗부분 → 머리");

                // 눈·입 표정 판이 머리를 삼키지 않는다 (2026-09-20)
                // 최종 그림의 eye_open·mouth_closed는 **빈 판**(눈 186×186 · 입 255×88)이라
                // 그림 사각형으로 판정하면 눈 판이 머리(408×210)보다 세로로 커져 머리가 거의 남지 않았다
                Check(ZoneOfPart(rig, GeckoPartId.Head, 0.5f, 0.92f) == GeckoTouchZone.Head,
                      $"터치({pose}): 머리 위 가운데 → 머리 (눈 판에 먹히지 않는다)");
                Check(ZoneOfPart(rig, GeckoPartId.Head, 0.1f, 0.5f) == GeckoTouchZone.Head,
                      $"터치({pose}): 뒤통수 → 머리");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    private static void TestTerrariumStructures()
    {
        Check(TerrariumLayout.PlacementOf(0) == DecorPlacement.Floor && TerrariumLayout.PlacementOf(1) == DecorPlacement.Floor
              && TerrariumLayout.PlacementOf(2) == DecorPlacement.Wall && TerrariumLayout.PlacementOf(3) == DecorPlacement.Wall
              && TerrariumLayout.PlacementOf(4) == DecorPlacement.Floor && TerrariumLayout.PlacementOf(5) == DecorPlacement.Floor
              && TerrariumLayout.PlacementOf(6) == DecorPlacement.Wall,
              "꾸미기 칸: 0·1·4·5 바닥, 2·3·6 뒷벽 (예전 칸 번호 그대로)");
        Check(TerrariumLayout.SlotCount == 7 && TerrariumLayout.CountOf(DecorPlacement.Floor) == 4
              && TerrariumLayout.CountOf(DecorPlacement.Wall) == 3, "꾸미기 칸: 바닥 4 · 뒷벽 3");
        Check(TerrariumLayout.DefaultAnchor(0).x < 0f && TerrariumLayout.DefaultAnchor(1).x > 0f
              && Mathf.Approximately(TerrariumLayout.DefaultAnchor(0).x, -TerrariumLayout.DefaultAnchor(1).x)
              && TerrariumLayout.DefaultAnchor(2).x < 0f && TerrariumLayout.DefaultAnchor(3).x > 0f,
              "꾸미기 칸: 기존 기본 자리가 왼쪽·오른쪽으로 나뉘어 한곳에 겹치지 않는다");
        bool gapsOk = true, rangeOk = true;
        var rangeRock = DecorItem("range_rock", DecorPlacement.Floor, DecorUse.Hide, 0);   // 가장 큰 바닥 장식 기준
        var rangeCork = DecorItem("range_cork", DecorPlacement.Wall,  DecorUse.ClimbPanel, 0);
        for (int a = 0; a < TerrariumLayout.SlotCount; a++)
        {
            var pa = TerrariumLayout.DefaultAnchor(a);
            var kind = TerrariumLayout.PlacementOf(a);
            rangeOk &= TerrariumLayout.ClampAnchor(kind == DecorPlacement.Floor ? rangeRock : rangeCork, pa, 1080f) == pa;
            for (int b = a + 1; b < TerrariumLayout.SlotCount; b++)
                if (TerrariumLayout.PlacementOf(b) == kind)
                    gapsOk &= !TerrariumLayout.TooClose(kind, pa, TerrariumLayout.DefaultAnchor(b));
        }
        Check(gapsOk, "꾸미기 칸: 같은 종류 칸의 기본 자리끼리는 모두 최소 간격보다 멀다");
        Check(rangeOk, "꾸미기 칸: 모든 기본 자리가 옮길 수 있는 범위 안 (집 크기 기준)");
        UnityEngine.Object.DestroyImmediate(rangeRock);
        UnityEngine.Object.DestroyImmediate(rangeCork);

        // 나뭇가지는 화면 가운데 쪽으로 뻗는다 — 오른쪽에 두면 그림을 좌우로 뒤집는다
        var branchItem = DecorItem("decor_branch", DecorPlacement.Wall, DecorUse.Branch, 80);
        TerrariumLayout.ImagePlacement(branchItem, new Vector2(-290f, 0f), out var leftPos, out _, out bool leftFlip);
        TerrariumLayout.ImagePlacement(branchItem, new Vector2(290f, 0f), out var rightPos, out _, out bool rightFlip);
        Check(!leftFlip && rightFlip && Mathf.Approximately(leftPos.x, -rightPos.x)
              && Mathf.Approximately(leftPos.y, TerrariumLayout.WALL_Y),
              "나뭇가지: 왼쪽은 그대로·오른쪽은 좌우 반전, 벽 높이에 놓인다");

        // 경로 방향 — 머리가 가는 쪽을 향한다
        Check(Mathf.Approximately(GeckoMovementAI.SegmentAngle(Vector2.up, true), 90f)
              && Mathf.Approximately(GeckoMovementAI.SegmentAngle(Vector2.up, false), -90f)
              && Mathf.Approximately(GeckoMovementAI.SegmentAngle(Vector2.down, true), -90f)
              && Mathf.Approximately(GeckoMovementAI.SegmentAngle(Vector2.down, false), 90f)
              && Mathf.Approximately(GeckoMovementAI.SegmentAngle(Vector2.right, true), 0f)
              && Mathf.Approximately(GeckoMovementAI.SegmentAngle(Vector2.left, false), 0f),
              "경로: 오르면 머리가 위, 내려오면 머리가 아래, 가로로 가면 눕는다 (좌우 방향 모두)");

        // 빈 유리벽 — 가까운 쪽 벽 앞에서 오른다 (가운데서 오르면 붙을 곳이 없다)
        Check(GeckoMovementAI.ClimbWallIsLeft(-300f, -500f, 500f)
              && !GeckoMovementAI.ClimbWallIsLeft(300f, -500f, 500f)
              && GeckoMovementAI.ClimbWallIsLeft(0f, -500f, 500f)
              && Mathf.Approximately(GeckoMovementAI.WallClimbX(true, -500f, 500f, 120f), -380f)
              && Mathf.Approximately(GeckoMovementAI.WallClimbX(false, -500f, 500f, 120f), 380f)
              && Mathf.Approximately(GeckoMovementAI.WallClimbX(true, -100f, 100f, 500f), 100f),
              "벽 타기: 가까운 쪽 유리벽 앞(끝에서 여백만큼 안쪽)에서 오른다");

        // 발은 벽을 향한다 — 오르내리는 동안 각도가 같고 그림만 좌우로 뒤집힌다
        float wallL = GeckoMovementAI.WallAngle(true), wallR = GeckoMovementAI.WallAngle(false);
        Check(Mathf.Approximately(GeckoMovementAI.FootDirection(wallL).x, -1f)
              && Mathf.Approximately(GeckoMovementAI.FootDirection(wallR).x, 1f)
              && Mathf.Approximately(GeckoMovementAI.SegmentAngle(Vector2.up, false), wallL)      // 왼쪽 벽: 왼쪽을 보고 오른다
              && Mathf.Approximately(GeckoMovementAI.SegmentAngle(Vector2.down, true), wallL)     // 같은 벽에서 머리만 아래로
              && Mathf.Approximately(GeckoMovementAI.SegmentAngle(Vector2.up, true), wallR)
              && Mathf.Approximately(GeckoMovementAI.SegmentAngle(Vector2.down, false), wallR),
              "벽 타기: 오르내려도 발이 짚은 벽은 그대로 (0°를 지나 뒤집히지 않는다)");

        // 내려오는 도중 다시 내려오라고 해도 꼭대기로 되올라가지 않는다 (2026-09-20)
        var route = new List<Vector2> { new Vector2(0f, 400f), new Vector2(0f, 900f), new Vector2(0f, 1400f) };
        GeckoMovementAI.TrimRouteAbove(route, 950f);   // 1400까지 올랐다가 950까지 내려온 상태
        Check(route.Count == 2 && Mathf.Approximately(route[1].y, 900f),
              "벽 내려오기: 이미 지나친 위쪽 점은 버린다 (되올라가지 않는다)");
        GeckoMovementAI.TrimRouteAbove(route, 100f);
        Check(route.Count == 1 && Mathf.Approximately(route[0].y, 400f),
              "벽 내려오기: 바닥 출발점은 남는다 (내려설 자리)");

        // 나뭇가지 경로 — 밑동(바닥 범위 안)에서 대각선으로 올라가 위쪽 가로 부분, 오른쪽 칸은 좌우 대칭
        var left  = TerrariumLayout.ClimbPath(DecorUse.Branch, TerrariumLayout.DefaultAnchor(2), 5000f, 1f);
        var right = TerrariumLayout.ClimbPath(DecorUse.Branch, TerrariumLayout.DefaultAnchor(3), 5000f, 1f);
        Check(left != null && left.Length == 3 && left[1].y > left[0].y + 200f && Mathf.Approximately(left[2].y, left[1].y)
              && left[0].y >= 380f && left[0].y <= 950f && left[1].x > left[0].x
              && Mathf.Approximately(left[0].x, TerrariumLayout.DefaultAnchor(2).x),
              "나뭇가지: 밑동(놓인 위치)에서 가운데 쪽 대각선으로 오른 뒤 가로로 걷는다");
        Check(right != null && Mathf.Approximately(right[0].x, -left[0].x) && Mathf.Approximately(right[2].x, -left[2].x),
              "나뭇가지: 오른쪽 칸은 왼쪽과 좌우 대칭");
        var panel = TerrariumLayout.ClimbPath(DecorUse.ClimbPanel, new Vector2(-150f, 0f), 5000f, 1f);
        Check(panel != null && Mathf.Approximately(panel[0].x, -150f) && Mathf.Approximately(panel[1].x, -150f)
              && panel[1].y > panel[0].y && panel[1].y <= TerrariumLayout.WALL_Y + TerrariumLayout.ImageSize(DecorUse.ClimbPanel).y,
              "코르크 뒤판: 옮긴 위치에서 판 안으로 곧게 오른다");
        Check(TerrariumLayout.ClimbPath(DecorUse.Vine, TerrariumLayout.DefaultAnchor(3), TerrariumLayout.WALL_Y + 50f, 1f) == null,
              "오를 높이가 없으면 구조물을 타지 않는다");

        // 칸 종류 · 예전 저장 정리
        var (repo, _, _, _) = Fresh();
        var terrarium = new TerrariumManager(repo);
        var rock  = DecorItem("decor_rock",  DecorPlacement.Floor, DecorUse.None,        0);
        var plant = DecorItem("decor_plant", DecorPlacement.Floor, DecorUse.None,       50);
        var hide  = DecorItem("decor_hide",  DecorPlacement.Floor, DecorUse.Hide,       50);
        var cork  = DecorItem("decor_cork",  DecorPlacement.Wall,  DecorUse.ClimbPanel, 60);
        DecorItemSO FindItem(string id)
        {
            foreach (var d in new[] { rock, plant, hide, cork })
                if (d.itemId == id) return d;
            return null;
        }

        var t = terrarium.GetData();
        t.decorSlots = new string[] { null, null, null, null };
        Check(terrarium.FindEmptySlot(rock) == 0 && terrarium.FindEmptySlot(cork) == 2,
              "빈 칸 찾기: 바닥 장식은 바닥 칸, 벽 구조물은 벽 칸");

        t.decorSlots = new string[] { null, null, "decor_rock", "decor_cork" };
        int refund = terrarium.NormalizeSlots(FindItem);
        Check(refund == 0 && t.decorSlots[0] == "decor_rock" && t.decorSlots[2] == null && t.decorSlots[3] == "decor_cork",
              "예전 저장: 벽 칸에 있던 바위는 빈 바닥 칸으로 옮긴다 (뒤판은 그대로)");

        int coin0 = repo.GetPlayerData().coin;
        t.decorSlots = new string[] { "decor_plant", "decor_rock", "decor_hide", null };   // 칸이 4개이던 저장
        refund = terrarium.NormalizeSlots(FindItem);
        Check(refund == 0 && t.decorSlots.Length == TerrariumLayout.SlotCount && t.decorSlots[2] == null
              && t.decorSlots[4] == "decor_hide" && t.decorSlots[0] == "decor_plant",
              "예전 저장(칸 4개): 칸이 늘어 벽 칸의 집은 새 바닥 칸으로 옮긴다");

        coin0 = repo.GetPlayerData().coin;
        t.decorSlots = new string[] { "decor_plant", "decor_rock", "decor_hide", null, "decor_rock", "decor_plant", null };
        refund = terrarium.NormalizeSlots(FindItem);
        Check(refund == 50 && repo.GetPlayerData().coin == coin0 + 50 && t.decorSlots[2] == null
              && t.decorSlots[0] == "decor_plant" && t.decorSlots[1] == "decor_rock",
              "예전 저장: 바닥 칸 4개가 가득 차 옮길 수 없으면 빼고 값을 돌려준다 (코인 +50)");

        t.decorSlots = new string[] { "decor_rock", "unknown_item", null };   // 길이가 다른 손상 저장
        terrarium.NormalizeSlots(FindItem);
        Check(t.decorSlots.Length == TerrariumLayout.SlotCount && t.decorSlots[0] == "decor_rock" && t.decorSlots[1] == "unknown_item",
              "칸 배열 길이를 칸 수에 맞추고, 모르는 장식은 그대로 둔다");

        // 옮긴 위치 — 저장 · 높이 맞춤 · 빈 칸 무시 · 장식을 바꾸면 기본 자리
        t.decorSlots     = new string[] { "decor_rock", null, "decor_cork", null };
        t.decorPositions = new Vector2[TerrariumLayout.SlotCount];
        Check(TerrariumLayout.AnchorOf(t, 0) == TerrariumLayout.DefaultAnchor(0), "옮기지 않은 장식은 기본 자리");
        terrarium.SetDecorPosition(0, new Vector2(-120f, 9999f));
        terrarium.SetDecorPosition(2, new Vector2(80f, 0f));
        Check(TerrariumLayout.AnchorOf(t, 0) == new Vector2(-120f, TerrariumLayout.FLOOR_MAX_Y)
              && TerrariumLayout.AnchorOf(t, 2) == new Vector2(80f, TerrariumLayout.WALL_Y),
              "옮긴 위치를 저장한다 (바닥은 높이 범위 안, 벽은 벽 높이)");
        terrarium.SetDecorPosition(1, new Vector2(100f, 500f));
        Check(t.decorPositions[1] == Vector2.zero, "빈 칸의 위치는 저장하지 않는다");
        var reloaded = new SaveManager(SAVE_STEM).Load().terrarium;
        Check(TerrariumLayout.AnchorOf(reloaded, 2) == new Vector2(80f, TerrariumLayout.WALL_Y), "옮긴 위치가 저장 파일에 남는다");
        terrarium.SetDecor(0, "decor_plant");
        Check(TerrariumLayout.AnchorOf(t, 0) == TerrariumLayout.DefaultAnchor(0), "칸의 장식을 바꾸면 기본 자리로 돌아간다");

        // 옮길 수 있는 범위 (화면 폭 1080 기준)
        Vector2 near = TerrariumLayout.ClampAnchor(rock, new Vector2(-5000f, 0f), 1080f);
        Check(Mathf.Approximately(near.x, -(540f - 150f - TerrariumLayout.EDGE_MARGIN)) && Mathf.Approximately(near.y, TerrariumLayout.FLOOR_MIN_Y),
              "바닥 장식: 그림이 화면 밖으로 나가지 않고, 게코가 다니는 바닥 안");
        Vector2 back = TerrariumLayout.ClampAnchor(rock, new Vector2(-5000f, 9999f), 1080f, _ => 0.6f);
        Check(back.x < near.x && Mathf.Approximately(back.y, TerrariumLayout.FLOOR_MAX_Y),
              "바닥 장식: 뒤로 가 작아지면 화면 끝에 더 붙을 수 있고, 뒷벽 구조물보다 앞");
        Vector2 wall = TerrariumLayout.ClampAnchor(cork, new Vector2(5000f, 300f), 1080f);
        Check(Mathf.Approximately(wall.x, 540f - 190f - TerrariumLayout.EDGE_MARGIN) && Mathf.Approximately(wall.y, TerrariumLayout.WALL_Y),
              "벽 구조물: 좌우로만 움직이고 그림이 화면 안");
        Check(TerrariumLayout.ClampAnchor(branchItem, new Vector2(-5000f, 0f), 1080f).x >= -TerrariumLayout.BRANCH_MAX_X - 0.01f,
              "나뭇가지: 밑동이 게코가 올라설 수 있는 범위 안");
        Check(TerrariumLayout.TooClose(DecorPlacement.Floor, new Vector2(0f, 600f), new Vector2(150f, 600f))
              && !TerrariumLayout.TooClose(DecorPlacement.Floor, new Vector2(0f, 600f), new Vector2(300f, 600f))
              && TerrariumLayout.TooClose(DecorPlacement.Wall, new Vector2(0f, 760f), new Vector2(150f, 760f)),
              "장식끼리 너무 붙으면 더 옮겨지지 않는다 (바닥 거리 220 · 벽 가로 200)");

        // 실제 장식 에셋 — 놓는 곳·쓰임·그림·이름 번역
        var expected = new[]
        {
            ("decor_hide",   DecorPlacement.Floor, DecorUse.Hide),
            ("decor_rock",   DecorPlacement.Floor, DecorUse.None),
            ("decor_plant",  DecorPlacement.Floor, DecorUse.None),
            ("decor_cork",   DecorPlacement.Wall,  DecorUse.ClimbPanel),
            ("decor_vine",   DecorPlacement.Wall,  DecorUse.Vine),
            ("decor_branch", DecorPlacement.Wall,  DecorUse.Branch),
        };
        foreach (var (id, placement, use) in expected)
        {
            var item = Resources.Load<DecorItemSO>("Decor/" + id);
            Check(item != null && item.category == DecorCategory.Decoration && item.placement == placement && item.use == use
                  && (item.previewSprite != null || item.icon != null) && Loc.TryGetPair("decor." + id, out _, out _),
                  $"장식 에셋 {id}: {placement} · {use} · 그림 · 이름 번역");
        }
    }

    private static DecorItemSO DecorItem(string id, DecorPlacement placement, DecorUse use, int coin)
    {
        var item = ScriptableObject.CreateInstance<DecorItemSO>();
        item.itemId    = id;
        item.category  = DecorCategory.Decoration;
        item.placement = placement;
        item.use       = use;
        item.coinPrice = coin;
        return item;
    }

    private static GeckoTouchZone ZoneOfPart(GeckoRig rig, GeckoPartId part, float u, float v)
        => GeckoTouch.ZoneAt(rig, rig.PartWorldPoint(part, new Vector2(u, v)));

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

    // 머리 움직임 (2026-09-21) — 둘러보기·바라보기는 목 이음새가 드러나지 않는 ±8° 안
    private static void TestHeadMotion()
    {
        Check(GeckoMotor.HEAD_LIMIT <= 8f, "머리: 평소 움직임은 ±8° 안 (넘으면 턱 밑 목선·등 돌기 겹침이 드러난다)");

        var neck = new Vector2(155f, 366f);
        Check(Mathf.Approximately(GeckoMotor.HeadAngleToward(neck, neck + new Vector2(100f, 1000f), out bool up), GeckoMotor.HEAD_LIMIT) && !up,
              "머리: 앞 위쪽을 보면 한계(+8°)까지만 든다");
        float low = GeckoMotor.HeadAngleToward(neck, neck + new Vector2(400f, -20f), out bool ahead);
        Check(low < 0f && low > -5f && !ahead, $"머리: 앞쪽 조금 아래는 살짝 숙인다 ({low:F1}°)");
        GeckoMotor.HeadAngleToward(neck, neck + new Vector2(-50f, 0f), out bool behind);
        Check(behind, "머리: 뒤쪽은 고개로 못 돌린다 (눈으로 흘끗)");

        // 누른 곳 바라보기 — 화면 좌표를 스킨 좌표로 되돌릴 때 좌우 반전·벽 회전까지 맞아야 한다
        var skin = SceneSkin();
        if (skin == null)
        {
            Check(false, "게코 스킨이 없어 머리 바라보기를 확인하지 못함");
            return;
        }
        var go = new GameObject("HeadTestGecko", typeof(RectTransform));
        try
        {
            var rig = go.AddComponent<GeckoRig>();
            rig.SetSkin(skin, useStageSkins: false);
            rig.SetGrowthStage(4, immediate: true);

            bool roundTrip = true;
            foreach (var (right, angle) in new[] { (true, 0f), (false, 0f), (true, 90f), (false, -90f) })
            {
                go.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                rig.SetFacing(right);
                rig.SolveRest();
                var p = new Vector2(500f, 400f);
                roundTrip &= (rig.WorldToSkin(rig.SkinToWorld(p)) - p).magnitude < 0.5f;
            }
            Check(roundTrip, "머리: 화면 → 스킨 좌표가 좌우 반전·벽 회전(±90°)에서도 되돌아온다");

            // 왼쪽을 보는 게코 — 화면 왼쪽(= 게코 앞)을 누르면 앞쪽으로 본다
            go.transform.localRotation = Quaternion.identity;
            rig.SetFacing(false);
            rig.SolveRest();
            Vector2 neckSkin  = rig.RestPosition(GeckoPartId.Head);
            Vector3 neckWorld = rig.SkinToWorld(neckSkin);
            GeckoMotor.HeadAngleToward(neckSkin, rig.WorldToSkin(neckWorld + new Vector3(-50f, 10f, 0f)), out bool leftBehind);
            GeckoMotor.HeadAngleToward(neckSkin, rig.WorldToSkin(neckWorld + new Vector3( 50f, 10f, 0f)), out bool rightBehind);
            Check(!leftBehind && rightBehind, "머리: 왼쪽을 볼 때는 화면 왼쪽이 앞, 오른쪽이 뒤");

            Check(rig.GetComponentInChildren<GeckoNeckBend>(true) != null, "목 휨: 게코 머리 그림에 GeckoNeckBend가 붙는다");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }

        // 목 휨 (2026-09-21) — 목 쪽은 몸통에 붙어 있고, 얼굴(눈 53%)부터는 머리 각도 그대로
        Check(GeckoNeckBend.FollowAt(0f) == 0f && GeckoNeckBend.FollowAt(GeckoNeckBend.NECK_START) == 0f
              && Mathf.Approximately(GeckoNeckBend.FollowAt(GeckoNeckBend.FACE_START), 1f) && Mathf.Approximately(GeckoNeckBend.FollowAt(0.53f), 1f),
              "목 휨: 목 쪽 12%까지는 몸통 그대로, 46%(눈 53% 앞)부터는 머리 각도 그대로");
        float midFollow = GeckoNeckBend.FollowAt((GeckoNeckBend.NECK_START + GeckoNeckBend.FACE_START) * 0.5f);
        Check(midFollow > 0.4f && midFollow < 0.6f, $"목 휨: 그 사이는 부드럽게 ({midFollow:F2})");

        // 실제로 그림을 휜다 — 머리가 14° 돌았을 때 목 쪽 끝은 -14°로 되돌려져 몸통 각도, 주둥이 쪽은 그대로
        var headGo = new GameObject("NeckBendTest", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
        try
        {
            var bend = headGo.AddComponent<GeckoNeckBend>();
            bend.SetAngle(14f);
            var vh = new UnityEngine.UI.VertexHelper();
            var c  = new Color32(255, 255, 255, 255);
            vh.AddVert(new Vector3(-30f, -80f), c, new Vector2(0f, 0f));
            vh.AddVert(new Vector3(-30f, 120f), c, new Vector2(0f, 1f));
            vh.AddVert(new Vector3(370f, 120f), c, new Vector2(1f, 1f));
            vh.AddVert(new Vector3(370f, -80f), c, new Vector2(1f, 0f));
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
            bend.ModifyMesh(vh);

            // 칸마다 꼭짓점 3개 (아래 · 목 피부 위 · 위)
            int cols = vh.currentVertCount / 3 - 1;
            UIVertex back = default, front = default, throatBottom = default, throatTop = default, chinBottom = default;
            vh.PopulateUIVertex(ref back, 2);                        // 목 쪽 끝 위
            vh.PopulateUIVertex(ref front, vh.currentVertCount - 1); // 주둥이 쪽 끝 위
            int mid = cols / 2;                                      // 가로 50% — 얼굴이 머리 각도대로 도는 곳의 목 아래
            vh.PopulateUIVertex(ref throatBottom, mid * 3);
            vh.PopulateUIVertex(ref throatTop, mid * 3 + 1);
            vh.PopulateUIVertex(ref chinBottom, cols * 3);           // 턱 끝 아래
            var undo = Quaternion.Euler(0f, 0f, -14f);
            float x50 = Mathf.Lerp(-30f, 370f, mid / (float)cols), yThroat = Mathf.Lerp(-80f, 120f, GeckoNeckBend.THROAT);
            Check(cols >= 12 && (back.position - undo * new Vector3(-30f, 120f)).magnitude < 0.01f
                  && (front.position - new Vector3(370f, 120f)).magnitude < 0.01f,
                  $"목 휨: 그림이 {cols}칸으로 나뉘어 목 쪽만 되돌려진다");
            Check((throatBottom.position - undo * new Vector3(x50, -80f)).magnitude < 0.01f
                  && (throatTop.position - new Vector3(x50, yThroat)).magnitude < 0.01f,
                  "목 피부: 고개를 들면 턱 밑 아래 가장자리는 몸통에 붙어 있고 그 위만 따라 올라간다 (늘어남)");
            Check((chinBottom.position - new Vector3(370f, -80f)).magnitude < 0.01f, "목 피부: 턱 끝은 붙이지 않는다 (윤곽이 늘어져 번지지 않게)");

            bend.SetAngle(-14f);
            vh.Clear();
            vh.AddVert(new Vector3(-30f, -80f), c, new Vector2(0f, 0f));
            vh.AddVert(new Vector3(-30f, 120f), c, new Vector2(0f, 1f));
            vh.AddVert(new Vector3(370f, 120f), c, new Vector2(1f, 1f));
            vh.AddVert(new Vector3(370f, -80f), c, new Vector2(1f, 0f));
            bend.ModifyMesh(vh);
            vh.PopulateUIVertex(ref throatBottom, mid * 3);
            Check((throatBottom.position - new Vector3(x50, -80f)).magnitude < 0.01f, "목 피부: 고개를 숙일 때는 붙이지 않는다 (줄어들며 뒤집히지 않게)");
            vh.Dispose();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(headGo);
        }
    }

    // 은신처 문으로 들어가기 (2026-09-21)
    private static void TestHideDoor()
    {
        // 에셋 — 동굴은 문으로 들어가고, 문이 게코보다 작은 집은 문 정보를 비워 뒤에 숨는다 (2026-09-21 둘째)
        var cave = DecorCatalog.Find("decor_cave");
        var cr   = cave != null ? cave.doorRect : default;
        Check(cave != null && cr.width > 0.5f && cr.height > 0.5f && cr.xMin >= 0f && cr.xMax <= 1f && cr.yMax <= 1f,
              $"은신처 문: 동굴 입구가 다 자란 게코가 들어갈 만큼 크다 ({cr.xMin:F2}~{cr.xMax:F2}, {cr.yMin:F2}~{cr.yMax:F2})");
        // 입구 높이 = 그림 460 × 비율 — 다 자란 게코 키 234 (고개를 6° 숙인다)
        Check(cr.height * 460f >= 234f, $"은신처 문: 동굴 입구 높이 {cr.height * 460f:F0} ≥ 게코 키 234 — 크기를 바꾸지 않고 들어간다");
        var hide = DecorCatalog.Find("decor_hide");
        Check(hide != null && hide.doorRect.width <= 0f, "은신처 문: 집은 문이 작아(게코 키의 35%) 문 정보를 비운다 — 뒤에 숨는다");

        // 동굴처럼 가운데 큰 문 — 화면 오른쪽 절반(200)이면 가운데 쪽(왼쪽)에서 들어간다
        const float FRONT = 254f, REAR = 386f;   // 다 자란 게코
        var door = Rect.MinMaxRect(60f, 46f, 340f, 321f);
        var p = GeckoMovementAI.PlanDoor(door, 200f, 460f, FRONT, REAR, -400f, 400f, 0.68f);
        float inside = (p.insideX + FRONT - p.edgeX) / (FRONT + REAR);
        Check(p.insideRight && Mathf.Approximately(p.edgeX, door.xMin) && Mathf.Abs(p.standX + FRONT + 8f - door.xMin) < 0.01f,
              "은신처 문: 가운데 문은 화면 가운데 쪽에서 — 주둥이가 문 가장자리 바로 앞에 선다");
        Check(Mathf.Abs(inside - 0.68f) < 0.01f, $"은신처 문: 다 들어가면 몸의 68%가 문 안 — 꼬리 쪽은 밖, 크기는 그대로 ({inside:P0})");

        // 문이 한쪽에 치우친 은신처 — 그쪽 바깥에서, 설 자리가 없으면 반대쪽에서
        var side = Rect.MinMaxRect(-372.7f, 30f, -276.1f, 113f);
        var ps   = GeckoMovementAI.PlanDoor(side, -300f, 460f, FRONT, REAR, -400f, 400f, 0.68f);
        Check(!ps.insideRight && Mathf.Approximately(ps.edgeX, side.xMax), "은신처 문: 들어갈 쪽에 설 자리가 없으면 반대쪽에서");
        var psOpen = GeckoMovementAI.PlanDoor(side, -300f, 460f, FRONT, REAR, -900f, 400f, 0.68f);
        Check(psOpen.insideRight, "은신처 문: 문이 왼쪽에 치우친 은신처는 왼쪽에서 들어간다");
    }

    // ── 도구 ──────────────────────────────────────────────────

    // 장식 효과 (2026-09-21) — 놓여 있기만 하면 생긴다 (DecorPerks)
    private static void TestDecorPerks()
    {
        // 에셋마다 맞는 효과가 붙어 있다
        var expect = new (string id, DecorPerk perk)[]
        {
            ("decor_moss_rock", DecorPerk.MoltRub), ("decor_plant", DecorPerk.Droplets), ("decor_rock", DecorPerk.Basking),
            ("decor_hide", DecorPerk.Shelter), ("decor_cave", DecorPerk.Shelter),
            ("decor_cork", DecorPerk.Play), ("decor_vine", DecorPerk.Play), ("decor_branch", DecorPerk.Play), ("decor_driftwood", DecorPerk.Play),
        };
        var wrong = new StringBuilder();
        foreach (var (id, perk) in expect)
        {
            var item = DecorCatalog.Find(id);
            if (item == null || item.perk != perk) wrong.Append(id).Append(' ');
        }
        Check(wrong.Length == 0, wrong.Length == 0 ? "장식 효과: 에셋 9종에 맞는 효과가 붙어 있다" : "장식 효과가 틀린 에셋: " + wrong);

        // 맞는 칸에 놓여 있어야 효과 — 바닥 장식을 벽 칸에 두면 없다, 비어 있으면 없다
        var t = new TerrariumData();
        Check(!DecorPerks.Has(t, DecorPerk.MoltRub), "장식 효과: 아무것도 없으면 효과 없음");
        t.decorSlots[0] = "decor_moss_rock";
        Check(DecorPerks.Has(t, DecorPerk.MoltRub) && !DecorPerks.Has(t, DecorPerk.Droplets), "장식 효과: 이끼 바위를 바닥 칸에 두면 허물 효과만");
        t.decorSlots[0] = null;
        t.decorSlots[2] = "decor_moss_rock";   // 뒷벽 칸
        Check(!DecorPerks.Has(t, DecorPerk.MoltRub), "장식 효과: 칸 종류가 안 맞으면 효과 없음");

        // 게임 규칙에 들어간다 — 같은 조건에서 장식만 바꿔 비교
        var (repo, gecko, _, g) = Fresh();
        var slots = repo.GetPlayerData().terrarium.decorSlots;

        g.moltBonus = 0f; g.thirst = 30f; g.health = 30f;
        float rateBare = gecko.MoltSuccessRate(g);
        slots[0] = "decor_moss_rock";
        Check(Mathf.Abs(gecko.MoltSuccessRate(g) - rateBare - DecorPerks.MOLT_RUB_BONUS) < 0.001f,
              $"장식 효과: 이끼 바위 → 허물 성공률 +10% ({rateBare:P0} → {gecko.MoltSuccessRate(g):P0})");
        slots[0] = null;

        g.thirst = 20f;
        gecko.GiveWater(g.id);
        float bareWater = g.thirst;
        g.thirst = 20f;
        slots[1] = "decor_plant";
        gecko.GiveWater(g.id);
        Check(Mathf.Approximately(g.thirst - bareWater, DecorPerks.DROPLET_WATER_BONUS),
              $"장식 효과: 화분 → 물 줄 때 목마름 +10 더 ({bareWater:F0} → {g.thirst:F0})");
        slots[1] = null;

        // 시간 보정 — 은신처는 기분이 덜 떨어지고, 바위는 건강이 더 빨리 오른다 (앱을 꺼 둔 동안에도)
        void Elapse(float hours)
        {
            g.hunger = g.thirst = g.cleanliness = 100f;
            g.lastUpdatedTicks = DateTime.UtcNow.AddHours(-hours).Ticks;
            gecko.ApplyElapsedProgressAll();
        }
        g.mood = 100f; Elapse(10f); float moodBare = g.mood;
        slots[0] = "decor_hide";
        g.mood = 100f; Elapse(10f);
        Check(Mathf.Abs((100f - g.mood) - (100f - moodBare) * DecorPerks.SHELTER_MOOD_MUL) < 0.05f,
              $"장식 효과: 은신처 → 기분이 20% 덜 떨어진다 (10시간 -{100f - moodBare:F1} → -{100f - g.mood:F1})");
        slots[0] = null;

        g.health = 10f; Elapse(5f); float healBare = g.health - 10f;
        slots[0] = "decor_rock";
        g.health = 10f; Elapse(5f);
        Check(Mathf.Abs((g.health - 10f) - (healBare + DecorPerks.BASK_HEALTH_REGEN * 5f)) < 0.05f,
              $"장식 효과: 바위 → 건강 회복 +50% (5시간 +{healBare:F2} → +{g.health - 10f:F2})");
        slots[0] = null;

        // 쓰다듬기 — 놀 거리(벽 구조물)가 있으면 애정도 +1
        g.affection = 10f;
        gecko.Pet(g.id);
        float petBare = g.affection - 10f;
        slots[2] = "decor_cork";
        g.affection = 10f;
        gecko.Pet(g.id);
        Check(Mathf.Approximately((g.affection - 10f) - petBare, DecorPerks.PLAY_PET_AFFECTION),
              $"장식 효과: 코르크 뒤판 → 쓰다듬기 애정도 +1 ({petBare:F0} → {g.affection - 10f:F0})");
        slots[2] = null;

        // 꾸미기 카드 한 줄 — 수치가 DecorPerks에서 그대로 온다, 효과 없으면 줄 없음
        Check(DecorSlotUI.PerkLabel(DecorPerk.MoltRub).Contains("10") && DecorSlotUI.PerkLabel(DecorPerk.Basking).Contains("50")
              && DecorSlotUI.PerkLabel(DecorPerk.Shelter).Contains("20") && DecorSlotUI.PerkLabel(DecorPerk.None) == null,
              $"장식 효과: 카드 문구 \"{DecorSlotUI.PerkLabel(DecorPerk.MoltRub)}\" · \"{DecorSlotUI.PerkLabel(DecorPerk.Basking)}\" · \"{DecorSlotUI.PerkLabel(DecorPerk.Shelter)}\"");

        // 찾아가서 설 자리 — 화면 가운데 쪽이 먼저, 막히면 바깥쪽
        Check(Mathf.Approximately(GeckoMovementAI.VisitX(-300f, 200f, -400f, 400f), -100f)
              && Mathf.Approximately(GeckoMovementAI.VisitX(300f, 200f, -400f, 400f), 100f),
              "장식 찾아가기: 장식의 화면 가운데 쪽에 선다");
        Check(Mathf.Approximately(GeckoMovementAI.VisitX(-50f, 300f, -200f, 200f), 200f),   // 250·-350 둘 다 범위 밖
              "장식 찾아가기: 양쪽 다 못 서면 다닐 수 있는 끝에 선다");
        Check(Mathf.Approximately(GeckoMovementAI.VisitX(-100f, 200f, -400f, 50f), -300f),   // 가운데 쪽 100은 범위 밖
              "장식 찾아가기: 가운데 쪽이 막히면 바깥쪽에 선다");
    }

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

    private static GeckoSpeciesSO Species(string id, int coin, bool free)
    {
        var s = ScriptableObject.CreateInstance<GeckoSpeciesSO>();
        s.speciesId           = id;
        s.displayName         = id;
        s.coinPrice           = coin;
        s.isUnlockedByDefault = free;
        return s;
    }

    private static DecorItemSO Decor(string id, DecorCategory category, int coin)
    {
        var item = ScriptableObject.CreateInstance<DecorItemSO>();
        item.itemId = id;
        item.category = category;
        item.coinPrice = coin;
        return item;
    }

    /// <summary>
    /// 전신 그림 스킨 (2026-09-22) — 그림 한 장이 게코 전체, 나머지 파츠는 보이지 않는 판정 자리.
    /// 크레스티드(하코)가 이 그림으로 나오므로 부위 판정도 이 그림으로 한 번 더 본다
    /// </summary>
    private static void TestWholeBodySkin()
    {
        // 휘는 영역 — 표의 숫자는 GeckoChildArt와 같은 그림 픽셀 (1460×883, 왼쪽 위 0,0)
        const float W = 1460f, H = 883f;
        Vector2 Uv(float x, float y) => new Vector2(x / W, (H - y) / H);
        var headZone = new Vector4(964f / W, 1110f / W, (H - 477f) / H, (H - 388f) / H);
        var tailZone = new Vector2(482f / W, 321f / W);
        Check(GeckoWholeBend.HeadWeight(Uv(1195f, 200f), headZone) > 0.99f && GeckoWholeBend.HeadWeight(Uv(1420f, 268f), headZone) > 0.99f,
              "전신 그림: 눈·입은 머리 각도를 그대로 따른다");
        Check(GeckoWholeBend.HeadWeight(Uv(1005f, 700f), headZone) < 0.01f && GeckoWholeBend.HeadWeight(Uv(1215f, 700f), headZone) < 0.01f
              && GeckoWholeBend.HeadWeight(Uv(1180f, 520f), headZone) < 0.01f,
              "전신 그림: 머리 아래 앞다리·가슴은 머리를 따라 돌지 않는다");
        Check(GeckoWholeBend.TailWeight(Uv(150f, 800f), tailZone) > 0.99f && GeckoWholeBend.TailWeight(Uv(470f, 850f), tailZone) < 0.02f,
              "전신 그림: 꼬리 끝은 꼬리를 따르고, 뒷발 발끝은 따르지 않는다");

        var skin    = AssetDatabase.LoadAssetAtPath<GeckoSkin>("Assets/_Game/GeckoSkins/GeckoSkin_Child.asset");
        var species = AssetDatabase.LoadAssetAtPath<GeckoSpeciesSO>("Assets/_Game/Resources/Species/crested.asset");
        if (skin == null)
        {
            Check(false, "전신 스킨이 없음 — -executeMethod GeckoChildArt.BuildBatch");
            return;
        }
        var bodyArt = skin.GetPart(GeckoPartId.Body);
        Check(skin.wholeBody && bodyArt != null && bodyArt.sprite != null, "전신 스킨: 몸통 파츠에 그림 한 장");
        Check(skin.wholeLegs.Count == 4 && skin.wholeTailChain.Length >= 6, "전신 스킨: 다리 4개 · 꼬리 사슬 뼈대가 있다");
        // 다리 무게 (그림 픽셀, 왼쪽 위 0,0 → 계산은 아래가 0) — 발가락 끝까지 다리를 따르고, 배·관절·꼬리 뿌리는 따르지 않는다
        Vector2 Px(float x, float y) => new Vector2(x, H - y);
        float LegW(GeckoPartId id, float x, float y)
        {
            foreach (var l in skin.wholeLegs)
                if (l.id == id)
                    return GeckoWholeBend.LegWeight(Px(x, y), Vector2.Scale(l.joint, new Vector2(W, H)), Vector2.Scale(l.foot, new Vector2(W, H)),
                                                    l.radius.x * W, l.radius.y * W, 0.035f * W);
            return -1f;
        }
        bool bellyFree = true;
        foreach (var id in GeckoWholeBend.LEGS) bellyFree &= LegW(id, 800f, 560f) == 0f;
        Check(bellyFree && LegW(GeckoPartId.LegFrontNear, 990f, 580f) < 0.01f && LegW(GeckoPartId.LegBackNear, 630f, 610f) < 0.01f,
              "전신 스킨: 배·어깨·엉덩이 자리는 다리를 따르지 않는다 (몸에 붙어 있다)");
        Check(LegW(GeckoPartId.LegBackNear, 470f, 850f) > 0.95f && LegW(GeckoPartId.LegBackNear, 690f, 850f) > 0.95f
              && LegW(GeckoPartId.LegFrontNear, 905f, 840f) > 0.95f && LegW(GeckoPartId.LegFrontNear, 1110f, 835f) > 0.95f,
              "전신 스킨: 가까운 발은 양 끝 발가락까지 다리를 온전히 따른다 (비스듬히 잘리지 않게)");
        Check(LegW(GeckoPartId.LegBackNear, 480f, 670f) < 0.2f, "전신 스킨: 꼬리 뿌리는 뒷다리를 따르지 않는다");
        Check(species != null && species.skin == skin, "전신 스킨: 크레스티드 종의 전용 그림으로 연결됨");

        var go = new GameObject("WholeBodyTestGecko", typeof(RectTransform));
        try
        {
            var rig = go.AddComponent<GeckoRig>();
            var painted = SceneSkin();
            var so = new SerializedObject(rig);
            so.FindProperty("_skin").objectReferenceValue = painted;   // 씬 기본 그림
            so.ApplyModifiedPropertiesWithoutUndo();

            rig.SetSkin(skin, useStageSkins: false);
            rig.SetGrowthStage(4, immediate: true);
            rig.SolveRest();

            UnityEngine.UI.Graphic G(GeckoPartId id)
            {
                var t = rig.Visual.Find(GeckoParts.LayerName(id));
                return t != null ? t.GetComponent<UnityEngine.UI.Graphic>() : null;
            }
            var body = G(GeckoPartId.Body);
            Check(rig.IsWholeBody && body != null && body.enabled
                  && !G(GeckoPartId.Head).enabled && !G(GeckoPartId.EyeL).enabled && !G(GeckoPartId.Mouth).enabled
                  && !G(GeckoPartId.LegFrontNear).enabled && !G(GeckoPartId.Tail).enabled,
                  "전신 그림: 몸통 그림 한 장만 그리고, 머리·눈·입·다리·꼬리는 그리지 않는다");
            Check(rig.WholeBend != null && rig.WholeBend.enabled
                  && body.GetComponent<GeckoWholeBend>() == rig.WholeBend
                  && G(GeckoPartId.Head).GetComponent<GeckoNeckBend>() is var nb && (nb == null || !nb.enabled),
                  "전신 그림: 몸통 그림을 휘고, 목 휨(파츠 스킨용)은 꺼진다");
            float len = rig.FrontReach + rig.RearReach;
            Check(len > 480f && len < 560f, $"전신 그림: 어덜트 길이 {len:0} (520 안팎)");

            foreach (var (facingRight, angle) in new[] { (true, 0f), (false, 0f), (true, 90f), (false, -90f) })
            {
                go.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                rig.SetFacing(facingRight);
                rig.SolveRest();
                string pose = $"전신 그림 {(facingRight ? "오른쪽" : "왼쪽")} {angle:0}도";
                Check(ZoneOfPart(rig, GeckoPartId.EyeL, 0.5f, 0.5f) == GeckoTouchZone.Eye, $"터치({pose}): 눈 → 눈");
                Check(ZoneOfPart(rig, GeckoPartId.Mouth, 0.5f, 0.5f) == GeckoTouchZone.Mouth, $"터치({pose}): 입 → 입");
                Check(ZoneOfPart(rig, GeckoPartId.LegFrontNear, 0.5f, 0.3f) == GeckoTouchZone.FrontLeg, $"터치({pose}): 앞다리 → 앞다리");
                Check(ZoneOfPart(rig, GeckoPartId.LegBackNear, 0.5f, 0.3f) == GeckoTouchZone.BackLeg, $"터치({pose}): 뒷다리 → 뒷다리");
                Check(ZoneOfPart(rig, GeckoPartId.Tail, 0.08f, 0.5f) == GeckoTouchZone.TailTip, $"터치({pose}): 꼬리 끝 → 꼬리 끝");
                Check(ZoneOfPart(rig, GeckoPartId.Tail, 0.9f, 0.5f) == GeckoTouchZone.TailBase, $"터치({pose}): 꼬리 뿌리 → 꼬리 뿌리");
                Check(ZoneOfPart(rig, GeckoPartId.Body, 800f / W, (H - 560f) / H) == GeckoTouchZone.Body, $"터치({pose}): 몸통 → 몸통");
                Check(ZoneOfPart(rig, GeckoPartId.Head, 0.2f, 0.8f) == GeckoTouchZone.Head
                      && ZoneOfPart(rig, GeckoPartId.Head, 0.5f, 0.92f) == GeckoTouchZone.Head, $"터치({pose}): 머리(볏·정수리) → 머리");
            }

            // 뼈대로 휘기 — 발·꼬리 끝·주둥이가 눈에 띄게 움직이고, 몸통은 제자리 (첫 판은 다리가 안 움직이고 꼬리 물결이 상쇄돼 멈춰 보였다)
            var bend = rig.WholeBend;
            var minP = Vector2.zero; var maxP = new Vector2(W, H);
            Vector2 foot = Uv(1005f, 860f), backFoot = Uv(580f, 860f), tailTip = Uv(160f, 820f), snout = Uv(1440f, 240f), belly = Uv(800f, 560f);
            Vector2 Moved(Vector2 uv) => bend.Deform(uv, minP, maxP) - Vector2.Scale(maxP, uv);

            var walk = new GeckoPose(12);
            walk[GeckoPartId.LegFrontNear].angle = 18f;
            walk[GeckoPartId.LegBackNear].angle  = -18f;
            walk[GeckoPartId.LegBackNear].offset = new Vector2(0f, 12f);
            bend.SetPose(walk);
            Check(Moved(foot).x > 60f && Moved(backFoot).x < -60f && Moved(backFoot).y > 8f,
                  $"전신 그림: 걸음 — 앞발이 앞으로 {Moved(foot).x:0} · 뒷발이 뒤로 {-Moved(backFoot).x:0}, 들림 {Moved(backFoot).y:0}");
            Check(Moved(belly).magnitude < 0.5f && Moved(snout).magnitude < 0.5f, "전신 그림: 걸음 — 배·머리는 다리를 따라 돌지 않는다");

            var sway = new GeckoPose(12);
            for (int k = 0; k < sway.tailBend.Length; k++) sway.tailBend[k] = 2.5f * Mathf.Sin(k * 0.42f);   // 마디마다 어긋난 물결
            bend.SetPose(sway);
            Check(Moved(tailTip).magnitude > 25f && Moved(belly).magnitude < 0.5f,
                  $"전신 그림: 꼬리 물결 — 합이 작아도 꼬리 끝이 {Moved(tailTip).magnitude:0} 움직이고 몸은 제자리");

            var look = new GeckoPose(12);
            look[GeckoPartId.Head].angle = 6f;
            bend.SetPose(look);
            Check(Moved(snout).y > 40f && Moved(foot).magnitude < 0.5f,
                  $"전신 그림: 고개 6° → 주둥이 {Moved(snout).y:0} 올라가고(배율 1.5) 앞발은 제자리");
            bend.ResetPose();

            // 혀 — 입 앞에서 나온다 (들어가 있을 때는 판정도 안 한다)
            go.transform.localRotation = Quaternion.identity;
            rig.SetFacing(true);
            rig.SolveRest();
            Vector2 tongue = rig.RestPosition(GeckoPartId.Tongue1), mouth = rig.RestPosition(GeckoPartId.Mouth);
            Check(tongue.x > mouth.x && Mathf.Abs(tongue.y - mouth.y) < 40f, "전신 그림: 혀 뿌리는 입 앞 끝");
            Check(!rig.TryPartLocal(GeckoPartId.Tongue1, rig.SkinToWorld(tongue), 0f, out _), "전신 그림: 들어간 혀는 판정하지 않는다");

            // 종을 바꾸면 — 전용 그림이 없는 종은 씬 기본 그림으로 (앞 게코의 전신 그림이 남지 않게)
            rig.UseDefaultSkin();
            rig.SolveRest();
            Check(rig.Skin == painted && !rig.IsWholeBody && G(GeckoPartId.Head).enabled && !rig.WholeBend.enabled,
                  "전신 그림: 전용 그림 없는 종으로 바꾸면 기본 파츠 그림으로 돌아간다");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    private static void Check(bool ok, string what)
    {
        s_lines.Add((ok ? "통과  " : "실패  ") + what);
        if (!ok) s_fail++;
    }
}
