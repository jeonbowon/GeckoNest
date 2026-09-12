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
- [ ] `DepthScaleConfig.asset` 생성: `Resources/Configs/` 우클릭 → Create → Hako → DepthScaleConfig
- [x] ~~게코 GameObject에 `DepthObject.cs` + `GeckoMovementAI.cs` 추가~~ → **취소** (2026-09-11: 게코는 UI로 그리므로 `DepthObject` 금지. `GeckoMovementAI`는 `Hako > Gecko > ①`이 붙인다)
- [ ] 씬 빈 GameObject에 `TerrariumDepthManager.cs` 추가, `_config` 연결
- [ ] `HomeUIController` Inspector에 `_geckoMovement`, `_depthManager` 레퍼런스 연결

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

## 버그 수정 이력

| 날짜 | 증상 | 원인 | 해결 |
|------|------|------|------|
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

## 진행 현황 (2026-09-11)

> 2026-09-12: STEP 1~6의 코드·연출·출시 설정을 모두 마쳤다. 남은 것은 Unity에서의 실행 확인, 최종 아트·사운드 교체, 기기 테스트다 (위 2026-09-12 항목 참고).

| 단계 | 목표 | 상태 |
|------|------|:----:|
| STEP 1 | 기반 골격 | ✅ |
| STEP 2 | 홈 + 돌봄 루프 | ✅ |
| STEP 3 | 성장 + 허물 | ✅ |
| STEP 4 | 스토어 + 인벤토리 | 🔧 씬 조립 대기 |
| STEP 5 | 꾸미기 | 🔧 씬 조립 대기 + 깊이 시스템 추가됨 |
| STEP 6 | 운영 기능 | 🔧 씬 조립 대기 |

> 🔧 = 스크립트 완료, Unity Editor 씬/프리팹 연결 작업 남음

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
- **자연사 처리**: `GROWTH_DAYS_NATURAL_DEATH = 900f` — STEP 6 예정
- ~~**람다 이벤트 해제 불가**~~: 해결됨 — `GeckoAnimatorController`는 이름 있는 메서드로 구독하고 OnDisable에서 해제한다 (성장·허물은 `GeckoEventQueue` 경유)
- **먹이 선택 UI**: 2차 MVP — 현재 `ownedItemIds` 첫 번째 아이템 자동 선택
- **씬 파일 직접 편집 방식**: Unity YAML 구조 파악 후 Python 스크립트로 오브젝트 append 및 m_Children 수정
