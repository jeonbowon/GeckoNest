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

    public bool HasEye(GeckoEye state)     => FindEye(state, false) != null;
    public bool HasMouth(GeckoMouth state) => FindMouth(state) != null;

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
