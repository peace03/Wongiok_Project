using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class SkillSystemModel
{
    [Space(10)][Header("장착한 액티브 스킬")]
    [SerializeField] private int maxActiveCount = 3;                                // 액티브 스킬 최대 장착 개수
    [SerializeField] private List<SkillInstance> equippedActives = new();           // 장착한 액티브 스킬들

    [Space(10)][Header("장착한 패시브 스킬")]
    [SerializeField] private int maxPassiveCount = 4;                               // 패시브 스킬 최대 장착 개수
    [SerializeField] private List<SkillInstance> equippedPassives = new();          // 장착한 패시브 스킬들

    [Space(10)][Header("모든 스킬들")]
    [SerializeField] private List<SkillInstance> allSkillList = new();              // 모든 스킬 리스트

    private readonly Dictionary<int, SkillInstance> allSkillDictionary = new();     // 모든 스킬 딕셔너리

    // 생성자
    public SkillSystemModel(GameObject owner, List<BaseSkillData> skillDatas)
    {
        // 스킬 데이터가 없다면
        if(skillDatas == null)
        {
            Debug.Log($"[Error | Skill] 스킬 객체 생성 실패 => 데이터 없음");
            return;
        }

        // 스킬 데이터의 수만큼
        foreach (var data in skillDatas)
        {
            // 해당 스킬이 없다면
            if(!allSkillDictionary.ContainsKey(data.Id))
            {
                // 스킬 객체 생성 및 저장
                allSkillDictionary[data.Id] = data.CreateInstance(owner);
                allSkillList.Add(allSkillDictionary[data.Id]);
                // 장착한 액티브 스킬들과 패시브 스킬들 리스트 연결
                allSkillDictionary[data.Id].SetEquippedSkills(equippedActives, equippedPassives);
                Debug.Log($"[Skill] {owner.name} - {data.SkillName} 스킬 추가", owner);
            }
            // 해당 스킬이 있다면
            else
                Debug.Log($"[Error | Skill] {data.SkillName} 스킬 존재 => 입력 - 대상 {owner.name}\n", owner);
        }

        // 스킬 장착
        EquipSkills();
    }

    // 스킬 장착 함수
    private void EquipSkills()
    {
        // 모든 스킬들의 수만큼
        foreach(var skill in allSkillList)
        {
            // 챕터 1의 스킬이 아니거나, 장착할 액티브 슬롯이 없거나, 장착할 패시브 슬롯이 없다면
            if (skill.Data.UnlockChapter != CHAPTER_TYPE.First ||
                (skill.IsActiveSkill && equippedActives.Count >= maxActiveCount) ||
                (!skill.IsActiveSkill && equippedPassives.Count >= maxPassiveCount))
                continue;

            // 장착할 액티브 슬롯이 있다면
            if (skill.IsActiveSkill && equippedActives.Count < maxActiveCount)
            {
                // 액티브 스킬 장착
                equippedActives.Add(skill);
                Debug.Log($"[Active | Skill] {skill.Data.SkillName} 장착");
            }
            // 장착할 패시브 슬롯이 있다면
            else if (!skill.IsActiveSkill && equippedPassives.Count < maxPassiveCount)
            {
                // 패시브 스킬 장착
                equippedPassives.Add(skill);
                Debug.Log($"[Passive | Skill] {skill.Data.SkillName} 장착");
            }
        }

        // 장착할 액티브 슬롯이 남았다면
        if (equippedActives.Count < maxActiveCount)
        {
            Debug.Log($"[Active | Skill] 빈 칸 {maxActiveCount - equippedActives.Count}개");

            // 남은 액티브 슬롯 칸 수만큼
            for (int i = equippedActives.Count; i < maxActiveCount; i++)
                // 빈 칸 생성
                equippedActives.Add(null);
        }

        // 장착할 패시브 슬롯이 남았다면
        if (equippedPassives.Count < maxPassiveCount)
        {
            Debug.Log($"[Passive | Skill] 빈 칸 {maxPassiveCount - equippedPassives.Count}개");

            // 남은 패시브 슬롯 칸 수만큼
            for (int i = equippedPassives.Count; i < maxPassiveCount; i++)
                // 빈 칸 생성
                equippedPassives.Add(null);
        }
    }

    // 장착한 액티브 스킬들 반환 함수
    public void GetEquippedActiveSkills(List<SkillInstance> results)
    {
        // 결과를 담을 리스트가 없다면
        if (results == null)
        {
            Debug.Log($"[Error | Skill] 장착한 액티브 스킬들 반환 실패 => 입력 - 리스트(없음)");
            return;
        }

        // 리스트 초기화
        results.Clear();

        // 장착한 액티브 스킬들의 수만큼
        foreach (var skill in equippedActives)
            // 결과 리스트에 추가
            results.Add(skill);
    }

    // 장착한 패시브 스킬들 반환 함수
    public void GetEquippedPassiveSkills(List<SkillInstance> results)
    {
        // 결과를 담을 리스트가 없다면
        if (results == null)
        {
            Debug.Log($"[Error | Skill] 장착한 액티브 스킬들 반환 실패 => 입력 - 리스트(없음)");
            return;
        }

        // 리스트 초기화
        results.Clear();

        // 장착한 패시브 스킬들의 수만큼
        foreach (var skill in equippedPassives)
            // 결과 리스트에 추가
            results.Add(skill);
    }

    // 액티브 스킬들 반환 함수
    public void GetActiveSkills(List<SkillInstance> results)
    {
        // 결과를 담을 리스트가 없다면
        if (results == null)
        {
            Debug.Log($"[Error | Skill] 액티브 스킬들 반환 실패 => 입력 - 리스트(없음)");
            return;
        }

        // 리스트 초기화
        results.Clear();

        // 모든 스킬들의 수만큼
        foreach (var skill in allSkillList)
            // 액티브 스킬이라면
            if (skill.IsActiveSkill)
                // 결과 리스트에 추가
                results.Add(skill);
    }

    // 스킬 변경 함수
    public bool SwapSkill(ACTIVE_SKILL_SLOT_TYPE slot, int id)
    {
        // 스킬 변경 성공 여부
        bool result = false;

        // ID에 해당하는 스킬이 없다면
        if (!allSkillDictionary.TryGetValue(id, out var skill))
            Debug.Log($"[Error | Skill] 해당 스킬 없음 => 입력 - ID({id})");
        // 슬롯 종류가 액티브 스킬 최대 장착 개수를 넘어간다면
        else if ((int)slot >= maxActiveCount)
            Debug.Log($"[Error | Skill] 액티브 최대 장착 개수 오버 => " +
                $"입력 - {slot.ToKoreanString()} | 최대 장착 개수({maxActiveCount})");
        // 패시브 스킬이라면
        else if (!skill.IsActiveSkill)
            Debug.Log($"[Error | Skill] 패시브 스킬 => 입력 - ID({id}) | {skill.Data.SkillName}");
        // 해당 슬롯에 장착된 스킬이라면
        else if (equippedActives[(int)slot]?.Data.Id == id)
            result = true;
        // 장착되어 있는 스킬이라면
        else if (skill.IsEquipped)
        {
            // 변경할 스킬의 장착 위치 찾기
            int swapedIndex = equippedActives.IndexOf(skill);
            // 해당 슬롯에 있는 스킬 저장
            var swapedSkill = equippedActives[(int)slot];
            // 해당 슬롯에 변경할 스킬 저장
            equippedActives[(int)slot] = skill;
            // 변경할 스킬의 장착 위치에, 해당 슬롯에 있던 스킬 저장
            equippedActives[swapedIndex] = swapedSkill;
            result = true;
        }
        // 슬롯이 비어있거나, 장착되지 않은 스킬이라면
        else if (equippedActives[(int)slot] == null || !skill.IsEquipped)
        {
            // 해당 슬롯에 변경할 스킬 저장
            equippedActives[(int)slot] = skill;
            result = true;
        }

        // 결과 반환
        return result;
    }

    // 강화 가능한 스킬들 반환 함수
    public void GetCanEnhanceSkills(List<SkillInstance> results)
    {
        // 결과를 담을 리스트가 없다면
        if (results == null)
        {
            Debug.Log($"[Error | Skill] 강화 가능한 스킬들 반환 실패 => 입력 - 리스트(없음)");
            return;
        }

        // 리스트 초기화
        results.Clear();

        // 모든 스킬들의 수만큼
        foreach (var skill in allSkillList)
            // 강화 가능한 스킬이라면
            if (skill.CanEnhance)
                // 결과 리스트에 추가
                results.Add(skill);
    }

    // 스킬 강화 함수
    public bool EnhanceSkill(int id)
    {
        // 스킬 변경 성공 여부
        bool result = false;

        // ID에 해당하는 스킬이 없다면
        if (!allSkillDictionary.TryGetValue(id, out var skill))
            Debug.Log($"[Error | Skill] 해당 스킬 없음 => 입력 - ID({id})");
        // 강화가 가능하지 않다면
        else if (!skill.CanEnhance)
            Debug.Log($"[Error | Skill] 강화 불가능(최대 레벨) => 입력 - ID({id}) | {skill.Data.SkillName}");
        else
        {
            // 스킬 강화
            skill.LevelUp();
            result = true;
        }

        // 결과 반환
        return result;
    }
}