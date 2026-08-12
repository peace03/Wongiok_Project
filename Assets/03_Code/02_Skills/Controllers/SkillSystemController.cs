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

    private GameInputReader ownerInput;                             // 소유자 입력 시스템

    private bool ownerIsGrounded = true;                            // 소유자 땅에 있음 여부
    private bool canExecutingSkill = true;                          // 스킬 실행 가능 여부

    public ISkillSystemProvider Presenter => presenter ?? null;
    public int Priority => (int)InitOrder.Skill + 1;                // 중요도

    // 스킬 사용 가능 여부 이벤트 구독
    private void OnEnable() => EventBus<CanExecutingActiveSkill>.action += SetOwnerIsGrounded;

    private void Update()
    {
        // 시간이 멈춰있거나, 프레젠터가 없거나, 실행기가 없다면
        if (Time.timeScale <= 0f || presenter == null || executer == null)
            return;

        // 장착한 액티브 스킬들 시간 진행
        presenter.TickActiveSkills(Time.deltaTime);
        // 스킬 실행 가능 여부 받아오기
        canExecutingSkill = presenter.GetCanExecutingSkill() && !executer.ExecutingSkill;

        // 소유자가 공중에 있거나, 스킬 실행이 불가능하다면
        if (!ownerIsGrounded || !canExecutingSkill)
            return;

        // A키를 눌렀다면
        if (ownerInput.SkillAPressed)
            // A 슬롯 액티브 스킬 실행
            ExecuteSkill(ACTIVE_SKILL_SLOT_TYPE.A);

        // S키를 눌렀다면
        if (ownerInput.SkillSPressed)
            // S 슬롯 액티브 스킬 실행
            ExecuteSkill(ACTIVE_SKILL_SLOT_TYPE.S);

        // D키를 눌렀다면
        if (ownerInput.SkillDPressed)
            // D 슬롯 액티브 스킬 실행
            ExecuteSkill(ACTIVE_SKILL_SLOT_TYPE.D);
    }

    private void OnDisable()
    {
        // 프레젠터 비활성화 함수 호출
        presenter.DisablePresenter();
        // 스킬 사용 가능 여부 이벤트 구독 해제
        EventBus<CanExecutingActiveSkill>.action -= SetOwnerIsGrounded;
    }

    // 초기화 함수
    public void Init()
    {
        // 스킬 데이터베이스 초기화
        SkillDatabase.Init();

        // 소유자가 없거나, 실행기가 없다면
        if (owner == null || executer == null)
            return;

        // 소유자 입력 시스템 받아오기
        ownerInput = owner.GetComponent<GameInputReader>();
        // 프레젠터 생성
        presenter = new(owner, executer, ownerInput);

        // 실행기의 따라다니는 대상이 소유자가 아니라면
        if (executer.transform.parent != owner.transform)
        {
            // 실행기의 위치, 각도를 소유자로 설정
            executer.transform.SetPositionAndRotation(owner.transform.position, owner.transform.rotation);
            // 실행기의 따라다니는 대상을 소유자로 설정
            executer.transform.SetParent(owner.transform, true);
        }
    }

    /// <summary>
    /// 스킬 실행 함수
    /// </summary>
    /// <param name="slot">실행할 스킬 위치</param>
    private void ExecuteSkill(ACTIVE_SKILL_SLOT_TYPE slot)
    {
        // 프레젠터가 없거나, 스킬 실행이 불가능하다면
        if (presenter == null || !canExecutingSkill)
            return;

        // 해당 슬롯의 액티브 스킬 실행
        presenter.ExecuteActiveSkill(slot);
    }

    /// <summary>
    /// 스킬 사용 가능 여부 설정 함수
    /// </summary>
    /// <param name="eventData">상태가 변한 객체 정보</param>
    private void SetOwnerIsGrounded(CanExecutingActiveSkill eventData)
    {
        // 소유자와 다른 객체라면
        if (owner != eventData.charactor)
            return;

        // 소유자의 현재 상태 반영
        ownerIsGrounded = eventData.isGrounded;
    }

    #region 플레이어 쪽에서 추가한 함수
    public CheckpointSkillSnapshot[] CaptureCheckpointSnapshot()
                                    => presenter != null ? presenter.CaptureCheckpointSnapshot()
                                                            : System.Array.Empty<CheckpointSkillSnapshot>();

    public void RestoreCheckpointSnapshot(CheckpointSkillSnapshot[] snapshot)
    {
        if (presenter == null)
            return;

        presenter.RestoreCheckpointSnapshot(snapshot);
    }
    #endregion
}