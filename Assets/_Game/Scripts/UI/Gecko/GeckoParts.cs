using System.Collections.Generic;

// ── 파츠 ──────────────────────────────────────────────────────
// 숫자 순서 = 그리는 순서(뒤 → 앞). 파츠 분리 지시서의 레이어 구성과 같다.
// 단, 접지 그림자는 지시서와 달리 맨 뒤에 둔다(게코를 덮으면 안 되므로).
public enum GeckoPartId
{
    Shadow       = 0,
    Tail         = 1,
    LegBackFar   = 2,
    LegFrontFar  = 3,
    Body         = 4,
    ShedPatch    = 5,
    LegBackNear  = 6,
    LegFrontNear = 7,
    Head         = 8,
    EyeL         = 9,
    EyeR         = 10,
    Mouth        = 11,
    Tongue1      = 12,
    Tongue2      = 13,
}

// ── 표정 ──────────────────────────────────────────────────────
public enum GeckoEye
{
    Open, LookLeft, LookRight, LookUp, Closed, Sleepy, Happy, Surprised, Sparkle,
}

public enum GeckoMouth
{
    Closed, Smile, OpenSmall, OpenWide, Chew, Drink, Surprised, Frown,
}

// ── 동작 ──────────────────────────────────────────────────────
// CLAUDE.md 애니메이션 트리거 이름을 그대로 쓴다.
// Idle_Breath(상시)와 Sleepy_Slow(상태)는 동작이 아니라 GeckoMood로 처리한다.
public enum GeckoAction
{
    None,
    Tongue_Lick,        // 입술 핥기 — 일정 확률로 눈 핥기로 바뀐다
    Tongue_EyeLick,     // 혀로 눈 닦기 (시그니처)
    Tongue_FeedCatch,
    Tongue_Drink,
    Happy_LookUp,
    Angry_TailFlick,
    Molt_Start,
    Molt_Finish,
    LevelUp_Pulse,
    Pet_Reaction,
    Surprise,
    Blink_Short,        // 눈꺼풀 있는 종 전용 (크레스티드는 기본 꺼짐)
    Jump,
}

public enum GeckoMood { Normal, Happy, Sleepy, Angry }

public static class GeckoParts
{
    public const int Count = 14;

    // 부모가 먼저 계산되도록 정렬한 순서 (그리는 순서와 다름)
    public static readonly GeckoPartId[] SolveOrder =
    {
        GeckoPartId.Shadow, GeckoPartId.Body,
        GeckoPartId.Tail, GeckoPartId.LegBackFar, GeckoPartId.LegFrontFar,
        GeckoPartId.ShedPatch, GeckoPartId.LegBackNear, GeckoPartId.LegFrontNear,
        GeckoPartId.Head, GeckoPartId.EyeL, GeckoPartId.EyeR, GeckoPartId.Mouth,
        GeckoPartId.Tongue1, GeckoPartId.Tongue2,
    };

    // 관절 부모. -1 = 뿌리
    private static readonly int[] s_parent =
    {
        -1,                         // Shadow
        (int)GeckoPartId.Body,      // Tail
        (int)GeckoPartId.Body,      // LegBackFar
        (int)GeckoPartId.Body,      // LegFrontFar
        -1,                         // Body
        (int)GeckoPartId.Body,      // ShedPatch
        (int)GeckoPartId.Body,      // LegBackNear
        (int)GeckoPartId.Body,      // LegFrontNear
        (int)GeckoPartId.Body,      // Head
        (int)GeckoPartId.Head,      // EyeL
        (int)GeckoPartId.Head,      // EyeR
        (int)GeckoPartId.Head,      // Mouth
        (int)GeckoPartId.Head,      // Tongue1
        (int)GeckoPartId.Tongue1,   // Tongue2
    };

    // PSD 레이어 이름 — 파츠 분리 지시서 03번 표와 글자 단위로 일치해야 한다
    private static readonly string[] s_layerNames =
    {
        "shadow_contact", "tail", "leg_back_far", "leg_front_far", "body", "shed_patch",
        "leg_back_near", "leg_front_near", "head", "eye_l", "eye_r", "mouth",
        "tongue_01", "tongue_02",
    };

    private static readonly string[] s_eyeNames =
    {
        "eye_open", "eye_look_left", "eye_look_right", "eye_look_up", "eye_closed",
        "eye_sleepy", "eye_happy", "eye_surprised", "eye_sparkle",
    };

    private static readonly string[] s_mouthNames =
    {
        "mouth_closed", "mouth_smile", "mouth_open_small", "mouth_open_wide",
        "mouth_chew", "mouth_drink", "mouth_surprised", "mouth_frown",
    };

    public static int ParentOf(GeckoPartId id) => s_parent[(int)id];
    public static string LayerName(GeckoPartId id) => s_layerNames[(int)id];
    public static string EyeSpriteName(GeckoEye e) => s_eyeNames[(int)e];
    public static string MouthSpriteName(GeckoMouth m) => s_mouthNames[(int)m];

    public static bool TryParseLayer(string name, out GeckoPartId id)
        => TryFind(s_layerNames, name, out id);

    public static bool TryParseEye(string name, out GeckoEye e)
        => TryFind(s_eyeNames, name, out e);

    public static bool TryParseMouth(string name, out GeckoMouth m)
        => TryFind(s_mouthNames, name, out m);

    private static bool TryFind<T>(IReadOnlyList<string> table, string name, out T value) where T : System.Enum
    {
        string key = Normalize(name);
        for (int i = 0; i < table.Count; i++)
        {
            if (table[i] == key)
            {
                value = (T)System.Enum.ToObject(typeof(T), i);
                return true;
            }
        }
        value = default;
        return false;
    }

    // "Eye_L", "eye-l", "eye l" 모두 "eye_l"로 취급
    public static string Normalize(string name)
        => string.IsNullOrEmpty(name) ? "" : name.Trim().ToLowerInvariant().Replace('-', '_').Replace(' ', '_');
}
