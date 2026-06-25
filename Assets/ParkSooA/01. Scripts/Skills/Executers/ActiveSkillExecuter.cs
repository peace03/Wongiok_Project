using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ActiveSkillExecuter : MonoBehaviour, IProjectileSkill, IAreaSkill
{
    [Header("실행 위치")]
    [Tooltip("액티브 스킬 효과가 실행될 위치이자 해당 레이어로 스킬 판정 필터링을 할 예정임")]
    [SerializeField] private Transform executePosition;                             // 실행 위치
    [Header("총알 공장")]
    [Tooltip("오브젝트 풀링 적용, 이것으로 스킬에 사용될 총알을 가져올 예정임")]
    [SerializeField] private BulletFactory bulletFactory;                           // 총알 공장

    private Dictionary<ActiveSkillLevelData, GameObject> weapons = new();           // 모든 무기들
    private Dictionary<ActiveSkillLevelData, List<ActiveSkillEffect>> effects       // 모든 효과들
                                                                        = new();
    private GameObject curWeapon;                                                   // 현재 무기
    private List<ActiveSkillEffect> curEffects;                                     // 현재 효과들

    private Coroutine skillCoroutine;                                               // 스킬 코루틴
    private WaitForSeconds fireDelayTime;                                           // 사격 딜레이 시간

    private LayerMask skillLayer;                                                   // 스킬 레이어

    // 스킬 레이어 초기화(실행 위치의 레이어로 설정)
    private void Awake() => skillLayer = 1 << executePosition.gameObject.layer;

    /// <summary>
    /// 발사체 스킬 실행 함수
    /// </summary>
    public void ExecuteSkill(ProjectileSkillLevelData skillData)
    {
        // 스킬 코루틴이 비어있지 않다면
        if (skillCoroutine != null)
        {
            Debug.Log($"[Skill] 발사체 액티브 스킬 실행 실패 => 스킬 진행 중");
            return;
        }

        Debug.Log($"[Skill] 발사체 액티브 스킬 실행 => 총 {skillData.ProjectileCount}개");
        // 사격 딜레이 시간 구하기
        fireDelayTime = new WaitForSeconds(skillData.MaxDuration /
            (skillData.ProjectileCount == 0 ? 1 : skillData.ProjectileCount));
        // 발사체 스킬 실행
        skillCoroutine = StartCoroutine(BulletFireRoutine(skillData.ProjectileCount,
                                        skillData.GetDamage(), skillData.PenetrationCount));
    }

    /// <summary>
    /// 영역 스킬 실행 함수
    /// </summary>
    public void ExecuteSkill(AreaSkillLevelData skillData)
    {

    }

    /// <summary>
    /// 이펙트 실행 함수
    /// </summary>
    private void ExecuteEffect()
    {
    }

    /// <summary>
    /// 총알 발사 코루틴 함수
    /// </summary>
    private IEnumerator BulletFireRoutine(int bulletCount, float damage, int penetrationCount)
    {
        // 발사체 수만큼
        for (int i = 0; i < bulletCount; i++)
        {
            // 발사 시작(실행 위치, 스킬 레이어, 데미지, 관통 횟수)
            bulletFactory.GetBullet().StartFire(executePosition, skillLayer, damage, penetrationCount);
            Debug.Log($"[Skill] 발사체 {i + 1} 번째 발사");
            // 사격 딜레이 시간만큼 대기
            yield return fireDelayTime;
        }

        // 스킬 코루틴 초기화
        skillCoroutine = null;
    }
}