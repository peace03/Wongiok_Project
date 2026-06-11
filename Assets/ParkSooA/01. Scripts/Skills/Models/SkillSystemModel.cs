using UnityEngine;
using System.Collections.Generic;

public class SkillSystemModel
{
    [SerializeField] private int maxActiveCount = 3;                            // 액티브 스킬 최대 장착 개수
    [SerializeField] private List<SkillInstance> equippedActives = new();       // 장착된 액티브 스킬들
    [SerializeField] private int maxPassiveCount = 4;                           // 패시브 스킬 최대 장착 개수
    [SerializeField] private List<SkillInstance> equippedPassives = new();      // 장착된 패시브 스킬들
    [SerializeField] private List<SkillInstance> unequippedActives = new();     // 미장착 액티브 스킬들

    private Dictionary<int, SkillInstance> allSkills = new();                   // 모든 스킬들

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
        foreach (var skill in skillDatas)
        {
            // 해당 스킬이 없다면
            if(!allSkills.ContainsKey(skill.Id))
            {
                // 스킬 객체 생성 및 저장
                allSkills[skill.Id] = skill.CreateInstance(owner);
                Debug.Log($"[Skill] {owner.name} - {skill.SkillName} 스킬 추가", owner);
            }
            // 해당 스킬이 있다면
            else
                Debug.Log($"[Error | Skill] {skill.SkillName} 스킬 존재 => 입력 - 대상 {owner.name}", owner);
        }
    }

    public void SwapSkill(ACTIVE_SKILL_SLOT_TYPE slot, int id)
    {
        // slot == id

        // id.equipped ? index 찾기 : 스왑
    }
}