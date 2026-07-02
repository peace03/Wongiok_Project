using UnityEngine;
using System.Collections.Generic;

public class BossController : MonoBehaviour, IInitializable
{
    //초기화 순서
    public int Priority => (int)InitOrder.Boss +2;

    private Rigidbody rb;
    private IBossLogics logics; //보스패턴 로직(Cinderella_Patterns)
    private BossStatus status; //능력치
    public Dictionary<State,BossState> bossState { get; }
        = new Dictionary<State, BossState>(); //상태 Dictionary
    private BossState curState; //현재 상태 패턴

    //초기화
    public void Init()
    {
        //rigidbody
        rb = GetComponent<Rigidbody>();
        //능력치
        status = ServiceLocator_Y.Get<BossStatus>();
        //FSM+BT
        logics = GetComponent<IBossLogics>();   //신데렐라 로직 참조
        bossState.Add(State.Spawn,      new SpawningState_Boss(this, logics));
        bossState.Add(State.Idle,       new IdleState_Boss(this, logics));
        bossState.Add(State.Attack,     new AttackingState_Boss(this, logics));
        bossState.Add(State.Ultimate,   new UltimateCastingState_Boss(this, logics));
        bossState.Add(State.Groggy,     new GroggyState_Boss(this, logics, status));
        bossState.Add(State.Defeated,   new DefeatedState_Boss(this, logics));

        curState = bossState[State.Spawn];
        curState?.Enter();
        //Debug.Log("BossController Init()실행 완료");
    }

    private void OnEnable()
    {
        EventBus<UltimateInvokeEvent>.action += SetUltimateState;
    }
    private void OnDisable()
    {
        EventBus<UltimateInvokeEvent>.action -= SetUltimateState;
    }

    private void FixedUpdate()
    {
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
        curState?.Update();
        if (Input.GetKeyDown(KeyCode.Space)) status.TakeDamage(20);
    }

    public void ChangeState(State state)
    {
        curState?.Exit();
        curState = bossState[state];
        curState?.Enter();
        //Debug.Log($"BossController ChangeState({state})실행 완료");
    }
    //궁극기 발동상태 전환
    public void SetUltimateState(UltimateInvokeEvent data) { ChangeState(State.Ultimate); }
}
