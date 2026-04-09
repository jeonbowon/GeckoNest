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

## 애니메이션 트리거

`GeckoManager` 이벤트 → `GeckoAnimatorController` 수신 → `Animator.SetTrigger()`. 클립명 오타 = 트리거 무시 (완전 일치 필수).

| 클립명 | 발동 조건 |
|--------|----------|
| `Idle_Breath` | 상시 루프 (default state) |
| `Blink_Short` | 3~7초 랜덤 |
| `Tongue_Lick` | 4~8초 랜덤 (시그니처 애니) |
| `Tongue_FeedCatch` | `FeedGecko()` 호출 시 |
| `Tongue_Drink` | `GiveWater()` 호출 시 |
| `Happy_LookUp` | mood > 70 AND affection > 50 |
| `Sleepy_Slow` | mood < 35 |
| `Angry_TailFlick` | hunger < 20 AND mood < 30 |
| `Molt_Start` | moltProgress > 80 |
| `Molt_Finish` | TryMolt() 성공 후 |
| `LevelUp_Pulse` | 성장 단계 상승 |

**혀 애니 팁:** `_anim.speed = Random.Range(0.9f, 1.1f)` 적용 — 기계적 느낌 제거 필수.

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
