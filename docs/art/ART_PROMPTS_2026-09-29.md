# 사선 시점 아트 생성 기록 — 2026-09-29

사용자가 승인한 40도 홈 시안을 기준으로 built-in image_gen을 사용했다. 생성 PNG는 원본 그대로 `Assets/_Game/Textures/Oblique/`에 복사했으며 기존 PNG와 GUID는 보존했다. 기준 시안은 2026-09-28 대화에 표시된 `exec-6c7c4aac-8a00-4f51-bb97-e34459c69ee0.png`이다. 배경·게코·장식은 각각 분리된 런타임 에셋이며 시안 전체를 배경으로 붙이지 않는다.

## bg

```text
(프롬프트는 대화의 image_gen 호출에 기록됨)
```

## gecko

```text
(프롬프트는 대화의 image_gen 호출에 기록됨)
```

## cave

```text
Use case: background-extraction. Production transparent game sprite. Reference is the approved HAKO concept; extract ONLY its mossy rock cave at the right. Redraw as one COMPLETE isolated low stone hide, 40 degree elevated three-quarter view, broad roof visibly seen from above, textured natural warm gray rock and restrained moss. A large dark rounded arch entrance faces toward viewer-left/front; dark solid interior (not transparent hole), no checkerboard. Same soft upper-left dappled lighting and premium painted natural detail as reference. Center the full silhouette in a square canvas with 6 percent transparent margin on every side, base near y88 percent from top. No ground, no surrounding plants, no animal, no other object, no UI or text. Actual transparent background; do not add a colored glow, shadow backdrop or floor.
```

## rock

```text
Use case: background-extraction. One production transparent sprite for HAKO. Isolate the moss-covered LOW ROCK from lower left of approved reference concept. Full complete single rounded craggy rock, broad green moss top seen from 40 degrees above, warm brown-gray exposed sides, detailed soft naturally painted texture matching reference exactly. Square canvas, object centered, fills 85 percent width and 65 percent height, base at88 percent height. Actual transparent background with clean edges. No ground, no leaf litter outside rock, no other objects, no plants rising out, no gecko, no UI, no text, no cast shadow or glow. Single low mossy rock only.
```

## branch

```text
Use case: stylized-concept. Production game sprite of ONE climbable terrarium branch on actual transparent background. Match supplied HAKO reference's exact realistic painted rough warm cork wood, softly lit from upper left, seen from 40 degrees above so upper bark surface is visible. Branch is a thick continuous gently crooked wooden ramp starting at lower LEFT and rising diagonally toward upper RIGHT, ending with a near-horizontal perch. Portrait 4:5 canvas. PRECISION composition: in coordinates measured from bottom-left, centerline passes through (12 percent width, 7 percent height), then (62 percent width,73 percent height), then (91 percent width,73 percent height). Wood diameter about 8 percent canvas width. The horizontal perch is usable, no sharp vertical prongs or many crossing twigs. Two very small leaflets near base only. Entire branch inside canvas, clean isolated silhouette. No animal, ground, scenery, planter, UI, labels, text, shadow backdrop or glow. Transparent background.
```

## cork

```text
Use case: stylized-concept. Production isolated game sprite. One tall narrow naturally irregular cork bark climbing slab for the back wall of a terrarium, entire object visible on true transparent background. Match reference's warm textured cork bark, moss flecks, refined natural painted lighting from upper left. Camera 40 degrees above, show a little top thickness, irregular natural silhouette, not a manufactured rectangular bulletin board. Slab vertical, width about 35 percent of height, centered in tall portrait canvas, 5 percent transparent border all around, thin restrained moss seams. No floor, no scene, no gecko, no UI, no letters, no glow or background shadow. Object only.
```

## eyes

```text
Production game expression texture atlas. Reference image is the exact gecko identity and colors. Create a square 3 by 3 GRID of NINE near-eye skin patches, equal square cells, on actual transparent background, NO lines or labels. Each patch is ONLY the gecko's large near amber eye with narrow cream/olive surrounding skin rim feathering smoothly to alpha, matching exact natural painted scale texture and golden iris of reference, centered at same scale and position in every cell. No head outline, no crest spikes, no cheek or mouth. Eye fills about 64 percent of cell width and 68 percent height; skin patch about88 percent. Read ROW MAJOR: row1 neutral open / pupil looks left / pupil looks right; row2 pupil looks up / softly closed / sleepy half-open; row3 relaxed happy narrowed / alert wider open / attentive bright natural open. Do NOT add sparkles, stars, hearts, human eyebrows or cartoon black outlines. Quiet biologically inspired expression, no huge exaggerated expressions. Transparent gaps between patches. EXACTLY 9 cells on square canvas, same size and aligned. This is a texture sheet, no UI.
```

## mouths

```text
Production transparent game expression atlas matching supplied HAKO gecko. EXACT 4 columns by 2 rows of equal square cells, wide 2:1 image. Each cell contains ONLY a horizontal reptile mouth line/slit surrounded by softly feathered cream-apricot scaly skin patch; no head outline, no nostril, no eyes, no teeth. Mouth center at same cell position, width about75 percent cell, patch fills92 percent width and50 percent height. Match reference's lower muzzle colors and delicate natural painted scales. ROW MAJOR expressions: row1 gently CLOSED / subtly relaxed almost closed / SMALL OPEN dark slit / WIDE OPEN dark rounded gape with tiny pink tongue inside; row2 CHEW slightly parted / DRINK small puckered opening / SURPRISED medium opening / FROWN slightly downward ends. Keep changes restrained not human lips or cartoon smiles. No labels, no grid lines, no UI, no background, real transparent gaps. Skin rim must fully cover original mouth line and fade softly at edges. 8 aligned patches only.
```

## 연결과 좌표

- `HakoObliqueArt.Build`가 Sprite 임포트, 기존 정글·동굴·이끼바위·코르크·가지 DecorItemSO 연결, 신규 GeckoSkin_Oblique 및 크레스티드 종 연결을 한다. PNG는 수정하지 않는다.
- 게코 캔버스 1536×1024, 발밑 기준 (900,824), 새 관절·꼬리 사슬·터치 영역·표정 UV는 빌더에 함께 기록한다. 네 발은 독립 관절. 표정은 새 아틀라스를 UV로 합성하며 입은 13도 회전한다.
- 가지 발 경로는 480×600 UI 사각형에서 (60,70) → (220,295) → (310,510) → (375,510) (그림 끝보다 안쪽에 쉬어 앞발 공간 확보). 좌우 반전은 그림과 경로에 함께 적용한다.
- 사선 동굴은 사각 portal 대신 기존 실루엣 가림 방식을 사용한다. 원래 정면 문 영역을 그대로 재사용하지 않는다.


## 2026-11-12 방향별 바닥 이동 그림

사용자가 추가 제작·연결을 승인했다. 내장 image_gen, transparent_background=true, 참조 이미지 hako_oblique_v1.png. 원본을 덮어쓰거나 생성기를 실행하지 않고 출력 PNG를 그대로 복사했다. 1536x1024, Unity 임포트 설정과 각 그림의 관절·터치 좌표를 수동 작성했다. 실제 렌더/보행/성능은 미검증이다. 이동용 그림에는 새 표정 아틀라스가 없으며 상호작용은 기존 스킨으로 돌아간다.

### side

- PNG: Assets/_Game/Textures/Oblique/hako_ground_side_v1.png
- 스킨: Assets/_Game/GeckoSkins/GeckoSkin_Ground_side.asset
- 생성 원본: exec-0787cd27-acfe-4bff-b7dc-80f0fc71fb0b.png

프롬프트:

Use case: identity-preserve. Production Unity 2D creature sprite, ONE complete crested gecko, same individual, olive and cream harlequin skin and golden eyes as reference. Change ONLY the viewing direction: pure RIGHTWARD side heading on a horizontal ground plane, viewed from fixed camera elevated 40 degrees. Nose points directly to the right of image, tail extends left, body longitudinal axis horizontal. Show visible dorsal surface but NOT turned toward camera; head looks directly along body direction. Four naturally splayed legs, feet distinctly separated from each other for independent skin deformation. Calm neutral walking-ready stance. Full tail and toes inside canvas. Transparent background, no ground, NO shadow, NO glow, no labels or UI. Keep natural painted detailed style, soft upper-left light. Landscape 3:2 canvas, 8 percent padding, body center around 55 percent width, feet baseline about 80 percent image height. This is a new directional sprite to accompany reference, not a rotating copy.

### near

- PNG: Assets/_Game/Textures/Oblique/hako_ground_near_v1.png
- 스킨: Assets/_Game/GeckoSkins/GeckoSkin_Ground_near.asset
- 생성 원본: exec-2bab9fdb-9c76-4945-8bef-74f0128ebdb7.png

프롬프트:

Use case: identity-preserve. ONE production Unity 2D directional sprite of the SAME crested gecko in reference: olive and cream harlequin skin, golden eyes, natural painted scale texture, calm creature. Fixed camera elevated 40 degrees above ground, NOT a top-down camera. Heading diagonally toward the viewer and RIGHT, a true 45-degree ground-plane yaw relative to rightward side view. Nose projects LOWER RIGHT and tail UPPER LEFT. Face turned toward camera, both eyes naturally visible as appropriate. Change the animal's actual ground-plane orientation, not just rotate the reference bitmap. Four anatomically correct splayed legs with separated visible feet suitable for independent skeletal deformation; neutral walking-ready stance. Complete tail and every toe inside image with 8 percent margin. Center full animal on landscape 3:2 canvas. Same character proportions and soft upper-left lighting. Background MUST be genuinely transparent alpha with no glow, colored haze, floor, cast shadow or backdrop. No text, no multiple poses, no labels, no UI.

### far

- PNG: Assets/_Game/Textures/Oblique/hako_ground_far_v1.png
- 스킨: Assets/_Game/GeckoSkins/GeckoSkin_Ground_far.asset
- 생성 원본: exec-3ffaaba1-8e52-42af-a746-b18630d1a6a4.png

프롬프트:

Use case: identity-preserve. ONE production Unity 2D directional sprite of the SAME crested gecko in reference: olive and cream harlequin skin, golden eyes, natural painted scale texture, calm creature. Fixed camera elevated 40 degrees above ground, NOT a top-down camera. Heading diagonally AWAY from viewer and RIGHT, true 45-degree ground-plane yaw. Nose projects UPPER RIGHT and tail LOWER LEFT. Show back of head and dorsal crests; do NOT turn face back toward viewer. Change the animal's actual ground-plane orientation, not just rotate the reference bitmap. Four anatomically correct splayed legs with separated visible feet suitable for independent skeletal deformation; neutral walking-ready stance. Complete tail and every toe inside image with 8 percent margin. Center full animal on landscape 3:2 canvas. Same character proportions and soft upper-left lighting. Background MUST be genuinely transparent alpha with no glow, colored haze, floor, cast shadow or backdrop. No text, no multiple poses, no labels, no UI.

### front

- PNG: Assets/_Game/Textures/Oblique/hako_ground_front_v1.png
- 스킨: Assets/_Game/GeckoSkins/GeckoSkin_Ground_front.asset
- 생성 원본: exec-33aee25e-2d38-442f-98ba-5c166f7bf325.png

프롬프트:

Use case: identity-preserve. ONE production Unity 2D directional sprite of the SAME crested gecko in reference: olive and cream harlequin skin, golden eyes, natural painted scale texture, calm creature. Fixed camera elevated 40 degrees above ground, NOT a top-down camera. Heading DIRECTLY TOWARD viewer. Nose at bottom-center, tail extending toward TOP center. Symmetric frontal three-quarter elevated view. Both eyes visible. Entire body foreshortened naturally in depth. Change the animal's actual ground-plane orientation, not just rotate the reference bitmap. Four anatomically correct splayed legs with separated visible feet suitable for independent skeletal deformation; neutral walking-ready stance. Complete tail and every toe inside image with 8 percent margin. Center full animal on landscape 3:2 canvas. Same character proportions and soft upper-left lighting. Background MUST be genuinely transparent alpha with no glow, colored haze, floor, cast shadow or backdrop. No text, no multiple poses, no labels, no UI.

### back

- PNG: Assets/_Game/Textures/Oblique/hako_ground_back_v1.png
- 스킨: Assets/_Game/GeckoSkins/GeckoSkin_Ground_back.asset
- 생성 원본: exec-8c4fdf8e-50b4-4fa0-8f32-b37f8d0df0f3.png

프롬프트:

Use case: identity-preserve. ONE production Unity 2D directional sprite of the SAME crested gecko in reference: olive and cream harlequin skin, golden eyes, natural painted scale texture, calm creature. Fixed camera elevated 40 degrees above ground, NOT a top-down camera. Heading DIRECTLY AWAY from viewer. Nose at TOP center, tail extending toward BOTTOM center. Symmetric rear elevated view, back of head visible; no face looking back. Entire body foreshortened naturally in depth. Change the animal's actual ground-plane orientation, not just rotate the reference bitmap. Four anatomically correct splayed legs with separated visible feet suitable for independent skeletal deformation; neutral walking-ready stance. Complete tail and every toe inside image with 8 percent margin. Center full animal on landscape 3:2 canvas. Same character proportions and soft upper-left lighting. Background MUST be genuinely transparent alpha with no glow, colored haze, floor, cast shadow or backdrop. No text, no multiple poses, no labels, no UI.


## 2026-11-13 — Directional anatomy repair and intermediate views

Built-in image_gen, transparent_background=true. Generated originals were copied unchanged into Assets/_Game/Textures/Oblique/. No existing PNG or GUID was overwritten. All outputs are 1536x1024 with alpha; checked zero alpha at the removed extra foot. Linking: HakoGroundMotionRepair.LinkBatch. Runtime measured angles are ±65 degrees for the new intermediate views (the generation brief requested approximately 67.5).

### hako_ground_near_v2.png
Reference/edit target: hako_ground_near_v1.png.

Prompt:
> Edit target is the attached game gecko sprite. Keep EXACT same 1536x1024 canvas framing, animal pose, olive cream markings, lighting, face, tail, body and positions of all remaining limbs. Anatomical correction ONLY: this image incorrectly contains FIVE legs. Remove the EXTRA leg and foot projecting upward/right behind the head, centered around image pixel (1200,430), including its toes around (1250,400). This is the upper-right extra appendage; preserve the far HIND leg and foot at (850,310), near HIND leg at (470,660), near FRONT leg at (900,850), and far FRONT leg at (1260,800). Exactly FOUR legs total. Fill removed appendage area with clean transparent alpha, preserve body contour under it. No background, no shadow, no glow or matte. Retain all other pixels/composition as closely as possible. Tail remains naturally tapering with rounded narrow tip.

### hako_ground_near_steep_v1.png / hako_ground_far_steep_v1.png
References: hako_ground_near_v1.png / hako_ground_far_v1.png, respectively. Each was a separate built-in call.

Prompt template (replace {pose} with its exact variant below):
> Create a new matching direction sprite of the reference crested gecko for this game's turn animation: {pose}. Keep same animal identity, olive and warm cream mottled pattern, golden eyes, natural painted detailed scale style, camera looking 40 degrees down. Full body centered on transparent canvas 1536x1024 with generous clear margins. Anatomically exactly FOUR legs: one foreleg each side of chest, one hindleg each side of hips, exactly four feet. No extra limbs, no fifth foot! Thin naturally tapering tail with rounded narrow tip, no fins. All four feet lie on same ground plane, quiet neutral walking stance. No background or glow, no text, no cast shadow. Reference is identity/style only; redraw correct perspective for specified heading. Maintain coherent compact body proportions.

Near pose:
> heading down-right 67.5 degrees from horizontal, almost frontal but head shifted toward lower right, tail extends toward upper left. Show both eyes, front face and foreshortened torso

Far pose:
> heading up-right 67.5 degrees from horizontal, almost directly away from viewer but head shifted toward upper right, tail extends down-left. Mostly back of head and back are visible

New limb, tail-chain, head and hit-proxy coordinates are recorded in Assets/Editor/Gecko/HakoGroundMotionRepair.cs and serialized GeckoSkin_Ground_near_steep / far_steep assets. Existing near skin preserves its GUID; its far foreleg is moved to the remaining lower-right foot. Runtime tail deformation now preserves whole cross-sections and the painted taper, without extra uncurling. GPU contact sheet: Logs/ground-motion-20261113.png; columns side, near, near_steep, front, far, far_steep, back; rows three gait phases, middle row mirrored.
## 2026-11-13 — Curled sleep artwork

Built-in image_gen, transparent_background=true. Saved unchanged into Assets/_Game/Textures/Oblique/hako_sleep_settling_v1.png and hako_sleep_curled_v1.png. Existing PNGs preserved. New GeckoSkin_Sleep_settling / GeckoSkin_Sleep_curled assets and texture meta were authored directly and referenced by GeckoSkin_Oblique.sleepSettling / sleepCurled. No Unity import, compile, test, capture or build was run at the user's instruction. Body origin (900,820), 1536x1024 source coordinate system; contact proxies are serialized in each new skin. In-game alignment remains unverified.

### Settling prompt
Reference: hako_ground_near_v2.png.
> Create one game sprite of this exact olive and cream crested gecko settling to sleep. Same elevated 40-degree camera and realistic painted style, same golden OPEN eyes (crested geckos have NO eyelids). Canvas 1536x1024 transparent, no background, halo, ground or shadow. Intermediate crouching pose: body lowered in gentle C shape, exactly four legs tucked closer under body, head lowered facing lower right, tail starts curling around LEFT side of body with slender tapered tip pointing forward. Keep animal scale same as reference: head about 340px wide at image (1100,640), belly centered (850,620), forefeet around y820, tail begins near (590,520) and curves around left. This is halfway between standing and tightly curled resting, not a standing pose, not a snake. Whole gecko within image with generous transparent margins. Only four feet, no extra limbs, tail thin and naturally rounded, preserve markings and animal identity.

### Curled prompt
Reference/edit target: the generated settling image.
> Edit this gecko intermediate crouch into the final deeply resting curled pose for a matched animation. SAME 1536x1024 transparent canvas, identical camera, lighting, markings and body scale; keep head almost same pixel position (1120,650), floor contact y800. Curl torso a little more into a compact gentle C, tuck limbs under body so two near feet remain partially visible and far limbs hidden anatomically (exactly four total legs, no extra limbs). Rest chin against the folded foreleg, lower snout subtly, tail hugs the left and lower body more closely and its narrow tip rests beside the near forefoot. Golden eye remains OPEN because this is a crested gecko with no eyelids, calm unfocused gaze, no eyelashes, no human expression. Preserve source identity and head size. Relaxed breathing-ready silhouette. No background, no glow, no cast shadow, no text or symbols. Keep body and head aligned to source so animation doesn't jump.
## 2026-11-16 — Anatomical ground turn atlases
Built-in image_gen with transparent_background=true. Final PNGs copied unchanged to Assets/_Game/Textures/Oblique/hako_turn_front_atlas_v1.png and hako_turn_rear_atlas_v1.png. Source sprites and GUIDs preserved. Front reference: ground_side_v1 and ground_front_v1. Rear references: generated front sheet and ground_back_v1. Runtime calibration: GeckoTurnAtlas_Crested.asset (top-left cell-local landmarks). No Unity compilation, tests, play or build performed.

Generation brief: One 4x4 equal-cell sprite sheet of the same olive/cream harlequin crested gecko, fixed elevated 40-degree camera, constant scale, chest/hip midpoint centered in each cell. Sixteen chronological poses covering 0 to 90 degrees in six-degree nominal increments. Front variant turns from screen right toward viewer; rear variant turns from screen right away from viewer. Head leads, shoulders follow, alternate feet step and support, hips follow, slender tail lags. Exactly four legs, complete tail/toes inside each cell. Actual anatomical perspective changes, never rotated copies. No grid, labels, floor, shadows or glow; transparent alpha requested.

Background-edit brief: Preserve all sixteen geckos, exact positions/proportions/markings and grid; remove white/gray or colored haze outside animals, preserve cream markings and toes, real transparent alpha. Returned PNGs retain low-alpha noise, so runtime uses an atlas-only alpha floor of 0.06. Generation is not evidence of coherent animation: frame identity, outlines, mirrored axial transitions and runtime visual quality remain unverified.

## 2026-11-18 레오파드 홈 스킨

내장 ImageGen의 투명 배경으로 제작했다. PNG는 `Assets/_Game/Textures/Leopard`에 원본 그대로 복사했다. `Assets/_Game/GeckoSkins/Leopard`의 스킨에 발·머리·눈 영역, 몸통 기준점과 꼬리 관절을 기록했다. 실제 홈에서의 접지·회전·표정은 미검증이다.

### `leopard_side_v1.png`

레오파드 외형은 `docs/art/leopard-desert-review-v1.png`, 그림 시점과 재질은 `hako_oblique_v1.png`를 참고했다.

> Create ONE transparent PNG game sprite of the leopard gecko character in reference 1, for the HAKO 2D terrarium game. Reference 1 is the identity and coloring in the lower-middle of the desert illustration. Reference 2, the crested gecko sprite, is a camera/composition and painterly material reference only. Draw a believable NORMAL leopard gecko from an approximately 40-degree elevated side view facing RIGHT (snout at right, tip of tail at left, body axis close to horizontal). Four clearly separate legs and distinctly visible feet with fine clawed toes, joints arranged as in a standing walking-ready reptile (two feet project beyond the upper side, two toward the lower side). Warm muted honey yellow, an ivory throat and belly, medium irregular very dark cocoa body spots sized to read on a phone, an unmistakably heavy fleshy tail that tapers and has 4 broad cocoa and cream bands. Broad low body, kind copper vertical-pupil eye with a fleshy eyelid, smooth brow, narrow pointed toes. The entire animal fits a tight but generous landscape frame, no clipping, equal room above and below. A clear delicate silhouette. Soft upper-left light, detailed warm painterly realism like reference 2 but no sticking pads, protruding crests, other species traits or glow. TRANSPARENT alpha backdrop, no ground, no baked shadow, no outlines around the silhouette, no accessories, text or frames. Asset for four-foot rigging.

### 방향과 수면

각 방향과 수면 이미지는 `leopard_side_v1.png`를 개체 기준으로 삼고, 해당 크레스티드 그림은 카메라 각도를 맞추는 데 사용했다. 다음 공통 프롬프트에 표의 자세를 넣었다.

> Create exactly ONE transparent alpha sprite for the HAKO 2D Unity gecko habitat. Reference 1 is the exact leopard gecko identity and painted natural style: retain the honey ochre spot pattern density and size, thick cream-and-cocoa banded tapered tail, ivory jaw, broad low body, copper eyes, no crests or pads, narrow clawed toes. Reference 2 is ONLY the desired CAMERA ANGLE and direction; ignore its crested gecko anatomy, color, and markings. Camera is elevated looking down by about 40 degrees; use the same amount of overhead tilt and lens as the base leopard. <방향 및 자세> Anatomically exactly four legs, no extra feet, coherent torso and tail connection. Keep warm natural upper-left lighting, consistent silhouette, head/body/tail proportions and adult body mass from ref 1. Body occupies most of a landscape or square frame, with the nose, tail tip and all feet completely inside frame. Painterly natural material, clean transparency everywhere except animal, shadow-free, no text, grid or background, no cutout glow.

| PNG | 각도 참고 | 방향 및 자세 |
|---|---|---|
| `leopard_near_v1.png` | `hako_ground_near_v2.png` | Front-quarter view, rotate the same individual 35 degrees toward the viewer. Nose points down-right. Both eyes may be partly visible. Tail recedes up-left, not in front of the belly. |
| `leopard_near_steep_v1.png` | `hako_ground_near_steep_v1.png` | Steep front-quarter view, rotate the same individual 65 degrees toward the viewer. Nose points toward the lower-right, almost toward the viewer. Head in near foreground and the tail starts behind the pelvis, clearly visible toward the upper center. |
| `leopard_front_v1.png` | `hako_ground_front_v1.png` | Straight frontal view, the same individual faces the lower center of the frame head-on, head closest to camera. Four feet visible, rear feet visibly separated behind the side of the body. Tail is seen behind the back receding toward the upper center; gently offset tip to the LEFT to show the bands. |
| `leopard_far_v1.png` | `hako_ground_far_v1.png` | Rear-quarter view, rotate the same individual 35 degrees away from the viewer with nose pointing upper-RIGHT. Broad back is visible, head recedes to upper-right, thick tail projects lower-left. |
| `leopard_back_v1.png` | `hako_ground_back_v1.png` | Straight rear view, the same individual faces the upper center of the frame directly away from viewer. Four feet visible, two on either side. Broad banded tail closest to viewer extends toward the bottom middle with a very slight gentle bend. No eyes visible from this angle. |
| `leopard_settling_v1.png` | `hako_sleep_settling_v1.png` | Drowsy lounging side-quarter pose facing right, belly and chin softly lowered nearly onto ground, legs relaxed beside body, eyelids gently shut, the character beginning to draw its heavy tail in toward near flanks in a relaxed half curve. Keep length and recognizable proportions. Follow the pose reference only for directional orientation. |
| `leopard_curled_v1.png` | `hako_sleep_curled_v1.png` | Resting in a loose crescent facing right, belly on ground, head comfortably lowered and clearly closed eyelids, short limbs loosely tucked, tail curls forward toward the front foot along the bottom edge without obscuring the head, with bands clearly visible. Natural relaxed sleeping leopard gecko, not tightly coiled. |

### `leopard_far_steep_v2.png`

`leopard_back_v1.png`와 `leopard_far_v1.png`를 참고해 후방 사선 65도 그림을 다시 제작했다.

> Paint one isolated transparent PNG video-game sprite of the SAME leopard gecko as both refs. Keep the natural rendering, four clawed feet, exact same body and head proportions and honey spotted skin, and the thick cream-and-brown banded tapered tail. CRITICAL POSE: the animal is turning right by ONLY twenty-five degrees from directly away (reference 1 is straight-away back view). It should be MUCH more upright in the picture than the shallower diagonal reference 2. Nose at normalized image coordinate (0.64, 0.09) in the upper-middle, tail tip near (0.39, 0.93) in the lower-middle. The line from base of tail to neck rises very steeply from roughly (0.46,0.60) to (0.60,0.27), so the gecko is mostly seen from behind. Tail nearest to viewer. Retain ref 1's approximate apparent leg spread and natural framing: any eyes only a tiny partial eye at far right, both pairs of feet visible separated, all feet inside frame. Camera is about 40 degrees down into a terrarium. Landscape 3:2 frame, same size as reference 1. Exactly one individual, true transparent alpha background, no baked shadow, no text. This is the intermediate rear three-quarter pose between right-facing and directly away for a turn animation, keep head/body/tail clearly in one near-vertical alignment.

### `leopard_eyes_v1.png`와 `leopard_mouths_v1.png`

`leopard_side_v1.png`에서 색과 무늬를 가져오고 기존 크레스티드 눈·입 아틀라스의 칸과 순서를 맞췄다.

> Gently edit reference 1, a 3x3 eye texture sheet for a game. Reference 2 is the new leopard gecko character and colors. Keep the nine centers and state order pixel aligned. Row 1 regular open, pupils left, pupils right. Row 2 eyes looking up, COMPLETELY CLOSED movable eyelid, half-lidded sleepy. Row 3 content partly closed, wide startled, bright alert. Each patch shows a small section of skin around the eyelids only. The exact middle cell must show the closed golden eyelid fully covering the eye, a subtle dark crease at its lower edge, and a soft scaly surround. Keep the other eight eyes copper with vertical pupils. Use true alpha outside the small skin patches. Match reference 2's muted honey-golden fine gecko scales with small irregular dark cocoa flecks and ivory underside; no olive-green and no crested brow spines. Match reference 1 dimensions, aspect ratio, cell centers, occupied patch size, surrounding blank margins, and exact grid registration; preserve it as a sprite atlas so it is aligned cell-to-cell. No titles, lines, numbering, drop shadow, halo, labels or marks outside the patches.

> Gently edit reference 1, a landscape 4 columns by 2 rows expression texture with eight small close-up lip shapes of a gecko. Reference 2 is the leopard gecko subject and color. Keep reference 1 expressions and their fixed positions: top closed, slight smile, slightly open, wide with soft tongue; bottom chew, drink, surprise, frown. Upper lip should be pale golden with a few brown flecks, lower lip ivory cream. Preserve the same clean slender seam shape, red-brown realistic inside of mouth. Use true alpha outside the small skin patches. Match reference 2's muted honey-golden fine gecko scales with small irregular dark cocoa flecks and ivory underside; no olive-green and no crested brow spines. Match reference 1 dimensions, aspect ratio, cell centers, occupied patch size, surrounding blank margins, and exact grid registration; preserve it as a sprite atlas so it is aligned cell-to-cell. No titles, lines, numbering, drop shadow, halo, labels or marks outside the patches.

## 2026-11-18 가고일 게코 전신 방향 원화

내장 ImageGen으로 투명 PNG 7장을 제작하고 `Assets/_Game/Textures/Gargoyle/`에 복사했다. `Assets/_Game/GeckoSkins/Gargoyle/`에 각 원화의 걷기·등반 기준을 기록했다. 머리 돌기, 가지를 잡는 발과 가늘어지는 꼬리는 [신시내티 동물원의 종 설명](https://cincinnatizoo.org/animals-archive/gargoyle-gecko/)과 [유티카 동물원의 종 설명](https://www.uticazoo.org/gargoyle-gecko/)을 참고했다.

> Create one transparent game sprite of a gargoyle gecko in a neutral ready-to-walk pose. Use an elevated orthographic terrarium camera, about 38 degrees down, and a warm semi-realistic painted finish. Give it blunt bony knobs behind the eyes, a broad muzzle, granular brown skin with broken cream marbling and muted rust flecks, broad adhesive toes, and one tapering tail. Keep four separate feet on the same imaginary surface. Use true alpha around the complete silhouette. Keep the animal isolated and shadow-free.

옆 원화를 개체 기준으로 썼다. 아래 방향으로 돌릴 때 돌기·등 무늬·발가락·몸통 비율을 유지하도록 요청했다. 정면에 가까운 사선은 가려진 뒷발이 드러나도록, 뒤쪽 사선은 옆과 후면 사선 중간 방향에서 네 발이 따로 보이도록 다시 제작했다.

| PNG | 카메라를 기준으로 한 방향 |
|---|---|
| `gargoyle_side_v1.png` | 화면 오른쪽을 보는 옆모습 |
| `gargoyle_near_v1.png` | 카메라 쪽 35도 |
| `gargoyle_near_steep_v1.png` | 카메라 쪽 65도 |
| `gargoyle_front_v1.png` | 카메라 쪽 정면 |
| `gargoyle_far_v1.png` | 카메라 반대쪽 35도 |
| `gargoyle_far_steep_v1.png` | 카메라 반대쪽 65도 |
| `gargoyle_back_v1.png` | 카메라 반대쪽 후면 |
