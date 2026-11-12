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

