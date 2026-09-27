using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One material per whole-body rig. Patterns use the body's texture UVs, never child quads.</summary>
public sealed class GeckoWholeSurface : IDisposable
{
    public const int MaxMarks = 15;
    public const string ShaderPath = "GeckoWholeSurface";
    private readonly Vector4[] _marks = new Vector4[MaxMarks];
    private Image _image;
    private Material _material, _previous;
    private Vector4 _face = new Vector4(-1, -1, -1, -1);
    public int MarkCount { get; private set; }
    public Material Material => _material;

    public void Apply(Image image, GeckoSkin skin, MorphPattern pattern, Color color, int seed)
    {
        if (image == null || image.sprite == null || skin == null ||
            (pattern == MorphPattern.None && skin.wholeEyeAtlas == null && skin.wholeMouthAtlas == null))
        {
            Dispose();
            return;
        }
        if (_image != image) Dispose();
        if (_material == null)
        {
            var shader = Resources.Load<Shader>(ShaderPath);
            if (shader == null)
            {
                Debug.LogError("[GeckoWholeSurface] Missing Resources/GeckoWholeSurface.shader");
                return;
            }
            _image = image;
            _previous = image.material;
            _material = new Material(shader) { name = "Gecko whole-body surface", hideFlags = HideFlags.HideAndDontSave };
            image.material = _material;
        }
        MarkCount = FillMarks(_marks, pattern, seed);
        _material.SetVectorArray("_Marks", _marks);
        _material.SetFloat("_MarkCount", MarkCount);
        _material.SetColor("_PatternColor", color);
        _material.SetVector("_SpriteUV", UnityEngine.Sprites.DataUtility.GetOuterUV(image.sprite));
        _material.SetTexture("_EyeAtlas", skin.wholeEyeAtlas != null ? skin.wholeEyeAtlas : Texture2D.blackTexture);
        _material.SetTexture("_MouthAtlas", skin.wholeMouthAtlas != null ? skin.wholeMouthAtlas : Texture2D.blackTexture);
        _material.SetVector("_EyeLeftRect", Pack(skin.wholeEyeLeftRect));
        _material.SetVector("_EyeRightRect", Pack(skin.wholeEyeRightRect));
        _material.SetVector("_MouthRect", Pack(skin.wholeMouthRect));
        _material.SetVector("_HasFace", new Vector4(skin.wholeEyeAtlas != null ? 1 : 0, skin.wholeMouthAtlas != null ? 1 : 0, 0, 0));
        image.SetMaterialDirty();
    }

    private static Vector4 Pack(Rect r) => new Vector4(r.x, r.y, r.width, r.height);

    public void SetFace(GeckoPose pose)
    {
        if (_material == null || pose == null) return;
        var face = new Vector4((int)pose.eyeL, (int)pose.eyeR, (int)pose.mouth, 0);
        if (_face == face) return;
        _face = face;
        _material.SetVector("_Face", face);
        // Parent UI masks may return a stencil material clone.
        var rendered = _image != null ? _image.materialForRendering : null;
        if (rendered != null && rendered != _material) rendered.SetVector("_Face", face);
    }

    // Normalized original-image space. Kept away from the face (right/top), deterministic per gecko.
    public static int FillMarks(Vector4[] marks, MorphPattern pattern, int seed)
    {
        if (marks == null || marks.Length < MaxMarks) throw new ArgumentException("15 marks required", nameof(marks));
        Array.Clear(marks, 0, marks.Length);
        int count = pattern == MorphPattern.None ? 0 : pattern == MorphPattern.Spots ? 15 : pattern == MorphPattern.Blotches ? 6 : 7;
        var rng = new System.Random(seed);
        float Next(float lo, float hi) => Mathf.Lerp(lo, hi, (float)rng.NextDouble());
        for (int i = 0; i < count; i++)
        {
            float x = pattern == MorphPattern.Stripes ? Mathf.Lerp(0.12f, 0.71f, i / (float)(count - 1)) : Next(0.12f, 0.73f);
            float y = Next(0.23f, 0.53f);
            float rx = pattern == MorphPattern.Spots ? Next(0.010f, 0.022f) : pattern == MorphPattern.Blotches ? Next(0.045f, 0.09f) : 0.018f;
            float ry = pattern == MorphPattern.Stripes ? 0.25f : rx * 1.65f;
            marks[i] = new Vector4(x, y, rx, ry);
        }
        return count;
    }

    public void Dispose()
    {
        if (_image != null && _image.material == _material) _image.material = _previous;
        if (_material != null)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(_material);
            else UnityEngine.Object.DestroyImmediate(_material);
        }
        _image = null;
        _material = _previous = null;
        MarkCount = 0;
        _face = new Vector4(-1, -1, -1, -1);
    }
}
