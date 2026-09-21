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
- **꾸미기 구조물 임시 그림:** 같은 방식으로 `-executeMethod DecorProxyArt.GenerateBatch` — `Textures/Decor/decor_cork·decor_vine·decor_branch·decor_moss_rock·decor_cave·decor_driftwood.png`와 `Resources/Decor` 장식 에셋(놓는 곳·쓰임·아래 여백·어덜트 조건)을 만들고, 기존 장식에 놓는 곳·쓰임·아래 여백을 채운다. **이미 있는 PNG는 덮어쓰지 않는다** (최종 그림 보호 — 다시 그리려면 PNG와 `.meta`를 지우고 실행). 메뉴는 없다
- **하단 탭 아이콘 임시 그림:** 같은 방식으로 `-executeMethod NavIconArt.GenerateBatch` — `Resources/Icons/tab_store·tab_gecko·tab_terrarium·tab_reward·tab_settings.png` 5장(96×96, 흰 실루엣 — 탭 바가 어두운 반투명이라). **이미 있는 PNG는 덮어쓰지 않는다.** `HomeUIController.EnsureNavButtonIcons`가 실행 중에 탭의 빈 `Emoji` 칸에 넣는다 (이모지·기호는 글꼴 아틀라스에 없어 □로 나온다). 최종 그림은 같은 이름·같은 크기로 바꿔 끼우면 된다. 메뉴는 없다
- **정글 테마 임시 그림:** 같은 방식으로 `-executeMethod ThemeProxyArt.GenerateBatch` — 예전 정글 배경(잎사귀 벽) 아래쪽에 코드로 그린 흙 바닥(반복 없는 잡음 흙 결 · 벽 밑동 이끼 · 멀수록 작은 자갈 · 벽에서 늘어진 풀)을 원근으로 깔아 `Textures/Backgrounds/theme_jungle.png`를 만들고 `bg_jungle` 테마가 쓰게 한다. **이미 있는 PNG는 덮어쓰지 않는다** — 다시 만들려면 PNG만 지운다(.meta를 두면 연결 유지). 최종 그림은 같은 이름으로 덮어쓰기. 메뉴는 없다
- **시간 건너뛰기:** 플레이 중 메뉴 `Hako > 검사 > 시간 건너뛰기`. `GameManager.DebugSkipTime(hours, caredFor)`(에디터 전용)이 기준 시각을 과거로 옮긴 뒤 평소 시간 보정 경로로 반영한다
  - **+6시간 · +24시간 (내버려 둠)** — 한 번에 반영, 오프라인 상한 48h 적용. 게이지 감소·경고 확인용 (24시간은 배고픔이 바로 0이 되므로 30 이하 경고는 6시간을 두세 번 눌러 본다)
  - **+7일 · +2주 · +30일 (잘 돌봄)** — 8시간씩 나눠 진행하며 구간마다 배고픔·목마름·청결·기분을 100으로 채운다. 한 번에 반영하면 48h 상한 때문에 한 달을 건너뛰어도 허물은 2일치만 진행되므로, 나이·허물·성장·일일 보상이 기간만큼 실제 순서대로 일어나게 나눈다. 애정도·건강은 직접 채우지 않는다 — 건강은 회복 규칙(+0.5/h)대로 오르고, 어덜트 조건 애정도 60은 쓰다듬기로
- **테스트 재화:** 플레이 중 메뉴 `Hako > 검사 > 재화` (코인 +1,000 / 코인 +10,000 / 젬 +100). `GameManager.DebugAddCurrency`(에디터 전용)가 더하고 바로 저장한다. 홈 윗줄은 바로 카운트업, 상점·꾸미기 화면은 나갔다 들어오면 반영
- **테스트 유대:** 플레이 중 메뉴 `Hako > 검사 > 유대` (다음 레벨까지 / 점수 +20 / 점수 +100 / 최고 레벨 / 처음으로). 홈에 있는 게코 대상, `GameManager.DebugAddBond`·`DebugBondNextLevel`·`DebugResetBond`(에디터 전용) — 애정도를 먼저 채우고 나머지는 넘친 몫에 바로(하루 한도 무시), 레벨이 오르면 평소처럼 보상 + 레벨업 연출, 바로 저장. "처음으로"는 보상 받은 레벨도 0이라 다시 오르면 보상도 다시. Lv.1 인사는 홈에 다시 들어올 때 나온다
- **홈 상태 게이지 · 허물 진행 막대:** `StatusPanel/*Bar/Fill`과 `MoltProgressFill` Image는 **Filled · Horizontal + 스프라이트 지정**이어야 한다. Simple이거나 **스프라이트가 비어 있으면 `fillAmount`가 무시되고 사각형 전체가 그려진다** (uGUI `Image.OnPopulateMesh`). 씬에는 기본 `UISprite`를 넣어 두었고, `HomeUIController.MakeFillable`이 실행 시 한 번 더 보정한다. 평소 색은 `HomeUIController.GAUGE_*`, 막대 오른쪽 위 숫자는 `GaugeView`가 실행 중에 만든다
- **홈 화면 배치 (1080×2400 기준):** 위 — 이름·성장 단계(왼쪽), **코인 → 젬**(오른쪽), 허물 막대, 상태 띠(가로 5칸: 아이콘 + 막대 + 숫자, 이름 글자 `Label`은 꺼 둠). 아래 — 둥근 돌봄 버튼 4개(위 아이콘 + 아래 글자, 내비 바 바로 위). **가운데는 게코 공간으로 비워 둔다** — 새 UI를 가운데에 올리지 않는다. 버튼 아이콘은 `HomeUIController._careButtonIcons`(지금은 32px 상태 아이콘 재사용)를 실행 중에 붙인다
- **Game 창 맞추기:** 메뉴 `Hako > 화면 > 게임 화면 맞추기 (1080x2400)` (`Editor/HakoGameViewFit`) — Game 창을 1080×2400 세로 고정 해상도로 고르고 확대를 화면에 딱 맞게 되돌린다. **Game 창 위에서 Ctrl/Alt + 휠을 굴리면 확대되고, 그 상태가 Unity를 다시 켜도 남아** 이름·코인·젬·돌봄 버튼·하단 탭이 화면 밖으로 잘린다 (2026-09-21 — 씬·코드는 멀쩡했다). 스크립트가 처음 컴파일될 때 한 번 저절로 실행된다
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

**테마 (2026-09-21 배경·바닥을 합쳤다):** 테마 = 뒷벽과 바닥이 한 장에 그려진 배경 그림 (`backgroundId`, 아이디는 예전 배경 그대로 `bg_*`). 바닥 띠(`TerrariumFloor`, 높이 150)는 하단 탭·돌봄 버튼에 가려 거의 안 보였고 게코는 배경 그림 위를 걸었으며, 섞으면 사막 배경 + 정글 흙처럼 어긋났다 → 꾸미기 화면의 바닥 탭은 숨기고(`TerrariumUIController.ApplyThemeTabs`, 배경 탭 글자는 "테마"), 홈은 바닥 띠를 끈다. `floorId`는 예전 저장을 읽으려고 필드만 남긴다. 저장 버전 10 — 산 유료 바닥(`TerrariumData.RETIRED_FLOORS`, 나무판 80)은 코인으로 돌려준다. 테마 그림 규격은 `ART_ORDER_GECKO.md` 8-1 (땅 0~1060, 게코 발 380~950이 땅 위)
**TerrariumData.ownedDecorIds:** 산 테마 (다시 골라도 결제 안 함). 장식은 놓을 때마다 결제. 예전 저장 파일은 `SaveManager.TryMigrate`에서 빈 목록으로 보정.
**TerrariumData.decorSlots[7]:** 0·1·4·5 바닥 칸, 2·3·6 뒷벽 칸 (`TerrariumLayout.PlacementOf` — 예전 4칸 저장의 번호는 그대로, 앱 시작 `NormalizeSlots`가 배열을 7개로 늘린다) — 장식은 종류가 맞는 칸에만 놓인다 (아래 "꾸미기 구조물").

**PlayerData:** `coin`, `gem`, `List<GeckoData> geckos`, `List<string> ownedItemIds`, `selectedGeckoId`, `TerrariumData`, `DailyRewardData`, `ProgressData`, `SettingsData`, `saveVersion`

**저장 파일 구조 (tmp → json 교체):**
| 파일 | 역할 |
|------|------|
| `player_data.json` | 메인 저장파일 |
| `player_data.tmp` | 저장 중 임시 — 끝까지 쓰고 디스크에 확정(`Flush(true)`)한 뒤에만 메인을 교체 |
| `player_data.bak` | 직전 정상 백업본 |

**읽기 순서 (`SaveManager.Load`):** 메인 → (메인이 없으면) 임시 → 백업 → 새 데이터. 메인이 없고 임시만 있다 = 저장 도중 멈춘 것이므로 임시가 가장 최신이다.

**새 플레이어:** `SaveManager`는 코인만 든 빈 데이터를 만들고, 기본 게코(하코)·첫 먹이는 `PlayerRepository.EnsureStarterGecko()`가 준다 (저장 손상으로 게코가 0마리일 때도 같은 경로). 게코 생성은 기본·분양 모두 `GeckoData.CreateNew`. 테마 기본값은 `TerrariumData.DEFAULT_BACKGROUND_ID`(정글)이며, 빈 값인 예전 저장 파일은 `TryMigrate`가 채운다.

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
구간 안에서 **언제부터인지**를 줄어들기 전 값으로 먼저 재고 그만큼만 반영한다 (2026-09-20) — 건강은 배고픔·목마름이 **0이 된 뒤 시간만큼만** −1/h, 기분 추가 패널티는 청결이 **20 아래가 된 뒤 시간만큼만** −0.5/h, 건강 회복은 둘 다 50을 넘는 동안만 +0.5/h. (예전에는 구간 끝 상태로 판단해 경과 시간 전체를 뺐다 — 80에서 24시간 방치 시 −8이어야 할 건강이 −24)

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

**어덜트 (마지막 단계, `GeckoManager.ADULT_STAGE`):** 도달하는 순간 한 번 보상 (`GeckoManager.AdultReward`) — **그 종의 첫 어덜트만 코인 +500 · 젬 +5** (`ADULT_REWARD_*` [TBD], 가고일 분양가와 같게 — 바로 새 친구를 들일 수 있게), 같은 종 두 번째부터는 **코인 +100** (`ADULT_REWARD_REPEAT_COIN` [TBD]). 받은 종은 `ProgressData.adultSpeciesIds`(저장 버전 5 — 예전 저장의 어덜트 종은 받은 것으로 기록), 마릿수는 `adultCount`. 실제 금액은 `GeckoEvent.rewardCoin/rewardGem`에 담겨 알림에 쓰인다. 연출은 성장 연출 + 하트·반짝이, 알림 "하코가 다 자랐어요! 코인 +500 젬 +5"(젬 0이면 코인만), 끝나면 "새 친구도 키워 볼까요?" + 하단 게코 탭이 통통 튄다. 어덜트는 성장치가 쌓이지 않고(먹이·허물 보너스 모두) 먹은 뒤 말풍선·선반에 "성장 +N"이 없다. **성장 말고 다른 효과가 없는 먹이(성장촉진제)는 `IsUselessFood` → 선반 "필요 없음"(아이콘 흐리게), 주면 거절·재고 유지·"다 자라서 필요 없어요"**. 허물은 계속 일어난다. 게코 목록 슬롯은 "어덜트 - 다 자람". 이미 어덜트였던 저장에는 보상을 소급하지 않는다. 자연사는 MVP에서 다루지 않는다 (2026-09-21 결정 — 나이만으로 게코가 사라지지 않는다. 되살릴 자리는 `GeckoManager` 성장 상수 옆 주석)

**어덜트의 선물 (2026-09-17, `RewardManager.CanGift/ClaimGift`):** 어덜트이고 배고픔·목마름·청결·기분·건강이 **모두 50 초과**면 게코마다 하루(UTC) 한 번 홈 바닥 앞쪽(게코·바닥 장식에서 200 떨어진 곳)에 금색 선물 상자(`FxSprites.Gift` + 도는 반짝이, `HomeUIController.RefreshGift`)가 놓인다. 누르면 **코인 20~40**, 20%로 먹이 1개(귀뚜라미·밀웜) (`GIFT_*` [TBD] — 5마리면 하루 최대 200) + 반짝이·기뻐하기·"선물이야!" + 결과 알림. 받은 날은 `GeckoData.giftDay`. 상태가 50 이하로 떨어지면 상자가 사라진다(`Refresh`마다 확인). 홈에는 선택 게코의 선물만 — 다른 게코는 게코 목록 슬롯에 급한 일이 없을 때 **"선물이 있어요"**(금색). 시간 건너뛰기로 하루 넘게 건너뛰면 선물도 새로

**어덜트 전용 장식 (2026-09-17):** `DecorItemSO.requiredAdults` — 키운 어덜트 수(`ProgressData.adultCount`, 같은 종도 셈)가 모자라면 꾸미기 목록에서 아이콘이 흐리고 가격 자리에 "어덜트 N마리", 누르면 "어덜트를 N마리 키우면 열려요"(값을 받기 전에 확인, `TerrariumManager.IsUnlocked`). 어덜트가 되는 사건 연출 끝에 새로 열린 장식을 알리고 꾸미기 탭이 통통 (`GeckoEvent.adultsRaised` → `TerrariumManager.NewlyUnlocked`). 저장 버전 6 — 예전 저장은 `adultCount`를 지금 어덜트 게코 수 이상으로 올린다.

| 장식 | 칸 · 쓰임 | 조건 | 가격 [TBD] |
|------|------|------|------|
| 이끼 바위 `decor_moss_rock` | 바닥 · 허물 준비 때 몸 비비기 (허물 +10%) | 어덜트 1 | 60 |
| 동굴 `decor_cave` | 바닥 · 은신처 (크기는 집과 같은 460) | 어덜트 2 | 120 |
| 큰 유목 `decor_driftwood` | 뒷벽 · 나뭇가지 경로 (`BRANCH_LINE` 그대로, 굵기도 같게) | 어덜트 3 | 150 |

**유대 레벨 (2026-09-17, `Domain/GeckoBond.cs`):** 게코마다 **유대 점수 = 애정도(0~100) + 넘친 몫**(`GeckoData.bondOverflow`). 돌봄의 애정도(쓰다듬기 3 · 먹이 2/좋아하는 먹이 4 · 물·청소 1)는 `GeckoManager.AddAffection` → `GeckoBond.AddAffection`이 100까지는 애정도, 넘는 몫은 **하루 30까지**(`DAILY_OVERFLOW_CAP`, `bondDay`·`bondToday`) 넘친 몫에 더한다. 레벨이 `bondRewardedLevel`보다 오르면 `CheckBondLevel`이 그 사이 보상을 모두 주고 `OnBondLevelUp` → 사건 대기열 `BondUp`(레벨·보상) → 하트·반짝이·기뻐하기 + "하코가 마음을 열었어요! 유대 Lv.3 / 빈 바닥을 두 번 톡톡 하면 다가와요  젬 +2". 수치 [TBD]:

| 레벨 | 점수 | 보상 | 풀리는 것 (`BondPerk`) |
|:---:|:---:|------|------|
| 1 | 20 | 코인 20 | **인사** — 홈에 들어오면 사건이 없을 때 한 번 `Wave` + "왔구나!" (`TryGreet`) |
| 2 | 50 | 코인 50 | **쓰다듬기 좋아함** — 연달아 좋아하는 횟수 4 → 6 (`GeckoBond.PetLimit`), 하트 한 번 더 |
| 3 | 100 | 젬 2 | **부르기** — 홈 바닥의 빈 곳(게코·장식 뒤의 투명 판 `FloorTapCatcher`, 높이 = 다니는 바닥 + 80)을 0.4초 안에 두 번 톡톡 → `GeckoMovementAI.CallTo`: 집·벽이면 먼저 나와서 1.6배 걸음으로 와 `Arrived` → 올려다보기 + "나 불렀어?" |
| 4 | 180 | 코인 100 | **재롱** — 쓰다듬기 25%로 `Spin` + "봐봐!" |
| 5 | 300 | 젬 5 | **손바닥** — 게코를 0.6초 길게 누르면(`GeckoTouch.LongPressed`, 이어지는 부위 반응은 건너뜀) 바닥에 있을 때만 `Hold` → 아래에서 손(`FxSprites.Hand`, 교체 `Resources/Fx/hand`, 손바닥 윗면이 그림 높이 48.5%)이 올라와 게코가 폴짝 → 140 들어 올려 3초 흔들 + 하트 "따뜻해~" → 내려놓고 `Release`. 들린 동안 원근 크기 고정, 도망·부르기·편집 모드 무시, 손은 겹침 순서에서 게코 바로 뒤 |

- 화면: 홈 성장 단계 글자 오른쪽 끝에 분홍 **"유대 3"**(`RefreshBondLabel` — 글자 길이에 맞춰 옮김, 누르면 `DescribeBond` 말풍선 "유대 Lv.3 / 다음 Lv.4 (120/180) / 풀린 것: ... / 오늘은 충분히 친해졌어요"). 게코 목록 카드는 "어덜트 - 다 자람  유대 3"(Lv.1부터). 업적 "단짝"(유대 Lv.5, 젬 10)
- 저장 버전 8 — 예전 저장은 지금 애정도로 정해지는 레벨을 보상 없이 받은 것으로 (`bondRewardedLevel`)
- 편집 모드에서는 바닥 판을 끄고, 손바닥 연출 중에는 장식 길게 누르기를 무시한다

**모프 (2026-09-17, `Domain/GeckoMorph.cs`):** 어덜트가 되는 순간(`EvaluateGrowth`) 등급을 뽑아 `GeckoData.morphId`를 정한다 — **흔함 70 · 희귀 25(두 모프가 나눔) · 아주 희귀 5** (`RARITY_WEIGHT` [TBD]). 처음 얻은 모프는 도감(`ProgressData.morphIds`)에 기록하고 **흔함 코인 50 · 희귀 코인 150 · 아주 희귀 젬 5** (`FIRST_REWARD` [TBD]). `OnGrowthUp` 바로 뒤 `OnMorphRevealed` → 사건 `MorphReveal`(성장 연출 다음) → 반짝이 + "하코의 무늬가 드러났어요! / 할리퀸 (희귀)  새 모프! 코인 +150" + 기뻐하기. 모프 연출 전·어덜트 전에는 **종별 기본색**(크레스티드 그대로 · 레오파드 노랑 · 가고일 회갈색, `BaseColor`) — `GeckoAnimatorController.ApplyMorph`가 `GeckoMorph.LookOf` + 대기열 `HasPending`으로 고른다.
- 모프 표 `GeckoMorph.ALL` (종마다 4개): 크레스티드 노멀 · 할리퀸 · 레드 · 달마시안 / 레오파드 노멀 · 탠저린 · 알비노 · 블리자드 / 가고일 노멀 · 레드 스트라이프 · 오렌지 얼룩 · 화이트. 이름 `morph.{id}`, 등급 `morph.rarity.N`
- **임시 모습 (`GeckoRig.SetMorph`):** 몸 파츠 7개(꼬리·다리·몸통·머리)에 색을 **곱하고**(`_tint × _morphMul`), 무늬(점 · 큰 얼룩 · 띠)는 몸통·머리 위에 `MorphDot` 이미지를 얹어 파츠 그림 모양(`Mask`)으로 자른다. 점 자리는 게코 id 씨앗(`SeedOf`)으로 고정, 파츠 자식이라 함께 움직인다. 꼬리는 휘는 그림이라 색만. 프록시가 이미 색이 있는 그림이라 밝은 모프(알비노·블리자드·화이트)는 비슷하게만 보인다 — **최종 그림이 오면 모프 전용 그림으로 바꾼다**
- 화면: 도감 종 줄에 모프 칸 4개(등급 색 칩 · 색 점 · 이름, 못 얻으면 ???) + "모프 2/4". 게코 목록 어덜트 카드 "어덜트 - 할리퀸". 업적 "모프 수집가"(6종, 젬 10)
- 저장 버전 9 — 예전 저장의 어덜트는 모프를 정하고(게코 id 씨앗) 도감에 기록, 보상·알림 없음
- 테스트 메뉴 `Hako > 검사 > 모프` — 다음 모프로(그 종 모프를 차례로) / 다시 뽑기(확률대로). 선택 게코가 어덜트일 때만, 도감 기록 · 보상 없음 (`GameManager.DebugChangeMorph`)

**게코 도감 · 업적 (2026-09-17, `Domain/RewardManager.Collection.cs` · `UI/CollectionPanel.cs`):** 게코 목록 윗줄 아래 오른쪽 **"도감 / 업적"** 버튼(`+ 분양` 버튼 복제, 목록을 110 내림 — `GeckoListUIController.EnsureBookButton`, 받을 보상이 있으면 "(N)")을 누르면 화면을 덮는 창. 받을 업적이 있으면 업적 탭부터.
- **도감 탭:** 종마다(`SpeciesCatalog` — Resources/Species, 가격 → id 순) 그림 · 이름(만나기 전 "???", 그림은 검은 그림자) · 도장 **만남**(`ProgressData.unlockedSpeciesIds`) · **어덜트**(`adultSpeciesIds`). 새 종을 처음 분양하면 **코인 +50** (`BOOK_MEET_COIN` [TBD], `RewardManager.RecordMet` ← `StoreManager.BuyGecko`, 게코 목록에 초록 알림 "도감에 새 친구를 기록했어요!"), 기본 게코는 보상 없이 기록. 모든 종 어덜트 → **젬 +20** 한 번 (`BOOK_COMPLETE_GEM`, `bookRewardClaimed`)
- **업적 탭:** `RewardManager.ACHIEVEMENTS` 8개 [TBD] — 첫 허물(1, 코인 50) · 허물 달인(20, 코인 200) · 첫 어덜트(1, 젬 3) · 게코 가족(어덜트 3, 젬 10) · 다정한 손길(쓰다듬기 100, 코인 150) · 든든한 식사(먹이 50, 코인 150) · 꾸준한 돌봄(돌봄 보상 7일, 젬 5) · 북적이는 집(게코 3마리, 코인 100). 세는 값 `AchievementStat` — 허물은 게코별 `moltCount` 합, 어덜트는 `adultCount`, 쓰다듬기·먹이는 `RecordCare`가 세는 `petCount`·`feedCount`(오늘의 목표를 넘긴 돌봄도), 돌봄 보상은 `ClaimGoals`의 `goalDays`, 게코는 지금 마릿수. 받은 업적 id는 `achievements`. 이름·설명 문구는 `achieve.{id}` · `achieve.desc.{stat 소문자}`
- **홈 알림:** 사건 대기열이 비었을 때 `RewardManager.TakeNewlyAchieved`(실행마다 한 번씩) → "업적 달성! 다정한 손길 / 게코 탭 > 도감에서 받아요" + 게코 탭 통통
- 저장 버전 7 — 예전 저장은 지금 게코의 종 · 어덜트 종을 만남으로 기록 (보상 없음). 쓰다듬기·먹이·돌봄 보상 수는 이때부터 센다
- 업적 추가: `ACHIEVEMENTS`에 한 줄 + 번역표 `achieve.{id}` (새 세는 값이면 `AchievementStat` · `StatValue` · `achieve.desc.*`)

**여러 마리 키우기 (2026-09-17):** 홈에는 선택한 게코(`selectedGeckoId`) 한 마리만 나오고, 분양해도 선택은 그대로다 (게코 목록 슬롯을 누르면 바뀜). 시간은 **모든 게코**에 흐르고(배고픔·허물·성장), 돌봄 버튼·만지기는 선택 게코에만 된다.
- **분양 규칙 (`StoreManager`):** 최대 `MAX_GECKOS` 5마리 [TBD] (넘으면 분양 패널 대신 안내, `BuyGecko`도 거절). 무료는 `IsFreeFor` — `isUnlockedByDefault` 종이면서 **그 종을 한 마리도 안 키울 때만** (하코가 크레스티드라 보통 크레스티드도 `coinPrice` 300). 드롭다운 "(무료)"도 같은 판정
- **게코 목록 슬롯 (`GeckoSlotUI`):** 오른쪽에 가장 급한 상태 하나(`GeckoManager.AlertOf` — 아파요(건강 ≤20) → 배고파요(≤30) → 목말라요(≤30) → 청소 필요(청결 ≤20) → 잘 지내요)와 선택 게코에 "홈에 있어요". 글자는 실행 중에 만든다 (프리팹 그대로)
- **알림:** "배고파해요/목말라해요"는 모든 게코 중 가장 먼저 25까지 떨어질 게코(`GeckoManager.MostUrgent`)의 이름과 시각. 보상 알림의 이름은 선택 게코
- 오늘의 돌봄 목표는 어느 게코를 돌봐도 센다. 안 보이는 게코의 성장·허물은 소리 + 결과 알림만
- **홈 화면은 선택 게코만 그린다** (`HomeUIController.IsHomeGecko` — `Refresh` 첫 줄, 2026-09-20). 시간 진행은 모든 게코에 `OnStateChanged`를 보내므로 걸러내지 않으면 30초마다 목록 마지막 게코의 이름·게이지·유대·선물 상자가 화면을 덮는다

**오늘의 돌봄 목표 (2026-09-17):** 하루(UTC 날짜 — 일일 보상과 같은 기준)마다 **먹이 2 · 물 2 · 쓰다듬기 3 · 청소 1**을 채우면 **코인 +100** (`RewardManager.GoalTarget`·`GOAL_REWARD_COIN` [TBD]). 실제로 한 돌봄만 센다 — `GeckoManager.OnCareDone`(거절·삐짐 제외)을 `AppBootstrap`이 `RewardManager.RecordCare`에 연결, 목표를 넘긴 돌봄은 세지 않는다. 진행은 `PlayerData.dailyGoal`(`DailyGoalData`, 날짜가 바뀌면 `RewardManager`가 새로 만든다). 화면: 일일 보상 팝업 카드 아래 `DailyGoalCard`(실행 중 생성, 보상 카드·받기 버튼 모양을 따라 함), 목표 하나를 채우면 결과 알림 "오늘의 돌봄: 쓰다듬기 3/3 완료", 모두 채우면 알림 + 하단 보상 탭 통통. 시간 건너뛰기로 하루 넘게 건너뛰면 목표도 새로 시작

**게코 직접 만지기 (2026-09-17):** `UI/Gecko/GeckoTouch` — 게코 옆(GeckoArea 안, 게코 오브젝트는 하위 캔버스라 그 안의 그림은 터치에 안 잡힌다)에 투명 터치 영역을 두고 게코 위치·크기·**회전**을 매 프레임 따라간다. 부위 8곳은 **파츠 그림 안의 판정 박스**(`GeckoTouch.ORDER` — 파츠 사각형 대비 중심·크기 비율, `GeckoRig.TryPartLocal`이 uv를 준다)로 정한다 — 순서 눈 → 입 → 앞다리 → 뒷다리 → 머리 → 꼬리 → 몸통. 박스는 눈 0.30×0.45 · 입 1.00×1.10 · 다리 1.30 · 꼬리 1.10 · 머리·몸통 1.00 `[TBD]`, 꼬리는 그림 안 가로 0.55 이상이 뿌리(지금 그림은 관절이 오른쪽 끝).
**그림 사각형을 그대로 쓰지 않는 이유 (2026-09-20):** 최종 그림은 머리에 눈·입이 이미 그려져 있고 `eye_open`·`mouth_closed`는 다른 표정을 덮는 **빈 판**이다(눈 186×186 · 입 255×88, 불투명 픽셀 0개). 그림 사각형 + 여유로 판정하던 때는 눈 판이 머리(408×210)보다 세로로 커져 **머리(쓰다듬기) 판정이 27%만 남았다**. 그림을 바꾸면 자가 검사 `TestTouchAndMovement`(씬에 적용된 스킨으로 돈다)로 확인할 것.
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

**게코 이동 (`GeckoMovementAI`, 2026-09-17):** 발 높이 `groundBand` **380~950 (씬 값)** 안에서 앞뒤·대각선으로 다니고, 멀수록 작고 느리게(`farScale` **0.62**, 씬 값 — 코드 기본값만 바꾸면 적용되지 않는다). 쉬고 나서 30%로 **벽 타기** (2026-09-18 자연스럽게 보강): **가까운 쪽 좌우 유리벽으로 먼저 걸어가**(`ClimbWallIsLeft`·`WallClimbX`, 끝에서 `wallMargin` 120 안쪽) **발이 그 벽을 향하게** 돌아서고(왼쪽 벽 = 왼쪽을 봄), 한 번 올려다본 뒤(`Happy_LookUp`) 400~800 오르고 2~4초 매달렸다가 내려와 눕는다.
- 발이 짚은 벽은 각도로 정해진다 (`WallAngle` — 왼쪽 −90° · 오른쪽 +90°, `FootDirection`이 확인). **오르내릴 때 각도는 그대로 두고 그림만 좌우 반전**해 머리 방향을 바꾼다 (`FlipOnWall` + `Segment`의 벽 분기) — 예전처럼 0°를 지나 돌면 벽에서 떨어져 한 바퀴 도는 것처럼 보인다
- 오르는 한계 = 영역 높이 − 420 − 몸 길이(상태 띠 아래), 오르는 동안 원근 크기는 출발한 바닥 높이 기준, 바닥에 내려서는 마지막 구간은 `climbLandSlow`(0.7)배로 천천히
- `GeckoMotor.SetClimbing` 벽 자세: 그림자 숨김 · 다리를 앞 22°/뒤 18° 벌리고(먼 쪽은 0.45·0.40배 — 많이 벌리면 몸통 뒤 여유 부분이 삐져나온다) 발을 몸 쪽으로 8 당김 · 몸통을 벽에 납작하게(6%) · 고개 +4° · 꼬리 늘어뜨림. 오르는 걸음은 크고 느리게(각도 ×1.25 · 발 드는 높이 ×1.4 · 몸이 좌우로 흔들림, `StrideLength`가 같이 커져 발이 미끄러지지 않는다), 가만히 매달리면 1.6~3.2초마다 **대각선 두 발을 바꿔 짚는다** 도망: 바닥은 누른 곳 반대쪽으로 2.5배 속도 300~400(앞뒤 −150~+250 비껴감) 후 뒤돌아봄, 벽은 위로 200~320 더 달아나거나 꼭대기면 후다닥 내려옴. 벽에 붙은 채 꺼지면 바닥·각도 0으로 복구. 수치는 모두 Inspector [TBD]
- **내려오기(`Descend`)는 올라온 길(`_route`)을 지우면서 되짚는다** (2026-09-20) — 시작 전에 `TrimRouteAbove`로 이미 지나친 위쪽 점을 버리고, 한 구간을 마칠 때마다 그 점을 지운다. 예전에는 내려오는 도중 만지거나(도망) 부르면 꼭대기부터 다시 훑어 **위로 되올라갔다**

**꾸미기 구조물 (2026-09-17, `Domain/TerrariumLayout`):** 장식 칸은 **바닥 4 (0·1·4·5) / 뒷벽 3 (2·3·6)** (2026-09-17 4칸 → 7칸) — 기본 자리는 바닥 (±300, 발 높이 600) · (−90, 450) · (90, 730), 뒷벽 (±290 · 0, 밑동 760). 씬에는 `DecorSlot0~3`만 있고 4~6은 `HomeUIController.EnsureDecorImages`가 같은 종류 칸 그림을 복제한다. 칸 자리·옮길 수 있는 범위·그림 크기·게코 경로는 이 파일 한 곳이고, 모두 **기준점(anchor)** 으로 계산한다 (씬의 `DecorSlot0~3` 위치는 `HomeUIController.PlaceDecorImage`가 실행 중에 덮어쓴다).
**옮기기 (편집 모드):** 홈에서 장식을 0.5초 길게 누르면(`DecorDragHandle`) 게코가 멈추고 장식에 노란 테두리 — 끌어서 옮기고 빈 곳을 누르면 끝. 바닥 장식은 좌우·앞뒤(발 높이 420~740, 뒷벽 앞, 원근으로 작아짐), 벽 구조물은 좌우만(높이 760 고정, 가지 밑동 ±430), 그림은 화면 안, 같은 종류끼리 바닥 220·벽 가로 200보다 가까워지지 않는다 (`ClampAnchor` · `TooClose`). 손을 떼면 `TerrariumManager.SetDecorPosition`이 `TerrariumData.decorPositions`에 저장 ((0,0) = 기본 자리), 칸의 장식을 바꾸면 기본 자리로. 나뭇가지는 놓인 쪽에서 화면 가운데 쪽으로 뻗는다(오른쪽이면 그림 반전). 게코는 옮긴 위치로 집에 들어가고 구조물을 탄다. `DecorItemSO.placement`(바닥/벽)가 칸 종류, `use`가 게코가 쓰는 법, `baseline`이 그림 아래 투명 여백(이 높이가 바닥에 닿는다).

| 장식 | 칸 | 게코 |
|------|------|------|
| 하이드 하우스 · 동굴 | 바닥 | **문으로 들어간다** (2026-09-21, 아래 "은신처 문") → 5~12초(졸리면 12~25초) 엎드려 쉼 → 안에서 돌아서서 나옴. 집을 누르면 "누구야?" 하고 나옴, 돌봄 버튼·꼬리 누르기도 나오게 함 |
| 바위 | 바닥 | 옆에 엎드려 몸 데우기 6~10초 (건강 회복 +50%) |
| 코르크 뒤판 · 덩굴 | 벽 | 밑동에서 위 끝 −160까지 곧게 오르내림 |
| 나뭇가지 | 벽 | `BRANCH_LINE` 3점 — 대각선으로 올라 가로 부분에서 엎드려 쉰 뒤 되짚어 내려옴 (오른쪽 칸은 그림 좌우 반전) |

- 이동은 **경로 따라가기**(`GeckoMovementAI.Segment`) — 구간마다 돌아서고 `SegmentAngle`(머리가 가는 쪽)로 기울인다. 뒷벽 구조물이 있으면 빈 유리벽은 타지 않는다
- **앞뒤 겹침 순서 (`HomeUIController.UpdateDepthOrder`, 매 프레임 · 바뀔 때만 적용):** 뒷벽 구조물은 늘 맨 뒤, 바닥 장식과 게코는 발 높이(y)가 클수록(뒤쪽) 먼저 그린다 — 게코보다 앞에 놓인 바위는 게코를 가린다. 게코 터치 영역은 게코 바로 다음. 은신처는 문으로 들어가 있으면 게코 **뒤**(안쪽은 게코 쪽에서 잘라 낸다), 문 정보가 없는 은신처에 숨었으면 게코·터치 영역 바로 앞. 편집 판은 이 무리 전체의 뒤(`DepthGroupFirstIndex`)
- 장식을 **짧게 누르면 게코가 그 장식으로 간다** (2026-09-21, `GeckoMovementAI.VisitDecor`) — 집은 들어가고(이미 안이면 "누구야?" 하고 나옴), 벽 구조물은 타고, 바닥 장식은 옆에 서서 비비기·핥기·몸 데우기. 동작 중·편집 중·손바닥 위에서는 무시. 예전에는 벽 구조물은 누를 수 없었고 바닥 장식은 숨은 게코 불러내기만 했다
- 칸 종류가 생기기 전 저장은 앱 시작 때 `TerrariumManager.NormalizeSlots`가 맞는 칸으로 옮기고, 자리가 없으면 빼고 값을 돌려준다
- 꾸미기 화면은 씬 목록 + `DecorCatalog`(Resources/Decor 전체)를 가격순으로 보여 준다 — 새 장식은 에셋만 추가하면 된다
- 그림 교체: `Textures/Decor/decor_*.png`를 같은 크기로 바꾼다. 나뭇가지는 가지 가운데 선이 `BRANCH_LINE`과 맞아야 게코 발이 가지 위에 놓인다

**은신처 문 (2026-09-21, `GeckoMovementAI.GoHide · PlanDoor`, `DecorItemSO.doorRect`):** 예전에는 은신처 그림을 게코 앞에 그려 가렸는데, 다 자란 게코(길이 640)가 은신처(460)보다 길어 **머리가 반대편으로 삐져나와** 굴에 들어가는 게 아니라 바위 뒤에 숨은 것처럼 보였다. 이제는 **문으로 들어간다**
- 문 = 그림 안 비율 사각형 (`doorRect`, 왼쪽 아래 0,0) — 동굴 0.30~0.70 × 0.10~0.43(타원 문의 45% 높이 폭) · 집 0.34~0.55 × 0.30~0.48. 홈 화면이 놓인 자리·원근·반전을 반영해 영역 좌표로 넘긴다 (`HomeUIController.DoorRectOf` → `Structure.door`). 폭 0이면 예전 방식(`GoHideBehind`)
- 순서: 문 앞에 서서(주둥이가 문 가장자리 8 앞) 문을 본다 → 은신처는 게코 뒤로, **문 가장자리 너머는 게코 오브젝트의 `RectMask2D`로 잘라 낸다**(가장자리 18 흐리게 — 어둠 속으로 스며들듯) → 0.5배 걸음으로 몸의 68%가 문 안에 들 때까지 기어 들어감 → 엎드리고 꼬리를 바닥 쪽으로 늘어뜨림(`GeckoMotor.SetBurrowed`) → 나올 때는 안에서 돌아서서(머리가 밖으로) 문 앞까지
- **문이 게코보다 작다** (동굴 문 높이 150 · 집 83 vs 게코 키 234) → 들어가면서 몸 높이를 문 높이 × 0.9에 맞춘다: 작아지기와 납작해지기를 반반(제곱근, 크기는 55% 아래로 안 줄임) — 동굴 크기 0.76·키 0.58, 집 0.57·0.32 (`GeckoRig.Squash`, 발밑 기준). 게코도 좁은 틈에는 몸을 납작하게 해 들어간다
- 들어갈 쪽: 문이 은신처 한쪽에 치우쳐 있으면 그쪽 바깥에서(집 = 왼쪽), 가운데면 화면 가운데 쪽에서, 그쪽에 설 자리가 없으면 반대쪽에서
- 문 안에 있을 때 게코를 누르면: 꼬리는 평소대로(끝 = 달아남), 그 밖은 집을 누른 것처럼 "누구야?" 하고 나온다
- 수치 Inspector `GeckoMovementAI` 은신처 항목(`doorFit` 0.9 · `doorMinShrink` 0.55 · `doorInside` 0.68 · `doorSoftness` 18) [TBD]
**장식 효과 (2026-09-21, `Domain/DecorPerks` · `DecorItemSO.perk`):** 장식은 **놓여 있기만 하면** 작은 효과가 생긴다 — 실제 게코 습성에서 가져왔고, 앱을 꺼 둔 동안(시간 보정)에도 그대로 계산된다. 같은 효과는 겹치지 않는다. 수치는 모두 [TBD]

| 효과 | 장식 | 규칙 (`GeckoManager`) | 게코 행동 (보여 주기) |
|------|------|------|------|
| `MoltRub` | 이끼 바위 | 허물 성공률 +10% (`MoltSuccessRate`) | 허물 준비(80 이상)면 쉬고 나서 50%로 찾아가 근질근질 두 번 + 허물 조각 "근질근질... 시원해~" (준비 전에 누르면 킁킁) |
| `Droplets` | 화분 | 물 줄 때 목마름 +10 더 (`GiveWater`) | 물을 준 뒤(마시기 끝나고) 잎에 물방울이 맺히고 가서 할짝 두 번 "물방울 맛있다!" (`HomeUIController.WetPlant`) |
| `Basking` | 바위 | 건강 회복 0.5 → 0.75/h (회복 조건은 그대로) | 쉬다가 찾아가 옆에 엎드려 6~10초 + 금빛 알갱이 "따뜻하다~" |
| `Shelter` | 하이드 하우스 · 동굴 | 시간당 기분 감소 ×0.8 | (예전 그대로) 들어가 숨기 |
| `Play` | 코르크 · 덩굴 · 나뭇가지 · 큰 유목 | 쓰다듬기 애정도 3 → 4 | (예전 그대로) 타고 놀기 |

- 게코 행동은 효과와 따로다 — `GeckoMovementAI.Visit`가 장식의 **화면 가운데 쪽 옆**(`VisitX` — 막히면 바깥쪽, 둘 다 막히면 다닐 수 있는 끝), 장식보다 30 앞(겹침 순서에서 게코가 앞)에 서서 장식을 보고 `DecorVisited(칸, 효과)` → 홈 화면이 동작·말풍선·연출(`GeckoFx.LeafDroplets · WarmGlow · RubFlakes`). 머무는 시간 `visitStay` 2.8초 · `baskStay` 6~10초, 설 거리 `visitReach` 0.6 (Inspector)
- 이동 AI가 바닥 장식도 받는다 (`Structure.perk · size`) — 쉬고 나서 고른 장식이 바닥 장식이면 쓸 일이 있을 때만(바위, 허물 준비 중 이끼 바위) 찾아가고 아니면 그냥 걷는다. 화분은 물 준 뒤에만. `MoltReady`는 홈 `Refresh`가 넣는다
- 꾸미기 카드: 가격 밑에 연두색 작은 줄로 효과(`DecorSlotUI.PerkLabel` — 수치는 `DecorPerks`에서 그대로, 번역 `perk.*`). 가격·보유·빼기·잠김 어느 상태에서도 보인다
- 새 장식: 에셋의 `perk`만 고르면 효과·카드 문구가 따라온다. 새 효과는 `DecorPerk` + `DecorPerks` 수치 + `GeckoManager`의 규칙 한 줄 + `perk.*` 번역 + `PerkLabel`
**화면 분위기 연출 (2026-09-18, `UI/Fx/TerrariumAtmosphere`):** 그림 없이 코드로만 — **비네트**(가장자리 어둡게, 테마 그림 위 · 장식·게코 아래 = `DepthGroupFirstIndex`), **먼지 14개**(아래에서 위로 천천히 떠오르며 좌우로 흔들리고 끝에서 옅어짐, 게코 앞), 모두 `raycastTarget` 꺼짐. `HomeUIController._atmosphere` 체크를 끄면 둘 다 안 나온다. 그림 교체는 `Resources/Fx/vignette`. **앞 잎사귀 2장**(아래 양쪽 모서리의 어두운 잎)은 2026-09-21에 뺐다 — 대부분 돌봄 버튼·하단 탭에 가려 끝만 삐져나왔고, 테마 흙 바닥이 밝아지자 검은 얼룩으로 보였다
**공기 원근:** 발 높이가 뒤로 갈수록 `GeckoMovementAI.farTint`(기본 0.90, 0.94, 1.00)를 섞어 곱한다 — 게코는 `GeckoRig.DepthTint`, 바닥 장식은 `HomeUIController.HazeTint`(뒷벽 구조물은 가장 뒤 값). 발밑 그림자는 그림보다 25% 넓고 15% 옅게 (`GeckoMotor.SHADOW_SPREAD/ALPHA`). 모두 [TBD]

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
| `UI/Gecko/GeckoMotor.cs` | 호흡·꼬리 물리·걷기·벽 타기 자세·표정·동작 22종 계산. 수치는 Inspector `[TBD]`. 연출 타이밍 상수(`FEED_*`, `DRINK_*`)와 `ActionStarted` 이벤트 공개 |
| `UI/Gecko/GeckoBendGraphic.cs` | 휘어지는 꼬리 메시 (UI) |
| `UI/Gecko/GeckoNeckBend.cs` | 머리 그림을 목에서 휘게 하는 메시 효과 — 고개를 들어도 턱 밑이 잘려 보이지 않게 (2026-09-21) |
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
| `Spin` | 유대 Lv.4 — 쓰다듬기 때 25% (바닥에 있을 때). 웅크렸다 높이 뛰며 몸 전체(몸통이 뿌리)를 한 바퀴, 다 돈 순간 0°로 바꿔 끝에서 되감기지 않는다 | 1.2초 |

**기분 (동작이 아니라 계속 유지되는 상태)** — `GeckoAnimatorController.ResolveMood`, 우선순위 위에서부터

| 기분 | 조건 | 모습 |
|------|------|------|
| `Angry` | hunger < 20 AND mood < 30 | 시무룩한 입, 고개 숙임, 꼬리 튕기기 |
| `Sleepy` | mood < 35 | 반쯤 감긴 눈, 느린 호흡, 가끔 졸기, 걷지 않음 (예전 `Sleepy_Slow`) |
| `Happy` | mood > 70 AND affection > 50 | 미소, 가끔 반짝이는 눈, 꼬리 말아 올림 |
| `Normal` | 그 외 | — |
| 허물 준비 | moltProgress ≥ 80 (기분과 별개) | 몸에 허물 조각 표시 |

호흡(예전 `Idle_Breath`)은 항상 켜져 있다. 기계적 느낌은 무작위 간격, Perlin 노이즈 머리 흔들림, 꼬리 스프링으로 없앤다.

**머리 움직임 (2026-09-21, `GeckoMotor.LayerHead · UpdateLook · StartLook · LookAt`):** 도마뱀은 머리를 계속 흔들지 않고 **끊어서** 움직인다 — 탁 돌리고(`_lookTurn` 0.08초), 멈춰서 보고(`_lookHold` 0.7~1.9초), 40%는 앞으로 돌아오지 않고 다른 곳을 한 번 더 본다(`_lookChain`), 돌아올 때는 느긋하게(`_lookReturn` 0.22초). 가만히 있으면 2.5~5.5초마다 위(40%, 눈도 위) · 아래 바닥(30%) · 앞 · 뒤를 흘끗, 각도 3.5~7.5°. 볼 때 목을 3픽셀 빼고 위를 보면 2픽셀 든다. 가만히 떠돌기 ±3°. 걷기·동작·엎드려 쉬기·졸기 중에는 앞을 본다. 예전에는 떠돌기 ±2.2° · 둘러보기 ±2.5°라 머리가 고정된 것처럼 보였다
- **바라보기:** 빈 바닥을 한 번 누르면 게코가 그쪽을 본다 (`FloorTapCatcher.Tapped` → `GeckoAnimatorController.LookAt`) — 앞쪽이면 고개를 그 방향으로, 뒤쪽이면 눈으로 흘끗. 선물 상자가 나타나면 그쪽을 본다. 방향은 `GeckoMotor.HeadAngleToward`(목 관절 기준, `GeckoRig.WorldToSkin`이 좌우 반전·벽 회전을 되돌린다)
- **각도 한계 `GeckoMotor.HEAD_LIMIT` = ±8°** (떠돌기 + 둘러보기 + 바라보기 합) — 차분하게 보이는 범위. 동작(올려다보기 16° · 하품 14° 등)은 따로
- **목 휨 (2026-09-21, `UI/Gecko/GeckoNeckBend`):** 머리를 통째로 돌리면 고개를 들 때 머리 그림 아래의 곧게 잘린 선이 몸통에서 떨어져 **턱 밑에 틈(잘린 목)**이, 숙이면 **등 돌기가 두 겹**으로 보였다 → 머리 Image에 붙인 메시 효과가 그림을 세로 띠 18칸으로 나눠 **목 쪽(그림 왼쪽 12%)은 몸통 각도로 되돌리고, 46%부터는 머리 각도 그대로**, 그 사이는 부드럽게 휜다 (`FollowAt`). 머리 오브젝트는 그대로 머리 각도만큼 돌아서 눈(53%)·입·혀 표정 그림이 어긋나지 않는다. 회전 중심은 머리 관절 = 그림 피벗. `GeckoRig`가 머리에 자동으로 붙이고 매 프레임 `SetAngle(머리 각도)`. 그림을 바꾸면 눈 자리가 46%보다 오른쪽인지 확인

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
- 게코·장식 그림을 새로 만드는 절차(캔버스 1600×900, 파츠 14 + 표정 17, 프롬프트, 메뉴 ②③)는 **`ART_ORDER_GECKO.md`**

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
| `Core/NotificationScheduler.cs` | 로컬 알림 — 백그라운드로 갈 때 "배고파해요"·"오늘의 보상" 예약, 앱을 열면 취소 | Mobile Notifications 설치됨 (2026-09-21) — 코드는 리플렉션으로 부르므로 패키지가 빠져도 컴파일은 된다 |
| `Core/KoreanText.cs` | 이름 뒤 조사 — 하코**가** / 별님**이** | — |

- 효과음·진동은 **UI 계층에서만** 부른다 (Domain·Data는 소리를 모른다)
- **TMP 글꼴은 정적 아틀라스** — `NanumGothic-Regular SDF`에는 한글 11,172자·자모·ASCII만 있고 예비 글꼴도 없다 (`LiberationSans SDF`는 Nanum으로 넘어간다). ★ ♥ → ← ↗ ✕ ⚙ … ✦ · (가운뎃점) 같은 기호와 **이모지**는 □로 나온다 → 코드 문구·**씬 텍스트** 모두 한글·영문·숫자·ASCII 기호만 쓰고, 아이콘은 그림(Image)으로 넣는다
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
- 저장 버전 3 (현재 10): 예전 저장의 `language = "ko"`는 고른 값이 아니라 기본값이었으므로 로드 때 비워 기기 언어를 따르게 한다
- 번역표의 모든 글자는 `NanumGothic-Regular SDF` 아틀라스에 있어야 한다 — 자가 검사 `TestLocalization`이 누락·`{0}` 자리 수·원문 겹침·글꼴 글자를 확인한다
- **영어 화면 확인:** 플레이 중 메뉴 `Hako > 검사 > 언어 > 영어` (설정 저장 + 씬 다시 열기). 확인 후 `기기 언어`로 되돌린다. 설정 화면의 언어 선택 UI는 아직 없다 (`SettingsManager.SetLanguage`만 있음)

## 주요 컨벤션 & 주의사항

- **모든 텍스트는 TextMeshPro** (UI Text 사용 금지)
- **꾸미기 배치: 칸 수 고정(바닥 4 · 뒷벽 3, 2026-09-17 늘림) + 홈에서 길게 눌러 범위 안에서만 끌어 옮기기** (2026-09-17 사용자 결정 — 예전 "자유 드래그 절대 금지"에서 완화). 개수 제한 없는 완전 자유 배치·회전·크기 조절은 MVP 이후
- **먹이 (2026-09-15 MVP에 포함 — 사용자 결정):** 먹이 버튼 → `FoodTray`(실행 중 생성하는 선반)에서 가진 먹이를 골라 준다. 마지막으로 준 먹이(`PlayerData.lastFoodItemId`)가 맨 앞, 선반 밖을 누르면 닫힘, 먹이가 없으면 상점으로
  - 버튼 글자·흐림도 **선반과 같은 목록**(`HomeUIController.OwnedFoods`)을 쓴다 (2026-09-20) — 예전에는 버튼만 배를 채우는 먹이를 세어, 영양제만 있으면 "먹이 없음"인데 선반은 열렸다
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
| `com.unity.mobile.notifications` | 2.4.3 | 로컬 알림 (2026-09-21 추가) |
| `com.unity.ugui` | 2.0.0 | uGUI |
| `com.unity.timeline` | 1.8.9 | 타임라인 애니메이션 |

**추가 설치 필요:** TextMeshPro (TMP Essentials), Newtonsoft JSON (선택)

## 현재 진행 상태

작업 시작 전 반드시 `DEV_LOG.md`를 읽어 현재 단계와 남은 작업을 확인할 것.
