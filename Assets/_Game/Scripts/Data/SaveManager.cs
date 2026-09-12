using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager
{
    private const string DEFAULT_STEM = "player_data";

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

    public PlayerData Load()
    {
        if (File.Exists(MainPath))
        {
            try
            {
                var json = File.ReadAllText(MainPath);
                var data = JsonUtility.FromJson<PlayerData>(json)
                           ?? throw new Exception("저장 파일이 비어 있거나 형식이 맞지 않습니다");
                return TryMigrate(data);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] 메인 파일 로드 실패, 백업 시도: {e.Message}");
                return LoadFromBackup();
            }
        }
        return CreateNewPlayerData();
    }

    public void Save(PlayerData data)
    {
        var json = JsonUtility.ToJson(data, prettyPrint: false);
        File.WriteAllText(TmpPath, json);                               // 1. tmp에 먼저 쓰기
        if (File.Exists(MainPath))
            File.Copy(MainPath, BakPath, overwrite: true);              // 2. 기존 main → bak
        if (File.Exists(MainPath))
            File.Delete(MainPath);
        File.Move(TmpPath, MainPath);                                   // 3. tmp → main (atomic)
    }

    private PlayerData LoadFromBackup()
    {
        if (File.Exists(BakPath))
        {
            try
            {
                var json = File.ReadAllText(BakPath);
                var data = JsonUtility.FromJson<PlayerData>(json)
                           ?? throw new Exception("백업 파일이 비어 있거나 형식이 맞지 않습니다");
                Debug.Log("[SaveManager] 백업 파일로 복원 성공");
                return TryMigrate(data);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] 백업 파일도 실패, 새 데이터 생성: {e.Message}");
            }
        }
        return CreateNewPlayerData();
    }

    private PlayerData TryMigrate(PlayerData data)
    {
        // JsonUtility.FromJson은 기본 생성자를 호출하지 않으므로 List 필드가 null일 수 있음
        data.geckos       ??= new List<GeckoData>();
        data.inventory    ??= new List<ItemStack>();
        data.ownedItemIds ??= new List<string>();
        data.terrarium    ??= new TerrariumData();
        data.terrarium.decorSlots    ??= new string[4];
        data.terrarium.ownedDecorIds ??= new List<string>();   // 예전 저장 파일에는 없는 필드

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

    private PlayerData CreateNewPlayerData()
    {
        var data = new PlayerData { coin = 100, gem = 0, saveVersion = 2 };

        // 기본 게코 (하코) 생성
        var gecko = new GeckoData
        {
            id               = Guid.NewGuid().ToString(),
            name             = "하코",
            speciesId        = "crested",
            growthStage      = 0,
            hunger           = 80f,
            thirst           = 80f,
            mood             = 80f,
            health           = 80f,
            cleanliness      = 80f,
            affection        = 0f,
            createdAtTicks   = DateTime.UtcNow.Ticks,
            lastUpdatedTicks = DateTime.UtcNow.Ticks,
        };

        data.geckos.Add(gecko);
        data.selectedGeckoId = gecko.id;
        data.inventory.Add(new ItemStack("cricket_small", 3)); // 초기 지급 3개

        Debug.Log("[SaveManager] 새 PlayerData 생성 완료");
        return data;
    }
}
