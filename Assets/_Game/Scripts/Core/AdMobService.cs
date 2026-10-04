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
    public enum AdState { WaitingForConsent, Initializing, Loading, Ready, Showing, Unavailable }

    // Google 공식 테스트 광고 단위. USE_TEST_ADS가 true이면 아래 실제 ID 대신 사용한다.
#if UNITY_ANDROID
    private const string TEST_REWARDED_ID = "ca-app-pub-3940256099942544/5224354917";
#elif UNITY_IOS
    private const string TEST_REWARDED_ID = "ca-app-pub-3940256099942544/1712485313";
#else
    private const string TEST_REWARDED_ID = "unused";
#endif

    // 기기 테스트까지 true를 유지한다. 실제 ID 경로 검사는 테스트 기기 등록 후 false로 전환한다.
    public static readonly bool USE_TEST_ADS = true;
    private const string ANDROID_REWARDED_ID = "ca-app-pub-3852398620139102/5000029820";
    private const string IOS_REWARDED_ID     = "";
    private const float RETRY_SECONDS = 30f;

    public static AdMobService Instance { get; private set; }

    public AdState State { get; private set; } = AdState.WaitingForConsent;
    public bool IsReady => State == AdState.Ready;
    public bool PrivacyOptionsRequired =>
        ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

    public event Action StateChanged;

    private RewardedAd _rewardedAd;
    private bool _begun;
    private bool _sdkInitialized;
    private bool _rewardGranted;
    private Coroutine _retry;

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
        _begun = true;

#if UNITY_EDITOR
        // 에디터에서는 네트워크 광고 대신 완료 콜백을 모의 실행해 UI·저장을 검사한다.
        SetState(AdState.Ready);
#elif UNITY_ANDROID || UNITY_IOS
        SetState(AdState.WaitingForConsent);
        var request = new ConsentRequestParameters { TagForUnderAgeOfConsent = false };
        ConsentInformation.Update(request, OnConsentUpdated);
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
            // 이전 실행에서 저장된 동의가 아직 유효할 수 있다. 새 폼을 열지는 않고
            // CanRequestAds 결과만 확인해 SDK 초기화 여부를 결정한다.
            RunOnMainThread(TryInitialize);
            return;
        }

        ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
        {
            if (formError != null)
                Debug.LogWarning("[AdMob] 동의 화면 표시 실패 — " + formError.Message);
            RunOnMainThread(TryInitialize);
        });

        // 이전 실행의 유효한 동의가 있으면 화면 로드와 중복되지 않게 TryInitialize의 가드가 막는다.
        if (ConsentInformation.CanRequestAds()) RunOnMainThread(TryInitialize);
    }

    private void TryInitialize()
    {
        NotifyStateChanged();
        if (_sdkInitialized) return;
        if (!ConsentInformation.CanRequestAds())
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
        ConsentForm.ShowPrivacyOptionsForm(error => RunOnMainThread(() =>
        {
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
        string id = RewardedId();
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError("[AdMob] 실제 보상형 광고 단위 ID가 비어 있습니다.");
            SetState(AdState.Unavailable);
            return;
        }

        DestroyRewardedAd();
        SetState(AdState.Loading);
        RewardedAd.Load(id, new AdRequest(), (ad, error) => RunOnMainThread(() =>
        {
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
