using UnityEngine;

public class AppBootstrap : MonoBehaviour
{
    private PlayerRepository _repo;
    private TimeManager      _time;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        // 1. 저장/시간 (하위 의존 없음)
        var save  = new SaveManager();
        _time = new TimeManager();
        _repo = new PlayerRepository(save);

        // 2. Domain 매니저
        var gecko = new GeckoManager(_repo, _time);

        // 3. 전역 진입점 초기화
        GameManager.Initialize(_repo, _time, gecko);

        // 4. 오프라인 진행 보정
        ApplyOfflineProgress(gecko);

        // 5. 홈으로
        SceneRouter.GoToHome();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            UpdateLastTicks();
    }

    private void OnApplicationQuit()
    {
        UpdateLastTicks();
    }

    private void ApplyOfflineProgress(GeckoManager gecko)
    {
        var data = _repo.GetPlayerData();
        foreach (var g in data.geckos)
        {
            float elapsed = _time.GetElapsedHours(g.lastUpdatedTicks);
            if (elapsed <= 0f) continue;

            Debug.Log($"[AppBootstrap] 오프라인 보정 — {g.name}: {elapsed:F1}h (적용: {_time.ClampOfflineProgress(elapsed):F1}h)");
            gecko.ApplyOfflineProgress(g.id, elapsed);
        }

        _repo.Save();
    }

    private void UpdateLastTicks()
    {
        var data = _repo.GetPlayerData();
        foreach (var gecko in data.geckos)
            gecko.lastUpdatedTicks = _time.GetNowTicks();
        _repo.Save();
    }
}
