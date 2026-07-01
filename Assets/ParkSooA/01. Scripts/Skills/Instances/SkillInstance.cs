using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class SkillInstance
{
    #region 변수
    [Header("스킬의 정보")]
    [Tooltip("스킬에 대한 데이터")]
    [SerializeField] private BaseSkillData data;                        // 스킬 정보
    [Header("스킬의 상태")]
    [Tooltip("현재 스킬 상태\n[사용 가능 / 쿨타임 중 / 실행 중 / 차징 중]")]
    [SerializeField] private SKILL_STATE state = SKILL_STATE.Ready;     // 스킬 상태
    [Header("스킬의 레벨")]
    [Tooltip("스킬의 현재 레벨\n플레이어가 게임 플레이 중 올릴 수 있는 레벨")]
    [SerializeField] private int curLevel = 1;                          // 현재 레벨
    [Header("스킬의 쿨타임")]
    [Tooltip("스킬의 현재 쿨타임\n0에서부터 프레임 단위로 최대 쿨타임까지 올라감")]
    [SerializeField] private float curCoolTime = 0f;                    // 현재 쿨타임
    [Header("스킬의 지속 시간")]
    [Tooltip("스킬의 현재 지속 시간\n0에서부터 프레임 단위로 최대 지속 시간까지 올라감")]
    [SerializeField] private float curDuration = 0f;                    // 현재 지속 시간
    [Header("스킬의 차징 시간")]
    [Tooltip("스킬의 현재 차징 시간\n0에서부터 프레임 단위로 최대 차징 시간까지 올라감")]
    [SerializeField] private float curChargingTime = 0f;                // 현재 차징 시간

    [NonSerialized] private List<SkillInstance> equippedActives;        // 장착된 액티브 스킬 목록
    [NonSerialized] private List<SkillInstance> equippedPassives;       // 장착된 패시브 스킬 목록

    [NonSerialized] private readonly GameObject owner;                  // 스킬 소유자
    #endregion

    #region 프로퍼티
    /// <summary>
    /// 기본 스킬 정보
    /// </summary>
    public BaseSkillData BaseData => data;
    /// <summary>
    /// 액티스 스킬 정보
    /// </summary>
    public ActiveSkillData ActiveData => IsActiveSkill ? data as ActiveSkillData : null;
    /// <summary>
    /// 패시브 스킬 정보
    /// </summary>
    public PassiveSkillData PassiveData => !IsActiveSkill ? data as PassiveSkillData : null;
    public int CurLevel => curLevel;
    /// <summary>
    /// 액티브 스킬 여부
    /// </summary>
    public bool IsActiveSkill => data.Type == SKILL_TYPE.Active;
    /// <summary>
    /// 스킬 장착 여부
    /// </summary>
    public bool IsEquipped => IsActiveSkill ? equippedActives.Contains(this)
                                                : equippedPassives.Contains(this);
    /// <summary>
    /// 강화 가능 여부
    /// </summary>
    public bool CanEnhance => curLevel < data.MaxLevel;
    /// <summary>
    /// 스킬 사용 가능 여부
    /// </summary>
    public bool IsReady => state == SKILL_STATE.Ready;
    /// <summary>
    /// 스킬 쿨타임 진행 여부
    /// </summary>
    public bool IsOnCoolTime => state == SKILL_STATE.CoolTime;
    /// <summary>
    /// 스킬 사용 진행 여부
    /// </summary>
    public bool IsExecuting => state == SKILL_STATE.Executing;
    /// <summary>
    /// 스킬 사용 전 차징 여부
    /// </summary>
    public bool IsCharging => state == SKILL_STATE.Charging;
    /// <summary>
    /// 쿨타임 비율
    /// </summary>
    public float CoolTimeRatio =>
        1f - (data.GetMaxCoolTime(curLevel) <= 0f ? 0f : curCoolTime / data.GetMaxCoolTime(curLevel));
    /// <summary>
    /// 지속 시간 비율
    /// </summary>
    public float DurationRatio =>
        1f - (data.GetMaxDuration(curLevel) <= 0f ? 0f : curDuration / data.GetMaxDuration(curLevel));
    /// <summary>
    /// 차징 시간 비율
    /// </summary>
    public float ChargingTimeRatio =>
        1f - (data.GetMaxChargingTime(curLevel) <= 0f ?
                                            0f : curChargingTime / data.GetMaxChargingTime(curLevel));
    #endregion

    /// <summary>
    /// 생성자
    /// </summary>
    public SkillInstance(GameObject owner, BaseSkillData data)
    {
        this.owner = owner;
        this.data = data;
    }

    /// <summary>
    /// 장착된 스킬 설정 함수
    /// </summary>
    public void SetEquippedSkills(List<SkillInstance> active, List<SkillInstance> passive)
    {
        equippedActives = active;
        equippedPassives = passive;
    }

    /// <summary>
    /// 스킬 레벨 상승 함수
    /// </summary>
    public void LevelUp()
    {
        // 강화 불가능이라면
        if(!CanEnhance)
        {
            Debug.Log($"[Skill] 강화 불가 => {data.SkillName} : Lv.{curLevel}");
            return;
        }

        // 레벨 증가
        curLevel = Math.Clamp(curLevel + 1, 1, Math.Max(1, data.MaxLevel));

        // 패시브 스킬이라면
        if(!IsActiveSkill)
            // 스킬 사용
            data.ExecuteSkill(owner, curLevel);
    }

    /// <summary>
    /// 스킬 사용 함수
    /// </summary>
    public void UseSkill()
    {
        // 사용 가능한 상태가 아니라면
        if (!IsReady)
        {
            Debug.Log($"[Skill] {state.ToKoreanString()} => {data.SkillName}");
            return;
        }

        Debug.Log($"[Skill] 사용 시작 => {data.SkillName}");

        // 차징 시간이 없다면
        if (data.GetMaxChargingTime(curLevel) <= 0f)
            // 실행 상태로 변경
            SwitchState(SKILL_STATE.Executing);
        // 차징 시간이 있다면
        else
            // 차징 상태로 변경
            SwitchState(SKILL_STATE.Charging);
    }

    /// <summary>
    /// 스킬 상태 변경 함수
    /// </summary>
    private void SwitchState(SKILL_STATE change)
    {
        // 현재 상태 바꾸기
        state = change;
        Debug.Log($"[Skill] {state.ToKoreanString()} => {data.SkillName}");

        // 사용 가능 상태라면
        if (IsReady)
        {
            Debug.Log("스킬 슬롯 UI에 반짝거리는 이펙트가 필요하다면 채우기");
            // 차징 이펙트 종료 이벤트 발행
            EventBus<StopActiveSkillEffect>.Publish(new StopActiveSkillEffect(data.Id,
                                                                ACTIVE_SKILL_EFFECT_TYPE.Charging));
            // 타겟(과녁) 이펙트 종료 이벤트 발행
            EventBus<StopActiveSkillEffect>.Publish(new StopActiveSkillEffect(data.Id,
                                                                ACTIVE_SKILL_EFFECT_TYPE.Target));
        }
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
                Debug.Log($"[Error | Skill] 사용 불가 => " +
                            $"입력 - {data.SkillName} : Lv.{curLevel} / 소유자(Owner) : 없음");
                return;
            }

            // 차징 이펙트 종료 이벤트 발행
            EventBus<StopActiveSkillEffect>.Publish(new StopActiveSkillEffect(data.Id,
                                                                ACTIVE_SKILL_EFFECT_TYPE.Charging));
            // 타겟(과녁) 이펙트 종료 이벤트 발행
            EventBus<StopActiveSkillEffect>.Publish(new StopActiveSkillEffect(data.Id,
                                                                ACTIVE_SKILL_EFFECT_TYPE.Target));
            // 현재 지속 시간 초기화
            curDuration = 0f;
            // 스킬 실행
            data.ExecuteSkill(owner, curLevel);
        }
        // 차징 상태라면
        else if (IsCharging)
        {
            // 현재 차징 시간 초기화
            curChargingTime = 0f;
            // 차징 이펙트 실행 이벤트 발행
            EventBus<ExecuteActiveSkillEffect>.Publish(new ExecuteActiveSkillEffect(data.Id,
                                                                ACTIVE_SKILL_EFFECT_TYPE.Charging));
            // 타겟(과녁) 이펙트 실행 이벤트 발행
            EventBus<ExecuteActiveSkillEffect>.Publish(new ExecuteActiveSkillEffect(data.Id,
                                                            ACTIVE_SKILL_EFFECT_TYPE.Target,
                                                pos: GetLastTargetPosition(owner.transform, 25f)));
        }
    }

    /// <summary>
    /// 마지막 타겟 위치 반환 함수
    /// </summary>
    private Vector3? GetLastTargetPosition(Transform origin, float distance)
    {
        // 원하는 위치에서 전방으로 사거리만큼 보이지 않는 레이저를 쏴서 부딪힌 물체 받아오기
        var hits = Physics.RaycastAll(origin.position, origin.forward, distance);

        // 부딪힌 물체가 없다면
        if (hits.Length == 0)
            return null;

        // 부딪힌 물체들을 실행 위치와의 거리를 기준으로 오름차순으로 정렬하기
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        // 인덱스를 저장할 변수
        int index;

        // 데미지를 입을 수 없는 물체의 인덱스를 찾는 데에 실패했다면(전부 데미지를 입을 수 있는 물체들이라면)
        if ((index = Array.FindIndex(hits, hit
                                            => !hit.transform.TryGetComponent<IDamageable>(out _))) == -1)
        {
            // 부딪힌 물체들의 수만큼
            foreach(var hit in hits)
            {
                // 데미지를 입을 수 있고 소유자와 같은 레이어를 가지고 있지 않다면
                if (hit.transform.TryGetComponent<IDamageable>(out _)
                    && hit.transform.gameObject.layer != owner.layer)
                    // 위치 반환
                    return hit.transform.position;
            }

            return null;
        }

        // 데미지를 입을 수 없는 마지막 물체를 제외한 나머지 물체들의 수만큼
        for(int i = index - 1; i >= 0; i--)
        {
            // 데미지를 입을 수 있고 소유자와 같은 레이어를 가지고 있지 않다면
            if (hits[i].transform.TryGetComponent<IDamageable>(out _)
                && hits[i].transform.gameObject.layer != owner.layer)
                // 위치 반환
                return hits[i].transform.position;
        }

        return null;
    }

    /// <summary>
    /// 스킬 취소 함수
    /// </summary>
    public void CancelSkill()
    {
        // 차징 상태가 아니라면
        if (!IsCharging)
            return;

        // 차징이 끝났다면
        if (curChargingTime >= Math.Max(0f, data.GetMaxChargingTime(curLevel)))
            // 실행 상태로 변경
            SwitchState(SKILL_STATE.Executing);
        // 차징이 끝나지 않았다면
        else
        {
            Debug.Log($"[Skill] 사용 취소 => {data.SkillName}");
            // 사용 가능 상태로 변경
            SwitchState(SKILL_STATE.Ready);
        }
    }

    /// <summary>
    /// 스킬 시간 진행 함수
    /// </summary>
    public void Tick(float time)
    {
        // 패시브 스킬이거나, 사용 가능 상태라면
        if (!IsActiveSkill || IsReady)
            return;

        // 차징 상태라면
        if(IsCharging)
        {
            // 차징 시간 진행
            curChargingTime = Math.Clamp(curChargingTime + time, 0f,
                                            Math.Max(0f, data.GetMaxChargingTime(curLevel)));

            // 차징이 끝났다면
            if (curChargingTime >= Math.Max(0f, data.GetMaxChargingTime(curLevel)))
                Debug.Log($"[Skill] 차징 완료 => {data.SkillName}");
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

    /// <summary>
    /// 레벨 초기화 함수
    /// </summary>
    public void ResetLevel() => curLevel = 1;
}