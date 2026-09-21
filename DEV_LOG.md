# GeckoNest Dev Log

---

## 2026-03-20 — STEP 2: 홈 UI 연동 및 먹이 버튼 동작 확인

### 완료
- `GeckoSpeciesSO.cs` 생성 — GeckoSpecies ScriptableObject 정의
- `cricket_small` ItemSO 에셋 생성 — 기본 먹이 아이템
- `GeckoAnimatorController.Awake()` 추가 — Animator 참조 초기화
- `AppBootstrap.EnsureDefaultGecko()` 추가 — 첫 실행 시 기본 게코 자동 생성
- `SaveManager.CreateNewPlayerData()`에 `cricket_small` 초기 지급 추가
- **버그 수정:** `Collection was modified` 예외 — foreach 도중 컬렉션 변경 차단
- **버그 수정:** `SaveManager IOException` — 파일 저장 경로/권한 문제 수정
- **버그 수정:** Canvas `LocalScale 0,0,0 → 1,1,1` — UI 비표시 문제 수정
- **버그 수정:** GeckoArea RaycastTarget이 하위 버튼 입력 차단하는 문제 수정
- Feed 버튼 → Hunger 수치 증가 동작 확인 완료

### 다음 할 것
- [x] UI 레이아웃 위치 정리 → 레이아웃 재편 섹션 참고
- [x] 게이지 Fill 값 반영 확인
- [x] Water / Pet / Clean 버튼 테스트

---

## 2026-03-23 — STEP 3: 성장 + 허물

### 도메인 로직

**`GeckoManager.cs`**
- `EvaluateGrowth()` 구현 — 성장 단계별 조건 switch 표현식
  ```
  Stage 0→1 : 15일
  Stage 1→2 : 30일 + 허물 1회
  Stage 2→3 : 60일 + 허물 3회 + Health ≥ 50
  Stage 3→4 : 120일 + 허물 5회 + Affection ≥ 60
  자연사    : 900일 (STEP 6에서 처리 예정)
  ```
- `ApplyOfflineProgress()` 개선
  - `moltProgress += 0.20/h` 누적 (~20일에 100% 도달)
  - 100 달성 시 `TryMolt()` → `EvaluateGrowth()` 자동 연쇄
- `TryMolt()` 성공률: 기본 70% + Thirst>50 시 +15% + Health>60 시 +10%
- 실패 시 `moltProgress` → 30 리셋
- `OnMoltFail` 이벤트 추가

**`TimeManager.cs`**
- `GetElapsedDays(long sinceTicks)` 추가

**`ItemSO.cs`**
- `healthRestore` 필드 추가

### 먹이 아이템 ScriptableObject 신규 6종

| 에셋 | hungerRestore | growthExpGain | 가격 |
|------|:---:|:---:|------|
| `gutloaded_cricket` | 25 | +2 | 15코인 |
| `mealworm` | 22 | +3 | 20코인 |
| `dubia_roach` | 28 | +4 | 30코인 |
| `superworm` | 32 | +5 | 40코인 |
| `calcium_dusting` | 5 | 0 | 25코인, healthRestore +10 |
| `growth_booster` | 0 | +8 | 5젬 |

경로: `Assets/_Game/Resources/Items/`

### HomeUI 신규 오브젝트 (`MainHome.unity` 직접 편집)

| 오브젝트 | 부모 | 용도 |
|----------|------|------|
| GeckoNameText | GeckoArea | 게코 이름 (TMP, 42pt Bold) |
| GrowthStageText | GeckoArea | 성장 단계명 (TMP, 32pt) |
| GrowthStageIcon | GeckoArea | 성장 단계 아이콘 Image (36×36) |
| MoltProgressFill | GeckoArea | 허물 진행 바 (Image Filled) |
| ResultPanel | Canvas | 성장/허물 결과 알림 (2.5초 자동 숨김) |
| ResultText | ResultPanel | 알림 TMP |

**`HomeUIController.cs` 변경**
- `STAGE_NAMES[]` 영어화: Hatchling / Baby / Juvenile / Sub-Adult / Adult
- `_growthStageIcon` + `_growthStageSprites[5]` 추가, `RefreshGrowthInfo()`에서 자동 교체
- 결과 메시지 영어: "grew up!", "Molt success!", "Molt failed..."
- `OnGrowthUp` / `OnMoltSuccess` / `OnMoltFail` 이벤트 구독

**`GeckoAnimatorController.cs` 변경**
- `OnMoltFail` 구독 → `Molt_Start` 트리거 (허물 실패 배지 유지 애니)

### 아이콘 에셋 (`Assets/_Game/Textures/Icons/`)

**StatusPanel 바 아이콘 — 32×32 PNG (Python stdlib 생성)**

| 파일 | 모양 |
|------|------|
| `hunger_icon.png` | 오렌지 드럼스틱 |
| `thirst_icon.png` | 파란 물방울 |
| `mood_icon.png` | 빨간 하트 |
| `health_icon.png` | 초록 십자가 |
| `clean_icon.png` | 노란 별 |

**성장 단계 아이콘 — 128×128 PNG**

| 파일 | 단계 |
|------|------|
| `stage0_egg.png` | Hatchling — 크림색 알 + 균열 |
| `stage1_baby.png` | Baby — 라임 그린 소형 게코 |
| `stage2_juvenile.png` | Juvenile — 중간 초록 + 등 점박이 |
| `stage3_subadult.png` | Sub-Adult — 포레스트 그린 + 이중 무늬 |
| `stage4_adult.png` | Adult — 짙은 초록 + 굵은 밴딩 + 발가락 |

> 초기 32×32로 생성 → 확대 시 깨짐 발생 → 128×128로 재생성

---

## 2026-03-23 — UI 레이아웃 재편

**레퍼런스 해상도:** 1080×2400 (CanvasScaler, Scale with Screen Size, Match 0.5)

### 목표 레이아웃
```
┌─────────────────────────┐
│         TopBar          │  h=150, top 고정
├──────┬──────────────────┤
│      │                  │
│Status│    GeckoArea     │  풀스크린 backdrop
│Panel │  (게코 이동 영역) │
│(좌측 │   아무것도 없어야 │
│반투명│      함)          │
│      │                  │
├──────┴──────────────────┤
│      ActionButtons      │  h=180, BottomTab 바로 위
├─────────────────────────┤
│Home│Store│List│Terr│Col │  BottomTab h=180, 화면 최하단
└─────────────────────────┘
```

### 변경 내역

| 오브젝트 | 변경 전 | 변경 후 |
|----------|---------|---------|
| GeckoArea | AnchorMin(0,0.5)/AnchorMax(1,0.5), y=400, h=1430 | AnchorMin(0,0)/AnchorMax(1,1) 풀스크린 |
| ActionButtons | AnchorMin(0,0.5), y=-830, h=460 | AnchorMin(0,0)/AnchorMax(1,0), y=180, h=180 |
| StatusPanel | full-width, y=-500, h=360 | 좌측 고정 x=130, w=260, h=800 |
| MoltProgressFill | GeckoArea 하단 y=100 (StatusPanel 겹침) | GeckoArea 상단 y=-222 |
| BottomTab 5버튼 | center(0.5) 앵커, y오프셋 수직 쌓임 | 각 1/5 폭 앵커 분할, 세로 stretch |

### StatusPanel 반투명
- `CanvasGroup` 컴포넌트 추가, alpha=0.82
- `m_BlocksRaycasts: 0` — 게코 터치 통과
- 내부 바 Y 좌표 재중앙: 0/150/300/450/600 → −300/−150/0/150/300

### BottomTab 가로 배치

| 탭 | AnchorMin X | AnchorMax X |
|----|:-----------:|:-----------:|
| HomeTab | 0.0 | 0.2 |
| StoreTab | 0.2 | 0.4 |
| GeckoListTab | 0.4 | 0.6 |
| TerrariumTab | 0.6 | 0.8 |
| CollectionTab | 0.8 | 1.0 |

---

## 2026-04-09 — 테라리움 깊이 시스템 + 게코 이동 AI

### 신규 스크립트

| 파일 | 경로 | 역할 |
|------|------|------|
| `DepthScaleConfig.cs` | `Scripts/Models/` | ScriptableObject — topY/bottomY, minScale/maxScale, minAlpha/maxAlpha 수치 보관 |
| `DepthObject.cs` | `Scripts/Domain/` | SpriteRenderer 필수. Update()마다 Y→normalizedDepth 계산 → 스케일/알파/sortingOrder 적용 |
| `GeckoMovementAI.cs` | `Scripts/Domain/` | 코루틴 기반 랜덤 이동. climbTargets 중 근처(1.5u) 있으면 30% 확률로 기어오름. moveBounds(Rect) 내에서만 이동 |
| `TerrariumDepthManager.cs` | `Scripts/Domain/` | `GetScaleAtY(y)` 미리보기 제공. `Register(DepthObject)` 외부 등록 지원 |

### HomeUIController.cs 변경
- `[Header("테라리움 깊이 & AI")]` — `_geckoMovement`, `_depthManager` SerializeField 추가
- `OnEnable()` — `_geckoMovement.enabled = true` 호출
- `RefreshTerrarium()` — 장식 활성화 시 `EnsureDepthObject()` 호출
- `EnsureDepthObject()` 헬퍼 — SpriteRenderer 없는 슬롯은 스킵, DepthObject 자동 추가 후 Register

### 생성된 폴더
- `Assets/_Game/Resources/Configs/` — DepthScaleConfig.asset 보관 위치

### Unity Editor에서 남은 작업
> 2026-09-14: 아래 항목은 모두 **취소** — 깊이 시스템(`DepthScaleConfig`·`DepthObject`·`TerrariumDepthManager`)은 2026-09-12에 삭제했고, `_geckoMovement`는 `Hako > Gecko > ①`이 연결한다. 남은 빈 폴더 `Resources/Configs`는 Unity를 열기 전에 지운다.

- [x] ~~`DepthScaleConfig.asset` 생성~~ → 취소
- [x] ~~게코 GameObject에 `DepthObject.cs` + `GeckoMovementAI.cs` 추가~~ → 취소 (게코는 UI로 그리므로 `DepthObject` 금지. `GeckoMovementAI`는 `Hako > Gecko > ①`이 붙인다)
- [x] ~~씬 빈 GameObject에 `TerrariumDepthManager.cs` 추가~~ → 취소
- [x] ~~`HomeUIController` Inspector에 `_geckoMovement`, `_depthManager` 연결~~ → `_depthManager` 필드 삭제, `_geckoMovement`는 ①이 연결

### 설계 메모
- `DepthObject`는 Image 기반 슬롯에는 자동 스킵 (SpriteRenderer 없으면 무시)
- 모든 수치([TBD]): `moveSpeed=80`, `waitTimeMin=1.5`, `waitTimeMax=4`, `CLIMB_DETECT_RADIUS=1.5`, `CLIMB_CHANCE=0.3`

---

## 2026-09-11 — 코드 리뷰 반영 + 핵심 루프 복구 + 손맛 연출 + 아트 방향

`109b095`(게코 코드 애니메이션) 리뷰 결과와 "고급스럽고 귀엽고 재미있게" 4단계 계획을 한 번에 반영.
2차 기능은 추가하지 않았다 — 전부 기존 기능(STEP 1~6)의 정확성과 완성도 작업.

### 1단계 — 핵심 루프 복구

| 파일 | 변경 |
|------|------|
| `AppBootstrap.cs` | 실행 중 **30초마다** 시간 진행 (`PROGRESS_TICK_SECONDS`, 저장 안 함) · 백그라운드 **복귀 시에도** 보정 + 저장 · `targetFrameRate = 60` · `AudioManager.Create()` |
| `GeckoManager.cs` | `ApplyElapsedProgressAll()` 추가 (시작·복귀·실행 중 모두 이것 하나) · 돌봄 결과 `CareResult` 반환 · 돌봄 제한 · 첫 허물 빠르게 |
| `GeckoEventQueue.cs` (신규) | 성장·허물 사건 대기열. 부팅 중 생긴 사건도 모았다가 홈에서 연출 |
| `GameManager.cs` | `Events` 추가 (Initialize 인자 1개 추가) |
| `SceneRouter.cs` + `SceneFader.cs` (신규) | 크림색 페이드 전환, 전환 중 입력 차단 (`IsTransitioning`) |

**예전 문제:** `OnApplicationPause(false)`(복귀)에서 아무것도 안 해서 백그라운드 시간이 통째로 사라졌고, 앱을 켜 둔 동안 상태값이 전혀 줄지 않았다.
허물·성장은 부팅 중(홈 화면 없음)에만 판정돼 연출·결과 패널이 사실상 재생되지 않았다.

**돌봄 제한 (`[TBD]`)**

| 행동 | 제한 | 게코 반응 |
|------|------|----------|
| 먹이 | hunger ≥ 95면 거절, **아이템 차감 안 함** (영양제 예외) | `Refuse` 고개 돌리기 + "배불러요" |
| 물 | thirst ≥ 95면 거절 | `Refuse` + "목 안 말라요" |
| 청소 | cleanliness ≥ 95면 변화 없음 | "이미 반짝반짝!" |
| 쓰다듬기 | 연달아 **4번**까지 좋아함, 8초에 1회분 회복. 넘으면 애정도 변화 없음 + 기분 -2 | `Angry_TailFlick` + "그만 만져~" |

**허물 페이싱:** 첫 허물 `FIRST_MOLT_PROGRESS_PER_HOUR = 1.67` (약 60시간 = 2.5일, 첫 주 안에 큰 이벤트) → 이후 0.20 (약 21일).
성장 조건(1→2: 30일+허물1, 2→3: 60일+허물3, 3→4: 120일+허물5)과 어긋나지 않는다 (허물 5회 ≈ 87일).

### 2단계 — 손맛 연출

| 파일 | 역할 |
|------|------|
| `UI/Fx/GeckoFx.cs` | 먹이가 떨어지고 **혀끝에 붙어** 입으로 · 분무 + 할짝마다 물방울 · 하트 · 반짝이 · 허물 조각 · 성장 빛 고리 · 말풍선 |
| `UI/Fx/UIParticles.cs` | UI 파티클 (Image 재사용, Update 하나) |
| `UI/Fx/SpeechBubble.cs` | 머리 위 말풍선 (홈의 TMP 글꼴 재사용) |
| `UI/Fx/UIPressScale.cs` | 버튼 눌림·튕김·'톡'. **씬의 모든 Button에 자동** (`UIFeelInstaller`), 목록 슬롯은 슬롯 스크립트가 붙임 |
| `UI/Fx/FxSprites.cs` | 연출 그림을 코드로 생성 (`Resources/Fx/`에 넣으면 교체) |
| `Core/AudioManager.cs` · `SfxLibrary.cs` · `SfxSynth.cs` · `Sfx.cs` | 효과음 16종 + 테라리움 환경음(배경음) 합성. `Resources/Audio/`에 넣으면 교체 |
| `Core/Haptics.cs` | Android 짧은 진동 (VibrationEffect, 설정 따름) |
| `HomeUIController.cs` | 돌봄 결과별 반응, 게이지 부드럽게 + 오를 때 부풂 + 위험 깜빡임, 코인·젬 카운트업, **사건 연출 대기열** (팝업·다른 동작이 끝나길 기다렸다가 차례로) |
| `GeckoAnimatorController.cs` | 사건 직접 구독 제거 → `PresentEvent`, 성장 전 크기로 잡아 두기, 종별 그림·깜빡임, 홈 진입 시 기분 즉시 적용 |
| `GeckoMotor.cs` | 새 동작 `Refuse`·`Molt_Itch`(허물 준비 중 가끔), `ActionStarted` 이벤트, 연출 타이밍 상수 공개 |
| 상점 · 보상 · 분양 · 꾸미기 UI | 성공/실패 효과음 + 진동 |

설정의 **배경음 끄기가 효과음까지 꺼지던 문제** 수정 (`AudioListener.volume = 0` → 배경음만 끔).

### 3단계 — 아트 방향

- `ART_GUIDE.md` 신규: 팔레트, 선·명암, 단계별 비율, 표정, 파츠 규격, UI 규칙, 글꼴, 사운드, 외주 체크리스트
- 프록시 그림: 회색 → **따뜻한 살구색 크레스티드** + 볼터치 + 갈색 외곽선
- 성장 단계별 비율 스킨 자동 생성: Hatchling·Baby 머리 ×1.22 · 눈 ×1.12 · 꼬리 ×0.88 / Juvenile 머리 ×1.10 (`GeckoProxyArt.GenerateStageSkins`)
- `GeckoSpeciesSO`에 `skin`, `canBlink` 추가 (`leopard.asset` canBlink = 1)

### 리뷰 지적 사항 반영

| # | 내용 | 파일 |
|---|------|------|
| 4 | 꼬리 반동이 프레임 속도에 따라 달라짐 → `dt` 반영 | `GeckoMotor.SimulateTail` |
| 5 | 캔버스째 PNG로 스킨을 만들면 관절이 캔버스 기준으로 틀어짐 → 투명 여백 뺀 실제 범위로 | `GeckoSkinImporter` |
| 6 | 스킨 재생성 시 에셋 이름이 비는 문제 | `GeckoSkinImporter` |
| 7 | 보폭 공식 `2π` → `4·L·sin(A)` (발 미끄러짐) | `GeckoMotor.StrideLength` |
| 8 | 플레이 중 파츠 컴포넌트 교체 시 NullRef | `GeckoRig.EnsureParts` |
| 9 | 먹이·물 버튼 `_geckoAnimator` null 체크 누락 | `HomeUIController` |
| 10 | 홈 진입 때마다 기분 자세가 0.7초에 걸쳐 바뀜 | `GeckoMotor.SetMood(…, immediate)` |
| 11 | 종별 그림·깜빡임 미적용 | `GeckoAnimatorController.ApplySpecies` |
| — | `③ 스킨 적용` 시 프록시 단계별 그림이 남던 문제 | `GeckoRigBuilder` |
| — | `GeckoData.cs` CP949 인코딩 → UTF-8, 주석의 성장 단계 0~4로 수정 | `GeckoData.cs` |

### 검증 (Unity 없이 한 것)

- 런타임 스크립트 전체 + `Editor/Gecko` 도구를 Unity API 시그니처 스텁으로 컴파일: Android / 일반 / 에디터 세 설정 모두 **오류 0 · 경고 0**
- 도메인 로직 실행 테스트 **26개 통과** (돌봄 제한, 첫 허물 60시간, 사건 대기열, 성장 전 단계 조회, 시간 보정 중복·시계 역행, 배경 보유)
  - 이 과정에서 쓰다듬기 피로가 탭 사이 회복 때문에 5번째까지 허용되던 문제를 발견해 수정
- 효과음 16종 + 환경음 합성 수치 검사 (클리핑·NaN 없음, 반복 이음매 연속), 합성 시간 약 70ms
- 프록시 게코·연출 그림을 PNG로 렌더링해 모양 확인

### Unity Editor에서 남은 작업

- [ ] 프로젝트 열기 → 생성되는 `.meta` 파일 **바로 커밋** (스크립트 27개 — 이전 커밋 15개 + 이번 12개 — 와 폴더 `UI/Gecko`, `UI/Fx`, `Editor/Gecko`)
- [ ] 콘솔에 컴파일 오류가 없는지 확인
- [ ] `MainHome` 열기 → `Hako > Gecko > ① 프록시 게코 만들기` → Ctrl+S
- [ ] 생성물 커밋: `Textures/Gecko/Proxy/*.png`, `GeckoSkins/*.asset`, `MainHome.unity`
- [ ] Boot 씬부터 실행해 아래 확인
  - 먹이: 벌레가 떨어지고 혀로 낚아챔 → 오물오물 / 배부를 때 거절
  - 물: 분무 + 할짝할짝 물방울 / 쓰다듬기 5번 연타 → 삐짐
  - 버튼 눌림·'톡' 소리, 씬 전환 페이드, 코인 카운트업(일일 보상)
  - GeckoObject 선택 → Inspector 버튼으로 동작 15종 미리보기
- [ ] 실제 기기(Android)에서 진동·60fps·소리 확인

### 설계 메모

- **사건 흐름:** `GeckoManager` 이벤트 → `GeckoEventQueue` (AppBootstrap이 시간 보정 **전에** 생성) → `HomeUIController.EventPresenter`가 하나씩 꺼냄 → `GeckoAnimatorController.PresentEvent` + `GeckoFx` + 결과 패널
- **실행 중 진행은 저장하지 않는다:** 메모리의 상태값과 `lastUpdatedTicks`가 함께 움직여서, 저장 전에 앱이 죽어도 다음 실행 때 파일 기준으로 다시 계산돼 결과가 같다
- **글꼴:** 정적 아틀라스라 특수문자(★♥→…✦)가 없다 → 문구는 한글·영문·숫자·기본 기호만. 결과 알림 문구를 한국어로 바꾸면서 기존 `✦`도 제거
- **꾸미기 버그 2건 수정:** 장식 슬롯이 가득 찼는데 코인을 먼저 차감하던 문제 / 산 배경·바닥을 다시 고르면 또 결제되던 문제 (`TerrariumData.ownedDecorIds` 추가, 예전 저장 파일은 `TryMigrate`에서 보정)

---

## 2026-09-12 — 마무리 (STEP 6 완료 + 출시 설정)

남아 있던 기능 구멍과 빌드 설정을 채웠다. 이제 코드·설정 쪽에서 MVP에 빠진 것은 없다.

### 기능

| 작업 | 내용 |
|------|------|
| **장식 빼기** | 이미 놓은 장식을 다시 누르면 무료로 빠진다 (슬롯 4개가 영영 잠기던 문제). 목록에 "빼기"로 표시 |
| **로컬 알림** | 백그라운드로 갈 때 예약 — ① 배고픔·목마름이 25까지 떨어질 시각 ② 다음 날 오전 10시 일일 보상. 앱을 열면 취소. `NotificationScheduler`가 Mobile Notifications 패키지를 **리플렉션으로** 부르므로 패키지가 없어도 컴파일이 깨지지 않는다 |
| **알림 권한** | Android 13+ `POST_NOTIFICATIONS` — 설정에서 알림을 켤 때, 앱 시작 시 요청 |
| **조사 처리** | `KoreanText` — "하코가 / 별님이". 알림·결과 알림이 같은 규칙을 쓴다 |
| **어느 씬에서나 실행** | Boot을 거치지 않고 MainHome·Store를 바로 실행하면 매니저를 만들고 그 씬을 다시 연다 (`AppBootstrap.EnsureBootstrapped`). 예전에는 빨간 오류만 뜨고 화면이 죽었다 |
| **로직 자가 검사** | 메뉴 `Hako > 검사 > 로직 자가 검사` — 규칙 26가지를 Unity 안에서 즉시 확인. 임시 저장 파일을 쓰고 끝나면 지운다 (`SaveManager`에 파일 이름 주입 추가) |

### 출시 설정 (ProjectSettings)

| 항목 | 이전 | 이후 |
|------|------|------|
| 패키지 이름 | 비어 있음 (**안드로이드 빌드 불가**) | `com.tnbsoft.hako` |
| 회사 · 제품 | DefaultCompany · GeckoNest | TNBSoft · Hako |
| 버전 | 1.0 | 0.1.0 (versionCode 1) |
| 화면 방향 | 자동 회전 (가로도 허용) | **세로 고정** |
| Physics 2D 중력 | y = -9.81 | **y = 0** (CLAUDE.md 필수 설정) |
| 앱 아이콘 | 없음 | `Textures/Icons/app_icon.png` → Legacy·Round 슬롯 12칸 |

> 회사·제품 이름이 바뀌어 **에디터의 저장 경로가 달라진다** — 에디터에서 쓰던 기존 저장 데이터는 새로 시작된다 (기기 빌드와는 무관).

### 앱 아이콘

코드로 그린 임시 아이콘 (초록 배경 + 크림 원 + 게코 얼굴, 1024×1024). 얼굴이 75% 안쪽에 들어와 런처가 모서리를 깎아도 잘리지 않는다. 규칙은 `ART_GUIDE.md` 10번.

### 검증

- 컴파일 검사 3종(Android 런타임 / 일반 런타임 / 에디터 + Gecko 도구 + 자가 검사): **오류 0 · 경고 0**
- 도메인 로직 테스트 26개: 모두 통과
- 아이콘·프록시 게코 렌더링으로 모양 확인

### 남은 것 (사람이 해야 하는 일)

1. Unity로 열어 콘솔 확인 → 새로 생긴 `.meta` 커밋 (스크립트 30개 + 폴더 3개 + `app_icon.png`)
2. `MainHome`에서 `Hako > Gecko > ① 프록시 게코 만들기` → 저장
3. `Hako > 검사 > 로직 자가 검사` 실행 → 모두 통과 확인
4. Boot 씬부터 실행해 돌봄·연출·소리 확인
5. 알림을 쓰려면 Mobile Notifications 패키지 설치
6. Android 기기에서 APK 내부 테스트 (진동·알림·60fps)
7. 최종 아트·사운드·글꼴 교체 (`ART_GUIDE.md`), 개인정보 처리방침 URL 확인, 스토어 등록

### 정리 · 최적화 (같은 날 추가 작업)

**지운 것 — 모두 git 이력에 남아 있어 되살릴 수 있다**

| 대상 | 이유 |
|------|------|
| `DepthObject.cs` · `TerrariumDepthManager.cs` · `DepthScaleConfig.cs` | 죽은 코드. 장식 슬롯이 UI Image라 항상 건너뛰었고, 게코는 UI로 그려 쓸 일이 없다. `HomeUIController.EnsureDepthObject`도 함께 제거 |
| `HomePanelBuilder.cs` · `TerrariumSceneBuilder.cs` | 3월에 씬을 한 번 만들고 역할이 끝난 빌더. 지금 실행하면 손으로 다듬은 씬을 덮어쓴다 |
| `Popup.unity` + `SceneRouter.OpenPopup/ClosePopup` | 카메라만 있는 빈 씬. 팝업은 모두 화면 안 패널로 처리한다. 추가로 열면 AudioListener가 2개가 되는 문제도 있었다 |
| `ScriptableObjects/` (옛 장식 5종 + 빈 폴더) | `Resources/Decor`로 대체됨. 프록시 스킨 저장 경로는 `_Game/GeckoSkins`로 이동 |
| 빈 폴더 `Animations`, `Audio`, `Prefabs/Gecko`, `Plugins`, `StreamingAssets` | 비어 있음. 소리는 `Resources/Audio/…`에서 읽는다 |
| 빌드 설정의 `SampleScene` 항목 | 파일이 없는 유령 항목 |
| 패키지 `com.unity.visualscripting`, `com.unity.multiplayer.center` | 쓰지 않음 (에디터 로딩만 느려짐) |
| 미사용 public API | `KoreanText.WithTopic/WithObject`, `GeckoSkin.HasEye/HasMouth`, `GeckoParts.TryParse*`, `GeckoRig.RestSize/RestPivot/RestScale/GrowthStage`, `SpeechBubble.Hide`, `UIParticles.Clear` |

**최적화**

| 항목 | 이전 | 이후 |
|------|------|------|
| 배경 `bg_desert` · `bg_jungle` (1080×2411) | maxTextureSize 2048 → 화면보다 작게 줄어듦 | 4096 — 원본 해상도 유지 |
| 성장 단계 일러스트 5장 (1024², 작은 아이콘으로만 표시) | 2048 | 512 (메모리 약 1/4) |
| 프록시 게코 텍스처 26장 | 압축 없음 | 압축 (임시 그림이라 화질 영향 미미) |
| 성장 단계 아이콘 | 실제 일러스트 2장 + 임시 아이콘 3장 혼용 | 일러스트 5장으로 통일 |

**검증 (Unity 없이)**

- 컴파일 3종(Android 런타임 / 일반 런타임 / 에디터 전체) **오류 0 · 경고 0** — 이번에는 `Assets/Editor` 아래 **모든** 스크립트를 포함해 확인
- 도메인 로직 테스트 27개 통과
- 씬·프리팹이 참조하는 프로젝트 스크립트 11종이 모두 존재 (Missing Script 없음)
- GUID 중복 없음 (162개), 코드가 찾는 Resources 경로 5종 모두 존재
- 삭제한 스크립트를 참조하는 코드·씬·에셋 없음

---

## 2026-09-14 — 리뷰 후속: 저장 복구 · 첫 실행 배경 · 글자 깨짐 · 보상 표시

`a2a17d8` 기준 전체 재검토에서 남아 있던 문제를 고쳤다. 새 기능은 추가하지 않았다.

### 수정

| 파일 | 변경 |
|------|------|
| `SaveManager.cs` | 임시 파일을 `Flush(true)`로 디스크에 확정한 뒤 교체. 읽기 순서 **메인 → (메인이 없으면) 임시 → 백업**. 예전에는 메인이 없으면 백업을 보지 않고 새 데이터를 만들어, 저장 도중(메인 삭제 직후) 멈추면 데이터가 초기화될 수 있었다 |
| `TerrariumData.cs` · `SaveManager.TryMigrate` | 기본 배경 `bg_jungle`·바닥 `floor_soil` (`DEFAULT_BACKGROUND_ID/DEFAULT_FLOOR_ID`). 새 데이터는 필드 초기값, 빈 값인 예전 저장 파일은 로드 때 채운다 |
| `GeckoData.CreateNew` · `PlayerRepository.EnsureStarterGecko` | 게코 생성 코드 3곳(SaveManager·AppBootstrap·StoreManager) → 1곳. 새 플레이어도 AppBootstrap의 기본 게코 보장 경로로 하코와 첫 먹이 3개를 받는다 |
| `RewardManager.GetStreak/PeekReward` | 받기 전 = 받을 연속 일수·보상, 받은 뒤 = 오늘 받은 일수·보상. 예전에는 "연속 1일"에 2일째 보상이 보이는 식으로 어긋났다 |
| `GeckoListUIController` | 드롭다운 항목과 같은 순서로 모은 목록에서 종을 고른다 (판매 목록에 빈 칸이 있으면 다른 종이 분양되던 문제) |
| `MainHome.unity` · `Terrarium.unity` | 글꼴에 없는 글자 11곳 (아래 표) |
| `HakoSelfTest.cs` | 저장 복구 · 기본 배경 · 기본 게코 · 보상 표시 검사 추가. `Fresh()`도 실제 게임과 같은 `EnsureStarterGecko` 경로를 탄다 |

### 글자 깨짐 (□) — 글꼴 글자표로 전수 확인

`NanumGothic-Regular SDF` 정적 아틀라스 = 한글 11,172자 + 자모 + ASCII. Nanum 텍스트는 예비 글꼴이 없고, `LiberationSans SDF` 텍스트는 Nanum → LiberationSans 동적(원본 TTF에 있는 글자만)으로 넘어간다.

| 위치 | 이전 | 이후 |
|------|------|------|
| MainHome 보상·설정 패널 닫기 버튼 2곳 | ✕ | X |
| MainHome 개인정보 버튼 | 개인정보 처리방침 ↗ | 개인정보 처리방침 |
| MainHome 하단 탭 아이콘 5곳 (꾸미기·게코·상점·설정·보상) | 🏠 🦎 🛒 ⚙ 🎁 | 빈 텍스트 — **아이콘 그림 필요** (라벨은 그대로) |
| Terrarium 뒤로 버튼 | ← 홈으로 | < 홈으로 |
| Terrarium 코인·젬 초기 텍스트 | 💰 0 / 💎 0 | 0 (실행 중에는 코드가 숫자로 덮어써서 원래도 보이지 않던 값) |

코드 안의 문구(말풍선·결과 알림·보상·에러)는 모두 아틀라스에 있다.

### 검증

- 씬·프리팹 텍스트 전체를 글꼴 글자표·예비 글꼴·원본 TTF와 대조 → 누락 0
- **컴파일과 자가 검사는 이번에 돌리지 못했다** — Unity에서 콘솔 확인 후 `Hako > 검사 > 로직 자가 검사` 실행 필요

### 남은 결정

- 성장촉진제(`growth_booster`, hunger 0)는 먹이 버튼 자동 선택(`hungerRestore > 0`)에 걸리지 않아 사도 쓸 수 없다. `growthExp`도 성장 조건에 쓰이지 않는다 → 성장 조건에 반영할지, MVP에서 판매를 뺄지
- `leopard.asset`이 `coinPrice: 300`인데 `isUnlockedByDefault: 1`이라 무료 분양된다
- 알림 권한 요청 시점 (지금은 첫 실행 직후) · 일일 보상 기준 시각 (UTC 자정 = 한국 09시) · 자연사(900일)

---

## 2026-09-15 — 홈 상태 게이지 · 돌봄 버튼 · 시간 건너뛰기

Unity에서 첫 플레이 확인. 컴파일 오류 0, 로직 자가 검사 50개 모두 통과, 프록시 게코 생성·기본 배경 표시 확인.
플레이 중 두 가지를 발견했다 — 상태 게이지가 값과 상관없이 흰 네모로만 보였고(30 이하일 때만 빨갛게), 돌봄 버튼이 Unity 기본 흰 버튼 그대로라 정글 배경과 어울리지 않았다.

### 원인

- `StatusPanel/*Bar/Fill` Image가 **Simple** 타입 → 코드가 `fillAmount`를 바꿔도 화면에 반영되지 않음
- 바탕(BG)과 채움(Fill)이 같은 흰색 100×100 네모로 겹쳐 있었고, 코드도 평소 색을 흰색으로 덮어씀

### 수정

| 대상 | 변경 |
|------|------|
| `MainHome.unity` 돌봄 버튼 4개 | 바탕 짙은 초록 반투명 (하단 탭과 같은 색), 글자 연한 초록 · 30pt · 굵게 |
| `MainHome.unity` 상태 게이지 5행 | 가로 막대 — 바탕 180×26 둥근 반투명, 채움 174×20 **Filled·Horizontal**, 아이콘은 막대 왼쪽, 이름은 막대 위. 위에서부터 배고픔 → 목마름 → 기분 → 건강 → 청결 |
| `HomeUIController.GaugeView` | 상태별 평소 색(`GAUGE_*`: 주황·하늘·분홍·초록·금색), 막대 오른쪽 위 숫자(실행 중 생성), Fill이 Filled가 아니면 실행 시 보정 |
| `GameManager.DebugSkipTime` · `Editor/HakoTimeSkip.cs` | 메뉴 `Hako > 검사 > 시간 건너뛰기` (+1시간 / +6시간 / +24시간 / +7일). 에디터 전용 — 배고픔이 1시간에 4씩 줄어 실제 시간으로는 확인이 어려워서 |

씬은 Unity를 닫은 상태에서 스크립트로 값 149개를 직접 바꿨다 (씬을 한 번 고치고 끝나는 빌더 메뉴는 만들지 않음 — 예전 `HomePanelBuilder`처럼 죽은 코드로 남기 때문). 적용 전 원본은 세션 임시 폴더에 백업했다.

### 확인 필요 (Unity)

- 컴파일 · 게이지와 버튼 모습 · 숫자 위치 · 시간 건너뛰기 동작 — **아직 Unity에서 열어 보지 않았다**
- 새 파일 `Assets/Editor/HakoTimeSkip.cs`의 `.meta` 커밋

---

## 2026-09-15 — 다국어 (기기 언어가 한국어면 한글, 그 밖에는 영어)

사용자 결정으로 지금 구현했다. 한글(하단 탭·테라리움·보상)과 영어(돌봄 버튼·게이지·상점·게코 목록)가 섞여 있던 문구도 이번에 정리됐다.

### 수정

| 파일 | 변경 |
|------|------|
| `Core/Loc.cs` (신규) | 번역표 92개 키 (공통·탭·홈·단계·한마디·결과 알림·보상·설정·상점·게코 목록·꾸미기·알림·아이템·장식·종 이름) + 언어 결정 + 이름 조사(`Subject`) |
| `UI/SceneTextLocalizer.cs` (신규) | 씬이 열릴 때 번역표 원문(한글 또는 영어)과 똑같은 TMP 글자를 현재 언어로 바꾼다 — 씬 파일에 컴포넌트를 붙이지 않음 |
| 코드 문구 | `HomeUIController`(한마디·결과 알림·단계 이름·먹이 표시) · `RewardPanelUI` · `StoreManager`(구매 실패) · `TerrariumUIController` · `GeckoListUIController`(드롭다운) · `ItemSlotUI`·`DecorSlotUI`·`GeckoSlotUI` · `NotificationScheduler` · `PlayerRepository`(기본 게코 이름) |
| `SettingsData` · `SaveManager` · `PlayerData` | `language` 기본값 `""` = 기기 언어. 저장 버전 3 — 예전 저장의 `"ko"`(고른 적 없는 기본값)를 비움 |
| `SettingsManager.SetLanguage` · `Editor/HakoLanguageMenu.cs` (신규) | 메뉴 `Hako > 검사 > 언어 > 한국어/영어/기기 언어` — 에디터는 PC 언어를 따라 영어 화면을 볼 방법이 없어서 |
| `MainHome.unity` | 청결 게이지 이름 `Clean` → `Cleanliness` (청소 버튼 `Clean`과 원문이 같아 번역이 모호해지는 것 방지) |
| `HakoSelfTest` | `TestLocalization` — 누락·`{0}` 자리 수·원문 겹침·언어 전환·조사·저장 버전 3·글꼴 글자 |

### 설계 메모

- **씬 글자를 원문 대조로 번역하는 이유:** 씬 약 40곳에 번역 컴포넌트를 붙이려면 씬 파일에 오브젝트를 수십 개 추가해야 해서 손상 위험이 크다. 대신 씬 글자를 고칠 때 번역표의 원문과 똑같이 적어야 한다 (CLAUDE.md 다국어 절)
- 게코 이름은 사용자 데이터라 번역하지 않는다 — 기본 게코는 생성 시점의 언어로 "하코" / "Hako"
- `Debug.Log`는 한국어 그대로
- 설정 화면의 언어 선택 UI는 아직 없다 (`SettingsManager.SetLanguage`만)

### 검증 (Unity 없이)

- **Unity 설치본의 컴파일러와 Unity가 마지막으로 쓴 컴파일 옵션(`Library/Bee/*.rsp`)으로 실제 컴파일:** 런타임 54개 파일 · 에디터 9개 파일 — **오류 0 · 경고 0** (새 파일 `Loc`·`SceneTextLocalizer`·`HakoTimeSkip`·`HakoLanguageMenu` 포함, 게이지·시간 건너뛰기 수정분도 함께 확인)
- 번역표 92개: 빈 문구 0 · `{0}` 자리 수 다름 0 · 글꼴(Nanum 아틀라스)에 없는 글자 0 · 뜻이 다른 원문 겹침 0
- 씬·프리팹 글자: 고정 글자는 모두 번역표에 있음. 표에 없는 것은 코드가 실행 중 덮어쓰는 자리표시(`연속 1일`·`코인 +50`·`v1.0.0`·`아이템명`·`Name`)와 `X`, 그리고 예전부터 있던 `GeckoSlot` 프리팹의 `Button`·`New Text`
- **로직 자가 검사 · 화면 확인은 Unity에서 필요**

### 확인 필요 (Unity)

- 새 파일 4개(`Loc.cs`·`SceneTextLocalizer.cs`·`HakoTimeSkip.cs`·`HakoLanguageMenu.cs`)의 `.meta` 커밋
- `Hako > 검사 > 로직 자가 검사` (새 검사 포함)
- 한국어 화면 → `Hako > 검사 > 언어 > 영어`로 영어 화면 → 글자 넘침·□ 확인 → `기기 언어`로 되돌리기
- `GeckoSlot` 프리팹의 `Button`·`New Text` 글자가 목록에 실제로 보이는지 (보이면 프리팹 정리 필요)

---

## 2026-09-15 — 윗줄 코인·젬 이름표

플레이 확인에서 윗줄 숫자 두 개가 무엇인지 알 수 없다는 지적 (홈은 왼쪽이 젬, 오른쪽이 코인). 씬은 건드리지 않고 코드로만 이름표를 붙였다.

| 파일 | 변경 |
|------|------|
| `Loc.cs` | `hud.coin` = "코인 {0}" / "Coins {0}", `hud.gem` = "젬 {0}" / "Gems {0}" |
| `HomeUIController.CountView` | 카운트업 숫자에 이름표. 이름표가 붙어 길어져도 두 줄로 꺾이지 않게 줄바꿈 끔 (홈 글자 칸 폭 200) |
| `StoreUIController` · `GeckoListUIController` · `TerrariumUIController` | 같은 이름표 (테라리움 윗줄은 HorizontalLayoutGroup이 두 글자를 나란히 배치 — 겹치지 않음) |

검증: Unity 컴파일러로 오프라인 컴파일 — 오류 0 · 경고 0. 이름표 글자(코인·젬·Coins·Gems)는 모두 글꼴 아틀라스에 있다.

---

## 2026-09-15 — 홈 글자 읽기 쉽게 (정글 배경 위)

플레이 확인에서 "글씨가 잘 안 보인다" — 윗줄 재화·이름·성장 단계·게이지 이름과 숫자가 가는 흰 글자로 복잡한 정글 그림 위에 바로 놓여 있었다. 상태 패널은 투명도 0.82까지 걸려 더 흐렸다.

| 대상 | 변경 |
|------|------|
| `MainHome.unity` 상태 패널 | 뒤에 둥근 반투명 어두운 판 추가 (Image, UISprite · Sliced, 색 0.08·0.1·0.08 · 알파 0.45, 터치 안 막음). CanvasGroup 알파 0.82 → 1 |
| `MainHome.unity` 글자 | 윗줄 코인·젬, 게코 이름, 성장 단계, 게이지 이름 5개를 굵게 |
| `HomeUIController.ApplyHudReadability` | 배경 위 흰 글자에 부드러운 어두운 그림자 (TMP underlay). 머티리얼은 글꼴당 하나를 같이 씀. 수치 `HUD_SHADOW_*` `[TBD]`. 게이지 숫자는 `InitViews`가 만든 뒤에 적용 |

- 버튼 글자는 어두운 바탕이 있어 그대로 둠
- 씬은 Unity를 닫은 상태에서 스크립트로 수정 (백업 → 건수 확인 → 저장 → fileID 중복 0 확인). 판은 게이지 BG의 CanvasRenderer·Image 문서를 본떠 새 fileID로 추가
- 그림자는 실행 중에만 보인다 (편집 화면에는 굵게·판만 보임)

검증: Unity 컴파일러로 오프라인 컴파일 — 오류 0 · 경고 0. **실제 모습은 Unity 플레이로 확인 필요** (그림자 세기는 `HUD_SHADOW_*`로 조절)

---

## 2026-09-15 — 홈 화면 배치 정리 (게코 공간 확보)

플레이 확인에서 "상태 창과 버튼이 게임을 방해한다" — 상태 창이 왼쪽 가운데에 세로로 길게(화면 높이 약 1/3), 돌봄 버튼 4개가 오른쪽 가운데에 세로로 쌓여 게코 공간을 가렸다.
버튼들은 부모(`ActionButtons`, 화면 아래 영역)와 달리 위쪽 좌표(1500)로 튀어나가 있어 처음 설계(내비 바 위 가로 줄)와도 달라져 있었다.
배치안 3가지(위·아래 한 줄 / 접기형 / 게코 터치 메뉴)를 보고한 뒤 사용자 결정: **추천안 + 숫자 표시 + 버튼 글자 + 코인 먼저**.

### 배치 (1080×2400 기준)

| 영역 | 변경 |
|------|------|
| 위 1줄 | 이름(왼쪽, 42pt) · **코인 → 젬**(오른쪽, 32pt). `TopBar`의 틀어진 크기·위치(폭 +800, x −400)도 바로잡음 |
| 위 2줄 | 성장 단계 아이콘(56) + 단계 이름(왼쪽) |
| 허물 막대 | 좌우 여백 48, 두께 12 |
| 상태 띠 | `StatusPanel`을 위쪽 가로 띠로 (높이 104). 5칸 가로 배치, 각 칸 `아이콘 + 막대 + 숫자`. 이름 글자(`Label`)는 꺼 둠 |
| 돌봄 버튼 | `ActionButtons`를 내비 바 바로 위(아래에서 136~308)로. 둥근 버튼(Knob 스프라이트) 160 × 4개 가로 배치, 위 아이콘 + 아래 글자(22pt) |
| 가운데 | 비워 둠 — 게코가 걷는 높이(발 400~520)는 이미 버튼 줄 위라 그대로 |

### 코드

| 파일 | 변경 |
|------|------|
| `HomeUIController._careButtonIcons` · `EnsureCareButtonIcons` | 버튼 아이콘을 실행 중에 한 번 붙인다 (씬 오브젝트를 늘리지 않음). 지금은 32px 상태 아이콘 재사용 — 먹이 = 배고픔, 물 = 물방울, 쓰다듬기 = 하트, 청소 = 별 |

- 씬은 Unity를 닫은 상태에서 스크립트로 수정 — 백업 → 값 178개 각각 정확히 1곳 일치 확인 → 저장 → 행 활성 유지 5/5 · 라벨 꺼짐 5/5 · fileID 중복 0 · 줄 수 +5(아이콘 참조 4줄 + 필드 이름) 확인
- 검증: Unity 컴파일러로 오프라인 컴파일 — 오류 0 · 경고 0. **실제 모습은 Unity 플레이로 확인 필요** (32px 아이콘은 버튼에서 조금 흐릴 수 있음 — 최종 아트에서 교체)

---

## 2026-09-15 — 먹이 고르기 · 좋은 먹이의 이득 · 먹이별 반응

사용자 지적: "먹이를 종류별로 사는데 줄 때는 똑같이 동작한다. 좋은 먹이를 먹으면 더 빨리 크거나 이득이 있어야 하지 않나"
확인한 원인 — 먹이 버튼은 **가장 먼저 산 먹이만 자동 선택**, 반응은 모두 같은 받아먹기, 기분 보너스는 전부 0, **`growthExp`는 성장 조건에 쓰이지 않아** 성장치가 의미 없었고, 성장촉진제(hunger 0)는 자동 선택에서 빠져 먹일 수 없었다.
계획(고르기 A · 이득 B · 반응 C)을 보고한 뒤 사용자 결정: **① 전부 + 제안 수치 그대로**. CLAUDE.md의 "먹이 종류 선택 UI는 2차 MVP" 규칙은 "MVP에 포함"으로 고쳤다.

### 먹이 수치 (`Resources/Items/*.asset`, 모두 `[TBD]`)

| 먹이 | 가격 | 배고픔 | 성장치 | 기분 | 특수 | 종류 | 좋아하는 종 |
|------|------|:---:|:---:|:---:|------|------|------|
| 귀뚜라미 | 10C | +20 | 1 | 0 | — | Normal | — |
| 것로딩 귀뚜라미 | 15C | +25 | 2 | +2 | — | Normal | — |
| 밀웜 | 20C | +22 | 3 | +3 | — | Normal | 크레스티드 |
| 두비아 바퀴 | 30C | +28 | 4 | +4 | 허물 +5% | Big | 레오파드 |
| 슈퍼밀웜 | 40C | +32 | 5 | +6 | — | Big | 가고일 |
| 칼슘 영양제 | 25C | +5 | 0 | 0 | 건강 +10 · 허물 +10% | Supplement | — |
| 성장촉진제 | 5젬 | 0 | 8 | 0 | 배불러도 먹음 | Supplement | — |

### 규칙 (`GeckoManager`)

- **성장 가속:** `EffectiveAgeDays` = 실제 일수 + 성장치 × 3시간, 앞당기는 양은 필요한 실제 날짜의 **최대 30%** (15일 조건은 실제 10.5일이 한계). 허물 횟수 등 다른 조건은 그대로. 성장치는 단계가 오르면 0 → 단계마다 새로 쌓는다. 자연사는 실제 날짜 기준
- **좋아하는 먹이:** `ItemSO.preferredSpeciesIds`에 종이 있으면 애정도 ×2, 기분 +3
- **허물 보너스:** `GeckoData.moltBonus`에 쌓고(최대 +15%) 다음 `TryMolt` 성공률에 더한 뒤 성공·실패와 상관없이 0
- `FeedGecko(id, item, out FeedEffect)` — 상한에 걸려 덜 오른 만큼을 뺀 실제 효과를 UI에 넘긴다. 마지막으로 준 먹이는 `PlayerData.lastFoodItemId`

### 화면 · 반응

| 파일 | 변경 |
|------|------|
| `UI/FoodTray.cs` (신규) | 먹이 선반 — 먹이 버튼 → 버튼 줄 위에 가진 먹이 칸(아이콘·이름·개수·효과 최대 3줄·좋아함 표시). 마지막에 준 먹이가 맨 앞, 선반 밖 누르면 닫힘, 먹이 없으면 상점. 실행 중 생성 (씬 변경 없음) |
| `HomeUIController` | `FeedWith` · `PlayFeedReaction` — Normal = 받아먹기, Big = `Tongue_FeedBig` + 먹이 1.3배, Supplement = `GeckoFx.Dust` + 할짝, 좋아하는 먹이 = 받아먹기가 끝나면 `Happy_LookUp` + 하트 + "최고야!". 먹은 뒤 실제 효과 말풍선("성장 +5  기분 +6"). 선반이 열려 있으면 성장·허물 연출을 기다림 |
| `GeckoMotor` · `GeckoParts` | 새 동작 `Tongue_FeedBig`(2.6초) — 앞 1.26초는 받아먹기와 박자까지 같고(먹이가 혀끝에 붙는 연출 유지) 뒤에 오래 오물오물, 끝은 0 |
| `GeckoFx.Dust` · `FeedDrop(icon, sizeScale)` | 가루 연출(촉진제는 반짝이 + 차임), 큰 먹이 크기 |
| `GeckoAnimatorController` | `TriggerFeedBig` · `TriggerHappy` · `TriggerLick` |
| `Loc` | `food.hunger/growth/mood/health/molt/favorite`, `line.favorite` |
| `HakoSelfTest.TestFoodEffects` | 성장 가속 · 30% 상한 · 좋아하는 먹이(애정도·기분) · 마지막 먹이 기억 · 허물 보너스(+10%, 상한 15%, 1회용) · 촉진제 배불러도 먹음 · 에셋 수치가 표와 같은지 |
| `GeckoInspectors` | 미리보기 버튼 "큰 먹이 오물오물" |

- **기존 저장 파일:** 이미 쌓인 `growthExp`가 곧바로 성장 가속에 반영된다 (사용자에게 미리 알림). `moltBonus`·`lastFoodItemId`는 새 필드라 0/빈 값으로 시작
- 먹이 에셋은 Unity를 닫은 상태에서 스크립트로 수정 — 배고픔·성장치·건강이 표와 같은지 먼저 확인한 뒤 기분·허물·종류·선호 종만 넣었다 (원본은 세션 임시 폴더에 백업)

### 검증 (Unity 없이)

- Unity 컴파일러로 오프라인 컴파일 — 런타임 55개(새 `FoodTray` 포함) · 에디터 9개, **오류 0 · 경고 0**
- 번역표 101개 — 빈 문구 0 · `{0}` 자리 수 다름 0 · 글꼴에 없는 글자 0 · 원문 겹침 0
- **로직 자가 검사 · 화면은 Unity에서 확인 필요**

---

## 2026-09-15 — 시간 건너뛰기 기간 조정 · 잘 돌봄 모드

사용자 지적: "1시간·6시간은 필요 없고 2주·한 달이 더 낫지 않나"
확인 — 건너뛰기가 한 번에 반영돼 **오프라인 상한 48시간**이 걸린다. 한 달을 건너뛰어도 나이만 +30일이고 허물은 2일치(첫 허물 뒤 0.2/h × 48 = 9.6)만 진행, 배고픔·목마름이 0이 되어 건강 -32 → 긴 건너뛰기로는 허물 횟수가 필요한 성장 조건을 확인할 수 없었다.
사용자 결정: 아래 수정안 적용, 단축키·화면 테스트 버튼은 보류.

| 메뉴 | 동작 |
|------|------|
| `+6시간 (내버려 둠)` · `+24시간 (내버려 둠)` | 예전 그대로 한 번에 반영 (48h 상한). 6시간은 30 이하 경고 깜빡임을 보려고 남김 — 24시간은 배고픔이 바로 0 |
| `+7일 (잘 돌봄)` · `+2주 (잘 돌봄)` · `+30일 (잘 돌봄)` | 8시간씩 나눠 반영, 구간마다 배고픔·목마름·청결·기분 = 100. 허물·성장 사건이 실제 순서대로 쌓임. 애정도·건강은 건드리지 않음 |

- `+1시간` 삭제. 두 묶음 사이 메뉴 구분선 (priority 121 → 132)
- 구간을 8시간으로 한 이유: 목마름 100이 8시간 뒤 60 → 허물 판정의 목마름 보너스(> 50)가 유지되고 0이 되지 않아 건강이 줄지 않는다
- 에디터 전용 (`#if UNITY_EDITOR`) — 실제 게임의 48시간 상한은 그대로
- Unity 컴파일러로 오프라인 컴파일 — 런타임 55개 · 에디터 9개, **오류 0 · 경고 0**. 메뉴 동작은 Unity에서 확인 필요

---

## 2026-09-15 — 건강 회복 · 다음 성장 조건 보기 · 하코 중복 조사

사용자 지적: "성장을 하면 게코도 커져야 하는 것 아니냐? 왜 변화가 없냐?"
확인 — 크기 코드(`GeckoRig._stageScales`, `PresentEvent → SetGrowthStage`)는 정상. **성장 자체가 일어나지 않았다**: 에디터 로그에 `+30일 (잘 돌봄)` 3번 동안 "성장 단계 상승" 0줄, 성장치 121이 0으로 안 돌아감. 저장 파일의 하코는 이미 주버나일(2) — 나이 254일 · 허물 6회는 충족, **건강 10 < 50**에서 막힘.
원인이 된 설계 구멍: ① 건강은 배고픔·목마름 0일 때 줄기만 하고 회복 수단이 칼슘(+10)뿐 ② 왜 안 크는지 화면에서 알 수 없음. 사용자 결정: ①② 모두 적용 + 하코 중복 확인.

| 파일 | 변경 |
|------|------|
| `GeckoManager` | **건강 회복** `HEALTH_REGEN` 0.5/h `[TBD]` — 배고픔·목마름이 **둘 다 50 초과인 시간만큼** (`HoursUntilCareNeeded(g, 50)`로 줄어들기 전 값에서 계산, 48h 상한 안). 잘 먹이면 10 → 50에 약 80시간 |
| `GeckoManager` | `GrowthCheck` 구조체 + `CheckGrowth(g, realDays)` / `GetGrowthCheck(id)` — 단계별 조건표를 한 곳에 모음. `EvaluateGrowth`는 `check.AllMet`만 본다 (판정 결과는 예전과 같음) |
| `HomeUIController` | 성장 단계 글자에 실행 중 `Button` 추가 → 누르면 `DescribeGrowth` 말풍선 4초. 그 단계에 있는 조건만, 지금 값은 내림(49.6을 "부족 (지금 50)"으로 보이지 않게) |
| `SpeechBubble` | `MAX_W` 520 → 640 — 영어 "Affection 60 - Need (now 52)"가 줄바꿈되지 않게 |
| `Loc` | `growth.next/adult/req_age/req_molt/req_health/req_affection/met/unmet` |
| `HakoSelfTest.TestHealthAndGrowthCheck` | 8시간 +4 · 배고픔 50 이하면 회복 없음 · 넉넉한 5시간만 +2.5 · 주버나일 건강 부족 → 미충족·말풍선 문구·성장 안 함 → 건강 50에서 성장 · 서브어덜트 조건에 애정도 · 어덜트는 조건 없음 |

- "잘 돌봄" 시간 건너뛰기(8시간 구간, 목마름 100 → 60)에서도 매 구간 +4 → 기존 저장의 하코는 `+7일 (잘 돌봄)` 한 번이면 건강 50을 넘어 서브어덜트로 자란다

### 하코 중복 조사 — 코드 원인 없음

저장 파일(`LocalLow/TNBSoft/Hako`)과 백업 모두 게코 2마리, 둘 다 이름 `하코` · 크레스티드 · 주버나일. 두 번째는 애정도 0 · 건강 0 (돌본 적 없음), 생성 시각이 첫 번째보다 27.2시간 뒤(시간 건너뛰기로 옮겨진 값이라 실제 간격은 알 수 없음).

| 경로 | 결과 |
|------|------|
| `PlayerRepository.EnsureStarterGecko` (지금) | `geckos.Count > 0`이면 만들지 않음 — 자가 검사로 확인 중 |
| 커밋 이력 c7f75c3 ~ cf8695e의 `SaveManager.CreateNewPlayerData` · `AppBootstrap.EnsureDefaultGecko` | 새 데이터에만 넣거나 `Count > 0`이면 return — 이미 게코가 있는 목록에 더하는 경로 없음 |
| `StoreManager.BuyGecko` (분양) | 이름을 비우면 종 이름(`크레스티드 게코` / 예전 `Crested Gecko`). **이름 칸에 직접 "하코"를 적었을 때만** 같은 이름이 된다 |
| 자가 검사 | 저장 파일 이름이 `player_data_selftest` — 진짜 저장을 건드리지 않음 (이력 전체 동일) |
| 씬 · 프리팹 | "하코"가 적힌 입력 칸 기본값 없음 (한글 이스케이프 형태까지 검색) |
| 에디터 로그 | 남아 있는 두 세션(`Editor.log` · `Editor-prev.log`)에는 게코 생성 기록 없음 — 그 전에 생긴 것 |

→ 코드로 두 번째 기본 게코가 생기는 경로는 찾지 못했다. 가장 가능성 높은 것은 게코 목록에서 분양할 때 이름을 "하코"로 적은 경우. 재현되면 `[StoreManager] 게코 분양 완료` 로그로 구분 가능

### 저장 파일 정리 (사용자 결정 — 백업 없이)

- Unity를 닫은 상태에서 `player_data.json`·`player_data.bak`의 두 번째 하코(`cbe8622a…`, 애정도 0 · 건강 0)만 글자 단위로 지움. 두 파일 모두 하코(`e1523016…`) 1마리 · 선택 게코 그대로 확인

### 조사 중 발견한 버그 — 게코 이름이 번역됨 (수정)

- `SceneTextLocalizer`가 씬이 열릴 때 **실행 중에 넣은 게코 이름도 번역표 원문과 같으면 바꿨다** — 영어 설정에서 "하코" → "Hako", 한국어에서 "Hako" → "하코", 이름을 "먹이"로 지으면 영어에서 "Feed". 홈 이름 글자는 `OnEnable`에서 채운 뒤 `sceneLoaded`에서 번역되므로 순서로는 피할 수 없음
- 수정: `SceneTextLocalizer.Ignore(TMP_Text)` — 제외 목록(`HashSet`, 파괴된 글자는 번역할 때마다 정리)에 든 글자는 건너뜀. `HomeUIController.OnEnable`(`_geckoNameText`) · `GeckoSlotUI.Awake`(`_nameText`)가 글자를 채우기 전에 등록
- 자가 검사 `TestLocalization`에 추가: 영어에서 제외한 "하코"는 그대로, 고정 글자 "먹이"는 "Feed"

### 검증 (Unity 없이)

- Unity 컴파일러로 오프라인 컴파일 — 런타임 55개 · 에디터 9개, **오류 0 · 경고 0**
- 번역표 109개 — 중복 키 0 · 빈 문구 0 · `{0}` 자리 수 다름 0 · 글꼴 범위 밖 글자 0
- **로직 자가 검사 · 말풍선 모습은 Unity에서 확인 필요**

---

## 2026-09-15 — 게코 메뉴를 플레이 중에 막음

사용자가 플레이 중에 `Hako > Gecko > ① 프록시 게코 만들기`를 실수로 누름.
확인 — 그림 26장·스킨 3개를 디스크에 다시 쓴 뒤 `EditorSceneManager.MarkSceneDirty`에서 `InvalidOperationException: This cannot be used during play mode`로 중단. 씬 변경은 플레이 중 변경이라 멈추면 되돌아가고 `MainHome.unity`는 수정되지 않음(수정 시각 그대로). 다시 쓴 그림·스킨 58개는 같은 규칙으로 그려 커밋본과 똑같음(`git status` 변경 없음) → 피해 없음.

- `GeckoRigBuilder` — ①②③ 메뉴에 `NotPlaying`(`!EditorApplication.isPlayingOrWillChangePlaymode`) 조건. ①은 검사 함수를 새로 붙이고 ②③은 기존 검사에 추가 → 플레이 중에는 회색
- Unity 컴파일러로 오프라인 컴파일 — 런타임 55개 · 에디터 9개, **오류 0 · 경고 0**. 메뉴가 회색인지는 Unity에서 확인 필요

---

## 2026-09-15 — 첫 실행 부화 연출

사용자 질문: "처음 게임 시작 시 게코가 알부터 깨고 나오는 것이냐?" → 없었다 (해츨링이 바로 홈에 나타남, 알은 성장 단계 아이콘에만). 선택지 ① 첫 실행 부화 연출 · ② 알 상태로 시작 · ③ 아이콘 교체 중 **①**, 탭 3번 + 8초 자동 부화, 첫 게임만 (분양 게코 제외) — 사용자 결정.

**흐름:** 알이 톡 나타나 기우뚱 → "톡톡 두드려 주세요" → 화면을 두드릴 때마다 금 한 줄 + 흔들림 + 진동 (2번째부터 껍질 부스러기) → 3번째에 크게 흔들리다 빛 고리 + 껍질 조각 + 반짝이, 차임·성공 진동 → 하코가 0.3배에서 튀어나오며 `Surprise` → `Happy_LookUp` + 하트 + "안녕! 반가워!" → 결과 알림 "하코가 태어났어요!" → 알림이 사라지면 일일 보상 팝업

| 파일 | 변경 |
|------|------|
| `UI/Fx/HatchIntro.cs` (신규) | 실행 중 생성. 화면 전체 투명 버튼(어디를 눌러도 두드림, 다른 버튼 막음) + 하위 캔버스 그림 층(그림자·알·금 3줄·파티클·말풍선). 알 아래 끝을 `GeckoObject` 발밑(피벗)에 매 프레임 맞추고 피벗을 알 아래 끝에 둬 오뚝이처럼 흔들림. 게코는 `CanvasGroup` 알파 0으로 숨기고(동작 계산은 계속) 이동 AI를 끔. 수치 `[TBD]`: 탭 3번 · 자동 8초(이후 0.7초 간격) · 알 상자 460 |
| `HomeUIController` | `OnEnable`에서 `NeedsHatchIntro`면 보상 자동 팝업 보류 · 이동 끔 · 게코 숨김(첫 프레임 깜빡임 방지). `Start`에서 연출 시작(말풍선·하트용 `GeckoFx`가 필요). `OnHatched` → `CompleteHatchIntro` · 결과 알림 · 알림이 사라진 뒤 보상 팝업. `CanPresent`는 부화 중 false. 알 그림이 없으면 연출 없이 기록만 |
| `GeckoManager` | `NeedsHatchIntro()` (기록 없음 + 게코 있음) · `CompleteHatchIntro()` (기록 후 바로 저장 — 도중에 꺼지면 다음 실행에 다시) |
| `ProgressData` · `PlayerData` · `SaveManager` | `hatchIntroSeen` 추가, **저장 버전 4**. v3 이하 + 게코 있음 → 본 것으로 (지금 쓰던 저장은 연출 없음). `progress`가 null이면 새로 |
| `FxSprites.Crack` | 지그재그 금 (꺾인 선 거리 함수, 가운데 굵게). `Resources/Fx/crack`으로 교체 가능 |
| `Loc` | `hatch.hint` · `hatch.hello` · `hatch.born` |
| `HakoSelfTest.TestHatchIntro` | 새 게임은 연출 필요 · 완료 후 다시 안 나옴 · 기록이 저장 파일에 남음 · 게코 있는 v3 저장은 건너뜀 |

- **알 그림:** 해츨링 아이콘(`gecko_stage0.png` — 꼬리가 삐져나온 금갈색 알)을 `_growthStageSprites[0]`에서 받는다. `Resources/Fx/egg`가 있으면 그쪽 우선. 그림 상자 안에서 알이 차지하는 비율(`EGG_BOTTOM` 0.21 · `EGG_CENTER_Y` 0.5 · `EGG_TOP` 0.78)은 지금 그림을 보고 잰 값 — 그림을 바꾸면 함께 조정
- 새 그림·소리 파일 없음 (알은 기존 아이콘, 소리는 `Tap`·`Pop`·`Chime`·`Heart` 합성음)
- 참고: 첫 실행에는 알림 권한 요청(Android 13+)도 뜰 수 있어 연출 위에 시스템 창이 겹칠 수 있다 — 권한 요청 시점은 결정 필요 항목에 이미 있음

### 검증 (Unity 없이)

- Unity 컴파일러로 오프라인 컴파일 — 런타임 56개(새 `HatchIntro` 포함) · 에디터 9개, **오류 0 · 경고 0**
- 번역표 112개 — 중복 키 0 · 빈 문구 0 · `{0}` 자리 수 다름 0 · 글꼴 범위 밖 글자 0 · 씬 원문 겹침 0
- **연출 모습(알 위치·금 위치·박자)과 자가 검사는 Unity에서 확인 필요**
- 사용자 플레이 확인: "알은 잘 적용된 것 같다" (저장 파일을 지우고 새 게임으로 확인)

---

## 2026-09-15 — 테스트용 재화 메뉴

사용자 요청: 테스트용으로 코인을 얻을 수 있는 기능. 계획 보고 후 에디터 메뉴 방식으로 진행 (휴대폰 테스트 빌드용 숨은 버튼은 보류).

| 파일 | 변경 |
|------|------|
| `Editor/HakoCurrency.cs` (신규) | `Hako > 검사 > 재화` — 코인 +1,000 · 코인 +10,000 · (구분선) · 젬 +100. 플레이 중에만 활성 |
| `GameManager.DebugAddCurrency(coin, gem)` | `#if UNITY_EDITOR` — 더하고 바로 저장. 빌드에 들어가지 않음 |

- 홈 윗줄 코인·젬(`CountView`)은 매 프레임 데이터를 읽어 바로 카운트업. 상점·꾸미기 화면의 재화 글자는 구매 이벤트 때만 갱신되므로 나갔다 들어오면 반영
- Unity 컴파일러로 오프라인 컴파일 — 런타임 56개 · 에디터 10개(새 `HakoCurrency` 포함), **오류 0 · 경고 0**. 메뉴 동작은 Unity에서 확인 필요

---

## 2026-09-17 — 성장 속도 A안 (어덜트까지 약 2주)

사용자 지적: "게임이 너무 재미가 없다. 성장 속도를 더 빨리". 예전 조건은 어덜트까지 약 4개월이고, 첫 허물(2.5일) 뒤로는 허물이 21일마다라 **시작 2주 동안 허물 한 번 외에 변화가 없었다**. A(약 2주) · B(약 1주) · C(약 1개월) 중 사용자 결정 **A**.

| 항목 | 예전 | A안 |
|------|------|------|
| 첫 허물 | 60시간 (1.67/h) | **12시간** (100/12 ≈ 8.33/h) |
| 이후 허물 | 21일 (0.20/h) | **3일** (100/72 ≈ 1.39/h) |
| 베이비 | 15일 | **1일** |
| 주버나일 | 30일 + 허물 1 | **3일** + 허물 1 |
| 서브어덜트 | 60일 + 허물 3 + 건강 50 | **7일** + 허물 2 + 건강 50 |
| 어덜트 | 120일 + 허물 5 + 애정도 60 | **14일** + 허물 3 + 애정도 60 |

- 허물 시점 0.5 · 3.5 · 6.5 · 9.5일 → 각 단계 날짜에 허물 횟수가 딱 맞게 채워진다 (날짜와 허물 속도를 함께 바꿔야 함 — CLAUDE.md에 명시)
- 먹이 가속(최대 30%)은 그대로 → 잘 먹이면 어덜트 약 10일
- 오프라인 상한 48시간 동안 허물이 3일 주기의 약 2/3 진행 — 이틀 비워도 흐름이 크게 끊기지 않음
- 자가 검사 기대값 변경: 첫 허물 6시간 50% · 두 번째부터 48시간 66.7% · 1.2일 → 베이비 · 3.5일이어도 허물 0이면 주버나일 안 됨 · 실제 19시간 + 성장치 2 → 베이비 · 실제 12시간은 성장치를 쌓아도 안 됨 · `EffectiveAgeDays(0.7, ∞) = 1` · 말풍선 "나이 7일 - 충족"
- 주석 예시·PROGRESS 확인 항목도 새 수치로
- **기존 저장:** 이틀 전 새로 시작한 하코는 다음 실행 때 첫 허물 + 베이비 사건이 바로 연출된다
- Unity 컴파일러로 오프라인 컴파일 — 런타임 56개 · 에디터 10개, **오류 0 · 경고 0**. 자가 검사·플레이는 Unity에서 확인 필요

---

## 2026-09-17 — 어덜트 이후: 헛표시 정리 · 달성 보상 · 새 친구 권유

사용자 질문: "어덜트가 되면 어떻게 되는가?" → 확인 결과 성장이 멈출 뿐 아무 일도 없었고, 먹이의 "성장 +N"이 헛표시, 성장촉진제(5젬)가 쓸모없이 소모, 성장치가 계속 쌓임, 자연사 판정은 어덜트에서 일어나지 않음(`EvaluateGrowth`가 단계 4면 바로 return). 추천 ①②③ 진행 (④ 알 낳기 · ⑤ 도감은 보류).
계획 단계에서 확인: 분양 가격이 크레스티드·레오파드 무료, **가고일만 500코인** → 할인 기능 대신 보상을 500코인으로 맞춤.

| 파일 | 변경 |
|------|------|
| `GeckoManager` | `ADULT_STAGE` 4 · `ADULT_REWARD_COIN` 500 · `ADULT_REWARD_GEM` 5 `[TBD]` · `IsAdult` · `IsUselessFood` (어덜트 + 성장치 외 효과 전혀 없음). `FeedGecko` — 쓸모없는 먹이는 Refused(재고 유지), 어덜트는 성장치 0 (`FeedEffect.growthExp`도 0). `TryMolt` — 어덜트는 허물 보너스 성장치 없음. `EvaluateGrowth` — 어덜트가 되는 순간 코인·젬 지급 + `adultCount++` (단계가 되돌아가지 않아 게코마다 한 번) |
| `ProgressData` | `adultCount` (새 필드, 예전 저장은 0 — 변환 불필요) |
| `HomeUIController` | 거절 말풍선 "다 자라서 필요 없어요"/"배불러요" 구분. 어덜트 사건 = 성장 연출 + 하트 + 0.6초 뒤 반짝이, 알림 `event.adult`, 알림 뒤 `SuggestNewFriend` — "새 친구도 키워 볼까요?"(선택된 게코일 때) + 게코 탭 3번 통통. 선반 칸에 `grown`·`useless` 전달 |
| `FoodTray` | `Option.grown` → 효과에서 성장 제외, `Option.useless` → "필요 없음" + 아이콘 45% 흐리게 (누르면 거절 반응). `DescribeItem(..., includeGrowth)` |
| `GeckoSlotUI` | 어덜트는 "어덜트 - 다 자람" (글꼴에 가운뎃점이 없어 하이픈) |
| `Loc` | `food.useless` · `line.grown` · `line.new_friend` · `event.adult` · `geckolist.grown` |
| `HakoSelfTest.TestAdultStage` | 보상 지급·기록 · 한 번만 · 어덜트 조건 없음 · 촉진제 거절·재고 유지 · 칼슘은 먹음 · 성장치·말풍선 성장 0 · 자라는 중이면 촉진제 유효 |

- 보상 코인·젬은 성장 판정 순간(부팅 중일 수도) 데이터에 더해지므로, 홈 윗줄 숫자는 연출보다 먼저 오를 수 있다
- 이미 어덜트인 저장에는 소급 지급하지 않음 (테스트 데이터)
- 번역표 117개 — 중복 0 · 빈 문구 0 · 자리 수 다름 0 · 글꼴 범위 밖 0 · 원문 겹침 0
- Unity 컴파일러로 오프라인 컴파일 — 런타임 56개 · 에디터 10개, **오류 0 · 경고 0**. 자가 검사·플레이는 Unity에서 확인 필요
- 커밋 `ac19e71` (로컬만 — Unity 확인 후 푸시)

---

## 2026-09-17 — 알림 권한 시점 · 레오파드 유료 · 오늘의 돌봄 목표 · 게코 직접 만지기

"다음 개선 방향" 추천 순서 진행. 사용자 결정(모두 추천안): 알림 권한 = 부화가 끝난 뒤 · 레오파드 = 300코인 · 돌봄 목표 = 계획대로 · 만지기 = 부위별 반응.

| 파일 | 변경 |
|------|------|
| `AppBootstrap` | 알림 권한 요청을 `!NeedsHatchIntro()`일 때만 (새 게임은 부화 뒤). `_gecko.OnCareDone += reward.RecordCare` 연결 |
| `HomeUIController` | `OpenRewardAfterResult` — "태어났어요" 알림이 끝나면 알림 권한 → 일일 보상 팝업. `OnGoalProgress` — 목표 하나 완료 알림 / 전부 완료 알림 + 보상 탭 통통. `PulseTab` 공용(새 친구 권유와 같이 씀). `EnsureGeckoTouch` · `OnGeckoTouched` — 머리 = `OnPetClicked`, 몸통 = `TriggerSurprise` + "앗!", 꼬리 = `TriggerAnnoyed` + `Fx.Annoyed` + "꼬리는 안 돼!" (몸통·꼬리는 동작 중이면 무시) |
| `Resources/Species/leopard.asset` | `isUnlockedByDefault` 1 → 0 (가격 300 그대로). 파일이 CRLF라 첫 스크립트가 일치 0개로 멈췄고, 줄바꿈을 살려 한 줄만 바꿈 |
| `Models/DailyGoalData.cs` (신규) | `CareKind` (Feed·Water·Pet·Clean) + `DailyGoalData` (UTC 날짜 번호 · 4개 횟수 · 받음) |
| `PlayerData` | `dailyGoal` (새 필드 — null이어도 `RewardManager`가 새로 만듦, 저장 버전 그대로) |
| `GeckoManager` | `OnCareDone` 이벤트 — 먹이·물·쓰다듬기·청소가 **Done일 때만** |
| `RewardManager` | `GoalTarget` (2·2·3·1 [TBD]) · `GOAL_REWARD_COIN` 100 [TBD] · `GoalCount` · `GoalsComplete` · `GoalsClaimed` · `CanClaimGoals` · `RecordCare`(목표까지만 세고 저장 + `OnGoalProgress`) · `ClaimGoals`(하루 한 번). 날짜는 일일 보상과 같은 UTC 기준 |
| `UI/DailyGoalCard.cs` (신규) | 보상 팝업 카드(세로 0.28~0.72) 아래 0.05~0.26 자리에 실행 중 생성 — 보상 카드·받기 버튼의 그림·색을 복사. 제목 + 2×2 목표(채우면 연두) + 받기 버튼 ("모두 채우면 코인 +100" 흐림 / "받기 코인 +100" / "오늘은 받았어요"). 글자 자동 크기 |
| `RewardPanelUI` | `EnsureGoalCard` — 팝업이 열릴 때 카드 생성·갱신 |
| `UI/Gecko/GeckoTouch.cs` (신규) | 투명 터치 영역을 GeckoArea 안 게코 바로 다음 형제로 — **게코 오브젝트는 하위 캔버스라 그 안 그림은 루트 캔버스 터치 판정에 안 잡힌다**. `GeckoRig.GetExtents`로 폭, 높이 = 폭 × 0.55 [TBD], 매 프레임 따라감. 누른 점과 머리·몸통·꼬리 그림 가운데 거리로 부위 결정 |
| `GeckoAnimatorController` | `TriggerSurprise` |
| `GameManager.DebugSkipTime` | 하루 넘게 건너뛰면 `dailyGoal.day`를 그만큼 뒤로 → 목표 새로 시작 |
| `Loc` | `line.poke` · `line.tail` · `goal.*` 10개 |
| `HakoSelfTest.TestDailyGoals` | 빈 목표 · 거절은 안 셈 · 목표 수까지만 · 전부 채우면 받기 가능 · +100 · 하루 한 번 · 다음 날 새로 |

- PROGRESS 결정 필요 표에서 레오파드 가격 · 알림 권한 줄 삭제 (결정됨)
- 게코 탭·보상 탭 강조, 목표 알림은 기존 결과 알림 패널을 씀 — 성장·허물 사건 연출은 알림이 사라질 때까지 기다린다 (`CanPresent`)
- 번역표 129개 — 중복 0 · 빈 문구 0 · 자리 수 다름 0 · 글꼴 범위 밖 0 · 원문 겹침 0
- Unity 컴파일러로 오프라인 컴파일 — 런타임 59개(새 3개 포함) · 에디터 10개, **오류 0 · 경고 0**. 카드 배치·터치 판정·자가 검사는 Unity에서 확인 필요 (새 파일 `.meta` 3개 생성됨 → 함께 커밋)

---

## 2026-09-17 — 꼬리를 만지면 도망

사용자 요청: "꼬리를 만지면 도망가도록". 계획 보고 후 진행 (꼬리 튕기기 → 도망으로 교체).

| 파일 | 변경 |
|------|------|
| `GeckoMovementAI` | `Flee(touchWorld)` · `IsFleeing` — 돌아다니기 코루틴을 멈추고 `FleeRoutine`: 누른 곳 반대쪽으로 `fleeDistance` 300~400, 화면 안 범위(`GetWalkableX`)로 자르고 80 미만밖에 못 가면 반대쪽, 발 높이 ±40 비껴감 → `WalkTo(target, fleeSpeedScale 2.5, mayPause: false)` → 반대로 돌아서서 `fleeLookBack` 1~2초 → 돌아다니기 재시작. `WalkTo`에 속도 배율(최고 속도·가속·감속 모두)과 중간 멈춤 여부 인자 추가 (평소 호출은 그대로). 걸음은 `AddTravel`이 이동 거리에 맞추므로 빨리 가면 발도 빨라진다. `PickTarget`의 범위 계산을 `GetWalkableX`로 분리 |
| `GeckoTouch` | 콜백에 누른 곳 월드 좌표를 함께 넘김 |
| `HomeUIController.OnGeckoTouched` | 꼬리 = `Flee` + `Fx.Annoyed`('흥' 소리·머리 위 김) + "꼬리는 안 돼!" (말풍선은 게코를 따라감). 달아나는 중에는 머리·몸통·꼬리 모두 무시 |

- 도망 수치 3개는 Inspector에서 조절 (`[TBD]`) — 씬에 저장된 값이 없어 코드 기본값을 쓴다
- 김은 계획의 "꼬리 쪽"이 아니라 기존 삐짐 연출을 재사용해 머리 위에 나온다
- Unity 컴파일러로 오프라인 컴파일 — 런타임 59개 · 에디터 10개, **오류 0 · 경고 0**. 도망 모습은 Unity에서 확인 필요

---

## 2026-09-17 — 원근·앞뒤 이동·벽 타기 · 부위별 반응 확장 · 배치 모드 자가 검사

사용자 지적: "왜 좌우로만 움직이냐? 원근감을 살리고 뒤쪽으로도 가고 위로도 기어올라가도록. 부위별(앞다리·뒷다리·꼬리 뒤쪽·눈·입) 반응을 늘리고 여러 가지 반응이 나오게. 오류가 있는지 정확하게 확인". 계획 보고 후 진행.
원인 — **씬에 저장된 `groundBand`가 400~520 (폭 120)** 이라 코드 기본값과 상관없이 거의 좌우로만 다녔고, `farScale` 0.86이라 원근도 약했다. 바닥 그림(`TerrariumFloor`, 높이 150)은 돌봄 버튼·내비 바 뒤에 가려져 있고 게코는 정글 배경 위를 걷는다.

### 이동 (`GeckoMovementAI` 다시 씀)

| 항목 | 내용 |
|------|------|
| 앞뒤 | `groundBand` 380~950 (씬 값도 수정), 목표를 앞뒤·대각선으로도 고름. `DepthScaleAt` — 가까운 쪽 1 → 먼 쪽 `farScale` 0.62 (씬 값도 수정), 속도도 원근을 따라 느려짐 |
| 벽 타기 | 쉬고 난 뒤 `climbChance` 30%로 `Climb`: 머리가 위로 오게 게코 오브젝트를 발밑 기준 ±90° 회전(`climbTurnTime` 0.45초) → `climbHeight` 400~800 오름(평소 속도 × 0.75) → `climbHang` 2~4초 매달림(가끔 혀 날름) → 머리를 아래로 180° 돌려(× 1.4) 출발 높이까지 내려옴 → 눕힘. 한계 `ClimbTopY` = 영역 높이 − `climbTopMargin` 420 − 몸 길이(머리·꼬리 중 긴 쪽, 머리를 아래로 돌리면 꼬리가 위로 가므로) |
| 원근 고정 | 벽을 타는 동안 원근 크기는 출발한 바닥 높이(`_groundY`) 기준 — 높이 올라가도 작아지지 않음 |
| 도망 | 바닥: 기존 + 앞뒤 비껴감 `fleeDepthShift` −150~+250. 벽: `ClimbEscape` — 위로 `fleeClimbRise` 200~320 달아날 자리가 있으면 올라가 뒤돌아본 뒤 내려오고, 꼭대기면 후다닥(× 2.5) 내려옴 |
| 공용 이동 | `Travel(target, speedScale, pauseAt)` — 바닥·벽 공용 가속·감속·동작 중 대기·걸음 맞춤. `WalkTo`는 돌아서기 + `Travel` (위아래로만 가면 방향 유지) |
| 꺼질 때 | 벽에 붙은 채 꺼지면(부화 연출 등) 출발 높이·각도 0으로 되돌리고 모터 벽 자세 해제 |

`GeckoMotor.SetClimbing` — `_wClimb`(0.33초에 걸쳐)만큼 바닥 그림자를 숨기고 다리를 앞뒤로 벌리고 꼬리를 늘어뜨린다. 걷기 흔들림·발 맞춤은 회전된 채 그대로 동작.

### 부위별 반응

| 파일 | 변경 |
|------|------|
| `GeckoTouch` 다시 씀 | 부위 8곳 `Head · Eye · Mouth · FrontLeg · BackLeg · Body · TailBase · TailTip`. **파츠의 실제 사각형 안인지**(`GeckoRig.TryPartLocal` — 회전·좌우 반전·크기 반영)로 판정, 순서 눈 → 입 → 앞다리 → 뒷다리 → 머리 → 꼬리 → 몸통. 꼬리는 그림 안 가로 위치 0.55 이상이면 뿌리(지금 그림은 관절 피벗이 오른쪽 0.91). 어느 그림에도 안 걸리면 가장 가까운 머리·몸통·꼬리 끝. 터치 영역은 게코 회전을 따라 함께 돈다 |
| 판정 여유 | 프록시 그림 크기(눈 140 · 입 200×120 · 머리 500×440 · 몸통 720×380 · 꼬리 640×300 · 앞다리 150×220 · 뒷다리 180×220)와 스킨 관절 값으로 계산 — **처음 계획한 눈 여유 45%는 입 가운데(높이 360)가 눈 영역(347~613)에 들어가** 20%로, 입 35% → 25%, 꼬리 20% → 5%(20%면 몸통 뒤쪽 1/3이 꼬리) |
| `GeckoRig.TryPartLocal` | 월드 좌표 → 파츠 그림 안 uv (안 그려지는·크기 0 파츠는 false) |
| `GeckoMotor` | 새 동작 5개 `Yawn` 1.6초 · `Wave` 1.4초 · `PawShake` 1.2초 · `Kick` 1.0초 · `Shiver` 1.0초 (곡선 시작·끝 0). 인스펙터 미리보기 버튼 추가 |
| `HomeUIController.OnGeckoTouched` | 머리 = 쓰다듬기 · 연타(3초 5번) = 꼬리 튕기기 + 삐짐 + 달아남 · 벽에 매달림 = 깜짝 + `Flee` · 꼬리 끝 = 도망 · 졸림 60% 하품 · 화남 70% 꼬리 튕기기 · 그 외 부위별 두 가지 중 무작위 (입은 배고픔 60 미만이면 60%로 "배고파요!" + 먹이 버튼 통통). `React` 공용(동작 + 말풍선 + 소리 + 진동, 핥기·점프 소리는 `GeckoFx`가 내므로 생략). 반응 문구 키 목록 `TouchLineKeys` |
| `GeckoAnimatorController` | `TriggerAction(GeckoAction)` |
| `Loc` | 반응 문구 14개 (한국어·영어, 각 두 가지) |

### 오류 확인

- `HakoSelfTest.RunBatch` 추가 (메뉴 실행은 `Run` → 대화 상자, 배치는 로그 + 종료 코드). 검사 추가 `TestTouchAndMovement`: 원근 3점·범위 밖, 벽 타기 한계, 판정 순서, 꼬리 뿌리·끝, 반응 문구 번역, 새 동작 길이, **실제 프록시 스킨으로 부위 판정 × 4자세(오른쪽 0° · 왼쪽 0° · 오른쪽 90° · 왼쪽 −90°)** — 눈·입·앞다리·뒷다리·꼬리 끝·몸통·머리
- Unity 컴파일러로 오프라인 컴파일 — 런타임 59개 · 에디터 10개, **오류 0 · 경고 0**
- **Unity 배치 모드 실행** (`-batchmode -nographics -executeMethod HakoSelfTest.RunBatch`, 13초): 실제 스크립트 재컴파일 확인, 우리 스크립트 경고·오류 0, **자가 검사 139개 모두 통과**, 새 파일 `.meta` 3개 생성. 첫 실행 명령은 이전 로그를 지우는 줄이 보안 검사(경로 공백)에 막혀 실행 전에 중단 → 로그 파일 이름을 시각으로 바꿔 재실행
- 번역표 143개 — 중복 0 · 빈 문구 0 · 자리 수 다름 0 · 글꼴 범위 밖 0 · 원문 겹침 0, 코드가 쓰는 `line.*` 27개 모두 있음
- 코드 검토: 도망·부화로 벽 타기가 끊겨도 각도·높이 복구, 세운 몸의 옆 폭(약 300)이 좌우 한계 여유(약 380) 안, 위쪽 한계가 상태 띠 아래, 터치 영역은 HUD 버튼보다 뒤 형제라 버튼을 가로채지 않음
- **움직임·반응의 자연스러움은 Unity 화면에서 확인 필요** (PROGRESS 확인 항목)
- 커밋 `bf40fc5` (푸시 완료)

---

## 2026-09-17 — 꾸미기 구조물: 은신처 · 코르크 뒤판 · 덩굴 · 나뭇가지

사용자 요청: "배경 말고 게코가 쉴 수 있는 집이나 타고 올라갈 수 있는 구조물". 확인 결과 — 장식(바위·화분·하이드 하우스)은 그림뿐이었고 **씬의 장식 칸 4개(`DecorSlot0~3`)가 모두 화면 한가운데 같은 자리(200×200)에 겹쳐** 여러 개를 놓아도 한곳에 포개졌다. 사용자 결정: 구조물 4종 전부 · 임시 그림 먼저 · 1·2단계 이어서.

### 칸 · 데이터

| 파일 | 변경 |
|------|------|
| `DecorItemSO` | `DecorPlacement placement` (Floor / Wall) · `DecorUse use` (None / Hide / ClimbPanel / Branch / Vine) · `baseline` (그림 아래 투명 여백 비율) |
| `Domain/TerrariumLayout.cs` (신규) | 칸 0·1 바닥(±300, 600) · 칸 2·3 뒷벽(±290, 760). 쓰임별 그림 크기(집 460 · 뒤판 380×1000 · 덩굴 220×1000 · 가지 480×600 · 그 외 300), `ImagePlacement`(피벗·오른쪽 칸 가지 좌우 반전), `ClimbPath`(뒤판·덩굴 = 밑동+30에서 위 끝−160까지 곧게 · 가지 = `BRANCH_LINE` 3점, 발은 굵기 절반 위), `HideDoor`. 같은 파일에 `DecorCatalog` (Resources/Decor 전체 조회) |
| `TerrariumManager` | `Fits` · `FindEmptySlot(item)` · **`NormalizeSlots`** — 앱 시작(`AppBootstrap`)에 맞지 않는 칸의 장식을 맞는 빈 칸으로 옮기고, 자리가 없으면 빼고 코인·젬 환불. 칸 배열 길이 보정 |
| `TerrariumUIController` | 씬 목록 + `DecorCatalog` 합쳐 가격순(씬 수정 없이 새 장식 표시), 종류별 빈 칸, "바닥/벽 자리가 가득 찼어요". 쓰지 않게 된 `FindEmptyDecorSlot` 삭제 |
| `DecorSlotUI` | 아이콘 `preserveAspect` (세로로 긴 뒤판·덩굴) |
| `HomeUIController` | `RefreshTerrarium` — 칸 자리·크기·피벗 적용, 구조물 목록을 이동 AI에 전달 (씬 목록이 비어도 동작). 바닥 장식만 터치(`Button`), 벽 구조물은 터치 안 받음. `OnGeckoHideChanged` — 들어가면 집 그림을 게코·터치 영역 바로 앞 순서로, 나오면 원래 순서로. `OnDecorTouched` — 숨은 게코 `ComeOut` + "누구야?". `CallOutOfHide` — 먹이·물·쓰다듬기·청소 전에. `OnGeckoPerched` — 50% "여기 좋다~" |

### 게코 행동 (`GeckoMovementAI` 다시 씀)

- 쉬고 난 뒤 구조물이 있으면 `structureChance` 45%로 무작위 구조물: **은신처** `GoHide` — 문 앞(가운데 쪽 300) → 천천히 안으로 → `HideChanged(true)` → 5~12초(졸리면 12~25초, 엎드림) → 돌아서면 `HideChanged(false)` → 문 앞으로. 졸린 기분이면 은신처가 있을 때 60%로 들어가 잔다
- **벽 구조물** `ClimbRoute(path)` — 밑동까지 걸어간 뒤 **경로 따라가기** `Segment`: 구간마다 돌아서고(`Face`) `SegmentAngle`로 기울인 뒤(20° 넘으면 벽 자세) 이동. 꼭대기에서 매달리거나(뒤판·덩굴) **엎드려 쉰다**(가지, `perchRest` 3~6초) → `Descend`: 올라온 점을 거꾸로 되짚고 눕는다. 도는 시간은 도는 각도에 비례
- 뒷벽 구조물이 없을 때만 빈 유리벽 `FreeClimb` (같은 경로 방식, 한 구간)
- 도망: 집 → 빠르게 나옴 · 빈 벽 → 위로 달아날 자리가 있으면 위로 · 구조물 → 빠르게 내려옴 · 바닥 → 기존
- 꺼질 때: 벽이면 밑동, 집이면 문 앞으로 옮기고 각도·자세 복구
- `GeckoMotor.SetResting` — 고개 숙임 · 배 낮춤 · 꼬리 늘어뜨림 · 눈 감음 · 숨 1.5배 느리게 · 자동 동작 쉼
- 다 자란 게코(원근 적용 폭 약 540)는 집(460)보다 커서 완전히 숨지 않는다 → 꼬리가 삐죽 나오게 설계 (사용자에게 미리 알림)

### 임시 그림 (`Editor/DecorProxyArt.cs`, 배치 실행 전용)

- 거리 함수로 그림: 코르크 뒤판(울퉁불퉁한 둥근 판 + 코르크 알갱이) · 덩굴(구불구불한 줄기 + 좌우 번갈아 잎 10장) · 나뭇가지(`BRANCH_LINE`을 따라 가늘어지는 가지 + 잔가지 + 잎 3장)
- 장식 에셋 3개 생성(가격 60 · 40 · 80 `[TBD]`), 기존 은신처(Hide, 여백 0.22) · 바위(0.20) · 화분(0.08)에 값 채움
- 첫 생성에서 나뭇가지 오른쪽 끝 잎이 그림 가장자리에서 잘려 잎 위치를 안쪽으로 옮기고, 이번에 만든 PNG를 지운 뒤 다시 생성 (같은 guid로 복원되어 연결 유지)
- 새 그림은 Unity 6가 새 방식의 스프라이트 번호(`fileID` 긴 숫자)를 쓴다 — 기존 `21300000`과 달라도 정상

### 검증

- Unity 컴파일러로 오프라인 컴파일 — 런타임 60개 · 에디터 11개(새 `TerrariumLayout` · `DecorProxyArt` 포함), **오류 0 · 경고 0**
- 번역표 150개 — 중복 0 · 빈 문구 0 · 자리 수 다름 0 · 글꼴 범위 밖 0 · 원문 겹침 0, 코드가 쓰는 `line·terrarium·decor·goal` 키 56개 모두 있음
- **Unity 배치 실행**: `DecorProxyArt.GenerateBatch` (그림 3장 · 에셋 3개) → `HakoSelfTest.RunBatch` **156개 모두 통과** — 새 검사 `TestTerrariumStructures`: 칸 종류·좌우 배치, 경로 방향 6가지, 가지 경로(밑동 범위·대각선·가로·좌우 대칭), 뒤판 직선, 오를 높이 없음, 빈 칸 찾기, 예전 저장 정리(옮기기 · 환불 +50 · 손상 배열·모르는 id), 실제 장식 에셋 6개(놓는 곳·쓰임·그림·이름 번역)
- 배치 명령에서 파일 삭제와 Unity 실행(경로에 `C:\Program Files`)을 한 줄에 넣으면 보안 검사가 막는다 → 두 명령으로 나눠 실행
- 코드 검토: 가지 경로 각도(좌우 칸·오르내림), 숨었을 때 꼬리·집·돌봄 버튼, 가지 위에서 도망 시 쉬는 자세 해제
- **배치·모습(집 여백, 가지 위 발 위치, 숨는 모습)은 Unity 화면에서 확인 필요**

---

## 2026-09-17 — 장식 옮기기 (범위 제한 드래그)

사용자 요청: "장식을 내가 원하는 위치에 놓도록". CLAUDE.md 규칙 "꾸미기 자유 드래그 배치는 MVP 절대 금지"를 알리고 선택지(A 범위 제한 드래그 · B 자리 후보 고르기 · C 완전 자유) 제시 → **A** 결정, 규칙을 "칸 수 고정 + 범위 안 드래그 허용"으로 완화.

| 파일 | 변경 |
|------|------|
| `TerrariumData` | `decorPositions` (Vector2[4], (0,0) = 기본 자리) — 예전 저장은 앱 시작 `NormalizeSlots`의 배열 길이 보정으로 채워짐 |
| `TerrariumLayout` | 칸 번호 대신 **기준점(anchor)** 으로 계산: `DefaultAnchor` · `AnchorOf(data, slot)` · `ClampAnchor`(바닥: 가로 = 화면 − 그림 절반×원근 − 10, 높이 420~740 / 벽: 가로만, 가지는 밑동 ±430, 높이 760 고정) · `TooClose`(바닥 거리 220 · 벽 가로 200) · `BranchRisesRight`(왼쪽·가운데면 오른쪽 위로) · `ImagePlacement(item, anchor)` · `ClimbPath(use, anchor, …)` · `HideDoor(anchor)`. 나뭇가지 그림은 밑동 발 위치가 anchor.x에 오게 (기본 자리 기준 10 이동) |
| `TerrariumManager` | `SetDecorPosition(slot, anchor)` (빈 칸 무시, 높이 맞춤, (0,0) 회피) · `SetDecor`는 그 칸 위치를 기본 자리로 · `NormalizeSlots`도 옮긴 칸 위치 초기화 · `EnsureSlots`가 위치 배열 길이도 맞춤. 파일을 새로 씀 |
| `UI/DecorDragHandle.cs` (신규) | 장식 그림의 누르기 입력 — 0.5초 길게 누르기(24px 넘게 움직이면 취소) · `CanDrag`일 때만 끌기, **길게 눌러 편집이 켜진 뒤 손을 떼지 않고 끌어도 이어짐** · 꺼질 때 끌던 것은 끝내기 알림 |
| `HomeUIController` | 모든 장식 그림에 입력 연결(`EnsureDecorInput`, 바닥 장식은 짧게 누르기도). `PlaceDecorImage(image, item, anchor)` — 바닥 장식은 `GeckoMovementAI.DepthScaleFor`로 원근 크기. **편집 모드** `EnterDecorEdit`: 이동 AI 끄기(벽·집에서 바닥으로) · 게코 터치 영역 끄기(가린 장식도 잡게) · 장식들 바로 뒤에 반투명 판(누르면 `ExitDecorEdit`) · `Outline` 노란 테두리 · 안내. 끌기: 시작 위치 + 손가락 이동량(GeckoArea 로컬) → `ClampAnchor` → 다른 장식과 `TooClose`면 멈춤 → 그림만 옮김, 손을 떼면 `SetDecorPosition` 저장. `ExitDecorEdit`: 판·테두리 끄고 게코 다시 움직이고 구조물 목록 새로. 꺼질 때는 상태만 정리 (비활성 오브젝트에서 연출 코루틴을 시작하지 않게). 편집 중 집 누르기 무시 |
| `GeckoMovementAI` | `Structure.anchor` — 은신처·경로를 옮긴 위치로 계산, 은신처 안 위치 `_hideAnchor` 기억(꺼질 때 문 앞으로), `DepthScaleFor`. 파일을 새로 씀 |
| `Loc` | `terrarium.edit_hint` · `terrarium.edit_saved` |
| `HakoSelfTest` | 기존 칸 검사를 기준점 방식으로 고치고 추가: 기본 자리 간격, 가지 좌우 반전·벽 높이, 가지 밑동 = 놓인 위치, 옮긴 뒤판 경로, 옮긴 위치 저장·높이 맞춤·빈 칸 무시·저장 파일·장식 교체 시 초기화, 범위(바닥 화면 안·바닥 안·뒤로 가면 더 붙음·뒷벽 앞 / 벽 가로만 / 가지 밑동), 간격 판정 |

- 알고 있는 한계: 평소(편집 전)에는 게코 터치 영역이 장식보다 위라 게코와 겹친 부분의 장식은 길게 누를 수 없다 · 편집 안내가 결과 알림 패널(화면 아래쪽 700)에 2.5초 뜨는 동안 그 자리는 눌리지 않는다

### 검증

- Unity 컴파일러로 오프라인 컴파일 — 런타임 61개(새 `DecorDragHandle`) · 에디터 11개, **오류 0 · 경고 0**
- 번역표 152개 — 중복 0 · 빈 문구 0 · 자리 수 다름 0 · 글꼴 범위 밖 0 · 원문 겹침 0, 코드가 쓰는 키 58개 모두 있음
- **Unity 배치 실행 `HakoSelfTest.RunBatch` — 168개 모두 통과** (옮긴 위치 관련 12개 포함), 새 파일 `.meta` 생성
- **끌기 손맛·범위·편집 모드 모습은 Unity 화면에서 확인 필요** (마우스로 길게 누른 뒤 끌기)

---

## 2026-09-17 — 여러 마리 키우기 정리: 목록 상태 표시 · 알림 대상 · 분양 규칙

사용자 질문 "게임 중에 게코를 또 분양받으면 어떻게 되는가" → 문제 3가지(안 보이는 게코 방치 · 알림이 선택 게코만 봄 · 무료 크레스티드 반복 분양으로 어덜트 보상 무한) 보고 → 1·2·3 진행 결정.

| 파일 | 변경 |
|------|------|
| `GeckoManager` | `enum GeckoAlert` + `AlertOf(g)` (건강 ≤20 → 배고픔 ≤30 → 목마름 ≤30 → 청결 ≤20 순, `ALERT_*` [TBD]) · `MostUrgent(geckos, threshold, out hours)` · `ThirstFirst(g, threshold)` (예전 알림은 현재 수치만 비교해 감소 속도 차이를 무시했다) · `AdultReward(progress, speciesId, out coin, out gem)` — 종마다 첫 어덜트 500/5, 같은 종 두 번째부터 `ADULT_REWARD_REPEAT_COIN` 100 [TBD] · `LastAdultRewardCoin/Gem`을 `OnGrowthUp` 직전에 채움 |
| `StoreManager` | `MAX_GECKOS = 5` [TBD] · `CanAdoptMore` · `IsFreeFor` — `isUnlockedByDefault` 종이라도 **그 종을 키우고 있으면 유료** (하코가 크레스티드라 보통 크레스티드도 300) · `BuyGecko`가 둘 다 확인 |
| `GeckoEventQueue` | `GeckoEvent.rewardCoin/rewardGem` — 어덜트 사건에 실제 보상을 복사 |
| `HomeUIController` | 어덜트 알림이 실제 보상을 보여줌 (젬 0이면 `event.adult_coin`) |
| `NotificationScheduler` | 돌봄 알림 = 모든 게코 중 `MostUrgent`의 이름·시각 (보상 알림 이름은 선택 게코 그대로) |
| `GeckoSlotUI` | `Setup(gecko, atHome, onSelect)` — 오른쪽에 상태 글자(색: 빨강·주황·노랑·초록)와 "홈에 있어요"를 실행 중에 만든다 (프리팹 변경 없음) |
| `GeckoListUIController` | 선택 게코 표시 · 드롭다운 "(무료)"를 `IsFreeFor`로, 분양 뒤 다시 만듦 · 5마리면 분양 패널 대신 안내 |
| `ProgressData` · `PlayerData` · `SaveManager` | `adultSpeciesIds` · 저장 버전 5 — 예전 저장의 어덜트 종은 받은 것으로 기록 |
| `Loc` | `event.adult_coin` · `geckolist.full/here/status_*` (160개) |
| `HakoSelfTest` | `TestMultipleGeckos` 19개 — 무료 조건, 유료 두 번째 크레스티드, 5마리 제한(코인 유지), 종별 보상·사건 금액·다른 종 첫 보상, v4 이전 저장, 상태 판정 우선순위·문구, 알림 대상·문구 |

### 검증

- 오프라인 컴파일 — 런타임 61개 · 에디터 11개, **오류 0 · 경고 0**
- **Unity 배치 실행 `HakoSelfTest.RunBatch` — 187개 모두 통과**
- 게코 목록 슬롯 오른쪽 글자 위치·색·가독성은 Unity 화면에서 확인 필요

### 후속 — 게코 슬롯 프리팹 수정 (사용자 화면: 흰 카드에 "Button"만 보이고 이름이 안 보임)

- 원인: `GeckoSlot.prefab` 자식 순서가 이름 → 단계 → **버튼(슬롯 전체 크기 흰 Image)** 이라 버튼이 글자를 덮었고, 글자도 흰색이었다. "Button"은 버튼 기본 글자
- `GeckoSlot.prefab` 직접 수정: `SelectButton`을 맨 앞(카드 배경)으로 · 카드 색 (0.16, 0.18, 0.24) · 기본 글자 비움 · 이름 (24, 16) 420×40 · 단계 (24, −20) 연회색 · 세 글자 모두 `RaycastTarget` 끔 (버튼 누르기를 막지 않게)
- `GeckoSlotUI`: 오른쪽 글자 여백 24, "홈에 있어요" 연회색, 홈 게코 카드에 초록 `Outline`
- `UIPressScale.SetTarget(transform)` 추가 — 버튼이 배경이라 버튼만 줄어들면 글자가 그대로여서, 슬롯 루트 전체가 눌리게
- 오프라인 컴파일 오류 0 · 배치 자가 검사 187개 통과 · 프리팹 가져오기 정상

---

## 2026-09-17 — 어덜트 이후 1단계: 어덜트의 선물 · 어덜트 전용 장식

사용자 요청 "어덜트 이후 즐길 거리" → 7가지 제안(선물 · 도감 · 업적 · 유대 레벨 · 전용 장식 · 모프 · 번식) → 추천 순서(1단계 선물+전용 장식 → 2단계 도감+업적 → 나중에 유대·모프·번식) 승인.

| 파일 | 변경 |
|------|------|
| `GeckoData` | `giftDay` |
| `RewardManager` | `GIFT_*` [TBD] · `TodayNumber()` · `CanGift(g)` · `ClaimGift(id, rng, out Gift)` (코인 20~40, 20% 먹이, 날짜 기록·저장) |
| `GameManager.DebugSkipTime` | 하루 넘게 건너뛰면 `giftDay`도 과거로 |
| `DecorItemSO` | `requiredAdults` |
| `TerrariumManager` | `AdultsRaised` · `IsUnlocked` · `NewlyUnlocked(items, before, after)` |
| `GeckoManager` · `GeckoEventQueue` | `LastAdultsRaised` → `GeckoEvent.adultsRaised` |
| `ProgressData`/`PlayerData`/`SaveManager` | 저장 버전 6 — `adultCount`를 지금 어덜트 수 이상으로 |
| `FxSprites` | `Gift` 모양 (리본 틈 상자 + 나비매듭), `RoundBox` |
| `HomeUIController` | `RefreshGift`(Refresh마다) · `EnsureGift`(GeckoArea 맨 위, 반짝이+상자 버튼) · `GiftSpot`(게코·바닥 장식과 200 떨어진 곳 12번 시도) · `OnGiftClicked`/`ShowGift` · Update에서 반짝이 회전·상자 통통 · Start에서 선물을 터치 영역 위로 · 어덜트 사건 끝에 `AnnounceUnlockedDecor` |
| `GeckoSlotUI` | 급한 일이 없고 선물이 있으면 "선물이 있어요" (금색) |
| `DecorSlotUI` · `TerrariumUIController` | 잠긴 장식: 흐린 아이콘 · "어덜트 N마리" · 누르면 안내(결제 전) |
| `DecorProxyArt` | 이끼 바위 300×300 · 동굴 460×460(어두운 입구) · 큰 유목 480×600(나뭇가지 선, 바랜 색, 잎 없음) + 에셋, `Upsert`에 놓는 곳·여백·조건 인자. 나뭇가지 그리기를 `DrawBranchStyle`로 나눔 |
| `Loc` | 선물 4 · 잠금 3 · 장식 이름 3 (170개) |
| `HakoSelfTest` | `TestAdultGiftAndUnlocks` 18개 — 선물 조건·하루 1회·다음 날·코인 범위·먹이 비율(고정 난수 300번)·먹이 에셋·문구, 잠금 판정·저장 기준·새로 열린 장식, 사건의 어덜트 수, v5 저장 보정, 새 장식 에셋 3개 |

- 동굴은 계획의 "더 큰 은신처" 대신 집과 같은 크기(460) — 은신처 크기가 `TerrariumLayout.ImageSize(use)` 하나라서. 크게 하려면 장식별 크기 필드가 필요

### 검증

- 오프라인 컴파일 오류 0 · 경고 0
- `DecorProxyArt.GenerateBatch` — 새 그림 3장·에셋 3개 생성 (그림 직접 확인)
- **Unity 배치 실행 `HakoSelfTest.RunBatch` — 205개 모두 통과**
- 선물 상자 모습·위치·누르기, 잠긴 장식 표시는 Unity 화면에서 확인 필요

### 후속 — 꾸미기 오류 문구가 세로로 나옴 · 장식 칸 4 → 7 · 앞뒤 겹침 순서

사용자 화면: 꾸미기에서 "바닥 자리가 가득 찼어요"가 한 글자씩 세로로 나오고, 장식이 조금밖에 안 들어감.

- **오류 문구:** `Terrarium.unity`의 `ErrorPanel`에 VerticalLayoutGroup(자식 크기 조절 꺼짐)이 붙어 `ErrorText` 폭을 0으로 만들었다 → `TerrariumUIController.FitErrorText`가 화면을 열 때 레이아웃을 끄고 글자를 패널 안(여백 25·15)에 꽉 채움, 줄바꿈·자동 크기(22~34). 씬 파일은 그대로 (Unity가 켜져 있었음). 상점·게코 목록 오류 패널에는 이 레이아웃이 없다
- **칸 수 (사용자 결정 — 바닥 4 · 뒷벽 3):** `TerrariumLayout.SlotCount` 7, `PLACEMENTS` 표(0·1·4·5 바닥 / 2·3·6 뒷벽 — 예전 번호 유지), `CountOf`, 새 기본 자리 (−90, 450) · (90, 730) · 뒷벽 (0, 760). `TerrariumData` 배열 기본 길이 = `SlotCount`, 예전 4칸 저장은 `NormalizeSlots` → `EnsureSlots`가 늘린다
- `HomeUIController.EnsureDecorImages` — 씬의 `DecorSlot0~3` 중 같은 종류 칸을 복제해 4~6번 그림을 만든다 (입력·테두리가 붙기 전에)
- **앞뒤 겹침 (사용자 결정 — 같이):** `UpdateDepthOrder`(LateUpdate) — 뒷벽 → (바닥 장식 + 게코를 발 높이 큰 순서), 게코 다음에 터치 영역, 숨은 은신처는 게코 바로 앞. 순서가 다를 때만 `SetSiblingIndex`. 예전 `_decorBaseSibling`(은신처 순서 기억)은 이 규칙으로 대체. 편집 판은 `DepthGroupFirstIndex` 앞
- `HakoSelfTest`: 칸 종류·개수(4/3), 같은 종류 기본 자리 간격 전부, 모든 기본 자리가 옮길 수 있는 범위 안(집 크기 기준), 4칸 저장 → 벽 칸의 집이 새 바닥 칸으로, 바닥 4칸이 가득 차면 환불
- 오프라인 컴파일 오류 0 · 경고 0. **Unity가 켜져 있어 배치 자가 검사는 못 돌림** — 메뉴 `Hako > 검사 > 로직 자가 검사`로 확인 필요 (→ 아래 2단계 배치 실행에서 함께 통과)

---

## 2026-09-17 — 어덜트 이후 2단계: 게코 도감 · 업적

| 파일 | 변경 |
|------|------|
| `Domain/RewardManager.Collection.cs` (신규) | `AchievementStat` · `AchievementDef` · `SpeciesCatalog`, `RewardManager`(partial) 도감: `HasMet` · `HasRaisedAdult` · `RecordMet(data, species, reward)` · `BookAdults` · `BookComplete` · `CanClaimBook` · `ClaimBook` / 업적: `ACHIEVEMENTS` 8개 · `StatValue` · `AchievementProgress` · `IsAchieved` · `IsClaimed` · `ClaimAchievement` · `ClaimableCount` · `TakeNewlyAchieved`(실행 중 한 번) · `CountCareForAchievements` |
| `RewardManager` | `partial` · `RecordCare`가 먼저 쓰다듬기·먹이를 센다 (목표를 넘겨도, 그때는 저장만) · `ClaimGoals`가 `goalDays++` |
| `ProgressData` | `petCount` · `feedCount` · `goalDays` · `bookRewardClaimed` (`unlockedSpeciesIds` · `achievements`는 예전부터 있던 빈 자리를 사용) |
| `PlayerData` · `SaveManager` | 저장 버전 7 — 게코 종 · 어덜트 종을 만남으로 (보상 없이), 목록 null 보정 |
| `PlayerRepository.EnsureStarterGecko` | 기본 게코 종을 만남으로 (보상 없이) |
| `StoreManager` | `LastMeetCoin` — 분양 때 새 종이면 코인 +50 |
| `UI/CollectionPanel.cs` (신규) | 실행 중 생성 창 — 제목·닫기·탭 · 스크롤 목록(RectMask2D + VerticalLayoutGroup + ContentSizeFitter) · 종 줄(그림/그림자 · 이름/??? · 도장 2개) · 완성 줄 · 업적 줄(이름 · 설명+진행 · 받기/보상/받았어요). 받으면 소리·진동 + 목록 다시 + 콜백 |
| `GeckoListUIController` | `EnsureBookButton`(분양 버튼 복제, 목록 110 내림) · `RefreshBookBadge` · `OpenBook`(받을 업적 있으면 업적 탭) · 분양 뒤 배지·새 종 알림 · `ShowMessage(message, color)` — 오류 패널을 초록 알림에도 사용, 맨 위로 |
| `HomeUIController` | 사건이 없을 때 `AnnounceAchievements` — 결과 알림 + 게코 탭 통통 |
| `Loc` | 도감 11 · 업적 5 · 업적 이름 8 · 설명 6 (200개) |
| `HakoSelfTest` | `TestBookAndAchievements` 23개 — 종 순서, 기본 게코 만남, 새 종 보상·재분양 없음, 도감 완성 전후·한 번만, 쓰다듬기(삐짐 제외)·먹이 세기, 게코 수·허물 합 업적, 달성 전 수령 불가, 새 달성 한 번만 알림, 수령·중복·없는 id, 돌봄 보상 → 꾸준한 돌봄, 저장 파일, v6 저장 보정, 문구 |

- 첫 배치 실행에서 `TestLocalization`이 **"글꼴에 없는 글자: ·"** 로 실패 → 버튼 문구를 "도감 / 업적"으로. CLAUDE.md 금지 기호 목록에 가운뎃점 추가

### 검증

- 오프라인 컴파일 — 런타임 63개(새 파일 2) · 에디터 11개, **오류 0 · 경고 0**
- **Unity 배치 실행 `HakoSelfTest.RunBatch` — 228개 모두 통과** (앞의 칸 7개·겹침 순서 검사 포함), 새 스크립트 `.meta` 생성
- 창 배치·글자 크기·스크롤·버튼 위치는 Unity 화면에서 확인 필요

---

## 2026-09-17 — 유대 레벨 (어덜트 이후 4번)

사용자 결정: 점수 = 애정도 + 넘친 몫 · Lv.1~5 전부 · 수치 계획대로 (20/50/100/180/300, 하루 넘친 몫 30).

| 파일 | 변경 |
|------|------|
| `Domain/GeckoBond.cs` (신규) | `BondPerk` · `LEVEL_POINTS` · `LEVEL_REWARDS` · `DAILY_OVERFLOW_CAP` · `Points` · `LevelOf` · `Level` · `NextPoints` · `Has` · `PerkOf` · `PetLimit`(4/6) · `RoomToday` · `TodayFull` · `AddAffection`(100까지 애정도, 넘는 몫은 하루 한도 안에서) |
| `GeckoData` | `bondOverflow` · `bondDay` · `bondToday` · `bondRewardedLevel` |
| `GeckoManager` | 애정도 4곳 → `AddAffection` (+ `CheckBondLevel`: 그 사이 보상 합산, `LastBond*`, `OnBondLevelUp`) · 쓰다듬기 한도를 게코별로 (`PET_FATIGUE_LIMIT` 상수 제거) |
| `GeckoEventQueue` · `GeckoAnimatorController` | `BondUp` 사건 (`bondLevel`, 보상) · 기뻐하기 |
| `GeckoParts` · `GeckoMotor` · `GeckoInspectors` | 동작 `Spin` (1.2초, 몸통 한 바퀴 — 다 돈 순간 0°) + 미리보기 |
| `GeckoMovementAI` | `CallTo`/`ComeTo`(집·벽에서 나와 1.6배로 와서 `Arrived`) · `Hold`/`Release`/`IsHeld`(들린 동안 원근 고정, 도망·나오기 무시) · `GroundTop` |
| `GeckoTouch` | 길게 누르기 0.6초(24px 넘게 움직이면 취소) → `LongPressed`, 뒤따르는 클릭 무시 |
| `UI/FloorTapCatcher.cs` (신규) | 투명 판 — 0.4초·80px 안의 두 번째 누르기 → `DoubleTapped` |
| `FxSprites` | `Hand` (손바닥·손가락 4·엄지·손목) · `HAND_PALM_TOP` · `Capsule` |
| `HomeUIController` | 유대 구역: `RefreshBondLabel`(성장 단계 글자 끝 오른쪽, 분홍, 누르면 `DescribeBond`) · `BondUpMessage` · `TryGreet`(사건 대기열 앞) · `EnsureFloorCatcher`(장식·게코 무리 앞 순서, 높이 = 바닥 + 80) · `OnFloorDoubleTapped` · `OnGeckoArrived` · `OnGeckoLongPressed` → `PalmRide`(손 올라옴 → 폴짝 → 함께 140 들어 올림 → 3초 → 내려놓음) · 쓰다듬기에 재롱 25%·하트 한 번 더 · `BondUp` 연출(하트·반짝이·말풍선·라벨 튕김) · 겹침 순서에 손 (게코 바로 뒤) · 편집 모드에서 바닥 판 끄기, 손바닥 중 장식 길게 누르기 무시 |
| `GeckoSlotUI` | 단계 글자 뒤 "유대 N" |
| `RewardManager.Collection` | `AchievementStat.BondLevel` · 업적 "단짝" (유대 Lv.5, 젬 10) |
| `PlayerData` · `SaveManager` | 저장 버전 8 — 지금 레벨을 보상 없이 받은 것으로 |
| `Loc` | 유대 24 · 업적 2 (226개). "쓰다듬기"는 돌봄 버튼 원문과 겹쳐 풀린 것 이름을 "쓰다듬기 좋아함"으로 |
| `HakoSelfTest` | `TestBond` 19개 — 레벨 경계·다음 점수·풀리는 것, 넘친 몫·하루 한도·다음 날, 쓰다듬기로 Lv.1 + 보상 + 사건, 여러 레벨 한꺼번 보상 · 한 번만, 한도 4/6 (실제 연타 6번), 단짝 업적, 말풍선, v7 저장, 문구, 동작 길이·손 그림 |

### 검증

- 오프라인 컴파일 — 런타임 65개(새 파일 2) · 에디터 11개, **오류 0 · 경고 0**
- **Unity 배치 실행 `HakoSelfTest.RunBatch` — 247개 모두 통과**, 새 스크립트 `.meta` 생성
- 라벨 위치, 인사·부르기·재롱·손바닥 연출의 모습과 손 그림은 Unity 화면에서 확인 필요

### 후속 — 테스트 메뉴 `Hako > 검사 > 유대`

- `Editor/HakoBond.cs` (신규): 다음 레벨까지 · 점수 +20 · 점수 +100 · 최고 레벨 (Lv.5) · 처음으로 (0점) — 플레이 중 · 선택 게코가 있을 때만
- `GameManager` (에디터 전용): `DebugAddBond(points)` — 애정도 먼저, 나머지는 넘친 몫 (하루 한도 무시) · `DebugBondNextLevel` · `DebugResetBond`(보상 받은 레벨도 0) → `CheckBondLevel`(보상·사건) · 저장 · 화면 갱신
- `GeckoManager.DebugNotifyChanged` (에디터 전용) — 값을 직접 바꾼 뒤 `OnStateChanged`
- 오프라인 컴파일 오류 0 · 배치 자가 검사 247개 통과 · `HakoBond.cs.meta` 생성

---

## 2026-09-17 — 모프(무늬) 수집 (어덜트 이후 6번)

사용자 결정: 어덜트가 될 때 공개 · 확률 70/25/5 · 종별 기본색 함께.

| 파일 | 변경 |
|------|------|
| `Domain/GeckoMorph.cs` (신규) | `MorphRarity` · `MorphPattern` · `MorphDef` · 표 12개 · `RARITY_WEIGHT` · `FIRST_REWARD` · `BaseColor` · `ForSpecies` · `Find` · `Roll`(종에 있는 등급만 가중치) · `Assign`(정하기 + 도감 + 처음 보상) · `Record` · `Has` · `LookOf` · `SwatchColor` · `SeedOf` |
| `GeckoData` · `ProgressData` · `PlayerData` · `SaveManager` | `morphId` · `morphIds` · 저장 버전 9 (예전 어덜트는 id 씨앗으로 정하고 기록, 보상 없음) |
| `GeckoManager` | 어덜트가 될 때 `LastMorph = Assign(...)` → `OnGrowthUp` 뒤 `OnMorphRevealed` |
| `GeckoEventQueue` | `MorphReveal` 사건 (`morphId` · `morphFirst` · 보상) · `HasPending(id, type)` |
| `GeckoRig` | `SetMorph(body, pattern, color, seed)` — 몸 파츠 7개 색 곱하기 · 몸통·머리 `Mask` + `MorphDot` 점(점 11/4 · 얼룩 4/2 · 띠 5/2) · 그림이 바뀌면 다시 · `MorphColorOf` · `PatternDotCount` |
| `GeckoAnimatorController` | `ApplyMorph`(0.5초 주기 동기화 — 어덜트 · 연출 끝이면 모프, 아니면 기본색) · `RevealMorph` · `PresentEvent`의 `MorphReveal` |
| `HomeUIController` | `MorphReveal` 연출(반짝이 두 번 · "멋지지?") · `MorphMessage` · `RarityKey` |
| `CollectionPanel` | 종 줄 260 — 그림·이름·도장을 위로, 아래에 `MorphLine`(칩 4개 · 등급 색 · 색 점 · "모프 N/4") |
| `GeckoSlotUI` | 어덜트 "어덜트 - 할리퀸" |
| `RewardManager.Collection` | `AchievementStat.Morphs` · 업적 "모프 수집가" (6종, 젬 10) |
| `GameManager` · `Editor/HakoMorph.cs` (신규) | 테스트 메뉴 `Hako > 검사 > 모프` — 다음 모프로 / 다시 뽑기 (`DebugChangeMorph`) |
| `Loc` | 모프 23 (249개) |
| `HakoSelfTest` | `TestMorph` 14개 — 표 구성·번역, 없는 종, 등급 확률(고정 난수 6000번), 어덜트 → 모프·도감·보상, 사건 순서·내용, 알림 문구, 두 번째 같은 모프 보상 없음, 기본색·연출 전후 모습, 씨앗 고정, 모프 수집가, v8 저장, **진짜 프록시 게코에 색·점 얹기/지우기**, 문구. 어덜트 보상을 정확히 보던 기존 검사 4곳은 모프 보상(무작위)을 더해 비교하도록, 어덜트 사건 뒤 모프 사건을 비우도록 수정 |

### 검증

- 오프라인 컴파일 — 런타임 66개 · 에디터 13개, **오류 0 · 경고 0**
- **Unity 배치 실행 `HakoSelfTest.RunBatch` — 261개 모두 통과**, 새 스크립트 `.meta` 생성
- 색·무늬가 실제로 어떻게 보이는지(Mask로 잘리는지, 밝은 모프 구분)는 Unity 화면에서 확인 필요

---

## 2026-09-18 — "그래픽이 유치하다" → 그림 주문서 + 화면 분위기 연출

사용자 지적: 그래픽이 유치하다. 확인해 보니 **배경·하이드 하우스·바위는 그려진 일러스트인데 게코 본체와 최근 장식 5종만 코드로 만든 임시 그림**이라 화풍이 섞여 있었다. 그림은 AI 생성으로 마련하기로 결정.

### ① 그림 주문서 (`ART_ORDER_GECKO.md` 신규)

- 화풍: 배경(`bg_jungle.png`)·하이드 하우스를 참고 그림으로, 영어 프롬프트 키워드와 negative
- 캔버스 규칙: **1600×900 · 투명 · 오른쪽 보기 · 발밑 중앙 (800, 60) · 꼬리 곧게 · 다리 분리**
- 절차: 전체 그림 1장 → 파츠 14장으로 자르기(**모두 같은 캔버스 크기로 내보내기** — `GeckoSkinImporter`의 ② 경로가 이때 관절을 자동으로 잡는다) → 표정 눈 9 · 입 8 → `Textures/Gecko/Final/` → 메뉴 `Hako > Gecko > ②③`
- 종별(레오파드·가고일) 프롬프트, 장식 6종(코르크·덩굴·가지·유목·이끼 바위·동굴) 크기·프롬프트 — 가지·유목은 `BRANCH_LINE` 좌표 명시
- 자주 나오는 문제(관절 틈·외곽선 굵기·흐릿함)와 체크리스트
- `ART_GUIDE.md`: "명암 1단계만" → **2단계 + 부드러운 그라데이션**으로 수정 (유치해 보이던 원인), 주문서 링크 추가

### ② 화면 분위기 연출 (코드만)

| 파일 | 변경 |
|------|------|
| `UI/Fx/TerrariumAtmosphere.cs` (신규) | 비네트(장식·게코 뒤 순서) · 먼지 14개(떠오르며 흔들리고 끝에서 옅어짐) · 앞 잎사귀 2장(천천히 기울어짐). 모두 raycast 꺼짐 |
| `FxSprites` | `Vignette`(가운데 투명 → 가장자리 검정, 알파만) · `Leaf`(두 원이 겹친 뾰족 잎) |
| `GeckoRig` | `DepthTint` — 파츠 색에 곱한다 (`_tint × _morphMul × DepthTint`) |
| `GeckoMovementAI` | `farTint`(0.90, 0.94, 1.00) · `DepthAt` · `DepthTintFor` · 매 프레임 리그에 넣기 |
| `HomeUIController` | `_atmosphere` 체크(끄면 셋 다 안 나옴) · `EnsureAtmosphere` · `HazeTint`로 바닥 장식도 같은 공기 원근 (뒷벽은 가장 뒤 값) |
| `GeckoMotor` | 발밑 그림자 `SHADOW_SPREAD 1.25` · `SHADOW_ALPHA 0.85` |
| `HakoSelfTest` | `TestAtmosphere` 3개 — 공기 원근 앞/뒤·범위 밖, 비네트·잎사귀 그림 |

### 검증

- 오프라인 컴파일 — 런타임 67개(새 파일 1) · 에디터 13개, **오류 0 · 경고 0**
- **Unity 배치 실행 `HakoSelfTest.RunBatch` — 264개 모두 통과**
- 비네트 농도·먼지 수·잎사귀 크기는 화면에서 보고 조정 필요 (`TerrariumAtmosphere` 상단 수치)

---

## 2026-09-18 — 그려진 게코 그림 1차 적용 (`GeckoArtCutter`)

사용자가 주문서대로 AI 그림 한 장(1376×768)을 만들어 `Textures/Gecko/Source/gecko_base.png`에 저장 → 자동 자르기 도구를 만들어 적용.

`Assets/Editor/Gecko/GeckoArtCutter.cs` (신규, 메뉴 `Hako > Gecko > ④` · 배치 `CutBatch`)

| 단계 | 내용 |
|------|------|
| 배경 제거 | 가장자리부터 흰색(밝기 214↑·색 기운 14 이하) 플러드 필, 경계의 흰 기운은 알파로 감쇠 — 826,350픽셀 |
| 파츠 자르기 | `RECTS`(원본 픽셀, 왼쪽 위 기준) 7개 → 1600×900 캔버스 같은 자리에 저장 → `GeckoSkinImporter`가 관절 자동 계산 |
| 얼굴 | **머리는 그려진 눈·입 그대로**, 기본 표정은 빈 그림. 다른 표정은 `Cover`(살빛 타원 + 그린 표정)로 덮는다 — 눈 4종·입 4종 |
| 그린 파츠 | 혀 두 마디·발밑 그림자 |
| 마무리 | 스프라이트 임포트 설정 → `GeckoSkin_Final` 생성 → MainHome 게코에 적용·씬 저장 |

- 만드는 과정에서 고친 것: ① 픽셀 배열 위아래 뒤집힘(Unity는 아래가 0) ② 옅은 회색 그림자가 배경으로 안 지워짐 ③ 눈·입을 지워 메우려다 외곽선을 따라 번져 머리 윤곽이 상함 → **지우지 않고 덮는 방식**으로 변경 ④ 표정 판이 그려진 눈을 덮도록 1.45배로 넓힘
- 결과 PNG 20장, 파츠 13/14 (`shed_patch` 없음 — 허물 준비 표시는 당분간 안 보인다)
- `ART_ORDER_GECKO.md`에 "3-1. 자동으로 자르기" 절 추가
- 오프라인 컴파일 오류 0 · **자가 검사 264개 통과**

---

## 2026-09-18 — 다리·발 모양 고침 (사각형 자르기 → 외곽선 따라 자르기)

사용자: "발 부분이 좀 이상하다" → 고친 뒤 "앞다리와 뒷다리 모양이 약간 이상하다". 원본을 좌표로 뜯어보고 파츠를 관절값대로 조립해 원인을 찾았다.

| 원인 | 고친 방법 |
|------|------|
| 다리 사각형이 겹쳐 **옆 다리 발가락**이 같이 담김 (발이 두 개, 걸을 때 따로 돌았다) | 다리마다 지울 구역(다각형) + 발 씨앗점 |
| 원본의 **발밑 회색 그림자**(밝기 212)가 안 지워져 발끝이 지저분하고 발밑 원점이 아래로 밀림 | 배경 기준 214→**190**, 색 기운 14→**20** |
| 다리 4개의 **관절이 같은 값**(0.45,0.10)이라 걸을 때 발이 미끄러짐 | 관절을 원본 좌표로: 허벅지 (560,392) · 겨드랑이 (838,432) · 먼 뒤 (660,470) · 먼 앞 (1000,445) |
| 몸통 오른쪽 끝(x 1000)과 머리 아래(y 375) 사이 **목·가슴에 구멍** | 몸통 사각형을 x 1165까지 늘리고 머리와 겹치는 턱·볼만 지움 |
| **네모난 자른 자리가 걸을 때 몸통 위로 드러남** (다리 ±18° 회전) | 몸통·다리를 `CUT_PARTS`로 **그림의 어두운 외곽선을 벽 삼아** 채워 윤곽대로 자름 (`TraceLimb`) |
| 다리가 흔들릴 때 몸통과 다리 사이 빈틈 | 몸통만 `keep` 22px 넓혀 다리 뒤를 메움 |

- `TraceLimb`: 씨앗 → 외곽선(밝기 108 미만) 벽으로 살 채우기 → 선을 2px씩 4번 넘어가며 다시 채워 **발가락 잇기** → 외곽선 두께 5px 넓히기. 발가락 하나하나 찍지 않아도 그려진 윤곽대로 잘린다
- 검증 방법 (Unity 없이): 파츠를 스킨의 관절값대로 조립해 ① 원본과 픽셀 비교 — **구멍 8,234px → 675px**(남은 것은 외곽 한두 줄) ② 다리를 ±18° 돌린 걷는 자세 그림으로 곧은 자른 자리 확인
- 오프라인 컴파일 오류 0 · **자가 검사 264개 통과** · MainHome 적용·씬 저장 완료
- 남은 한계: 어깨는 그림에 외곽선이 없어 겨드랑이에서 곧게 끊는다(팔을 크게 흔들면 옅은 이음새) · 먼 쪽 다리는 몸통에 가려질 위쪽을 넉넉히 담아 둔다

---

## 2026-09-18 — 벽 타기 연출 보강

사용자: "벽을 타고 올라가는 모습도 자연스럽게 만들어줘". 어색한 원인 4가지를 찾아 모두 고쳤다.

| 원인 | 고친 방법 |
|------|------|
| 화면 **가운데에서** 그대로 올라가 붙을 곳이 없어 보였다 | 가까운 쪽 좌우 유리벽으로 먼저 걸어가 오른다 (`ClimbWallIsLeft`·`WallClimbX`, `wallMargin` 120) + 발이 그 벽을 향하게 돌아서고 한 번 올려다본다 |
| 꼭대기에서 돌아설 때 +90°→−90°가 **0°를 지나** 벽에서 떨어져 한 바퀴 도는 것처럼 보였다 | `FlipOnWall` — 각도는 그대로 두고 **그림만 좌우 반전**해 머리를 아래로. `Segment`도 벽에서는 같은 규칙으로 방향을 고른다 |
| 매달린 자세가 바닥에 선 자세와 거의 같았다 | 다리 앞 22°/뒤 18° 벌림(먼 쪽 0.45·0.40배) · 발을 몸 쪽으로 8 당김 · 몸통 6% 납작 · 고개 +4° · 꼬리 늘어뜨림 |
| 오르는 리듬이 평지 걸음 그대로였다 | 걸음 각도 ×1.25 · 발 드는 높이 ×1.4 · 몸 좌우 흔들림(`StrideLength`도 같이 커져 발이 미끄러지지 않는다) · 매달리면 1.6~3.2초마다 대각선 두 발 바꿔 짚기 |

- 먼 쪽 다리 그림의 위쪽 여유를 줄였다 (`leg_back_far` y 440→456 · `leg_front_far` y 410→428) — 벽에서 다리를 벌리면 몸통 뒤 여유가 삐져나왔다
- 바닥에 내려서는 마지막 구간은 `climbLandSlow`(0.7)배로 천천히
- 수치는 모두 Inspector `[TBD]` (`GeckoMotor` 벽 타기 항목 · `GeckoMovementAI` 빈 유리벽 타기 항목) — 씬에 기본값으로 들어갔다
- 검증: 오프라인 컴파일 오류 0 · **자가 검사 266개 통과**(벽 쪽 x·발이 짚은 벽 유지 2개 추가) · 파츠를 조립한 벽 자세 그림으로 확인

---

## 2026-09-20 — 코드 점검에서 나온 버그 5건 수정 + 자가 검사 추가

최종 그림 적용(`07c659b`) 뒤 코드를 다시 훑어 찾은 문제를 고쳤다. 다섯 건 모두 **자가 검사로 잡히지 않던 것**이라 검사도 함께 넣었다.

| # | 파일 | 증상 → 수정 |
|---|------|------|
| 1 | `UI/Gecko/GeckoTouch.cs` | 최종 그림에서 **머리(쓰다듬기) 판정이 27%만 남음** — `eye_open`·`mouth_closed`가 다른 표정을 덮는 **빈 판**(눈 186×186 · 입 255×88, 불투명 픽셀 0개)인데 그 사각형 + 여유로 판정해, 눈 판이 머리(408×210)보다 세로로 커졌다 → `ORDER`를 (파츠, 부위, **판정 박스 중심·크기**)로 바꾸고 `TryPartLocal`은 uv만 받아 온다. 눈 0.30×0.45 · 입 1.00×1.10 · 다리 1.30 · 꼬리 1.10 [TBD] |
| 2 | `Editor/HakoSelfTest.cs` | 부위 판정·모프 검사가 **프록시 스킨**으로만 돌아 씬 그림(`GeckoSkin_Painted`)을 검증하지 못했다 → `SceneSkin()`(최종 → 없으면 프록시) |
| 3 | `UI/HomeUIController.cs` | 게코 2마리 이상이면 **30초마다 홈이 다른 게코로 바뀜** (이름·게이지·유대 레벨·선물 상자) — 시간 진행이 모든 게코에 `OnStateChanged`를 보내는데 `Refresh`가 걸러내지 않았다 → `IsHomeGecko(g, selectedGeckoId)` |
| 4 | `Domain/GeckoMovementAI.cs` | 벽에서 **내려오다 만지거나 부르면 꼭대기로 되올라감** — `Descend`가 `_route`를 지우지 않아 매번 맨 위부터 되짚었다 → `TrimRouteAbove`(지나친 위쪽 점 버리기) + 구간마다 `RemoveAt` |
| 5 | `Domain/GeckoManager.cs` | 오프라인 **건강 감소가 규칙보다 큼** — 구간 끝에 배고픔·목마름이 0이면 경과 시간 전체를 뺐다 (80에서 24시간 방치 시 −8이어야 할 것이 −24) → 0이 된 **뒤 시간만큼만**. 청결 20 이하 기분 패널티도 같은 방식 (`CLEAN_MOOD_THRESHOLD`·`CLEAN_MOOD_PENALTY`) |
| 6 | `UI/HomeUIController.cs` | 먹이 버튼이 **배를 채우는 먹이만** 세어, 영양제만 있으면 "먹이 없음"인데 선반은 열렸다 → 버튼도 선반과 같은 목록(`OwnedFoods(PlayerData, GeckoData)` static) · `GetFirstFoodItem` 삭제 |

### 추가한 자가 검사

- **터치:** 머리 위 가운데(0.5, 0.92) · 뒤통수(0.1, 0.5) → 머리 (예전 코드면 둘 다 "눈"으로 실패), 판정 박스 표 확인 (눈 < 그림 판, 머리·몸통 = 그림 그대로)
- **여러 마리:** `IsHomeGecko` 판정 + 실제로 2마리에 시간 진행을 돌려 **홈이 1번만 갱신**되는지
- **벽 내려오기:** `TrimRouteAbove` — 지나친 위쪽 점은 버리고 바닥 출발점은 남긴다
- **굶주림·더러움:** 0이 된 뒤 시간만큼만 건강 −1/h (24시간 중 8시간 → −8), 이미 0이면 전체, 청결 20 아래 뒤부터 기분 −0.5/h
- **먹이 버튼:** 영양제만 있어도 목록에 나온다 · 마지막으로 준 먹이가 맨 앞 · 어덜트에게 성장촉진제는 "필요 없음" · 데이터 없으면 빈 목록

### 검증 상태 (⚠ 남은 일)
> **2026-09-21 해결** — Unity가 설치되어 배치 검사를 돌렸다. 컴파일 에러 1건(`CS0165`)을 고친 뒤 **모두 통과 (287개)**, 플레이 확인도 마쳤다. 아래는 당시 기록이다.

- **이 작업에서는 컴파일·자가 검사를 돌리지 못했다** — 이 PC에서 `C:\Program Files\Unity\Hub\Editor` 아래에 `6000.2.8f1`이 보이지 않아(폴더 비어 있음) 배치 실행이 불가했다
- 수동 검토만 한 상태이므로 **Unity에서 `Hako > 검사 > 로직 자가 검사`를 먼저 돌릴 것**. 터치 박스 수치(특히 눈 0.30×0.45)는 화면에서 얼굴·몸·꼬리를 실제로 눌러 보며 조정

---

---

## 2026-09-21 — 검증 · 남은 결정 2건 · 하단 탭 아이콘 · 알림 패키지

이 PC에 Unity 6000.2.8f1이 설치되어, 전날 커밋(`a0460be`)이 하지 못한 **컴파일·배치 자가 검사**를 처음으로 돌렸다.

### 1. 컴파일이 막혀 있었다 (`2d67e66`)

`HakoSelfTest`의 판정 박스 검사에서 `TryBoxOf` 세 번을 `&&`로 이어 붙여, 뒤쪽 호출이 건너뛰어질 수 있으니
컴파일러가 `headBox`·`bodyBox`를 확정 할당으로 보지 않았다 (`error CS0165`). `Assembly-CSharp-Editor` 외 2개가
함께 실패해 **프로젝트 전체가 컴파일되지 않는 상태**였다 → 세 변수를 미리 선언. 이후 **모두 통과 (287개)**.

전날 고친 5건은 사용자가 플레이로 직접 확인했다 (머리 쓰다듬기 · 부위 8곳 · 2마리 30초 · 벽 내려오다 만지기 ·
영양제만 남긴 먹이 버튼 · `+24시간` 뒤 건강 96 = 규칙대로).

### 2. 남아 있던 결정 2건 (`PROGRESS.md` 4장)

| 항목 | 결정 | 반영 |
|------|------|------|
| 일일 보상 기준 시각 | **UTC 자정 유지** | 현지 자정으로 바꾸면 시간대를 돌려 하루치를 여러 번 받을 수 있다. 기준 대신 **안내**를 넣었다 — 보상 팝업 금액 아래 "매일 09:00에 새로 고침" (`RewardManager.LocalResetTimeText`가 기기 시간대로 계산, 번역 키 `reward.reset`) |
| 자연사 (900일) | **MVP 제외** | 어덜트는 원래 판정에서 빠져 있어 실제로 걸리는 건 어덜트에 못 간 게코뿐이라 벌처럼 느껴진다 → `EvaluateGrowth`의 판정과 `GROWTH_DAYS_NATURAL_DEATH` 삭제, 되살릴 자리는 성장 상수 옆 주석으로 |

### 3. 하단 탭 아이콘 5종 (`NavIconArt`)

이모지가 글꼴 아틀라스에 없어 □로 나오던 탓에 2026-09-14부터 **빈 칸**이던 자리다.
`Assets/Editor/NavIconArt.cs`가 96×96 **흰 실루엣** 5장을 코드로 그려 `Resources/Icons/tab_*.png`로 만든다
(탭 바가 어두운 반투명 0.1·0.13·0.1 @ 59%, 글자가 흰색이라). `HomeUIController.EnsureNavButtonIcons`가
실행 중에 탭의 빈 `Emoji` 칸(LayoutElement 38px) 안에 넣어, **씬은 건드리지 않는다**.
최종 그림은 같은 이름·같은 크기로 바꿔 끼우면 된다. 자가 검사가 5장 존재와 이름·순서 일치를 확인한다.

### 4. Mobile Notifications 설치

`manifest.json`에 `com.unity.mobile.notifications 2.4.3`. 지금까지 패키지가 없어 `NotificationScheduler`가
리플렉션 호출을 **조용히 전부 건너뛰고 있었다** — "배고파해요"·"오늘의 보상" 알림이 하나도 가지 않던 상태다.
실제로 알림이 오는지는 기기 내부 테스트에서 확인한다.

### 5. 문서 정리

`PROGRESS.md` 머리말·1장 표·2장 체크를 지금 상태로 맞추고(`5dcb1be`·`9975576`), 3장 맨 위에 **권장 순서(6판)** 를 넣었다 —
새 게임 첫 흐름 → 만지기·돌봄 → 꾸미기 → 여러 마리·도감 → 어덜트까지 → 영어·재시작.
## 버그 수정 이력

| 날짜 | 증상 | 원인 | 해결 |
|------|------|------|------|
| 2026-09-20 | 최종 그림에서 머리를 눌러도 쓰다듬기가 잘 안 됨 | 눈·입 표정 판(빈 그림)의 사각형으로 판정 — 눈 판이 머리보다 세로로 큼 | 파츠 안 판정 박스로 분리 (`GeckoTouch.ORDER`) |
| 2026-09-20 | 게코 2마리 이상이면 홈 화면이 30초마다 다른 게코로 바뀜 | `Refresh`가 선택 게코를 확인하지 않음 (시간 진행은 모든 게코에 알림) | `HomeUIController.IsHomeGecko` |
| 2026-09-20 | 벽에서 내려오다 놀라면 꼭대기로 되올라감 | `Descend`가 지나온 경로를 지우지 않음 | `TrimRouteAbove` + 구간마다 경로 소비 |
| 2026-09-20 | 이틀 비우면 건강이 규칙보다 훨씬 많이 깎임 | 구간 끝 상태로 판단해 경과 시간 전체를 뺌 | 0이 된 뒤 시간만큼만 |
| 2026-09-20 | 영양제만 있으면 먹이 버튼은 "먹이 없음"인데 선반은 열림 | 버튼만 `hungerRestore > 0`을 셈 | 버튼·선반이 같은 목록(`OwnedFoods`) |
| 2026-09-15 | Filled로 바꾼 뒤에도 게이지 5개·허물 진행 막대가 값과 상관없이 가득 차 보임 (건강 80도 100과 같은 길이) | Filled Image에 스프라이트가 없으면 uGUI가 `fillAmount`를 무시하고 사각형 전체를 그림 (`Image.OnPopulateMesh`의 `activeSprite == null` 분기) | 씬 채움 이미지 6곳에 기본 `UISprite` 지정 + `HomeUIController.MakeFillable`이 실행 시 흰 스프라이트·Filled 보정 |
| 2026-09-15 | 상태 게이지가 값과 상관없이 흰 네모로만 보임 | Fill Image가 Simple 타입 + 코드가 평소 색을 흰색으로 덮어씀 | Filled 가로 막대 + 상태별 색 + 숫자, 실행 시 타입 보정 |
| 2026-09-14 | 저장 도중 앱이 멈추면 다음 실행 때 데이터가 초기화될 수 있음 | 메인 파일이 없으면 임시·백업을 보지 않고 새 데이터 생성 | 메인 → 임시 → 백업 순서로 복원, 임시 파일 디스크 확정 |
| 2026-09-14 | 새로 시작하면 홈 배경·바닥이 꺼져 있음 | `backgroundId`·`floorId` 기본값 없음 | 기본값 + 예전 저장 파일 보정 |
| 2026-09-14 | 닫기·개인정보·하단 탭 아이콘·뒤로 버튼이 □로 표시 | 정적 글꼴 아틀라스에 없는 기호·이모지 | ASCII로 바꾸거나 비움 (탭 아이콘은 그림 필요) |
| 2026-09-14 | 일일 보상 "연속 N일"과 보상 금액이 어긋남 | 받기 전에 저장된 어제 일수를 표시 | 받기 전·후 같은 기준으로 표시 |
| 2026-09-14 | 분양 드롭다운에서 고른 것과 다른 종이 분양될 수 있음 | 빈 칸을 건너뛴 드롭다운 번호로 원래 배열을 조회 | 드롭다운과 같은 순서의 목록 사용 |
| 2026-09-12 | 장식 슬롯 4개가 차면 영영 바꿀 수 없음 | 빼는 UI 없음 | 놓은 장식을 다시 누르면 빼기 |
| 2026-09-12 | 패키지 이름이 비어 안드로이드 빌드 불가 | 초기 설정 그대로 | `com.tnbsoft.hako` |
| 2026-09-12 | 가로로 회전하면 UI가 깨짐 | 자동 회전 허용 | 세로 고정 |
| 2026-09-12 | 저장 파일이 비어 있으면 NullReference 예외 | 파싱 결과 null 미검사 | 명확한 메시지로 백업 복구 경로를 탄다 |
| 2026-03-23 | Type mismatch: Expected Canvas, found CanvasGroup | CanvasGroup에 `!u!223`(Canvas 타입 ID) 사용 | `!u!225`(CanvasGroup 타입 ID)로 수정 |
| 2026-09-11 | 백그라운드에 있던 시간이 상태값에 반영되지 않음 | 복귀(`OnApplicationPause(false)`) 처리 없음 + 다음 pause에서 기준 시각 덮어씀 | 복귀·진입 모두 `ApplyElapsedProgressAll` + 저장 |
| 2026-09-11 | 앱을 켜 둔 동안 상태값이 줄지 않음 | 실행 중 진행 처리 없음 | 30초 주기 진행 |
| 2026-09-11 | 허물·성장 연출과 결과 알림이 거의 안 보임 | 판정이 부팅 중(홈 없음)에 일어남 | `GeckoEventQueue` |
| 2026-09-11 | 설정에서 배경음을 끄면 효과음도 꺼짐 | `AudioListener.volume = 0` | `AudioManager` 배경음만 끔 |
| 2026-09-11 | 장식 슬롯이 가득 찼을 때 코인만 사라짐 | 결제 후 슬롯 확인 | 슬롯 먼저 확인 |
| 2026-09-11 | 산 배경·바닥을 다시 고르면 또 결제 | 보유 기록 없음 | `ownedDecorIds` |
| 2026-09-11 | 쓰다듬기 연타 제한이 1회 늦게 걸림 | 탭 사이 소수점 회복 (3.97 < 4) | 반 칸 여유 판정 |

---

## 진행 현황

> 현재 단계별 상태·남은 작업·플레이 확인 체크리스트는 **`PROGRESS.md`** 에서 관리한다 (2026-09-14 기준: STEP 1~6 코드·씬 조립 완료, Unity 실행 확인 전).
> 아래는 2026-04-09 당시 기록이다.

### 스크립트 완료 현황 (2026-04-09)
- `StoreManager`, `StoreUIController`, `ItemSlotUI` — 완료
- `GeckoListUIController`, `GeckoSlotUI` — 완료
- `TerrariumManager`, `TerrariumUIController`, `DecorSlotUI`, `DecorItemSO` — 완료
- `RewardManager`, `RewardPanelUI` — 완료
- `SettingsManager`, `SettingsPanelUI` — 완료
- `GameManager.SpendGem()` 추가 — 완료
- `DepthScaleConfig`, `DepthObject`, `GeckoMovementAI`, `TerrariumDepthManager` — 완료 (2026-04-09)
- **버그 수정**: TerrariumUIController 재화 직접 수정 → SpendCoin/SpendGem 위임
- **버그 수정**: HomeUIController.OnFeedClicked() null 체크 추가

### DecorItemSO 에셋 생성 완료 (`Assets/_Game/Resources/Decor/`)
| 에셋 | 카테고리 | 가격 |
|------|----------|------|
| bg_jungle | Background | Free |
| bg_desert | Background | 100C |
| floor_soil | Floor | Free |
| floor_bark | Floor | 80C |
| decor_rock | Decoration | Free |
| decor_plant | Decoration | 50C |
| decor_hide | Decoration | 50C |

---

## 설계 메모

- **오프라인 보정 상한**: 48h `[TBD]` — `TimeManager.ClampOfflineProgress()`
- **수치 미확정** `[TBD]`: `HUNGER_DECAY(4)`, `THIRST_DECAY(5)`, `MOOD_DECAY(1)`, `MOLT_PROGRESS_PER_HOUR(0.20)`
- ~~**자연사 처리**: `GROWTH_DAYS_NATURAL_DEATH = 900f` — STEP 6 예정~~ → **MVP 제외로 결정 (2026-09-21)**, 판정·상수 삭제
- ~~**람다 이벤트 해제 불가**~~: 해결됨 — `GeckoAnimatorController`는 이름 있는 메서드로 구독하고 OnDisable에서 해제한다 (성장·허물은 `GeckoEventQueue` 경유)
- ~~**먹이 선택 UI**: 2차 MVP — 수량이 남은 첫 먹이 자동 선택~~ → **`FoodTray` 선반으로 구현됨 (2026-09-15)**. 버튼 글자도 같은 목록을 쓴다 (2026-09-20)
- **씬 파일 직접 편집 방식**: Unity YAML 구조 파악 후 Python 스크립트로 오브젝트 append 및 m_Children 수정

