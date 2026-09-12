using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 버튼 손맛 — 누르면 쏙 들어가고, 떼면 살짝 튕기며 돌아온다. 누를 때 작은 '톡' 소리.
///
/// 씬의 모든 Button에 자동으로 붙는다 (UIFeelInstaller). 목록처럼 나중에 만들어지는 버튼은
/// 슬롯 스크립트가 Ensure를 부른다. 이미 붙어 있으면 다시 붙이지 않는다.
/// </summary>
[DisallowMultipleComponent]
public class UIPressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerClickHandler
{
    private const float PRESSED   = 0.92f;
    private const float STIFFNESS = 900f;   // 스프링 — 떼었을 때 한 번 살짝 튕긴다
    private const float DAMPING   = 26f;

    [Tooltip("누를 때 '톡' 소리")]
    public bool tapSound = true;

    private Selectable _selectable;
    private Vector3    _baseScale = Vector3.one;
    private float      _scale = 1f, _vel;
    private bool       _pressed, _settled = true, _init;

    public static void Ensure(Selectable s)
    {
        if (s != null && s.GetComponent<UIPressScale>() == null) s.gameObject.AddComponent<UIPressScale>();
    }

    public static void InstallUnder(GameObject root)
    {
        if (root == null) return;
        foreach (var b in root.GetComponentsInChildren<Button>(true)) Ensure(b);
    }

    private void Awake() => Init();

    private void Init()
    {
        if (_init) return;
        _init = true;
        _selectable = GetComponent<Selectable>();
        _baseScale  = transform.localScale;
    }

    private bool Interactable => _selectable == null || _selectable.IsInteractable();

    public void OnPointerDown(PointerEventData e)
    {
        if (!Interactable) return;
        _pressed = true;
        _settled = false;
    }

    public void OnPointerUp(PointerEventData e)   => Release();
    public void OnPointerExit(PointerEventData e) => Release();

    public void OnPointerClick(PointerEventData e)
    {
        if (tapSound && Interactable) AudioManager.PlayVaried(Sfx.Tap, 0.8f, 0.05f);
    }

    private void Release()
    {
        if (!_pressed) return;
        _pressed = false;
        _settled = false;
    }

    private void Update()
    {
        if (_settled) return;

        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        float target = _pressed ? PRESSED : 1f;
        _vel   += (target - _scale) * STIFFNESS * dt;
        _vel   *= Mathf.Exp(-DAMPING * dt);
        _scale += _vel * dt;

        if (!_pressed && Mathf.Abs(_scale - 1f) < 0.001f && Mathf.Abs(_vel) < 0.01f)
        {
            _scale = 1f;
            _vel = 0f;
            _settled = true;
        }
        transform.localScale = _baseScale * _scale;
    }

    private void OnDisable()
    {
        _pressed = false;
        _scale = 1f;
        _vel = 0f;
        _settled = true;
        if (_init) transform.localScale = _baseScale;
    }
}

/// <summary>씬이 열릴 때마다 모든 버튼에 UIPressScale을 붙인다. 씬 파일을 고치지 않아도 된다.</summary>
public static class UIFeelInstaller
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;   // 도메인 리로드를 끈 에디터에서도 한 번만 구독
        SceneManager.sceneLoaded += OnSceneLoaded;
        InstallScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => InstallScene(scene);

    private static void InstallScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        foreach (var root in scene.GetRootGameObjects()) UIPressScale.InstallUnder(root);
    }
}
