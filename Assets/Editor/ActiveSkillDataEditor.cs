using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

// ActiveSkillData 및 이를 상속받는 모든 자식 SO의 인스펙터를 확장합니다
[CustomEditor(typeof(ActiveSkillData), true)]
public class ActiveSkillDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 1. 기존 유니티 인스펙터 기본 필드들을 먼저 그립니다 (Id, 스킬이름 등)
        DrawDefaultInspector();

        ActiveSkillData data = (ActiveSkillData)target;

        GUILayout.Space(20);
        GUILayout.Label("=== [SerializeReference] 데이터 동적 주입 ===", EditorStyles.boldLabel);

        // 2. 인스펙터에 직관적인 버튼 배치
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("발사체 레벨 데이터 추가 (+)", GUILayout.Height(35)))
        {
            AddLevelData(data, new ProjectileSkillLevelData());
        }
        if (GUILayout.Button("영역 레벨 데이터 추가 (+)", GUILayout.Height(35)))
        {
            AddLevelData(data, new AreaSkillLevelData());
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        EditorGUILayout.HelpBox("리스트의 Size를 직접 늘리면 빈 칸만 뜹니다! 위의 버튼을 눌러 레벨을 추가하세요.", MessageType.Info);
    }

    private void AddLevelData(ActiveSkillData data, ActiveSkillLevelData newLevelData)
    {
        Type type = data.GetType();
        FieldInfo field = null;

        // 제네릭 부모 클래스(LevelBasedSkillData)까지 거슬러 올라가며 levelDatas 필드를 찾습니다
        while (type != null && field == null)
        {
            field = type.GetField("levelDatas", BindingFlags.NonPublic | BindingFlags.Instance);
            type = type.BaseType;
        }

        if (field != null)
        {
            System.Collections.IList list = field.GetValue(data) as System.Collections.IList;
            if (list == null)
            {
                list = (System.Collections.IList)Activator.CreateInstance(field.FieldType);
                field.SetValue(data, list);
            }

            // 되돌리기(Ctrl+Z) 및 변경사항 저장을 위해 유니티 엔진에 기록
            Undo.RecordObject(data, "Add Level Data");
            list.Add(newLevelData); // 강제로 메모리에 구체 객체 꽂아넣기!

            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets(); // 다른 컴퓨터로 이동 시 데이터가 날아가지 않도록 즉시 파일 저장
        }
    }
}