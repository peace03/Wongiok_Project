using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class BossController : MonoBehaviour, IInitializable
{
    //초기화 순서
    public int Priority => (int)InitOrder.Boss +2;

    private Rigidbody rb;
    private IBossLogics logics; //보스패턴 로직(Cinderella_Patterns)
    private BossStatus status; //능력치
    private Animator animator;
    private bool isDefeated;

    private const float DefeatPresentationDuration = 5f;
    public Dictionary<State,BossState> bossState { get; }
        = new Dictionary<State, BossState>(); //상태 Dictionary
    private BossState curState; //현재 상태 패턴
    private GameInputReader _input;

    //초기화
    public void Init()
    {
        //rigidbody
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        //능력치
        status = ServiceLocator.Get<BossStatus>();
        //FSM+BT
        logics = GetComponent<IBossLogics>();   //보스 로직 참조
        bossState.Add(State.Spawn,      new SpawningState_Boss(this, logics));
        bossState.Add(State.Idle,       new IdleState_Boss(this, logics));
        bossState.Add(State.Attack,     new AttackingState_Boss(this, logics));
        bossState.Add(State.Ultimate,   new UltimateCastingState_Boss(this, logics));
        bossState.Add(State.Groggy,     new GroggyState_Boss(this, logics, status));
        bossState.Add(State.Defeated,   new DefeatedState_Boss(this, logics));

        isDefeated = false;
        curState = bossState[State.Spawn];
        curState?.Enter();
        //Debug.Log("BossController Init()실행 완료");
    }

    private void OnEnable()
    {
        EventBus<UltimateInvokeEvent>.action += SetUltimateState;
        EventBus<BossDeadEvent>.action += SetDefeatedState;
    }
    private void OnDisable()
    {
        EventBus<UltimateInvokeEvent>.action -= SetUltimateState;
        EventBus<BossDeadEvent>.action -= SetDefeatedState;
    }

    private void FixedUpdate()
    {
        if (isDefeated)
            return;

        // 1. 보스가 공중에 있고, 현재 아래로 떨어지는 중일 때만 작동 (y 속도가 0보다 작을 때)
        if (rb.linearVelocity.y < 0 && !logics.IsPhysicsOverridden)
        {
            // 2. 유니티 기본 중력(Physics.gravity.y)에 배율을 곱하여 매 틱마다 아래로 강하게 끌어내림
            // ForceMode.Acceleration을 사용하면 질량(Mass)에 상관없이 순수하게 가속도만 더해집니다.
            Vector3 extraGravity = Vector3.up * Physics.gravity.y * (50f - 1);
            rb.AddForce(extraGravity, ForceMode.Acceleration);
        }
        curState?.FixedUpdate();
    }
    private void Update()
    {
        if (isDefeated)
            return;

        curState?.Update();
        //if (Input.GetKeyDown(KeyCode.Space)) status.TakeDamage(10);
    }

    public void ChangeState(State state)
    {
        if (isDefeated && state != State.Defeated)
            return;

        curState?.Exit();
        curState = bossState[state];
        curState?.Enter();
        //Debug.Log($"BossController ChangeState({state})실행 완료");
    }
    //궁극기 발동상태 전환
    public void SetUltimateState(UltimateInvokeEvent data) { ChangeState(State.Ultimate); }

    //보스가 죽을 때 상태 전환
    private void SetDefeatedState(BossDeadEvent data)
    {
        Debug.Log("죽음!!!!!!!!!!!!!!");
        if (isDefeated)
            return;

        isDefeated = true;
        ChangeState(State.Defeated);

        // 보스가 죽으면 현재 공격의 패링 창도 함께 닫는다.
        EventBus<CanParryEvent>.Publish(
            new CanParryEvent(logics.GetAttackId(), false));
        EventBus<ColliderToggleEvent>.Publish(
            new ColliderToggleEvent(logics.GetAttackId(), false));

        if (rb != null)
            rb.linearVelocity = Vector3.zero;

        if (animator != null)
            animator.SetInteger("Num", Random.Range((int)Animation.Defeated1, (int)Animation.Defeated3 + 1));

        StartCoroutine(DefeatPresentation());
    }

    private IEnumerator DefeatPresentation()
    {
        yield return new WaitForSeconds(DefeatPresentationDuration);

        EventBus<BossDeathPresentationFinishedEvent>.Publish(default);
        gameObject.SetActive(false);
    }
}
