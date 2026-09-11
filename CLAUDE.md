# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**GeckoNest (HAKO)** is a gecko 펫 육성 시뮬레이션 앱 built with Unity 6 (6000.2.8f1). Android 우선 출시, iOS 순차 확장. 1인 개발 / MVP 우선. 현재 초기 scaffold 단계 — 폴더 구조와 설정만 갖춰진 상태.

**플랫폼:** Android (Target API: 최신) → iOS 순차 확장
**렌더 파이프라인:** Built-in 2D (URP 아님)
**Scripting Backend:** IL2CPP, Target Architecture: ARM64

## Unity Development

모든 빌드/테스트/실행은 **Unity Editor**에서 수행. CLI 빌드 없음.

- **Open project:** Unity Hub → Open → `D:/AppsWeb/Unity/GeckoNest`
- **Entry scene:** `Assets/_Game/Scenes/Boot.unity`
- **Run tests:** Unity Editor → Window → General → Test Runner
- **APK 빌드:** File → Build Settings → Android → Build
- **AAB (구글플레이용):** Build Settings → Build App Bundle (Google Play) 체크

**Project Settings 필수 확인:**
- Version Control → Mode: **Visible Meta Files**
- Asset Serialization → Mode: **Force Text** (meta 충돌 방지)
- Physics 2D → Gravity Y = **0** (게코는 중력 없음)
- Quality → Low/Medium 만 남기고 삭제 (모바일 최적화)

## Architecture

**데이터 흐름: UI → Manager → Repository → Save (단방향)**
UI 클래스에서 `PlayerData.coin` 같은 데이터 직접 수정 금지.

```
Core/           AppBootstrap, GameManager, SceneRouter
Domain/         GeckoManager, StoreManager, TerrariumManager, RewardManager
Data/           SaveManager, TimeManager, PlayerRepository
Models/         GeckoData, PlayerData, ItemData 등 직렬화 클래스 ([Serializable])
UI/             *UIController 클래스들
```

**SceneRouter:** 씬 전환은 반드시 `SceneRouter.cs` 한 곳에서만. `SceneManager.LoadScene()`을 UI 클래스에서 직접 호출 금지.

**씬 구성:**
| 씬 | 용도 |
|----|------|
| `Boot.unity` | 앱 시작 + 매니저 초기화 → AppBootstrap |
| `MainHome.unity` | 핵심 플레이 화면 |
| `Store.unity` | 상점 |
| `GeckoList.unity` | 게코 목록 |
| `Terrarium.unity` | 꾸미기 |
| `Popup.unity` | 공용 팝업 (Additive load) |

**AppBootstrap 초기화 순서** (의존성 역방향 NullRef 방지):
1. `SaveManager` → `TimeManager` → `PlayerRepository`
2. `GeckoManager`, `StoreManager`, `TerrariumManager`, `RewardManager`
3. `GameManager.Initialize(...)` → `SceneRouter.GoToHome()`

## Data Models

`[Serializable]` + `JsonUtility` 기반. **Dictionary 사용 금지** (JsonUtility 직렬화 불가). List만 사용.

**GeckoData 핵심 필드:**
```csharp
string id, name, speciesId          // 식별
int growthStage (0~3), float growthExp, float moltProgress, int moltCount  // 성장
float hunger, thirst, mood, health, cleanliness, affection  // 상태값 (0~100)
long lastUpdatedTicks               // ← 핵심! OnApplicationPause(true)에서 반드시 갱신
```

**PlayerData:** `coin`, `gem`, `List<GeckoData> geckos`, `List<string> ownedItemIds`, `selectedGeckoId`, `TerrariumData`, `DailyRewardData`, `ProgressData`, `SettingsData`, `saveVersion`

**저장 파일 구조 (tmp→json 원자적 교체):**
| 파일 | 역할 |
|------|------|
| `player_data.json` | 메인 저장파일 |
| `player_data.tmp` | 저장 중 임시 (완료 후 rename) |
| `player_data.bak` | 직전 정상 백업본 |

**저장 타이밍:** 먹이/물 사용, 구매, 장식 적용, `OnApplicationPause(true)`. 매 프레임 저장 절대 금지.

## 게코 상태 시스템

| 상태값 | 시간당 감소 | 위험 기준 | 0 도달 시 |
|--------|------------|----------|----------|
| Hunger | -4/h [TBD] | 30 이하 경고 | Health -1/h |
| Thirst | -5/h [TBD] | 30 이하 경고 | Health -1/h |
| Cleanliness | -0.67/h | 20 이하 | Mood -0.5/h |
| Mood | -1/h + 연쇄 | 25 이하 | — |
| Health | 매우 느림 | 20 이하 위험 | — |

**오프라인 진행:** `TimeManager.ClampOfflineProgress(hours)` 필수 적용 (상한 48h [TBD]). `DateTime.UtcNow` 사용 (로컬 시간대 조작 방어).

**허물 판정 (`TryMolt`):** `moltProgress >= 100` 시 발동. 기본 성공률 70%, thirst > 50 이면 +15%, health > 60 이면 +10%. 실패 시 moltProgress를 0이 아닌 30으로 리셋 (강한 패널티 금지).

**오프라인 보정:** `ApplyOfflineProgress()` → AppBootstrap에서 앱 재실행 시 모든 게코에 적용 후 즉시 저장.

## ScriptableObjects

```csharp
// ItemSO — 먹이/장식 아이템
[CreateAssetMenu(menuName = "Hako/Item")]
public class ItemSO : ScriptableObject {
    public string itemId, displayName;
    public Sprite icon;
    public int coinPrice, gemPrice;
    public float hungerRestore, thirstRestore, moodBonus, growthExpGain;
    public string[] preferredSpeciesIds;
}

// GeckoSpeciesSO — 게코 종
[CreateAssetMenu(menuName = "Hako/GeckoSpecies")]
public class GeckoSpeciesSO : ScriptableObject {
    public string speciesId;   // "crested" | "leopard" | "gargoyle"
    public string displayName;
    public Sprite thumbnailSprite;
    public RuntimeAnimatorController animController;
    public int coinPrice;
    public bool isUnlockedByDefault;
}
```

## 게코 애니메이션 (코드 방식)

**Animator·키프레임 클립을 쓰지 않는다.** 움직임은 `GeckoMotor`가 매 프레임 코드로 계산한다. 그림(스킨)을 바꿔도 움직임이 그대로 유지되게 하기 위함이다.

```
GeckoManager 이벤트 / 선택 게코 상태값
  → GeckoAnimatorController   게임 ↔ 모터 연결 (기분 판정, 이벤트 → 동작)
  → GeckoMotor                 층을 쌓아 GeckoPose 계산
  → GeckoRig.Solve(pose)       파츠 14개 배치 (정기구학) → 화면
```

**파일**

| 파일 | 역할 |
|------|------|
| `UI/Gecko/GeckoParts.cs` | 파츠·표정·동작 enum과 레이어 이름 표 (`GeckoPartId`, `GeckoEye`, `GeckoMouth`, `GeckoAction`, `GeckoMood`) |
| `UI/Gecko/GeckoRig.cs` | 스킨 적용, 관절 계산, 좌우 반전, 성장 단계 크기 |
| `UI/Gecko/GeckoMotor.cs` | 호흡·꼬리 물리·걷기·표정·동작 13종 계산. 수치는 Inspector `[TBD]` |
| `UI/Gecko/GeckoBendGraphic.cs` | 휘어지는 꼬리 메시 (UI) |
| `UI/Gecko/GeckoPose.cs` | 한 프레임 자세 데이터 |
| `Models/GeckoSkin.cs` | 그림 한 벌 (ScriptableObject). **그림 교체 = 이 에셋 교체** |
| `UI/GeckoAnimatorController.cs` | 게임 데이터 연결. 이름은 예전 그대로지만 Animator를 쓰지 않는다 |
| `Domain/GeckoMovementAI.cs` | 테라리움 바닥 돌아다니기 (UI 좌표), 발 높이에 따른 원근 |
| `Assets/Editor/Gecko/` | `Hako > Gecko` 메뉴, 프록시 그림 생성, 인스펙터 |

**동작 (`GeckoMotor.Play(GeckoAction)`)** — 이름 오타 방지를 위해 enum으로만 호출한다.

| 동작 | 호출 | 길이 |
|------|------|------|
| `Tongue_Lick` | 대기 중 자동 4~8초 (`_lickInterval`). 30% 확률로 `Tongue_EyeLick`으로 바뀜 (`_eyeLickChance`) | 0.55초 |
| `Tongue_EyeLick` | **시그니처** — 혀로 눈 닦기. 크레스티드는 눈꺼풀이 없어 혀로 눈을 닦는다 | 1.5초 |
| `Tongue_FeedCatch` | 먹이 버튼 → `TriggerFeedCatch()` | 1.4초 |
| `Tongue_Drink` | 물 버튼 → `TriggerDrink()` | 1.9초 |
| `Pet_Reaction` | 쓰다듬기 버튼 → `TriggerPet()` | 1.6초 |
| `Happy_LookUp` | 청소 버튼 → `TriggerClean()` / 기쁨 기분에서 자동 9~18초 (70%) | 1.3초 |
| `Jump` | 기쁨 기분에서 자동 (30%) | 0.95초 |
| `Angry_TailFlick` | 화남 기분에서 자동 4.5~9초 | 1.0초 |
| `Molt_Start` | `OnMoltFail` — 실패해도 껍질이 들뜨는 연출 | 1.3초 |
| `Molt_Finish` | `OnMoltSuccess` | 1.8초 |
| `LevelUp_Pulse` | `OnGrowthUp` + 성장 단계 크기 전환 | 1.1초 |
| `Surprise` | 자동 호출 없음 (터치 반응용 예약) | 0.8초 |
| `Blink_Short` | 수동 재생 전용. 자동 깜빡임은 `_canBlink` 켠 종만 3~7초 (크레스티드 기본 꺼짐) | 0.16초 |

**기분 (동작이 아니라 계속 유지되는 상태)** — `GeckoAnimatorController.ResolveMood`, 우선순위 위에서부터

| 기분 | 조건 | 모습 |
|------|------|------|
| `Angry` | hunger < 20 AND mood < 30 | 시무룩한 입, 고개 숙임, 꼬리 튕기기 |
| `Sleepy` | mood < 35 | 반쯤 감긴 눈, 느린 호흡, 가끔 졸기, 걷지 않음 (예전 `Sleepy_Slow`) |
| `Happy` | mood > 70 AND affection > 50 | 미소, 가끔 반짝이는 눈, 꼬리 말아 올림 |
| `Normal` | 그 외 | — |
| 허물 준비 | moltProgress ≥ 80 (기분과 별개) | 몸에 허물 조각 표시 |

호흡(예전 `Idle_Breath`)은 항상 켜져 있다. 기계적 느낌은 무작위 간격, Perlin 노이즈 머리 흔들림, 꼬리 스프링으로 없앤다.

**계산 순서 (`GeckoMotor.LateUpdate`)**: ① 호흡 → ② 기분 자세 → ③ 걷기 → ④ 머리·둘러보기 → ⑤ 동작 → ⑥ 접지·그림자 → ⑦ 꼬리 물리 → ⑧ 표정

**새 동작 추가**

1. `GeckoParts.cs`의 `GeckoAction`에 이름 추가
2. `GeckoMotor.DurationOf`에 길이 추가
3. `GeckoMotor.EvaluateAction` switch + `Act*` 메서드 작성 — 곡선은 **시작과 끝이 0**이어야 한다 (동작끼리 0.12초 크로스페이드). `Rot/Move/Grow/Fade/SetEyes/SetMouth` 헬퍼는 가중치를 자동 반영한다
4. 호출: `GeckoAnimatorController.Trigger*` 추가 또는 `motor.Play()`
5. `Assets/Editor/Gecko/GeckoInspectors.cs`의 `ACTIONS`에 미리보기 버튼 추가

**그림 · 좌표 규칙**

- 스킨 좌표 = 스킨 픽셀, **게코 발밑 중앙이 (0,0)**, **오른쪽을 보는 그림** 기준. 좌우 반전은 `GeckoRig.SetFacing`이 처리한다
- 파츠는 **이름으로 찾는다** — `GeckoParts.cs`의 레이어 이름(`tail`, `body`, `head`, `eye_l` …)과 표정 이름(`eye_open`, `eye_look_left` …, `mouth_closed`, `mouth_smile` …)과 글자 단위로 일치해야 한다
- 게코는 **UI(하위 Canvas)** 로 그린다 — 홈 화면이 Screen Space Overlay라 월드 스프라이트는 배경에 가려진다. `DepthObject`·`TerrariumDepthManager`는 월드 스프라이트용이라 게코에 쓰지 않는다
- 성장 단계 크기: `GeckoRig._stageScales` = 0.55 / 0.68 / 0.80 / 0.90 / 1.00. 단계별 전용 그림은 `_stageSkins`
- `GeckoSpeciesSO.animController`는 현재 쓰지 않는다

**메뉴 (`Hako > Gecko`)**: ① 프록시 게코 만들기 (MainHome에서) · ② 선택한 PSD·폴더로 스킨 만들기 · ③ 선택한 스킨을 씬 게코에 적용

**확인**: 플레이 중 Hierarchy에서 `GeckoObject` 선택 → Inspector의 `GeckoMotor` 아래 버튼으로 동작·성장 단계를 하나씩 미리 본다.

## 주요 컨벤션 & 주의사항

- **모든 텍스트는 TextMeshPro** (UI Text 사용 금지)
- **꾸미기 자유 드래그 배치는 MVP 절대 금지** (슬롯 방식만)
- **먹이 버튼 MVP:** ownedItemIds 첫 번째 아이템 자동 선택 (종류 선택 UI는 2차 MVP)
- **경로에 한글/공백 포함 시 Android 빌드 실패** — 영문 경로 필수
- **Keystore 파일은 프로젝트 외부 보관, Git 커밋 절대 금지** (`.gitignore`에 이미 포함)
- **미확정 수치는 코드에 `const float HUNGER_DECAY = 4f; // [TBD]` 형태로 자리 유지**
- `GameManager.Instance`의 `selectedGeckoId` null 체크 및 geckos 목록 유효성 검사 필수
- `FeedGecko` 호출 전 인벤토리 확인 필수 (item=null이면 NullRef)

## MVP 단계 로드맵

| 단계 | 목표 | 완료 기준 |
|------|------|----------|
| 1 | 기반 골격 | 재실행 시 데이터 복원 + 오프라인 시간 보정 |
| 2 | 홈 + 돌봄 루프 | 먹이/물 → 상태 즉시 반영 + 혀 애니 |
| 3 | 성장 + 허물 | 성장 단계 상승 + 허물 이벤트 연출 |
| 4 | 스토어 + 인벤토리 | 먹이 구매 → 사용 + 게코 분양 → 목록 추가 |
| 5 | 꾸미기 | 배경/장식 저장 후 홈 즉시 반영 |
| 6 | 운영 기능 | 일일 보상 + 설정 + 개인정보 + APK 내부 테스트 |

**STEP 6 전에 2차 기능 구현 시작 금지.**
막힌 문제가 3시간 이상 해결 안 되면 우회 방법 먼저, 완벽보다 진도 우선.

## Key Packages

| 패키지 | 버전 | 용도 |
|--------|------|------|
| `com.unity.feature.2d` | 2.0.1 | 2D 게임 툴 번들 |
| `com.unity.inputsystem` | 1.14.2 | New Input System |
| `com.unity.ugui` | 2.0.0 | uGUI |
| `com.unity.timeline` | 1.8.9 | 타임라인 애니메이션 |

**추가 설치 필요:** TextMeshPro (TMP Essentials), Mobile Notifications, Newtonsoft JSON (선택)

## 현재 진행 상태

작업 시작 전 반드시 `DEV_LOG.md`를 읽어 현재 단계와 남은 작업을 확인할 것.
