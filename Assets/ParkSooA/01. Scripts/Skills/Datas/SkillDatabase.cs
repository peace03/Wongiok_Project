using UnityEngine;
using System.Collections.Generic;

public static class SkillDatabase
{
    private static readonly Dictionary<int, BaseSkillData> skillDatas = new();      // 스킬 정보들

    public static IReadOnlyDictionary<int, BaseSkillData> SkillDatas => skillDatas;

    static SkillDatabase()
    {
        // 스킬 정보들 가져오기
        var datas = Resources.LoadAll<BaseSkillData>("Datas/Skill/");

        // 가져온 스킬 정보들의 수만큼
        foreach(var data in datas)
        {
            // ID에 해당하는 스킬 정보가 없다면
            if (!skillDatas.TryGetValue(data.Id, out var existData))
                // 스킬 정보 추가
                skillDatas[data.Id] = data;
            // ID에 해당하는 스킬 정보가 있다면
            else
                Debug.Log($"[Error | Skill] 해당 데이터 있음 => " +
                    $"입력 : {data.SkillName}[ID({data.Id})] | {existData.SkillName}[ID({existData.Id})]");
        }
    }

    // 스킬 정보 찾기 함수
    public static BaseSkillData FindData(int id, bool viewLog = true)
    {
        // ID에 해당하는 스킬 정보가 없다면
        if(!skillDatas.TryGetValue(id, out var data))
        {
            if(viewLog)
                Debug.Log($"[Error | Skill] 해당 데이터 없음 => 입력 : ID({id})");

            return null;
        }

        return data;
    }

    // 스킬 정보들 찾기 함수
    public static void FindDatas(int[] ids, List<BaseSkillData> results)
    {
        // ID들이 없거나, 찾는 ID가 없거나, 결과를 담을 List가 없다면
        if(ids == null || ids.Length == 0 || results == null)
        {
            Debug.Log($"[Error | Skill] 필수 검색 조건 부족 => 입력 : ID({(ids == null ? "없음" : $"있음({ids.Length})")}), "
                                                                            + $"List({(results == null ? "없음" : "있음")})");
            return;
        }

        // List 초기화
        results.Clear();
        // 데이터를 담을 변수
        BaseSkillData data;

        // ID의 수만큼
        foreach (int id in ids)
        {
            // 데이터 찾기
            data = FindData(id);

            // ID에 해당하는 데이터가 있다면
            if (data != null)
                // 결과 List에 추가
                results.Add(data);
        }
    }
}