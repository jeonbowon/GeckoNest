using UnityEngine;

public class GeckoAnimatorController : MonoBehaviour
{
    [SerializeField] private Animator _anim;

    private float _tongueTimer;
    private float _blinkTimer;

    private void Awake()
    {
        if (_anim == null)
            _anim = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        GameManager.Instance.Gecko.OnStateChanged += OnStateChanged;
        GameManager.Instance.Gecko.OnMoltSuccess  += _ => _anim.SetTrigger("Molt_Finish");
        GameManager.Instance.Gecko.OnGrowthUp     += _ => _anim.SetTrigger("LevelUp_Pulse");
        ResetTimers();
    }

    private void OnDisable()
    {
        if (GameManager.Instance?.Gecko == null) return;
        GameManager.Instance.Gecko.OnStateChanged -= OnStateChanged;
        GameManager.Instance.Gecko.OnMoltSuccess  -= _ => _anim.SetTrigger("Molt_Finish");
        GameManager.Instance.Gecko.OnGrowthUp     -= _ => _anim.SetTrigger("LevelUp_Pulse");
    }

    private void Update()
    {
        HandleTongueLick();
        HandleBlink();
        PollEmotionState();
    }

    // ── 혀 내밀기 (4~8초 랜덤) ────────────────────────────────

    private void HandleTongueLick()
    {
        _tongueTimer -= Time.deltaTime;
        if (_tongueTimer > 0f) return;

        // 속도 변동으로 기계적 느낌 제거
        _anim.speed = Random.Range(0.9f, 1.1f);
        _anim.SetTrigger("Tongue_Lick");
        _tongueTimer = Random.Range(4f, 8f);
    }

    // ── 눈 깜빡임 (3~7초 랜덤) ────────────────────────────────

    private void HandleBlink()
    {
        _blinkTimer -= Time.deltaTime;
        if (_blinkTimer > 0f) return;

        _anim.speed = 1f;
        _anim.SetTrigger("Blink_Short");
        _blinkTimer = Random.Range(3f, 7f);
    }

    // ── 감정 상태 폴링 ────────────────────────────────────────

    private void PollEmotionState()
    {
        var g = GameManager.Instance?.GetSelectedGecko();
        if (g == null) return;

        _anim.SetBool("IsHappy",  g.mood > 70f && g.affection > 50f);
        _anim.SetBool("IsSleepy", g.mood < 35f);
        _anim.SetBool("IsAngry",  g.hunger < 20f && g.mood < 30f);

        // 허물 배지 트리거 — 80 이상 진입 시 Molt_Start
        _anim.SetBool("IsMolting", g.moltProgress >= 80f);
    }

    // ── 이벤트 수신 ───────────────────────────────────────────

    private void OnStateChanged(GeckoData g)
    {
        // FeedGecko 직후 호출 — FeedCatch 트리거는 HomeUIController에서 발동
    }

    // ── 공개 메서드 (HomeUIController에서 호출) ───────────────

    public void TriggerFeedCatch() => _anim.SetTrigger("Tongue_FeedCatch");
    public void TriggerDrink()     => _anim.SetTrigger("Tongue_Drink");

    // ── 내부 ──────────────────────────────────────────────────

    private void ResetTimers()
    {
        _tongueTimer = Random.Range(2f, 5f);
        _blinkTimer  = Random.Range(1f, 3f);
    }
}
