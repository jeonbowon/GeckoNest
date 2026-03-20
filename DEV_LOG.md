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
- [ ] UI 레이아웃 위치 정리
- [ ] 게이지 Fill 값 반영 확인
- [ ] Water / Pet / Clean 버튼 테스트
