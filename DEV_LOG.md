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

## 버그 수정 이력

| 날짜 | 증상 | 원인 | 해결 |
|------|------|------|------|
| 2026-03-23 | Type mismatch: Expected Canvas, found CanvasGroup | CanvasGroup에 `!u!223`(Canvas 타입 ID) 사용 | `!u!225`(CanvasGroup 타입 ID)로 수정 |

---

## 진행 현황 (2026-03-23)

| 단계 | 목표 | 상태 |
|------|------|:----:|
| STEP 1 | 기반 골격 | ✅ |
| STEP 2 | 홈 + 돌봄 루프 | ✅ |
| STEP 3 | 성장 + 허물 | ✅ |
| STEP 4 | 스토어 + 인벤토리 | ⬜ |
| STEP 5 | 꾸미기 | ⬜ |
| STEP 6 | 운영 기능 | ⬜ |

### STEP 4 착수 전 체크리스트
- [ ] ActionButtons — Feed/Water 버튼만 남기고 Pet/Clean 배치 재검토
- [ ] StatusPanel 바 크기/위치 Unity Editor에서 파인튜닝
- [ ] GrowthStageIcon SizeDelta 조정 (현재 36×36, 128px 텍스처 기준 확대 권장)

### STEP 4 예정 작업
- `StoreManager`: 코인/젬으로 아이템 구매, 인벤토리 증감
- `Store.unity` UI: 아이템 목록, 구매 버튼
- `GeckoList.unity` UI: 게코 목록, 신규 분양
- 먹이 버튼 인벤토리 연동 (현재 MVP: `ownedItemIds[0]` 자동 선택)

---

## 설계 메모

- **오프라인 보정 상한**: 48h `[TBD]` — `TimeManager.ClampOfflineProgress()`
- **수치 미확정** `[TBD]`: `HUNGER_DECAY(4)`, `THIRST_DECAY(5)`, `MOOD_DECAY(1)`, `MOLT_PROGRESS_PER_HOUR(0.20)`
- **자연사 처리**: `GROWTH_DAYS_NATURAL_DEATH = 900f` — STEP 6 예정
- **람다 이벤트 해제 불가**: `GeckoAnimatorController`의 OnMoltSuccess/Fail/GrowthUp 람다는 `-=` 해제 불가 → GO Destroy 시 자동 해제, 실용상 문제없음
- **먹이 선택 UI**: 2차 MVP — 현재 `ownedItemIds` 첫 번째 아이템 자동 선택
- **씬 파일 직접 편집 방식**: Unity YAML 구조 파악 후 Python 스크립트로 오브젝트 append 및 m_Children 수정
