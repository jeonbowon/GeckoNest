# Hako expression atlases — 2026-09-27

Paths below are relative to the repository root. Current verification status: [PROGRESS.md](../../PROGRESS.md).

Tool: built-in image generation (imagegen skill); no API/CLI fallback.
Reference: `Assets/_Game/Textures/Gecko/ChildArt/hako_child_body.png` (original unchanged).
Outputs: `Assets/_Game/Textures/Gecko/ChildArt/hako_eyes_v1.png` and `hako_mouths_v1.png`.
Both outputs were visually inspected as atlases and copied into the project with alpha preserved. In-game compositing has NOT been inspected. These are candidate assets pending Unity visual QA, not approved final art.

## Eye prompt

Use case: stylized-concept. Asset: production Unity 2D game eye expression atlas. Reference image is the existing Hako gecko, use its near large golden brown eye and warm peach textured surrounding skin. Create ONE square 1536x1536 transparent PNG arranged in EXACTLY 3 columns x 3 rows of equal 512x512 cells, no gaps between cells, no text or labels or grid. Every cell contains ONLY the same single near eye with identical camera angle, eye position centered x256 y256, eye area about 280 wide 330 high. Surround it with a small feather-edged patch of the matching warm peach skin to occlude an existing drawn eye. Patch fits inside 470x470; outer 20 pixels transparent. No head silhouette, no noses, no mouths. Row-major expressions: row1 normal open golden eye; looking left; looking right. Row2 looking up; closed eye with warm skin covering eye and a curved dark brown line; half closed sleepy. Row3 joyful curved closed eye; surprised wide eye; sparkling happy open eye with 2 highlights. Preserve reference's watercolor children's illustration texture and eye identity. All eyes same size and alignment; no oversized changes, no black rectangular background, genuine alpha transparency.

## Mouth prompt

Use case: stylized-concept. Production Unity 2D mouth expression sprite atlas matching reference Hako gecko. ONE landscape image exactly 4 columns x 2 rows equal cells. Each cell has the same small gecko mouth viewed from side facing right, with pale warm cream watercolor skin patch (reference lower muzzle), softly feathered to genuine transparency at edges. No eyes, noses, cheeks, heads, body, text, grids, labels. Same center and scale all cells. Mouth spans 70% cell width and at most 45% cell height, centered in cell. First row left-to-right: closed gently curved lip line; happy smile; slightly open mouth; wide open happy mouth dark warm brown interior and small pink tongue. Second row: chewing small oval mouth; drinking small pursed mouth; surprised oval mouth; mild sad frown. Keep mouth shapes in center of each equal cell with 15% padding on all sides, no touching neighboring cells. Matching watercolor texture cream skin, delicate brown line, polished children's game illustration. Output only eight individual mouth patches aligned on transparent background.

## Integration

The shader uses normalized grid coordinates, so generated pixel dimensions need not match the requested resolution. Cell indices follow GeckoEye (3x3) and GeckoMouth (4x2), top-to-bottom. Normal/closed states sample the original gecko, preserving its identity at rest. Inspector regions are normalized body-image UV rectangles; atlas center cropping and feathering are in `GeckoWholeSurface.shader`.
