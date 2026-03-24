"""
build_geckolist_scene.py
GeckoList.unity 씬을 Python으로 직접 생성.

레이아웃 (1080×2400 기준):
  ┌──────────────────────────┐
  │  TopBar  (h=150, top)    │  CoinText / GemText / "My Geckos"
  ├──────────────────────────┤
  │                          │
  │  ScrollView (fill)       │  게코 슬롯 목록 (런타임 생성)
  │                          │
  ├──────────────────────────┤
  │  AdoptButton + BackButton│  (h=160, bottom, 좌우 분할)
  └──────────────────────────┘

  AdoptPanel (전체화면 오버레이, 평소 숨김)
    어두운 BG + 중앙 패널
      TitleText / SpeciesDropdown / NameInputField / Confirm / Cancel

  ErrorPanel (전체화면, 평소 숨김)

사용법:  python Tools/build_geckolist_scene.py
"""

import random, os

G = {
    "Image"              : "fe87c0e1cc204ed48ad3b37840f39efc",
    "Button"             : "4e29b1a8efbd4b44bb3f3716e73f07ff",
    "TMP_Text"           : "f4688fdb7df04437aeb418b961361dc5",
    "TMP_Dropdown"       : "7b743370ac3e4ec2a1668f5455a8ef8a",
    "TMP_InputField"     : "2da0c512f12947e489f739169773d7ca",
    "CanvasScaler"       : "0cd44c1031e13a943bb63640046fad76",
    "GraphicRaycaster"   : "dc42784cf147c0c48a680349fa168899",
    "EventSystem"        : "76c392e42b5098c458856cdf6ecaaaa1",
    "InputModule"        : "01614664b831546d2ae94a42149d80ac",
    "ScrollRect"         : "1aa08ab6e0800fa44ae55d278d1423e3",
    "Mask"               : "31a19414c41e5ae4aae2af33fee712f6",
    "VerticalLayoutGroup": "59f8146938fff824cb5fd77236b75775",
    "ContentSizeFitter"  : "3245ec927659c4140ac4f8d17403cc18",
    "GeckoListUI"        : "36f56723b70e87b4fbbd643e2f2f1a59",
    "InputActions"       : "ca9f5fa95ffab41fb9a615ab714db018",
    "TMP_Font"           : "8f586378b4e144a9851e7b34d9b748ee",
}

_used = set()
def uid():
    while True:
        n = random.randint(100_000_000, 2_000_000_000)
        if n not in _used:
            _used.add(n)
            return n

# ── YAML 헬퍼 (build_store_scene.py 와 동일) ───────────────────────────────────

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

def image_comp(fid, go_fid, color="1 1 1 1", sprite_fid=10905, raycast=1):
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

def button_comp(fid, go_fid, target_fid):
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
  m_TargetGraphic: {{fileID: {target_fid}}}
  m_OnClick:
    m_PersistentCalls:
      m_Calls: []"""

def scrollview_block(scroll_go, scroll_rt, scroll_comp, scroll_img, scroll_cr,
                     viewport_rt, content_rt, parent_rt):
    return f"""--- !u!114 &{scroll_comp}
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
      m_Calls: []"""

# ══════════════════════════════════════════════════════════════════════════════
# ID 풀
# ══════════════════════════════════════════════════════════════════════════════
cam_go=uid(); cam_tr=uid(); cam_comp=uid(); cam_audio=uid()
es_go=uid(); es_tr=uid(); es_comp=uid(); es_input=uid()
canvas_go=uid(); canvas_rt=uid(); canvas_comp=uid()
canvas_scaler=uid(); canvas_raycaster=uid(); canvas_controller=uid()

# TopBar
topbar_go=uid(); topbar_rt=uid()
coin_go=uid(); coin_rt=uid(); coin_txt=uid(); coin_cr=uid()
gem_go=uid(); gem_rt=uid(); gem_txt=uid(); gem_cr=uid()
title_go=uid(); title_rt=uid(); title_txt=uid(); title_cr=uid()

# ScrollView
sv_go=uid(); sv_rt=uid(); sv_comp=uid(); sv_img=uid(); sv_cr=uid()
vp_go=uid(); vp_rt=uid(); vp_mask=uid(); vp_img=uid(); vp_cr=uid()
ct_go=uid(); ct_rt=uid(); ct_vlg=uid(); ct_csf=uid()

# BottomBar (Adopt + Back)
btm_go=uid(); btm_rt=uid()
adopt_go=uid(); adopt_rt=uid(); adopt_btn=uid(); adopt_img=uid(); adopt_cr=uid()
adopt_lbl_go=uid(); adopt_lbl_rt=uid(); adopt_lbl=uid(); adopt_lbl_cr=uid()
back_go=uid(); back_rt=uid(); back_btn=uid(); back_img=uid(); back_cr=uid()
back_lbl_go=uid(); back_lbl_rt=uid(); back_lbl=uid(); back_lbl_cr=uid()

# AdoptPanel (전체 오버레이, 숨김)
apanel_go=uid(); apanel_rt=uid(); apanel_img=uid(); apanel_cr=uid()
apanel_inner_go=uid(); apanel_inner_rt=uid(); apanel_inner_img=uid(); apanel_inner_cr=uid()
ap_title_go=uid(); ap_title_rt=uid(); ap_title_txt=uid(); ap_title_cr=uid()
ap_dd_go=uid(); ap_dd_rt=uid(); ap_dd_comp=uid(); ap_dd_img=uid(); ap_dd_cr=uid()
ap_input_go=uid(); ap_input_rt=uid(); ap_input_comp=uid(); ap_input_img=uid(); ap_input_cr=uid()
ap_input_txt_go=uid(); ap_input_txt_rt=uid(); ap_input_txt=uid(); ap_input_txt_cr=uid()
ap_input_ph_go=uid(); ap_input_ph_rt=uid(); ap_input_ph=uid(); ap_input_ph_cr=uid()
ap_confirm_go=uid(); ap_confirm_rt=uid(); ap_confirm_btn=uid(); ap_confirm_img=uid(); ap_confirm_cr=uid()
ap_confirm_lbl_go=uid(); ap_confirm_lbl_rt=uid(); ap_confirm_lbl=uid(); ap_confirm_lbl_cr=uid()
ap_cancel_go=uid(); ap_cancel_rt=uid(); ap_cancel_btn=uid(); ap_cancel_img=uid(); ap_cancel_cr=uid()
ap_cancel_lbl_go=uid(); ap_cancel_lbl_rt=uid(); ap_cancel_lbl=uid(); ap_cancel_lbl_cr=uid()

# ErrorPanel
err_go=uid(); err_rt=uid(); err_img=uid(); err_cr=uid()
errtxt_go=uid(); errtxt_rt=uid(); errtxt_txt=uid(); errtxt_cr=uid()

# ══════════════════════════════════════════════════════════════════════════════
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

# ── Camera ────────────────────────────────────────────────────────────────────
blocks.append(go_block(cam_go, "Main Camera", 0, [cam_tr, cam_comp, cam_audio]))
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
canvas_children = [topbar_rt, sv_rt, btm_rt, apanel_rt, err_rt]
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

# GeckoListUIController
blocks.append(f"""--- !u!114 &{canvas_controller}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {canvas_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {G['GeckoListUI']}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::GeckoListUIController
  _coinText: {{fileID: {coin_txt}}}
  _gemText: {{fileID: {gem_txt}}}
  _geckoListContent: {{fileID: {ct_rt}}}
  _geckoSlotPrefab: {{fileID: 0}}
  _adoptPanel: {{fileID: {apanel_go}}}
  _speciesDropdown: {{fileID: {ap_dd_comp}}}
  _nameInputField: {{fileID: {ap_input_comp}}}
  _confirmAdoptButton: {{fileID: {ap_confirm_btn}}}
  _cancelAdoptButton: {{fileID: {ap_cancel_btn}}}
  _adoptButton: {{fileID: {adopt_btn}}}
  _speciesForSale: []
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

blocks.append(go_block(coin_go, "CoinText", 5, [coin_rt, coin_txt, coin_cr]))
blocks.append(rect_transform(coin_rt, coin_go, topbar_rt, [],
    anchor_min="0 0", anchor_max="0.35 1",
    pos="20 0", size="0 0", pivot="0 0.5"))
blocks.append(tmp_text(coin_txt, coin_go, "100", font_size=36, color="1 0.85 0.2 1", h_align=1, v_align=256))
blocks.append(canvas_renderer(coin_cr, coin_go))

blocks.append(go_block(gem_go, "GemText", 5, [gem_rt, gem_txt, gem_cr]))
blocks.append(rect_transform(gem_rt, gem_go, topbar_rt, [],
    anchor_min="0.65 0", anchor_max="1 1",
    pos="-20 0", size="0 0", pivot="1 0.5"))
blocks.append(tmp_text(gem_txt, gem_go, "0", font_size=36, color="0.4 0.8 1 1", h_align=4, v_align=256))
blocks.append(canvas_renderer(gem_cr, gem_go))

blocks.append(go_block(title_go, "TitleText", 5, [title_rt, title_txt, title_cr]))
blocks.append(rect_transform(title_rt, title_go, topbar_rt, [],
    anchor_min="0.25 0", anchor_max="0.75 1",
    pos="0 0", size="0 0", pivot="0.5 0.5"))
blocks.append(tmp_text(title_txt, title_go, "My Geckos", font_size=48, color="1 1 1 1", h_align=2, v_align=256, bold=1))
blocks.append(canvas_renderer(title_cr, title_go))

# ── ScrollView ────────────────────────────────────────────────────────────────
blocks.append(go_block(sv_go, "ScrollView", 5, [sv_rt, sv_comp, sv_img, sv_cr]))
blocks.append(rect_transform(sv_rt, sv_go, canvas_rt, [vp_rt],
    anchor_min="0 0", anchor_max="1 1",
    pos="0 0", size="0 -310", pivot="0.5 0.5"))
blocks.append(image_comp(sv_img, sv_go, color="1 1 1 0", raycast=0))
blocks.append(canvas_renderer(sv_cr, sv_go))
blocks.append(scrollview_block(sv_go, sv_rt, sv_comp, sv_img, sv_cr, vp_rt, ct_rt, canvas_rt))

blocks.append(go_block(vp_go, "Viewport", 5, [vp_rt, vp_mask, vp_img, vp_cr]))
blocks.append(rect_transform(vp_rt, vp_go, sv_rt, [ct_rt],
    anchor_min="0 0", anchor_max="1 1",
    pos="0 0", size="0 0", pivot="0 1"))
blocks.append(image_comp(vp_img, vp_go, color="1 1 1 1", sprite_fid=10907, raycast=0))
blocks.append(canvas_renderer(vp_cr, vp_go))
blocks.append(f"""--- !u!114 &{vp_mask}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {vp_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {G['Mask']}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Mask
  m_ShowMaskGraphic: 0""")

blocks.append(go_block(ct_go, "Content", 5, [ct_rt, ct_vlg, ct_csf]))
blocks.append(rect_transform(ct_rt, ct_go, vp_rt, [],
    anchor_min="0 1", anchor_max="1 1",
    pos="0 0", size="0 0", pivot="0.5 1"))
blocks.append(f"""--- !u!114 &{ct_vlg}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {ct_go}}}
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
--- !u!114 &{ct_csf}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {ct_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {G['ContentSizeFitter']}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.ContentSizeFitter
  m_HorizontalFit: 0
  m_VerticalFit: 2""")

# ── BottomBar (Adopt + Back) ───────────────────────────────────────────────────
blocks.append(go_block(btm_go, "BottomBar", 5, [btm_rt]))
blocks.append(rect_transform(btm_rt, btm_go, canvas_rt,
    [adopt_rt, back_rt],
    anchor_min="0 0", anchor_max="1 0",
    pos="0 0", size="0 160", pivot="0.5 0"))

# AdoptButton (우측 절반)
blocks.append(go_block(adopt_go, "AdoptButton", 5,
    [adopt_rt, adopt_btn, adopt_img, adopt_cr]))
blocks.append(rect_transform(adopt_rt, adopt_go, btm_rt, [adopt_lbl_rt],
    anchor_min="0.5 0", anchor_max="1 1",
    pos="0 0", size="0 0", pivot="0.5 0.5"))
blocks.append(button_comp(adopt_btn, adopt_go, adopt_img))
blocks.append(image_comp(adopt_img, adopt_go, color="0.2 0.6 0.3 1", sprite_fid=10905))
blocks.append(canvas_renderer(adopt_cr, adopt_go))
blocks.append(go_block(adopt_lbl_go, "Text (TMP)", 5, [adopt_lbl_rt, adopt_lbl, adopt_lbl_cr]))
blocks.append(rect_transform(adopt_lbl_rt, adopt_lbl_go, adopt_rt, [],
    anchor_min="0 0", anchor_max="1 1",
    pos="0 0", size="0 0", pivot="0.5 0.5"))
blocks.append(tmp_text(adopt_lbl, adopt_lbl_go, "+ Adopt", font_size=40, color="1 1 1 1", h_align=2, v_align=512))
blocks.append(canvas_renderer(adopt_lbl_cr, adopt_lbl_go))

# BackButton (좌측 절반)
blocks.append(go_block(back_go, "BackButton", 5,
    [back_rt, back_btn, back_img, back_cr]))
blocks.append(rect_transform(back_rt, back_go, btm_rt, [back_lbl_rt],
    anchor_min="0 0", anchor_max="0.5 1",
    pos="0 0", size="0 0", pivot="0.5 0.5"))
blocks.append(button_comp(back_btn, back_go, back_img))
blocks.append(image_comp(back_img, back_go, color="0.2 0.2 0.2 1", sprite_fid=10905))
blocks.append(canvas_renderer(back_cr, back_go))
blocks.append(go_block(back_lbl_go, "Text (TMP)", 5, [back_lbl_rt, back_lbl, back_lbl_cr]))
blocks.append(rect_transform(back_lbl_rt, back_lbl_go, back_rt, [],
    anchor_min="0 0", anchor_max="1 1",
    pos="0 0", size="0 0", pivot="0.5 0.5"))
blocks.append(tmp_text(back_lbl, back_lbl_go, "< Back", font_size=40, color="1 1 1 1", h_align=2, v_align=512))
blocks.append(canvas_renderer(back_lbl_cr, back_lbl_go))

# ── AdoptPanel (오버레이, 숨김) ───────────────────────────────────────────────
blocks.append(go_block(apanel_go, "AdoptPanel", 5,
    [apanel_rt, apanel_img, apanel_cr], active=0))
blocks.append(rect_transform(apanel_rt, apanel_go, canvas_rt, [apanel_inner_rt],
    anchor_min="0 0", anchor_max="1 1",
    pos="0 0", size="0 0", pivot="0.5 0.5"))
blocks.append(image_comp(apanel_img, apanel_go, color="0 0 0 0.75"))
blocks.append(canvas_renderer(apanel_cr, apanel_go))

# 중앙 패널
inner_children = [ap_title_rt, ap_dd_rt, ap_input_rt, ap_confirm_rt, ap_cancel_rt]
blocks.append(go_block(apanel_inner_go, "Panel", 5,
    [apanel_inner_rt, apanel_inner_img, apanel_inner_cr]))
blocks.append(rect_transform(apanel_inner_rt, apanel_inner_go, apanel_rt, inner_children,
    anchor_min="0.1 0.25", anchor_max="0.9 0.75",
    pos="0 0", size="0 0", pivot="0.5 0.5"))
blocks.append(image_comp(apanel_inner_img, apanel_inner_go, color="0.15 0.15 0.2 1", sprite_fid=10907))
blocks.append(canvas_renderer(apanel_inner_cr, apanel_inner_go))

# Title
blocks.append(go_block(ap_title_go, "AdoptTitle", 5, [ap_title_rt, ap_title_txt, ap_title_cr]))
blocks.append(rect_transform(ap_title_rt, ap_title_go, apanel_inner_rt, [],
    anchor_min="0 0.78", anchor_max="1 1",
    pos="0 0", size="0 0", pivot="0.5 0.5"))
blocks.append(tmp_text(ap_title_txt, ap_title_go, "Adopt a Gecko", font_size=48, color="1 1 1 1", h_align=2, v_align=256, bold=1))
blocks.append(canvas_renderer(ap_title_cr, ap_title_go))

# SpeciesDropdown (TMP_Dropdown)
blocks.append(go_block(ap_dd_go, "SpeciesDropdown", 5,
    [ap_dd_rt, ap_dd_comp, ap_dd_img, ap_dd_cr]))
blocks.append(rect_transform(ap_dd_rt, ap_dd_go, apanel_inner_rt, [],
    anchor_min="0.05 0.55", anchor_max="0.95 0.75",
    pos="0 0", size="0 0", pivot="0.5 0.5"))
blocks.append(image_comp(ap_dd_img, ap_dd_go, color="0.25 0.25 0.3 1", sprite_fid=10905))
blocks.append(canvas_renderer(ap_dd_cr, ap_dd_go))
blocks.append(f"""--- !u!114 &{ap_dd_comp}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {ap_dd_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {G['TMP_Dropdown']}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: Unity.TextMeshPro::TMPro.TMP_Dropdown
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
  m_TargetGraphic: {{fileID: {ap_dd_img}}}
  m_Template: {{fileID: 0}}
  m_CaptionText: {{fileID: 0}}
  m_CaptionImage: {{fileID: 0}}
  m_Placeholder: {{fileID: 0}}
  m_ItemText: {{fileID: 0}}
  m_ItemImage: {{fileID: 0}}
  m_Value: 0
  m_Options: []
  m_OnValueChanged:
    m_PersistentCalls:
      m_Calls: []
  m_AlphaFadeSpeed: 0.15""")

# NameInputField (TMP_InputField)
input_children = [ap_input_txt_rt, ap_input_ph_rt]
blocks.append(go_block(ap_input_go, "NameInputField", 5,
    [ap_input_rt, ap_input_comp, ap_input_img, ap_input_cr]))
blocks.append(rect_transform(ap_input_rt, ap_input_go, apanel_inner_rt, input_children,
    anchor_min="0.05 0.33", anchor_max="0.95 0.52",
    pos="0 0", size="0 0", pivot="0.5 0.5"))
blocks.append(image_comp(ap_input_img, ap_input_go, color="0.25 0.25 0.3 1", sprite_fid=10905))
blocks.append(canvas_renderer(ap_input_cr, ap_input_go))
blocks.append(f"""--- !u!114 &{ap_input_comp}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {ap_input_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {G['TMP_InputField']}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: Unity.TextMeshPro::TMPro.TMP_InputField
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
  m_TargetGraphic: {{fileID: {ap_input_img}}}
  m_TextViewport: {{fileID: 0}}
  m_TextComponent: {{fileID: {ap_input_txt}}}
  m_Placeholder: {{fileID: {ap_input_ph}}}
  m_VerticalScrollbar: {{fileID: 0}}
  m_VerticalScrollbarEventHandler: {{fileID: 0}}
  m_LayoutGroup: {{fileID: 0}}
  m_ContentType: 0
  m_InputType: 0
  m_AsteriskChar: 42
  m_KeyboardType: 0
  m_LineType: 0
  m_HideMobileInput: 0
  m_HideSoftKeyboard: 0
  m_CharacterLimit: 20
  m_OnEndEdit:
    m_PersistentCalls:
      m_Calls: []
  m_OnSubmit:
    m_PersistentCalls:
      m_Calls: []
  m_OnSelect:
    m_PersistentCalls:
      m_Calls: []
  m_OnDeselect:
    m_PersistentCalls:
      m_Calls: []
  m_OnTextSelection:
    m_PersistentCalls:
      m_Calls: []
  m_OnEndTextSelection:
    m_PersistentCalls:
      m_Calls: []
  m_OnValueChanged:
    m_PersistentCalls:
      m_Calls: []
  m_OnTouchScreenKeyboardStatusChanged:
    m_PersistentCalls:
      m_Calls: []
  m_CaretWidth: 1
  m_ReadOnly: 0
  m_RichText: 0
  m_GlobalPointSize: 14
  m_GlobalFontAsset: {{fileID: 11400000, guid: {G['TMP_Font']}, type: 2}}""")

# InputField 텍스트 / 플레이스홀더
blocks.append(go_block(ap_input_txt_go, "Text", 5, [ap_input_txt_rt, ap_input_txt, ap_input_txt_cr]))
blocks.append(rect_transform(ap_input_txt_rt, ap_input_txt_go, ap_input_rt, [],
    anchor_min="0 0", anchor_max="1 1",
    pos="10 0", size="-20 0", pivot="0.5 0.5"))
blocks.append(tmp_text(ap_input_txt, ap_input_txt_go, "", font_size=36, color="1 1 1 1", h_align=1, v_align=256))
blocks.append(canvas_renderer(ap_input_txt_cr, ap_input_txt_go))

blocks.append(go_block(ap_input_ph_go, "Placeholder", 5, [ap_input_ph_rt, ap_input_ph, ap_input_ph_cr]))
blocks.append(rect_transform(ap_input_ph_rt, ap_input_ph_go, ap_input_rt, [],
    anchor_min="0 0", anchor_max="1 1",
    pos="10 0", size="-20 0", pivot="0.5 0.5"))
blocks.append(tmp_text(ap_input_ph, ap_input_ph_go, "Gecko name...", font_size=36, color="0.6 0.6 0.6 1", h_align=1, v_align=256))
blocks.append(canvas_renderer(ap_input_ph_cr, ap_input_ph_go))

# Confirm Button
blocks.append(go_block(ap_confirm_go, "ConfirmButton", 5,
    [ap_confirm_rt, ap_confirm_btn, ap_confirm_img, ap_confirm_cr]))
blocks.append(rect_transform(ap_confirm_rt, ap_confirm_go, apanel_inner_rt, [ap_confirm_lbl_rt],
    anchor_min="0.55 0.05", anchor_max="0.95 0.28",
    pos="0 0", size="0 0", pivot="0.5 0.5"))
blocks.append(button_comp(ap_confirm_btn, ap_confirm_go, ap_confirm_img))
blocks.append(image_comp(ap_confirm_img, ap_confirm_go, color="0.2 0.6 0.3 1", sprite_fid=10905))
blocks.append(canvas_renderer(ap_confirm_cr, ap_confirm_go))
blocks.append(go_block(ap_confirm_lbl_go, "Text (TMP)", 5, [ap_confirm_lbl_rt, ap_confirm_lbl, ap_confirm_lbl_cr]))
blocks.append(rect_transform(ap_confirm_lbl_rt, ap_confirm_lbl_go, ap_confirm_rt, [],
    anchor_min="0 0", anchor_max="1 1",
    pos="0 0", size="0 0", pivot="0.5 0.5"))
blocks.append(tmp_text(ap_confirm_lbl, ap_confirm_lbl_go, "Adopt!", font_size=40, color="1 1 1 1", h_align=2, v_align=512))
blocks.append(canvas_renderer(ap_confirm_lbl_cr, ap_confirm_lbl_go))

# Cancel Button
blocks.append(go_block(ap_cancel_go, "CancelButton", 5,
    [ap_cancel_rt, ap_cancel_btn, ap_cancel_img, ap_cancel_cr]))
blocks.append(rect_transform(ap_cancel_rt, ap_cancel_go, apanel_inner_rt, [ap_cancel_lbl_rt],
    anchor_min="0.05 0.05", anchor_max="0.45 0.28",
    pos="0 0", size="0 0", pivot="0.5 0.5"))
blocks.append(button_comp(ap_cancel_btn, ap_cancel_go, ap_cancel_img))
blocks.append(image_comp(ap_cancel_img, ap_cancel_go, color="0.5 0.2 0.2 1", sprite_fid=10905))
blocks.append(canvas_renderer(ap_cancel_cr, ap_cancel_go))
blocks.append(go_block(ap_cancel_lbl_go, "Text (TMP)", 5, [ap_cancel_lbl_rt, ap_cancel_lbl, ap_cancel_lbl_cr]))
blocks.append(rect_transform(ap_cancel_lbl_rt, ap_cancel_lbl_go, ap_cancel_rt, [],
    anchor_min="0 0", anchor_max="1 1",
    pos="0 0", size="0 0", pivot="0.5 0.5"))
blocks.append(tmp_text(ap_cancel_lbl, ap_cancel_lbl_go, "Cancel", font_size=40, color="1 1 1 1", h_align=2, v_align=512))
blocks.append(canvas_renderer(ap_cancel_lbl_cr, ap_cancel_lbl_go))

# ── ErrorPanel ────────────────────────────────────────────────────────────────
blocks.append(go_block(err_go, "ErrorPanel", 5, [err_rt, err_img, err_cr], active=0))
blocks.append(rect_transform(err_rt, err_go, canvas_rt, [errtxt_rt],
    anchor_min="0 0", anchor_max="1 1",
    pos="0 0", size="0 0", pivot="0.5 0.5"))
blocks.append(image_comp(err_img, err_go, color="0 0 0 0.75"))
blocks.append(canvas_renderer(err_cr, err_go))
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
    "..", "Assets", "_Game", "Scenes", "GeckoList.unity")
out_path = os.path.normpath(out_path)

with open(out_path, "w", encoding="utf-8", newline="\n") as f:
    f.write("\n".join(blocks) + "\n")

print(f"[OK] GeckoList.unity generated: {out_path}")
print("[NOTE] In Unity Editor:")
print("    1. Assign GeckoSlot prefab to GeckoListUIController._geckoSlotPrefab")
print("    2. Drag GeckoSpeciesSO assets into GeckoListUIController._speciesForSale array")
