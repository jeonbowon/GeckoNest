# 게코 그림 주문서 (AI 그림 생성용)

> 목적: 지금 **코드로 그린 임시 게코**를 배경·하이드 하우스와 같은 화풍의 **그려진 그림**으로 바꾼다.
> 규격은 `ART_GUIDE.md`, 넣는 방법은 아래 5장. 다 만들면 메뉴 `Hako > Gecko > ②③` 한 번으로 교체된다.

---

## 0. 전체 흐름

| 단계 | 하는 일 | 결과 |
|:--:|------|------|
| 1 | **전체 그림 1장** 생성 (어른 크레스티드, 오른쪽 보기, 투명 배경) | `gecko_base.png` |
| 2 | 그 그림을 **파츠 14장**으로 자른다 (같은 캔버스 크기로 내보내기) | `body.png`, `head.png` … |
| 3 | **표정**(눈 9 · 입 8)을 같은 자리에 그려 내보낸다 | `eye_open.png` … |
| 4 | Unity 폴더에 넣고 메뉴 ②③ 실행 | 게코가 새 그림으로 바뀜 |
| 5 | 확인 (아래 체크리스트) | |

**핵심:** 파츠를 따로따로 생성하면 화풍·색이 어긋난다. **한 장을 만들고 잘라 쓰는 것**이 가장 안전하다.

---

## 1. 화풍

**참고 그림 (AI에 함께 넣으면 가장 정확하다)**
- `Assets/_Game/Textures/Backgrounds/bg_jungle.png` — 배경
- `Assets/_Game/Textures/decor_hide.png` — 하이드 하우스

**화풍 키워드 (영어 프롬프트에 그대로)**
```
stylized semi-realistic painted illustration, clean dark brown ink outline,
soft cel shading with two tones plus gentle gradient, subtle texture,
warm natural lighting from upper left, storybook terrarium mood, muted rich colors
```

**피할 것 (negative prompt)**
```
flat vector, thick black outline, cartoon sticker, photo, hyperrealistic scales,
text, watermark, background, ground, shadow, multiple creatures, cropped limbs
```

**색** (ART_GUIDE 1장) — 몸통 `#E3A86A`, 배 `#F7E3C2`, 외곽선 `#6B4330`(검정 금지), 눈 홍채 `#F3DA9A` + 흰 하이라이트.
예전 가이드의 "명암 1단계"는 배경과 어울리지 않아 **2단계 + 부드러운 그라데이션**으로 바꾼다 (ART_GUIDE 2장도 함께 고쳤다).

---

## 2. 캔버스 규칙 (꼭 지켜야 하는 것)

| 항목 | 값 |
|------|------|
| 캔버스 | **1600 × 900 px**, 배경 투명 (PNG) |
| 방향 | **오른쪽을 보는 옆모습** (왼쪽은 코드가 뒤집는다) |
| 발밑 중앙 | 캔버스의 **(800, 60)** — 네 발이 닿는 가상의 바닥선 y=60 |
| 전체 폭 | 꼬리 끝 ~ 주둥이 끝이 약 1,480 px (좌우 여백 60씩) |
| 꼬리 | **곧게 수평으로** (휘는 건 코드가 한다) |
| 다리 | 네 다리가 몸통과 **겹치지 않게 떨어뜨려** 그린다 (자르기 쉽게) |
| 그림자 | 넣지 않는다 (코드가 그린다) |

> 자를 때와 표정 그릴 때 **캔버스 크기를 절대 바꾸지 않는다.** 모든 PNG가 1600×900이면 Unity가 위치를 자동으로 잡는다.

---

## 3. 1단계 — 전체 그림 프롬프트

```
A cute crested gecko in full body side view, facing right, standing on all four legs,
stylized semi-realistic painted illustration, clean dark brown ink outline,
soft cel shading with two tones plus gentle gradient, subtle bumpy skin texture,
warm apricot orange skin with cream belly, large friendly round eye with white highlight,
gentle closed smile, slightly chunky cute proportions, straight horizontal tail,
all four legs clearly separated from the body, no background, transparent background,
no shadow, centered, whole body inside the frame, 16:9 wide canvas
```
- 비율: 가로로 긴 16:9로 뽑고 1600×900으로 맞춘다.
- 마음에 드는 그림이 나올 때까지 여러 장 뽑아 고른다. **이 한 장이 게임 전체의 인상**이 된다.
- 배경이 같이 나오면 투명 배경 도구(Photopea의 "Magic Cut", remove.bg 등)로 지운다.

---

## 3-1. (자동) 그림 한 장을 자동으로 자르기 — `Hako > Gecko > ④`

손으로 자르기 전에 **자동 1차 처리**를 먼저 써 볼 수 있다 (`Assets/Editor/Gecko/GeckoArtCutter.cs`).

1. 만든 그림을 `Assets/_Game/Textures/Gecko/Source/gecko_base.png`로 저장
2. 메뉴 `Hako > Gecko > ④ 그림 한 장 자르기` (또는 배치: `-executeMethod GeckoArtCutter.CutBatch`)
3. 자동으로 하는 일
   - **흰 배경 제거** (가장자리부터 밝기 190 이상·색 기운 20 이하를 따라 지운다 — **발밑 회색 그림자까지** 없앤다.
     이 그림자가 남으면 발끝이 지저분하고 발밑 원점이 아래로 밀려 발이 파묻혀 보인다)
   - `RECTS` 표의 사각형으로 **머리·꼬리**를 잘라 1600×900 캔버스 같은 자리에 저장
   - **몸통·다리 4개는 `CUT_PARTS` 표로 그림의 외곽선을 따라 잘라 낸다** — 사각형으로 자르면 옆 다리 발가락이 섞이고,
     무엇보다 네모난 자른 자리가 **걸을 때(다리 ±18° 회전) 몸통 위로 드러난다.
     씨앗점에서 시작해 어두운 외곽선(`INK_LUM`)을 벽으로 살만 채우고, 선을 조금씩 넘어가며 발가락을 잇는다
   - 몸통은 `keep`(기본 22px)만큼 넓혀 **다리 뒤를 메운다** — 다리가 흔들릴 때 빈틈이 생기지 않는다
   - 다리 관절(`joint`)은 원본 좌표로 준다 — 실제 어깨·허벅지 자리라야 걸을 때 발이 미끄러지지 않는다
   - **머리는 눈·입이 그려진 채로 둔다.** 기본 표정(`eye_open`·`mouth_closed`)은 빈 그림이라 그려진 얼굴이 그대로 보이고,
     다른 표정은 **살빛 판으로 덮고 그 위에 그린다** (감은 눈·웃는 눈·졸린 눈·반짝이는 눈, 입 4종)
   - 혀 두 마디·발밑 그림자를 그려 넣고, 스킨 에셋을 만들어 **MainHome 게코에 적용·저장**
4. 자른 자리가 어긋나면 `GeckoArtCutter`의 `RECTS`·`CUT_PARTS`(원본 픽셀, 왼쪽 위 기준)와 `EYE_RECT`·`MOUTH_RECT`만 고쳐 다시 실행
   - `CUT_PARTS`의 `cuts`(지울 구역 다각형)는 **외곽선이 없어 살이 그대로 이어진 곳**(허벅지-옆구리, 겨드랑이-가슴)만 막으면 된다
   - 잘 되었는지 보는 법: 쉬는 자세는 원본 그림과 같아야 하고, 다리를 ±18° 돌려 보았을 때
     **곧은 자른 자리가 몸통 위에 나타나지 않아야** 한다 (몸통의 배 외곽선만 보이는 게 정상)

> 자동 처리는 **1차용**이다. 파츠 경계가 딱 맞지 않거나 표정을 더 살리고 싶으면 아래 손 작업으로 다듬는다.

## 4. 2단계 — 파츠 14장으로 자르기

무료 웹 편집기 **Photopea**(photopea.com, 포토샵과 조작이 같다)로 할 수 있다.

1. `gecko_base.png`를 연다 → 캔버스 1600×900 확인
2. 파츠마다 **레이어를 하나씩 만들고**, 올가미로 그 부분을 선택해 잘라 옮긴다
3. 가려진 부분(몸통에 가린 먼 쪽 다리, 머리에 가린 목 등)은 **조금 이어 그려** 채운다 — 움직일 때 틈이 보이지 않게
4. 파츠 경계는 **20~30px 겹치게** 남긴다
5. 레이어를 하나씩 켜고 **File > Export as > PNG** (캔버스 크기 그대로)

**파일 이름 (정확히 지켜야 한다)**

| 파일 | 내용 |
|------|------|
| `body.png` | 몸통 (배 포함, 목까지) |
| `head.png` | 머리 (눈·입 제외한 얼굴·턱·뿔) |
| `tail.png` | 꼬리 — 곧게 편 상태, 뿌리 쪽이 오른쪽 |
| `leg_front_near.png` / `leg_front_far.png` | 앞다리 (가까운 쪽 / 먼 쪽, 먼 쪽은 84% 밝기) |
| `leg_back_near.png` / `leg_back_far.png` | 뒷다리 |
| `shadow_contact.png` | 비워 둬도 된다 (코드가 그림자를 그린다) |
| `shed_patch.png` | 허물 조각 — 몸 위에 얹히는 반투명 껍질 (없으면 임시 그림 유지) |
| `tongue_01.png` / `tongue_02.png` | 혀 두 마디 — 입 안에서 오른쪽으로 뻗는 방향, 곧게 |

---

## 5. 3단계 — 표정 (눈 9 · 입 8)

눈과 입은 **머리 위 제자리에 그려** 같은 캔버스로 내보낸다. 눈 한 장을 만든 뒤 조금씩 고쳐 쓰면 빠르다.

| 눈 파일 | 모습 | 입 파일 | 모습 |
|------|------|------|------|
| `eye_open.png` | 기본 (동그란 눈 + 흰 점 2개) | `mouth_closed.png` | 기본 (다문 선) |
| `eye_look_left.png` / `eye_look_right.png` / `eye_look_up.png` | 동공만 그쪽으로 | `mouth_smile.png` | 웃는 입 |
| `eye_closed.png` | 감은 선 (위로 볼록) | `mouth_open_small.png` | 살짝 벌린 입 |
| `eye_sleepy.png` | 반쯤 감김 | `mouth_open_wide.png` | 크게 벌린 입 (혀가 나오는 곳) |
| `eye_happy.png` | 웃는 눈 (아래로 볼록) | `mouth_chew.png` | 오물오물 |
| `eye_surprised.png` | 크게 뜬 눈 | `mouth_drink.png` | 물 마시는 입 |
| `eye_sparkle.png` | 반짝이는 눈 | `mouth_surprised.png` / `mouth_frown.png` | 놀람 / 시무룩 |

- 왼쪽·오른쪽 눈이 다르면 `eye_r_open.png`처럼 `eye_r_`로 시작하는 파일을 따로 준다. 없으면 같은 그림을 양쪽에 쓴다.
- 눈은 **머리 그림에는 그리지 않는다** (머리에는 눈이 들어갈 자리를 비워 둔다).

---

## 6. 4단계 — Unity에 넣기

1. PNG를 모두 `Assets/_Game/Textures/Gecko/Final/` 폴더에 넣는다 (폴더가 없으면 만든다)
2. Unity에서 그 PNG들을 선택 → Inspector에서 **Texture Type = Sprite (2D and UI)**, **Generate Mip Maps 켜기** → Apply
3. Project 창에서 **`Final` 폴더를 선택** → 메뉴 `Hako > Gecko > ② 선택한 PSD·폴더로 스킨 만들기`
4. 만들어진 스킨 에셋을 선택 → `Hako > Gecko > ③ 선택한 스킨을 씬 게코에 적용`
5. MainHome을 플레이해 확인 → Ctrl+S로 씬 저장

> 모든 PNG가 같은 캔버스(1600×900)면 관절 위치를 자동으로 잡는다. 크기가 제각각이면 임시 위치로 들어가므로 손으로 맞춰야 한다.

---

## 7. 종별 그림 (나중에)

같은 방법으로 종마다 한 벌씩 만들면 `GeckoSpeciesSO.skin`에 넣어 종마다 다르게 보이게 할 수 있다. 프롬프트에서 아래만 바꾼다.

| 종 | 바꿀 문구 |
|------|------|
| 레오파드 게코 | `a cute leopard gecko, yellow skin with dark brown spots, thick banded tail, eyelids and slit pupils, smooth bumpy skin` |
| 가고일 게코 | `a cute gargoyle gecko, grey-brown skin with cream blotches, small horn-like bumps above the eyes, stubby tail` |

---

## 8. 장식 그림 5종 (지금 임시 그림)

`Assets/_Game/Textures/Decor/`의 같은 이름 PNG를 **같은 크기로** 바꾸면 바로 반영된다. 배경 투명.

| 파일 | 크기 | 프롬프트 요점 |
|------|:--:|------|
| `decor_cork.png` | 380×1000 | `a tall cork bark panel for a terrarium wall, rough bark texture, moss patches` |
| `decor_vine.png` | 220×1000 | `a hanging pothos vine with heart shaped leaves, top to bottom` |
| `decor_branch.png` | 480×600 | `a bare climbing branch rising diagonally then turning horizontal` — **가지 가운데 선이 (60,40) → (300,440) → (440,440)** 을 지나야 게코 발이 맞는다 |
| `decor_driftwood.png` | 480×600 | 위와 같은 선, 하얗게 바랜 유목 |
| `decor_moss_rock.png` | 300×300 | `a mossy rock, moss on top, flat bottom` (아래 12%는 투명 여백) |
| `decor_cave.png` | 460×460 | `a rock cave with a dark round entrance in the middle, moss on top` (아래 10% 투명 여백, 입구는 게코가 들어가 가려질 만큼 크게) |

> 모두 **정면에서 살짝 위에서 본 각도**, 배경 그림과 같은 화풍으로.

---

## 9. 자주 나오는 문제

| 증상 | 원인·해결 |
|------|------|
| 파츠가 어긋나 붙는다 | PNG 캔버스 크기가 서로 다르다 → 모두 1600×900으로 다시 내보낸다 |
| 움직일 때 관절에 틈이 보인다 | 파츠 경계를 20~30px 겹치게 그린다 |
| 걸을 때 다리가 몸에서 떨어져 보인다 | 다리 위쪽(어깨·허벅지)을 몸통 아래로 더 길게 그린다 |
| 화면에서 흐릿하다 | Generate Mip Maps 켜기, 원본을 너무 작게 만들지 않기 |
| 외곽선이 너무 굵고 유치하다 | 선 굵기를 전체 폭의 0.2~0.3% (1480px 기준 3~4px)로, 색은 검정이 아니라 짙은 갈색 |
| 배경 투명이 지저분하다 | 지우고 남은 반투명 테두리를 지운다 (Photopea: Layer > Matting > Defringe) |

---

## 10. 체크리스트

- [ ] 모든 PNG 1600×900, 배경 투명
- [ ] 오른쪽을 보는 옆모습, 발밑 중앙 (800, 60)
- [ ] 파일 이름이 위 표와 글자 단위로 같음
- [ ] 머리에 눈·입이 그려져 있지 않음
- [ ] 꼬리는 곧게, 다리 4개가 분리
- [ ] 눈 9종 · 입 8종
- [ ] Unity에서 Sprite + Mip Maps
- [ ] 메뉴 ②③ 실행 후 플레이 확인 (걷기·혀 내밀기·벽 타기에서 관절이 어긋나지 않는지)
