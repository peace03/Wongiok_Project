using UnityEngine;
using System;

public enum INIT_PRIORITY_TYPE
{
    Player = 0,
    Skill = 100,
    Monster = 200,
    Boss = 300,
    UI = 400
}

public class SkillTestController : MonoBehaviour, IInitializable
{
    public int Priority => (int)INIT_PRIORITY_TYPE.Skill;           // 중요도

    [SerializeField] private GameObject owner;                      // 소유자
    [SerializeField] private SkillSystemPresenter presenter;        // 프레젠터

    void Start() => Init();

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
            Debug.Log($"[Error | Skill] 스킬 시스템 초기화 실패 => 입력 - 소유자(Owner) 없음");
    }

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
}