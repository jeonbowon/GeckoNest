using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
