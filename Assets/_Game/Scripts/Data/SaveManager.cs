using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class SaveManager
{
    private const string DEFAULT_STEM = "player_data";
    private const int    START_COIN   = 100;   // [TBD] 새 플레이어 시작 코인

    private readonly string _stem;

    /// <summary>stem = 저장 파일 이름 (확장자 앞부분). 자가 검사처럼 진짜 저장을 건드리면 안 될 때만 바꾼다.</summary>
    public SaveManager(string stem = DEFAULT_STEM)
    {
        _stem = string.IsNullOrEmpty(stem) ? DEFAULT_STEM : stem;
    }

    private static string RootPath => Application.persistentDataPath;
    private string MainPath => Path.Combine(RootPath, _stem + ".json");
    private string TmpPath  => Path.Combine(RootPath, _stem + ".tmp");
    private string BakPath  => Path.Combine(RootPath, _stem + ".bak");

    /// <summary>이 저장 파일 3종을 지운다 (자가 검사 뒷정리용).</summary>
    public void DeleteFiles()
    {
        foreach (var p in new[] { MainPath, TmpPath, BakPath })
            if (File.Exists(p)) File.Delete(p);
    }

    /// <summary>메인 → (메인이 없을 때) 임시 → 백업 순서로 읽는다. 모두 없거나 깨졌으면 새 데이터.</summary>
    public PlayerData Load()
    {
        if (File.Exists(MainPath))
        {
            if (TryRead(MainPath, out var main, out var error)) return main;
            Debug.LogWarning($"[SaveManager] 메인 파일 로드 실패, 백업 시도: {error}");
        }
        else if (File.Exists(TmpPath))
        {
            // 메인은 없는데 임시 파일이 있다 = 저장 도중(메인을 지운 직후) 앱이 멈췄다.
            // 임시 파일은 끝까지 쓰고 디스크에 확정한 뒤에만 메인을 지우므로, 가장 최신의 온전한 데이터다.
            if (TryRead(TmpPath, out var tmp, out var error))
            {
                Debug.LogWarning("[SaveManager] 저장 도중 멈춘 흔적 — 임시 파일로 복원");
                return tmp;
            }
            Debug.LogWarning($"[SaveManager] 임시 파일 로드 실패, 백업 시도: {error}");
        }

        if (File.Exists(BakPath))
        {
            if (TryRead(BakPath, out var bak, out var error))
            {
                Debug.Log("[SaveManager] 백업 파일로 복원 성공");
                return bak;
            }
            Debug.LogError($"[SaveManager] 백업 파일도 실패, 새 데이터 생성: {error}");
        }

        return CreateNewPlayerData();
    }

    public void Save(PlayerData data)
    {
        var json = JsonUtility.ToJson(data, prettyPrint: false);

        // 1. 임시 파일에 끝까지 쓰고 디스크에 확정 — 여기서 멈추면 메인 파일은 그대로 남는다
        WriteDurable(TmpPath, json);

        // 2. 기존 메인 → 백업 (복사하는 동안에도 메인은 남아 있다)
        if (File.Exists(MainPath))
        {
            File.Copy(MainPath, BakPath, overwrite: true);
            File.Delete(MainPath);
        }

        // 3. 임시 → 메인. 2와 3 사이에 멈추면 메인이 없지만 Load가 임시 파일로 복원한다
        File.Move(TmpPath, MainPath);
    }

    private static void WriteDurable(string path, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            fs.Write(bytes, 0, bytes.Length);
            fs.Flush(true);   // OS 버퍼에만 두지 않고 디스크에 기록 — 전원이 꺼져도 빈 파일로 남지 않게
        }
    }

    private static bool TryRead(string path, out PlayerData data, out string error)
    {
        data  = null;
        error = null;
        try
        {
            var parsed = JsonUtility.FromJson<PlayerData>(File.ReadAllText(path));
            if (parsed == null)
            {
                error = "파일이 비어 있거나 형식이 맞지 않습니다";
                return false;
            }
            data = TryMigrate(parsed);
            return true;
        }
        catch (Exception e)
        {
            error = e.Message;
            return false;
        }
    }

    private static PlayerData TryMigrate(PlayerData data)
    {
        // JsonUtility.FromJson은 기본 생성자를 호출하지 않으므로 List 필드가 null일 수 있음
        data.geckos       ??= new List<GeckoData>();
        data.inventory    ??= new List<ItemStack>();
        data.ownedItemIds ??= new List<string>();
        data.terrarium    ??= new TerrariumData();
        data.terrarium.decorSlots    ??= new string[4];
        data.terrarium.ownedDecorIds ??= new List<string>();   // 예전 저장 파일에는 없는 필드

        // 배경·바닥이 비어 있으면 무료 기본값 — 예전에는 새로 시작하면 홈 배경·바닥이 꺼진 채로 보였다
        if (string.IsNullOrEmpty(data.terrarium.backgroundId)) data.terrarium.backgroundId = TerrariumData.DEFAULT_BACKGROUND_ID;
        if (string.IsNullOrEmpty(data.terrarium.floorId))      data.terrarium.floorId      = TerrariumData.DEFAULT_FLOOR_ID;

        if (data.saveVersion < 2)
        {
            // v1 → v2: ownedItemIds(List<string>) → inventory(List<ItemStack>)
            // 동일 itemId가 여러 번 들어 있으면 count로 합산
            foreach (var id in data.ownedItemIds)
            {
                var existing = data.inventory.Find(s => s.itemId == id);
                if (existing != null)
                    existing.count++;
                else
                    data.inventory.Add(new ItemStack(id, 1));
            }
            data.ownedItemIds.Clear();
            data.saveVersion = 2;
            Debug.Log($"[SaveManager] v1 → v2 마이그레이션 완료 (인벤토리 {data.inventory.Count}종)");
        }
        return data;
    }

    // 게코와 첫 먹이는 PlayerRepository.EnsureStarterGecko가 준다 (새 플레이어 · 저장 손상 복구 공통).
    // 배경·바닥 기본값은 TerrariumData 필드 초기값에 들어 있다.
    private static PlayerData CreateNewPlayerData()
    {
        Debug.Log("[SaveManager] 새 PlayerData 생성");
        return new PlayerData { coin = START_COIN, gem = 0, saveVersion = 2 };
    }
}
