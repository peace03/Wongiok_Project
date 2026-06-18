using UnityEngine;
using System.Collections;

public enum INIT_PRIORITY_TYPE
{
    Player = 0,
    Skill = 100,
    Monster = 200,
    Boss = 300,
    UI = 400
}

public class SkillTestController : MonoBehaviour, IInitializable, IProjectileActive
{
    [SerializeField] private GameObject owner;                      // 소유자
    [SerializeField] private GameObject bullet;                     // 총알 프리팹
    [SerializeField] private SkillSystemPresenter presenter;        // 프레젠터

    private Coroutine skillCoroutine;                               // 스킬 코루틴
    private WaitForSeconds fireDelayTime;                           // 사격 딜레이 시간

    public int Priority => (int)INIT_PRIORITY_TYPE.Skill;           // (임시)중요도
    //public int Priority => (int)InitOrder.Skill;                  // 중요도
    public GameObject Bullet => bullet;

    // 임시 초기화
    void Start() => Init();

    private void Update()
    {
        if (presenter == null)
            return;

        // A키를 눌렀다면
        if (Input.GetKeyDown(KeyCode.A))
            // A키 액티브 스킬 실행
            presenter.ExecuteActiveSkill(ACTIVE_SKILL_SLOT_TYPE.A);

        // S키를 눌렀다면
        if (Input.GetKeyDown(KeyCode.S))
            // S키 액티브 스킬 실행
            presenter.ExecuteActiveSkill(ACTIVE_SKILL_SLOT_TYPE.S);

        // D키를 눌렀다면
        if (Input.GetKeyDown(KeyCode.D))
            // D키 액티브 스킬 실행
            presenter.ExecuteActiveSkill(ACTIVE_SKILL_SLOT_TYPE.D);
    }

    public void Init()
    {
        // 스킬 데이터베이스 초기화
        SkillDatabase.Init();

        // 소유자가 있다면
        if (owner != null)
        {
            // 프레젠터 생성
            presenter = new(owner);
            Debug.Log($"[Skill] 스킬 시스템 초기화", this);
        }
        // 소유자가 없다면
        else
            Debug.LogError($"[Error | Skill] 스킬 시스템 초기화 실패 => 입력 - 소유자(Owner) : 없음");
    }

    // 발사체 액티브 스킬 실행 함수
    public void ExecuteProjectileActiveSkill(ProjectileSkillLevelData levelData)
    {
        // 스킬 코루틴이 비어있지 않다면
        if (skillCoroutine != null)
        {
            Debug.Log($"[Skill] 발사체 액티브 스킬 실행 실패 => 스킬 진행 중");
            return;
        }
        // 소유자가 없다면
        else if (owner == null)
        {
            Debug.LogError($"[Error | Skill] 발사체 액티브 스킬 실패 => 입력 - 소유자 : 없음");
            return;
        }
        // 총알 프리팹이 없다면
        else if (bullet == null)
        {
            Debug.LogError($"[Error | Skill] 발사체 액티브 스킬 실패 => 입력 - (임시)총알 : 없음");
            return;
        }

        Debug.Log($"[Skill] 발사체 액티브 스킬 실행 => 총 {levelData.ProjectileCount}개");

        // 사격 딜레이 시간 구하기
        fireDelayTime = new WaitForSeconds(levelData.MaxDuration / (levelData.ProjectileCount == 0 ?
                                                                        1 : levelData.ProjectileCount));
        // 총알 발사
        skillCoroutine = StartCoroutine(BulletFireRoutine(owner, levelData.ProjectileCount));
    }

    // 총알 발사 코루틴 함수
    private IEnumerator BulletFireRoutine(GameObject owner, int count)
    {
        // 발사체 수만큼
        for (int i = 0; i < count; i++)
        {
            // 소유자의 앞에 총알 생성
            Instantiate(bullet, owner.transform.position + owner.transform.forward, owner.transform.rotation);
            Debug.Log($"[Skill] 총알 {i + 1} / {count} 번째 생성");
            // 사격 딜레이 시간만큼 대기
            yield return fireDelayTime;
        }

        // 스킬 코루틴 초기화
        skillCoroutine = null;
    }
}