using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class ActiveSkillExecuter : MonoBehaviour, IProjectileSkill, IAreaSkill
{
    [Header("기본 실행 위치")]
    [Tooltip("무기 외형이 없을 때 액티브 스킬이 실행될 위치이자 해당 레이어로 스킬 판정 필터링을 할 예정임")]
    [SerializeField] private Transform defaultExecutePos;                           // 기본 실행 위치
    [Header("총알 공장")]
    [Tooltip("오브젝트 풀링 적용, 이것으로 스킬에 사용될 총알을 가져올 예정임")]
    [SerializeField] private BulletFactory bulletFactory;                           // 총알 공장
    [Header("(임시)최대 사거리")]
    [Tooltip("마지막 적을 탐지하는 최대 사거리임\n따라서, 실제 스킬 사거리와는 다를 수 있음")]
    [SerializeField] private float maxDistance = 30f;                               // 최대 사거리

    private readonly List<Transform> executePositions = new();                      // 실행 위치들

    private Coroutine skillCoroutine;                                               // 스킬 코루틴
    private WaitForSeconds fireDelayTime;                                           // 사격 딜레이 시간

    private LayerMask skillLayer;                                                   // 스킬 레이어

    private void Awake()
    {
        // 스킬 레이어 초기화(실행 위치의 레이어로 설정)
        skillLayer = 1 << defaultExecutePos.gameObject.layer;
        // 실행 위치들 초기화 함수
        ResetExecutePositions();
    }

    /// <summary>
    /// 실행 위치들 설정 함수
    /// </summary>
    public void SetExecutePositions(List<Transform> positions)
    {
        // 실행 위치들 초기화
        executePositions.Clear();

        // 위치들의 수만큼
        foreach (var pos in positions)
            // 위치가 비어있지 않다면
            if (pos != null)
                // 실행 위치 추가
                executePositions.Add(pos);

        // 실행 위치가 없다면
        if (executePositions.Count == 0)
            // 기본 실행 위치 추가
            executePositions.Add(defaultExecutePos);
    }

    /// <summary>
    /// 실행 위치들 초기화 함수
    /// </summary>
    public void ResetExecutePositions()
    {
        // 실행 위치들 초기화
        executePositions.Clear();
        // 기본 실행 위치 추가
        executePositions.Add(defaultExecutePos);
    }

    /// <summary>
    /// 발사체 스킬 실행 함수
    /// </summary>
    public void ExecuteSkill(int id, ProjectileSkillLevelData skillData)
    {
        // 스킬 코루틴이 비어있지 않다면
        if (skillCoroutine != null)
        {
            Debug.Log($"[Skill] 발사체 액티브 스킬 실행 실패 => 스킬 진행 중");
            return;
        }

        Debug.Log($"[Skill] 발사체 액티브 스킬 실행 => 총 {skillData.ProjectileCount}개");
        // 타겟(과녁) 이펙트 실행 이벤트 발행
        EventBus<ExecuteActiveSkillEffect>.Publish(new ExecuteActiveSkillEffect(id,
                                                        ACTIVE_SKILL_EFFECT_TYPE.Target,
                                            pos : GetLastTargetPosition(defaultExecutePos, maxDistance)));
        // 사격 딜레이 시간 구하기
        fireDelayTime = new WaitForSeconds(skillData.MaxDuration /
                            (skillData.ProjectileCount == 0 ? 1 : skillData.ProjectileCount));
        // 발사체 스킬 실행
        skillCoroutine = StartCoroutine(BulletFireRoutine(skillData.ProjectileCount,
                                        skillData.GetDamage(), skillData.PenetrationCount));
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

        // 데미지를 입을 수 없는 물체의 인덱스를 찾는 데에 실패했다면
        if ((index = Array.FindIndex(hits, hit
                                            => !hit.transform.TryGetComponent<IDamageable>(out _))) == -1)
        {
            // 제일 마지막 물체가 데미지를 입을 수 있다면
            if (hits[^1].transform.TryGetComponent<IDamageable>(out _))
                // 콜라이더의 위치 반환
                return hits[^1].transform.position;
            // 데미지를 입을 수 없다면
            else
                return null;
        }

        // 인덱스가 범위 안에 있고 데미지를 입을 수 있는 물체라면
        if (index - 1 >= 0 && hits[index - 1].transform.TryGetComponent<IDamageable>(out _))
            // 콜라이더의 위치 반환
            return hits[index - 1].transform.position;

        return null;
    }

    /// <summary>
    /// 총알 발사 코루틴 함수
    /// </summary>
    private IEnumerator BulletFireRoutine(int bulletCount, float damage, int penetrationCount)
    {
        // 발사체 수만큼
        for (int i = 0; i < bulletCount; i += executePositions.Count)
        {
            // 실행 위치들의 수만큼
            foreach (var pos in executePositions)
            {
                // 발사 시작(실행 위치, 스킬 레이어, 데미지, 관통 횟수)
                bulletFactory.GetBullet().StartFire(pos, skillLayer, damage, penetrationCount);
                Debug.Log($"[Skill] 발사체 {i + 1} 번째 발사");
            }

            // 사격 딜레이 시간만큼 대기
            yield return fireDelayTime;
        }

        // 스킬 코루틴 초기화
        skillCoroutine = null;
    }

    /// <summary>
    /// 영역 스킬 실행 함수
    /// </summary>
    public void ExecuteSkill(int id, AreaSkillLevelData skillData)
    {
        List<IDamageable> targets = new();

        for(int i = 0; i < skillData.Stages.Count; i++)
        {
            foreach(var pos in executePositions)
            {
                targets = GetTargetsInArea(pos, skillData.Stages[i].distance,
                                                    skillData.Stages[i].angle * 0.5f);

                foreach (var target in targets)
                    target.TakeDamage(skillData.Stages[i].damage);
            }
        }
    }

    // 범위 안에 있는 타겟들 반환 함수
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
            if(Vector3.Angle(forward.normalized, dir.normalized) <= angle)
                // 데미지를 받을 수 있는 대상이라면
                if (hit.TryGetComponent<IDamageable>(out var target))
                    // 타겟들 리스트에 추가
                    targets.Add(target);
        }

        // 타겟들 리스트 반환
        return targets;
    }
}