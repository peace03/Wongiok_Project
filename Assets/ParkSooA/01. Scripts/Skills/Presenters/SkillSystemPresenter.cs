using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class SkillSystemPresenter
{
    [Header("(임시)현재 챕터")]
    [SerializeField] private CHAPTER_TYPE curChapter = CHAPTER_TYPE.First;      // 현재 챕터

    [Space(10)][Header("스킬 모델")]
    [SerializeField] private SkillSystemModel model;                            // 스킬 모델(인스펙터에서 보는 용도)
    //[SerializeField] private readonly SkillSystemModel model;                 // 스킬 모델

    private List<SkillInstance> modelResults = new();                           // 스킬 모델 결과들
    private List<UIPlayerSkillSlotData> uiEventDatas = new();                   // 스킬 뷰 이벤트 데이터들

    // 생성자
    public SkillSystemPresenter(GameObject owner)
    {
        // 스킬 데이터를 담을 리스트
        List<BaseSkillData> skillDatas = new();
        // 현재 챕터의 스킬 데이터 받아오기
        SkillDatabase.FindDatasByChapter(curChapter, skillDatas);   // 나중에 현재 챕터 부분 수정해야 함

        // 스킬 데이터가 있다면
        if(skillDatas.Count != 0)
        {
            // 스킬 모델 생성하기
            model = new(owner, skillDatas);
            //model.OnActiveSkillsChanged
        }
        // 스킬 데이터가 없다면
        else
            Debug.LogError($"[Error | Skill] 스킬 모델 생성 실패 => 입력 - 대상 : {owner.name} / 스킬 데이터 : 없음", owner);
    }

    // 액티브 스킬들 새로고침 함수
    public void RefreshActiveSkills()
    {
        // 모델이 없다면
        if (model == null)
        {
            Debug.LogError($"[Error | Skill] 액티브 스킬들 새로고침 실패 => 입력 - 스킬 모델 : 없음");
            return;
        }

        // 장착한 액티브 스킬들 받아오기
        model.GetEquippedActiveSkills(modelResults);
        // UI용 데이터 리스트 초기화
        uiEventDatas.Clear();

        // 스킬이 있다면
        if (modelResults.Count != 0)
            // 장착한 액티브 스킬들의 개수만큼
            for (int i = 0; i < modelResults.Count; i++)
                // 액티브 스킬 최대 장착 개수까지
                if (i < model.MaxActiveCount)
                    // UI용 데이터 리스트에 추가
                    uiEventDatas.Add(ChangeToUIData((ACTIVE_SKILL_SLOT_TYPE)i, modelResults[i]));
        // 스킬이 없다면
        else
            Debug.Log($"[Skill] UI용 데이터 리스트 추가 실패 => 장착한 액티브 스킬 : 없음");

        // 장착한 액티브 스킬 이벤트 발행
        EventBus<UISetPlayerSkillSlotsEvent>.Publish(new UISetPlayerSkillSlotsEvent(uiEventDatas.ToArray()));
    }

    // UI용 데이터로 변환해서 반환하는 함수
    private UIPlayerSkillSlotData ChangeToUIData(ACTIVE_SKILL_SLOT_TYPE slot, SkillInstance skill = null)
    {
        // 슬롯 범위가 액티브 스킬 최대 장착 개수를 넘어갔다면
        if ((int)slot >= model.MaxActiveCount)
        {
            Debug.LogError($"[Error | Skill] UI용 데이터 변환 실패 => " +
                            $"입력 - {slot.ToKoreanString()} / 최대 장착 개수 : {model.MaxActiveCount} / " +
                            $"스킬 : {(skill == null ? "없음" : skill.Data.SkillName)}");
            return default;
        }

        // 스킬이 있다면
        if (skill == null)
            // UI용 데이터로 변환해서 반환하기
            return new(skill.Data.Icon, slot.ToKoreanString(), skill.CurLevel, skill.CoolTimeRatio, skill.IsReady);
        // 스킬이 없다면
        else
            // 스킬 입력 키만 넣어서 반환하기
            return new(null, slot.ToKoreanString(), 0, 0f, false);
    }

    // 액티브 스킬 실행
    public void ExecuteActiveSkill(ACTIVE_SKILL_SLOT_TYPE slot)
    {
        // 모델이 없다면
        if (model == null)
        {
            Debug.LogError($"[Error | Skill] 액티브 스킬 실행 실패 => 입력 - 스킬 모델 : 없음");
            return;
        }

        // 액티브 스킬 실행
        model.ExecuteActiveSkill(slot);
    }
}