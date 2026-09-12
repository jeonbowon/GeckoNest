using System;

/// <summary>
/// 임시 효과음·환경음을 코드로 합성한다. 순수 계산이라 Unity 없이도 검증할 수 있다.
/// 실제 소리 파일을 Resources/Audio 에 넣으면 그쪽이 우선이다 (SfxLibrary).
///
/// 음색 방향: 맑은 종소리(배음별 감쇠) · 부드러운 미끄럼음 · 걸러낸 잡음. 날카로운 전자음은 쓰지 않는다.
/// </summary>
public static class SfxSynth
{
    public const int SFX_RATE = 44100;
    public const int AMB_RATE = 22050;

    private static int s_rate = SFX_RATE;

    // ── 효과음 ────────────────────────────────────────────────

    public static float[] Render(Sfx id)
    {
        s_rate = SFX_RATE;
        var rng = new Random(1000 + (int)id);   // 고정 시드 — 매번 같은 소리
        float[] b;
        float peak;

        switch (id)
        {
            case Sfx.Tap:
                b = Buf(0.09f);
                Bell(b, 0f, 1760f, 0.6f, 0.02f, 0.5f);
                Noise(b, rng, 0f, 0.012f, 0.35f, 0.0005f, 0.003f, 1800f, 7000f);
                peak = 0.42f;
                break;

            case Sfx.Pop:
                b = Buf(0.18f);
                Sweep(b, 0f, 0.12f, 380f, 1000f, 1f, 0.002f, 0.04f);
                Bell(b, 0.012f, 1320f, 0.3f, 0.05f, 0.5f);
                peak = 0.55f;
                break;

            case Sfx.Lick:
                b = Buf(0.14f);
                Noise(b, rng, 0f, 0.06f, 1f, 0.002f, 0.016f, 1800f, 5200f);
                Sweep(b, 0.004f, 0.05f, 700f, 1400f, 0.5f, 0.002f, 0.018f);
                peak = 0.5f;
                break;

            case Sfx.Crunch:
                b = Buf(0.36f);
                for (int k = 0; k < 4; k++)
                {
                    float t = k * 0.075f + (float)rng.NextDouble() * 0.015f;
                    Noise(b, rng, t, 0.05f, 1f - k * 0.15f, 0.001f, 0.011f, 900f, 4500f);
                    Sweep(b, t, 0.03f, 190f, 120f, 0.35f, 0.001f, 0.014f);
                }
                peak = 0.5f;
                break;

            case Sfx.Spray:
                b = Buf(0.55f);
                Noise(b, rng, 0f, 0.55f, 1f, 0.03f, 0.15f, 3000f, 11000f);
                Noise(b, rng, 0f, 0.08f, 0.5f, 0.004f, 0.03f, 1500f, 6000f);
                peak = 0.38f;
                break;

            case Sfx.Drip:
                b = Buf(0.2f);
                Sweep(b, 0f, 0.12f, 850f, 2300f, 1f, 0.001f, 0.035f);
                Bell(b, 0.02f, 2300f, 0.15f, 0.05f, 0.3f);
                peak = 0.45f;
                break;

            case Sfx.Heart:
                b = Buf(0.8f);
                Bell(b, 0f,     1318.5f, 0.8f, 0.2f,  0.8f);   // E6
                Bell(b, 0.085f, 1760.0f, 0.9f, 0.28f, 0.8f);   // A6
                peak = 0.5f;
                break;

            case Sfx.Sparkle:
            {
                b = Buf(1.1f);
                float[] notes = { 1567.98f, 1975.53f, 2349.32f, 3135.96f, 2637.02f };   // G6 B6 D7 G7 E7
                for (int i = 0; i < notes.Length; i++)
                    Bell(b, i * 0.05f, notes[i], 0.7f - i * 0.08f, 0.28f, 0.7f);
                Noise(b, rng, 0f, 0.6f, 0.06f, 0.05f, 0.22f, 6000f, 14000f);
                peak = 0.45f;
                break;
            }

            case Sfx.Chime:
            {
                b = Buf(2.4f);
                float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.51f };      // C5 E5 G5 C6 E6
                for (int i = 0; i < notes.Length; i++)
                    Bell(b, i * 0.11f, notes[i], 0.8f, 0.9f, 0.8f);
                Bell(b, 0.55f, 261.63f, 0.3f, 1.2f, 0.3f);                              // 아래 C4로 받쳐준다
                peak = 0.62f;
                break;
            }

            case Sfx.MoltSuccess:
            {
                b = Buf(1.9f);
                Noise(b, rng, 0f, 0.5f, 0.25f, 0.15f, 0.2f, 800f, 4000f);               // 껍질 벗겨지는 '스르륵'
                float[] notes = { 783.99f, 987.77f, 1174.66f, 1567.98f };                // G5 B5 D6 G6
                for (int i = 0; i < notes.Length; i++)
                    Bell(b, 0.12f + i * 0.08f, notes[i], 0.8f, 0.7f, 0.8f);
                Bell(b, 0.55f, 3135.96f, 0.3f, 0.4f, 0.5f);
                peak = 0.62f;
                break;
            }

            case Sfx.MoltFail:
                b = Buf(1.1f);
                Bell(b, 0f,   659.25f, 0.8f, 0.35f, 0.4f);   // E5
                Bell(b, 0.2f, 523.25f, 0.8f, 0.45f, 0.4f);   // C5 — 슬프지 않게, 담담하게
                peak = 0.45f;
                break;

            case Sfx.Coin:
                b = Buf(0.6f);
                Bell(b, 0f,     987.77f, 0.7f, 0.12f, 1.2f);   // B5
                Bell(b, 0.075f, 1318.5f, 0.9f, 0.3f,  1.2f);   // E6
                peak = 0.5f;
                break;

            case Sfx.Refuse:
                b = Buf(0.34f);
                Sweep(b, 0f, 0.28f, 560f, 360f, 1f, 0.01f, 0.12f, 9f, 0.03f, 0.15f);
                peak = 0.45f;
                break;

            case Sfx.Annoyed:
                b = Buf(0.46f);
                Sweep(b, 0f,    0.15f, 470f, 330f, 1f, 0.006f, 0.07f, 0f, 0f, 0.3f);
                Sweep(b, 0.17f, 0.22f, 400f, 260f, 1f, 0.006f, 0.09f, 0f, 0f, 0.3f);
                peak = 0.45f;
                break;

            case Sfx.Boing:
                b = Buf(0.42f);
                Sweep(b, 0f, 0.36f, 170f, 640f, 1f, 0.004f, 0.14f, 14f, 0.06f, 0f);
                peak = 0.5f;
                break;

            case Sfx.Error:
                b = Buf(0.5f);
                Bell(b, 0f,    440f,    0.8f, 0.14f, 0.3f);   // A4
                Bell(b, 0.11f, 349.23f, 0.8f, 0.2f,  0.3f);   // F4
                peak = 0.42f;
                break;

            default:
                b = Buf(0.01f);
                peak = 0f;
                break;
        }

        Normalize(b, peak);
        FadeTail(b, 0.008f);
        return b;
    }

    // ── 환경음 (배경음 대신) ──────────────────────────────────

    /// <summary>
    /// 테라리움 환경음 24초 반복 — 잎사귀 사이 공기, 가끔 떨어지는 물방울, 먼 풀벌레.
    /// 끝과 처음을 겹쳐 이어 붙여 반복할 때 이음매가 들리지 않는다.
    /// </summary>
    public static float[] RenderAmbience()
    {
        s_rate = AMB_RATE;
        const float LOOP = 24f;
        const float TAIL = 2f;
        var rng = new Random(7);
        var b = Buf(LOOP + TAIL);

        // 1) 공기·잎사귀 — 브라운 잡음을 걸러 천천히 숨 쉬듯 크기를 바꾼다
        float brown = 0f, lp = 0f;
        float aLp = OnePole(700f);
        for (int i = 0; i < b.Length; i++)
        {
            float x = (float)(rng.NextDouble() * 2.0 - 1.0);
            brown = brown * 0.995f + x * 0.03f;
            lp += aLp * (brown - lp);
            float t = i / (float)s_rate;
            float breathe = 0.7f + 0.3f * (float)(Math.Sin(2.0 * Math.PI * t / 8.0 + 1.3) * Math.Sin(2.0 * Math.PI * t / 13.0));
            b[i] += lp * breathe;
        }

        // 2) 물방울 — 잎끝에서 똑, 짧은 메아리
        for (int k = 0; k < 12; k++)
        {
            float t  = (float)rng.NextDouble() * LOOP;
            float f0 = 900f + (float)rng.NextDouble() * 500f;
            float a  = 0.10f + (float)rng.NextDouble() * 0.08f;
            Sweep(b, t,         0.05f, f0,        f0 * 2.3f, a,         0.001f, 0.03f);
            Sweep(b, t + 0.13f, 0.04f, f0 * 1.1f, f0 * 2.5f, a * 0.3f,  0.001f, 0.025f);
        }

        // 3) 먼 풀벌레 — 아주 작게
        for (int k = 0; k < 4; k++)
        {
            float t0 = (float)rng.NextDouble() * LOOP;
            for (int p = 0; p < 8; p++)
                Sweep(b, t0 + p * 0.035f, 0.018f, 4200f, 4000f, 0.02f, 0.002f, 0.006f);
        }

        // 반복 이음매 — 뒤에 더 만든 TAIL을 앞부분에 겹친다
        int n    = (int)(LOOP * s_rate);
        int fade = (int)(TAIL * s_rate);
        var loop = new float[n];
        Array.Copy(b, loop, n);
        for (int i = 0; i < fade; i++)
        {
            float u = i / (float)fade;
            loop[i] = b[i] * u + b[n + i] * (1f - u);
        }

        Normalize(loop, 0.5f);
        s_rate = SFX_RATE;
        return loop;
    }

    // ── 합성 재료 ─────────────────────────────────────────────

    private static float[] Buf(float seconds) => new float[Math.Max(1, (int)Math.Ceiling(seconds * s_rate))];

    private static float OnePole(float cutoffHz)
        => cutoffHz <= 0f ? 0f : 1f - (float)Math.Exp(-2.0 * Math.PI * cutoffHz / s_rate);

    private static float Env(float t, float attack, float decay)
        => t < attack ? t / attack : (float)Math.Exp(-(t - attack) / decay);

    /// <summary>맑은 종소리. 높은 배음일수록 빨리 사라져서 '띵' 하고 부드럽게 남는다.</summary>
    private static void Bell(float[] b, float start, float freq, float amp, float decay, float bright = 1f)
    {
        Partial(b, start, freq,        amp,                  decay);
        Partial(b, start, freq * 2f,   amp * 0.28f * bright, decay * 0.55f);
        Partial(b, start, freq * 3f,   amp * 0.10f * bright, decay * 0.35f);
        Partial(b, start, freq * 5.4f, amp * 0.06f * bright, decay * 0.12f);
    }

    // 사인은 재귀식(y[n] = 2cos(w)·y[n-1] − y[n-2]), 감쇠는 곱셈으로 — 모바일에서 미리 만들 때 멈칫하지 않게
    private static void Partial(float[] b, float start, float freq, float amp, float decay)
    {
        if (freq >= s_rate * 0.45f || amp <= 0f) return;   // 표현 가능한 주파수만
        const float ATTACK = 0.003f;
        int s0 = (int)(start * s_rate);
        if (s0 >= b.Length) return;
        int n      = Math.Min(b.Length - s0, (int)((ATTACK + decay * 7f) * s_rate));
        int attack = Math.Max(1, (int)(ATTACK * s_rate));

        double w  = 2.0 * Math.PI * freq / s_rate;
        double k  = 2.0 * Math.Cos(w);
        double y1 = Math.Sin(-w), y2 = Math.Sin(-2.0 * w);
        double env = 1.0, fall = Math.Exp(-1.0 / (decay * s_rate));

        for (int i = 0; i < n; i++)
        {
            double y = k * y1 - y2;
            y2 = y1;
            y1 = y;

            double e;
            if (i < attack) e = i / (double)attack;
            else
            {
                e = env;
                env *= fall;
            }
            b[s0 + i] += (float)(amp * e * y);
        }
    }

    /// <summary>음높이가 미끄러지는 소리 (뿅 · 보잉 · 흥). harm2 = 둘째 배음 섞는 양</summary>
    private static void Sweep(float[] b, float start, float dur, float f0, float f1, float amp,
                              float attack, float decay, float vibHz = 0f, float vibDepth = 0f, float harm2 = 0f)
    {
        int s0 = (int)(start * s_rate);
        if (s0 >= b.Length || dur <= 0f) return;
        int n = Math.Min(b.Length - s0, (int)(dur * s_rate));
        double phase = 0.0;
        double ratio = f1 / f0;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)s_rate;
            double f = f0 * Math.Pow(ratio, t / dur);
            if (vibHz > 0f) f *= 1.0 + vibDepth * Math.Sin(2.0 * Math.PI * vibHz * t);
            phase += 2.0 * Math.PI * f / s_rate;

            float env = Env(t, attack, decay);
            float end = Math.Min(1f, (n - i) / (s_rate * 0.004f));   // 끝 4ms 페이드 — 딸깍 방지
            b[s0 + i] += amp * env * end * (float)(Math.Sin(phase) + harm2 * Math.Sin(2.0 * phase));
        }
    }

    /// <summary>대역을 거른 잡음 (칙 · 바삭 · 쏴). lowCut 아래와 highCut 위를 줄인다.</summary>
    private static void Noise(float[] b, Random rng, float start, float dur, float amp,
                              float attack, float decay, float lowCutHz, float highCutHz)
    {
        int s0 = (int)(start * s_rate);
        if (s0 >= b.Length || dur <= 0f) return;
        int n = Math.Min(b.Length - s0, (int)(dur * s_rate));
        float aHi = OnePole(highCutHz);
        float aLo = OnePole(lowCutHz);
        float y1 = 0f, y2 = 0f;
        for (int i = 0; i < n; i++)
        {
            float x = (float)(rng.NextDouble() * 2.0 - 1.0);
            y1 += aHi * (x - y1);
            y2 += aLo * (y1 - y2);
            float t = i / (float)s_rate;
            b[s0 + i] += amp * Env(t, attack, decay) * (y1 - y2);
        }
    }

    private static void Normalize(float[] b, float peak)
    {
        float max = 0f;
        for (int i = 0; i < b.Length; i++) max = Math.Max(max, Math.Abs(b[i]));
        if (max < 1e-6f) return;
        float k = peak / max;
        for (int i = 0; i < b.Length; i++) b[i] *= k;
    }

    private static void FadeTail(float[] b, float seconds)
    {
        int n = Math.Min(b.Length, (int)(seconds * s_rate));
        for (int i = 0; i < n; i++) b[b.Length - 1 - i] *= i / (float)n;
    }
}
