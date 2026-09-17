using UnityEngine;
using UnityEngine.SceneManagement;

public class AppBootstrap : MonoBehaviour
{
    private const int    TARGET_FPS            = 60;    // 코드 애니메이션이 30fps에서 뚝뚝 끊겨 보이지 않게
    private const float  PROGRESS_TICK_SECONDS = 30f;   // [TBD] 실행 중 상태값 진행 주기
    private const string BOOT_SCENE            = "Boot";

    private PlayerRepository _repo;
    private TimeManager      _time;
    private GeckoManager     _gecko;
    private float            _tickTimer;

    private static string s_reloadScene;   // Boot을 거치지 않고 실행했을 때 초기화 후 다시 열 씬

    /// <summary>
    /// Boot 씬을 거치지 않고 아무 씬이나 바로 실행해도 게임이 돌아가게 한다 (에디터 작업 편의 · 안전망).
    /// 매니저를 만든 뒤 그 씬을 다시 열어 UI가 정상적으로 초기화되게 한다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureBootstrapped()
    {
        if (GameManager.Instance != null) return;   // Boot 씬에서 이미 초기화됨

        var scene = SceneManager.GetActiveScene();
        if (scene.name == BOOT_SCENE) return;       // Boot 씬이면 씬에 놓인 AppBootstrap이 처리한다

        Debug.Log($"[AppBootstrap] Boot 씬을 거치지 않고 '{scene.name}'을 실행했습니다 — 매니저를 만들고 씬을 다시 엽니다.");
        s_reloadScene = scene.name;
        new GameObject("[AppBootstrap]").AddComponent<AppBootstrap>();
    }

    private void Awake()
    {
        // 이미 초기화돼 있으면 (씬을 다시 열었을 때의 Boot 오브젝트 등) 중복 초기화하지 않는다
        if (GameManager.Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        Application.targetFrameRate = TARGET_FPS;

        // 0. 연출 서비스 (소리) — 설정 적용보다 먼저 있어야 한다
        AudioManager.Create();

        // 1. 저장/시간 (하위 의존 없음)
        var save  = new SaveManager();
        _time = new TimeManager();
        _repo = new PlayerRepository(save);

        // 2. Domain 매니저
        _gecko        = new GeckoManager(_repo, _time);
        var store     = new StoreManager(_repo);
        var terrarium = new TerrariumManager(_repo);
        var reward    = new RewardManager(_repo);
        _gecko.OnCareDone += reward.RecordCare;   // 오늘의 돌봄 목표 — 실제로 한 돌봄만 센다
        var settings  = new SettingsManager(_repo);

        // 2-b. 성장·허물 사건 대기열 — 시간 보정보다 먼저 만들어야 부팅 중 생긴 사건도 모인다
        var events = new GeckoEventQueue(_gecko);

        // 3. 전역 진입점 초기화
        GameManager.Initialize(_repo, _time, _gecko, store, terrarium, reward, settings, events);

        // 4-a. 설정 즉시 적용 (BGM 등)
        settings.ApplyAll();

        // 4-b. 언어 — 기본 게코 이름·첫 화면 글자보다 먼저 정해야 한다
        Loc.Init(settings.GetSettings().language);

        // 5. 게코 없으면 기본 게코 보장 (저장 파일 손상 등 방어)
        EnsureDefaultGecko();

        // 6. 오프라인 진행 보정 후 즉시 저장
        ApplyElapsedProgress("앱 시작");

        // 6-b. 앱을 열었으니 예약해 둔 알림은 지운다 (알림이 켜져 있으면 권한도 확인)
        //      새 게임은 부화 연출 위에 권한 창이 뜨지 않게 부화가 끝난 뒤 묻는다 (HomeUIController.OnHatched)
        NotificationScheduler.CancelAll();
        if (settings.GetSettings().notificationOn && !_gecko.NeedsHatchIntro()) NotificationScheduler.RequestPermission();

        // 7. 홈으로 (Boot을 거치지 않고 실행한 경우에는 원래 보던 씬으로 되돌아간다)
        if (string.IsNullOrEmpty(s_reloadScene))
        {
            SceneRouter.GoToHome();
        }
        else
        {
            string scene = s_reloadScene;
            s_reloadScene = null;
            SceneRouter.GoToScene(scene);
        }
    }

    // 앱을 켜 둔 동안에도 시간이 흐른다 — 주기적으로 상태값을 진행시킨다.
    // 여기서는 저장하지 않는다: 메모리 값과 lastUpdatedTicks가 함께 움직이므로,
    // 저장 전에 앱이 죽어도 다음 실행 때 파일 기준으로 다시 계산되어 결과가 같다.
    private void Update()
    {
        if (_gecko == null) return;
        _tickTimer += Time.unscaledDeltaTime;
        if (_tickTimer < PROGRESS_TICK_SECONDS) return;
        _tickTimer = 0f;
        _gecko.ApplyElapsedProgressAll();
    }

    // 백그라운드로 갈 때: 지금까지의 진행을 반영하고 저장
    // 돌아올 때: 자리 비운 시간을 반영하고 저장 (예전에는 복귀 시 아무것도 하지 않아 그 시간이 사라졌다)
    private void OnApplicationPause(bool pauseStatus)
    {
        ApplyElapsedProgress(pauseStatus ? "백그라운드 진입" : "복귀");

        // 자리를 비우는 동안 돌볼 때가 되면 알려 준다. 돌아오면 예약을 지운다.
        if (pauseStatus) NotificationScheduler.ScheduleAll();
        else             NotificationScheduler.CancelAll();
    }

    private void OnApplicationQuit()
    {
        ApplyElapsedProgress("종료");
        NotificationScheduler.ScheduleAll();
    }

    // 새 플레이어(저장 파일 없음)도 이 경로로 게코를 받는다
    private void EnsureDefaultGecko()
    {
        if (!_repo.EnsureStarterGecko()) return;
        _repo.Save();
        Debug.Log("[AppBootstrap] 기본 게코 생성 완료 — 하코");
    }

    private void ApplyElapsedProgress(string reason)
    {
        if (_gecko == null || _repo == null) return;

        var data = _repo.GetPlayerData();
        foreach (var g in data.geckos)
        {
            float elapsed = _time.GetElapsedHours(g.lastUpdatedTicks);
            if (elapsed >= 0.05f)   // 3분 이상일 때만 기록 (로그 과다 방지)
                Debug.Log($"[AppBootstrap] {reason} 시간 보정 — {g.name}: {elapsed:F1}h (적용: {_time.ClampOfflineProgress(elapsed):F1}h)");
        }

        _gecko.ApplyElapsedProgressAll();
        _repo.Save();
        _tickTimer = 0f;
    }
}
