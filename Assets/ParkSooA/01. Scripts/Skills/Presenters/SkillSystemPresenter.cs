using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class SkillSystemPresenter
{
    [Header("(임시)현재 챕터")]
    [SerializeField] private CHAPTER_TYPE curChapter = CHAPTER_TYPE.First;      // 현재 챕터

    [Space(10)][Header("스킬 모델")]
    [SerializeField] private readonly SkillSystemModel model;                   // 스킬 모델

    private readonly ISkillView view;                                           // 스킬 UI

    private List<SkillInstance> modelResults = new();                           // 스킬 모델 결과들

    // 생성자
    public SkillSystemPresenter(GameObject owner, ISkillView view)
    {
        // 스킬 데이터를 담을 리스트
        List<BaseSkillData> skillDatas = new();
        // 현재 챕터의 스킬 데이터 받아오기
        SkillDatabase.FindDatasByChapter(curChapter, skillDatas);   // 나중에 현재 챕터 부분 수정해야 함

        // 스킬 데이터가 있다면
        if(skillDatas.Count != 0)
            // 스킬 모델 생성하기
            model = new(owner, skillDatas);
        // 스킬 데이터가 없다면
        else
            Debug.Log($"[Error | Skill] 스킬 모델 생성 실패 => 입력 - 대상 {owner.name} | 스킬 데이터(없음)", owner);

        // 스킬 UI 저장
        this.view = view;
        //view.RefreshSkillSlots()
    }

    // 장착한 액티브 스킬들 받아오기 함수
    private bool TryGetEquippedActiveSkills()
    {
        // 모델이 없다면
        if(model == null)
        {
            Debug.Log($"[Error | Skill] 장착한 액티브 스킬들 받아오기 실패 => 입력 - 스킬 모델(없음)");
            return false;
        }

        // 장착한 액티브 스킬들 받아오기
        model.GetEquippedActiveSkills(modelResults);

        // 장착한 액티브 스킬들의 개수 결과 반환
        return modelResults.Count != 0;
    }

    //private UIPlayerSkillSlotData ChangeToUIData(ACTIVE_SKILL_SLOT_TYPE slot, SkillInstance skill)
    //{
    //    return new(skill.Data.Icon, slot.ToKoreanString(), skill.CurLevel, )
    //}
}