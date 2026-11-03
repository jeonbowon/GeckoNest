using System;
using System.Collections;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

/// <summary>
/// UMP 동의 → Mobile Ads 초기화 → 보상형 광고 한 편 미리 불러오기를 한 곳에서 관리한다.
/// 재화는 건드리지 않고, 보상 완료 사실만 호출자에게 전달한다.
/// </summary>
public sealed class AdMobService : MonoBehaviour
{
    public enum AdState { WaitingForAge, WaitingForConsent, Initializing, Loading, Ready, Showing, Unavailable }

    // Google 공식 테스트 광고 단위. USE_TEST_ADS가 true이면 아래 실제 ID 대신 사용한다.
#if UNITY_ANDROID
    private const string TEST_REWARDED_ID = "ca-app-pub-3940256099942544/5224354917";
#elif UNITY_IOS
    private const string TEST_REWARDED_ID = "ca-app-pub-3940256099942544/1712485313";
#else
    private const string TEST_REWARDED_ID = "unused";
#endif

    // Android 출시 설정. 실제 ID 경로를 개발 중 검사할 때는 AdMob 테스트 기기를 등록한다.
    public static readonly bool USE_TEST_ADS = false;
    private const string ANDROID_REWARDED_ID = "ca-app-pub-3852398620139102/5000029820";
    private const string IOS_REWARDED_ID     = "";
    private const float RETRY_SECONDS = 30f;

    public static AdMobService Instance { get; private set; }

    public AdState State { get; private set; } = AdState.WaitingForAge;
    public bool IsReady => State == AdState.Ready;
    public bool PrivacyOptionsRequired =>
        _begun && !_protectedAudience && _consentReady
        && ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

    public event Action StateChanged;

    private RewardedAd _rewardedAd;
    private bool _begun;
    private bool _sdkInitialized;
    private bool _rewardGranted;
    private Coroutine _retry;
    private bool _protectedAudience = true;
    private bool _consentReady;
    private bool _privacyFormOpen;
    private int _loadGeneration;

    public static RequestConfiguration RequestConfigurationFor(SettingsData settings)
    {
        bool protect = AdAudiencePolicy.RequiresProtection(settings);
        var configuration = new RequestConfiguration
        {
            AgeRestrictedTreatment = protect ? AgeRestrictedTreatment.Child : AgeRestrictedTreatment.Unspecified,
            PublisherFirstPartyIdEnabled = !protect,
        };
        if (protect) configuration.MaxAdContentRating = MaxAdContentRating.G;
        return configuration;
    }

    public static AdMobService Create(bool beginImmediately)
    {
        if (Instance == null)
            new GameObject("[AdMobService]").AddComponent<AdMobService>();
        if (beginImmediately) Instance.Begin();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>새 게임은 부화 연출이 끝난 뒤, 기존 게임은 부팅 직후 한 번 호출한다.</summary>
    public void Begin()
    {
        if (_begun) return;
        var settings = GameManager.Instance != null ? GameManager.Instance.Settings.GetSettings() : null;
        if (!AdAudiencePolicy.HasChoice(settings))
        {
            SetState(AdState.WaitingForAge);
            return;
        }
        _protectedAudience = AdAudiencePolicy.RequiresProtection(settings);
        _begun = true;

#if UNITY_EDITOR
        // 에디터에서는 네트워크 광고 대신 완료 콜백을 모의 실행해 UI·저장을 검사한다.
        SetState(AdState.Ready);
#elif UNITY_ANDROID || UNITY_IOS
        // Must precede both UMP and SDK initialization, including existing installs.
        MobileAds.SetRequestConfiguration(RequestConfigurationFor(settings));
        SetState(AdState.WaitingForConsent);
        var request = new ConsentRequestParameters { TagForUnderAgeOfConsent = _protectedAudience };
        ConsentInformation.Update(request, error => RunOnMainThread(() => OnConsentUpdated(error)));
#else
        SetState(AdState.Unavailable);
#endif
    }

#if UNITY_ANDROID || UNITY_IOS
    private void OnConsentUpdated(FormError error)
    {
        if (error != null)
        {
            Debug.LogWarning("[AdMob] 동의 정보 갱신 실패 — " + error.Message);
            // Do not reuse an old adult consent result before this session's age-tagged update.
            SetState(AdState.Unavailable);
            return;
        }

        _consentReady = true;
        if (_protectedAudience)
        {
            // UMP TFUA suppresses consent collection for protected users.
            TryInitialize();
            return;
        }

        ConsentForm.LoadAndShowConsentFormIfRequired(formError => RunOnMainThread(() =>
        {
            if (formError != null)
                Debug.LogWarning("[AdMob] 동의 화면 표시 실패 — " + formError.Message);
            TryInitialize();
        }));
    }

    private void TryInitialize()
    {
        NotifyStateChanged();
        if (_sdkInitialized) return;
        if (!_consentReady || !ConsentInformation.CanRequestAds())
        {
            SetState(AdState.Unavailable);
            return;
        }

        _sdkInitialized = true;
        SetState(AdState.Initializing);
        MobileAds.Initialize(status => RunOnMainThread(() =>
        {
            if (status == null)
            {
                _sdkInitialized = false;
                SetState(AdState.Unavailable);
                return;
            }
            LoadRewardedAd();
        }));
    }
#endif

    public bool ShowRewarded(Action onEarned, Action onClosed = null, Action onUnavailable = null)
    {
        if (!IsReady)
        {
            onUnavailable?.Invoke();
            return false;
        }

#if UNITY_EDITOR
        SetState(AdState.Showing);
        StartCoroutine(SimulateEditorReward(onEarned, onClosed));
        return true;
#elif UNITY_ANDROID || UNITY_IOS
        if (_rewardedAd == null || !_rewardedAd.CanShowAd())
        {
            SetState(AdState.Unavailable);
            onUnavailable?.Invoke();
            LoadRewardedAd();
            return false;
        }

        var shownAd = _rewardedAd;
        _rewardGranted = false;
        SetState(AdState.Showing);

        shownAd.OnAdFullScreenContentClosed += () => RunOnMainThread(() =>
        {
            FinishShownAd(shownAd);
            onClosed?.Invoke();
        });
        shownAd.OnAdFullScreenContentFailed += error => RunOnMainThread(() =>
        {
            Debug.LogWarning("[AdMob] 보상 광고 표시 실패 — " + error);
            FinishShownAd(shownAd);
            onUnavailable?.Invoke();
            onClosed?.Invoke();
        });
        shownAd.Show(reward => RunOnMainThread(() =>
        {
            if (_rewardGranted) return;
            _rewardGranted = true;
            onEarned?.Invoke();
        }));
        return true;
#else
        onUnavailable?.Invoke();
        return false;
#endif
    }

    public void ShowPrivacyOptions(Action<string> onComplete = null)
    {
#if UNITY_ANDROID || UNITY_IOS
        if (!PrivacyOptionsRequired || _privacyFormOpen || State == AdState.Showing)
        {
            onComplete?.Invoke(null);
            return;
        }
        _privacyFormOpen = true;
        ++_loadGeneration;
        if (_retry != null) { StopCoroutine(_retry); _retry = null; }
        DestroyRewardedAd();
        SetState(AdState.WaitingForConsent);
        ConsentForm.ShowPrivacyOptionsForm(error => RunOnMainThread(() =>
        {
            _privacyFormOpen = false;
            if (ConsentInformation.CanRequestAds())
            {
                if (_sdkInitialized) LoadRewardedAd();
                else TryInitialize();
            }
            else SetState(AdState.Unavailable);
            NotifyStateChanged();
            onComplete?.Invoke(error != null ? error.Message : null);
        }));
#else
        onComplete?.Invoke(null);
#endif
    }

#if UNITY_ANDROID || UNITY_IOS
    private void LoadRewardedAd()
    {
        if (!_begun || !_consentReady || _privacyFormOpen || !ConsentInformation.CanRequestAds())
        {
            SetState(AdState.Unavailable);
            return;
        }
        string id = RewardedId();
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError("[AdMob] 실제 보상형 광고 단위 ID가 비어 있습니다.");
            SetState(AdState.Unavailable);
            return;
        }

        DestroyRewardedAd();
        int generation = ++_loadGeneration;
        SetState(AdState.Loading);
        RewardedAd.Load(id, new AdRequest(), (ad, error) => RunOnMainThread(() =>
        {
            if (this == null || generation != _loadGeneration || _privacyFormOpen
                || !ConsentInformation.CanRequestAds())
            {
                ad?.Destroy();
                return;
            }
            if (error != null || ad == null)
            {
                Debug.LogWarning("[AdMob] 보상 광고 로드 실패 — " + error);
                SetState(AdState.Unavailable);
                ScheduleRetry();
                return;
            }
            _rewardedAd = ad;
            SetState(AdState.Ready);
            Debug.Log("[AdMob] 보상 광고 준비 완료");
        }));
    }

    private void FinishShownAd(RewardedAd shownAd)
    {
        if (_rewardedAd == shownAd) _rewardedAd = null;
        shownAd?.Destroy();
        LoadRewardedAd();
    }

    private void ScheduleRetry()
    {
        if (_retry != null) StopCoroutine(_retry);
        _retry = StartCoroutine(RetryAfterDelay());
    }

    private IEnumerator RetryAfterDelay()
    {
        yield return new WaitForSecondsRealtime(RETRY_SECONDS);
        _retry = null;
        if (_begun && _sdkInitialized && State == AdState.Unavailable) LoadRewardedAd();
    }
#endif

#if UNITY_EDITOR
    private IEnumerator SimulateEditorReward(Action onEarned, Action onClosed)
    {
        yield return new WaitForSecondsRealtime(0.35f);
        onEarned?.Invoke();
        SetState(AdState.Ready);
        onClosed?.Invoke();
    }
#endif

    private static string RewardedId()
    {
        if (USE_TEST_ADS) return TEST_REWARDED_ID;
#if UNITY_ANDROID
        return ANDROID_REWARDED_ID;
#elif UNITY_IOS
        return IOS_REWARDED_ID;
#else
        return null;
#endif
    }

    private void SetState(AdState state)
    {
        if (State == state) return;
        State = state;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => StateChanged?.Invoke();

    private static void RunOnMainThread(Action action)
    {
        if (action != null) MobileAdsEventExecutor.ExecuteInUpdate(action);
    }

    private void DestroyRewardedAd()
    {
        if (_rewardedAd == null) return;
        _rewardedAd.Destroy();
        _rewardedAd = null;
    }

    private void OnDestroy()
    {
        DestroyRewardedAd();
        if (Instance == this) Instance = null;
    }
}
