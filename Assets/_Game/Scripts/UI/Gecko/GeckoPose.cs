using UnityEngine;

/// <summary>파츠 하나의 순간 자세. 관절 부모 기준 상대값.</summary>
public struct GeckoPartPose
{
    public float   angle;   // 도, 반시계 +
    public Vector2 offset;  // 스킨 픽셀, 부모 공간
    public Vector2 scale;
    public float   alpha;

    public static readonly GeckoPartPose Rest = new GeckoPartPose
    {
        angle = 0f, offset = Vector2.zero, scale = Vector2.one, alpha = 1f,
    };
}

/// <summary>
/// 게코 전체의 한 프레임 자세. GeckoMotor가 채우고 GeckoRig가 화면에 반영한다.
/// </summary>
public class GeckoPose
{
    public readonly GeckoPartPose[] parts = new GeckoPartPose[GeckoParts.Count];
    public readonly float[] tailBend;

    public GeckoEye   eyeL  = GeckoEye.Open;
    public GeckoEye   eyeR  = GeckoEye.Open;
    public GeckoMouth mouth = GeckoMouth.Closed;

    /// <summary>게코 전체 크기 배율 (성장 펄스 등)</summary>
    public float rootScale = 1f;

    public GeckoPose(int tailSegments)
    {
        tailBend = new float[Mathf.Max(1, tailSegments)];
        Reset();
    }

    public void Reset()
    {
        for (int i = 0; i < parts.Length; i++) parts[i] = GeckoPartPose.Rest;
        for (int i = 0; i < tailBend.Length; i++) tailBend[i] = 0f;
        eyeL = eyeR = GeckoEye.Open;
        mouth = GeckoMouth.Closed;
        rootScale = 1f;
    }

    public ref GeckoPartPose this[GeckoPartId id] => ref parts[(int)id];
}
