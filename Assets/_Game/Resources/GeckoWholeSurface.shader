Shader "Hako/UI/WholeSurface"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _PatternColor ("Pattern", Color) = (0,0,0,0)
        _SpriteUV ("Sprite UV", Vector) = (0,0,1,1)
        _MarkCount ("Marks", Float) = 0
        _EyeAtlas ("Eye atlas 3x3", 2D) = "black" {}
        _MouthAtlas ("Mouth atlas 4x2", 2D) = "black" {}
        _Face ("Eye left/right/mouth", Vector) = (0,0,0,0)
        _HasFace ("Atlas availability", Vector) = (0,0,0,0)
        _EyeLeftRect ("Near eye region", Vector) = (0,0,0,0)
        _EyeRightRect ("Far eye region", Vector) = (0,0,0,0)
        _MouthRect ("Mouth region", Vector) = (0,0,0,0)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 local : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _MainTex;
            sampler2D _EyeAtlas, _MouthAtlas;
            float4 _Face, _HasFace, _EyeLeftRect, _EyeRightRect, _MouthRect;
            float4 _FaceSampling;
            fixed4 _Color, _TextureSampleAdd, _PatternColor;
            float4 _ClipRect, _SpriteUV, _Marks[15];
            float _MarkCount;
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.local = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }
            fixed4 FacePatch(sampler2D atlas, float2 uv, float4 region, float state, float2 grid, float inset, float angle)
            {
                if (state < 0.5 || region.z <= 0 || region.w <= 0) return fixed4(0,0,0,0);
                float aspect = max(_FaceSampling.w, 0.001);
                float2 delta = uv - region.xy - region.zw * 0.5;
                delta.y /= aspect;
                float cs = cos(angle), sn = sin(angle);
                delta = float2(cs * delta.x + sn * delta.y, -sn * delta.x + cs * delta.y);
                delta.y *= aspect;
                float2 q = delta / region.zw + 0.5;
                if (any(q < 0) || any(q > 1)) return fixed4(0,0,0,0);
                float2 cell = float2(fmod(state, grid.x), grid.y - 1 - floor(state / grid.x));
                float2 sampleUV = (cell + lerp(inset, 1-inset, q)) / grid;
                fixed4 p = tex2D(atlas, sampleUV);
                float border = min(min(q.x, 1-q.x), min(q.y, 1-q.y));
                p.a *= smoothstep(0, 0.12, border);
                return p;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) + _TextureSampleAdd;
                float2 uv = (i.uv - _SpriteUV.xy) / max(_SpriteUV.zw - _SpriteUV.xy, float2(0.00001,0.00001));
                float coverage = 0;
                for (int n = 0; n < 15; n++)
                {
                    if (n >= _MarkCount) break;
                    float2 q = (uv - _Marks[n].xy) / max(_Marks[n].zw, float2(0.00001,0.00001));
                    float d = length(q);
                    float edge = max(fwidth(d), 0.035);
                    coverage = max(coverage, 1 - smoothstep(1 - edge, 1 + edge, d));
                }
                // Preserve source alpha: no marks can appear outside the original silhouette.
                c.rgb = lerp(c.rgb, _PatternColor.rgb, coverage * _PatternColor.a);
                // Face patches are sampled in the same original UVs as the body: no transform drift.
                fixed4 face;
                if (_HasFace.x > 0.5)
                {
                    face = FacePatch(_EyeAtlas, uv, _EyeLeftRect, _Face.x, float2(3,3), _FaceSampling.x, 0);
                    c.rgb = lerp(c.rgb, face.rgb, face.a);
                    face = FacePatch(_EyeAtlas, uv, _EyeRightRect, _Face.y, float2(3,3), _FaceSampling.x, 0);
                    c.rgb = lerp(c.rgb, face.rgb, face.a);
                }
                if (_HasFace.y > 0.5)
                {
                    face = FacePatch(_MouthAtlas, uv, _MouthRect, _Face.z, float2(4,2), _FaceSampling.y, _FaceSampling.z);
                    c.rgb = lerp(c.rgb, face.rgb, face.a);
                }
                c *= i.color;
                #ifdef UNITY_UI_CLIP_RECT
                c.a *= UnityGet2DClipping(i.local.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(c.a - 0.001);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
