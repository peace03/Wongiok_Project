using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class SkillSystemPresenter : ISkillSystemProvider
{
    [Header("(임시)현재 챕터")]
    [SerializeField] private CHAPTER_TYPE curChapter = CHAPTER_TYPE.First;      // 현재 챕터
    [SerializeField] private SkillSystemModel model;                            // 스킬 모델

    private readonly List<SkillInstance> modelResults = new();                  // 스킬 모델 결과들 리스트
    private readonly List<BaseSkillData> databaseResults = new();               // 스킬 데이터베이스 결과들 리스트
    private readonly List<UIPauseSkillInfoData> equippedSkillUIDatas            // 장착한 스킬 UI 데이터들 리스트
                                                                = new();
    private readonly List<UIPauseSkillInfoData> unequippedSkillUIDatas          // 미장착한 스킬 UI 데이터들 리스트
                                                                = new();

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="owner">스킬 소유자</param>
    /// <param name="executer">액티브 스킬 실행기</param>
    public SkillSystemPresenter(GameObject owner, ActiveSkillExecuter executer,
                                                                        GameInputReader ownerInput = null)
    {
        // 스킬 데이터를 담을 리스트
        List<BaseSkillData> skillDatas = new();
        // 현재 챕터의 스킬 데이터 받아오기
        SkillDatabase.FindDatasByChapter(curChapter, skillDatas);   // 나중에 현재 챕터 부분 수정해야 함

        // 스킬 데이터가 있다면
        if (skillDatas.Count != 0)
        {
            // 스킬 모델 생성하기
            model = new(owner, executer, skillDatas, curChapter, ownerInput);
            // 액티브 스킬 변경 이벤트 구독
            model.OnActiveSkillsChanged += RefreshActiveSkills;
            // UI 레벨업 스킬 선택 이벤트 구독
            EventBus<UILevelUpSkillSelectedEvent>.action += RefreshSelectedSkill;
            // 스킬 스왑(미장착 -> 장착) 이벤트 구독
            EventBus<UIPauseSkillEquipRequestedEvent>.action += RefreshSelectedSkills;
            // 스킬 스왑(장착 -> 장착) 이벤트 구독
            EventBus<UIPauseSkillSwapRequestedEvent>.action += RefreshSelectedSkills;
            // 모든 스킬 새로고침
            RefreshAllSkills();
        }
        // 스킬 데이터가 없다면
        else
            Debug.Log($"[Error | Skill] 스킬 모델 생성 실패 => " +
                        $"입력 - 대상 : {owner.name} / 스킬 데이터 : 없음", owner);
    }

    /// <summary>
    /// 프레젠터가 비활성화될 때 호출하는 함수
    /// </summary>
    public void DisablePresenter()
    {
        // 액티브 스킬 변경 이벤트 구독 해제
        model.OnActiveSkillsChanged -= RefreshActiveSkills;
        // 모델 비활성화
        model.DisableModel();
        // UI 레벨업 스킬 선택 이벤트 구독 해제
        EventBus<UILevelUpSkillSelectedEvent>.action -= RefreshSelectedSkill;
        // 스킬 스왑(미장착 -> 장착) 이벤트 구독 해제
        EventBus<UIPauseSkillEquipRequestedEvent>.action -= RefreshSelectedSkills;
        // 스킬 스왑(장착 -> 장착) 이벤트 구독 해제
        EventBus<UIPauseSkillSwapRequestedEvent>.action -= RefreshSelectedSkills;
    }

    /// <summary>
    /// 모든 스킬 새로고침 함수
    /// </summary>
    public void RefreshAllSkills()
    {
        // 액티브 스킬 새로고침
        RefreshActiveSkills();
        // 패시브 스킬 새로고침
        RefreshPassiveSkills();
    }

    /// <summary>
    /// 액티브 스킬들 새로고침 함수
    /// </summary>
    public void RefreshActiveSkills()
    {
        // 모델이 없다면
        if (model == null)
        {
            Debug.Log($"[Error | Skill] 액티브 스킬들 새로고침 실패 => 입력 - 스킬 모델 : 없음");
            return;
        }

        // 장착한 액티브 스킬들 받아오기
        model.GetEquippedActiveSkills(modelResults);
        // 받아온 결과들을 일시정지(스탯, 스킬) UI 데이터들로 변환하기
        ChangePauseUIDatas(equippedSkillUIDatas);
        // 미장착한 액티브 스킬들 받아오기
        model.GetUnequippedActiveSkills(modelResults);
        // 받아온 결과들을 일시정지(스탯, 스킬) UI 데이터들로 변환하기
        ChangePauseUIDatas(unequippedSkillUIDatas);
        
        // 미장착한 액티브 스킬들이 없다면
        if(unequippedSkillUIDatas.Count == 0)
        {
            // 스킬 데이터베이스에서 다음 챕터 스킬 데이터들 받아오기
            SkillDatabase.FindDatasByChapter(curChapter + 1, databaseResults);
            // 받아온 결과들을 일시정지(스탯, 스킬) UI 데이터들로 변환하기
            ChangePauseUIDatasByDatabase(unequippedSkillUIDatas);
        }

        // 액티브 스킬 새로고침 이벤트 발행
        EventBus<RefreshUIEvent>.Publish(new RefreshUIEvent(equippedSkillUIDatas.ToArray(),
                                                                unequippedSkillUIDatas.ToArray()));
    }

    /// <summary>
    /// 일시정지 UI 데이터들로 변환하는 함수
    /// </summary>
    /// <param name="datas">데이터들을 저장할 리스트</param>
    private void ChangePauseUIDatas(List<UIPauseSkillInfoData> datas)
    {
        // 스킬 모델의 결과들이 없거나, 비어있다면
        if (modelResults == null || modelResults.Count == 0)
            return;

        // 데이터들을 저장할 리스트 초기화
        datas.Clear();

        // 스킬 모델의 결과들의 수만큼
        foreach (var result in modelResults)
            // 일시정지 UI 데이터 추가하기
            AddPauseUIData(datas, result);
    }

    /// <summary>
    /// 일시정지 UI 데이터로 변환 후, 데이터들 리스트에 추가하는 함수
    /// </summary>
    /// <param name="datas">데이터들을 저장할 리스트</param>
    /// <param name="skill">일시정지 UI 데이터로 변환할 스킬 객체(생략 가능, 기본값 : 비어있음)</param>
    private void AddPauseUIData(List<UIPauseSkillInfoData> datas, SkillInstance skill = null)
    {
        // 데이터들을 저장할 리스트가 없다면
        if (datas == null)
            return;

        // 스킬 객체가 있고 데이터가 있다면
        if (skill != null && skill.BaseData != null)
            // 일시정지 UI 데이터로 변환 후, 데이터들 리스트에 저장
            datas.Add(new UIPauseSkillInfoData(skill.BaseData.Icon, skill.BaseData.SkillName,
                                skill.CurLevel, skill.BaseData.Desc, skill.IsEquipped, skill.BaseData.Id));
        // 스킬이 없다면
        else
            // 일시정지 UI 데이터의 기본값을 데이터들 리스트에 저장
            datas.Add(new UIPauseSkillInfoData(null, "", 0, "", false));
    }

    /// <summary>
    /// 일시정지 UI 데이터들로 변환하는 함수
    /// </summary>
    /// <param name="datas">데이터들을 저장할 리스트</param>
    private void ChangePauseUIDatasByDatabase(List<UIPauseSkillInfoData> datas)
    {
        // 스킬 데이터베이스의 결과들이 없거나, 비어있다면
        if (databaseResults == null || databaseResults.Count == 0)
            return;

        // 데이터들을 저장할 리스트 초기화
        datas.Clear();

        // 스킬 데이터베이스의 결과들의 수만큼
        foreach (var result in databaseResults)
            // 일시정지 UI 데이터 추가하기
            AddPauseUIDataByData(datas, result);
    }

    /// <summary>
    /// 일시정지 UI 데이터로 변환 후, 데이터들 리스트에 추가하는 함수
    /// </summary>
    /// <param name="datas">데이터들을 저장할 리스트</param>
    /// <param name="skill">일시정지 UI 데이터로 변환할 스킬 데이터(생략 가능, 기본값 : 비어있음)</param>
    private void AddPauseUIDataByData(List<UIPauseSkillInfoData> datas, BaseSkillData skill = null)
    {
        // 데이터들을 저장할 리스트가 없다면
        if (datas == null)
            return;

        // 스킬 데이터가 있다면
        if (skill != null)
            // 일시정지 UI 데이터로 변환 후, 데이터들 리스트에 저장
            datas.Add(new UIPauseSkillInfoData(skill.Icon, skill.SkillName, 1, skill.Desc, false,
                                                                                        skill.Id, false));
        // 스킬이 없다면
        else
            // 일시정지 UI 데이터의 기본값을 데이터들 리스트에 저장
            datas.Add(new UIPauseSkillInfoData(null, "", 0, "", false));
    }

    /// <summary>
    /// 패시브 스킬 새로고침 함수
    /// </summary>
    public void RefreshPassiveSkills()
    {
        // 모델이 없다면
        if (model == null)
        {
            Debug.Log($"[Error | Skill] 패시브 스킬들 새로고침 실패 => 입력 - 스킬 모델 : 없음");
            return;
        }

        // 장착한 패시브 스킬들 받아오기
        model.GetEquippedPassiveSkills(modelResults);
        // 받아온 결과들을 일시정지 UI 데이터로 변환하기
        ChangePauseUIDatas(equippedSkillUIDatas);
        // 미장착한 스킬 UI 데이터 초기화
        unequippedSkillUIDatas.Clear();
        // 패시브 스킬 새로고침 이벤트 발행
        EventBus<RefreshUIEvent>.Publish(new RefreshUIEvent(equippedSkillUIDatas.ToArray(),
                                                                unequippedSkillUIDatas.ToArray(),
                                                                isActiveSkill: false));
    }

    public void GetCanEnhanceSkillUIDatas(List<UIPauseSkillInfoData> results)
    {
        if (model == null)
            return;

        model.GetCanEnhanceSkills(modelResults);

        if (modelResults == null || modelResults.Count == 0)
            return;

        BaseSkillData data;
        results.Clear();

        foreach (var skill in modelResults)
        {
            if (skill.BaseData == null)
                continue;

            data = skill.BaseData;
            results.Add(new(data.Icon, data.SkillName, skill.CurLevel, data.Desc, skill.IsEquipped, data.Id));
        }
    }

    /// <summary>
    /// 특정 스킬 새로고침 함수
    /// </summary>
    /// <param name="skillUIData">선택한 스킬 UI 데이터</param>
    public void RefreshSelectedSkill(UILevelUpSkillSelectedEvent skillUIData)
    {
        // 모델이 없다면
        if (model == null)
        {
            Debug.Log($"[Error | Skill] 스킬 레벨업 실패 => 입력 - 스킬 모델 : 없음");
            return;
        }

        SkillInstance skill;

        // 레벨업이 불가능한 스킬이라면
        if ((skill = model.EnhanceSkill(skillUIData.SkillId)) == null)
        {
            Debug.Log($"[Error | Skill] 스킬 레벨업 실패 => " +
                        $"입력 - 스킬 ID : {skillUIData.SkillId} / 레벨업 불가");
            return;
        }
        else if (skill != null)
        {
            if (skill.IsActiveSkill)
                RefreshActiveSkills();
            else
                RefreshPassiveSkills();
        }
    }

    /// <summary>
    /// 특정 스킬들 새로고침 함수
    /// </summary>
    /// <param name="skillUIData">선택한 스킬 UI 데이터들</param>
    public void RefreshSelectedSkills(UIPauseSkillEquipRequestedEvent skillUIData)
    {
        // 모델이 없다면
        if (model == null)
        {
            Debug.Log($"[Error | Skill] 스킬 스왑 실패 => 입력 - 스킬 모델 : 없음");
            return;
        }

        // 슬롯 위치 받아오기
        ACTIVE_SKILL_SLOT_TYPE slot = (ACTIVE_SKILL_SLOT_TYPE)skillUIData.TargetSlotIndex;

        // 스킬 스왑에 실패했다면
        if (!model.SwapSkill(slot, skillUIData.SkillId))
        {
            Debug.Log($"[Error | Skill] 스킬 스왑 실패 => " +
                        $"입력 - 스킬 ID : {skillUIData.SkillId} / 변경 위치 : {slot.ToKoreanString()}");
            return;
        }
    }

    /// <summary>
    /// 특정 스킬들 새로고침 함수
    /// </summary>
    /// <param name="skillUIData">선택한 스킬 UI 데이터들</param>
    public void RefreshSelectedSkills(UIPauseSkillSwapRequestedEvent skillUIData)
    {
        // 모델이 없다면
        if (model == null)
        {
            Debug.Log($"[Error | Skill] 스킬 스왑 실패 => 입력 - 스킬 모델 : 없음");
            return;
        }

        // 슬롯 위치 받아오기
        ACTIVE_SKILL_SLOT_TYPE slot = (ACTIVE_SKILL_SLOT_TYPE)skillUIData.TargetSlotIndex;
        // 변경할 스킬의 ID 받아오기
        int skillId = model.GetEquippedActiveSkillId(skillUIData.SourceSlotIndex);

        // 스킬 스왑에 실패했다면
        if (!model.SwapSkill(slot, skillId))
        {
            Debug.Log($"[Error | Skill] 스킬 스왑 실패 => " +
                        $"입력 - 스킬 ID : {skillId} / 변경 위치 : {slot.ToKoreanString()}");
            return;
        }
    }

    /// <summary>
    /// 액티브 스킬 실행 함수
    /// </summary>
    public void ExecuteActiveSkill(ACTIVE_SKILL_SLOT_TYPE slot)
    {
        // 모델이 없다면
        if (model == null)
        {
            Debug.Log($"[Error | Skill] 액티브 스킬 실행 실패 => 입력 - 스킬 모델 : 없음");
            return;
        }

        // 액티브 스킬 실행
        model.ExecuteActiveSkill(slot);
    }

    /// <summary>
    /// 액티브 스킬 시간 진행 함수
    /// </summary>
    public void TickActiveSkills(float time)
    {
        // 모델이 없다면
        if (model == null)
        {
            Debug.Log($"[Error | Skill] 액티브 스킬 실행 실패 => 입력 - 스킬 모델 : 없음");
            return;
        }

        // 액티브 스킬 시간 진행
        model.TickActiveSkills(time);
    }

    #region 플레이어 쪽에서 추가한 함수
    public CheckpointSkillSnapshot[] CaptureCheckpointSnapshot()
            => model != null ? model.CaptureCheckpointSnapshot() : Array.Empty<CheckpointSkillSnapshot>();

    public void RestoreCheckpointSnapshot(CheckpointSkillSnapshot[] snapshot)
    {
        if (model == null)
            return;

        model.RestoreCheckpointSnapshot(snapshot);
        RefreshPassiveSkills();
    }
    #endregion
}