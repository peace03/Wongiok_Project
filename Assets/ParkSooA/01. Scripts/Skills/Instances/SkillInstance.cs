using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class SkillInstance
{
    [SerializeField] private GameObject owner;                          // 스킬 소유자
    [SerializeField] private BaseSkillData data;                        // 스킬 정보
    [SerializeField] private SKILL_STATE state = SKILL_STATE.Ready;     // 스킬 상태
    [SerializeField] private int curLevel = 1;                          // 현재 레벨
    [SerializeField] private float curCoolTime = 0f;                    // 현재 쿨타임
    [SerializeField] private float curDuration = 0f;                    // 현재 지속 시간
    [SerializeField] private float curChargingTime = 0f;                // 현재 차징 시간

    private List<SkillInstance> equippedActives;                        // 장착된 액티브 스킬 목록
    private List<SkillInstance> equippedPassives;                       // 장착된 패시브 스킬 목록

    public BaseSkillData Data => data;
    // 액티브 스킬 여부
    public bool IsActiveSkill => data.Type == SKILL_TYPE.Active;
    // 스킬 장착 여부
    public bool IsEquipped => IsActiveSkill ? equippedActives.Contains(this) : equippedPassives.Contains(this);
    // 강화 가능 여부
    public bool CanEnhance => curLevel < data.MaxLevel;
    // 스킬 사용 가능 여부
    public bool IsReady => state == SKILL_STATE.Ready;
    // 스킬 쿨타임 진행 여부
    public bool IsOnCoolTime => state == SKILL_STATE.CoolTime;
    // 스킬 사용 진행 여부
    public bool IsExecuting => state == SKILL_STATE.Executing;
    // 스킬 사용 전 차징 여부
    public bool IsCharging => state == SKILL_STATE.Charging;
    // 쿨타임 비율
    public float CoolTimeRatio =>
        1f - data.GetMaxCoolTime(curLevel) <= 0f ? 0f : curCoolTime / data.GetMaxCoolTime(curLevel);
    // 지속 시간 비율
    public float DurationRatio =>
        1f - data.GetMaxDuration(curLevel) <= 0f ? 0f : curDuration / data.GetMaxDuration(curLevel);
    // 차징 시간 비율
    public float ChargingTimeRatio =>
        1f - data.GetMaxChargingTime(curLevel) <= 0f ? 0f : curChargingTime / data.GetMaxChargingTime(curLevel);

    // 생성자
    public SkillInstance(GameObject owner, BaseSkillData data)
    {
        this.owner = owner;
        this.data = data;
    }

    // 장착된 스킬 설정 함수
    public void SetEquippedSkills(List<SkillInstance> active, List<SkillInstance> passive)
    {
        equippedActives = active;
        equippedPassives = passive;
    }

    // 스킬 레벨 상승 함수
    public void LevelUp()
    {
        // 강화 불가능이라면
        if(!CanEnhance)
        {
            Debug.Log($"[Skill] {data.SkillName}(Lv.{curLevel}) - 강화 불가");
            return;
        }

        // 레벨 증가
        curLevel = Math.Clamp(curLevel + 1, 1, Math.Max(1, data.MaxLevel));

        // 패시브 스킬이라면
        if(!IsActiveSkill)
            // 스킬 사용
            data.ExecuteSkill(owner, curLevel);
    }

    // 스킬 사용 함수
    public void UseSkill()
    {
        // 사용 가능한 상태가 아니라면
        if (!IsReady)
        {
            Debug.Log($"[Skill] {data.SkillName} - {state.ToKoreanString()}");
            return;
        }

        Debug.Log($"[Skill] {data.SkillName} - 사용");

        // 차징 시간이 없다면
        if (data.GetMaxChargingTime(curLevel) <= 0f)
            // 실행 상태로 변경
            SwitchState(SKILL_STATE.Executing);
        // 차징 시간이 있다면
        else
            // 차징 상태로 변경
            SwitchState(SKILL_STATE.Charging);
    }

    // 스킬 상태 변경 함수
    private void SwitchState(SKILL_STATE change)
    {
        // 패시브 스킬이라면
        if (!IsActiveSkill)
        {
            Debug.Log($"[Skill] {data.SkillName} - 패시브 스킬");
            return;
        }

        // 현재 상태 바꾸기
        state = change;
        Debug.Log($"[Skill] {data.SkillName} - {state.ToKoreanString()}");

        // 사용 가능 상태라면
        if (IsReady)
            Debug.Log("스킬 슬롯 UI에 반짝거리는 이펙트가 필요하다면 채우기");
        // 쿨타임 상태라면
        else if (IsOnCoolTime)
            // 현재 쿨타임 초기화
            curCoolTime = 0f;
        // 실행 상태라면
        else if (IsExecuting)
        {
            // 소유자가 없다면
            if(owner == null)
            {
                Debug.Log($"[Error | Skill] {data.SkillName}(Lv.{curLevel}) - 스킬 소유자(Owner) 없음");
                return;
            }

            // 현재 지속 시간 초기화
            curDuration = 0f;
            // 스킬 실행
            data.ExecuteSkill(owner, curLevel);
        }
        // 차징 상태라면
        else if (IsCharging)
            // 현재 차징 시간 초기화
            curChargingTime = 0f;
    }

    // 스킬 시간 진행 함수
    public void Tick(float time)
    {
        // 패시브 스킬이거나, 사용 가능 상태라면
        if (!IsActiveSkill || IsReady)
            return;

        // 차징 상태라면
        if(IsCharging)
        {
            // 차징 시간 진행
            curChargingTime = Math.Clamp(curChargingTime + time, 0f, Math.Max(0f, data.GetMaxChargingTime(curLevel)));

            // 차징이 끝났다면
            if (curChargingTime >= Math.Max(0f, data.GetMaxChargingTime(curLevel)))
                // 실행 상태로 변경
                SwitchState(SKILL_STATE.Executing);
        }
        // 실행 상태라면
        else if(IsExecuting)
        {
            // 실행 시간 진행
            curDuration = Math.Clamp(curDuration + time, 0f, Math.Max(0f, data.GetMaxDuration(curLevel)));

            // 실행이 끝났다면
            if (curDuration >= Math.Max(0f, data.GetMaxDuration(curLevel)))
                // 쿨타임 상태로 변경
                SwitchState(SKILL_STATE.CoolTime);
        }
        // 쿨타임 상태라면
        else if(IsOnCoolTime)
        {
            // 쿨타임 진행
            curCoolTime = Math.Clamp(curCoolTime + time, 0f, Math.Max(0f, data.GetMaxCoolTime(curLevel)));

            // 쿨타임이 끝났다면
            if (curCoolTime >= Math.Max(0f, data.GetMaxCoolTime(curLevel)))
                // 사용 가능 상태로 변경
                SwitchState(SKILL_STATE.Ready);
        }
    }

    // 레벨 초기화 함수
    public void ResetLevel() => curLevel = 1;
}