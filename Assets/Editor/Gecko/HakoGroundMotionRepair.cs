using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Versioned artwork linking and isolated directional mesh regression captures.</summary>
public static class HakoGroundMotionRepair
{
    private const string Art = "Assets/_Game/Textures/Oblique/";
    private const string Skins = "Assets/_Game/GeckoSkins/";
    private static readonly string[] Names = { "side", "near", "near_steep", "front", "far", "far_steep", "back" };
    private static Vector2 U(float x, float y) => new Vector2(x / 1536f, 1f - y / 1024f);

    public static void LinkBatch()
    {
        try { Link(); CheckTailWidths(); HakoSelfTest.RunBatch(); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }

    private static Sprite Import(string name)
    {
        string path = Art + name + ".png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null || sprite.rect.size != new Vector2(1536, 1024))
            throw new InvalidOperationException("Anatomy requires 1536x1024: " + path);
        return sprite;
    }

    private static GeckoSkin Skin(string name) => AssetDatabase.LoadAssetAtPath<GeckoSkin>(Skins + "GeckoSkin_Ground_" + name + ".asset");

    private static void Link()
    {
        var near = Skin("near");
        near.GetPart(GeckoPartId.Body).sprite = Import("hako_ground_near_v2");
        // Corrected far foreleg is below the face; the removed upper appendage was a fifth leg.
        SetLeg(near, 1, 1190, 690, 1260, 800, .03f, .065f);
        EditorUtility.SetDirty(near);

        Make("near_steep", "near", -65f, new Vector2(850, 920), new Vector2(875, 650),
            new[] { U(620,420), U(525,310), U(425,225), U(310,145), U(260,65) },
            new[] { new Vector4(735,705,710,865), new Vector4(1065,765,1110,910),
                    new Vector4(580,465,470,610), new Vector4(820,395,960,420) });
        Make("far_steep", "far", 65f, new Vector2(800, 850), new Vector2(1090,290),
            new[] { U(750,675), U(625,755), U(500,835), U(375,920), U(255,965) },
            new[] { new Vector4(1105,365,1190,455), new Vector4(1015,310,905,280),
                    new Vector4(820,675,900,790), new Vector4(735,540,610,510) });
        var root = AssetDatabase.LoadAssetAtPath<GeckoSkin>(HakoObliqueArt.SkinPath);
        root.groundViews.RemoveAll(v => v != null && (Mathf.Approximately(v.angle, -65) || Mathf.Approximately(v.angle, 65)));
        root.groundViews.Add(new GeckoGroundView { angle = -65, skin = Skin("near_steep") });
        root.groundViews.Add(new GeckoGroundView { angle = 65, skin = Skin("far_steep") });
        EditorUtility.SetDirty(root);
        foreach (string name in Names)
        {
            var skin = Skin(name);
            skin.groundStride = 108f; // [TBD] longer crawl stride; ordinary travel speed is reduced separately
            EditorUtility.SetDirty(skin);
        }
        AssetDatabase.SaveAssets();
    }

    private static void Make(string name, string template, float angle, Vector2 origin, Vector2 head, Vector2[] tail, Vector4[] legs)
    {
        string path = Skins + "GeckoSkin_Ground_" + name + ".asset";
        var skin = Skin(name);
        if (skin == null)
        {
            skin = UnityEngine.Object.Instantiate(Skin(template));
            skin.name = "GeckoSkin_Ground_" + name;
            AssetDatabase.CreateAsset(skin, path);
        }
        skin.GetPart(GeckoPartId.Body).sprite = Import("hako_ground_" + name + "_v1");
        skin.GetPart(GeckoPartId.Body).jointPivot = U(origin.x, origin.y);
        skin.GetPart(GeckoPartId.Body).jointPosition = Vector2.zero;
        skin.groundForward = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        skin.wholeHeadPivot = U(head.x, head.y);
        skin.wholeHeadGain = .25f;
        skin.wholeTailChain = tail;
        // Contact proxies are authored in the same pixel space as the visible sprite.
        SetProxy(skin, GeckoPartId.Head, head, origin, new Vector2(330, 270));
        SetProxy(skin, GeckoPartId.EyeL, head + new Vector2(-45, 45), origin, new Vector2(100,100));
        SetProxy(skin, GeckoPartId.EyeR, head + new Vector2(90, 45), origin, new Vector2(100,100));
        SetProxy(skin, GeckoPartId.Mouth, head + new Vector2(40, 110), origin, new Vector2(150,45));
        Vector2 tailMid = (tail[1] + tail[tail.Length-1]) * .5f;
        SetProxy(skin, GeckoPartId.Tail, new Vector2(tailMid.x*1536, (1-tailMid.y)*1024), origin, new Vector2(250,350));
        for (int i=0; i<4; i++)
        {
            Vector4 l = legs[i];
            SetLeg(skin, i, l.x, l.y, l.z, l.w, .035f, .065f);
        }
        EditorUtility.SetDirty(skin);
    }

    private static void SetProxy(GeckoSkin skin, GeckoPartId id, Vector2 point, Vector2 origin, Vector2 size)
    {
        var part = skin.GetPart(id);
        if (part == null) return;
        part.jointPosition = new Vector2(point.x-origin.x, origin.y-point.y);
        part.jointPivot = Vector2.one * .5f;
        part.hitSize = size;
    }

    private static void SetLeg(GeckoSkin skin, int index, float jx, float jy, float fx, float fy, float top, float foot)
    {
        var limb = skin.wholeLegs[index];
        limb.joint = U(jx,jy); limb.foot = U(fx,fy); limb.radius = new Vector2(top,foot);
        var body = skin.GetPart(GeckoPartId.Body);
        Vector2 origin = new Vector2(body.jointPivot.x*1536, (1-body.jointPivot.y)*1024);
        SetProxy(skin, limb.id, new Vector2(fx,fy), origin, Vector2.one * foot*3072);
    }

    private static GeckoPose Pose(GeckoSkin skin, float phase)
    {
        var pose = new GeckoPose(12);
        pose[GeckoPartId.Tongue1].scale = pose[GeckoPartId.Tongue2].scale = Vector2.zero;
        pose[GeckoPartId.ShedPatch].alpha = 0;
        pose.tailUncurl = .8f; // Regression: should not uncurl the already extended directional tail.
        for (int k=0; k<12; k++) pose.tailBend[k] = Mathf.Sin(phase-k*.4f)*5;
        for (int i=0; i<4; i++)
        {
            float t = Mathf.Repeat(phase/(2*Mathf.PI) + (i==0 || i==3 ? 0 : .5f),1);
            float stroke = skin.groundStride*.65f*.5f;
            float along = t<.65f ? Mathf.Lerp(stroke,-stroke,t/.65f) : Mathf.Lerp(-stroke,stroke,Mathf.SmoothStep(0,1,(t-.65f)/.35f));
            pose[skin.wholeLegs[i].id].offset = skin.groundForward*along + Vector2.up*(t<.65f ? 0 : Mathf.Sin((t-.65f)/.35f*Mathf.PI)*12);
        }
        GeckoMotor.ApplyGroundTorso(pose,skin.groundForward,phase,1f,8f,Mathf.Sin(phase*.3f),skin.wholeHeadGain);
        return pose;
    }

    public static void CheckTailWidths()
    {
        int checks = 0;
        foreach (string name in Names)
        {
            var skin = Skin(name);
            var go = new GameObject("Tail regression", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            try
            {
                var bend = go.AddComponent<GeckoWholeBend>();
                bend.Configure(skin.wholeHeadPivot,skin.wholeHeadZone,skin.wholeHeadGain,skin.wholeTailChain,skin.wholeTailZone,skin.wholeLegs.ToArray(),true);
                bend.ConfigureDirection(true,skin.groundForward,skin.directionalHeadHalfWidth);
                Vector2 size = new Vector2(1536,1024);
                int last = skin.wholeTailChain.Length-1;
                Vector2 end = Vector2.Scale(skin.wholeTailChain[last],size), start = Vector2.Scale(skin.wholeTailChain[last-1],size);
                Vector2 axis = (end-start).normalized, normal = new Vector2(-axis.y,axis.x);
                Vector2 centre = Vector2.Lerp(start,end,.8f);
                for (int f=0;f<32;f++)
                {
                    bend.SetPose(Pose(skin,f*Mathf.PI/16));
                    Vector2 a = centre+normal*12, b = centre-normal*12;
                    Vector2 da = bend.Deform(new Vector2(a.x/size.x,a.y/size.y),Vector2.zero,size);
                    Vector2 db = bend.Deform(new Vector2(b.x/size.x,b.y/size.y),Vector2.zero,size);
                    float ratio = Vector2.Distance(da,db)/24;
                    if (ratio < .8f || ratio > 1.2f) throw new InvalidOperationException(name+" tail width changed: "+ratio);
                    checks++;
                }
                // Torso sway must move the chest while leaving all four painted feet and tail tip fixed.
                var torso = new GeckoPose(12);
                GeckoMotor.ApplyGroundTorso(torso,skin.groundForward,Mathf.PI/2,1,8,0,skin.wholeHeadGain);
                bend.SetPose(torso);
                foreach (var leg in skin.wholeLegs)
                {
                    Vector2 rest=Vector2.Scale(leg.foot,size);
                    if (Vector2.Distance(rest,bend.Deform(leg.foot,Vector2.zero,size))>.01f)
                        throw new InvalidOperationException(name+" torso/head moved a planted foot");
                }
                Vector2 chest=Vector2.Lerp(skin.wholeTailChain[0],skin.wholeHeadPivot,.45f);
                if (Vector2.Distance(Vector2.Scale(chest,size),bend.Deform(chest,Vector2.zero,size))<1f)
                    throw new InvalidOperationException(name+" torso did not move");
                if (Vector2.Distance(end,bend.Deform(skin.wholeTailChain[last],Vector2.zero,size))>.01f)
                    throw new InvalidOperationException(name+" torso distorted tail tip");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        Debug.Log("[GroundMotion] Tail cross-section checks passed: " + checks);
        Debug.Log("[GroundMotion] Torso/chest, four-foot pinning and tail isolation passed: 7 skins");
        var turnGo = new GameObject("Heading regression",typeof(RectTransform),typeof(GeckoRig));
        try
        {
            var rig = turnGo.GetComponent<GeckoRig>();
            rig.SetSkin(AssetDatabase.LoadAssetAtPath<GeckoSkin>(HakoObliqueArt.SkinPath),false);
            rig.SetGrowthStage(4,true);
            for (int degree=0;degree<360;degree+=15)
            {
                Vector2 direction = new Vector2(Mathf.Cos(degree*Mathf.Deg2Rad),Mathf.Sin(degree*Mathf.Deg2Rad));
                if (!rig.SetGroundHeading(direction)) throw new InvalidOperationException("Missing heading " + degree);
                rig.SolveRest();
                Vector2 artworkAxis = GeckoRig.ArtworkForward(rig.Skin);
                Vector2 actual = rig.transform.Find("Visual").TransformVector(artworkAxis).normalized;
                if (Vector2.Dot(actual,direction)<.999f) throw new InvalidOperationException("Heading/mirror mismatch " + degree);
                if (Vector2.Dot(rig.GroundTravelDirection(direction),artworkAxis)<.999f)
                    throw new InvalidOperationException("Foot travel does not match heading " + degree);
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(turnGo); }
        Debug.Log("[GroundMotion] Heading and foot-direction checks passed: 24");
    }

    public static void CaptureBatch()
    {
        try
        {
            CheckTailWidths();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraGo = new GameObject("Motion capture camera", typeof(Camera));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraGo, scene);
            var camera = cameraGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.12f,.15f,.12f);
            camera.orthographic = true; camera.orthographicSize = 600; camera.transform.position = new Vector3(0,0,-10);
            var canvasGo = new GameObject("Motion capture canvas",typeof(RectTransform),typeof(Canvas));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasGo,scene);
            var canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=1;
            var rt = new RenderTexture(2800,1200,24); camera.targetTexture=rt;
            for (int row=0;row<3;row++) for (int col=0;col<Names.Length;col++)
            {
                var go = new GameObject(Names[col],typeof(RectTransform),typeof(GeckoRig));
                var rect = (RectTransform)go.transform; rect.SetParent(canvasGo.transform,false);
                rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f); rect.pivot=new Vector2(.5f,0);
                rect.anchoredPosition=new Vector2(-1200+col*400,310-row*380); rect.sizeDelta=Vector2.zero;
                rect.localScale=Vector3.one*.6f;
                var rig=go.GetComponent<GeckoRig>();
                rig.SetSkin(AssetDatabase.LoadAssetAtPath<GeckoSkin>(HakoObliqueArt.SkinPath),false); rig.SetGrowthStage(4,true);
                Vector2 heading=Skin(Names[col]).groundForward;
                if (row==1) heading.x=-heading.x;
                rig.SetGroundHeading(heading);
                rig.Solve(Pose(Skin(Names[col]),row*2*Mathf.PI/3),0);
            }
            Canvas.ForceUpdateCanvases(); camera.Render();
            var previous=RenderTexture.active; RenderTexture.active=rt;
            var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0); image.Apply();
            Directory.CreateDirectory("Logs"); File.WriteAllBytes(_capturePath,image.EncodeToPNG());
            RenderTexture.active=previous; camera.targetTexture=null; rt.Release();
            UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt);
            // Batch-only temporary scene is deliberately never saved.
            Debug.Log("[GroundMotion] Captured seven headings at three gait phases, isolated preview scene.");
            EditorApplication.Exit(0);
        }
        catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }

    private static string _capturePath = "Logs/ground-motion-20261113.png";
    public static void CaptureTorsoBatch()
    {
        _capturePath = "Logs/torso-motion-20261113.png";
        CaptureBatch();
    }

    public static void VerifyTorsoBatch()
    {
        try { CheckTailWidths(); HakoSelfTest.RunBatch(); }
        catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
