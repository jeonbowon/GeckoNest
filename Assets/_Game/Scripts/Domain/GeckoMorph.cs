using System.Collections.Generic;
using UnityEngine;

public enum MorphRarity { Common = 0, Rare = 1, VeryRare = 2 }

/// <summary>모프 무늬 — 임시 그림 위에 코드로 얹는 점 (GeckoRig.SetMorph)</summary>
public enum MorphPattern { None, Spots, Blotches, Stripes }

/// <summary>모프 하나 — 이름은 번역표 morph.{id}. 최종 그림이 생기면 skinPath(Resources)에 전용 그림을 둔다</summary>
public readonly struct MorphDef
{
    public readonly string       id;
    public readonly string       speciesId;
    public readonly MorphRarity  rarity;
    public readonly Color        body;      // 몸통·머리·다리·꼬리에 곱하는 색
    public readonly MorphPattern pattern;
    public readonly Color        patternColor;

    public MorphDef(string id, string speciesId, MorphRarity rarity, Color body, MorphPattern pattern, Color patternColor)
    {
        this.id = id; this.speciesId = speciesId; this.rarity = rarity;
        this.body = body; this.pattern = pattern; this.patternColor = patternColor;
    }

    public bool   IsValid => !string.IsNullOrEmpty(id);
    public string NameKey => "morph." + id;
}

/// <summary>
/// 모프(무늬) 수집 (2026-09-17, 어덜트 이후 6번). 어덜트가 되는 순간 등급을 뽑아 정한다 (흔함 70 · 희귀 25 · 아주 희귀 5).
/// 그 전에는 종별 기본색. 처음 얻은 모프는 도감(ProgressData.morphIds)에 남고 보상을 준다.
/// 최종 그림 전까지는 색 곱하기 + 점무늬(임시) — 그림을 이미 색이 있는 프록시 위에 곱하므로 밝은 모프는 비슷하게만 보인다.
/// </summary>
public static class GeckoMorph
{
    public static readonly float[] RARITY_WEIGHT = { 70f, 25f, 5f };   // [TBD] 등급 확률 (희귀는 그 등급 모프끼리 나눈다)

    // 처음 얻었을 때 보상 [TBD]
    public static readonly (int coin, int gem)[] FIRST_REWARD = { (50, 0), (150, 0), (0, 5) };

    private static readonly Color WHITE = Color.white;
    private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

    // 종별 기본색 (어덜트 전) [TBD]
    private static readonly Dictionary<string, Color> BASE = new Dictionary<string, Color>
    {
        ["crested"]  = WHITE,                    // 프록시 그림 그대로 (주황 갈색)
        ["leopard"]  = C(1.00f, 0.93f, 0.55f),   // 노랑
        ["gargoyle"] = C(0.76f, 0.72f, 0.68f),   // 회갈색
    };

    public static readonly MorphDef[] ALL =
    {
        new MorphDef("crested_normal",    "crested",  MorphRarity.Common,   WHITE,                   MorphPattern.None,     Color.clear),
        new MorphDef("crested_harlequin", "crested",  MorphRarity.Rare,     WHITE,                   MorphPattern.Blotches, C(1.00f, 0.95f, 0.80f, 0.85f)),
        new MorphDef("crested_red",       "crested",  MorphRarity.Rare,     C(1.00f, 0.58f, 0.50f),  MorphPattern.None,     Color.clear),
        new MorphDef("crested_dalmatian", "crested",  MorphRarity.VeryRare, C(1.00f, 0.97f, 0.90f),  MorphPattern.Spots,    C(0.10f, 0.08f, 0.08f, 0.90f)),

        new MorphDef("leopard_normal",    "leopard",  MorphRarity.Common,   C(1.00f, 0.93f, 0.55f),  MorphPattern.Spots,    C(0.15f, 0.10f, 0.05f, 0.85f)),
        new MorphDef("leopard_tangerine", "leopard",  MorphRarity.Rare,     C(1.00f, 0.70f, 0.35f),  MorphPattern.Spots,    C(0.18f, 0.10f, 0.05f, 0.80f)),
        new MorphDef("leopard_albino",    "leopard",  MorphRarity.Rare,     C(1.00f, 0.86f, 0.80f),  MorphPattern.Spots,    C(0.80f, 0.55f, 0.45f, 0.70f)),
        new MorphDef("leopard_blizzard",  "leopard",  MorphRarity.VeryRare, C(0.96f, 0.96f, 0.93f),  MorphPattern.None,     Color.clear),

        new MorphDef("gargoyle_normal",   "gargoyle", MorphRarity.Common,   C(0.76f, 0.72f, 0.68f),  MorphPattern.Blotches, C(0.30f, 0.28f, 0.26f, 0.60f)),
        new MorphDef("gargoyle_red",      "gargoyle", MorphRarity.Rare,     C(0.82f, 0.62f, 0.58f),  MorphPattern.Stripes,  C(0.75f, 0.20f, 0.15f, 0.85f)),
        new MorphDef("gargoyle_orange",   "gargoyle", MorphRarity.Rare,     C(0.86f, 0.76f, 0.66f),  MorphPattern.Blotches, C(1.00f, 0.55f, 0.15f, 0.85f)),
        new MorphDef("gargoyle_white",    "gargoyle", MorphRarity.VeryRare, C(0.95f, 0.93f, 0.90f),  MorphPattern.None,     Color.clear),
    };

    public static Color BaseColor(string speciesId)
        => speciesId != null && BASE.TryGetValue(speciesId, out var c) ? c : WHITE;

    public static List<MorphDef> ForSpecies(string speciesId)
    {
        var list = new List<MorphDef>();
        foreach (var m in ALL) if (m.speciesId == speciesId) list.Add(m);
        return list;
    }

    public static MorphDef Find(string id)
    {
        if (!string.IsNullOrEmpty(id))
            foreach (var m in ALL) if (m.id == id) return m;
        return default;
    }

    /// <summary>등급 → 그 등급 모프 중 하나. 종에 모프가 없으면 default</summary>
    public static MorphDef Roll(string speciesId, System.Random rng)
    {
        var list = ForSpecies(speciesId);
        if (list.Count == 0) return default;
        rng ??= new System.Random();

        // 이 종에 있는 등급만 가중치에 넣는다
        double total = 0;
        for (int r = 0; r < RARITY_WEIGHT.Length; r++)
            if (list.Exists(m => (int)m.rarity == r)) total += RARITY_WEIGHT[r];
        double pick = rng.NextDouble() * total;
        int rarity = 0;
        for (int r = 0; r < RARITY_WEIGHT.Length; r++)
        {
            if (!list.Exists(m => (int)m.rarity == r)) continue;
            rarity = r;
            if (pick < RARITY_WEIGHT[r]) break;
            pick -= RARITY_WEIGHT[r];
        }
        var same = list.FindAll(m => (int)m.rarity == rarity);
        return same[rng.Next(same.Count)];
    }

    /// <summary>모프가 드러난 결과 (사건·알림용)</summary>
    public struct Reveal
    {
        public string morphId;
        public bool   first;   // 처음 얻은 모프 (도감에 새로 기록)
        public int    coin, gem;
    }

    /// <summary>
    /// 게코의 모프를 정한다 (이미 있으면 그대로). 도감에 기록하고, reward면 처음 얻은 모프의 보상을 준다.
    /// 저장은 부르는 쪽에서
    /// </summary>
    public static Reveal Assign(PlayerData data, GeckoData g, System.Random rng, bool reward)
    {
        var result = new Reveal();
        if (data == null || g == null) return result;
        if (string.IsNullOrEmpty(g.morphId))
        {
            var m = Roll(g.speciesId, rng);
            if (!m.IsValid) return result;
            g.morphId = m.id;
        }
        result.morphId = g.morphId;
        result.first   = Record(data, g.morphId);
        if (result.first && reward)
        {
            var def = Find(g.morphId);
            var (coin, gem) = FIRST_REWARD[(int)def.rarity];
            data.coin += coin;
            data.gem  += gem;
            result.coin = coin;
            result.gem  = gem;
        }
        return result;
    }

    /// <summary>도감에 모프 기록 — 처음이면 true</summary>
    public static bool Record(PlayerData data, string morphId)
    {
        if (data == null || string.IsNullOrEmpty(morphId)) return false;
        data.progress ??= new ProgressData();
        data.progress.morphIds ??= new List<string>();
        if (data.progress.morphIds.Contains(morphId)) return false;
        data.progress.morphIds.Add(morphId);
        return true;
    }

    public static bool Has(PlayerData data, string morphId)
        => data != null && data.progress != null && data.progress.morphIds != null && data.progress.morphIds.Contains(morphId);

    /// <summary>게코가 지금 보여야 하는 모습 — 모프가 드러났으면 그 모프, 아니면 종별 기본색 (무늬 없음)</summary>
    public static MorphDef LookOf(GeckoData g, bool revealed)
    {
        if (g == null) return default;
        if (revealed)
        {
            var m = Find(g.morphId);
            if (m.IsValid) return m;
        }
        return new MorphDef("", g.speciesId, MorphRarity.Common, BaseColor(g.speciesId), MorphPattern.None, Color.clear);
    }

    // 프록시 그림의 대표색 (주황 갈색) — 모프 색을 곱해 도감 색 점으로 보여 준다
    private static readonly Color PROXY_TONE = C(0.88f, 0.60f, 0.36f);

    /// <summary>도감 칸의 색 점 — 무늬가 있으면 몸 색과 무늬 색을 반씩</summary>
    public static Color SwatchColor(MorphDef m)
    {
        Color body = m.body * PROXY_TONE;
        body.a = 1f;
        if (m.pattern == MorphPattern.None) return body;
        Color mix = Color.Lerp(body, m.patternColor, 0.45f);
        mix.a = 1f;
        return mix;
    }

    /// <summary>무늬 위치를 고정하는 씨앗 — 게코 id로 (다시 켜도 같은 자리)</summary>
    public static int SeedOf(GeckoData g)
    {
        if (g == null || string.IsNullOrEmpty(g.id)) return 0;
        unchecked
        {
            int h = 17;
            foreach (char ch in g.id) h = h * 31 + ch;
            return h;
        }
    }
}
