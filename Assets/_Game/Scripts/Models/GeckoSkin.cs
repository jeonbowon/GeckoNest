using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게코 그림 한 벌. 프록시 → 최종 아트 교체는 GeckoRig에 넣는 이 에셋만 바꾸면 된다.
///
/// 좌표계: 스킨 픽셀, 게코 발밑 중앙이 (0,0), 오른쪽을 보는 상태 기준.
/// </summary>
[CreateAssetMenu(fileName = "GeckoSkin", menuName = "Hako/GeckoSkin")]
public class GeckoSkin : ScriptableObject
{
    [Tooltip("꼬리 끝 ~ 주둥이 끝 전체 길이 (스킨 픽셀). 화면 표시 크기의 기준.")]
    public float referenceWidth = 1460f;

    [Header("전신 그림 (2026-09-22)")]
    [Tooltip("켜면 몸통(body) 그림 한 장이 게코 전체다. 머리·꼬리는 그 그림을 휘어 움직이고(GeckoWholeBend), " +
             "그림이 없는 파츠는 hitSize 크기로 터치 판정·연출 위치만 맡는다 (보이지 않음). 목 휨(GeckoNeckBend)은 쓰지 않는다")]
    public bool wholeBody;
    [Tooltip("몸통 그림 안 머리 관절 (uv, 0,0 = 왼쪽 아래)")]
    public Vector2 wholeHeadPivot = new Vector2(0.75f, 0.5f);
    [Tooltip("머리를 따르는 영역 — x: 가로 0이 되는 곳, y: 1이 되는 곳, z: 세로 0이 되는 곳, w: 1이 되는 곳 (uv)")]
    public Vector4 wholeHeadZone = new Vector4(0.66f, 0.76f, 0.46f, 0.56f);
    [Tooltip("머리 각도 배율 — 그림 한 장에서는 머리만 휘어 움직임이 작게 보인다")]
    public float wholeHeadGain = 1f;
    [Tooltip("꼬리 사슬 (uv) — 뿌리부터 끝까지 꼬리 가운데 선. 마디마다 꼬리 굽힘을 받아 휜다")]
    public Vector2[] wholeTailChain = new Vector2[0];
    [Tooltip("꼬리를 따르는 영역 — x: 가로 0이 되는 곳, y: 1이 되는 곳 (uv)")]
    public Vector2 wholeTailZone = new Vector2(0.33f, 0.22f);
    [Tooltip("다리 4개 — 어깨·엉덩이 → 발 뼈와 폭. 다리 각도(걸음)만큼 관절을 중심으로 휜다")]
    public List<GeckoWholeLimb> wholeLegs = new List<GeckoWholeLimb>();

    public List<GeckoPartArt>  parts  = new List<GeckoPartArt>();
    public List<GeckoEyeArt>   eyes   = new List<GeckoEyeArt>();
    public List<GeckoMouthArt> mouths = new List<GeckoMouthArt>();

    // ── 조회 ──────────────────────────────────────────────────

    public GeckoPartArt GetPart(GeckoPartId id)
    {
        for (int i = 0; i < parts.Count; i++)
            if (parts[i] != null && parts[i].id == id) return parts[i];
        return null;
    }

    public GeckoPartArt GetOrAddPart(GeckoPartId id)
    {
        var art = GetPart(id);
        if (art != null) return art;
        art = new GeckoPartArt { id = id };
        parts.Add(art);
        return art;
    }

    /// <summary>눈 상태 스프라이트. 없으면 기본 눈 → eye_l/eye_r 레이어 순으로 대체.</summary>
    public Sprite GetEye(GeckoEye state, bool right)
    {
        var s = FindEye(state, right);
        if (s == null && state != GeckoEye.Open) s = FindEye(GeckoEye.Open, right);
        if (s == null) s = GetPart(right ? GeckoPartId.EyeR : GeckoPartId.EyeL)?.sprite;
        if (s == null && right) s = GetPart(GeckoPartId.EyeL)?.sprite;
        return s;
    }

    /// <summary>입 상태 스프라이트. 없으면 닫힌 입 → mouth 레이어 순으로 대체.</summary>
    public Sprite GetMouth(GeckoMouth state)
    {
        var s = FindMouth(state);
        if (s == null && state != GeckoMouth.Closed) s = FindMouth(GeckoMouth.Closed);
        if (s == null) s = GetPart(GeckoPartId.Mouth)?.sprite;
        return s;
    }

    private Sprite FindEye(GeckoEye state, bool right)
    {
        for (int i = 0; i < eyes.Count; i++)
        {
            var e = eyes[i];
            if (e == null || e.state != state) continue;
            return right && e.right != null ? e.right : e.left;
        }
        return null;
    }

    private Sprite FindMouth(GeckoMouth state)
    {
        for (int i = 0; i < mouths.Count; i++)
            if (mouths[i] != null && mouths[i].state == state) return mouths[i].sprite;
        return null;
    }
}

[Serializable]
public class GeckoPartArt
{
    public GeckoPartId id;
    public Sprite      sprite;

    [Tooltip("관절(회전 중심) 위치. 게코 발밑 중앙 기준, 스킨 픽셀.")]
    public Vector2 jointPosition;

    [Tooltip("스프라이트 안에서 관절 위치 (0~1). 0,0 = 왼쪽 아래")]
    public Vector2 jointPivot = new Vector2(0.5f, 0.5f);

    [Tooltip("기본 크기 배율. 먼 쪽 다리·먼 쪽 눈의 원근 표현용")]
    public Vector2 scale = Vector2.one;

    [Tooltip("기본 색. 먼 쪽 다리를 살짝 어둡게 할 때 사용")]
    public Color tint = Color.white;

    [Tooltip("그림이 없을 때 쓰는 크기 (스킨 픽셀) — 전신 그림에서 보이지 않는 파츠의 터치 판정·연출 위치. 0이면 쓰지 않는다")]
    public Vector2 hitSize;
}

[Serializable]
public class GeckoWholeLimb
{
    public GeckoPartId id;
    [Tooltip("어깨·엉덩이 (몸통 그림 안 uv)")]
    public Vector2 joint;
    [Tooltip("발 가운데 (uv) — 관절 → 발이 다리 뼈")]
    public Vector2 foot;
    [Tooltip("다리 폭 (그림 폭 비율) — x: 관절 쪽, y: 발 쪽(발가락까지 덮게)")]
    public Vector2 radius;
}

[Serializable]
public class GeckoEyeArt
{
    public GeckoEye state;
    public Sprite   left;
    [Tooltip("비워두면 왼쪽 눈 그림을 같이 쓴다")]
    public Sprite   right;
}

[Serializable]
public class GeckoMouthArt
{
    public GeckoMouth state;
    public Sprite     sprite;
}
