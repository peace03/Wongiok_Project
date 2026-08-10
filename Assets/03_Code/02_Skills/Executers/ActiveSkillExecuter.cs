using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveSkillExecuter : MonoBehaviour, IProjectileSkill, IAreaSkill
{
    [Header("기본 실행 위치")]
    [Tooltip("무기 외형이 없을 때 액티브 스킬이 실행될 위치이자 해당 레이어로 스킬 판정 필터링을 할 예정임")]
    [SerializeField] private Transform defaultExecutePos;                   // 기본 실행 위치
    [Header("총알 공장")]
    [Tooltip("오브젝트 풀링 적용, 이것으로 스킬에 사용될 총알을 가져올 예정임")]
    [SerializeField] private BulletFactory bulletFactory;                   // 총알 공장

    private PlayerAnimatorDriver ownerAnimatorDriver;                       // 소유자 애니메이터 시스템

    private WaitForSeconds projectileDelayTime;                             // 발사체 스킬 딜레이 시간
    private WaitForSeconds areaDelayTime;                                   // 범위 스킬 딜레이 시간

    private LayerMask skillLayer;                                           // 스킬 레이어

    private float projectileDelayTimeValue;                                 // 발사체 스킬 딜레이 시간량
    private float startAnimationWaitTime = 0;                               // 시작 애니메이션 대기 시간

    private int curFps;                                                     // 현재 프레임
    private int executingSkillId = -1;                                      // 실행 중인 스킬 ID

    private bool executingSkill = false;                                    // 스킬 실행 중 여부

    private readonly Dictionary<ACTIVE_SKILL_EFFECT_TYPE,                   // 이펙트 종류별 실행 중인 이펙트들
                                    List<Effect>> activeEffects = new();
    private readonly List<GameObject> effectPrefabs = new();                // 이펙트 프리팹들
    private readonly List<Transform> executePlaces = new();                 // 실행 위치들

    public IReadOnlyList<Transform> ExecutePlaces => executePlaces;

    // 액티브 스킬 실행 위치들 변경 이벤트 구독
    private void OnEnable() => EventBus<ChangeActiveSkillExecutePositions>.action += SetExecutePositions;

    private void Awake()
    {
        // 현재 프레임 구하기
        curFps = Mathf.RoundToInt(1f / Time.deltaTime);
        // 스킬 레이어 초기화(실행 위치의 레이어로 설정)
        skillLayer = 1 << defaultExecutePos.gameObject.layer;
        // 실행 위치들 초기화
        ResetExecutePositions();
    }

    // 액티브 스킬 실행 위치들 변경 이벤트 구독 해제
    private void OnDisable()
    {
        EventBus<ChangeActiveSkillExecutePositions>.action -= SetExecutePositions;
        SetExecutingSkill(false);
    }

    /// <summary>
    /// 초기화 함수
    /// </summary>
    /// <param name="driver">소유자 애니메이터 시스템</param>
    public void Initialize(PlayerAnimatorDriver driver) => ownerAnimatorDriver = driver;

    /// <summary>
    /// 실행 위치들 설정 함수
    /// </summary>
    public void SetExecutePositions(ChangeActiveSkillExecutePositions change)
    {
        // 실행 위치들 초기화
        executePlaces.Clear();

        // 위치들의 수만큼
        foreach (var pos in change.positions)
            // 위치가 비어있지 않다면
            if (pos != null)
                // 실행 위치 추가
                executePlaces.Add(pos);

        // 실행 위치가 없다면
        if (executePlaces.Count == 0)
            // 기본 실행 위치 추가
            executePlaces.Add(defaultExecutePos);
    }

    /// <summary>
    /// 실행 위치들 초기화 함수
    /// </summary>
    public void ResetExecutePositions()
    {
        // 실행 위치들 초기화
        executePlaces.Clear();
        // 기본 실행 위치 추가
        executePlaces.Add(defaultExecutePos);
    }

    /// <summary>
    /// 발사체 스킬 실행 함수
    /// </summary>
    /// <param name="skillId">스킬 ID</param>
    /// <param name="levelData">스킬 레벨별 데이터</param>
    public void ExecuteSkill(int skillId, ProjectileSkillLevelData levelData)
    {
        // 실행 중인 스킬이 있다면
        if (executingSkill)
            return;

        // 실행할 스킬 ID 저장
        executingSkillId = skillId;
        // 발사체 스킬 딜레이 시간량 구하기
        projectileDelayTimeValue = levelData.MaxDuration / (levelData.ProjectileCount == 0 ?
                                                                    1 : levelData.ProjectileCount);
        // 발사체 스킬 딜레이 저장하기
        projectileDelayTime = new(projectileDelayTimeValue);

        // 소유자 애니메이터 시스템이 있다면
        if (ownerAnimatorDriver != null)
            // 스킬 시작 애니메이션 대기 시간(차징 시간 제외) 받아오기
            startAnimationWaitTime = Mathf.Max(0f, ownerAnimatorDriver.SkillStartAnimDuration
                                                                                    - levelData.MaxChargingTime);

        if(activeEffects.ContainsKey(ACTIVE_SKILL_EFFECT_TYPE.Main))
            activeEffects[ACTIVE_SKILL_EFFECT_TYPE.Main].Clear();

        // 스킬 실행 중
        SetExecutingSkill(true);
        // 발사체 스킬 실행
        StartCoroutine(ProjectileRoutine(levelData.ProjectileCount, levelData.GetDamage(),
                                                levelData.MaxDuration > 0f ? levelData.MaxDuration : null,
                                                    levelData.PenetrationCount, levelData.MaxChargingTime > 0f));
    }

    /// <summary>
    /// 발사체 스킬 코루틴 함수
    /// </summary>
    /// <param name="bulletCount">발사체 개수</param>
    /// <param name="damage">데미지</param>
    /// <param name="penetrationCount">관통 횟수</param>
    /// <param name="isCharging">차징 여부</param>
    private IEnumerator ProjectileRoutine(int bulletCount, float damage, float? maxDuration,
                                                                        int penetrationCount, bool isCharging)
    {
        float waitTimer = startAnimationWaitTime;

        while(waitTimer > 0f)
        {
            if (!executingSkill)
                yield break;

            waitTimer -= Time.deltaTime;
            yield return null;
        }

        var data = SkillDatabase.FindDataById(executingSkillId);

        if (data == null || data.AsActiveData == null)
        {
            SetExecutingSkill(false);
            yield break;
        }

        foreach (var sound in data.AsActiveData.Sounds)
        {
            if (sound.clip == null)
            {
                //Debug.Log($"[Skill] 사운드 파일 없음 => 입력 - {data.SkillName}");
                continue;
            }

            EventBus<StartControlledSfxEvent>.Publish(new($"{data.Id}_Sound", sound.clip,
                                                                                    sound.volume, false));
        }

        // 실행 위치들의 수만큼
        foreach (var place in executePlaces)
            // 총구 이펙트 실행하기
            ExecuteEffects(data.AsActiveData, ACTIVE_SKILL_EFFECT_TYPE.Muzzle, place);

        Bullet bullet;

        // 현재 발사체 개수만큼
        for (int count = 0; count < bulletCount; count += executePlaces.Count)
        {
            // 실행 위치들의 수만큼
            foreach (var place in executePlaces)
            {
                // 돌격 소총이라면
                if (executingSkillId == (int)ACTIVE_SKILL_ID.Rifle)
                    // 외형 킨 총알 가져오기
                    bullet = bulletFactory.GetBullet();
                // 그 외라면
                else
                    // 외형 끈 총알 가져오기
                    bullet = bulletFactory.GetBullet(false);

                // 총알 위치와 각도 설정하기
                bullet.transform.SetPositionAndRotation(place.position, place.rotation);
                // 총알 이펙트 실행하기
                ExecuteEffects(data.AsActiveData, ACTIVE_SKILL_EFFECT_TYPE.Main, place,
                                                new Vector3(0, 0, -0.5f), bullet.transform, bullet);
                // 총알 속도 구하기
                float bulletSpeed = 50f / (projectileDelayTimeValue == 0f ? 1f : projectileDelayTimeValue);
                // 타격/피격 이펙트에 해당하는 이펙트 프리팹 받아오기
                data.AsActiveData.GetEffectsByEffectType(ACTIVE_SKILL_EFFECT_TYPE.Hit, effectPrefabs);
                // 총알 발사 시작(실행 위치, 스킬 레이어, 데미지, 관통 횟수,
                //                  총알 속도, 카메라 흔들림 값, 타격/피격 이펙트들)
                bullet.StartFire(skillLayer, damage, penetrationCount,
                                    Mathf.Clamp(bulletSpeed, 10f, 50f),
                                    projectileDelayTimeValue > 0f ? 0.1f : (!isCharging ? 1f : 0.5f),
                                                                                            effectPrefabs);

                // 발사체 스킬 딜레이 시간만큼 대기하기
                yield return projectileDelayTime;
            }
        }

        // 총구 이펙트 즉시 종료
        StopEffects(ACTIVE_SKILL_EFFECT_TYPE.Muzzle);

        // 발사체 스킬 딜레이 시간량이 있다면(지속 시간이 있었다면)
        if (projectileDelayTimeValue > 0f)
            // 스킬 종료 히트 스탑 이벤트 발행(현재 프레임의 3/4)
            EventBus<HitStopEvent>.Publish(new((curFps / 4) * 3, TimeEffectSource.Skill,
                                                TimeEffectPriority.Medium, TimeEffectGroups.CombatFeel));
        
        // 실행 위치들 초기화
        ResetExecutePositions();
        // 스킬 실행 끝남
        SetExecutingSkill(false);
    }

    /// <summary>
    /// 이펙트 종류별 이펙트들 실행 함수
    /// </summary>
    /// <param name="data">액티브 스킬 데이터</param>
    /// <param name="type">이펙트 종류</param>
    /// <param name="place">실행 위치</param>
    /// <param name="pos">추가 위치(생략 가능, 기본값 : 없음)</param>
    /// <param name="target">따라다닐 대상(생략 가능, 기본값 : 없음)</param>
    /// <param name="bullet">총알(생략 가능, 기본값 : 없음)</param>
    private void ExecuteEffects(ActiveSkillData data, ACTIVE_SKILL_EFFECT_TYPE type, Transform place,
                                        Vector3? pos = null, Transform target = null, Bullet bullet = null)
    {
        // 이펙트 종류에 맞는 이펙트 프리팹 받아오기
        data.GetEffectsByEffectType(type, effectPrefabs);

        // 받아온 이펙트 프리팹이 없다면
        if (effectPrefabs.Count == 0)
            return;

        // 이펙트 프리팹의 수만큼
        for(int i = 0; i < effectPrefabs.Count; i++)
        {
            // 첫번째(중요도가 가장 높은) 이펙트이고 총알이 있다면
            if (i == 0 && bullet != null)
                // 총알의 콜라이더 크기를 총알 이펙트 크기로 설정
                bullet.SetColliderSize(effectPrefabs[i].transform.localScale);

            // 이펙트 실행 후 받아오기
            var effect = EffectManager.Instance.PlayEffect(effectPrefabs[i],
                                                place.position + (pos ?? Vector3.zero), place.rotation,
                                                                    parent : target != null ? target : place);

            // 나선 이펙트라면
            if (effect.TryGetComponent<IWaveEffect>(out var wave))
                // 나선 이펙트 정보 설정하기
                wave.SetInfo();

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
        if (!activeEffects.TryGetValue(type, out var effects))
        {
            //Debug.Log($"[Skill] 이펙트 종료 실패 => 입력 - {type.ToKoreanString()}");
            return;
        }

        // 이펙트들의 수만큼
        foreach (var effect in effects)
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
    /// 스킬 취소 함수
    /// </summary>
    public void CancelSkill()
    {
        // 실행 중인 스킬이 없다면
        if (!executingSkill)
            return;

        // 스킬 실행 중지
        SetExecutingSkill(false);
        // 스킬 사용 사운드 정지
        EventBus<StopControlledSfxEvent>.Publish(new($"{executingSkillId}_Sound"));
        // 총구 이펙트 즉시 종료
        StopEffects(ACTIVE_SKILL_EFFECT_TYPE.Muzzle, true);
        // 총알 이펙트 리스트 초기화
        activeEffects[ACTIVE_SKILL_EFFECT_TYPE.Main].Clear();
        // 무기 외형 착용 해제 이벤트 발행
        EventBus<ChangeWeaponState>.Publish(new(executingSkillId, false));

        // 소유자 애니메이터 시스템이 없다면
        if (ownerAnimatorDriver == null)
            return;
        // 실행하려는 스킬 ID가 액티브 스킬 ID의 범위를 넘어간다면
        else if (executingSkillId < (int)ACTIVE_SKILL_ID.Start + 1)
            return;

        // 스킬 애니메이션 취소
        ownerAnimatorDriver.CancelSkill((ACTIVE_SKILL_ID)executingSkillId);
        executingSkillId = -1;
    }

    #region 플레이어 쪽에서 추가한 함수
    private void SetExecutingSkill(bool isExecuting)
    {
        if (executingSkill == isExecuting)
            return;

        executingSkill = isExecuting;
        EventBus<PlayerSkillEffectExecutionChangedEvent>.Publish(
            new PlayerSkillEffectExecutionChangedEvent(isExecuting));
    }
    #endregion

    /// <summary>
    /// 범위 스킬 실행 함수
    /// </summary>
    public void ExecuteSkill(int id, AreaSkillLevelData skillData)
    {
        // 실행할 스킬 단계가 없다면
        if (skillData.Stages.Count == 0)
        {
            //Debug.Log($"[Skill] 범위 액티브 스킬 실행 실패 => 입력 - 스킬 ID : {id} / 스킬 단계 : 없음");
            return;
        }

        // 범위 스킬 실행
        StartCoroutine(AreaRoutine(skillData));
    }

    /// <summary>
    /// 범위 스킬 코루틴 함수
    /// </summary>
    private IEnumerator AreaRoutine(AreaSkillLevelData skillData)
    {
        // 최대 지속 시간 받아오기
        float duration = skillData.MaxDuration;
        // 타겟들을 저장할 리스트
        List<IDamageable> targets = new();
        // 스킬 단계 인덱스
        int stageIndex = 0;
        // 범위 스킬 딜레이 시간 구하기
        areaDelayTime = new WaitForSeconds(skillData.Stages[stageIndex].tickInterval);

        // 최대 지속 시간만큼
        while (duration >= 0f)
        {
            // 실행 위치들의 수만큼
            foreach (var pos in executePlaces)
            {
                // 실행 위치에서 범위 안에 있는 타겟들 받아오기
                targets = GetTargetsInArea(pos, skillData.Stages[stageIndex].distance,
                                                    skillData.Stages[stageIndex].angle * 0.5f);

                // 타겟들이 없다면
                if (targets == null)
                    continue;

                // 타겟들의 수만큼
                foreach (var target in targets)
                    // 데미지 전달
                    target.TakeDamage(skillData.Stages[stageIndex].damage);
            }

            // 범위 스킬 딜레이 시간만큼 대기
            yield return areaDelayTime;
            // 지속 시간 감소
            duration = Mathf.Max(0f, duration - skillData.Stages[stageIndex].tickInterval);

            // 스킬 단계가 남아있다면
            if (skillData.Stages.Count > stageIndex + 1)
            {
                // 다음 스킬 단계로
                stageIndex++;
                // 다음 범위 스킬 딜레이 시간 구하기
                areaDelayTime = new WaitForSeconds(skillData.Stages[stageIndex].tickInterval);
            }
            // 지속 시간이 끝났다면
            else if (duration == 0f)
                break;
        }

        // 실행 위치들 초기화
        ResetExecutePositions();
    }

    /// <summary>
    /// 범위 안의 타겟들 반환 함수
    /// </summary>
    private List<IDamageable> GetTargetsInArea(Transform origin, float distance, float angle)
    {
        // 원하는 위치에서 사거리의 반지름의 구의 범위에서 스킬 소유자를 제외한 나머지 대상 받아오기
        var hits = Physics.OverlapSphere(origin.position, distance, ~skillLayer.value);
        // 타겟들을 저장할 리스트
        List<IDamageable> targets = new();
        // 방향들을 저장할 변수들
        Vector3 dir, forward;

        // 받아온 대상들의 수만큼
        foreach (var hit in hits)
        {
            // 대상과의 방향 저장
            dir = hit.transform.position - origin.position;
            // 정면 방향 구하기
            forward = origin.forward;
            // 높이 초기화
            forward.y = dir.y = 0;

            // 대상이 범위(각도) 안에 있다면
            if (Vector3.Angle(forward.normalized, dir.normalized) <= angle)
                // 데미지를 받을 수 있는 대상이라면
                if (hit.TryGetComponent<IDamageable>(out var target))
                    // 타겟들 리스트에 추가
                    targets.Add(target);
        }

        // 타겟들 리스트 반환
        return targets;
    }
}
