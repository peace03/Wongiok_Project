using UnityEngine;
using System.Collections;

public class ActiveSkillExecuter : MonoBehaviour, IProjectileSkill, IAreaSkill
{
    [Header("실행 위치")]
    [SerializeField] private Transform executePosition;             // 실행 위치
    [Header("(임시)총알 프리팹")]
    [SerializeField] private GameObject bullet;                     // 총알 프리팹

    private Coroutine skillCoroutine;                               // 스킬 코루틴
    private WaitForSeconds fireDelayTime;                           // 사격 딜레이 시간

    // 발사체 스킬 실행 함수
    public void ExecuteSkill(ProjectileSkillLevelData skillData)
    {
        // 스킬 코루틴이 비어있지 않다면
        if (skillCoroutine != null)
        {
            Debug.Log($"[Skill] 발사체 액티브 스킬 실행 실패 => 스킬 진행 중");
            return;
        }
        // 총알 프리팹이 없다면
        else if (bullet == null)
        {
            Debug.LogError($"[Error | Skill] 발사체 액티브 스킬 실패 => 입력 - (임시)총알 : 없음");
            return;
        }

        Debug.Log($"[Skill] 발사체 액티브 스킬 실행 => 총 {skillData.ProjectileCount}개");

        // 사격 딜레이 시간 구하기
        fireDelayTime = new WaitForSeconds(skillData.MaxDuration / (skillData.ProjectileCount == 0 ?
                                                                        1 : skillData.ProjectileCount));
        // 총알 발사
        skillCoroutine = StartCoroutine(BulletFireRoutine(skillData.ProjectileCount));
    }

    // 영역 스킬 실행 함수
    public void ExecuteSkill(AreaSkillLevelData skillData)
    {

    }

    // 총알 발사 코루틴 함수
    private IEnumerator BulletFireRoutine(int count)
    {
        // 발사체 수만큼
        for (int i = 0; i < count; i++)
        {
            // 소유자의 앞에 총알 생성
            Instantiate(bullet, executePosition.transform.position, executePosition.transform.rotation);
            Debug.Log($"[Skill] 총알 {i + 1} / {count} 번째 생성");
            // 사격 딜레이 시간만큼 대기
            yield return fireDelayTime;
        }

        // 스킬 코루틴 초기화
        skillCoroutine = null;
    }
}