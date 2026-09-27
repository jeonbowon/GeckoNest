# AGENTS.md

## 프로젝트와 먼저 읽을 문서

GeckoNest(HAKO)는 Unity 6 **6000.2.8f1**, Built-in 2D 기반 게코 육성 앱이다. Android 우선, iOS 후속, 1인 개발 MVP 범위를 유지한다.

1. 작업 시작 전 [PROGRESS.md](PROGRESS.md)에서 현재 변경과 미검증 항목을 확인한다.
2. [프로젝트 상세 사양](docs/PROJECT_REFERENCE.md)의 관련 절을 작업 전에 읽고 따른다. 기존 AGENTS.md의 게임 규칙·수치·에디터 메뉴·주의사항은 이 문서에 통합되어 있다. 광범위한 변경이면 전체를 읽는다.
3. 플레이 검사는 [검사 목록](docs/testing/PLAYTEST.md), 변경 배경은 [개발 기록](docs/history/DEV_LOG.md)을 참고한다.
4. 그림 작업은 [아트 가이드](docs/art/ART_GUIDE.md)와 [그림 주문서](docs/art/ART_ORDER_GECKO.md)를 함께 읽는다.

## 변경 규칙

- UI → Manager → Repository → Save 단방향. UI에서 플레이어 데이터를 직접 수정하지 않는다.
- 씬 전환은 SceneRouter 한 곳에서 처리한다. UI에서 SceneManager.LoadScene을 직접 호출하지 않는다.
- JsonUtility 저장 모델은 직렬화 가능한 List를 사용한다. Dictionary를 저장 필드로 쓰지 않는다.
- 기존 저장과 마이그레이션을 보존한다. 저장 파일·서명 키·사용자 작업을 임의로 삭제하거나 덮어쓰지 않는다.
- 시간 진행은 GeckoManager.ApplyElapsedProgressAll 경로, UTC 기준과 오프라인 48시간 상한을 따른다. 상태와 lastUpdatedTicks를 함께 갱신하고 매 프레임 저장하지 않는다.
- 성장·허물·유대 사건은 GeckoEventQueue를 통해 순서대로 보여 준다. 홈 UI는 선택 게코만 갱신한다.
- UnityEngine.Object에는 ?. 대신 Unity의 null 비교를 사용한다. 선택 게코·목록·먹이 인자를 검사한다.
- UI 글자는 TextMeshPro, 새 문구는 Core/Loc.cs에 한국어·영어를 함께 등록한다. 사용자 이름은 번역에서 제외한다. 정적 글꼴에 없는 기호·이모지는 Image 아이콘으로 대체한다.
- 홈 중앙은 게코 공간으로 비워 둔다. 배경 위 글자는 기존 가독성 처리를 따른다.
- .meta와 GUID 연결을 보존하고 최종 PNG를 생성기로 덮어쓰지 않는다. 아트 변경 시 터치 영역·애니메이션·스킨 연결도 확인한다.
- 효과음·진동은 UI 계층에서만 호출한다. 도메인·데이터 계층에 소리를 넣지 않는다.
- 미확정 수치는 기존 [TBD] 상수로 관리한다. STEP 6 완료 전 2차 기능을 시작하지 않는다.
- Keystore는 프로젝트 밖에 보관하며 Git에 넣지 않는다. Android 프로젝트 경로에는 한글·공백을 피한다.
- 기존 미커밋 변경을 보존하고 요청 범위 밖 구현·배포·커밋·푸시를 하지 않는다.

## 검증

- 실제 컴파일·테스트·실행·빌드는 Unity Editor로 한다. 독립 C# 컴파일을 Unity 검증으로 간주하지 않는다.
- 로직 검사: Hako > 검사 > 로직 자가 검사. 플레이 검사: Window > General > Test Runner 및 검사 목록.
- Unity가 닫혀 있으면 해당 버전 Editor의 -batchmode -nographics -executeMethod HakoSelfTest.RunBatch 실행은 허용한다. 정확한 경로·명령·다른 검사 메뉴는 상세 사양을 따른다.
- APK/AAB 패키징은 Unity Editor에서 한다. 출시 준비 검사는 Hako > 검사 > 출시 준비 검사(읽기 전용)이며 실제 빌드·정책 확인·기기 테스트를 대체하지 않는다.
- 정적 확인, Unity 컴파일, 자가 검사, 화면 확인, 기기 테스트를 구분해 보고한다. 실행하지 않은 검사와 과거 통과 기록을 현재 통과로 표시하지 않는다.

## 문서 관리

- 작업 지침은 이 파일, 상세 동작은 docs/PROJECT_REFERENCE.md, 현재 상태는 PROGRESS.md에 각각 한 번만 관리한다.
- 상세 검사 목록은 docs/testing/PLAYTEST.md, 날짜별 기록은 docs/history/DEV_LOG.md에 남긴다.
- CLAUDE.md는 이 지침의 연결 문서로 유지한다. 중복 상태 문서나 별도 인수인계 파일을 만들지 않는다.
