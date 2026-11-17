using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Authored ground-plane turns. Each cell is a new pose, never a rotated bitmap.</summary>
[CreateAssetMenu(menuName = "Hako/Gecko Turn Atlas")]
public sealed class GeckoTurnAtlas : ScriptableObject
{
    public Texture2D front;
    public Texture2D rear;
    public Frame[] frames = new Frame[0];

    [Serializable]
    public sealed class Frame
    {
        public bool rear;
        public int cell;
        public float angle;
        // Normalized cell coordinates, origin at TOP LEFT, measured on the authored atlas.
        public Vector2 neck, head, hip, tailMid, tailTip;
        public Vector2 frontNear, frontFar, backNear, backFar;
    }

    public sealed class Instance : IDisposable
    {
        public readonly List<GeckoGroundView> views = new List<GeckoGroundView>();
        private readonly List<Sprite> sprites = new List<Sprite>();

        public Instance(GeckoTurnAtlas atlas)
        {
            if (atlas == null || atlas.frames == null) return;
            foreach (var f in atlas.frames)
            {
                if (f == null) continue;
                Texture2D texture = f.rear ? atlas.rear : atlas.front;
                if (texture == null || f.cell < 0 || f.cell >= 16) continue;
                // Round cell edges independently so non-power-of-two source art is not resampled.
                int col = f.cell % 4, row = f.cell / 4;
                int left = Mathf.RoundToInt(col*texture.width/4f);
                int right = Mathf.RoundToInt((col+1)*texture.width/4f);
                int bottom = Mathf.RoundToInt((3-row)*texture.height/4f);
                int top = Mathf.RoundToInt((4-row)*texture.height/4f);
                Vector2 size = new Vector2(right-left,top-bottom);
                Vector2 neck = UV(f.neck), hip = UV(f.hip), head = UV(f.head);
                Vector2 centre = (neck+hip)*.5f;
                var sprite = Sprite.Create(texture,new Rect(left,bottom,size.x,size.y),centre,
                    100f,0,SpriteMeshType.FullRect);
                sprite.name = "Turn_" + f.angle;
                sprites.Add(sprite);
                var skin = CreateInstance<GeckoSkin>();
                skin.name = sprite.name;
                skin.hideFlags = HideFlags.DontSave;
                skin.authoredTurnFrame = true;
                skin.referenceWidth = size.x*1.25f; // same apparent scale as the original ground artwork
                skin.groundStride = size.x*(108f/1536f);
                skin.wholeBody = skin.independentWholeLegs = skin.directionalAnatomy = true;
                skin.groundForward = new Vector2(Mathf.Cos(f.angle*Mathf.Deg2Rad),Mathf.Sin(f.angle*Mathf.Deg2Rad));
                skin.wholeHeadPivot = neck;
                skin.wholeHeadGain = .4f;
                skin.directionalHeadHalfWidth = .16f;
                skin.wholeTailChain = new[] { hip, UV(f.tailMid), UV(f.tailTip) };
                skin.parts.Add(new GeckoPartArt { id = GeckoPartId.Body, sprite = sprite,
                    jointPivot = centre, scale = Vector2.one });
                AddLimb(skin,GeckoPartId.LegFrontNear,neck,f.frontNear,centre,size);
                AddLimb(skin,GeckoPartId.LegFrontFar,neck,f.frontFar,centre,size);
                AddLimb(skin,GeckoPartId.LegBackNear,hip,f.backNear,centre,size);
                AddLimb(skin,GeckoPartId.LegBackFar,hip,f.backFar,centre,size);
                Ghost(skin,GeckoPartId.Head,head,centre,size,new Vector2(.24f,.18f));
                Vector2 across = new Vector2(-(neck-hip).y,(neck-hip).x).normalized;
                Ghost(skin,GeckoPartId.EyeL,head+across*.065f,centre,size,new Vector2(.10f,.10f));
                Ghost(skin,GeckoPartId.EyeR,head-across*.065f,centre,size,new Vector2(.10f,.10f));
                Ghost(skin,GeckoPartId.Mouth,head+(head-neck)*.35f,centre,size,new Vector2(.15f,.07f));
                Ghost(skin,GeckoPartId.Tail,(hip+UV(f.tailTip))*.5f,centre,size,new Vector2(.25f,.2f));
                views.Add(new GeckoGroundView { angle = f.angle, skin = skin });
            }
        }

        private static Vector2 UV(Vector2 p) => new Vector2(p.x,1f-p.y);

        private static void AddLimb(GeckoSkin skin,GeckoPartId id,Vector2 root,Vector2 footTop,
            Vector2 centre,Vector2 size)
        {
            Vector2 foot = UV(footTop);
            Vector2 joint = Vector2.Lerp(root,foot,.35f);
            skin.wholeLegs.Add(new GeckoWholeLimb { id = id, joint = joint, foot = foot,
                radius = new Vector2(.045f,.075f) });
            Ghost(skin,id,foot,centre,size,new Vector2(.17f,.16f));
        }

        private static void Ghost(GeckoSkin skin,GeckoPartId id,Vector2 point,Vector2 centre,
            Vector2 size,Vector2 area)
        {
            skin.parts.Add(new GeckoPartArt { id = id, jointPosition = Vector2.Scale(point-centre,size),
                jointPivot = new Vector2(.5f,.5f), hitSize = Vector2.Scale(area,size), scale = Vector2.one });
        }

        public void Dispose()
        {
            foreach (var view in views) if (view.skin != null) UnityEngine.Object.Destroy(view.skin);
            foreach (var sprite in sprites) if (sprite != null) UnityEngine.Object.Destroy(sprite);
            views.Clear();
            sprites.Clear();
        }
    }
}
