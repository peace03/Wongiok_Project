using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class SkillSystemModel
{
    [Header("장착한 액티브 스킬들")]
    [Tooltip("액티브 스킬의 장착 가능한 최대 개수")]
    [SerializeField] private int maxActiveCount = 3;                            // 액티브 스킬 최대 장착 개수
    [Tooltip("장착한 액티브 스킬들")]
    [SerializeField] private List<SkillInstance> equippedActives = new();       // 장착한 액티브 스킬들
    [Header("장착하지 않은 액티브 스킬들")]
    [SerializeField] private List<SkillInstance> unequippedActives = new();     // 장착하지 않은 액티브 스킬들
    [Header("장착한 패시브 스킬들")]
    [Tooltip("패시브 스킬의 장착 가능한 최대 개수")]
    [SerializeField] private int maxPassiveCount = 4;                           // 패시브 스킬 최대 장착 개수
    [Tooltip("장착한 패시브 스킬들")]
    [SerializeField] private List<SkillInstance> equippedPassives = new();      // 장착한 패시브 스킬들
    [Header("모든 스킬들")]
    [SerializeField] private List<SkillInstance> allSkillList = new();          // 모든 스킬 리스트

    private readonly Dictionary<int, SkillInstance> allSkillDictionary          // 모든 스킬 딕셔너리
                                                                    = new();
    private readonly List<EffectAddData> effectDatas = new();                   // 스킬 이펙트 정보 리스트

    public event Action OnActiveSkillsChanged;                                  // 액티브 스킬 변경 이벤트 변수
    public event Action<SkillInstance> OnSkillEnhanced;                         // 스킬 강화 이벤트 변수

    public int MaxActiveCount => maxActiveCount;

    /// <summary>
    /// 생성자
    /// </summary>
    public SkillSystemModel(GameObject owner, ActiveSkillExecuter executer, List<BaseSkillData> skillDatas)
    {
        // 스킬 데이터가 없다면
        if(skillDatas == null)
        {
            //Debug.Log($"[Error | Skill] 스킬 객체 생성 실패 => 데이터 : 없음");
            return;
        }

        // 스킬 데이터의 수만큼
        foreach (var data in skillDatas)
        {
            // 해당 스킬이 없다면
            if(!allSkillDictionary.ContainsKey(data.Id))
            {
                // 스킬 객체 생성 및 저장
                allSkillDictionary[data.Id] = data.CreateInstance(owner, executer);
                allSkillList.Add(allSkillDictionary[data.Id]);
                // 장착한 액티브 스킬들과 패시브 스킬들 리스트 연결
                allSkillDictionary[data.Id].SetEquippedSkills(equippedActives, equippedPassives);

                // 액티브 스킬이라면
                if (allSkillDictionary[data.Id].IsActiveSkill)
                    // 미장착한 액티브 스킬 리스트에 추가
                    unequippedActives.Add(allSkillDictionary[data.Id]);

                //Debug.Log($"[Skill] 스킬 추가 => {owner.name} : {data.SkillName}", owner);
            }
            // 해당 스킬이 있다면
            else
                Debug.Log($"[Error | Skill] {data.SkillName} 스킬 존재 => 입력 - 대상 {owner.name}\n", owner);
        }

        // 스킬 장착
        EquipSkills();
    }

    /// <summary>
    /// 스킬 장착 함수
    /// </summary>
    private void EquipSkills()
    {
        // 모든 스킬들의 수만큼
        foreach(var skill in allSkillList)
        {
            // 챕터 1의 스킬이 아니거나, 장착할 액티브 슬롯이 없거나, 장착할 패시브 슬롯이 없다면
            if (skill.BaseData.UnlockChapter != CHAPTER_TYPE.First ||
                (skill.IsActiveSkill && equippedActives.Count >= maxActiveCount) ||
                (!skill.IsActiveSkill && equippedPassives.Count >= maxPassiveCount))
                continue;

            // 액티브 스킬이고 장착할 액티브 슬롯이 있다면
            if (skill.IsActiveSkill && equippedActives.Count < maxActiveCount)
            {
                // 액티브 스킬 장착
                equippedActives.Add(skill);
                // 스킬 이펙트 정보 리스트 설정하기
                SetEffectDatas(skill.ActiveData.Effects);
                // 추가할 무기 외형 정보 이벤트 발행
                EventBus<WeaponVisualAddData>.Publish(new WeaponVisualAddData(skill.ActiveData.Id,
                                                                                skill.ActiveData.Weapon));
                // 추가할 이펙트 정보들 이벤트 발행
                EventBus<EffectAddDatas>.Publish(new EffectAddDatas(effectDatas));
                // 미장착한 액티브 스킬 리스트에서 제거
                unequippedActives.Remove(skill);
                Debug.Log($"[Active | Skill] 스킬 장착 => " +
                            $"위치 : {(equippedActives.Count == 1 ? "A" : equippedActives.Count == 2 ? "S" : "D")}" +
                            $" / {skill.BaseData.SkillName}");
            }
            // 장착할 패시브 슬롯이 있다면
            else if (!skill.IsActiveSkill && equippedPassives.Count < maxPassiveCount)
            {
                // 패시브 스킬 장착
                equippedPassives.Add(skill);
                // 패시브 스킬 사용
                skill.UseSkill();
            }
        }

        // 장착할 액티브 슬롯이 남았다면
        if (equippedActives.Count < maxActiveCount)
        {
            //Debug.Log($"[Active | Skill] 빈 슬롯 => {maxActiveCount - equippedActives.Count}개");

            // 남은 액티브 슬롯 칸 수만큼
            for (int i = equippedActives.Count; i < maxActiveCount; i++)
                // 빈 칸 생성
                equippedActives.Add(null);
        }

        // 장착할 패시브 슬롯이 남았다면
        if (equippedPassives.Count < maxPassiveCount)
        {
            //Debug.Log($"[Passive | Skill] 빈 슬롯 => {maxPassiveCount - equippedPassives.Count}개");

            // 남은 패시브 슬롯 칸 수만큼
            for (int i = equippedPassives.Count; i < maxPassiveCount; i++)
                // 빈 칸 생성
                equippedPassives.Add(null);
        }
    }

    /// <summary>
    /// 스킬 이펙트 정보 리스트 설정 함수
    /// </summary>
    /// <param name="effects">액티브 스킬 이펙트 리스트</param>
    private void SetEffectDatas(IReadOnlyList<ActiveSkillEffect> effects)
    {
        // 리스트가 없거나, 비어있다면
        if (effects == null || effects.Count == 0)
            return;

        // 리스트 초기화
        effectDatas.Clear();

        // 이펙트들의 수만큼
        for (int i = 0; i < effects.Count; i++)
            // 이펙트 프리팹이 있다면
            if (effects[i].prefab != null)
                // 스킬 이펙트 정보 리스트에 추가
                effectDatas.Add(new EffectAddData(effects[i].prefab));
    }

    /// <summary>
    /// 장착한 액티브 스킬들 반환 함수
    /// </summary>
    public void GetEquippedActiveSkills(List<SkillInstance> results)
    {
        // 결과를 담을 리스트가 없다면
        if (results == null)
        {
            //Debug.Log($"[Error | Skill] 장착한 액티브 스킬들 반환 실패 => 입력 - 리스트 : 없음");
            return;
        }

        // 리스트 초기화
        results.Clear();

        // 장착한 액티브 스킬들의 수만큼
        foreach (var skill in equippedActives)
            // 결과 리스트에 추가
            results.Add(skill);
    }

    /// <summary>
    /// 장착한 패시브 스킬들 반환 함수
    /// </summary>
    public void GetEquippedPassiveSkills(List<SkillInstance> results)
    {
        // 결과를 담을 리스트가 없다면
        if (results == null)
        {
            //Debug.Log($"[Error | Skill] 장착한 액티브 스킬들 반환 실패 => 입력 - 리스트 : 없음");
            return;
        }

        // 리스트 초기화
        results.Clear();

        // 장착한 패시브 스킬들의 수만큼
        foreach (var skill in equippedPassives)
            // 결과 리스트에 추가
            results.Add(skill);
    }

    /// <summary>
    /// 미장착한 액티브 스킬들 반환 함수
    /// </summary>
    public void GetUnequippedActiveSkills(List<SkillInstance> results)
    {
        // 결과를 담을 리스트가 없다면
        if (results == null)
        {
            //Debug.Log($"[Error | Skill] 액티브 스킬들 반환 실패 => 입력 - 리스트 : 없음");
            return;
        }

        // 리스트 초기화
        results.Clear();

        // 미장착한 액티브 스킬들의 수만큼
        foreach (var skill in unequippedActives)
            // 결과 리스트에 추가
            results.Add(skill);
    }

    /// <summary>
    /// 액티브 스킬들 반환 함수
    /// </summary>
    public void GetActiveSkills(List<SkillInstance> results)
    {
        // 결과를 담을 리스트가 없다면
        if (results == null)
        {
            //Debug.Log($"[Error | Skill] 액티브 스킬들 반환 실패 => 입력 - 리스트 : 없음");
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

    /// <summary>
    /// 강화 가능한 스킬들 반환 함수
    /// </summary>
    public void GetCanEnhanceSkills(List<SkillInstance> results)
    {
        // 결과를 담을 리스트가 없다면
        if (results == null)
        {
            //Debug.Log($"[Error | Skill] 강화 가능한 스킬들 반환 실패 => 입력 - 리스트 : 없음");
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

    /// <summary>
    /// 액티브 스킬 실행 함수
    /// </summary>
    public void ExecuteActiveSkill(ACTIVE_SKILL_SLOT_TYPE slot)
    {
        // 해당 슬롯이 비어있다면
        if (equippedActives[(int)slot] == null || equippedActives[(int)slot].BaseData == null)
        {
            //Debug.Log($"[Skill] 실행할 액티브 스킬 없음 => 입력 - 슬롯 : {slot.ToKoreanString()}");
            return;
        }

        // 스킬 실행
        equippedActives[(int)slot].UseSkill();
    }

    /// <summary>
    /// 액티브 스킬 취소 함수
    /// </summary>
    public void CancelActiveSkill(ACTIVE_SKILL_SLOT_TYPE slot)
    {
        // 해당 슬롯이 비어있다면
        if (equippedActives[(int)slot] == null || equippedActives[(int)slot].BaseData == null)
        {
            //Debug.Log($"[Skill] 취소할 액티브 스킬 없음 => 입력 - 슬롯 : {slot.ToKoreanString()}");
            return;
        }

        // 스킬 취소
        equippedActives[(int)slot].CancelSkill();
    }

    /// <summary>
    /// 장착한 액티브 스킬들 시간 진행 함수
    /// </summary>
    public void TickActiveSkills(float time)
    {
        // 장착한 액티브 스킬들의 수만큼
        for(int i = 0; i < maxActiveCount; i++)
        {
            // 장착된 액티브 스킬이 없거나, 스킬 정보가 비어있다면
            if (equippedActives[i] == null || equippedActives[i].BaseData == null)
                continue;

            // 시간 진행
            equippedActives[i].Tick(time);
        }
    }

    /// <summary>
    /// 스킬 변경 함수
    /// </summary>
    public bool SwapSkill(ACTIVE_SKILL_SLOT_TYPE slot, int? id = null)
    {
        // 스킬 변경 성공 여부
        bool result = false;

        // ID가 비어있다면
        if(id == null)
        {
            // 미장착한 액티브 스킬에 추가
            unequippedActives.Add(equippedActives[(int)slot]);
            // 해당 슬롯 비우기
            equippedActives[(int)slot] = null;
            result = true;
        }
        // ID에 해당하는 스킬이 없다면
        else if (!allSkillDictionary.TryGetValue((int)id, out var skill))
            Debug.Log($"[Error | Skill] 해당 스킬 없음 => 입력 - ID : {id}");
        // 슬롯 종류가 액티브 스킬 최대 장착 개수를 넘어간다면
        else if ((int)slot >= maxActiveCount)
            Debug.Log($"[Error | Skill] 액티브 최대 장착 개수 오버 => " +
                        $"입력 - {slot.ToKoreanString()} / 최대 장착 개수 :{maxActiveCount}");
        // 패시브 스킬이라면
        else if (!skill.IsActiveSkill)
            Debug.Log($"[Error | Skill] 패시브 스킬 => 입력 - ID :{id} / {skill.BaseData.SkillName}");
        // 해당 슬롯에 장착된 스킬이라면
        else if (equippedActives[(int)slot]?.BaseData.Id == id)
            result = true;
        // 슬롯이 비어있다면
        else if (equippedActives[(int)slot] == null || equippedActives[(int)slot].BaseData == null)
        {
            // 해당 슬롯에 변경할 스킬 저장
            equippedActives[(int)slot] = skill;
            // 스킬 이펙트 정보 리스트 설정하기
            SetEffectDatas(skill.ActiveData.Effects);
            // 무기 외형 정보 이벤트 발행
            EventBus<WeaponVisualAddData>.Publish(new WeaponVisualAddData(skill.ActiveData.Id,
                                                                        skill.ActiveData.Weapon));
            // 스킬 이펙트 정보들 이벤트 발행
            EventBus<EffectAddDatas>.Publish(new EffectAddDatas(effectDatas));
            // 미장착한 액티브 스킬 리스트에서 제거
            unequippedActives.Remove(skill);
            result = true;
        }
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
        // 장착되지 않은 스킬이라면
        else if (!skill.IsEquipped)
        {
            // 미장착한 액티브 스킬 리스트에 추가
            unequippedActives.Add(equippedActives[(int)slot]);
            // 해당 슬롯에 변경할 스킬 저장
            equippedActives[(int)slot] = skill;
            // 스킬 이펙트 정보 리스트 설정하기
            SetEffectDatas(skill.ActiveData.Effects);
            // 무기 외형 정보 이벤트 발행
            EventBus<WeaponVisualAddData>.Publish(new WeaponVisualAddData(skill.ActiveData.Id,
                                                                        skill.ActiveData.Weapon));
            // 스킬 이펙트 정보들 이벤트 발행
            EventBus<EffectAddDatas>.Publish(new EffectAddDatas(effectDatas));
            // 미장착한 액티브 스킬 리스트에서 제거
            unequippedActives.Remove(skill);
            result = true;
        }

        // 변경된 스킬이 있다면
        if (result)
            // 액티브 스킬 변경 이벤트 발행
            OnActiveSkillsChanged?.Invoke();

        // 결과 반환
        return result;
    }

    /// <summary>
    /// 스킬 강화 함수
    /// </summary>
    public bool EnhanceSkill(int id)
    {
        // 스킬 강화 성공 여부
        bool result = false;

        // ID에 해당하는 스킬이 없다면
        if (!allSkillDictionary.TryGetValue(id, out var skill))
            Debug.Log($"[Error | Skill] 해당 스킬 없음 => 입력 - ID : {id}");
        // 강화가 가능하지 않다면
        else if (!skill.CanEnhance)
            Debug.Log($"[Error | Skill] 강화 불가능(최대 레벨) => " +
                        $"입력 - ID :{id} / {skill.BaseData.SkillName}");
        else
        {
            // 스킬 강화
            skill.LevelUp();
            result = true;
        }

        // 스킬 강화에 성공했다면
        if (result)
            // 스킬 강화 이벤트 발행
            OnSkillEnhanced?.Invoke(skill);

        // 결과 반환
        return result;
    }
}