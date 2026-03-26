# GeckoNest — 프로젝트 진행 상태

> 마지막 업데이트: 2026-03-26
> 엔진: Unity 6 (6000.2.8f1) · 플랫폼: Android 우선
> 상태: **스크립트 전체 완료 / Unity Editor 씬 조립 대기 중**

---

## 1. 전체 진행 현황

| STEP | 목표 | 스크립트 | 씬/프리팹 | 검증 |
|------|------|:--------:|:---------:|:----:|
| STEP 1 | 기반 골격 (저장, 오프라인 보정) | ✅ | ✅ | ✅ |
| STEP 2 | 홈 + 돌봄 루프 (먹이/물/쓰다듬기/청소) | ✅ | ✅ | ✅ |
| STEP 3 | 성장 + 허물 시스템 | ✅ | ✅ | ✅ |
| STEP 4 | 스토어 + 인벤토리 | ✅ | 🔧 | ⬜ |
| STEP 5 | 꾸미기 (테라리움) | ✅ | 🔧 | ⬜ |
| STEP 6 | 운영 기능 (일일보상, 설정) | ✅ | 🔧 | ⬜ |

> ✅ 완료 · 🔧 Unity Editor 작업 필요 · ⬜ 미착수

---

## 2. 완료된 작업 (스크립트 기준)

### Core
- `AppBootstrap.cs` — 초기화 순서 전체 구현 (SaveManager → TimeManager → PlayerRepository → Domain → GameManager)
- `GameManager.cs` — 싱글톤, SpendCoin / SpendGem / AddCoin / AddGem 포함
- `SceneRouter.cs` — 씬 전환 단일 창구

### Data
- `SaveManager.cs` — tmp→json 원자적 저장, bak 백업
- `PlayerRepository.cs` — 게코 CRUD, 인벤토리 추가/차감
- `TimeManager.cs` — GetElapsedHours / GetElapsedDays / ClampOfflineProgress(48h)

### Models
- `GeckoData.cs`, `PlayerData.cs`, `ItemSO.cs`, `GeckoSpeciesSO.cs`
- `DecorItemSO.cs` (enum DecorCategory: Background=0, Floor=1, Decoration=2)
- `TerrariumData.cs`, `DailyRewardData.cs`, `ProgressData.cs`, `SettingsData.cs`, `ItemStack.cs`

### Domain
- `GeckoManager.cs` — FeedGecko / GiveWater / Pet / Clean / ApplyOfflineProgress / EvaluateGrowth / TryMolt
- `StoreManager.cs` — BuyItem / BuyGecko
- `TerrariumManager.cs` — SetBackground / SetFloor / SetDecor
- `RewardManager.cs` — CanClaim / ClaimReward / 7일 순환 보상 테이블
- `SettingsManager.cs` — BGM / SFX / 진동 / 알림 설정

### UI
- `HomeUIController.cs` — 상태 게이지, 먹이/물/쓰다듬기/청소 버튼, 허물/성장 알림, 테라리움 비주얼
- `GeckoAnimatorController.cs` — 혀/눈 깜빡임 타이머, 감정 상태 폴링
- `StoreUIController.cs` — 아이템 목록 동적 생성, 구매 처리
- `GeckoListUIController.cs` — 게코 목록, 분양 패널
- `TerrariumUIController.cs` — 배경/바닥/장식 탭 전환
- `RewardPanelUI.cs` — 일일 보상 팝업
- `SettingsPanelUI.cs` — BGM/SFX/진동/알림 토글, 개인정보 버튼

### UI 컴포넌트 (프리팹에 부착할 스크립트)
- `ItemSlotUI.cs` — 아이콘/이름/가격/수량/구매 버튼
- `GeckoSlotUI.cs` — 이름/성장단계/선택 버튼
- `DecorSlotUI.cs` — 아이콘/이름/가격/선택 버튼 (선택 시 초록 강조)

### 에셋 파일
- **ItemSO** 7종 (`Assets/_Game/Resources/Items/`): cricket_small, gutloaded_cricket, mealworm, dubia_roach, superworm, calcium_dusting, growth_booster
- **GeckoSpeciesSO** 3종 (`Assets/_Game/Resources/Species/`): crested, leopard, gargoyle
- **DecorItemSO** 7종 (`Assets/_Game/Resources/Decor/`): bg_jungle, bg_desert, floor_soil, floor_bark, decor_rock, decor_plant, decor_hide

### 버그 수정 이력
| 날짜 | 파일 | 내용 |
|------|------|------|
| 2026-03-26 | TerrariumUIController.cs | UI에서 data.coin/gem 직접 수정 → GameManager.SpendCoin/SpendGem 위임 |
| 2026-03-26 | HomeUIController.cs | OnFeedClicked() GetSelectedGecko() null 체크 누락 수정 |
| 2026-03-26 | GameManager.cs | SpendGem() 메서드 추가 |

---

## 3. 미완료 — Unity Editor 작업

> ⚠️ **순서 중요**: 프리팹을 먼저 만들어야 씬 Inspector에서 연결 가능

### 0. Build Settings 확인 (최우선)
- [ ] `File → Build Settings` 씬 목록 확인
  ```
  0: Assets/_Game/Scenes/Boot.unity
  1: Assets/_Game/Scenes/MainHome.unity
  2: Assets/_Game/Scenes/Store.unity
  3: Assets/_Game/Scenes/GeckoList.unity
  4: Assets/_Game/Scenes/Terrarium.unity
  ```

---

### STEP 4-A: ItemSlot 프리팹 생성

저장 위치: `Assets/_Game/Prefabs/ItemSlotPrefab.prefab`

```
ItemSlot (Root)                  RectTransform: 400×120
  ├─ IconImage                   Image, 80×80, 좌측 상단
  ├─ NameText                    TMP_Text, 28pt, Bold
  ├─ PriceText                   TMP_Text, 24pt, 우측 정렬  ("15 C" / "5 G")
  ├─ CountText                   TMP_Text, 22pt            ("x3")
  └─ BuyButton                   Button → BtnText TMP "구매"
```

체크리스트:
- [ ] `ItemSlotUI` 컴포넌트 루트에 추가
- [ ] Inspector → _iconImage: IconImage
- [ ] Inspector → _nameText: NameText
- [ ] Inspector → _priceText: PriceText
- [ ] Inspector → _countText: CountText
- [ ] Inspector → _buyButton: BuyButton
- [ ] 프리팹으로 저장

---

### STEP 4-B: Store.unity 씬 조립

```
Canvas (CanvasScaler 1080×2400, Scale with Screen Size, Match 0.5)
  ├─ TopBar                      h=150, 상단 고정
  │    ├─ CoinText               TMP_Text
  │    └─ GemText                TMP_Text
  ├─ ScrollView                  남은 영역 stretch
  │    └─ Viewport > Content     VerticalLayoutGroup, Spacing=10
  ├─ BackButton                  Button "← 뒤로"
  └─ ErrorPanel                  비활성, 전체화면 중앙 팝업
       └─ ErrorText              TMP_Text
```

`StoreUIController` 컴포넌트 Inspector 연결:

| 필드 | 연결 대상 |
|------|----------|
| _coinText | TopBar/CoinText |
| _gemText | TopBar/GemText |
| _itemListContent | ScrollView/Viewport/Content |
| _itemSlotPrefab | ItemSlotPrefab (프리팹 드래그) |
| _itemsForSale (배열 7) | Resources/Items/ 7개 에셋 드래그 |
| _errorPanel | ErrorPanel |
| _errorText | ErrorPanel/ErrorText |
| _backButton | BackButton |

- [ ] 위 구조로 씬 오브젝트 생성
- [ ] StoreUIController 컴포넌트 Inspector 8개 필드 전부 연결
- [ ] 씬 저장

---

### STEP 4-C: GeckoSlot 프리팹 생성

저장 위치: `Assets/_Game/Prefabs/GeckoSlotPrefab.prefab`

```
GeckoSlot (Root)                 RectTransform: 500×100
  ├─ NameText                    TMP_Text, 32pt Bold
  ├─ StageText                   TMP_Text, 26pt ("Hatchling" 등)
  └─ SelectButton                Button (전체 영역 덮기)
```

체크리스트:
- [ ] `GeckoSlotUI` 컴포넌트 루트에 추가
- [ ] _nameText / _stageText / _selectButton 연결
- [ ] 프리팹으로 저장

---

### STEP 4-D: GeckoList.unity 씬 조립

```
Canvas
  ├─ TopBar (CoinText / GemText)
  ├─ ScrollView > Viewport > Content    VerticalLayoutGroup
  ├─ AdoptPanel (비활성)
  │    ├─ SpeciesDropdown              TMP_Dropdown
  │    ├─ NameInputField               TMP_InputField ("이름 입력")
  │    ├─ ConfirmButton                Button "확인"
  │    └─ CancelButton                 Button "취소"
  ├─ AdoptButton                       Button "+ 분양받기"
  ├─ BackButton                        Button "← 뒤로"
  └─ ErrorPanel (비활성) > ErrorText
```

`GeckoListUIController` Inspector 연결:

| 필드 | 연결 대상 |
|------|----------|
| _geckoListContent | ScrollView/Viewport/Content |
| _geckoSlotPrefab | GeckoSlotPrefab |
| _adoptPanel | AdoptPanel |
| _speciesDropdown | AdoptPanel/SpeciesDropdown |
| _nameInputField | AdoptPanel/NameInputField |
| _confirmAdoptButton | AdoptPanel/ConfirmButton |
| _cancelAdoptButton | AdoptPanel/CancelButton |
| _adoptButton | AdoptButton |
| _speciesForSale (배열 3) | Resources/Species/ 3개 드래그 |
| _backButton | BackButton |
| _errorPanel / _errorText | ErrorPanel / ErrorText |

- [ ] 씬 오브젝트 생성
- [ ] GeckoListUIController Inspector 11개 필드 연결
- [ ] 씬 저장

---

### STEP 5-A: DecorSlot 프리팹 생성

저장 위치: `Assets/_Game/Prefabs/DecorSlotPrefab.prefab`

```
DecorSlot (Root)                 RectTransform: 200×200
  ├─ IconImage                   Image, 140×140, 중앙 상단
  ├─ NameText                    TMP_Text, 24pt
  ├─ PriceText                   TMP_Text, 22pt ("100 C" / "Free")
  └─ SelectButton                Button (전체 영역)
```

체크리스트:
- [ ] `DecorSlotUI` 컴포넌트 루트에 추가
- [ ] _iconImage / _nameText / _priceText / _selectButton 연결
- [ ] 프리팹으로 저장

---

### STEP 5-B: Terrarium.unity 씬 조립

```
Canvas
  ├─ TopBar (CoinText / GemText)
  ├─ TabBar                      h=100, 상단 바 아래
  │    ├─ BgTabButton            Button "배경"
  │    ├─ FloorTabButton         Button "바닥"
  │    └─ DecorTabButton         Button "장식"
  ├─ ScrollView > Viewport > Content    GridLayoutGroup 2열, Cell 200×200
  ├─ BackButton
  └─ ErrorPanel (비활성) > ErrorText
```

`TerrariumUIController` Inspector 연결:

| 필드 | 연결 대상 |
|------|----------|
| _bgTabButton / _floorTabButton / _decorTabButton | TabBar 3개 버튼 |
| _itemListContent | ScrollView/Viewport/Content |
| _decorSlotPrefab | DecorSlotPrefab |
| _allDecorItems (배열 7) | Resources/Decor/ 7개 에셋 드래그 |
| _backButton | BackButton |
| _errorPanel / _errorText | ErrorPanel / ErrorText |

- [ ] 씬 오브젝트 생성
- [ ] TerrariumUIController Inspector 9개 필드 연결
- [ ] 씬 저장

---

### STEP 5-C: MainHome.unity — 테라리움 비주얼 연결

HomeUIController Inspector에서 추가 연결:

| 필드 | 위치/설명 |
|------|----------|
| _backgroundImage | GeckoArea 하위 배경 Image (전체 stretch) |
| _floorImage | GeckoArea 하위 바닥 Image (하단 고정) |
| _decorImages[0~3] | 장식 슬롯 Image 4개 (기본 비활성) |
| _allDecorItems (배열 7) | Resources/Decor/ 7개 에셋 드래그 |

- [ ] GeckoArea에 배경/바닥/장식 Image 오브젝트 생성
- [ ] HomeUIController 테라리움 필드 4종 연결

---

### STEP 6-A: MainHome.unity — RewardPanel 추가

```
RewardPanel (Canvas 최상위, 기본 비활성)    전체화면 stretch
  ├─ Overlay                     Image, RGBA(0,0,0,0.6), Raycast 차단
  └─ PopupBox                    Image(흰배경), 800×700, 중앙 앵커
       ├─ TitleText              TMP_Text "일일 보상", 42pt Bold
       ├─ StreakText             TMP_Text "연속 N일", 32pt
       ├─ RewardText            TMP_Text "코인 +100", 36pt
       ├─ ClaimButton           Button
       │    └─ ClaimBtnText     TMP_Text "받기"
       ├─ CloseButton           Button "X"
       └─ ResultText            TMP_Text (기본 비활성, 수령 후 표시)
```

`RewardPanelUI` 컴포넌트 추가 → Inspector 연결:

| 필드 | 연결 대상 |
|------|----------|
| _streakText | StreakText |
| _rewardText | RewardText |
| _claimButton | ClaimButton |
| _claimButtonText | ClaimButton/ClaimBtnText |
| _closeButton | CloseButton |
| _resultText | ResultText |

- [ ] RewardPanel 오브젝트 생성 (기본 비활성)
- [ ] RewardPanelUI Inspector 6개 필드 연결
- [ ] HomeUIController → _rewardPanel: RewardPanel 연결

---

### STEP 6-B: MainHome.unity — SettingsPanel 추가

```
SettingsPanel (Canvas 최상위, 기본 비활성)
  ├─ Overlay
  └─ PopupBox                    800×900
       ├─ TitleText              TMP_Text "설정"
       ├─ BgmRow
       │    ├─ Label             TMP_Text "BGM"
       │    └─ BgmToggle        Toggle
       ├─ SfxRow > SfxToggle
       ├─ VibrationRow > VibrationToggle
       ├─ NotificationRow > NotificationToggle
       ├─ PrivacyButton         Button "개인정보 처리방침"
       ├─ VersionText           TMP_Text (자동으로 "v{버전}" 채워짐)
       └─ CloseButton           Button "X"
```

`SettingsPanelUI` 컴포넌트 추가 → Inspector 연결:

| 필드 | 연결 대상 |
|------|----------|
| _bgmToggle | BgmRow/BgmToggle |
| _sfxToggle | SfxRow/SfxToggle |
| _vibrationToggle | VibrationRow/VibrationToggle |
| _notificationToggle | NotificationRow/NotificationToggle |
| _privacyButton | PrivacyButton |
| _versionText | VersionText |
| _closeButton | CloseButton |

- [ ] SettingsPanel 오브젝트 생성 (기본 비활성)
- [ ] SettingsPanelUI Inspector 7개 필드 연결
- [ ] HomeUIController → _settingsPanel: SettingsPanel 연결

---

## 4. 생성 필요한 에셋 목록

### 스크립트로 생성 완료된 에셋 ✅
| 경로 | 파일 | 상태 |
|------|------|:----:|
| Resources/Items/ | cricket_small, gutloaded_cricket, mealworm, dubia_roach, superworm, calcium_dusting, growth_booster | ✅ |
| Resources/Species/ | crested, leopard, gargoyle | ✅ |
| Resources/Decor/ | bg_jungle, bg_desert, floor_soil, floor_bark, decor_rock, decor_plant, decor_hide | ✅ |

### Unity Editor에서 만들어야 하는 에셋
| 에셋 | 경로 | 우선순위 |
|------|------|:--------:|
| ItemSlotPrefab.prefab | Assets/_Game/Prefabs/ | 높음 (Store 씬 필요) |
| GeckoSlotPrefab.prefab | Assets/_Game/Prefabs/ | 높음 (GeckoList 씬 필요) |
| DecorSlotPrefab.prefab | Assets/_Game/Prefabs/ | 보통 (Terrarium 씬 필요) |
| 게코 스프라이트/애니메이터 | Assets/_Game/Textures/ | 낮음 (MVP에 없어도 동작) |
| 아이템 아이콘 Sprite | Texture 임포트 후 SO에 연결 | 낮음 |

> 스프라이트 없으면 icon 필드가 null → UI에서 빈 Image로 표시. 기능 동작에는 무관.

---

## 5. 권장 작업 순서

```
1. Build Settings 씬 등록 확인                   (5분)
2. Prefabs 폴더 생성 후 ItemSlot 프리팹 제작      (15분)
3. Store.unity 씬 조립 + Inspector 연결           (20분)
4. GeckoSlot 프리팹 제작                          (10분)
5. GeckoList.unity 씬 조립 + Inspector 연결       (20분)
6. Play → STEP 4 기능 검증 (구매/분양)            (10분)
7. DecorSlot 프리팹 제작                          (10분)
8. Terrarium.unity 씬 조립 + Inspector 연결       (20분)
9. MainHome에 테라리움 비주얼 Image 오브젝트 추가  (15분)
10. Play → STEP 5 기능 검증 (꾸미기)              (10분)
11. MainHome에 RewardPanel 오브젝트 추가          (15분)
12. MainHome에 SettingsPanel 오브젝트 추가        (15분)
13. Play → STEP 6 기능 검증 (보상/설정)           (10분)
14. 전체 동작 검증 체크리스트 순서대로 실행        (20분)
```

---

## 6. 전체 동작 검증 체크리스트

### STEP 1–3 (기존 확인됨)
- [ ] Boot → MainHome 자동 전환, 콘솔 오류 없음
- [ ] 먹이 버튼 → Hunger 수치 증가 + 혀 애니 재생
- [ ] Water 버튼 → Thirst 수치 증가 + 음수 애니
- [ ] 앱 종료 후 재시작 → 오프라인 시간 보정 로그 출력
- [ ] 데이터 복원 → 이전 상태값 유지

### STEP 4 (스토어 + 인벤토리)
- [ ] BottomTab Store 버튼 → Store 씬 전환
- [ ] 아이템 목록 7개 표시 (아이콘 없어도 이름/가격 표시)
- [ ] 아이템 구매 → 보유 수량(CountText) 증가 + 코인 차감
- [ ] 코인 부족 → 에러 패널 "코인이 부족합니다" 표시 (2.5초 후 숨김)
- [ ] 뒤로 → MainHome 복귀
- [ ] BottomTab GeckoList 버튼 → GeckoList 씬 전환
- [ ] 게코 목록 표시 (기본 하코 포함)
- [ ] "+ 분양받기" → AdoptPanel 표시 → 종 선택 → 이름 입력 → 확인 → 목록에 추가
- [ ] 분양받은 게코 슬롯 탭 → 해당 게코 선택 + HomeScreen 복귀
- [ ] 먹이 버튼 → 인벤토리 수량 1 감소 (0 되면 Store 씬으로 이동)

### STEP 5 (꾸미기)
- [ ] BottomTab Terrarium 버튼 → Terrarium 씬 전환
- [ ] 배경 탭 → bg_jungle, bg_desert 슬롯 표시
- [ ] 바닥 탭 → floor_soil, floor_bark 슬롯 표시
- [ ] 장식 탭 → decor_rock, decor_plant, decor_hide 슬롯 표시
- [ ] 무료 아이템 선택 → 코인 차감 없이 적용
- [ ] 유료 아이템 선택 → 코인 차감 확인
- [ ] 코인 부족 → 에러 패널 표시
- [ ] 홈 복귀 → 배경/바닥/장식 즉시 반영
- [ ] 앱 재시작 → 꾸미기 상태 유지 (저장 확인)

### STEP 6 (운영 기능)
- [ ] 앱 첫 진입 → RewardPanel 자동 표시 (CanClaim=true)
- [ ] "받기" 버튼 → 코인 증가 + "연속 1일" 표시
- [ ] 당일 재진입 → RewardPanel "내일 다시" 버튼 (비활성)
- [ ] 보상 버튼 클릭 → RewardPanel 수동 표시
- [ ] 설정 버튼 → SettingsPanel 표시
- [ ] BGM 토글 OFF → AudioListener.volume = 0
- [ ] BGM 토글 ON → AudioListener.volume = 1
- [ ] 버전 텍스트 표시 확인 ("v0.1" 등)
- [ ] X 버튼 → 패널 닫힘

---

## 7. 알려진 제약사항 및 미결 항목

| 항목 | 내용 | 처리 예정 |
|------|------|----------|
| 자연사 처리 | growthStage 900일 도달 시 로그만 출력 | STEP 6 이후 |
| 오프라인 상한 | 48h `[TBD]` — TimeManager.ClampOfflineProgress | 수치 확정 후 |
| HUNGER_DECAY 등 수치 | 4f/5f/0.67f 등 `[TBD]` 마킹됨 | 플레이테스트 후 조정 |
| 게코 스프라이트/애니 | animController = null (SO에 미연결) | 아트 작업 후 |
| 아이템 아이콘 | icon = null (SO에 미연결) | 아트 작업 후 |
| AudioManager | BGM은 AudioListener.volume으로 임시 처리 | STEP 6 이후 |
| GeckoAnimatorController 람다 해제 | OnMoltSuccess/Fail/GrowthUp 람다 -= 불가 | GO Destroy 시 자동 해제, 실용상 무관 |
| 개인정보 URL | `https://www.tnb-soft.com/hako-privacy` (미개설) | 배포 전 |
| 2차 MVP 먹이 선택 UI | 현재 인벤토리 첫 아이템 자동 선택 | STEP 6 이후 |
