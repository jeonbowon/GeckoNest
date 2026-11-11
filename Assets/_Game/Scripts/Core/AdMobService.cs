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
#if HAKO_DEVICE_TEST
    public static readonly bool USE_TEST_ADS = true;
#else
    public static readonly bool USE_TEST_ADS = false;
#endif
    private const string ANDROID_REWARDED_ID = "ca-app-pub-3852398620139102/5000029820";
    private const string IOS_REWARDED_ID     = "";
    private const float RETRY_SECONDS = 30f;
    private const float PREPARATION_TIMEOUT_SECONDS = 60f;

    public static AdMobService Instance { get; private set; }

    public AdState State { get; private set; } = AdState.WaitingForAge;
    public bool IsReady => State == AdState.Ready;
    public bool IsPresentingSystemUI => _consentFormOpen || _privacyFormOpen
        || State == AdState.WaitingForAge || State == AdState.WaitingForConsent
        || State == AdState.Showing;
    public string StatusTextKey => State switch
    {
        AdState.WaitingForAge => "ad.waiting_age",
        AdState.WaitingForConsent => "ad.waiting_consent",
        AdState.Initializing => "ad.initializing",
        AdState.Unavailable => "ad.unavailable",
        _ => "ad.loading",
    };
    public bool PrivacyOptionsRequired =>
        _begun && !_protectedAudience && _consentReady
        && ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

    public event Action StateChanged;

    private RewardedAd _rewardedAd;
    private bool _begun;
    private bool _sdkInitialized;
    private bool _rewardGranted;
    private Coroutine _retry;
    private enum RetryStep { None, Consent, Initialize, Load }
    private RetryStep _retryStep;
    private bool _consentFormOpen;
    private bool _protectedAudience = true;
    private bool _consentReady;
    private bool _privacyFormOpen;
    private int _loadGeneration;
    private RequestConfiguration _requestConfiguration;
    private float _preparationStartedAt;
    private bool _waitingForConsentUpdate;
    private int _consentGeneration;
    private int _initializationGeneration;

    public static RequestConfiguration RequestConfigurationFor(SettingsData settings, bool sdkInitialized = true)
    {
        bool protect = AdAudiencePolicy.RequiresProtection(settings);
        var configuration = new RequestConfiguration
        {
            AgeRestrictedTreatment = protect ? AgeRestrictedTreatment.Child : AgeRestrictedTreatment.Unspecified,
            // null skips the separate ID API before initialization, not age protection.
            PublisherFirstPartyIdEnabled = sdkInitialized ? (bool?)!protect : null,
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
        StartConsentUpdate();
#else
        SetState(AdState.Unavailable);
#endif
    }

#if UNITY_ANDROID || UNITY_IOS
    private void StartConsentUpdate()
    {
        if (_privacyFormOpen || _consentFormOpen || State == AdState.Showing) return;
        var settings = GameManager.Instance != null ? GameManager.Instance.Settings.GetSettings() : null;
        if (!AdAudiencePolicy.HasChoice(settings))
        {
            _begun = false;
            _retryStep = RetryStep.None;
            SetState(AdState.WaitingForAge);
            return;
        }
        _retryStep = RetryStep.Consent;
        _consentReady = false;
        _protectedAudience = AdAudiencePolicy.RequiresProtection(settings);
        try
        {
            MobileAdsEventExecutor.Initialize();
            _requestConfiguration = RequestConfigurationFor(settings);
            MobileAds.SetRequestConfiguration(RequestConfigurationFor(settings, sdkInitialized: _sdkInitialized));
            SetState(AdState.WaitingForConsent);
            _waitingForConsentUpdate = true;
            int generation = ++_consentGeneration;
            var request = new ConsentRequestParameters { TagForUnderAgeOfConsent = _protectedAudience };
            ConsentInformation.Update(request, error => RunOnMainThread(() =>
            {
                if (generation != _consentGeneration) return;
                _waitingForConsentUpdate = false;
                OnConsentUpdated(error);
            }));
        }
        catch (Exception error) { FailPreparation("begin", error.Message); }
    }

    private void OnConsentUpdated(FormError error)
    {
        if (error != null)
        {
            // Do not reuse an old adult consent result before this session's age-tagged update.
            FailPreparation("consent", error.Message);
            return;
        }

        _consentReady = true;
        if (_protectedAudience)
        {
            // UMP TFUA suppresses consent collection for protected users.
            TryInitialize();
            return;
        }

        _consentFormOpen = true;
        int generation = _consentGeneration;
        ConsentForm.LoadAndShowConsentFormIfRequired(formError => RunOnMainThread(() =>
        {
            if (generation != _consentGeneration) return;
            _consentFormOpen = false;
            if (formError != null)
            {
                Debug.LogWarning("[AdMob] 동의 화면 표시 실패 — " + formError.Message);
                if (!ConsentInformation.CanRequestAds())
                {
                    FailPreparation("consent-form", formError.Message);
                    return;
                }
            }
            TryInitialize();
        }));
    }

    private void TryInitialize()
    {
        NotifyStateChanged();
        if (State == AdState.Initializing || _privacyFormOpen || _consentFormOpen) return;
        if (!_consentReady || !ConsentInformation.CanRequestAds())
        {
            // A completed consent flow that disallows ads is not a network failure.
            _retryStep = RetryStep.None;
            SetState(AdState.Unavailable);
            return;
        }
        if (_sdkInitialized)
        {
            LoadRewardedAd();
            return;
        }

        _retryStep = RetryStep.Initialize;
        SetState(AdState.Initializing);
        int generation = ++_initializationGeneration;
        MobileAds.Initialize(status => RunOnMainThread(() =>
        {
            // Android's SDK client shares the initialization callback across attempts.
            // A delayed native completion can therefore invoke the current callback twice.
            if (generation != _initializationGeneration || _sdkInitialized || State != AdState.Initializing) return;
            if (status == null)
            {
                FailPreparation("initialize", "No initialization status");
                return;
            }
            // An ID-policy failure must prevent loading with incomplete protection.
            MobileAds.SetRequestConfiguration(_requestConfiguration);
            _sdkInitialized = true;
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
        if (!PrivacyOptionsRequired || _privacyFormOpen || _consentFormOpen || _waitingForConsentUpdate
            || State == AdState.Initializing || State == AdState.Showing)
        {
            onComplete?.Invoke(null);
            return;
        }
        _privacyFormOpen = true;
        _retryStep = RetryStep.None;
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
        _retryStep = RetryStep.Load;
        try { LoadRewardedAdCore(); }
        catch (Exception error) { FailPreparation("load", error.Message); }
    }

    private void LoadRewardedAdCore()
    {
        if (!_begun || !_sdkInitialized || !_consentReady || _privacyFormOpen || !ConsentInformation.CanRequestAds())
        {
            _retryStep = RetryStep.None;
            SetState(AdState.Unavailable);
            return;
        }
        string id = RewardedId();
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError("[AdMob] 실제 보상형 광고 단위 ID가 비어 있습니다.");
            _retryStep = RetryStep.None;
            SetState(AdState.Unavailable);
            return;
        }

        DestroyRewardedAd();
        int generation = ++_loadGeneration;
        _retryStep = RetryStep.Load;
        SetState(AdState.Loading);
        RewardedAd.Load(id, new AdRequest(), (ad, error) => RunOnMainThread(() =>
        {
            if (this == null || generation != _loadGeneration || _privacyFormOpen
                || !ConsentInformation.CanRequestAds())
            {
                ad?.Destroy();
                if (this != null && generation == _loadGeneration && !_privacyFormOpen)
                    SetState(AdState.Unavailable);
                return;
            }
            if (error != null || ad == null)
            {
                Debug.LogWarning("[AdMob] 보상 광고 로드 실패 — " + error);
                ad?.Destroy();
                SetState(AdState.Unavailable);
                ScheduleRetry();
                return;
            }
            _rewardedAd = ad;
            _retryStep = RetryStep.None;
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
        if (_retryStep == RetryStep.None || !_begun || _privacyFormOpen || _consentFormOpen) return;
        if (_retry != null) StopCoroutine(_retry);
        _retry = StartCoroutine(RetryAfterDelay());
    }

    private IEnumerator RetryAfterDelay()
    {
        yield return new WaitForSecondsRealtime(RETRY_SECONDS);
        _retry = null;
        if (!_begun || State != AdState.Unavailable || _privacyFormOpen || _consentFormOpen) yield break;
        try
        {
            switch (_retryStep)
            {
                case RetryStep.Consent: StartConsentUpdate(); break;
                case RetryStep.Initialize: TryInitialize(); break;
                case RetryStep.Load: LoadRewardedAd(); break;
            }
        }
        catch (Exception error) { FailPreparation("retry", error.Message); }
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
        _preparationStartedAt = Time.realtimeSinceStartup;
        Debug.Log($"[AdMob] State: {state}");
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => StateChanged?.Invoke();

    private void RunOnMainThread(Action action)
    {
        if (action == null) return;
        MobileAdsEventExecutor.ExecuteInUpdate(() =>
        {
            if (this == null) return;
            try { action(); }
            catch (Exception error) { FailPreparation("callback", error.Message); }
        });
    }

    private void FailPreparation(string stage, string reason)
    {
        _waitingForConsentUpdate = false;
        _consentFormOpen = false;
        if (_retryStep == RetryStep.Consent) _consentReady = false;
        ++_consentGeneration;
        ++_initializationGeneration;
        ++_loadGeneration;
        Debug.LogWarning($"[AdMob] Preparation failed ({stage}): {reason}");
        SetState(AdState.Unavailable);
#if UNITY_ANDROID || UNITY_IOS
        ScheduleRetry();
#endif
    }

    private void Update()
    {
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
        // A user reading a consent/privacy form is not a network timeout.
        bool waiting = _waitingForConsentUpdate || State == AdState.Initializing || State == AdState.Loading;
        if (!waiting || Time.realtimeSinceStartup - _preparationStartedAt < PREPARATION_TIMEOUT_SECONDS) return;
        FailPreparation("timeout", State.ToString());
#endif
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
