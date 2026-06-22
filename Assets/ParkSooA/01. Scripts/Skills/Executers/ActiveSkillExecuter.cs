using UnityEngine;
using System.Collections;

public class ActiveSkillExecuter : MonoBehaviour, IProjectileSkill, IAreaSkill
{
    [Header("소유자")]
    [SerializeField] private GameObject owner;                      // 소유자
    [Header("실행 위치")]
    [SerializeField] private Transform executePosition;             // 실행 위치
    [Header("총알 공장(오브젝트 풀링)")]
    [SerializeField] private BulletFactory bulletFactory;           // 총알 공장

    private Coroutine skillCoroutine;                               // 스킬 코루틴
    private WaitForSeconds fireDelayTime;                           // 사격 딜레이 시간

    private LayerMask ownerLayer;                                   // 소유자 레이어

    // 소유자 레이어 초기화
    private void Awake() => InitOwnerLayer();
    
    // 소유자 레이어 초기화 함수
    private void InitOwnerLayer()
    {
        // 소유자가 있다면
        if(owner != null)
        {
            // 소유자의 레이어로 설정
            ownerLayer = 1 << owner.layer;
            return;
        }

        // 실행기의 레이어로 설정
        ownerLayer = 1 << gameObject.layer;
        Debug.LogWarning($"[Skill] 소유자 찾기 실패 => 현재 레이어 : {ownerLayer.value}", this);
    }

    // 발사체 스킬 실행 함수
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
        fireDelayTime = new WaitForSeconds(skillData.MaxDuration / (skillData.ProjectileCount == 0 ?
                                                                        1 : skillData.ProjectileCount));
        // 발사체 스킬 실행
        skillCoroutine = StartCoroutine(BulletFireRoutine(skillData.ProjectileCount, skillData.GetDamage(),
                                                                                    skillData.PenetrationCount));
    }

    // 영역 스킬 실행 함수
    public void ExecuteSkill(AreaSkillLevelData skillData)
    {

    }

    // 총알 발사 코루틴 함수
    private IEnumerator BulletFireRoutine(int bulletCount, float damage, int penetrationCount = 1)
    {
        // 발사체 수만큼
        for (int i = 0; i < bulletCount; i++)
        {
            // 발사 시작(실행 위치, 소유자 레이어, 데미지, 관통 횟수)
            bulletFactory.GetBullet().StartFire(executePosition, ownerLayer, damage, penetrationCount);
            Debug.Log($"[Skill] 발사체 {i + 1} 번째 발사");
            // 사격 딜레이 시간만큼 대기
            yield return fireDelayTime;
        }

        // 스킬 코루틴 초기화
        skillCoroutine = null;
    }
}