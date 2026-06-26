using UnityEngine;

public class SkillSystemController : MonoBehaviour, IInitializable
{
    [Header("스킬 소유자")]
    [Tooltip("플레이어, 몬스터, NPC 등등")]
    [SerializeField] private GameObject owner;                      // 소유자
    [Header("스킬 시스템")]
    [SerializeField] private SkillSystemPresenter presenter;        // 프레젠터

    public int Priority => (int)InitOrder.Skill;                    // 중요도

    // 액티브 스킬 슬롯 누름 이벤트 구독
    private void OnEnable() => EventBus<PressedSkillSlot>.action += ExecuteSkill;

    // 임시 초기화
    private void Awake() => Init();

    private void Update()
    {
        // A키를 눌렀다면
        if (Input.GetKeyDown(KeyCode.A))
            // A키 누름 이벤트 발행
            EventBus<PressedSkillSlot>.Publish(new PressedSkillSlot(ACTIVE_SKILL_SLOT_TYPE.A));

        // S키를 눌렀다면
        if (Input.GetKeyDown(KeyCode.S))
            // S키 누름 이벤트 발행
            EventBus<PressedSkillSlot>.Publish(new PressedSkillSlot(ACTIVE_SKILL_SLOT_TYPE.S));

        // D키를 눌렀다면
        if (Input.GetKeyDown(KeyCode.D))
            // D키 누름 이벤트 발행
            EventBus<PressedSkillSlot>.Publish(new PressedSkillSlot(ACTIVE_SKILL_SLOT_TYPE.D));

        // 프레젠터가 없다면
        if (presenter == null)
            return;

        // 장착한 액티브 스킬들 시간 진행
        presenter.TickActiveSkills(Time.deltaTime);
    }

    // 액티브 스킬 슬롯 누름 이벤트 구독 해제
    private void OnDisable() => EventBus<PressedSkillSlot>.action -= ExecuteSkill;

    // 초기화 함수
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

    // 스킬 실행 함수
    private void ExecuteSkill(PressedSkillSlot type)
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
                Debug.LogWarning($"[Skill] 스킬 실행 실패 => " +
                                    $"입력 - 슬롯 : {type.type.ToKoreanString()}", this);
                break;
        }
    }
}