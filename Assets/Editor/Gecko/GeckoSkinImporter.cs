using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 최종 아트(PSD 또는 스프라이트 폴더) → GeckoSkin 에셋.
///
/// 레이어/스프라이트 이름으로 파츠를 찾는다 (파츠 분리 지시서 03번 표).
///   tail, body, head, leg_front_near … / eye_open, eye_look_left … / mouth_closed, mouth_smile …
///
/// 위치 정보
///   ① PSD + "Character Rig" 켜짐 → 레이어 원래 위치를 그대로 읽는다 (권장)
///   ② 모든 스프라이트가 같은 크기 → 캔버스 통째로 내보낸 PNG로 보고 그대로 겹친다
///   ③ 그 외 → 프록시 위치에 임시 배치 (관절 조정 필요)
/// </summary>
internal static class GeckoSkinImporter
{
    public static GeckoSkin Build(string path, StringBuilder report)
    {
        if (string.IsNullOrEmpty(path))
        {
            report.AppendLine("선택된 에셋이 없습니다. PSD 파일이나 스프라이트 폴더를 선택한 뒤 실행해 주십시오.");
            return null;
        }

        // 1) 스프라이트 모으기
        var sprites = new List<Sprite>();
        bool isFolder = AssetDatabase.IsValidFolder(path);
        if (isFolder)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { path }))
                sprites.AddRange(AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)).OfType<Sprite>());
        }
        else
        {
            sprites.AddRange(AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>());
        }

        if (sprites.Count == 0)
        {
            report.AppendLine("스프라이트를 찾지 못했습니다.");
            report.AppendLine("PSD라면 임포트 설정에서 Texture Type = Sprite (2D and UI), 모드 = Multiple 로 두고 Apply 해 주십시오.");
            return null;
        }

        var byName = new Dictionary<string, Sprite>();
        foreach (var s in sprites)
        {
            string key = GeckoParts.Normalize(s.name);
            if (!byName.ContainsKey(key)) byName.Add(key, s);
        }

        // 2) 위치 정보 (스프라이트 이름 → 원본 캔버스 픽셀 사각형)
        var rects = new Dictionary<string, Rect>();
        var prefab = isFolder ? null : AssetDatabase.LoadMainAssetAtPath(path) as GameObject;
        if (prefab != null)
        {
            foreach (var sr in prefab.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.sprite == null) continue;
                Vector3 local = prefab.transform.InverseTransformPoint(sr.transform.position);
                Vector2 pivotPx = new Vector2(local.x, local.y) * sr.sprite.pixelsPerUnit;
                rects[GeckoParts.Normalize(sr.sprite.name)] = new Rect(pivotPx - sr.sprite.pivot, sr.sprite.rect.size);
            }
            report.AppendLine($"● PSD 캐릭터 리그에서 레이어 위치 {rects.Count}개를 읽었습니다.");
        }
        else
        {
            var sizes = sprites.Select(s => s.rect.size).Distinct().ToList();
            if (sizes.Count == 1)
            {
                foreach (var s in sprites) rects[GeckoParts.Normalize(s.name)] = new Rect(Vector2.zero, s.rect.size);
                report.AppendLine("● 모든 스프라이트가 같은 크기라서, 캔버스를 통째로 내보낸 그림으로 보고 그대로 겹쳤습니다.");
            }
            else
            {
                report.AppendLine("▲ 위치 정보가 없습니다. 프록시 게코 위치에 임시로 배치합니다 — 관절 조정이 필요합니다.");
                report.AppendLine("   PSD 임포트 설정에서 'Character Rig'를 켜면 원래 위치를 자동으로 읽습니다.");
            }
        }

        // 3) 파츠 찾기
        var skin = ScriptableObject.CreateInstance<GeckoSkin>();
        var found = new Dictionary<GeckoPartId, Sprite>();
        var missing = new List<string>();
        for (int i = 0; i < GeckoParts.Count; i++)
        {
            var id = (GeckoPartId)i;
            var sp = Find(byName, GeckoParts.LayerName(id));
            if (sp == null && id == GeckoPartId.EyeL)  sp = Find(byName, "eye_open");
            if (sp == null && id == GeckoPartId.EyeR)  sp = Find(byName, "eye_r_open") ?? Find(byName, "eye_open");
            if (sp == null && id == GeckoPartId.Mouth) sp = Find(byName, "mouth_closed");
            if (sp == null) { missing.Add(GeckoParts.LayerName(id)); continue; }
            found[id] = sp;
        }

        // 4) 기준점: 발밑 중앙
        bool HasRect(GeckoPartId id) => found.ContainsKey(id) && rects.ContainsKey(GeckoParts.Normalize(found[id].name));
        Rect RectOf(GeckoPartId id) => rects[GeckoParts.Normalize(found[id].name)];

        var legs = new[] { GeckoPartId.LegFrontNear, GeckoPartId.LegBackNear, GeckoPartId.LegFrontFar, GeckoPartId.LegBackFar };
        var placed = found.Keys.Where(id => id != GeckoPartId.Shadow && HasRect(id)).ToList();
        Vector2 origin = Vector2.zero;
        bool hasLayout = placed.Count > 0;
        if (hasLayout)
        {
            var legRects = legs.Where(HasRect).Select(RectOf).ToList();
            float ground = legRects.Count > 0 ? legRects.Min(r => r.yMin) : placed.Min(id => RectOf(id).yMin);
            float cx = HasRect(GeckoPartId.Body) ? RectOf(GeckoPartId.Body).center.x
                                                  : (placed.Min(id => RectOf(id).xMin) + placed.Max(id => RectOf(id).xMax)) * 0.5f;
            origin = new Vector2(cx, ground);
        }

        float bodyCx = HasRect(GeckoPartId.Body) ? RectOf(GeckoPartId.Body).center.x : origin.x;
        bool faceLeft = HasRect(GeckoPartId.Head) && RectOf(GeckoPartId.Head).center.x < bodyCx;

        // 5) 파츠 채우기
        foreach (var kv in found)
        {
            var id = kv.Key;
            var sp = kv.Value;
            var art = skin.GetOrAddPart(id);
            art.sprite = sp;

            if (HasRect(id))
            {
                Rect r = RectOf(id);
                art.jointPivot    = DefaultPivot(id, r, bodyCx);
                art.jointPosition = r.min + Vector2.Scale(art.jointPivot, r.size) - origin;
            }
            else
            {
                var proxy = GeckoProxyLayout.Get(id);
                art.jointPivot    = proxy.id == id ? proxy.pivot : new Vector2(0.5f, 0.5f);
                art.jointPosition = proxy.joint;
            }
        }

        // 혀 둘째 마디는 첫 마디 끝에 이어 붙인다 (지시서: 입 안에 접힌 채 배치)
        var t1 = skin.GetPart(GeckoPartId.Tongue1);
        var t2 = skin.GetPart(GeckoPartId.Tongue2);
        var mouth = skin.GetPart(GeckoPartId.Mouth);
        if (t1 != null && mouth != null) t1.jointPosition = mouth.jointPosition + new Vector2(0f, 3f);
        if (t1 != null && t2 != null && t1.sprite != null)
            t2.jointPosition = t1.jointPosition + new Vector2(t1.sprite.rect.width * (1f - t1.jointPivot.x) * 0.9f, 0f);

        // 6) 눈 · 입 변형
        int eyeCount = 0, mouthCount = 0;
        foreach (GeckoEye e in System.Enum.GetValues(typeof(GeckoEye)))
        {
            string n = GeckoParts.EyeSpriteName(e);
            var left  = Find(byName, n);
            var right = Find(byName, n.Replace("eye_", "eye_r_"));
            if (e == GeckoEye.Open)
            {
                if (left == null)  left  = Find(byName, "eye_l");
                if (right == null) right = Find(byName, "eye_r");
            }
            if (left == null) continue;
            skin.eyes.Add(new GeckoEyeArt { state = e, left = left, right = right });
            eyeCount++;
        }
        foreach (GeckoMouth m in System.Enum.GetValues(typeof(GeckoMouth)))
        {
            var s = Find(byName, GeckoParts.MouthSpriteName(m));
            if (s == null && m == GeckoMouth.Closed) s = Find(byName, "mouth");
            if (s == null) continue;
            skin.mouths.Add(new GeckoMouthArt { state = m, sprite = s });
            mouthCount++;
        }

        // 7) 화면 크기 기준
        if (hasLayout)
            skin.referenceWidth = placed.Max(id => RectOf(id).xMax) - placed.Min(id => RectOf(id).xMin);
        else
            skin.referenceWidth = GeckoProxyLayout.ReferenceWidth;

        // 8) 저장 (이미 있으면 내용만 갈아 끼워 GUID 유지)
        string dir  = isFolder ? path : Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = Path.GetFileNameWithoutExtension(path);
        string skinPath = $"{dir}/GeckoSkin_{name}.asset";

        var existing = AssetDatabase.LoadAssetAtPath<GeckoSkin>(skinPath);
        if (existing != null)
        {
            Undo.RecordObject(existing, "Rebuild Gecko Skin");
            EditorUtility.CopySerialized(skin, existing);
            Object.DestroyImmediate(skin);
            skin = existing;
            EditorUtility.SetDirty(skin);
        }
        else
        {
            AssetDatabase.CreateAsset(skin, skinPath);
        }
        AssetDatabase.SaveAssets();

        // 9) 보고
        report.AppendLine();
        report.AppendLine($"파츠 {found.Count}/{GeckoParts.Count}개 · 눈 {eyeCount}종 · 입 {mouthCount}종");
        if (missing.Count > 0) report.AppendLine("없는 파츠: " + string.Join(", ", missing));
        if (eyeCount <= 1)   report.AppendLine("▲ 눈 표정이 1종뿐입니다 — eye_look_left, eye_happy 등을 추가하면 표정이 살아납니다.");
        if (mouthCount <= 1) report.AppendLine("▲ 입 모양이 1종뿐입니다 — mouth_smile, mouth_open_wide 등을 추가해 주십시오.");
        if (faceLeft)        report.AppendLine("▲ 머리가 몸통 왼쪽에 있습니다. 게코는 오른쪽을 보는 그림이어야 합니다 — 좌우 반전해서 다시 내보내 주십시오.");
        report.AppendLine();
        report.AppendLine($"저장: {skinPath}");
        report.AppendLine("다음: 메뉴 Hako > Gecko > ③ 선택한 스킨을 씬 게코에 적용");
        return skin;
    }

    private static Sprite Find(Dictionary<string, Sprite> byName, string key)
        => byName.TryGetValue(GeckoParts.Normalize(key), out var s) ? s : null;

    /// <summary>관절 위치 기본값 — 몸통 쪽을 향한 끝을 관절로 잡는다. 필요하면 Scene 뷰에서 옮긴다.</summary>
    private static Vector2 DefaultPivot(GeckoPartId id, Rect r, float bodyCx)
    {
        bool bodyOnRight = r.center.x < bodyCx;
        switch (id)
        {
            case GeckoPartId.Tail:         return new Vector2(bodyOnRight ? 0.92f : 0.08f, 0.45f);
            case GeckoPartId.Head:         return new Vector2(bodyOnRight ? 0.78f : 0.22f, 0.18f);
            case GeckoPartId.Body:
            case GeckoPartId.ShedPatch:    return new Vector2(0.5f, 0.45f);
            case GeckoPartId.LegFrontNear:
            case GeckoPartId.LegFrontFar:
            case GeckoPartId.LegBackNear:
            case GeckoPartId.LegBackFar:   return new Vector2(0.5f, 0.88f);
            case GeckoPartId.Tongue1:
            case GeckoPartId.Tongue2:      return new Vector2(0.1f, 0.5f);
            default:                       return new Vector2(0.5f, 0.5f);
        }
    }
}
