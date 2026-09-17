using System.Collections.Generic;

public enum GeckoEventType { GrowthUp, MoltSuccess, MoltFail, BondUp, MorphReveal }

/// <summary>화면에 보여줘야 할 게코 사건 하나 (성장·허물). 발생 시점의 값을 복사해 둔다.</summary>
public struct GeckoEvent
{
    public GeckoEventType type;
    public string geckoId;
    public string geckoName;
    public int    growthStage;   // 발생 직후 단계
    public int    moltCount;     // 발생 직후 누적 허물 횟수
    public int    rewardCoin;    // 어덜트 달성 때 실제로 받은 보상 (그 밖에는 0)
    public int    rewardGem;
    public int    adultsRaised;  // 어덜트 달성 직후 키운 어덜트 수 (그 밖에는 0)
    public int    bondLevel;     // 유대 레벨이 오른 뒤 레벨 (BondUp만)
    public string morphId;       // 드러난 모프 (MorphReveal만)
    public bool   morphFirst;    // 처음 얻은 모프
}

/// <summary>
/// 성장·허물 사건을 쌓아 두는 대기열.
///
/// 허물·성장 판정은 대부분 앱 시작(Boot 씬)의 시간 보정 중에 일어나서, 그 순간에는 홈 화면이 없다.
/// 그래서 사건을 여기 모아 두었다가 홈 화면이 준비되면 하나씩 꺼내 연출한다.
/// 홈 화면이 떠 있는 동안 생긴 사건도 똑같이 이 대기열을 거친다 (연출끼리 겹치지 않게).
/// </summary>
public class GeckoEventQueue
{
    private const int MAX_EVENTS = 8;   // 오래 쌓여도 연출이 끝없이 이어지지 않게

    private readonly Queue<GeckoEvent> _queue = new Queue<GeckoEvent>();
    private readonly GeckoManager      _gecko;

    public int Count => _queue.Count;

    public GeckoEventQueue(GeckoManager gecko)
    {
        _gecko = gecko;
        gecko.OnGrowthUp    += g => Enqueue(GeckoEventType.GrowthUp,    g);
        gecko.OnMoltSuccess += g => Enqueue(GeckoEventType.MoltSuccess, g);
        gecko.OnMoltFail    += g => Enqueue(GeckoEventType.MoltFail,    g);
        gecko.OnBondLevelUp += g => Enqueue(GeckoEventType.BondUp,      g);
        gecko.OnMorphRevealed += g => Enqueue(GeckoEventType.MorphReveal, g);
    }

    public bool TryDequeue(out GeckoEvent e)
    {
        if (_queue.Count > 0)
        {
            e = _queue.Dequeue();
            return true;
        }
        e = default;
        return false;
    }

    /// <summary>
    /// 아직 연출하지 않은 성장 사건 중 이 게코의 가장 이른 것. 있으면 성장 전 단계를 돌려준다.
    /// 홈 진입 시 게코를 성장 전 크기로 보여줬다가 연출과 함께 커지게 하는 데 쓴다.
    /// </summary>
    public bool TryGetPendingGrowthFrom(string geckoId, out int fromStage)
    {
        foreach (var e in _queue)
        {
            if (e.type == GeckoEventType.GrowthUp && e.geckoId == geckoId)
            {
                fromStage = e.growthStage - 1;
                return true;
            }
        }
        fromStage = -1;
        return false;
    }

    /// <summary>이 게코의 이 종류 사건이 아직 연출 전인가 (모프 연출 전에는 기본색으로 보여 준다)</summary>
    public bool HasPending(string geckoId, GeckoEventType type)
    {
        foreach (var e in _queue)
            if (e.type == type && e.geckoId == geckoId) return true;
        return false;
    }

    private void Enqueue(GeckoEventType type, GeckoData g)
    {
        if (g == null) return;
        if (_queue.Count >= MAX_EVENTS) _queue.Dequeue();
        bool adult = type == GeckoEventType.GrowthUp && GeckoManager.IsAdult(g);
        bool bond  = type == GeckoEventType.BondUp;
        bool morph = type == GeckoEventType.MorphReveal;
        _queue.Enqueue(new GeckoEvent
        {
            type         = type,
            geckoId      = g.id,
            geckoName    = g.name,
            growthStage  = g.growthStage,
            moltCount    = g.moltCount,
            // 보상은 이벤트 직전에 GeckoManager가 채운 값 (EvaluateGrowth · CheckBondLevel)
            rewardCoin   = adult ? _gecko.LastAdultRewardCoin : bond ? _gecko.LastBondCoin : morph ? _gecko.LastMorph.coin : 0,
            rewardGem    = adult ? _gecko.LastAdultRewardGem  : bond ? _gecko.LastBondGem  : morph ? _gecko.LastMorph.gem  : 0,
            adultsRaised = adult ? _gecko.LastAdultsRaised : 0,
            bondLevel    = bond  ? _gecko.LastBondLevel    : 0,
            morphId      = morph ? _gecko.LastMorph.morphId : null,
            morphFirst   = morph && _gecko.LastMorph.first,
        });
    }
}
