using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveSkillExecuter : MonoBehaviour, IProjectileSkill, IAreaSkill
{
    [Header("기본 실행 위치")]
    [Tooltip("무기 외형이 없을 때 액티브 스킬이 실행될 위치이자 해당 레이어로 스킬 판정 필터링을 할 예정임")]
    [SerializeField] private Transform defaultExecutePos;                           // 기본 실행 위치
    [Header("총알 공장")]
    [Tooltip("오브젝트 풀링 적용, 이것으로 스킬에 사용될 총알을 가져올 예정임")]
    [SerializeField] private BulletFactory bulletFactory;                           // 총알 공장

    private readonly List<Transform> executePlaces = new();                         // 실행 위치들
    private readonly List<GameObject> effectPrefabs = new();                        // 이펙트 프리팹들

    private WaitForSeconds projectileDelayTime;                                     // 발사체 스킬 딜레이 시간
    private WaitForSeconds areaDelayTime;                                           // 범위 스킬 딜레이 시간

    private LayerMask skillLayer;                                                   // 스킬 레이어

    // 액티브 스킬 실행 위치들 변경 이벤트 구독
    private void OnEnable() => EventBus<ChangeActiveSkillExecutePositions>.action += SetExecutePositions;

    private void Awake()
    {
        // 스킬 레이어 초기화(실행 위치의 레이어로 설정)
        skillLayer = 1 << defaultExecutePos.gameObject.layer;
        // 실행 위치들 초기화
        ResetExecutePositions();
    }

    // 액티브 스킬 실행 위치들 변경 이벤트 구독 해제
    private void OnDisable() => EventBus<ChangeActiveSkillExecutePositions>.action -= SetExecutePositions;

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
    public void ExecuteSkill(int id, ProjectileSkillLevelData skillData)
    {
        // 스킬 ID로 스킬 정보 찾기
        var data = SkillDatabase.FindDataById(id);

        // 총구 이펙트에 해당하는 이펙트 프리팹 받아오기
        data.AsActiveSkillData.GetEffectsByEffectType(ACTIVE_SKILL_EFFECT_TYPE.Muzzle, effectPrefabs);

        // 이펙트 프리팹들의 수만큼
        foreach (var prefab in effectPrefabs)
            // 실행 위치들의 수만큼
            foreach (var place in executePlaces)
                // 이펙트 실행하기(지속 시간이 있다면 ? 지속 시간만큼, 아니라면 이펙트 시간만큼)
                EventBus<EffectPlayData>.Publish(new EffectPlayData(prefab, place.position, place.rotation,
                    skillData.MaxDuration > 0f ? skillData.MaxDuration : null));

        // 발사체 이펙트에 해당하는 이펙트 프리팹 받아오기
        data.AsActiveSkillData.GetEffectsByEffectType(ACTIVE_SKILL_EFFECT_TYPE.Main, effectPrefabs);

        // 발사체 스킬 딜레이 시간 구하기
        projectileDelayTime = new WaitForSeconds(skillData.MaxDuration /
                            (skillData.ProjectileCount == 0 ? 1 : skillData.ProjectileCount));
        // 발사체 스킬 실행
        StartCoroutine(ProjectileRoutine(skillData.ProjectileCount, skillData.GetDamage(),
                                                                        skillData.PenetrationCount));
    }

    /// <summary>
    /// 발사체 스킬 코루틴 함수
    /// </summary>
    /// <param name="bulletCount">발사체 개수</param>
    /// <param name="damage">데미지</param>
    /// <param name="penetrationCount">관통 횟수</param>
    private IEnumerator ProjectileRoutine(int bulletCount, float damage, int penetrationCount)
    {
        // 현재 발사체 개수를 저장할 변수
        int curBulletCount = 0;

        // 실행 위치들의 수만큼
        foreach (var place in executePlaces)
        {
            // 발사체 개수만큼
            for (; curBulletCount < bulletCount; curBulletCount++)
            {
                // 총알 가져오기
                var bullet = bulletFactory.GetBullet();
                // 총알 위치와 각도 설정하기
                bullet.transform.SetPositionAndRotation(place.position, place.rotation);

                // 총알 이펙트들의 수만큼
                foreach (var prefab in effectPrefabs)
                {
                    // 이펙트 실행 및 실행한 이펙트 받아오기
                    var effect = EffectManager.Instance.PlayEffect(prefab, new Vector3(0, 0, -0.5f),
                                                    bullet.transform.rotation, parent: bullet.transform);

                    // 나선 이펙트라면
                    if (effect.TryGetComponent<IWaveEffect>(out var wave))
                        // 나선 이펙트 정보 설정하기
                        wave.SetInfo();
                }

                // 총알 발사 시작(실행 위치, 스킬 레이어, 데미지, 관통 횟수)
                bullet.StartFire(place, skillLayer, damage, penetrationCount);
                // 발사체 스킬 딜레이 시간만큼 대기
                yield return projectileDelayTime;
            }
        }

        // 실행 위치들 초기화
        ResetExecutePositions();
    }

    /// <summary>
    /// 범위 스킬 실행 함수
    /// </summary>
    public void ExecuteSkill(int id, AreaSkillLevelData skillData)
    {
        // 실행할 스킬 단계가 없다면
        if (skillData.Stages.Count == 0)
        {
            Debug.Log($"[Skill] 범위 액티브 스킬 실행 실패 => 입력 - 스킬 ID : {id} / 스킬 단계 : 없음");
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