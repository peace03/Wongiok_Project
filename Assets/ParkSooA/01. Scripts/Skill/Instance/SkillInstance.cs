using UnityEngine;
using System.Collections.Generic;

public class SkillInstance
{
    private List<SkillInstance> equippedActives;        // 장착된 액티브 스킬 목록
    private List<SkillInstance> equippedPassives;       // 장착된 패시브 스킬 목록

    private BaseSkillData data;                         // 스킬 정보
    private SKILL_STATE state = SKILL_STATE.Ready;      // 스킬 상태
    private int curLevel = 1;                           // 현재 레벨

    // 액티브 일때만 필요함
    private float curCoolTime = 0f;                     // 현재 쿨타임
    private float curDuration = 0f;                     // 현재 지속 시간
    private float curChargingTime = 0f;                 // 현재 차징 시간

    public bool IsActiveSkill => data.Type == SKILL_TYPE.Active;
    public bool IsEquipped => data.Type == SKILL_TYPE.Active ?
        equippedActives.Contains(this) : equippedPassives.Contains(this);
    public bool CanEnhance => curLevel < data.MaxLevel;
    public bool IsReady => state == SKILL_STATE.Ready;
    public bool IsOnCoolTime => state == SKILL_STATE.CoolTime;
    public bool IsExecuting => state == SKILL_STATE.Executing;
    public bool IsCharging => state == SKILL_STATE.Charging;
    //public float CoolTimeRatio => 1f - curCoolTime / data as LevelBasedSkillData
}