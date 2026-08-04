using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class SkillDatabase
{
    private static readonly Dictionary<int, BaseSkillData> skillDataDictionary = new();     // 스킬 정보들 딕셔너리
    private static readonly List<BaseSkillData> skillDataList = new();                      // 스킬 정보들 리스트

    private static bool isInit = false;                                                     // 초기화 여부

    public static IReadOnlyDictionary<int, BaseSkillData> SkillDataDictionary => skillDataDictionary;

    /// <summary>
    /// 초기화 함수
    /// </summary>
    public static void Init()
    {
        // 초기화가 됐다면
        if(isInit)
            return;

        // ID 오름차순으로 스킬 정보들 가져오기
        var datas = Resources.LoadAll<BaseSkillData>("Datas/Skills/").OrderBy(data => data.Id);

        // 가져온 스킬 정보들의 수만큼
        foreach(var data in datas)
        {
            if (data == null)
                continue;

            // ID에 해당하는 스킬 정보가 없다면
            if (!skillDataDictionary.TryGetValue(data.Id, out var existData))
            {
                // 액티브 스킬 데이터 변환이 가능하다면
                if(data.AsActiveData != null)
                {
                    // 이펙트 정렬하기
                    data.AsActiveData.SortEffects();
                    // 사운드 정렬하기
                    data.AsActiveData.SortSounds();
                }

                // 스킬 정보 추가
                skillDataDictionary[data.Id] = data;
                skillDataList.Add(data);
            }
            // ID에 해당하는 스킬 정보가 있다면
            else
                Debug.Log($"[Error | Skill] 해당 데이터 있음 => " +
                            $"입력 - {data.SkillName} : ID({data.Id}) / " +
                            $"{existData.SkillName} : ID({existData.Id})");
        }

        // 초기화됨
        isInit = true;
        //Debug.Log($"[Skill] 데이터베이스 초기화 완료");
    }

    /// <summary>
    /// ID로 스킬 정보 찾는 함수
    /// </summary>
    /// <param name="id">스킬 ID</param>
    /// <param name="viewLog">로그 출력 여부</param>
    /// <returns></returns>
    public static BaseSkillData FindDataById(int id, bool viewLog = true)
    {
        // ID에 해당하는 스킬 정보가 없다면
        if(!skillDataDictionary.TryGetValue(id, out var data))
        {
            // 로그 출력 여부에 따라
            if(viewLog)
                Debug.Log($"[Error | Skill] 해당 데이터 없음 => 입력 - ID : {id}");

            return null;
        }

        return data;
    }

    /// <summary>
    /// ID로 스킬 정보들 찾는 함수
    /// </summary>
    /// <param name="ids">스킬 ID들</param>
    /// <param name="results">스킬 정보들</param>
    public static void FindDatasById(int[] ids, List<BaseSkillData> results)
    {
        // ID들이 없거나, 찾는 ID가 없거나, 결과를 담을 리스트가 없다면
        if(ids == null || ids.Length == 0 || results == null)
        {
            Debug.Log($"[Error | Skill] 필수 검색 조건 부족 => " +
                        $"입력 - ID : {(ids == null ? "없음" : $"있음({ids.Length})")} / " +
                        $"리스트 : {(results == null ? "없음" : "있음")}");
            return;
        }

        // 리스트 초기화
        results.Clear();
        // 데이터를 담을 변수
        BaseSkillData data;

        // ID의 수만큼
        foreach (int id in ids)
            // ID에 해당하는 데이터가 있다면
            if ((data = FindDataById(id)) != null)
                // 결과 리스트에 추가
                results.Add(data);
    }

    /// <summary>
    /// 챕터 종류로 스킬 정보들 찾는 함수
    /// </summary>
    /// <param name="chapter">챕터 종류</param>
    /// <param name="results">스킬 정보들</param>
    public static void FindDatasByChapter(CHAPTER_TYPE chapter, List<BaseSkillData> results)
    {
        // 결과를 담을 리스트가 없다면
        if(results == null)
        {
            Debug.Log($"[Error | Skill] 필수 검색 조건 부족 => " +
                        $"입력 - {chapter.ToKoreanString()} / 리스트 : 없음");
            return;
        }

        // 리스트 초기화
        results.Clear();

        // 스킬 정보들의 수만큼
        foreach (var data in skillDataList)
            // 해금 챕터 종류와 같다면
            if (data.UnlockChapter == chapter)
                // 겨로가 리스트에 추가
                results.Add(data);
    }
}