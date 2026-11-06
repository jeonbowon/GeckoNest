using UnityEngine;

/// <summary>
/// Colored, rounded illustrations for the home controls. Uses the existing procedural
/// sprite approach without overwriting imported artwork or its GUIDs. Cached once per app.
/// </summary>
public static class HomeButtonArt
{
    private const int Size = 192;
    private static readonly Sprite[] Cache = new Sprite[9];
    private static readonly Color Ink = new Color(0.37f, 0.25f, 0.18f);
    private static readonly Color Cream = new Color(1f, 0.95f, 0.78f);
    private static readonly Color Gold = new Color(1f, 0.73f, 0.28f);
    private static readonly Color Peach = new Color(0.98f, 0.67f, 0.43f);
    private static readonly Color Pink = new Color(0.98f, 0.48f, 0.59f);
    private static readonly Color Mint = new Color(0.40f, 0.76f, 0.56f);
    private static readonly Color Blue = new Color(0.40f, 0.75f, 0.94f);

    public static Sprite Get(int index)
    {
        if (index < 0 || index >= Cache.Length) return null;
        if (Cache[index] != null) return Cache[index];
        var pixels = new Color[Size * Size];
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                var p = new Vector2((x + 0.5f) / Size, (y + 0.5f) / Size);
                pixels[x + y * Size] = Draw(p, index);
            }
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        texture.name = "HomeButtonArt_" + index;
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        Cache[index] = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
        Cache[index].name = texture.name;
        return Cache[index];
    }

    private static Color Draw(Vector2 p, int index)
    {
        Color c = Color.clear;
        switch (index)
        {
            case 0: // Food: fruit and pellets in a smiling bowl.
                Oval(ref c, p, 0.35f, 0.64f, 0.14f, 0.15f, Peach);
                Oval(ref c, p, 0.57f, 0.65f, 0.14f, 0.16f, Gold);
                Oval(ref c, p, 0.73f, 0.60f, 0.10f, 0.10f, Mint);
                Line(ref c, p, 0.54f, 0.78f, 0.64f, 0.87f, 0.035f, Mint);
                Box(ref c, p, 0.5f, 0.39f, 0.34f, 0.22f, 0.12f, Mint);
                Box(ref c, p, 0.5f, 0.58f, 0.38f, 0.055f, 0.04f, Cream);
                Face(ref c, p, 0.5f, 0.36f, 0.8f);
                break;
            case 1: // Water: plump blue drop with a friendly face.
                float drop = Mathf.Min(Ellipse(p, 0.5f, 0.40f, 0.29f, 0.29f),
                    Mathf.Max(Mathf.Abs(p.x - 0.5f) - (0.91f - p.y) * 0.55f, Mathf.Abs(p.y - 0.66f) - 0.25f));
                Shape(ref c, p, drop, Blue);
                Face(ref c, p, 0.5f, 0.37f, 1f);
                Oval(ref c, p, 0.35f, 0.54f, 0.035f, 0.075f, Cream, false);
                break;
            case 2: // Pet: palm carrying a pink heart.
                Box(ref c, p, 0.49f, 0.29f, 0.24f, 0.13f, 0.09f, Peach);
                Line(ref c, p, 0.29f, 0.26f, 0.18f, 0.40f, 0.07f, Peach);
                Line(ref c, p, 0.65f, 0.27f, 0.80f, 0.41f, 0.07f, Peach);
                float heart = Mathf.Min(Ellipse(p, 0.40f, 0.71f, 0.17f, 0.16f), Ellipse(p, 0.62f, 0.71f, 0.17f, 0.16f));
                heart = Mathf.Min(heart, Mathf.Max(Mathf.Abs(p.x - 0.51f) - (p.y - 0.38f) * 0.85f, Mathf.Abs(p.y - 0.56f) - 0.18f));
                Shape(ref c, p, heart, Pink);
                Oval(ref c, p, 0.38f, 0.75f, 0.045f, 0.035f, Cream, false);
                break;
            case 3: // Clean: chunky brush and golden sparkle.
                Line(ref c, p, 0.43f, 0.46f, 0.63f, 0.84f, 0.075f, Mint);
                Box(ref c, p, 0.38f, 0.31f, 0.24f, 0.18f, 0.06f, Gold);
                Box(ref c, p, 0.38f, 0.46f, 0.25f, 0.055f, 0.03f, Peach);
                for (int i = 0; i < 3; i++) Line(ref c, p, 0.26f + i * 0.12f, 0.16f, 0.26f + i * 0.12f, 0.29f, 0.012f, Peach);
                Sparkle(ref c, p, 0.78f, 0.61f, 0.14f);
                break;
            case 4: // Store: candy-colored canopy and warm window.
                Box(ref c, p, 0.5f, 0.40f, 0.31f, 0.27f, 0.05f, Cream);
                Box(ref c, p, 0.5f, 0.37f, 0.17f, 0.17f, 0.035f, Mint);
                Box(ref c, p, 0.5f, 0.72f, 0.37f, 0.12f, 0.06f, Pink);
                for (int i = 0; i < 3; i++) Box(ref c, p, 0.25f + i * 0.25f, 0.70f, 0.055f, 0.10f, 0.035f, Cream);
                Oval(ref c, p, 0.5f, 0.36f, 0.08f, 0.08f, Gold);
                break;
            case 5: // Gecko: wide eyes, peach cheeks and crest.
                for (int i = 0; i < 5; i++) Oval(ref c, p, 0.23f + i * 0.135f, 0.73f + (i == 2 ? 0.07f : 0f), 0.065f, 0.09f, Gold);
                Oval(ref c, p, 0.5f, 0.48f, 0.36f, 0.31f, Peach);
                Oval(ref c, p, 0.34f, 0.57f, 0.115f, 0.15f, Cream);
                Oval(ref c, p, 0.66f, 0.57f, 0.115f, 0.15f, Cream);
                Oval(ref c, p, 0.35f, 0.57f, 0.045f, 0.09f, Ink, false);
                Oval(ref c, p, 0.65f, 0.57f, 0.045f, 0.09f, Ink, false);
                Oval(ref c, p, 0.33f, 0.61f, 0.018f, 0.025f, Color.white, false);
                Oval(ref c, p, 0.63f, 0.61f, 0.018f, 0.025f, Color.white, false);
                Smile(ref c, p, 0.5f, 0.37f, 1f);
                break;
            case 6: // Decor: little glass terrarium, leaf and stone.
                Box(ref c, p, 0.5f, 0.48f, 0.34f, 0.32f, 0.09f, Blue);
                Box(ref c, p, 0.5f, 0.25f, 0.31f, 0.075f, 0.035f, Peach);
                Line(ref c, p, 0.38f, 0.30f, 0.42f, 0.65f, 0.022f, Mint);
                Oval(ref c, p, 0.33f, 0.55f, 0.13f, 0.065f, Mint);
                Oval(ref c, p, 0.49f, 0.66f, 0.12f, 0.065f, Mint);
                Oval(ref c, p, 0.65f, 0.36f, 0.13f, 0.10f, Cream);
                Box(ref c, p, 0.5f, 0.81f, 0.36f, 0.045f, 0.025f, Mint);
                break;
            case 7: // Rewards: wrapped present with a large bow.
                Oval(ref c, p, 0.39f, 0.77f, 0.14f, 0.11f, Pink);
                Oval(ref c, p, 0.63f, 0.77f, 0.14f, 0.11f, Pink);
                Box(ref c, p, 0.5f, 0.38f, 0.31f, 0.23f, 0.06f, Gold);
                Box(ref c, p, 0.5f, 0.62f, 0.35f, 0.075f, 0.035f, Gold);
                Box(ref c, p, 0.5f, 0.40f, 0.06f, 0.24f, 0.015f, Pink);
                Oval(ref c, p, 0.51f, 0.73f, 0.065f, 0.065f, Cream);
                break;
            default: // Settings: soft flower-shaped gear, familiar central opening.
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI / 4f;
                    Oval(ref c, p, 0.5f + Mathf.Cos(a) * 0.27f, 0.5f + Mathf.Sin(a) * 0.27f, 0.115f, 0.115f, Blue);
                }
                Oval(ref c, p, 0.5f, 0.5f, 0.29f, 0.29f, Blue);
                Oval(ref c, p, 0.5f, 0.5f, 0.14f, 0.14f, Cream);
                Oval(ref c, p, 0.5f, 0.5f, 0.065f, 0.065f, Gold, false);
                break;
        }
        return c;
    }

    private static void Face(ref Color c, Vector2 p, float x, float y, float scale)
    {
        Oval(ref c, p, x - 0.10f * scale, y + 0.04f * scale, 0.023f * scale, 0.035f * scale, Ink, false);
        Oval(ref c, p, x + 0.10f * scale, y + 0.04f * scale, 0.023f * scale, 0.035f * scale, Ink, false);
        Smile(ref c, p, x, y, scale);
    }
    private static void Smile(ref Color c, Vector2 p, float x, float y, float scale)
    {
        Line(ref c, p, x - 0.065f * scale, y, x, y - 0.035f * scale, 0.012f * scale, Ink);
        Line(ref c, p, x, y - 0.035f * scale, x + 0.065f * scale, y, 0.012f * scale, Ink);
        Oval(ref c, p, x - 0.19f * scale, y, 0.04f * scale, 0.022f * scale, Pink, false);
        Oval(ref c, p, x + 0.19f * scale, y, 0.04f * scale, 0.022f * scale, Pink, false);
    }
    private static void Sparkle(ref Color c, Vector2 p, float x, float y, float radius)
    {
        Shape(ref c, p, (Mathf.Abs(p.x - x) + Mathf.Abs(p.y - y) - radius) * 0.7f, Gold);
    }
    private static float Ellipse(Vector2 p, float x, float y, float rx, float ry)
    {
        return (new Vector2((p.x - x) / rx, (p.y - y) / ry).magnitude - 1f) * Mathf.Min(rx, ry);
    }
    private static void Oval(ref Color c, Vector2 p, float x, float y, float rx, float ry, Color fill, bool outline = true)
    {
        Shape(ref c, p, Ellipse(p, x, y, rx, ry), fill, outline);
    }
    private static void Box(ref Color c, Vector2 p, float x, float y, float hx, float hy, float round, Color fill)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x - x) - hx + round, Mathf.Abs(p.y - y) - hy + round);
        float d = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - round;
        Shape(ref c, p, d, fill);
    }
    private static void Line(ref Color c, Vector2 p, float ax, float ay, float bx, float by, float radius, Color fill)
    {
        Vector2 a = new Vector2(ax, ay), b = new Vector2(bx, by);
        Vector2 delta = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, delta) / delta.sqrMagnitude);
        Shape(ref c, p, (p - a - delta * t).magnitude - radius, fill, false);
    }
    private static void Shape(ref Color c, Vector2 p, float distance, Color fill, bool outline = true)
    {
        if (outline) Blend(ref c, Ink, Mathf.Clamp01(0.5f - (distance - 0.014f) * Size));
        Color lit = Color.Lerp(fill * new Color(0.88f, 0.88f, 0.88f, 1f), fill, Mathf.Clamp01(p.y + 0.25f));
        Blend(ref c, lit, Mathf.Clamp01(0.5f - distance * Size));
    }
    private static void Blend(ref Color under, Color over, float coverage)
    {
        float alpha = coverage * over.a;
        float total = alpha + under.a * (1f - alpha);
        if (total <= 0f) return;
        Color result = (over * alpha + under * (under.a * (1f - alpha))) / total;
        result.a = total;
        under = result;
    }
}
