"""
build_store_scene.py
Store.unity 씬을 Python으로 직접 생성.

레이아웃 (1080×2400 기준):
  ┌──────────────────────────┐
  │  TopBar  (h=150, top)    │  CoinText / GemText / "Store"
  ├──────────────────────────┤
  │                          │
  │  ScrollView (fill)       │  아이템 슬롯 목록 (런타임 생성)
  │                          │
  ├──────────────────────────┤
  │  BackButton  (h=160, bot)│  "< Back"
  └──────────────────────────┘
  ErrorPanel (전체화면, 평소 숨김)

사용법:  python Tools/build_store_scene.py
"""

import random, string, os

# ── GUID 사전 ──────────────────────────────────────────────────────────────────
G = {
    # Unity uGUI / TMP
    "Image"              : "fe87c0e1cc204ed48ad3b37840f39efc",
    "Button"             : "4e29b1a8efbd4b44bb3f3716e73f07ff",
    "TMP_Text"           : "f4688fdb7df04437aeb418b961361dc5",
    "CanvasScaler"       : "0cd44c1031e13a943bb63640046fad76",
    "GraphicRaycaster"   : "dc42784cf147c0c48a680349fa168899",
    "EventSystem"        : "76c392e42b5098c458856cdf6ecaaaa1",
    "InputModule"        : "01614664b831546d2ae94a42149d80ac",
    "ScrollRect"         : "1aa08ab6e0800fa44ae55d278d1423e3",
    "Scrollbar"          : "2a4db7a114972834c8e4117be1d82ba3",
    "Mask"               : "31a19414c41e5ae4aae2af33fee712f6",
    "VerticalLayoutGroup": "59f8146938fff824cb5fd77236b75775",
    "ContentSizeFitter"  : "3245ec927659c4140ac4f8d17403cc18",
    # Custom
    "StoreUIController"  : "d0af96c4c1415b6449c43b0dfef14cb8",
    # Assets
    "InputActions"       : "ca9f5fa95ffab41fb9a615ab714db018",
    "TMP_Font"           : "8f586378b4e144a9851e7b34d9b748ee",
}

# ── fileID 생성기 ──────────────────────────────────────────────────────────────
_used = set()
def uid():
    while True:
        n = random.randint(100_000_000, 2_000_000_000)
        if n not in _used:
            _used.add(n)
            return n

# ── YAML 블록 헬퍼 ─────────────────────────────────────────────────────────────

def go_block(fid, name, layer, components, active=1):
    comp_lines = "\n".join(f"  - component: {{fileID: {c}}}" for c in components)
    return f"""--- !u!1 &{fid}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
{comp_lines}
  m_Layer: {layer}
  m_Name: {name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: {active}"""

def rect_transform(fid, go_fid, father, children,
                   anchor_min="0.5 0.5", anchor_max="0.5 0.5",
                   pos="0 0", size="100 100", pivot="0.5 0.5"):
    ax, ay = anchor_min.split()
    bx, by = anchor_max.split()
    px, py = pos.split()
    sx, sy = size.split()
    vx, vy = pivot.split()
    ch_lines = "\n".join(f"  - {{fileID: {c}}}" for c in children) if children else ""
    ch_block = f"  m_Children:\n{ch_lines}" if ch_lines else "  m_Children: []"
    return f"""--- !u!224 &{fid}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_fid}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
{ch_block}
  m_Father: {{fileID: {father}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: {ax}, y: {ay}}}
  m_AnchorMax: {{x: {bx}, y: {by}}}
  m_AnchoredPosition: {{x: {px}, y: {py}}}
  m_SizeDelta: {{x: {sx}, y: {sy}}}
  m_Pivot: {{x: {vx}, y: {vy}}}"""

def canvas_renderer(fid, go_fid):
    return f"""--- !u!222 &{fid}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_fid}}}
  m_CullTransparentMesh: 1"""

def image_component(fid, go_fid, color="1 1 1 1", sprite_fid=10905, raycast=1):
    r, g, b, a = color.split()
    return f"""--- !u!114 &{fid}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_fid}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {G['Image']}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {{fileID: 0}}
  m_Color: {{r: {r}, g: {g}, b: {b}, a: {a}}}
  m_RaycastTarget: {raycast}
  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {{fileID: {sprite_fid}, guid: 0000000000000000f000000000000000, type: 0}}
  m_Type: 1
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1"""

def tmp_text(fid, go_fid, text, font_size=36, color="1 1 1 1", h_align=2, v_align=512, bold=0):
    r, g, b, a = color.split()
    rgba_hex = int(r_f(r)*255)<<24 | int(r_f(g)*255)<<16 | int(r_f(b)*255)<<8 | int(r_f(a)*255)
    style = 1 if bold else 0
    return f"""--- !u!114 &{fid}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_fid}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {G['TMP_Text']}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: Unity.TextMeshPro::TMPro.TextMeshProUGUI
  m_Material: {{fileID: 0}}
  m_Color: {{r: 1, g: 1, b: 1, a: 1}}
  m_RaycastTarget: 0
  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_text: {text}
  m_isRightToLeft: 0
  m_fontAsset: {{fileID: 11400000, guid: {G['TMP_Font']}, type: 2}}
  m_sharedMaterial: {{fileID: 2180264, guid: {G['TMP_Font']}, type: 2}}
  m_fontSharedMaterials: []
  m_fontMaterial: {{fileID: 0}}
  m_fontMaterials: []
  m_fontColor32:
    serializedVersion: 2
    rgba: 4294967295
  m_fontColor: {{r: {r}, g: {g}, b: {b}, a: {a}}}
  m_enableVertexGradient: 0
  m_colorMode: 3
  m_fontColorGradient:
    topLeft: {{r: 1, g: 1, b: 1, a: 1}}
    topRight: {{r: 1, g: 1, b: 1, a: 1}}
    bottomLeft: {{r: 1, g: 1, b: 1, a: 1}}
    bottomRight: {{r: 1, g: 1, b: 1, a: 1}}
  m_fontColorGradientPreset: {{fileID: 0}}
  m_spriteAsset: {{fileID: 0}}
  m_tintAllSprites: 0
  m_StyleSheet: {{fileID: 0}}
  m_TextStyleHashCode: -1183493901
  m_overrideHtmlColors: 0
  m_faceColor:
    serializedVersion: 2
    rgba: 4294967295
  m_fontSize: {font_size}
  m_fontSizeBase: {font_size}
  m_fontWeight: 400
  m_enableAutoSizing: 0
  m_fontSizeMin: 18
  m_fontSizeMax: 72
  m_fontStyle: {style}
  m_HorizontalAlignment: {h_align}
  m_VerticalAlignment: {v_align}
  m_textAlignment: 65535
  m_characterSpacing: 0
  m_wordSpacing: 0
  m_lineSpacing: 0
  m_lineSpacingMax: 0
  m_paragraphSpacing: 0
  m_charWidthMaxAdj: 0
  m_TextWrappingMode: 1
  m_wordWrappingRatios: 0.4
  m_overflowMode: 0
  m_linkedTextComponent: {{fileID: 0}}
  parentLinkedComponent: {{fileID: 0}}
  m_enableKerning: 0
  m_ActiveFontFeatures: 6e72656b
  m_enableExtraPadding: 0
  checkPaddingRequired: 0
  m_isRichText: 1
  m_EmojiFallbackSupport: 1
  m_parseCtrlCharacters: 1
  m_isOrthographic: 1
  m_isCullingEnabled: 0
  m_horizontalMapping: 0
  m_verticalMapping: 0
  m_uvLineOffset: 0
  m_geometrySortingOrder: 0
  m_IsTextObjectScaleStatic: 0
  m_VertexBufferAutoSizeReduction: 0
  m_useMaxVisibleDescender: 1
  m_pageToDisplay: 1
  m_margin: {{x: 0, y: 0, z: 0, w: 0}}
  m_isUsingLegacyAnimationComponent: 0
  m_isVolumetricText: 0
  m_hasFontAssetChanged: 0
  m_baseMaterial: {{fileID: 0}}
  m_maskOffset: {{x: 0, y: 0, z: 0, w: 0}}"""

def r_f(v):
    return float(v)

def button_component(fid, go_fid, target_graphic_fid):
    return f"""--- !u!114 &{fid}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_fid}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {G['Button']}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Button
  m_Navigation:
    m_Mode: 3
    m_WrapAround: 0
    m_SelectOnUp: {{fileID: 0}}
    m_SelectOnDown: {{fileID: 0}}
    m_SelectOnLeft: {{fileID: 0}}
    m_SelectOnRight: {{fileID: 0}}
  m_Transition: 1
  m_Colors:
    m_NormalColor: {{r: 1, g: 1, b: 1, a: 1}}
    m_HighlightedColor: {{r: 0.9607843, g: 0.9607843, b: 0.9607843, a: 1}}
    m_PressedColor: {{r: 0.78431374, g: 0.78431374, b: 0.78431374, a: 1}}
    m_SelectedColor: {{r: 0.9607843, g: 0.9607843, b: 0.9607843, a: 1}}
    m_DisabledColor: {{r: 0.78431374, g: 0.78431374, b: 0.78431374, a: 0.5019608}}
    m_ColorMultiplier: 1
    m_FadeDuration: 0.1
  m_SpriteState:
    m_HighlightedSprite: {{fileID: 0}}
    m_PressedSprite: {{fileID: 0}}
    m_SelectedSprite: {{fileID: 0}}
    m_DisabledSprite: {{fileID: 0}}
  m_AnimationTriggers:
    m_NormalTrigger: Normal
    m_HighlightedTrigger: Highlighted
    m_PressedTrigger: Pressed
    m_SelectedTrigger: Selected
    m_DisabledTrigger: Disabled
  m_Interactable: 1
  m_TargetGraphic: {{fileID: {target_graphic_fid}}}
  m_OnClick:
    m_PersistentCalls:
      m_Calls: []"""

# ── ID 풀 ──────────────────────────────────────────────────────────────────────
# Camera
cam_go=uid(); cam_tr=uid(); cam_comp=uid(); cam_audio=uid()
# EventSystem
es_go=uid(); es_tr=uid(); es_comp=uid(); es_input=uid()
# Canvas
canvas_go=uid(); canvas_rt=uid(); canvas_comp=uid(); canvas_scaler=uid(); canvas_raycaster=uid(); canvas_controller=uid()
# TopBar
topbar_go=uid(); topbar_rt=uid()
coin_go=uid(); coin_rt=uid(); coin_txt=uid(); coin_cr=uid()
gem_go=uid(); gem_rt=uid(); gem_txt=uid(); gem_cr=uid()
title_go=uid(); title_rt=uid(); title_txt=uid(); title_cr=uid()
# ScrollView
scroll_go=uid(); scroll_rt=uid(); scroll_comp=uid(); scroll_img=uid(); scroll_cr=uid()
viewport_go=uid(); viewport_rt=uid(); viewport_mask=uid(); viewport_img=uid(); viewport_cr=uid()
content_go=uid(); content_rt=uid(); content_vlg=uid(); content_csf=uid()
# BackButton
back_go=uid(); back_rt=uid(); back_btn=uid(); back_img=uid(); back_cr=uid()
back_txt_go=uid(); back_txt_rt=uid(); back_txt=uid(); back_txt_cr=uid()
# ErrorPanel
err_go=uid(); err_rt=uid(); err_img=uid(); err_cr=uid()
errtxt_go=uid(); errtxt_rt=uid(); errtxt_txt=uid(); errtxt_cr=uid()

blocks = []

# ── 씬 헤더 ───────────────────────────────────────────────────────────────────
blocks.append("""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!29 &1
OcclusionCullingSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_OcclusionBakeSettings:
    smallestOccluder: 5
    smallestHole: 0.25
    backfaceThreshold: 100
  m_SceneGUID: 00000000000000000000000000000000
  m_OcclusionCullingData: {fileID: 0}
--- !u!104 &2
RenderSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 10
  m_Fog: 0
  m_FogColor: {r: 0.5, g: 0.5, b: 0.5, a: 1}
  m_FogMode: 3
  m_FogDensity: 0.01
  m_LinearFogStart: 0
  m_LinearFogEnd: 300
  m_AmbientSkyColor: {r: 0.212, g: 0.227, b: 0.259, a: 1}
  m_AmbientEquatorColor: {r: 0.114, g: 0.125, b: 0.133, a: 1}
  m_AmbientGroundColor: {r: 0.047, g: 0.043, b: 0.035, a: 1}
  m_AmbientIntensity: 1
  m_AmbientMode: 3
  m_SubtractiveShadowColor: {r: 0.42, g: 0.478, b: 0.627, a: 1}
  m_SkyboxMaterial: {fileID: 0}
  m_HaloStrength: 0.5
  m_FlareStrength: 1
  m_FlareFadeSpeed: 3
  m_HaloTexture: {fileID: 0}
  m_SpotCookie: {fileID: 10001, guid: 0000000000000000e000000000000000, type: 0}
  m_DefaultReflectionMode: 0
  m_DefaultReflectionResolution: 128
  m_ReflectionBounces: 1
  m_ReflectionIntensity: 1
  m_CustomReflection: {fileID: 0}
  m_Sun: {fileID: 0}
  m_UseRadianceAmbientProbe: 0
--- !u!157 &3
LightmapSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 13
  m_BakeOnSceneLoad: 0
  m_GISettings:
    serializedVersion: 2
    m_BounceScale: 1
    m_IndirectOutputScale: 1
    m_AlbedoBoost: 1
    m_EnvironmentLightingMode: 0
    m_EnableBakedLightmaps: 0
    m_EnableRealtimeLightmaps: 0
  m_LightmapEditorSettings:
    serializedVersion: 12
    m_Resolution: 2
    m_BakeResolution: 40
    m_AtlasSize: 1024
    m_AO: 0
    m_AOMaxDistance: 1
    m_CompAOExponent: 1
    m_CompAOExponentDirect: 0
    m_ExtractAmbientOcclusion: 0
    m_Padding: 2
    m_LightmapParameters: {fileID: 0}
    m_LightmapsBakeMode: 1
    m_TextureCompression: 1
    m_ReflectionCompression: 2
    m_MixedBakeMode: 2
    m_BakeBackend: 1
    m_PVRSampling: 1
    m_PVRDirectSampleCount: 32
    m_PVRSampleCount: 512
    m_PVRBounces: 2
    m_PVREnvironmentSampleCount: 256
    m_PVREnvironmentReferencePointCount: 2048
    m_PVRFilteringMode: 1
    m_PVRDenoiserTypeDirect: 1
    m_PVRDenoiserTypeIndirect: 1
    m_PVRDenoiserTypeAO: 1
    m_PVRFilterTypeDirect: 0
    m_PVRFilterTypeIndirect: 0
    m_PVRFilterTypeAO: 0
    m_PVREnvironmentMIS: 1
    m_PVRCulling: 1
    m_PVRFilteringGaussRadiusDirect: 1
    m_PVRFilteringGaussRadiusIndirect: 1
    m_PVRFilteringGaussRadiusAO: 1
    m_PVRFilteringAtrousPositionSigmaDirect: 0.5
    m_PVRFilteringAtrousPositionSigmaIndirect: 2
    m_PVRFilteringAtrousPositionSigmaAO: 1
    m_ExportTrainingData: 0
    m_TrainingDataDestination: TrainingData
    m_LightProbeSampleCountMultiplier: 4
  m_LightingDataAsset: {fileID: 20201, guid: 0000000000000000f000000000000000, type: 0}
  m_LightingSettings: {fileID: 0}
--- !u!196 &4
NavMeshSettings:
  serializedVersion: 2
  m_ObjectHideFlags: 0
  m_BuildSettings:
    serializedVersion: 3
    agentTypeID: 0
    agentRadius: 0.5
    agentHeight: 2
    agentSlope: 45
    agentClimb: 0.4
    ledgeDropHeight: 0
    maxJumpAcrossDistance: 0
    minRegionArea: 2
    manualCellSize: 0
    cellSize: 0.16666667
    manualTileSize: 0
    tileSize: 256
    buildHeightMesh: 0
    maxJobWorkers: 0
    preserveTilesOutsideBounds: 0
    debug:
      m_Flags: 0
  m_NavMeshData: {fileID: 0}""")

# ── Main Camera ───────────────────────────────────────────────────────────────
blocks.append(go_block(cam_go, "Main Camera", 0,
    [cam_tr, cam_comp, cam_audio], active=1))
blocks.append(f"""--- !u!81 &{cam_audio}
AudioListener:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {cam_go}}}
  m_Enabled: 1
--- !u!20 &{cam_comp}
Camera:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {cam_go}}}
  m_Enabled: 1
  serializedVersion: 2
  m_ClearFlags: 1
  m_BackGroundColor: {{r: 0.08, g: 0.08, b: 0.12, a: 0}}
  m_projectionMatrixMode: 1
  m_GateFitMode: 2
  m_FOVAxisMode: 0
  m_Iso: 200
  m_ShutterSpeed: 0.005
  m_Aperture: 16
  m_FocusDistance: 10
  m_FocalLength: 50
  m_BladeCount: 5
  m_Curvature: {{x: 2, y: 11}}
  m_BarrelClipping: 0.25
  m_Anamorphism: 0
  m_SensorSize: {{x: 36, y: 24}}
  m_LensShift: {{x: 0, y: 0}}
  m_NormalizedViewPortRect:
    serializedVersion: 2
    x: 0
    y: 0
    width: 1
    height: 1
  near clip plane: 0.3
  far clip plane: 1000
  field of view: 60
  orthographic: 1
  orthographic size: 5
  m_Depth: -1
  m_CullingMask:
    serializedVersion: 2
    m_Bits: 4294967295
  m_RenderingPath: -1
  m_TargetTexture: {{fileID: 0}}
  m_TargetDisplay: 0
  m_TargetEye: 3
  m_HDR: 1
  m_AllowMSAA: 1
  m_AllowDynamicResolution: 0
  m_ForceIntoRT: 0
  m_OcclusionCulling: 1
  m_StereoConvergence: 10
  m_StereoSeparation: 0.022
--- !u!4 &{cam_tr}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {cam_go}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: -10}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}""")

# ── Canvas ────────────────────────────────────────────────────────────────────
canvas_children = [topbar_rt, scroll_rt, back_rt, err_rt]
blocks.append(go_block(canvas_go, "Canvas", 5,
    [canvas_rt, canvas_comp, canvas_scaler, canvas_raycaster, canvas_controller]))

blocks.append(f"""--- !u!223 &{canvas_comp}
Canvas:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {canvas_go}}}
  m_Enabled: 1
  serializedVersion: 3
  m_RenderMode: 0
  m_Camera: {{fileID: 0}}
  m_PlaneDistance: 100
  m_PixelPerfect: 0
  m_ReceivesEvents: 1
  m_OverrideSorting: 0
  m_OverridePixelPerfect: 0
  m_SortingBucketNormalizedSize: 0
  m_VertexColorAlwaysGammaSpace: 0
  m_AdditionalShaderChannelsFlag: 25
  m_UpdateRectTransformForStandalone: 0
  m_SortingLayerID: 0
  m_SortingOrder: 0
  m_TargetDisplay: 0
--- !u!114 &{canvas_scaler}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {canvas_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {G['CanvasScaler']}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.CanvasScaler
  m_UiScaleMode: 1
  m_ReferencePixelsPerUnit: 100
  m_ScaleFactor: 1
  m_ReferenceResolution: {{x: 1080, y: 2400}}
  m_ScreenMatchMode: 0
  m_MatchWidthOrHeight: 0.5
  m_PhysicalUnit: 3
  m_FallbackScreenDPI: 96
  m_DefaultSpriteDPI: 96
  m_DynamicPixelsPerUnit: 1
  m_PresetInfoIsWorld: 0
--- !u!114 &{canvas_raycaster}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {canvas_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {G['GraphicRaycaster']}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.GraphicRaycaster
  m_IgnoreReversedGraphics: 1
  m_BlockingObjects: 0
  m_BlockingMask:
    serializedVersion: 2
    m_Bits: 4294967295""")

# StoreUIController — 슬롯은 런타임에 생성되므로 SerializeField 레퍼런스를 여기서 연결
blocks.append(f"""--- !u!114 &{canvas_controller}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {canvas_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {G['StoreUIController']}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::StoreUIController
  _coinText: {{fileID: {coin_txt}}}
  _gemText: {{fileID: {gem_txt}}}
  _itemListContent: {{fileID: {content_rt}}}
  _itemSlotPrefab: {{fileID: 0}}
  _itemsForSale: []
  _errorPanel: {{fileID: {err_go}}}
  _errorText: {{fileID: {errtxt_txt}}}
  _backButton: {{fileID: {back_btn}}}""")

blocks.append(rect_transform(canvas_rt, canvas_go, 0, canvas_children,
    anchor_min="0 0", anchor_max="0 0", pos="0 0", size="0 0", pivot="0 0"))

# ── TopBar ────────────────────────────────────────────────────────────────────
blocks.append(go_block(topbar_go, "TopBar", 5, [topbar_rt]))
blocks.append(rect_transform(topbar_rt, topbar_go, canvas_rt,
    [coin_rt, gem_rt, title_rt],
    anchor_min="0 1", anchor_max="1 1",
    pos="0 0", size="0 150", pivot="0.5 1"))

# CoinText (좌측)
blocks.append(go_block(coin_go, "CoinText", 5, [coin_rt, coin_txt, coin_cr]))
blocks.append(rect_transform(coin_rt, coin_go, topbar_rt, [],
    anchor_min="0 0", anchor_max="0.35 1",
    pos="20 0", size="0 0", pivot="0 0.5"))
blocks.append(tmp_text(coin_txt, coin_go, "100", font_size=36, color="1 0.85 0.2 1", h_align=1, v_align=256))
blocks.append(canvas_renderer(coin_cr, coin_go))

# GemText (우측)
blocks.append(go_block(gem_go, "GemText", 5, [gem_rt, gem_txt, gem_cr]))
blocks.append(rect_transform(gem_rt, gem_go, topbar_rt, [],
    anchor_min="0.65 0", anchor_max="1 1",
    pos="-20 0", size="0 0", pivot="1 0.5"))
blocks.append(tmp_text(gem_txt, gem_go, "0", font_size=36, color="0.4 0.8 1 1", h_align=4, v_align=256))
blocks.append(canvas_renderer(gem_cr, gem_go))

# TitleText (가운데)
blocks.append(go_block(title_go, "TitleText", 5, [title_rt, title_txt, title_cr]))
blocks.append(rect_transform(title_rt, title_go, topbar_rt, [],
    anchor_min="0.25 0", anchor_max="0.75 1",
    pos="0 0", size="0 0", pivot="0.5 0.5"))
blocks.append(tmp_text(title_txt, title_go, "Store", font_size=48, color="1 1 1 1", h_align=2, v_align=256, bold=1))
blocks.append(canvas_renderer(title_cr, title_go))

# ── ScrollView ────────────────────────────────────────────────────────────────
blocks.append(go_block(scroll_go, "ScrollView", 5,
    [scroll_rt, scroll_comp, scroll_img, scroll_cr]))
blocks.append(rect_transform(scroll_rt, scroll_go, canvas_rt, [viewport_rt],
    anchor_min="0 0", anchor_max="1 1",
    pos="0 0", size="-160 -310", pivot="0.5 0.5"))
# ScrollRect 배경 이미지 (투명)
blocks.append(image_component(scroll_img, scroll_go, color="1 1 1 0", raycast=0))
blocks.append(canvas_renderer(scroll_cr, scroll_go))
blocks.append(f"""--- !u!114 &{scroll_comp}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {scroll_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {G['ScrollRect']}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.ScrollRect
  m_Content: {{fileID: {content_rt}}}
  m_Horizontal: 0
  m_Vertical: 1
  m_MovementType: 1
  m_Elasticity: 0.1
  m_Inertia: 1
  m_DecelerationRate: 0.135
  m_ScrollSensitivity: 1
  m_Viewport: {{fileID: {viewport_rt}}}
  m_HorizontalScrollbar: {{fileID: 0}}
  m_VerticalScrollbar: {{fileID: 0}}
  m_HorizontalScrollbarVisibility: 2
  m_VerticalScrollbarVisibility: 2
  m_HorizontalScrollbarSpacing: -3
  m_VerticalScrollbarSpacing: -3
  m_OnValueChanged:
    m_PersistentCalls:
      m_Calls: []""")

# Viewport
blocks.append(go_block(viewport_go, "Viewport", 5,
    [viewport_rt, viewport_mask, viewport_img, viewport_cr]))
blocks.append(rect_transform(viewport_rt, viewport_go, scroll_rt, [content_rt],
    anchor_min="0 0", anchor_max="1 1",
    pos="0 0", size="0 0", pivot="0 1"))
blocks.append(image_component(viewport_img, viewport_go, color="1 1 1 1", sprite_fid=10907, raycast=0))
blocks.append(canvas_renderer(viewport_cr, viewport_go))
blocks.append(f"""--- !u!114 &{viewport_mask}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {viewport_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {G['Mask']}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Mask
  m_ShowMaskGraphic: 0""")

# Content
blocks.append(go_block(content_go, "Content", 5, [content_rt, content_vlg, content_csf]))
blocks.append(rect_transform(content_rt, content_go, viewport_rt, [],
    anchor_min="0 1", anchor_max="1 1",
    pos="0 0", size="0 0", pivot="0.5 1"))
blocks.append(f"""--- !u!114 &{content_vlg}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {content_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {G['VerticalLayoutGroup']}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.VerticalLayoutGroup
  m_Padding:
    m_Left: 20
    m_Right: 20
    m_Top: 20
    m_Bottom: 20
  m_Spacing: 16
  m_ChildAlignment: 0
  m_ReverseArrangement: 0
  m_ChildControlWidth: 1
  m_ChildControlHeight: 0
  m_ChildScaleWidth: 0
  m_ChildScaleHeight: 0
  m_ChildForceExpandWidth: 1
  m_ChildForceExpandHeight: 0
--- !u!114 &{content_csf}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {content_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {G['ContentSizeFitter']}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.ContentSizeFitter
  m_HorizontalFit: 0
  m_VerticalFit: 2""")

# ── BackButton ────────────────────────────────────────────────────────────────
blocks.append(go_block(back_go, "BackButton", 5,
    [back_rt, back_btn, back_img, back_cr]))
blocks.append(rect_transform(back_rt, back_go, canvas_rt, [back_txt_rt],
    anchor_min="0 0", anchor_max="1 0",
    pos="0 0", size="0 160", pivot="0.5 0"))
blocks.append(button_component(back_btn, back_go, back_img))
blocks.append(image_component(back_img, back_go, color="0.2 0.2 0.2 1", sprite_fid=10905))
blocks.append(canvas_renderer(back_cr, back_go))
# BackButton Label
blocks.append(go_block(back_txt_go, "Text (TMP)", 5, [back_txt_rt, back_txt, back_txt_cr]))
blocks.append(rect_transform(back_txt_rt, back_txt_go, back_rt, [],
    anchor_min="0 0", anchor_max="1 1",
    pos="0 0", size="0 0", pivot="0.5 0.5"))
blocks.append(tmp_text(back_txt, back_txt_go, "< Back", font_size=40, color="1 1 1 1", h_align=2, v_align=512))
blocks.append(canvas_renderer(back_txt_cr, back_txt_go))

# ── ErrorPanel (숨김) ──────────────────────────────────────────────────────────
blocks.append(go_block(err_go, "ErrorPanel", 5, [err_rt, err_img, err_cr], active=0))
blocks.append(rect_transform(err_rt, err_go, canvas_rt, [errtxt_rt],
    anchor_min="0 0", anchor_max="1 1",
    pos="0 0", size="0 0", pivot="0.5 0.5"))
blocks.append(image_component(err_img, err_go, color="0 0 0 0.75"))
blocks.append(canvas_renderer(err_cr, err_go))
# ErrorText
blocks.append(go_block(errtxt_go, "ErrorText", 5, [errtxt_rt, errtxt_txt, errtxt_cr]))
blocks.append(rect_transform(errtxt_rt, errtxt_go, err_rt, [],
    anchor_min="0.1 0.35", anchor_max="0.9 0.65",
    pos="0 0", size="0 0", pivot="0.5 0.5"))
blocks.append(tmp_text(errtxt_txt, errtxt_go, "", font_size=40, color="1 0.4 0.4 1", h_align=2, v_align=512))
blocks.append(canvas_renderer(errtxt_cr, errtxt_go))

# ── EventSystem ───────────────────────────────────────────────────────────────
blocks.append(go_block(es_go, "EventSystem", 0, [es_tr, es_comp, es_input]))
blocks.append(f"""--- !u!114 &{es_input}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {es_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {G['InputModule']}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: Unity.InputSystem::UnityEngine.InputSystem.UI.InputSystemUIInputModule
  m_SendPointerHoverToParent: 1
  m_MoveRepeatDelay: 0.5
  m_MoveRepeatRate: 0.1
  m_XRTrackingOrigin: {{fileID: 0}}
  m_ActionsAsset: {{fileID: -944628639613478452, guid: {G['InputActions']}, type: 3}}
  m_PointAction: {{fileID: -1654692200621890270, guid: {G['InputActions']}, type: 3}}
  m_MoveAction: {{fileID: -8784545083839296357, guid: {G['InputActions']}, type: 3}}
  m_SubmitAction: {{fileID: 392368643174621059, guid: {G['InputActions']}, type: 3}}
  m_CancelAction: {{fileID: 7727032971491509709, guid: {G['InputActions']}, type: 3}}
  m_LeftClickAction: {{fileID: 3001919216989983466, guid: {G['InputActions']}, type: 3}}
  m_MiddleClickAction: {{fileID: -2185481485913320682, guid: {G['InputActions']}, type: 3}}
  m_RightClickAction: {{fileID: -4090225696740746782, guid: {G['InputActions']}, type: 3}}
  m_ScrollWheelAction: {{fileID: 6240969308177333660, guid: {G['InputActions']}, type: 3}}
  m_TrackedDevicePositionAction: {{fileID: 6564999863303420839, guid: {G['InputActions']}, type: 3}}
  m_TrackedDeviceOrientationAction: {{fileID: 7970375526676320489, guid: {G['InputActions']}, type: 3}}
  m_DeselectOnBackgroundClick: 1
  m_PointerBehavior: 0
  m_CursorLockBehavior: 0
  m_ScrollDeltaPerTick: 6
--- !u!114 &{es_comp}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {es_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {G['EventSystem']}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.EventSystems.EventSystem
  m_FirstSelected: {{fileID: 0}}
  m_sendNavigationEvents: 1
  m_DragThreshold: 10
--- !u!4 &{es_tr}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {es_go}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}""")

# ── SceneRoots ────────────────────────────────────────────────────────────────
blocks.append(f"""--- !u!1660057539 &9223372036854775807
SceneRoots:
  m_ObjectHideFlags: 0
  m_Roots:
  - {{fileID: {cam_tr}}}
  - {{fileID: {canvas_rt}}}
  - {{fileID: {es_tr}}}""")

# ── 파일 출력 ─────────────────────────────────────────────────────────────────
out_path = os.path.join(os.path.dirname(__file__),
    "..", "Assets", "_Game", "Scenes", "Store.unity")
out_path = os.path.normpath(out_path)

with open(out_path, "w", encoding="utf-8", newline="\n") as f:
    f.write("\n".join(blocks) + "\n")

print(f"[OK] Store.unity generated: {out_path}")
print("[NOTE] In Unity Editor:")
print("    1. Assign ItemSlot prefab to StoreUIController._itemSlotPrefab")
print("    2. Drag ItemSO assets into StoreUIController._itemsForSale array")
