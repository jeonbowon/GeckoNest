# GeckoNest — 현재 상태와 다음 작업

> 마지막 정리: 2026-10-08. 구현되어 있는 것과 실제 검증을 마친 것을 구분한다.

## 현재 판단

- 10/8 어린이·성인 혼합 이용 대응: 광고 SDK 시작 전 중립적인 연령 입력, 응답 거절, 로컬 연령 구분 저장을 추가했다. 만 18세 미만과 미확인은 보수적으로 Child/G 광고 설정과 UMP 미성년 처리를 적용한다. 실제 나이는 저장하지 않는다. 기존 저장은 v12로 마이그레이션하며 게임 진행을 보존한다.
- 10/8 Unity 6000.2.8f1 컴파일·자가 검사 497개 통과(`Logs/age-selftest-20261008-final.log`). 별도 테스트 저장으로 실제 MainHome Play Mode에서 초기 SDK 차단, 빈 입력 차단, 어린이/응답 거절 저장, 한영 GPU 화면 확인 통과(`Logs/age-preview-20261008.log`). 네트워크 광고는 Editor 모의 실행이며 실기기 검증이 아니다.
- 10/8 Unity Editor에서 어린이 대응 포함 `Builds/Android/Release/hako-0.1.0-2.aab` 생성 성공(오류 0, 실제 파일 60,083,452 bytes). 버전 0.1.0/코드 2, 패키지 com.tnbsoft.hako, minSdk 24/targetSdk 36, IL2CPP/ARM64. 기존 업로드 인증서 일치, jarsigner/bundletool 검증, 실제 광고 ID 및 연령 처리 metadata 포함, 네이티브 7개와 번들 16KB 정렬 통과. SHA256 `DB0C8A9748B646FD77B3B6411791E4699AF6155F34907190690A71B72ED6F1AE`. 증거: `Logs/release-aab-build-20261008.log`, `Logs/release-aab-20261008-validation.json`. 아래 10/7 코드 1 파일은 이번 출시본으로 사용하지 않는다.
- 코드 2 IL2CPP 심볼 ZIP도 생성했다. Cloud Diagnostics 자동 업로드는 인증 부재로 실패했으나 로컬 빌드는 성공했다. ADB 연결 기기 목록이 비어 있어 실기기 광고/UMP 검증은 수행하지 않았다. Play Console 업로드 및 심사 제출도 미수행이다.
- 사용자 제공 Play Console 화면에서 조직 계정, HAKO 생성, 패키지 com.tnbsoft.hako, 콘텐츠 등급 완료를 확인했다. 대상 연령은 6세 이상을 선택했고 어린이 관련 준수 선언을 진행 중이다. 코드 검사만으로 정책·법규 준수 전체를 보증하지 않는다.

- Play Console은 사용자 확인에 따른 비즈니스(조직) 계정이다. 신규 개인 계정의 12명·14일 비공개 테스트 요건을 적용하지 않는다. 사용자는 내부 테스트 트랙을 생략하고 프로덕션 출시를 진행하기로 했다.
- 10/6 공개 `https://www.tnb-soft.com/app-ads.txt`를 직접 요청해 HTTP 200 및 `text/plain` 응답을 확인했다. Google 행은 `google.com, pub-3852398620139102, DIRECT, f08c47fec0942fa0`이며 코드의 Android 광고 게시자 ID와 일치한다. 파일 게시와 ID 일치는 확인 완료. AdMob 콘솔의 크롤링/검증 상태는 이번 확인에 포함하지 않았다.
- 10/8 개인정보처리방침 `https://www.tnb-soft.com/hako-privacy` 정정·공개 배포 완료. 가상 게코 게임, 로컬 저장과 삭제/OS 백업, 연령 구분, 어린이·미확인 광고 보호, Google 광고/UMP 데이터 처리, 문의 및 권리를 반영했다. 홈페이지 저장소 커밋 `1c08441`, Next.js 빌드·TypeScript 통과. 공개 URL HTTP 200과 새 시행일·연령 보호·게임 문구, 이전 사육 기록 설명 제거를 확인했다. 별도 AdMob 설정·실기기 검증은 남아 있다.
- 10/7 Unity 6000.2.8f1 Editor에서 출시 AAB 생성 성공(오류 0): `Builds/Android/Release/hako-0.1.0-1.aab`, 60,079,693 bytes. 패키지 `com.tnbsoft.hako`, 버전 `0.1.0`/코드 `1`, Target SDK 36, 최소 SDK 24, IL2CPP/ARM64, Development 비활성. 광고 Unity 플러그인의 실제 Manifest가 API 24를 요구해 최초 빌드 실패를 수정했다.
- 사용자 생성 키 `D:/Keystores/hako-upload.keystore`, Alias `hako-upload`로 서명했다. 사용자가 Editor에 입력한 비밀번호를 사용했고 별도 비밀번호 파일은 만들지 않았다. AAB의 jarsigner 검증 통과, 인증서 TNBSoft/jeonbowon·만료 2076-09-23 확인. 키는 프로젝트 밖에 보존했다.
- 최종 AAB bundletool 검증, 실제 AdMob 앱 ID/보상형 ID 포함, 디버그·testOnly 비활성, 16KB 번들 정렬 및 네이티브 라이브러리 7개의 ELF LOAD 정렬을 확인했다. 검증 기록 `Logs/release-aab-20261007-validation.json`, Unity 빌드 로그 `Logs/release-aab-build-20261007.log`. SHA256 `879E69220D6AE60F36EE73FA17C564C59A4D35EF285BFEFBE45B451ECC99E4C5`.
- 로컬 심볼 ZIP `Builds/Android/Release/hako-0.1.0-1-0.1.0-v1-IL2CPP.symbols.zip`도 생성했다. Unity Cloud Diagnostics 인증 부재로 자동 심볼 업로드는 실패했으나 AAB 빌드는 성공했다. Play Console 업로드·심사 제출은 아직 수행하지 않았다.
- 9/29 테스트 APK `Builds/Android/Hako-device-test.apk`와 기기 화면 캡처는 광고 추가 전 Debug 서명 결과다. 10/7 출시 AAB의 기기 실행·실제 광고/UMP 검증을 대신하지 않는다.
- 10/6 최신 코드 Unity 6000.2.8f1 배치 컴파일·로직 자가 검사 465개 통과, 종료 코드 0. 로그 `Logs/release-selftest-20261006-final.log`. 이번 실행은 Android 플레이·빌드 검증이 아니다. Play Console 및 AdMob 콘솔은 연결된 로그인 화면이 없어 UMP 게시·데이터 보안·스토어 등록·app-ads.txt 콘솔 검증·배포 상태를 확인하지 못했다.

STEP 1~6의 코드·씬은 구현되어 있다. 사용자 승인 시안을 바탕으로 정글 홈을 약 40도 위에서 보는 2D 사선 아트로 전환했다. 보상 탭과 성장 완료 뒤에만 사용자가 선택해 보는 AdMob 보상형 광고도 연결했다. 기존 저장 좌표와 보상 이력은 마이그레이션으로 보존한다. Unity 배치 컴파일·자가 검사 및 별도 테스트 데이터의 GPU 화면/가지 왕복 검사를 수행했다. 전체 수동 플레이·Android 광고/기기 검증은 남아 있으며 출시 완료 상태가 아니다.

- 최신 광고 변경: 보상 탭은 시청 완료 시 코인 30, UTC 하루 최대 3회. 성장 완료 제안은 해당 게코·해당 성장 단계에서 한 번만 코인 50. 강제 광고·배너·앱 시작 광고는 없다. UMP 동의와 필요한 경우 설정의 개인정보 선택 화면을 제공한다.
- Google Mobile Ads Unity 플러그인은 11.5.0이다. 10/4 사용자가 제공한 HAKO Android 실제 앱 ID와 보상형 광고 단위 ID를 설정했다. 출시 요청에 따라 `USE_TEST_ADS = false`로 전환하고 실제 ID를 사용한 AAB를 생성했다. 전환 후 10/6 Unity Editor 로직 자가 검사 465개 통과(`Logs/release-selftest-production-20261006.log`), 10/7 Android 컴파일·패키징 성공. iOS는 출시 설정 전이며 Android 광고/UMP 실제 동작 검증은 별도다.
- 최신 변경: `Textures/Oblique`의 PNG 8장과 `GeckoSkin_Oblique` 연결. 정글 바닥 이동 높이 500~1320, 가지의 실제 그림에 맞춘 경로, 사선 동굴의 실루엣 가림, 독립 네 발과 기존 꼬리 동작. 상단 배경 패널·재화 가림 수정, 차분한 돌봄/내비 패널. 관찰 개선의 상태 기반 행동·성장 안내는 유지.
- 검사 코드 추가: 빈 슬롯 회귀 검사, 72개 표정 조합 및 모프·좌표·스킨 전환 검사, 읽기 전용 출시 준비 검사.
- 사선 표정은 새 눈/입 아틀라스로 연결했다. 대표 표정의 GPU 화면은 확인했지만 모든 조합·모프·언어·기기 검증은 남아 있다. 레오파드/가고일 전용 최종 그림과 최종 오디오는 별도다.
- 기존 파츠 스킨 경로, 원본 몸 그림 및 사용자 작업은 보존했다.

## 검증 상태

10/8 어린이 대응 코드를 포함한 Unity 6000.2.8f1 배치 컴파일과 로직 자가 검사 497개가 통과했다. 연령 분류·요청 설정·저장 v12 마이그레이션·기존 광고 보상·한영 문구를 포함한다. 광고 네트워크의 실제 로드/시청/보상 콜백과 UMP 화면은 에디터 모의 실행으로 대체되므로 Android 기기에서 별도 확인해야 한다.

| 구분 | 최신 변경 상태 |
|------|------|
| 정적 검사 | diff 공백 확인, 새 PNG 알파와 실제 크기 확인. Unity 검증을 대신하지 않음 |
| Unity 컴파일·셰이더 임포트 | Unity 6000.2.8f1 배치 컴파일 및 GPU 렌더 실행 성공 |
| 로직 자가 검사 | 10/8 어린이 대응 포함 497개 통과. `Logs/age-selftest-20261008-final.log` |
| 표정·모프·터치·동작 화면 확인 | 별도 메모리 데이터로 실제 MainHome Play Mode GPU 캡처: 기본/표정/가지 휴식. 눈 터치 판정, AI 가지 오르기→휴식→하강 확인. 모든 표정·모프·벽·은신처·드래그의 수동 검사는 남음 |
| Android 출시 빌드·서명·구조 | 10/8 코드 2 AAB 성공, 업로드 인증서 일치 및 jarsigner/bundletool 검증, 연령 코드 포함, ARM64 네이티브 7개와 번들 16KB 정렬 통과 |
| Android 성능·저장·알림·진동·실제 광고/UMP | 9/29 테스트 APK와 화면 캡처는 존재. 10/7 AAB 기기 실행 및 광고 추가 이후 실제 광고/UMP 검증은 수행하지 않음 |

기록의 287/372/380개 통과는 이전 변경의 결과이며 최신 코드의 통과 증거가 아니다.
현재 PC에서 `C:/Program Files/Unity/Hub/Editor/6000.2.8f1/Editor/Unity.exe`와 해당 버전을 확인했다. 10/7 출시 빌드 전 준비 검사 Configuration errors: 0. 출시 서명은 설정·AAB 검증 완료이며, 종별 대체 아트·합성음 경고와 수동 검증 항목은 남아 있다.

## 다음 작업 순서

1. 공개 개인정보처리방침 정정은 10/8 완료했다. Play 데이터 보안 답변을 현재 코드와 정정된 방침에 맞춰 작성한다.
2. 로그인된 Play Console에서 스토어 등록정보·앱 콘텐츠·데이터 보안 등 필수 항목의 실제 완료 상태를 확인하고, 생성한 AAB로 프로덕션 출시를 준비한다. 내부 테스트 트랙은 사용자 결정에 따라 생략한다.
3. AdMob 콘솔에서 UMP 게시·앱 준비·app-ads.txt 검증 상태를 확인한다. 공개 파일 게시·게시자 ID 일치는 확인 완료이며 콘솔 상태는 미확인이다.
4. 실기기 광고/UMP 및 [플레이 검사 목록](docs/testing/PLAYTEST.md)의 미검증 항목은 빌드 성공과 구분해 관리한다. 실제 광고 경로를 개발 중 검사한다면 등록한 테스트 기기를 사용한다.

**Hako > 검사 > 출시 준비 검사**는 읽기 전용 점검이다. 온라인 정책 확인, 키 생성, 빌드·배포나 기기 실행을 대신하지 않는다.

## 유지할 결정

- 일일 보상은 UTC 자정 기준을 유지하고 기기 시간대의 갱신 시각을 안내한다.
- 자연사는 MVP에서 제외한다.
- Mobile Notifications 패키지와 임시 하단 탭 아이콘은 추가되어 있지만, 기기 알림 확인과 최종 아트 승인은 별도다.

## 문서 위치

- [작업 규칙](AGENTS.md) · [프로젝트 상세 사양](docs/PROJECT_REFERENCE.md)
- [플레이 검사 목록](docs/testing/PLAYTEST.md)
- [개발 기록과 2026-09-27 상세 인수인계](docs/history/DEV_LOG.md)
- [아트·사운드 가이드](docs/art/ART_GUIDE.md) · [그림 주문서](docs/art/ART_ORDER_GECKO.md) · [표정 생성 기록](docs/art/ART_PROMPTS_2026-09-27.md)

별도 CODEX_HANDOFF.md의 내용은 개발 기록에 보존했고, 다음 작업과 검증 상태는 이 문서로 통합했다.
