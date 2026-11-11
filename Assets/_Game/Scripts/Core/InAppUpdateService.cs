using System;
using System.Collections;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR && !HAKO_DEVICE_TEST
using Google.Play.AppUpdate;
#endif

/// <summary>Optional Play flexible update. Never starts installation without a user action and a saved game.</summary>
public sealed class InAppUpdateService : MonoBehaviour
{
    public enum UpdateState { Hidden, Available, Downloading, Ready, Failed, Installing }
    public static InAppUpdateService Instance { get; private set; }
    public UpdateState State { get; private set; }
    public float DownloadProgress { get; private set; }
    public string FailureKey { get; private set; } = "update.failed";
    public bool Dismissed { get; private set; }
    public bool Busy => _busy;
    public bool HomeAvailable { get; set; }
    private bool _busy;
    private bool _needsCheck = true;
    private float _nextCheck;
    private int _checkFailures;
    private bool _unavailableOnDevice;
    private const float CHECK_INTERVAL = 30f;
    private const float OPERATION_TIMEOUT = 30f;

    public static bool Supported
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR && !HAKO_DEVICE_TEST
            return Application.identifier == "com.tnbsoft.hako";
#else
            return false;
#endif
        }
    }

    public static void Create()
    {
        if (Instance != null || !Supported) return;
        new GameObject("[InAppUpdateService]").AddComponent<InAppUpdateService>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private bool CanPrompt => HomeAvailable && Application.isFocused
        && !SceneRouter.IsTransitioning
        && (AdMobService.Instance == null || !AdMobService.Instance.IsPresentingSystemUI);

    public void Dismiss() { Dismissed = true; }

    public void Download()
    {
#if UNITY_ANDROID && !UNITY_EDITOR && !HAKO_DEVICE_TEST
        if (!Supported || _busy || !CanPrompt || _request != null) return;
        if (State != UpdateState.Available && State != UpdateState.Failed) return;
        Dismissed = false;
        Run(Check(true)); // Fresh AppUpdateInfo for every consent attempt.
#endif
    }

    public void Install()
    {
#if UNITY_ANDROID && !UNITY_EDITOR && !HAKO_DEVICE_TEST
        if (!Supported || _busy || !CanPrompt || State != UpdateState.Ready) return;
        bool saved = false;
        try { saved = GameManager.Instance != null && GameManager.Instance.SaveBeforeAppUpdate(); }
        catch (Exception ex) { Debug.LogWarning("[InAppUpdate] save: " + ex.Message); }
        if (!saved)
        {
            FailureKey = "update.save_failed";
            return; // Keep downloaded state so saving/installation can be retried.
        }
        FailureKey = "update.failed";
        Run(Complete());
#endif
    }

    private void OnApplicationPause(bool paused)
    {
        if (!paused) { _needsCheck = true; _checkFailures = 0; }
    }

    private void OnApplicationFocus(bool focused)
    {
        if (focused) { _needsCheck = true; _checkFailures = 0; }
    }

#if UNITY_ANDROID && !UNITY_EDITOR && !HAKO_DEVICE_TEST
    private AppUpdateManager _manager;
    private AppUpdateRequest _request;

    private void Update()
    {
        if (!Supported || _unavailableOnDevice) return;
        if (_request != null && !_busy && State != UpdateState.Installing)
        {
            DownloadProgress = _request.DownloadProgress;
            if (_request.IsDone)
            {
                var finished = _request;
                _request = null;
                if (finished.Error == AppUpdateErrorCode.ErrorUserCanceled
                    || finished.Status == AppUpdateStatus.Canceled)
                {
                    State = UpdateState.Hidden;
                    Dismissed = true;
                }
                else if (finished.Error != AppUpdateErrorCode.NoError)
                    Fail("download: " + finished.Error);
                else if (finished.Status == AppUpdateStatus.Downloaded)
                    Ready();
                else _needsCheck = true;
            }
        }
        if (!_busy && CanPrompt && Time.realtimeSinceStartup >= _nextCheck
            && (_needsCheck || State == UpdateState.Downloading))
            Run(Check(false));
    }

    // Catch JNI/SDK exceptions even after a coroutine yield. They must not break the game.
    private void Run(IEnumerator operation)
    {
        _busy = true;
        StartCoroutine(Guard(operation));
    }

    private IEnumerator Guard(IEnumerator operation)
    {
        try
        {
            while (true)
            {
                bool more;
                object current = null;
                try { more = operation.MoveNext(); if (more) current = operation.Current; }
                catch (Exception ex) { Fail(ex.GetType().Name + ": " + ex.Message); break; }
                if (!more) break;
                yield return current;
            }
        }
        finally
        {
            (operation as IDisposable)?.Dispose();
            _busy = false;
        }
    }

    private IEnumerator Check(bool startDownload)
    {
        _needsCheck = false;
        _nextCheck = Time.realtimeSinceStartup + CHECK_INTERVAL;
        if (_manager == null) _manager = new AppUpdateManager();
        var operation = _manager.GetAppUpdateInfo();
        float deadline = Time.realtimeSinceStartup + OPERATION_TIMEOUT;
        while (!operation.IsDone)
        {
            if (Time.realtimeSinceStartup >= deadline)
            {
                CheckFailed("check timeout", startDownload);
                yield break; // No callback subscribed: a late result cannot change our state.
            }
            yield return null;
        }
        if (!operation.IsSuccessful)
        {
            _unavailableOnDevice = operation.Error == AppUpdateErrorCode.ErrorApiNotAvailable
                || operation.Error == AppUpdateErrorCode.ErrorPlayStoreNotFound
                || operation.Error == AppUpdateErrorCode.ErrorAppNotOwned;
            CheckFailed("check: " + operation.Error, startDownload);
            if (_unavailableOnDevice) State = UpdateState.Hidden;
            yield break;
        }
        var info = operation.GetResult();
        if (info == null) { CheckFailed("empty update info", startDownload); yield break; }
        _checkFailures = 0;
        if (info.AppUpdateStatus == AppUpdateStatus.Downloaded)
        {
            Ready();
            yield break;
        }
        if (info.AppUpdateStatus == AppUpdateStatus.Pending || info.AppUpdateStatus == AppUpdateStatus.Downloading)
        {
            State = UpdateState.Downloading;
            DownloadProgress = info.TotalBytesToDownload == 0 ? 0f
                : (float)((double)info.BytesDownloaded / info.TotalBytesToDownload);
            yield break; // Observe an existing download; do not request consent again.
        }
        if (_request != null) yield break; // Consent window/download still owned by the SDK.
        var options = AppUpdateOptions.FlexibleAppUpdateOptions();
        if (info.UpdateAvailability != UpdateAvailability.UpdateAvailable || !info.IsUpdateTypeAllowed(options))
        {
            State = UpdateState.Hidden;
            yield break;
        }
        State = UpdateState.Available;
        if (!startDownload || !CanPrompt) yield break;
        _request = _manager.StartUpdate(info, options);
        State = UpdateState.Downloading;
        DownloadProgress = 0;
    }

    private IEnumerator Complete()
    {
        State = UpdateState.Installing;
        var operation = _manager.CompleteUpdate();
        float deadline = Time.realtimeSinceStartup + OPERATION_TIMEOUT;
        while (!operation.IsDone && Time.realtimeSinceStartup < deadline) yield return null;
        if (operation.IsDone && !operation.IsSuccessful)
        {
            State = UpdateState.Ready;
            FailureKey = "update.install_failed";
            Debug.LogWarning("[InAppUpdate] install: " + operation.Error);
            yield break;
        }
        // Play normally closes the process. Do not leave an input blocker forever if it does not.
        while (Time.realtimeSinceStartup < deadline) yield return null;
        State = UpdateState.Ready;
        FailureKey = "update.install_failed";
    }

    private void Ready()
    {
        if (State != UpdateState.Ready) Dismissed = false;
        State = UpdateState.Ready;
        DownloadProgress = 1;
    }

    private void CheckFailed(string detail, bool show)
    {
        Fail(detail, show);
        // At most two automatic retries; foreground entry gets a fresh budget.
        _needsCheck = ++_checkFailures < 3 && !_unavailableOnDevice;
    }

    private void Fail(string detail, bool show = true)
    {
        Debug.LogWarning("[InAppUpdate] " + detail);
        FailureKey = "update.failed";
        if (State == UpdateState.Installing)
        {
            State = UpdateState.Ready;
            FailureKey = "update.install_failed";
        }
        else if (show) State = UpdateState.Failed;
        _nextCheck = Time.realtimeSinceStartup + CHECK_INTERVAL;
    }
#endif

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
