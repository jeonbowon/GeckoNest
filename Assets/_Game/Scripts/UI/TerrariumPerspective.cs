using UnityEngine;

/// <summary>View-only projection. Stored decoration coordinates keep their original meaning.</summary>
public static class TerrariumPerspective
{
    public static readonly Vector2 LegacyGround = new Vector2(380f, 950f);
    public static readonly Vector2 ObliqueGround = new Vector2(500f, 1320f); // [TBD] approved 40-degree art
    private const float DepthGain = 820f / 570f;

    public static Vector2 Project(Vector2 stored, bool oblique)
        => oblique ? new Vector2(stored.x, 500f + (stored.y - 380f) * DepthGain) : stored;

    public static Vector2 Unproject(Vector2 displayed, bool oblique)
        => oblique ? new Vector2(displayed.x, 380f + (displayed.y - 500f) / DepthGain) : displayed;

    public static Vector2 Anchor(TerrariumData data, int slot, bool oblique)
        => Project(TerrariumLayout.AnchorOf(data, slot), oblique);
}
