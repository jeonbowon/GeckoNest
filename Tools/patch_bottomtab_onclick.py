"""
patch_bottomtab_onclick.py
MainHome.unity BottomTab 버튼들의 OnClick을 SceneRouter 정적 메서드에 연결.

연결 대상:
  HomeTab      (Button &796498756)  -> SceneRouter.GoToHome
  StoreTab     (Button &726963909)  -> SceneRouter.GoToStore
  GeckoListTab (Button &1956806926) -> SceneRouter.GoToGeckoList
  TerrariumTab (Button &1907075690) -> SceneRouter.GoToTerrarium
  CollectionTab(Button &473931388)  -> STEP 6 미구현, 스킵

Unity Button.OnClick PersistentCall 구조 (정적 메서드, void, 인수 없음):
  m_Mode: 1          = void / no-arg
  m_CallState: 2     = RuntimeOnly
  m_Target: {fileID: 0}  = 정적 메서드이므로 타겟 없음

사용법:  python Tools/patch_bottomtab_onclick.py
"""

import re, os, shutil

SCENE = os.path.normpath(
    os.path.join(os.path.dirname(__file__),
                 "..", "Assets", "_Game", "Scenes", "MainHome.unity"))

# ── 연결 테이블 ────────────────────────────────────────────────────────────────
# (Button MonoBehaviour fileID, SceneRouter 메서드명)
CONNECTIONS = [
    (796498756,  "GoToHome"),
    (726963909,  "GoToStore"),
    (1956806926, "GoToGeckoList"),
    (1907075690, "GoToTerrarium"),
    # 473931388 CollectionTab -> STEP 6에서 처리
]

# ── PersistentCall 블록 생성기 ─────────────────────────────────────────────────
def make_call(method_name: str) -> str:
    """
    Button.OnClick 안에 삽입할 PersistentCall YAML.
    들여쓰기는 씬 파일의 실제 indent(6칸)에 맞춤.
    """
    return (
        "      m_Calls:\n"
        "      - m_Target: {fileID: 0}\n"
        "        m_TargetAssemblyTypeName: SceneRouter, Assembly-CSharp\n"
        f"        m_MethodName: {method_name}\n"
        "        m_Mode: 1\n"
        "        m_Arguments:\n"
        "          m_ObjectArgument: {fileID: 0}\n"
        "          m_ObjectArgumentAssemblyTypeName: UnityEngine.Object, UnityEngine\n"
        "          m_IntArgument: 0\n"
        "          m_FloatArgument: 0\n"
        "          m_StringArgument: \n"
        "          m_BoolArgument: 0\n"
        "        m_CallState: 2"
    )

# ── 메인 처리 ─────────────────────────────────────────────────────────────────

# 백업
backup = SCENE + ".bak_bottomtab"
shutil.copy2(SCENE, backup)
print(f"Backup: {backup}")

with open(SCENE, "r", encoding="utf-8") as f:
    content = f.read()

patched = 0

for file_id, method in CONNECTIONS:
    # 대상 Button MonoBehaviour 블록의 시작 앵커를 찾는다.
    # 패턴: "--- !u!114 &<fileID>\n" 이후 "m_Calls: []" 가 처음 나오는 지점을 교체.
    # 단, 다음 "--- !" (다음 YAML 도큐먼트)가 나오기 전에만 교체.
    anchor = f"--- !u!114 &{file_id}\n"
    anchor_pos = content.find(anchor)
    if anchor_pos == -1:
        print(f"  [WARN] fileID {file_id} not found, skip")
        continue

    # 이 블록의 끝 = 다음 "--- !" 위치
    next_doc = content.find("\n--- !", anchor_pos + len(anchor))
    block = content[anchor_pos:next_doc] if next_doc != -1 else content[anchor_pos:]

    old_calls = "      m_Calls: []"
    if old_calls not in block:
        print(f"  [SKIP] {method}: already patched or pattern mismatch")
        continue

    new_block = block.replace(old_calls, make_call(method), 1)
    content = content[:anchor_pos] + new_block + (content[next_doc:] if next_doc != -1 else "")
    patched += 1
    print(f"  [OK] {method} -> fileID {file_id}")

with open(SCENE, "w", encoding="utf-8", newline="\n") as f:
    f.write(content)

print(f"\nDone: {patched}/{len(CONNECTIONS)} buttons patched -> {SCENE}")
if patched < len(CONNECTIONS):
    print(f"[NOTE] CollectionTab is intentionally skipped (STEP 6).")
