using UnityEngine;

public class SkillSystemController : MonoBehaviour, IInitializable
{
    [Header("스킬 소유자")]
    [Tooltip("플레이어, 몬스터, NPC 등등")]
    [SerializeField] private GameObject owner;                      // 소유자
    [Header("스킬 실행기")]
    [SerializeField] private ActiveSkillExecuter executer;          // 실행기
    [Header("스킬 시스템")]
    [SerializeField] private SkillSystemPresenter presenter;        // 프레젠터

    public int Priority => (int)InitOrder.Skill;                    // 중요도

    private void OnEnable()
    {
        // 액티브 스킬 슬롯 키 누름 이벤트 구독
        EventBus<StartedPressSkillSlot>.action += ExecuteSkill;
        // 액티브 스킬 슬롯 키 뗌 이벤트 구독
        EventBus<CanceledPressSkillSlot>.action += CancelSkill;
    }

    // 임시 초기화
    //private void Awake() => Init();

    private void Update()
    {
        #region 임시 Input 시스템
        // A키를 눌렀다면
        if (Input.GetKeyDown(KeyCode.A))
            // A키 누름 이벤트 발행
            EventBus<StartedPressSkillSlot>.Publish(new StartedPressSkillSlot(ACTIVE_SKILL_SLOT_TYPE.A));

        // A키를 취소했다면
        if (Input.GetKeyUp(KeyCode.A))
            // A키 취소 이벤트 발행
            EventBus<CanceledPressSkillSlot>.Publish(new CanceledPressSkillSlot(ACTIVE_SKILL_SLOT_TYPE.A));

        // S키를 눌렀다면
        if (Input.GetKeyDown(KeyCode.S))
            // S키 누름 이벤트 발행
            EventBus<StartedPressSkillSlot>.Publish(new StartedPressSkillSlot(ACTIVE_SKILL_SLOT_TYPE.S));

        // S키를 취소했다면
        if (Input.GetKeyUp(KeyCode.S))
            // S키 취소 이벤트 발행
            EventBus<CanceledPressSkillSlot>.Publish(new CanceledPressSkillSlot(ACTIVE_SKILL_SLOT_TYPE.S));

        // D키를 눌렀다면
        if (Input.GetKeyDown(KeyCode.D))
            // D키 누름 이벤트 발행
            EventBus<StartedPressSkillSlot>.Publish(new StartedPressSkillSlot(ACTIVE_SKILL_SLOT_TYPE.D));

        // D키를 취소했다면
        if (Input.GetKeyUp(KeyCode.D))
            // D키 취소 이벤트 발행
            EventBus<CanceledPressSkillSlot>.Publish(new CanceledPressSkillSlot(ACTIVE_SKILL_SLOT_TYPE.D));
        #endregion

        // 프레젠터가 없다면
        if (presenter == null)
            return;

        // 장착한 액티브 스킬들 시간 진행
        presenter.TickActiveSkills(Time.deltaTime);
    }

    private void OnDisable()
    {
        // 프레젠터 비활성화 함수 호출
        presenter.DisablePresenter();
        // 액티브 스킬 슬롯 누름 이벤트 구독 해제
        EventBus<StartedPressSkillSlot>.action -= ExecuteSkill;
        // 액티브 스킬 슬롯 키 뗌 이벤트 구독 해제
        EventBus<CanceledPressSkillSlot>.action -= CancelSkill;
    }

    // 초기화 함수
    public void Init()
    {
        // 스킬 데이터베이스 초기화
        SkillDatabase.Init();

        // 소유자가 있고 실행기가 있다면
        if (owner != null && executer != null)
        {
            // 프레젠터 생성
            presenter = new(owner, executer);

            // 실행기의 따라다니는 대상이 소유자가 아니라면
            if(executer.transform.parent != owner.transform)
            {
                // 실행기의 위치, 각도를 소유자로 설정
                executer.transform.SetPositionAndRotation(owner.transform.position, owner.transform.rotation);
                // 실행기의 따라다니는 대상을 소유자로 설정
                executer.transform.SetParent(owner.transform, true);
            }

            //Debug.Log($"[Skill] 스킬 시스템 초기화", this);
        }
        // 소유자가 없다면
        else
            Debug.Log($"[Error | Skill] 스킬 시스템 초기화 실패 => 입력 - 소유자(Owner) : 없음");
    }

    /// <summary>
    /// 스킬 실행 함수
    /// </summary>
    public CheckpointSkillSnapshot[] CaptureCheckpointSnapshot()
    {
        return presenter != null
            ? presenter.CaptureCheckpointSnapshot()
            : System.Array.Empty<CheckpointSkillSnapshot>();
    }

    public void RestoreCheckpointSnapshot(
        CheckpointSkillSnapshot[] snapshot)
    {
        presenter?.RestoreCheckpointSnapshot(snapshot);
    }

    private void ExecuteSkill(StartedPressSkillSlot type)
    {
        // 프레젠터가 없다면
        if (presenter == null)
            return;

        // 슬롯 종류에 따라
        switch (type.type)
        {
            // A키라면
            case ACTIVE_SKILL_SLOT_TYPE.A:
                // A키 액티브 스킬 실행
                presenter.ExecuteActiveSkill(ACTIVE_SKILL_SLOT_TYPE.A);
                break;
            // S키라면
            case ACTIVE_SKILL_SLOT_TYPE.S:
                // S키 액티브 스킬 실행
                presenter.ExecuteActiveSkill(ACTIVE_SKILL_SLOT_TYPE.S);
                break;
            // D키라면
            case ACTIVE_SKILL_SLOT_TYPE.D:
                // D키 액티브 스킬 실행
                presenter.ExecuteActiveSkill(ACTIVE_SKILL_SLOT_TYPE.D);
                break;
            // 그 외라면
            default:
                Debug.Log($"[Skill] 스킬 실행 실패 => " +
                            $"입력 - 슬롯 : {type.type.ToKoreanString()}", this);
                break;
        }
    }

    /// <summary>
    /// 스킬 취소 함수
    /// </summary>
    private void CancelSkill(CanceledPressSkillSlot type)
    {
        // 프레젠터가 없다면
        if (presenter == null)
            return;

        // 슬롯 종류에 따라
        switch (type.type)
        {
            // A키라면
            case ACTIVE_SKILL_SLOT_TYPE.A:
                // A키 액티브 스킬 취소
                presenter.CancelActiveSkill(ACTIVE_SKILL_SLOT_TYPE.A);
                break;
            // S키라면
            case ACTIVE_SKILL_SLOT_TYPE.S:
                // S키 액티브 스킬 취소
                presenter.CancelActiveSkill(ACTIVE_SKILL_SLOT_TYPE.S);
                break;
            // D키라면
            case ACTIVE_SKILL_SLOT_TYPE.D:
                // D키 액티브 스킬 취소
                presenter.CancelActiveSkill(ACTIVE_SKILL_SLOT_TYPE.D);
                break;
            // 그 외라면
            default:
                Debug.Log($"[Skill] 스킬 취소 실패 => " +
                            $"입력 - 슬롯 : {type.type.ToKoreanString()}", this);
                break;
        }
    }
}
