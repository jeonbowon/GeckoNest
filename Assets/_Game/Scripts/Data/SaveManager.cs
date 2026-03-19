using System;
using System.IO;
using UnityEngine;

public class SaveManager
{
    private static string RootPath => Application.persistentDataPath;
    private static string MainPath => Path.Combine(RootPath, "player_data.json");
    private static string TmpPath  => Path.Combine(RootPath, "player_data.tmp");
    private static string BakPath  => Path.Combine(RootPath, "player_data.bak");

    public PlayerData Load()
    {
        if (File.Exists(MainPath))
        {
            try
            {
                var json = File.ReadAllText(MainPath);
                var data = JsonUtility.FromJson<PlayerData>(json);
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
        File.Move(TmpPath, MainPath);                                   // 3. tmp → main (atomic)
    }

    private PlayerData LoadFromBackup()
    {
        if (File.Exists(BakPath))
        {
            try
            {
                var json = File.ReadAllText(BakPath);
                var data = JsonUtility.FromJson<PlayerData>(json);
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
        if (data.saveVersion < 1)
        {
            // 향후 마이그레이션 로직 추가
            data.saveVersion = 1;
        }
        return data;
    }

    private PlayerData CreateNewPlayerData()
    {
        var data = new PlayerData { coin = 100, gem = 0, saveVersion = 1 };

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
            health           = 100f,
            cleanliness      = 100f,
            affection        = 0f,
            createdAtTicks   = DateTime.UtcNow.Ticks,
            lastUpdatedTicks = DateTime.UtcNow.Ticks,
        };

        data.geckos.Add(gecko);
        data.selectedGeckoId = gecko.id;

        Debug.Log("[SaveManager] 새 PlayerData 생성 완료");
        return data;
    }
}
