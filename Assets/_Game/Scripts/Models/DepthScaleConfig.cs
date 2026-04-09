using UnityEngine;

[CreateAssetMenu(menuName = "Hako/DepthScaleConfig", fileName = "DepthScaleConfig")]
public class DepthScaleConfig : ScriptableObject
{
    [Header("Y 좌표 범위 (World Space)")]
    public float topY    = 3f;   // 화면 상단 (멀리) [TBD]
    public float bottomY = -3f;  // 화면 하단 (가까이) [TBD]

    [Header("스케일 범위")]
    public float minScale = 0.5f; // topY에서의 스케일 [TBD]
    public float maxScale = 1.2f; // bottomY에서의 스케일 [TBD]

    [Header("알파 범위")]
    public float minAlpha = 0.6f; // topY에서의 알파 [TBD]
    public float maxAlpha = 1.0f; // bottomY에서의 알파 [TBD]
}
