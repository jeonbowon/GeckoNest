using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>GeckoRig Inspector — 그림 다시 적용, 관절 저장</summary>
[CustomEditor(typeof(GeckoRig))]
public class GeckoRigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var rig = (GeckoRig)target;

        EditorGUILayout.Space(8);
        if (rig.Skin == null)
            EditorGUILayout.HelpBox("그림(GeckoSkin)이 비어 있습니다. 메뉴 Hako > Gecko > ① 을 실행하거나 스킨을 넣어 주십시오.", MessageType.Warning);

        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            if (GUILayout.Button("그림 다시 적용 (쉬는 자세로 배치)", GUILayout.Height(26)))
            {
                Undo.RegisterFullObjectHierarchyUndo(rig.gameObject, "Apply Gecko Skin");
                rig.EnsureParts();
                rig.ApplySkin();
                rig.SolveRest();
                EditorSceneManager.MarkSceneDirty(rig.gameObject.scene);
            }

            if (GUILayout.Button("관절 위치를 스킨에 저장", GUILayout.Height(26)))
            {
                Undo.RegisterFullObjectHierarchyUndo(rig.gameObject, "Save Gecko Joints");
                int n = rig.SaveJointsToSkin();
                EditorSceneManager.MarkSceneDirty(rig.gameObject.scene);
                EditorUtility.DisplayDialog("관절 저장", $"{n}개 파츠의 관절 위치를 스킨에 저장했습니다.", "확인");
            }
        }

        EditorGUILayout.HelpBox(
            "관절(회전 중심) 옮기는 법\n" +
            "1) Hierarchy에서 Visual 아래 파츠 선택 (예: tail)\n" +
            "2) Scene 뷰에서 Rect 도구(T), 상단 도구 모음을 'Pivot'으로\n" +
            "3) 파란 동그라미를 끌어 관절 자리로 옮김\n" +
            "4) 위 '관절 위치를 스킨에 저장'",
            MessageType.Info);
    }
}

/// <summary>GeckoMotor Inspector — 플레이 중 동작 미리보기</summary>
[CustomEditor(typeof(GeckoMotor))]
public class GeckoMotorEditor : Editor
{
    private static readonly (GeckoAction action, string label)[] ACTIONS =
    {
        (GeckoAction.Tongue_EyeLick,   "혀로 눈 닦기 ★"),
        (GeckoAction.Tongue_Lick,      "혀 내밀기"),
        (GeckoAction.Tongue_FeedCatch, "먹이 받아먹기"),
        (GeckoAction.Tongue_Drink,     "물 마시기"),
        (GeckoAction.Pet_Reaction,     "쓰다듬기 반응"),
        (GeckoAction.Happy_LookUp,     "기뻐하기"),
        (GeckoAction.Jump,             "점프"),
        (GeckoAction.Surprise,         "놀람"),
        (GeckoAction.Angry_TailFlick,  "꼬리 튕기기 (화남)"),
        (GeckoAction.Molt_Start,       "허물 시작 / 실패"),
        (GeckoAction.Molt_Finish,      "허물 성공"),
        (GeckoAction.LevelUp_Pulse,    "성장"),
        (GeckoAction.Blink_Short,      "깜빡임 (눈꺼풀 종)"),
    };

    private static readonly string[] STAGES = { "Hatchling", "Baby", "Juvenile", "Sub-Adult", "Adult" };

    public override bool RequiresConstantRepaint() => Application.isPlaying;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var motor = (GeckoMotor)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("동작 미리보기", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("플레이 모드에서 아래 버튼으로 각 동작을 바로 확인할 수 있습니다.\n기분은 위 '미리보기' 항목으로 강제할 수 있습니다.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("지금", motor.IsBusy ? motor.CurrentAction.ToString() : $"대기 — 기분 {motor.Mood}{(motor.IsMolting ? " · 허물 준비" : "")}");

        for (int i = 0; i < ACTIONS.Length; i += 2)
        {
            EditorGUILayout.BeginHorizontal();
            for (int k = i; k < Mathf.Min(i + 2, ACTIONS.Length); k++)
                if (GUILayout.Button(ACTIONS[k].label, GUILayout.Height(24)))
                    motor.Play(ACTIONS[k].action);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("성장 단계 크기", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        for (int s = 0; s < STAGES.Length; s++)
        {
            bool on = motor.PreviewStage == s;
            if (GUILayout.Toggle(on, STAGES[s], EditorStyles.miniButton) != on)
                motor.PreviewStage = on ? -1 : s;
        }
        EditorGUILayout.EndHorizontal();
        if (motor.PreviewStage >= 0)
            EditorGUILayout.HelpBox("성장 단계를 미리보기로 고정 중입니다. 다시 누르면 실제 데이터를 따릅니다.", MessageType.None);
    }
}
