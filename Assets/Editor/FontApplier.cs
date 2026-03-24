using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Hako/폰트 일괄 적용 — 현재 열린 씬의 모든 TextMeshProUGUI에 NanumGothic SDF를 적용한다.
/// MainHome 외 다른 씬에서도 동일하게 사용 가능.
/// </summary>
public static class FontApplier
{
    private const string FONT_ASSET_PATH = "Assets/_Game/Fonts/NanumGothic-Regular SDF.asset";

    [MenuItem("Hako/폰트 일괄 적용 (현재 씬)")]
    public static void ApplyFontToCurrentScene()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ASSET_PATH);
        if (font == null)
        {
            EditorUtility.DisplayDialog("폰트 없음",
                $"폰트 에셋을 찾을 수 없습니다.\n경로: {FONT_ASSET_PATH}", "확인");
            return;
        }

        var allTMP = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        if (allTMP.Length == 0)
        {
            EditorUtility.DisplayDialog("결과", "씬에 TextMeshProUGUI 오브젝트가 없습니다.", "확인");
            return;
        }

        if (!EditorUtility.DisplayDialog("폰트 일괄 적용",
            $"현재 씬: {EditorSceneManager.GetActiveScene().name}\n" +
            $"대상 TMP 개수: {allTMP.Length}개\n" +
            $"폰트: {font.name}\n\n적용하시겠습니까?",
            "적용", "취소"))
            return;

        Undo.SetCurrentGroupName("Apply Korean Font to All TMP");
        int group = Undo.GetCurrentGroup();

        int count = 0;
        foreach (var tmp in allTMP)
        {
            if (tmp.font == font) continue;
            Undo.RecordObject(tmp, "Apply Font");
            tmp.font = font;
            EditorUtility.SetDirty(tmp);
            count++;
        }

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"[FontApplier] {count}개 TextMeshProUGUI에 '{font.name}' 적용 완료. " +
                  $"({allTMP.Length - count}개는 이미 동일 폰트)");

        EditorUtility.DisplayDialog("완료",
            $"{count}개 TMP에 '{font.name}' 적용 완료.\n" +
            $"(이미 적용된 {allTMP.Length - count}개는 건너뜀)\n\n" +
            "Ctrl+S로 씬을 저장하세요.", "확인");
    }
}
