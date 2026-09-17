# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**GeckoNest (HAKO)** is a gecko 펫 육성 시뮬레이션 앱 built with Unity 6 (6000.2.8f1). Android 우선 출시, iOS 순차 확장. 1인 개발 / MVP 우선.
현재 상태: STEP 1~6 스크립트·연출·설정 완료. 최종 아트/사운드 교체와 기기 테스트가 남아 있다 (`DEV_LOG.md` 참고).

**플랫폼:** Android (Target API: 최신) → iOS 순차 확장
**렌더 파이프라인:** Built-in 2D (URP 아님)
**Scripting Backend:** IL2CPP, Target Architecture: ARM64
**패키지 이름:** `com.tnbsoft.hako` · 회사 `TNBSoft` · 제품 `Hako` · 버전 `0.1.0` · **세로 고정**
**앱 아이콘:** `Assets/_Game/Textures/Icons/app_icon.png` (Player Settings의 Legacy·Round 슬롯에 연결됨. 적응형 아이콘은 비어 있어 Unity가 자동 생성)

## Unity Development

모든 빌드/테스트/실행은 **Unity Editor**에서 수행. CLI 빌드 없음.

- **Open project:** Unity Hub → Open → `D:/AppsWeb/Unity/GeckoNest`
- **Entry scene:** `Assets/_Game/Scenes/Boot.unity`
- **로직 자가 검사:** 메뉴 `Hako > 검사 > 로직 자가 검사` — 돌봄 제한·허물 속도·시간 보정·사건 대기열 등 게임 규칙을 플레이 없이 확인 (진짜 저장 파일은 건드리지 않는다)
  - **창 없이 실행 (Unity가 꺼져 있을 때):** `"C:\Program Files\Unity\Hub\Editor\6000.2.8f1\Editor\Unity.exe" -batchmode -nographics -projectPath D:\AppsWeb\Unity\GeckoNest -executeMethod HakoSelfTest.RunBatch -logFile (로그)` — 실제 Unity 컴파일 + 자가 검사, 로그에 "통과/실패" 줄과 "모두 통과 (N개)", 실패가 있으면 종료 코드 1. 새 스크립트의 `.meta`도 이때 생긴다 (약 15초)
- **시간 건너뛰기:** 플레이 중 메뉴 `Hako > 검사 > 시간 건너뛰기`. `GameManager.DebugSkipTime(hours, caredFor)`(에디터 전용)이 기준 시각을 과거로 옮긴 뒤 평소 시간 보정 경로로 반영한다
  - **+6시간 · +24시간 (내버려 둠)** — 한 번에 반영, 오프라인 상한 48h 적용. 게이지 감소·경고 확인용 (24시간은 배고픔이 바로 0이 되므로 30 이하 경고는 6시간을 두세 번 눌러 본다)
  - **+7일 · +2주 · +30일 (잘 돌봄)** — 8시간씩 나눠 진행하며 구간마다 배고픔·목마름·청결·기분을 100으로 채운다. 한 번에 반영하면 48h 상한 때문에 한 달을 건너뛰어도 허물은 2일치만 진행되므로, 나이·허물·성장·일일 보상이 기간만큼 실제 순서대로 일어나게 나눈다. 애정도·건강은 직접 채우지 않는다 — 건강은 회복 규칙(+0.5/h)대로 오르고, 어덜트 조건 애정도 60은 쓰다듬기로
- **테스트 재화:** 플레이 중 메뉴 `Hako > 검사 > 재화` (코인 +1,000 / 코인 +10,000 / 젬 +100). `GameManager.DebugAddCurrency`(에디터 전용)가 더하고 바로 저장한다. 홈 윗줄은 바로 카운트업, 상점·꾸미기 화면은 나갔다 들어오면 반영
- **홈 상태 게이지 · 허물 진행 막대:** `StatusPanel/*Bar/Fill`과 `MoltProgressFill` Image는 **Filled · Horizontal + 스프라이트 지정**이어야 한다. Simple이거나 **스프라이트가 비어 있으면 `fillAmount`가 무시되고 사각형 전체가 그려진다** (uGUI `Image.OnPopulateMesh`). 씬에는 기본 `UISprite`를 넣어 두었고, `HomeUIController.MakeFillable`이 실행 시 한 번 더 보정한다. 평소 색은 `HomeUIController.GAUGE_*`, 막대 오른쪽 위 숫자는 `GaugeView`가 실행 중에 만든다
- **홈 화면 배치 (1080×2400 기준):** 위 — 이름·성장 단계(왼쪽), **코인 → 젬**(오른쪽), 허물 막대, 상태 띠(가로 5칸: 아이콘 + 막대 + 숫자, 이름 글자 `Label`은 꺼 둠). 아래 — 둥근 돌봄 버튼 4개(위 아이콘 + 아래 글자, 내비 바 바로 위). **가운데는 게코 공간으로 비워 둔다** — 새 UI를 가운데에 올리지 않는다. 버튼 아이콘은 `HomeUIController._careButtonIcons`(지금은 32px 상태 아이콘 재사용)를 실행 중에 붙인다
- **Run tests:** Unity Editor → Window → General → Test Runner
- **APK 빌드:** File → Build Settings → Android → Build
- **AAB (구글플레이용):** Build Settings → Build App Bundle (Google Play) 체크

**Project Settings 필수 확인:**
- Version Control → Mode: **Visible Meta Files**
- Asset Serialization → Mode: **Force Text** (meta 충돌 방지)
- Physics 2D → Gravity Y = **0** (게코는 중력 없음) — 적용됨
- Quality → Android 기본 레벨 = **Medium** (적용됨). 레벨 목록 정리는 선택 사항

## Architecture

**데이터 흐름: UI → Manager → Repository → Save (단방향)**
UI 클래스에서 `PlayerData.coin` 같은 데이터 직접 수정 금지.

```
Core/           AppBootstrap, GameManager, SceneRouter, SceneFader
                AudioManager, SfxLibrary, SfxSynth, Sfx, Haptics   (소리·진동 — Boot 없이 실행해도 오류 없이 무음)
Domain/         GeckoManager, GeckoEventQueue, StoreManager, TerrariumManager, RewardManager
Data/           SaveManager, TimeManager, PlayerRepository
Models/         GeckoData, PlayerData, ItemData 등 직렬화 클래스 ([Serializable])
UI/             *UIController 클래스들
UI/Gecko/       게코 코드 애니메이션 (아래 "게코 애니메이션")
UI/Fx/          연출 — GeckoFx, UIParticles, SpeechBubble, UIPressScale, FxSprites
```

**SceneRouter:** 씬 전환은 반드시 `SceneRouter.cs` 한 곳에서만. `SceneManager.LoadScene()`을 UI 클래스에서 직접 호출 금지. 전환은 `SceneFader`가 크림색 페이드로 처리하고, 전환 중(`SceneRouter.IsTransitioning`)에는 입력을 막는다.

**씬 구성:**
| 씬 | 용도 |
|----|------|
| `Boot.unity` | 앱 시작 + 매니저 초기화 → AppBootstrap |
| `MainHome.unity` | 핵심 플레이 화면 |
| `Store.unity` | 상점 |
| `GeckoList.unity` | 게코 목록 |
| `Terrarium.unity` | 꾸미기 |

**어느 씬에서 실행해도 된다:** Boot을 거치지 않고 MainHome·Store 등을 바로 실행하면 `AppBootstrap.EnsureBootstrapped`가 매니저를 만들고 그 씬을 다시 연다. 빌드에서는 Boot이 첫 씬이므로 평소에는 동작하지 않는다.

**AppBootstrap 초기화 순서** (의존성 역방향 NullRef 방지):
0. `Application.targetFrameRate = 60`, `AudioManager.Create()`
1. `SaveManager` → `TimeManager` → `PlayerRepository`
2. `GeckoManager`, `StoreManager`, `TerrariumManager`, `RewardManager`, `SettingsManager`
3. `GeckoEventQueue` — **시간 보정보다 먼저** (부팅 중 생긴 허물·성장도 모으려고)
4. `GameManager.Initialize(...)` → 설정 적용 → 기본 게코 보장 → 시간 보정 + 저장 → `SceneRouter.GoToHome()`

## Data Models

`[Serializable]` + `JsonUtility` 기반. **Dictionary 사용 금지** (JsonUtility 직렬화 불가). List만 사용.

**GeckoData 핵심 필드:**
```csharp
string id, name, speciesId          // 식별
int growthStage (0~4), float growthExp, float moltProgress, int moltCount  // 성장 (0=Hatchling … 4=Adult)
float hunger, thirst, mood, health, cleanliness, affection  // 상태값 (0~100)
long lastUpdatedTicks               // ← 핵심! 경과 시간 기준. 시간 진행(ApplyOfflineProgress) 때마다 갱신
```

**TerrariumData.ownedDecorIds:** 산 배경·바닥 (다시 골라도 결제 안 함). 장식은 놓을 때마다 결제. 예전 저장 파일은 `SaveManager.TryMigrate`에서 빈 목록으로 보정.

**PlayerData:** `coin`, `gem`, `List<GeckoData> geckos`, `List<string> ownedItemIds`, `selectedGeckoId`, `TerrariumData`, `DailyRewardData`, `ProgressData`, `SettingsData`, `saveVersion`

**저장 파일 구조 (tmp → json 교체):**
| 파일 | 역할 |
|------|------|
| `player_data.json` | 메인 저장파일 |
| `player_data.tmp` | 저장 중 임시 — 끝까지 쓰고 디스크에 확정(`Flush(true)`)한 뒤에만 메인을 교체 |
| `player_data.bak` | 직전 정상 백업본 |

**읽기 순서 (`SaveManager.Load`):** 메인 → (메인이 없으면) 임시 → 백업 → 새 데이터. 메인이 없고 임시만 있다 = 저장 도중 멈춘 것이므로 임시가 가장 최신이다.

**새 플레이어:** `SaveManager`는 코인만 든 빈 데이터를 만들고, 기본 게코(하코)·첫 먹이는 `PlayerRepository.EnsureStarterGecko()`가 준다 (저장 손상으로 게코가 0마리일 때도 같은 경로). 게코 생성은 기본·분양 모두 `GeckoData.CreateNew`. 배경·바닥 기본값은 `TerrariumData.DEFAULT_BACKGROUND_ID/DEFAULT_FLOOR_ID`이며, 빈 값인 예전 저장 파일은 `TryMigrate`가 채운다.

**첫 실행 부화 연출:** 새 게임 첫 홈 화면에서 한 번만 — 게코 자리에 알(해츨링 아이콘 그림, `Resources/Fx/egg`로 교체 가능)이 놓이고, 화면을 3번 두드리면(8초 안 누르면 스스로) 금이 가다 깨지며 게코가 튀어나와 인사한다. `HomeUIController`가 `GeckoManager.NeedsHatchIntro()`로 판단해 `UI/Fx/HatchIntro`를 실행 중에 만들고, 끝나면 `CompleteHatchIntro()`가 `ProgressData.hatchIntroSeen`을 바로 저장한다. 연출 중에는 화면 전체를 덮어 버튼을 막고, 사건 연출·일일 보상 팝업은 부화 뒤로 미룬다. 저장 버전 4 — 게코가 있는 예전 저장은 본 것으로 친다. 다시 보려면 플레이를 멈추고 저장 파일(`player_data.json/.bak/.tmp`)을 지운다. 분양한 게코는 알에서 시작하지 않는다

**저장 타이밍:** 먹이/물 사용, 구매, 장식 적용, 앱 시작 보정 후, `OnApplicationPause`(진입·복귀 모두), 종료. 매 프레임 저장 절대 금지.
실행 중 30초 주기 시간 진행은 **저장하지 않는다** — 상태값과 `lastUpdatedTicks`가 함께 움직여서, 저장 전에 앱이 죽어도 다음 실행 때 파일 기준으로 다시 계산돼 결과가 같다.

## 게코 상태 시스템

| 상태값 | 시간당 감소 | 위험 기준 | 0 도달 시 |
|--------|------------|----------|----------|
| Hunger | -4/h [TBD] | 30 이하 경고 | Health -1/h |
| Thirst | -5/h [TBD] | 30 이하 경고 | Health -1/h |
| Cleanliness | -0.67/h | 20 이하 | Mood -0.5/h |
| Mood | -1/h + 연쇄 | 25 이하 | — |
| Health | 배고픔·목마름이 **둘 다 50 초과인 동안 +0.5/h 회복** [TBD] (0이면 -1/h) | 20 이하 위험 | 주버나일 → 서브어덜트 성장 조건 50 |

**오프라인 진행:** `TimeManager.ClampOfflineProgress(hours)` 필수 적용 (상한 48h [TBD]). `DateTime.UtcNow` 사용 (로컬 시간대 조작 방어).

**허물 판정 (`TryMolt`):** `moltProgress >= 100` 시 발동. 기본 성공률 70%, thirst > 50 이면 +15%, health > 60 이면 +10%. 실패 시 moltProgress를 0이 아닌 30으로 리셋 (강한 패널티 금지).
허물 진행 속도: **첫 허물 8.33/h** (12시간 — 첫날 안에 큰 이벤트) → 이후 1.39/h (3일 주기). `[TBD]` (2026-09-17 A안, 예전 1.67/h → 0.20/h)

**시간 보정:** `GeckoManager.ApplyElapsedProgressAll()` 하나로 처리 — 앱 시작, 백그라운드 진입·복귀(`OnApplicationPause`), 실행 중 30초 주기(`AppBootstrap.Update`). 시계를 과거로 돌리면(경과 ≤ 0) 진행하지 않는다.

**돌봄 제한** — 돌봄 메서드는 `CareResult`(Done / Refused / Annoyed / Failed)를 돌려주고, UI는 이 값으로 게코 반응을 고른다.

| 행동 | 제한 `[TBD]` | 결과 |
|------|------|------|
| `FeedGecko` | hunger ≥ 95 → Refused, **아이템 차감 안 함** (hungerRestore 0인 영양제는 예외) | 거절 동작 + 말풍선 |
| `GiveWater` | thirst ≥ 95 → Refused | 거절 동작 + 말풍선 |
| `Clean` | cleanliness ≥ 95 → Refused | 말풍선만 |
| `Pet` | 연달아 4번까지 Done, 8초에 1회분 회복. 넘으면 Annoyed (애정도 변화 없음, 기분 -2) | 꼬리 튕기기 + 말풍선 |

**성장·허물 사건 연출:** `GeckoManager` 이벤트 → `GeckoEventQueue`(최대 8개) → `HomeUIController.EventPresenter`가 팝업·다른 동작이 끝나길 기다렸다가 하나씩 → `GeckoAnimatorController.PresentEvent` + `GeckoFx` + 결과 알림.
UI에서 `OnGrowthUp`/`OnMoltSuccess`/`OnMoltFail`을 직접 구독하지 않는다 (부팅 중 사건을 놓치고, 연출끼리 겹친다). 성장 사건이 남아 있으면 홈 진입 시 게코 크기·단계 이름을 성장 전으로 보여줬다가 연출 때 바꾼다.

**성장 조건 (2026-09-17 A안 — 어덜트까지 약 2주):** 베이비 1일 · 주버나일 3일 + 허물 1회 · 서브어덜트 7일 + 허물 2회 + 건강 50 · 어덜트 14일 + 허물 3회 + 애정도 60 (날짜는 먹이 성장치로 최대 30% 앞당김). 허물이 0.5 · 3.5 · 6.5 · 9.5일에 일어나 각 단계 날짜에 허물 횟수가 딱 맞게 채워진다 — 날짜나 허물 속도를 바꿀 때는 둘을 함께 본다. 계산은 `GeckoManager.CheckGrowth` 한 곳 — 판정(`EvaluateGrowth`)과 화면(`GetGrowthCheck`)이 같이 쓴다. 홈 왼쪽 위 **성장 단계 글자를 누르면** `HomeUIController.DescribeGrowth`가 다음 단계 조건을 말풍선으로 보여준다 ("건강 50 - 부족 (지금 10)"). 조건을 바꾸면 말풍선도 저절로 따라간다.

**어덜트 (마지막 단계, `GeckoManager.ADULT_STAGE`):** 도달하는 순간 한 번 **코인 +500 · 젬 +5** (`ADULT_REWARD_*` [TBD], 가고일 분양가와 같게 — 바로 새 친구를 들일 수 있게)와 `ProgressData.adultCount` 기록. 연출은 성장 연출 + 하트·반짝이, 알림 "하코가 다 자랐어요! 코인 +500 젬 +5", 끝나면 "새 친구도 키워 볼까요?" + 하단 게코 탭이 통통 튄다. 어덜트는 성장치가 쌓이지 않고(먹이·허물 보너스 모두) 먹은 뒤 말풍선·선반에 "성장 +N"이 없다. **성장 말고 다른 효과가 없는 먹이(성장촉진제)는 `IsUselessFood` → 선반 "필요 없음"(아이콘 흐리게), 주면 거절·재고 유지·"다 자라서 필요 없어요"**. 허물은 계속 일어난다. 게코 목록 슬롯은 "어덜트 - 다 자람". 이미 어덜트였던 저장에는 보상을 소급하지 않는다. 자연사(900일) 판정은 어덜트에게 일어나지 않는다 (결정 필요 항목)

**오늘의 돌봄 목표 (2026-09-17):** 하루(UTC 날짜 — 일일 보상과 같은 기준)마다 **먹이 2 · 물 2 · 쓰다듬기 3 · 청소 1**을 채우면 **코인 +100** (`RewardManager.GoalTarget`·`GOAL_REWARD_COIN` [TBD]). 실제로 한 돌봄만 센다 — `GeckoManager.OnCareDone`(거절·삐짐 제외)을 `AppBootstrap`이 `RewardManager.RecordCare`에 연결, 목표를 넘긴 돌봄은 세지 않는다. 진행은 `PlayerData.dailyGoal`(`DailyGoalData`, 날짜가 바뀌면 `RewardManager`가 새로 만든다). 화면: 일일 보상 팝업 카드 아래 `DailyGoalCard`(실행 중 생성, 보상 카드·받기 버튼 모양을 따라 함), 목표 하나를 채우면 결과 알림 "오늘의 돌봄: 쓰다듬기 3/3 완료", 모두 채우면 알림 + 하단 보상 탭 통통. 시간 건너뛰기로 하루 넘게 건너뛰면 목표도 새로 시작

**게코 직접 만지기 (2026-09-17):** `UI/Gecko/GeckoTouch` — 게코 옆(GeckoArea 안, 게코 오브젝트는 하위 캔버스라 그 안의 그림은 터치에 안 잡힌다)에 투명 터치 영역을 두고 게코 위치·크기·**회전**을 매 프레임 따라간다. 부위 8곳은 **파츠의 실제 사각형 안인지**(`GeckoRig.TryPartLocal`)로 정한다 — 순서 눈 → 입 → 앞다리 → 뒷다리 → 머리 → 꼬리 → 몸통, 파츠마다 여유(`GeckoTouch.ORDER`: 눈 20% · 입 25% · 다리 15% · 꼬리 5%, **이웃 부위를 덮지 않게 그림 크기로 계산한 값** — 그림을 바꾸면 자가 검사 `TestTouchAndMovement`로 확인), 꼬리는 그림 안 가로 0.55 이상이 뿌리(지금 그림은 관절이 오른쪽 끝).
반응 (`HomeUIController.OnGeckoTouched`, 머리 외에는 수치 변화 없음, 다른 동작 중이면 무시):

| 부위 | 반응 (둘 중 무작위) |
|------|------|
| 머리 | 쓰다듬기 (같은 효과·피로·오늘의 목표, 벽에서도) |
| 눈 | `Tongue_EyeLick` "눈 닦는 중~" / `Refuse` "눈은 안 돼!" |
| 입 | `Tongue_Lick` "냠?" / `Yawn` "하아암~" — 배고픔 60 미만이면 60%로 "배고파요!" + 먹이 버튼 통통 |
| 앞다리 | `Wave` "안녕!" / `PawShake` "발 만지지 마~" |
| 뒷다리 | `Jump` "간지러워!" / `Kick` "뒷발 차기!" |
| 몸통·등 | `Surprise` "앗!" / `Shiver` "부르르" |
| 꼬리 뿌리 | `Angry_TailFlick` + 김 "꼬리 건드리지 마" |
| 꼬리 끝 | **도망** `GeckoMovementAI.Flee` + 김 "꼬리는 안 돼!" |

먼저 걸리는 규칙: 3초 안에 5번 연타 → 꼬리 튕기기 + 삐짐 + 도망 · 벽에 매달려 있음 → `Surprise` "깜짝이야!" + 도망(더 위로 / 후다닥 내려옴) · 졸림 60% → `Yawn` "음냐..." · 화남 70% → 꼬리 튕기기 "건드리지 마!". 달아나는 중에는 게코를 눌러도 무시. 반응 문구 키는 `HomeUIController.TouchLineKeys`(자가 검사가 번역표 확인). 부화 연출·먹이 선반·팝업이 열려 있으면 그쪽이 위에 있어 자연히 막힌다

**게코 이동 (`GeckoMovementAI`, 2026-09-17):** 발 높이 `groundBand` **380~950 (씬 값)** 안에서 앞뒤·대각선으로 다니고, 멀수록 작고 느리게(`farScale` **0.62**, 씬 값 — 코드 기본값만 바꾸면 적용되지 않는다). 쉬고 나서 30%로 **벽 타기**: 게코 오브젝트를 발밑 기준 ±90° 돌려(머리가 위) 400~800 오르고 2~4초 매달렸다가 머리를 아래로 돌려 내려와 눕는다. 오르는 한계 = 영역 높이 − 420 − 몸 길이(상태 띠 아래), 오르는 동안 원근 크기는 출발한 바닥 높이 기준, `GeckoMotor.SetClimbing`이 그림자를 숨기고 다리를 벌린다. 도망: 바닥은 누른 곳 반대쪽으로 2.5배 속도 300~400(앞뒤 −150~+250 비껴감) 후 뒤돌아봄, 벽은 위로 200~320 더 달아나거나 꼭대기면 후다닥 내려옴. 벽에 붙은 채 꺼지면 바닥·각도 0으로 복구. 수치는 모두 Inspector [TBD]

**알림 권한 요청 시점:** 앱 시작 시(알림 켜짐) — 단 새 게임은 부화 연출과 "태어났어요" 알림이 끝난 뒤(`HomeUIController.OpenRewardAfterResult`), 그다음 일일 보상 팝업. 설정에서 알림을 켤 때도 요청

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
    public RuntimeAnimatorController animController;   // 사용하지 않음
    public int coinPrice;
    public bool isUnlockedByDefault;
    public GeckoSkin skin;     // 종 전용 그림 (비우면 씬 게코의 기본 그림)
    public bool canBlink;      // 눈꺼풀 있는 종만 (leopard = true)
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
| `UI/Gecko/GeckoMotor.cs` | 호흡·꼬리 물리·걷기·벽 타기 자세·표정·동작 21종 계산. 수치는 Inspector `[TBD]`. 연출 타이밍 상수(`FEED_*`, `DRINK_*`)와 `ActionStarted` 이벤트 공개 |
| `UI/Gecko/GeckoBendGraphic.cs` | 휘어지는 꼬리 메시 (UI) |
| `UI/Gecko/GeckoPose.cs` | 한 프레임 자세 데이터 |
| `Models/GeckoSkin.cs` | 그림 한 벌 (ScriptableObject). **그림 교체 = 이 에셋 교체** |
| `UI/GeckoAnimatorController.cs` | 게임 데이터 연결. 이름은 예전 그대로지만 Animator를 쓰지 않는다 |
| `Domain/GeckoMovementAI.cs` | 테라리움 돌아다니기 (UI 좌표) — 앞뒤 원근, 벽 타기(±90° 회전), 도망 |
| `UI/Gecko/GeckoTouch.cs` | 게코 직접 만지기 — 회전을 따라가는 터치 영역, 파츠 사각형으로 부위 8곳 판정 |
| `Assets/Editor/Gecko/` | `Hako > Gecko` 메뉴, 프록시 그림 생성, 인스펙터 |

**동작 (`GeckoMotor.Play(GeckoAction)`)** — 이름 오타 방지를 위해 enum으로만 호출한다.

| 동작 | 호출 | 길이 |
|------|------|------|
| `Tongue_Lick` | 대기 중 자동 4~8초 (`_lickInterval`). 30% 확률로 `Tongue_EyeLick`으로 바뀜 (`_eyeLickChance`) | 0.55초 |
| `Tongue_EyeLick` | **시그니처** — 혀로 눈 닦기. 크레스티드는 눈꺼풀이 없어 혀로 눈을 닦는다 | 1.5초 |
| `Tongue_FeedCatch` | 먹이 버튼 → `TriggerFeedCatch()` + `GeckoFx.FeedDrop` (먹이가 혀끝에 붙어 들어감) | 1.4초 |
| `Tongue_FeedBig` | 큰 먹이(Big) → `TriggerFeedBig()` — 앞부분은 받아먹기와 같고 뒤에 오래 오물오물 | 2.6초 |
| `Tongue_Drink` | 물 버튼 → `TriggerDrink()` + `GeckoFx.Mist` (분무 + 할짝마다 물방울) | 1.9초 |
| `Pet_Reaction` | 쓰다듬기 버튼 → `TriggerPet()` | 1.6초 |
| `Happy_LookUp` | 청소 버튼 → `TriggerClean()` / 기쁨 기분에서 자동 9~18초 (70%) | 1.3초 |
| `Jump` | 기쁨 기분에서 자동 (30%) | 0.95초 |
| `Angry_TailFlick` | 화남 기분에서 자동 4.5~9초 / 쓰다듬기 과함 → `TriggerAnnoyed()` | 1.0초 |
| `Molt_Start` | 허물 실패 사건 — 실패해도 껍질이 들뜨는 연출 | 1.3초 |
| `Molt_Finish` | 허물 성공 사건 | 1.8초 |
| `LevelUp_Pulse` | 성장 사건 + 성장 단계 크기 전환 | 1.1초 |
| `Refuse` | 배부름·목 안 마름 → `TriggerRefuse()` (고개 젖히고 도리도리) | 1.1초 |
| `Molt_Itch` | 허물 준비 중(≥80) 기분 동작 대신 가끔 (50%, 간격 절반) — 근질근질 | 1.2초 |
| `Surprise` | 첫 실행 부화 연출 — 알에서 튀어나온 순간 (`HatchIntro`). 자동 호출 없음 | 0.8초 |
| `Blink_Short` | 수동 재생 전용. 자동 깜빡임은 `canBlink` 켠 종만 3~7초 (크레스티드 기본 꺼짐) | 0.16초 |
| `Yawn` | 입을 만짐 / 졸릴 때 만짐 — 고개 들고 입 크게, 눈 질끈 | 1.6초 |
| `Wave` | 앞다리를 만짐 — 가까운 앞발을 번쩍 들어 흔든다 | 1.4초 |
| `PawShake` | 앞다리를 만짐 — 앞발을 조금 들어 파르르 | 1.2초 |
| `Kick` | 뒷다리를 만짐 — 가까운 뒷발로 뒤를 휙휙 두 번 | 1.0초 |
| `Shiver` | 몸통을 만짐 — 부르르 (허물 근질근질과 달리 껍질이 안 보인다) | 1.0초 |

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
- 게코는 **UI(하위 Canvas)** 로 그린다 — 홈 화면이 Screen Space Overlay라 월드 스프라이트는 배경에 가려진다 (월드 스프라이트용이던 `DepthObject`·`TerrariumDepthManager`는 2026-09-12에 삭제)
- 성장 단계 크기: `GeckoRig._stageScales` = 0.55 / 0.68 / 0.80 / 0.90 / 1.00. 단계별 전용 그림은 `_stageSkins` (프록시는 ①이 머리·눈을 키운 해츨링·주버나일 비율 스킨을 넣는다)
- 종 전용 그림(`GeckoSpeciesSO.skin`)을 쓰면 단계별 그림은 무시한다 (`GeckoRig.SetSkin(skin, useStageSkins: false)`)
- 색·비율·표정·파츠 규격은 **`ART_GUIDE.md`** 를 따른다

**메뉴 (`Hako > Gecko`)**: ① 프록시 게코 만들기 (MainHome에서) · ② 선택한 PSD·폴더로 스킨 만들기 · ③ 선택한 스킨을 씬 게코에 적용 (프록시 단계별 그림을 비운다). 셋 다 **플레이 중에는 회색** — 씬·에셋을 바꾸는 메뉴라 플레이 중에는 변경이 멈출 때 사라지고 도중에 오류로 끊긴다

**확인**: 플레이 중 Hierarchy에서 `GeckoObject` 선택 → Inspector의 `GeckoMotor` 아래 버튼으로 동작·성장 단계를 하나씩 미리 본다.

## 연출 · 소리 · 진동

| 파일 | 역할 | 교체 방법 |
|------|------|----------|
| `UI/Fx/GeckoFx.cs` | 게코 주변 연출 + 말풍선. HomeUIController가 `Start`에서 GeckoArea 위에 만든다. 위치는 `GeckoRig.PartWorldPoint`로 그 순간의 파츠 위치를 읽는다 | — |
| `UI/Fx/UIParticles.cs` | UI 파티클 (Image 재사용) | — |
| `UI/Fx/FxSprites.cs` | 하트·물방울·반짝이 등을 코드로 생성 | `Resources/Fx/{이름}` |
| `UI/Fx/UIPressScale.cs` | 버튼 눌림·튕김·'톡'. `UIFeelInstaller`가 씬마다 모든 Button에 자동으로 붙인다. **나중에 Instantiate하는 버튼은 `UIPressScale.Ensure(button)`** | — |
| `Core/AudioManager.cs` | `AudioManager.Play(Sfx.X)` — 효과음·배경음은 설정을 따로 따른다 | `Resources/Audio/Sfx/{이름소문자}`, `Resources/Audio/Bgm/home` |
| `Core/SfxSynth.cs` | 파일이 없을 때 쓰는 합성 소리 (순수 계산) | — |
| `Core/Haptics.cs` | `Haptics.Light/Medium/Success()` — Android 짧은 진동, `vibrationOn` 설정 따름 | — |
| `Core/NotificationScheduler.cs` | 로컬 알림 — 백그라운드로 갈 때 "배고파해요"·"오늘의 보상" 예약, 앱을 열면 취소 | Mobile Notifications 패키지 설치 시 동작 (없으면 조용히 건너뜀) |
| `Core/KoreanText.cs` | 이름 뒤 조사 — 하코**가** / 별님**이** | — |

- 효과음·진동은 **UI 계층에서만** 부른다 (Domain·Data는 소리를 모른다)
- **TMP 글꼴은 정적 아틀라스** — `NanumGothic-Regular SDF`에는 한글 11,172자·자모·ASCII만 있고 예비 글꼴도 없다 (`LiberationSans SDF`는 Nanum으로 넘어간다). ★ ♥ → ← ↗ ✕ ⚙ … ✦ 같은 기호와 **이모지**는 □로 나온다 → 코드 문구·**씬 텍스트** 모두 한글·영문·숫자·ASCII 기호만 쓰고, 아이콘은 그림(Image)으로 넣는다
- **배경 그림 위 글자:** 홈은 정글 그림 위에 흰 글자가 놓인다. 판을 깔 수 있으면 반투명 어두운 둥근 판(상태 패널처럼), 판을 못 깔면 굵게 + `HomeUIController.ApplyHudReadability`의 TMP 그림자(underlay, 글꼴당 머티리얼 하나 공유)를 쓴다. 새로 배경 위에 글자를 올리면 같은 처리를 할 것
- Unity 오브젝트에 `?.`를 쓰지 않는다 — 파괴·미연결 오브젝트를 null로 보지 않는다. `x != null ? x : null`로 바꾼 뒤 쓴다 (예: `HomeUIController.Anim`)

## 다국어 (한국어 / 영어)

- **언어 결정:** `SettingsData.language`가 `"ko"`·`"en"`이면 그것, 비어 있으면(기본) **기기 언어** — 한국어 기기면 한국어, 그 밖에는 영어. `AppBootstrap`이 설정 적용 직후 `Loc.Init` (기본 게코 이름보다 먼저)
- **번역표는 `Core/Loc.cs` 한 곳.** 화면에 보이는 문구를 새로 넣으면 **반드시 표에 한국어·영어를 함께 추가**하고 `Loc.Get` / `Loc.Format` / `Loc.Pick`(`|`로 나눈 여러 문구 중 하나)으로 부른다. `Debug.Log`는 번역하지 않는다
- **씬 글자에는 번역 컴포넌트를 붙이지 않는다.** `SceneTextLocalizer`가 씬이 열릴 때 **번역표 원문(한글 또는 영어)과 똑같은 TMP 글자**를 현재 언어로 바꾼다 → 씬 글자를 고칠 때는 번역표의 원문과 똑같이 적는다. 나중에 Instantiate하는 프리팹의 고정 글자는 `SceneTextLocalizer.LocalizeUnder(gameObject)` (예: `ItemSlotUI`)
- **게코 이름처럼 사용자가 정한 글자는 `SceneTextLocalizer.Ignore(text)`로 뺀다** (글자를 채우기 전에, `OnEnable`·`Awake`에서). 빼지 않으면 이름이 번역표 원문과 같을 때 바뀐다 — 영어에서 "하코" → "Hako", "먹이" → "Feed". 지금 적용: `HomeUIController._geckoNameText`, `GeckoSlotUI._nameText`
- 원문이 같은데 뜻이 다르면 어느 쪽으로 바꿀지 모호하다 → 씬 글자를 다르게 적는다 (예: 청소 버튼 `Clean` / 청결 게이지 `Cleanliness`)
- 아이템·장식·종 이름: 키 `item.{id}` · `decor.{id}` · `species.{id}`, 표에 없으면 에셋의 `displayName` (`Loc.ItemName/DecorName/SpeciesName`)
- 이름 + 조사: `Loc.Subject(name)` — 한국어 "하코가", 영어 "Hako". 게코 이름 자체는 사용자 데이터라 번역하지 않는다 (기본 게코는 생성 시점 언어로 "하코"/"Hako")
- 저장 버전 3: 예전 저장의 `language = "ko"`는 고른 값이 아니라 기본값이었으므로 로드 때 비워 기기 언어를 따르게 한다
- 번역표의 모든 글자는 `NanumGothic-Regular SDF` 아틀라스에 있어야 한다 — 자가 검사 `TestLocalization`이 누락·`{0}` 자리 수·원문 겹침·글꼴 글자를 확인한다
- **영어 화면 확인:** 플레이 중 메뉴 `Hako > 검사 > 언어 > 영어` (설정 저장 + 씬 다시 열기). 확인 후 `기기 언어`로 되돌린다. 설정 화면의 언어 선택 UI는 아직 없다 (`SettingsManager.SetLanguage`만 있음)

## 주요 컨벤션 & 주의사항

- **모든 텍스트는 TextMeshPro** (UI Text 사용 금지)
- **꾸미기 자유 드래그 배치는 MVP 절대 금지** (슬롯 방식만)
- **먹이 (2026-09-15 MVP에 포함 — 사용자 결정):** 먹이 버튼 → `FoodTray`(실행 중 생성하는 선반)에서 가진 먹이를 골라 준다. 마지막으로 준 먹이(`PlayerData.lastFoodItemId`)가 맨 앞, 선반 밖을 누르면 닫힘, 먹이가 없으면 상점으로
  - 먹이별 효과는 `ItemSO` 에셋: 배고픔·기분·건강·성장치·`moltBonus`(다음 허물 1회 성공률, 최대 +15%)·`kind`(Normal / Big / Supplement)·`preferredSpeciesIds`
  - **성장 가속:** 성장치 1 = 성장 일수 3시간, 필요한 실제 날짜의 **최대 30%**까지 (`GeckoManager.EffectiveAgeDays`). 성장치는 단계가 오르면 0
  - **좋아하는 먹이:** 크레스티드 = 밀웜 · 레오파드 = 두비아 · 가고일 = 슈퍼밀웜 → 애정도 2배, 기분 +3, 기뻐하는 반응
  - **반응:** Normal = 혀로 받아먹기 · Big = `Tongue_FeedBig`(받아먹고 오래 오물오물) · Supplement = `GeckoFx.Dust` + 할짝 · 좋아하는 먹이 = 받아먹은 뒤 `Happy_LookUp` + 하트. 먹은 뒤 실제 효과(`FeedEffect`)를 말풍선으로 ("성장 +5  기분 +6")
  - 성장촉진제·영양제는 배고픔을 채우지 않으므로 배불러도 먹는다 (칼슘은 배고픔 +5라 95 이상이면 거절)
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

**추가 설치 필요:** TextMeshPro (TMP Essentials), **Mobile Notifications** (알림을 쓰려면 — Window → Package Manager → Unity Registry에서 설치. 코드는 리플렉션으로 부르므로 없어도 컴파일은 된다), Newtonsoft JSON (선택)

## 현재 진행 상태

작업 시작 전 반드시 `DEV_LOG.md`를 읽어 현재 단계와 남은 작업을 확인할 것.
