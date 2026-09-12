/// <summary>
/// 한국어 조사 붙이기. 이름 끝 글자의 받침에 따라 골라 준다 — "하코가 / 별님이".
/// 한글이 아닌 이름(영문·숫자)은 괄호 형태로 안전하게 쓴다.
/// </summary>
public static class KoreanText
{
    private const int HANGUL_FIRST = 0xAC00;
    private const int HANGUL_LAST  = 0xD7A3;
    private const int JONGSEONG    = 28;      // 받침 종류 수 (0 = 받침 없음)

    /// <summary>주격 조사 — 하코**가** / 별님**이**</summary>
    public static string WithSubject(string name) => Attach(name, "이", "가", "(이)가", "게코가");

    private static string Attach(string name, string withBatchim, string without, string unknown, string fallback)
    {
        if (string.IsNullOrEmpty(name)) return fallback;

        char last = name[name.Length - 1];
        if (last < HANGUL_FIRST || last > HANGUL_LAST) return name + unknown;
        return name + ((last - HANGUL_FIRST) % JONGSEONG != 0 ? withBatchim : without);
    }
}
