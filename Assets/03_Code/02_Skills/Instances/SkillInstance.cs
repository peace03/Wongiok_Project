using System;
using System.Collections.Generic;
using UnityEngine;

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

    private readonly Dictionary<ACTIVE_SKILL_EFFECT_TYPE,               // 이펙트 종류별 실행 중인 이펙트들
                                    List<Effect>> activeEffects = new();

    private readonly List<GameObject> effectPrefabs = new();            // 이펙트 프리팹들

    [NonSerialized] private readonly GameObject owner;                  // 스킬 소유자
    [NonSerialized] private readonly ActiveSkillExecuter executer;      // 액티브 스킬 실행기

    private readonly int curFps;                                        // 현재 프레임
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
    /// <summary>
    /// 스킬의 현재 레벨
    /// </summary>
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
    /* 사용하고 있지 않은 프로퍼티
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
    */
    #endregion

    /// <summary>
    /// 생성자
    /// </summary>
    public SkillInstance(GameObject owner, ActiveSkillExecuter executer, BaseSkillData data, int fps)
    {
        this.owner = owner;
        this.executer = executer;
        this.data = data;
        // 현재 프레임 구하기
        curFps = fps > 0f ? fps : 60;
        // 이펙트 종류마다 실행 중인 이펙트들 초기화
        InitActiveEffects();
        // 스킬 취소 이벤트 구독
        EventBus<CancelSkill>.action += CancelSkill;
    }

    /// <summary>
    /// 이펙트 종류마다 실행 중인 이펙트들 초기화 함수
    /// </summary>
    private void InitActiveEffects()
    {
        activeEffects[ACTIVE_SKILL_EFFECT_TYPE.Charging] = new List<Effect>();
        activeEffects[ACTIVE_SKILL_EFFECT_TYPE.Target] = new List<Effect>();
    }

    /// <summary>
    /// 스킬 객체가 비활성화될 때 호출하는 함수
    /// </summary>
    public void DisableInstance() => EventBus<CancelSkill>.action -= CancelSkill;

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

        //Debug.Log($"[Skill] 사용 시작 => {data.SkillName}");
        // 무기 외형 착용 이벤트 발행
        EventBus<ChangeWeaponState>.Publish(new ChangeWeaponState(data.Id));
        // 현재 쿨타임 초기화
        curCoolTime = 0f;

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
    /// <param name="change">스킬 상태</param>
    /// <param name="effectClear">이펙트 초기화 여부</param>
    private void SwitchState(SKILL_STATE change, bool effectClear = true)
    {
        // 현재 상태가 차징이였다면
        if(IsCharging)
        {
            // 차징 이펙트 종료
            StopEffects(ACTIVE_SKILL_EFFECT_TYPE.Charging, effectClear);
            // 타겟 이펙트 종료
            StopEffects(ACTIVE_SKILL_EFFECT_TYPE.Target, effectClear);
        }

        // 현재 상태 바꾸기
        state = change;
        Debug.Log($"[Skill] {state.ToKoreanString()} => {data.SkillName}");

        // 바꾼 상태가 사용 가능이라면
        if (IsReady)
            Debug.Log("스킬 슬롯 UI에 반짝거리는 이펙트가 필요하다면 이벤트 보내기");
        // 바꾼 상태가 실행이라면
        else if (IsExecuting)
        {
            // 소유자가 없다면
            if (owner == null)
            {
                //Debug.Log($"[Error | Skill] 사용 불가 => " +
                //            $"입력 - {data.SkillName} : Lv.{curLevel} / 소유자(Owner) : 없음");
                return;
            }

            // 차징 시간이 있는 스킬이라면 ? 히트 스탑 프레임을 현재 프레임의 3/4 : 즉발 스킬이라면 현재 프레임의 절반
            int hitStopFrame = data.GetMaxChargingTime(curLevel) > 0f ? (curFps / 4) * 3 : curFps / 2;
            // 스킬 시작 히트 스탑 이벤트 발행
            EventBus<HitStopEvent>.Publish(new HitStopEvent(hitStopFrame, TimeEffectSource.Skill,
                                                TimeEffectPriority.Medium, TimeEffectGroups.CombatFeel));
            // 현재 지속 시간 초기화
            curDuration = 0f;
            // 스킬 실행
            data.ExecuteSkill(owner, curLevel);
        }
        // 바꾼 상태가 차징 상태라면
        else if (IsCharging)
        {
            // 실행 위치들의 수만큼
            foreach(var place in executer.ExecutePlaces)
            {
                // 차징 이펙트 실행
                ExecuteEffects(ACTIVE_SKILL_EFFECT_TYPE.Charging, owner.transform, Vector3.up);
                // 타겟 찾기
                var target = GetLastTarget(owner.transform.position + Vector3.up, place.forward, 10.25f);
                
                // 타겟을 찾았다면
                if (target != null)
                    // 타겟 이펙트 실행
                    ExecuteEffects(ACTIVE_SKILL_EFFECT_TYPE.Target, target);
            }

            // 현재 차징 시간 초기화
            curChargingTime = 0f;
        }
    }

    /// <summary>
    /// 이펙트 종류별 이펙트들 실행 함수
    /// </summary>
    /// <param name="type">이펙트 종류</param>
    /// <param name="place">실행 위치</param>
    /// <param name="pos">추가 위치(생략 가능, 기본값 : 없음)</param>
    /// <param name="rot">추가 각도(생략 가능, 기본값 : 없음)</param>
    /// <param name="target">따라다닐 대상(생략 가능, 기본값 : 없음)</param>
    private void ExecuteEffects(ACTIVE_SKILL_EFFECT_TYPE type, Transform place, Vector3? pos = null,
                                                                                    Transform target = null)
    {
        // 이펙트 종류에 맞는 이펙트 프리팹 받아오기
        data.AsActiveSkillData.GetEffectsByEffectType(type, effectPrefabs);

        // 받아온 이펙트 프리팹이 없다면
        if (effectPrefabs.Count == 0)
            return;

        // 이펙트 프리팹의 수만큼
        foreach (var prefab in effectPrefabs)
        {
            // 이펙트 실행 후 받아오기
            var effect = EffectManager.Instance.PlayEffect(prefab, place.position + (pos ?? Vector3.zero),
                                                        place.rotation, parent : target != null ? target : place);
            
            // 실행 중인 이펙트들에 이펙트 종류가 없다면
            if (!activeEffects.ContainsKey(type))
            {
                //Debug.Log($"[Skill] 이펙트 종류[{type.ToKoreanString()}] 추가 => " +
                //            $"입력 - 스킬 ID : {data.Id} / 스킬 이름 : {data.SkillName}");
                activeEffects[type] = new List<Effect>();
            }

            // 받아온 이펙트 추가
            activeEffects[type].Add(effect);
        }
    }

    /// <summary>
    /// 이펙트 종류별 이펙트들 종료 함수
    /// </summary>
    /// <param name="type">이펙트 종류</param>
    /// <param name="immediately">즉시 종료 여부(기본값 : 즉시 종료 안함)</param>
    private void StopEffects(ACTIVE_SKILL_EFFECT_TYPE type, bool immediately = false)
    {
        // 이펙트 종류에 해당하는 이펙트들이 없다면
        if(!activeEffects.TryGetValue(type, out var effects))
        {
            //Debug.Log($"[Skill] 이펙트 종료 실패 => 입력 - {type.ToKoreanString()}");
            return;
        }

        // 이펙트들의 수만큼
        foreach(var effect in effects)
        {
            // 이펙트가 없거나, 이펙트가 비활성화 되어있다면
            if (effect == null || !effect.gameObject.activeSelf)
                continue;

            // 이펙트 종료
            effect.StopEffect(immediately);
        }

        // 이펙트들 초기화
        effects.Clear();
    }

    /// <summary>
    /// 마지막 타겟 반환 함수
    /// </summary>
    /// <param name="position">시작 위치</param>
    /// <param name="direction">탐색 방향</param>
    /// <param name="distance">탐색 사거리</param>
    private Transform GetLastTarget(Vector3 position, Vector3 direction, float distance)
    {
        // 특정 위치에서, 특정 방향으로 사거리만큼 보이지 않는 레이저를 쏴서 부딪힌 물체 받아오기
        var hits = Physics.RaycastAll(position, direction, distance);

        // 부딪힌 물체가 없다면
        if (hits.Length == 0)
            return null;

        // 부딪힌 물체들을 실행 위치와의 거리를 기준으로 내림차순(큰 -> 작)으로 정렬하기
        Array.Sort(hits, (a, b) => b.distance.CompareTo(a.distance));
        // 인덱스를 저장할 변수
        int index;

        // 데미지를 입을 수 없는 물체의 인덱스를 찾는 데에 실패했다면(전부 데미지를 입을 수 있는 물체들이라면)
        if ((index = Array.FindIndex(hits, hit => !hit.collider.isTrigger
                                                    && !hit.transform.TryGetComponent<IDamageable>(out _))) == -1)
        {
            // 부딪힌 물체들의 수만큼
            foreach(var hit in hits)
                // 데미지를 입을 수 있고 소유자와 같은 레이어를 가지고 있지 않다면
                if (hit.transform.TryGetComponent<IDamageable>(out _)
                            && hit.transform.gameObject.layer != owner.layer)
                    // 위치 반환
                    return hit.transform;

            return null;
        }

        // 데미지를 입을 수 없는 마지막 물체를 제외한 나머지 물체들의 수만큼
        for(int i = index - 1; i >= 0; i--)
        {
            // 데미지를 입을 수 있고 소유자와 같은 레이어를 가지고 있지 않다면
            if (hits[i].transform.TryGetComponent<IDamageable>(out _)
                        && hits[i].transform.gameObject.layer != owner.layer)
                // 위치 반환
                return hits[i].transform;
        }

        Debug.Log("저긴가");
        return null;
    }

    /// <summary>
    /// [이벤트] 스킬 취소 함수
    /// </summary>
    /// <param name="cancel">취소할 스킬 정보(정보 없음))</param>
    public void CancelSkill(CancelSkill cancel) => CancelSkill();

    /// <summary>
    /// 스킬 취소 함수
    /// </summary>
    public bool CancelSkill()
    {
        // 차징 상태가 아니라면
        if (!IsCharging)
            return false;

        // 차징이 끝났다면
        if (curChargingTime >= Math.Max(0f, data.GetMaxChargingTime(curLevel)))
        {
            // 실행 상태로 변경(이펙트 초기화 X)
            SwitchState(SKILL_STATE.Executing, false);
            return false;
        }

        //Debug.Log($"[Skill] 사용 취소 => {data.SkillName}");
        // 무기 외형 착용 해제 이벤트 발행
        EventBus<ChangeWeaponState>.Publish(new ChangeWeaponState(data.Id, false));
        // 쿨타임 상태로 변경
        SwitchState(SKILL_STATE.CoolTime);
        return true;
    }

    /// <summary>
    /// 스킬 시간 진행 함수
    /// </summary>
    public void Tick(float time)
    {
        // 패시브 스킬이거나, 사용 가능 상태라면
        if (!IsActiveSkill || IsReady)
            return;

        // 쿨타임 진행
        curCoolTime = Math.Clamp(curCoolTime + time, 0f, Math.Max(0f, data.GetMaxCoolTime(curLevel)));

        // 차징 상태라면
        if (IsCharging)
        {
            // 차징 시간 진행
            curChargingTime = Math.Clamp(curChargingTime + time, 0f,
                                            Math.Max(0f, data.GetMaxChargingTime(curLevel)));

            // 차징이 끝났다면
            if (curChargingTime >= Math.Max(0f, data.GetMaxChargingTime(curLevel)))
                // 사용 상태로 변경
                SwitchState(SKILL_STATE.Executing);
        }
        // 실행 상태라면
        else if(IsExecuting)
        {
            // 실행 시간 진행
            curDuration = Math.Clamp(curDuration + time, 0f, Math.Max(0f, data.GetMaxDuration(curLevel)));

            // 실행이 끝났다면
            if (curDuration >= Math.Max(0f, data.GetMaxDuration(curLevel)))
            {
                // 쿨타임이 끝났다면
                if (curCoolTime >= Math.Max(0f, data.GetMaxCoolTime(curLevel)))
                    // 사용 가능 상태로 변경
                    SwitchState(SKILL_STATE.Ready);
                // 쿨타임이 남았다면
                else
                    // 쿨타임 상태로 변경
                    SwitchState(SKILL_STATE.CoolTime);
            }
        }
        // 쿨타임 상태라면
        else if(IsOnCoolTime)
            // 쿨타임이 끝났다면
            if (curCoolTime >= Math.Max(0f, data.GetMaxCoolTime(curLevel)))
                // 사용 가능 상태로 변경
                SwitchState(SKILL_STATE.Ready);
    }

    /// <summary>
    /// 레벨 초기화 함수
    /// </summary>
    public void ResetLevel() => curLevel = 1;
}